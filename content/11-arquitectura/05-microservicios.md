---
slug: microservicios
title: "Microservicios"
order: 5
estimatedMinutes: 18
summary: "Entendé qué problema resuelven los microservicios, qué costo introducen y cómo evitar la trampa de distribuir un sistema sin necesidad."
quiz:
  - question: "¿Qué problema resuelven mejor los microservicios que un monolito modular?"
    options:
      - "Escribir menos código"
      - "Desplegar y escalar partes del sistema de forma independiente"
      - "Evitar los tests"
      - "Eliminar la necesidad de bases de datos"
    answer: 1
    explanation: "La independencia de despliegue y escala por servicio es el beneficio central. El resto suele empeorar."
  - question: "¿Qué es una transacción distribuida en microservicios?"
    options:
      - "Una transacción que abarca varios servicios y requiere coordinación"
      - "Una transacción local de una base"
      - "Un backup automático"
      - "Un tipo de índice"
    answer: 0
    explanation: "Cuando una operación toca datos de dos servicios, ya no hay una transacción ACID simple; se usan sagas o compensaciones."
  - question: "¿Qué indica un alto acoplamiento entre dos servicios?"
    options:
      - "Que se comunican por HTTP"
      - "Que cambian juntos con frecuencia o comparten modelo de datos"
      - "Que exponen JSON"
      - "Que usan contenedores"
    answer: 1
    explanation: "Si cada cambio en uno obliga a cambiar el otro, la separación es ficticia: probablemente sean un solo servicio."
  - question: "¿Qué práctica ayuda a operar microservicios sin caos?"
    options:
      - "Logs, trazas distribuidas, health checks y contratos versionados"
      - "Desplegar todo manualmente"
      - "Compartir la misma base entre todos"
      - "Ocultar errores para no generar ruido"
    answer: 0
    explanation: "La observabilidad y los contratos explícitos son requisitos operativos, no extras."
---

# Microservicios

## Objetivo

Comprender qué ganan y qué cuestan los microservicios, cuándo elegirlos y qué prácticas mínimas requiere operarlos sin degradar el sistema.

## Requisitos previos

Haber leído [Monolito modular](/modulos/arquitectura/monolito-modular). Necesitás manejar APIs HTTP, contenedores a nivel básico y bases de datos relacionales.

## Concepto

Un microservicio es un servicio pequeño, desplegable de forma independiente, con una responsabilidad de negocio acotada y su propio almacenamiento. "Pequeño" se refiere al alcance, no a la cantidad de líneas.

Beneficios reales:

- Despliegue independiente por equipo.
- Escala selectiva del servicio que lo necesita.
- Aislamiento de fallos: uno caído no tumba todo si se diseña bien.
- Elección de tecnología por servicio, aunque en la práctica conviene homogeneizar.

Costos igual de reales:

| Problema | Impacto |
|---|---|
| Red entre servicios | Latencia, timeouts, reintentos e idempotencia |
| Transacciones distribuidas | Sagas, compensaciones y consistencia eventual |
| Contratos | Versionado, compatibilidad y despliegues coordinados |
| Observabilidad | Trazas, correlación de logs y métricas por servicio |
| Infraestructura | Contenedores, orquestación, service discovery, secretos |
| Depuración | Un flujo atraviesa cinco servicios y tres colas |

La pregunta correcta no es "¿microservicios sí o no?" sino **¿qué necesidad operativa me obliga a separar?** Si no hay una respuesta concreta —escalar un módulo específico, equipos que no pueden coordinarse, un dominio que requiere ciclos de release distintos— el monolito modular es más barato.

La regla de oro: **no distribuyas lo que todavía no entendés**. Un sistema mal separado en servicios es un monolito distribuido, con todos los problemas de ambos y ninguno de los beneficios.

## Ejemplo mínimo

Un servicio con contrato explícito, health check e idempotencia:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!);

builder.Services.AddScoped<ReservaService>();

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapPost("/reservas", async (
    ReservaRequest request,
    ReservaService reservas,
    CancellationToken cancellationToken) =>
{
    // Idempotencia: el mismo Id no duplica la reserva.
    var existente = await reservas.ObtenerAsync(request.IdempotencyKey, cancellationToken);
    if (existente is not null)
    {
        return TypedResults.Ok(existente);
    }

    var reserva = await reservas.CrearAsync(request, cancellationToken);
    return TypedResults.Created($"/reservas/{reserva.Id}", reserva);
});

app.Run();

public sealed record ReservaRequest(Guid IdempotencyKey, Guid ProductoId, int Cantidad);
```

```bash
curl -i -X POST http://localhost:5001/reservas \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: 7c1f5b1e" \
  -d '{"idempotencyKey":"4a7b...","productoId":"9c2d...","cantidad":2}'
```

```text
HTTP/1.1 201 Created
X-Correlation-Id: 7c1f5b1e
```

La clave de idempotencia evita que un reintento de red cree dos reservas. Sin ella, los reintentos se convierten en duplicados.

## Ejemplo aplicado

Saga para una operación que cruza servicios: crear pedido, reservar stock y cobrar. No hay transacción única; hay compensaciones.

```csharp
public sealed class CrearPedidoSaga(
    IPedidosClient pedidos,
    IStockClient stock,
    IPagosClient pagos,
    ILogger<CrearPedidoSaga> logger)
{
    public async Task<ResultadoSaga> EjecutarAsync(
        CrearPedidoCommand command,
        CancellationToken cancellationToken)
    {
        var pedidoId = await pedidos.CrearAsync(command, cancellationToken);

        try
        {
            await stock.ReservarAsync(command.ProductoId, command.Cantidad, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "No se pudo reservar stock para {PedidoId}", pedidoId);
            await pedidos.CancelarAsync(pedidoId, cancellationToken);
            return ResultadoSaga.Fallido("Sin stock.");
        }

        try
        {
            await pagos.CobrarAsync(pedidoId, command.Monto, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "No se pudo cobrar el pedido {PedidoId}", pedidoId);
            await stock.LiberarAsync(command.ProductoId, command.Cantidad, cancellationToken);
            await pedidos.CancelarAsync(pedidoId, cancellationToken);
            return ResultadoSaga.Fallido("Pago rechazado.");
        }

        await pedidos.ConfirmarAsync(pedidoId, cancellationToken);
        return ResultadoSaga.Exitoso(pedidoId);
    }
}
```

```text
Paso 1: pedido creado en estado pendiente
Paso 2: stock reservado
Paso 3: pago aprobado → pedido confirmado
Fallo en pago    → se libera stock y se cancela pedido
Fallo en stock   → se cancela pedido
```

Cada paso debe ser idempotente y cada compensación debe poder ejecutarse aunque el paso anterior haya fallado a medias. Ese es el costo real de distribuir.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Un servicio por tabla | Se confundió dominio con esquema | Separar por capacidad de negocio |
| Todos comparten la misma base | Atajo operativo | Base o esquema por servicio |
| Llamadas síncronas en cadena larga | Diseño ingenuo | Reducir la cadena o usar mensajería |
| Sin idempotencia en reintentos | Se asumió una sola entrega | Claves de idempotencia |
| Sin trazas ni correlación | Depuración imposible | OpenTelemetry y un id de correlación |
| Despliegue coordinado de todo | Contratos rígidos | Versionado compatible y evolución independiente |
| Equipo chico operando diez servicios | Se subestimó el costo | Volver al monolito modular |

## Buenas prácticas

- Un servicio, una capacidad de negocio y un equipo responsable.
- Contratos versionados y compatibles hacia atrás.
- Idempotencia en toda operación que pueda reintentarse.
- Timeouts, reintentos con backoff y circuit breaker.
- Observabilidad desde el día uno: logs, métricas y trazas.
- Health checks y despliegues automatizados.
- Datos propios: sin tablas compartidas.

## Ejercicio

Para un sistema de e-commerce, decidí qué separarías y qué mantendrías junto. Justificá con necesidades operativas, no con moda. Luego describí una saga para "confirmar pedido" con sus compensaciones.

<details>
<summary>Ver solución</summary>

```text
Mantener junto (monolito modular):
- Catálogo y precios: cambian juntos y no tienen escala extrema.
- Administración y reportes: bajo volumen, misma base.

Separar (servicio):
- Pagos: requisitos de seguridad, auditoría y certificación propios.
- Notificaciones: picos de volumen y fallos que no deben afectar la compra.

Saga "confirmar pedido":
1. Crear pedido (pendiente).
2. Reservar stock → compensación: liberar stock.
3. Cobrar → compensación: reembolsar.
4. Confirmar pedido.
Cada paso con clave de idempotencia y registro de estado.
```

La separación se justifica por seguridad, cumplimiento y perfiles de carga distintos, no por "tener microservicios".

</details>

## Resumen

- Los microservicios compran independencia de despliegue a cambio de complejidad operativa.
- La red introduce latencia, fallos parciales y transacciones distribuidas.
- La idempotencia, la observabilidad y los contratos son requisitos, no extras.
- Un monolito modular bien separado suele ser el punto de partida correcto.
- Se separa cuando hay una necesidad operativa concreta.

## Siguiente paso

Al separar servicios, la comunicación deja de ser una llamada en memoria. En [Comunicación entre servicios](/modulos/arquitectura/comunicacion-servicios) vas a elegir entre síncrono y asíncrono, y manejar sus fallos.
