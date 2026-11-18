---
slug: comunicacion-servicios
title: "Comunicación entre servicios"
order: 6
estimatedMinutes: 15
summary: "Elegí entre HTTP síncrono y mensajería asíncrona, diseñá eventos y manejá timeouts, reintentos y fallos parciales sin duplicar operaciones."
quiz:
  - question: "¿Cuándo conviene comunicación asíncrona por mensajería?"
    options:
      - "Cuando el emisor necesita la respuesta inmediata para continuar"
      - "Cuando el emisor no necesita esperar y se tolera consistencia eventual"
      - "Cuando hay que leer una tabla compartida"
      - "Cuando se quiere simplificar la depuración"
    answer: 1
    explanation: "La mensajería desacopla en el tiempo: el emisor publica y sigue. A cambio, el resultado llega después."
  - question: "¿Qué problema genera un reintento sin idempotencia?"
    options:
      - "Mayor latencia"
      - "Duplicar efectos, como cobrar dos veces o crear dos pedidos"
      - "Perder el log"
      - "Que el servicio no compile"
    answer: 1
    explanation: "Los reintentos son inevitables ante fallos de red. La operación debe poder ejecutarse más de una vez con el mismo resultado."
  - question: "¿Qué hace un circuit breaker?"
    options:
      - "Corta el servicio para siempre"
      - "Deja de llamar temporalmente a una dependencia que falla y permite recuperación"
      - "Borra los logs de error"
      - "Reinicia la base de datos"
    answer: 1
    explanation: "El circuito abierto evita saturar una dependencia caída y da tiempo a que se recupere, con reintentos controlados."
  - question: "¿Qué significa consistencia eventual?"
    options:
      - "Que los datos nunca son consistentes"
      - "Que los cambios se propagan con un retraso acotado, no instantáneo"
      - "Que se pierden escrituras"
      - "Que todo es una transacción ACID"
    answer: 1
    explanation: "El sistema converge a un estado consistente después de un tiempo, pero una lectura inmediata puede ver el estado anterior."
---

# Comunicación entre servicios

## Objetivo

Elegir el estilo de comunicación adecuado entre servicios, diseñar eventos y comandos claros y proteger las llamadas con timeouts, reintentos, idempotencia y circuit breaker.

## Requisitos previos

Haber leído [Microservicios](/modulos/arquitectura/microservicios). Necesitás manejar `HttpClient`, JSON y el concepto de cola de mensajes.

## Concepto

Dos estilos de comunicación, con problemas distintos:

| Estilo | Cuándo | Ventaja | Costo |
|---|---|---|---|
| Síncrono (HTTP) | Se necesita la respuesta para continuar | Simple, inmediato | Acopla disponibilidad y latencia |
| Asíncrono (mensajería) | El resultado puede llegar después | Desacopla y absorbe picos | Consistencia eventual y complejidad |

Síncrono es la opción natural para una consulta: "¿hay stock?". Asíncrono es la opción natural para un efecto: "el pedido fue creado, notificá". En una misma arquitectura conviven ambos.

Un problema central de las llamadas síncronas es la **cadena de fallos**: A llama a B, B a C y C a D. Si D tarda, la latencia se suma y la probabilidad de fallo se multiplica. Cuanto más larga la cadena, más frágil el flujo.

Las defensas mínimas de una llamada HTTP:

- **Timeout:** nunca esperar indefinidamente. Un valor corto y explícito.
- **Reintento con backoff exponencial** solo para errores transitorios, no para `400` ni `404`.
- **Circuit breaker:** si la dependencia falla repetidamente, dejar de llamarla por un tiempo.
- **Idempotencia:** la operación puede repetirse sin duplicar efectos.
- **Degradación:** si el servicio de recomendaciones falla, mostrar la página sin recomendaciones en lugar de un error total.

En mensajería, los patrones clave:

- **Comando:** "reservá stock" con un destinatario claro.
- **Evento:** "pedido creado" sin destinatario; varios pueden escuchar.
- **Cola de mensajes muertos:** los que fallan repetidamente se apartan para revisión.
- **Entrega al menos una vez:** es la garantía habitual, por eso la idempotencia es obligatoria.

## Ejemplo mínimo

Cliente HTTP con timeout, reintentos y circuit breaker usando Polly:

```csharp
using System.Net.Http.Json;

using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpClient<IStockClient, StockClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Servicios:Stock"]!);
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    });

var app = builder.Build();

app.MapGet("/disponibilidad/{productoId:guid}", async (
    Guid productoId,
    IStockClient stock,
    CancellationToken cancellationToken) =>
{
    try
    {
        var disponible = await stock.HayDisponibleAsync(productoId, 1, cancellationToken);
        return TypedResults.Ok(new { disponible });
    }
    catch (HttpRequestException)
    {
        // Degradación: se responde con información parcial, no con un 500.
        return TypedResults.Ok(new { disponible = (bool?)null, aviso = "Stock no disponible." });
    }
});

app.Run();
```

```csharp
public interface IStockClient
{
    Task<bool> HayDisponibleAsync(Guid productoId, int cantidad, CancellationToken cancellationToken);
}

public sealed class StockClient(HttpClient http) : IStockClient
{
    public async Task<bool> HayDisponibleAsync(
        Guid productoId,
        int cantidad,
        CancellationToken cancellationToken)
    {
        var resultado = await http.GetFromJsonAsync<DisponibilidadResponse>(
            $"/stock/{productoId}?cantidad={cantidad}",
            cancellationToken);

        return resultado?.Disponible ?? false;
    }
}

public sealed record DisponibilidadResponse(bool Disponible);
```

```text
Servicio de stock OK        → 200 {"disponible":true}
Servicio caído o lento      → 200 {"disponible":null,"aviso":"..."}
```

La degradación explícita es una decisión de producto: preferir una respuesta parcial antes que una página de error.

## Ejemplo aplicado

Publicar un evento y consumirlo de forma idempotente:

```csharp
public sealed record PedidoCreado(Guid PedidoId, Guid ProductoId, int Cantidad, DateTimeOffset OcurridoEn);

public interface IEventBus
{
    Task PublicarAsync<T>(T evento, CancellationToken cancellationToken) where T : class;
}

public sealed class CrearPedidoHandler(IPedidoRepository pedidos, IEventBus bus)
{
    public async Task<Guid> HandleAsync(CrearPedidoCommand command, CancellationToken cancellationToken)
    {
        var pedido = new Pedido(Guid.NewGuid(), command.ProductoId, command.Cantidad);
        await pedidos.AgregarAsync(pedido, cancellationToken);

        await bus.PublicarAsync(
            new PedidoCreado(pedido.Id, pedido.ProductoId, pedido.Cantidad, DateTimeOffset.UtcNow),
            cancellationToken);

        return pedido.Id;
    }
}
```

```csharp
// Consumidor: procesa al menos una vez, así que debe ser idempotente.
public sealed class ReservarStockConsumer(StockDbContext db, ILogger<ReservarStockConsumer> logger)
{
    public async Task HandleAsync(PedidoCreado evento, CancellationToken cancellationToken)
    {
        var yaProcesado = await db.EventosProcesados
            .AnyAsync(e => e.EventoId == evento.PedidoId, cancellationToken);

        if (yaProcesado)
        {
            logger.LogInformation("Evento {PedidoId} ya procesado", evento.PedidoId);
            return;
        }

        var producto = await db.Productos.FindAsync([evento.ProductoId], cancellationToken);
        if (producto is null)
        {
            logger.LogWarning("Producto {ProductoId} inexistente", evento.ProductoId);
            return;
        }

        producto.Reservar(evento.Cantidad);
        db.EventosProcesados.Add(new EventoProcesado(evento.PedidoId, DateTimeOffset.UtcNow));

        await db.SaveChangesAsync(cancellationToken);
    }
}
```

```text
Primer mensaje  → reserva stock y registra el evento
Mensaje repetido → detecta el id y no vuelve a reservar
```

La tabla de eventos procesados convierte "al menos una vez" en un efecto único.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Reintentar un `400` | Se reintenta cualquier error | Reintentar solo errores transitorios y de red |
| Sin timeout | El cliente espera para siempre | Timeout corto y explícito |
| Duplicados tras un reintento | Falta idempotencia | Claves de idempotencia o registro de eventos |
| Cadena síncrona de cinco servicios | Diseño ingenuo | Reducir cadena o pasar a mensajería |
| Cola sin dead letter | Mensajes venenosos | Dead letter queue y alertas |
| Evento con demasiados datos internos | Se acopló el contrato al modelo | Eventos con datos mínimos y versionados |
| Un fallo de notificación tumba la compra | Sin degradación | Hacer la notificación asíncrona o tolerar su fallo |

## Buenas prácticas

- Preferí asíncrono para efectos, síncrono para consultas que necesitan respuesta.
- Timeouts y reintentos con backoff en toda llamada de red.
- Idempotencia en consumidores y en endpoints que puedan reintentarse.
- Degradación definida por producto, no improvisada en el código.
- Contratos de eventos versionados y con datos mínimos.
- Dead letter queue y monitoreo de mensajes fallidos.

## Ejercicio

Implementá un consumidor de `PedidoCreado` que envíe un email y sea idempotente. Si el envío falla, el mensaje debe reintentarse y, tras cinco intentos, ir a una cola de mensajes muertos.

<details>
<summary>Ver solución</summary>

```csharp
public sealed class EnviarEmailConsumer(
    EmailDbContext db,
    IEmailService email,
    ILogger<EnviarEmailConsumer> logger)
{
    private const int MaxIntentos = 5;

    public async Task HandleAsync(PedidoCreado evento, int intento, CancellationToken cancellationToken)
    {
        var envio = await db.Envios.FirstOrDefaultAsync(e => e.PedidoId == evento.PedidoId, cancellationToken);

        if (envio?.Estado == EstadoEnvio.Enviado)
        {
            logger.LogInformation("Email de {PedidoId} ya enviado", evento.PedidoId);
            return;
        }

        envio ??= new EnvioEmail(evento.PedidoId);
        db.Envios.Add(envio);

        try
        {
            await email.EnviarAsync(evento.PedidoId, cancellationToken);
            envio.MarcarEnviado();
        }
        catch (Exception ex) when (intento < MaxIntentos)
        {
            logger.LogWarning(ex, "Fallo el envío, intento {Intento}", intento);
            throw; // La cola reintenta.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Se agotan los intentos para {PedidoId}", evento.PedidoId);
            envio.MarcarFallido();
            // La infraestructura mueve el mensaje a la dead letter queue.
        }
        finally
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
```

```text
Intento 1-4 fallidos → reintento con backoff
Intento 5 fallido    → estado Fallido y dead letter
Reentrega del mismo evento → detecta Enviado y no repite
```

El estado persistido es lo que hace idempotente al consumidor, incluso si el mensaje se entrega varias veces.

</details>

## Resumen

- Síncrono para consultas inmediatas; asíncrono para efectos desacoplados.
- Toda llamada de red necesita timeout, reintento selectivo y circuit breaker.
- Los reintentos exigen idempotencia.
- La mensajería suele garantizar entrega al menos una vez.
- La degradación explícita protege la experiencia del usuario.

## Siguiente paso

Con los servicios comunicados, aparece la pregunta sobre cómo leer y escribir datos sin mezclar modelos. En [CQRS](/modulos/arquitectura/cqrs) vas a separar comandos y consultas con criterio.
