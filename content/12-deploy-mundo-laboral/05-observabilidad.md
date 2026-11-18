---
slug: observabilidad
title: "Logs, métricas y salud"
order: 5
estimatedMinutes: 15
summary: "Instrumentá una API con logs estructurados, métricas y health checks para detectar problemas antes de que los reporte un usuario."
quiz:
  - question: "¿Qué nivel de log usar para un error que impide completar una operación?"
    options:
      - "Trace"
      - "Debug"
      - "Information"
      - "Error"
    answer: 3
    explanation: "Error registra fallos que afectan una operación concreta. Critical se reserva para fallos que comprometen toda la aplicación."
  - question: "¿Qué ventaja tienen los logs estructurados sobre la concatenación de strings?"
    options:
      - "Ocupan menos espacio siempre"
      - "Permiten filtrar y agrupar por propiedades sin parsear texto"
      - "No necesitan revisión"
      - "Se escriben más rápido"
    answer: 1
    explanation: "Al usar plantillas con parámetros, el logger guarda las propiedades por separado y el sistema de consulta puede filtrar por ellas."
  - question: "¿Qué endpoint de health checks conviene exponer para el balanceador de carga?"
    options:
      - "Uno que valide base de datos y dependencias críticas"
      - "Uno que devuelva siempre 200"
      - "Uno que ejecute migraciones"
      - "Uno que muestre las claves de configuración"
    answer: 0
    explanation: "El readiness debe reflejar si la instancia puede atender tráfico, incluyendo dependencias críticas como la base de datos."
  - question: "¿Para qué sirve el tag en un health check?"
    options:
      - "Para agrupar checks y mapearlos a endpoints distintos"
      - "Para ordenar alfabéticamente los logs"
      - "Para definir el nivel de log"
      - "Para cifrar la respuesta"
    answer: 0
    explanation: "Con tags podés separar, por ejemplo, checks de liveness y de readiness y publicarlos en rutas diferentes."
---

# Logs, métricas y salud

## Objetivo

Entender qué contar, qué registrar y qué exponer para operar una API en producción, y distinguir un problema de proceso de un problema de dependencia.

## Requisitos previos

Necesitás una API desplegada o lista para desplegar, como la de [Deploy en la nube](/modulos/deploy-mundo-laboral/cloud). También ayuda conocer inyección de dependencias y el ciclo de vida de la aplicación.

## Concepto

La observabilidad responde tres preguntas distintas. Los logs cuentan qué pasó en un caso concreto. Las métricas cuentan cuántas veces pasó y con qué latencia. Los traces muestran el recorrido de una petición entre componentes.

En .NET, `ILogger<T>` es la abstracción estándar. No escribas `Console.WriteLine`: el logger respeta niveles, filtros y proveedores. Los niveles van de `Trace` a `Critical`. En producción se suele usar `Information` como base y `Warning` para ruido, dejando `Debug` solo para diagnóstico puntual.

Un log estructurado usa plantillas con parámetros: `logger.LogInformation("Pedido {PedidoId} creado", id)`. Nunca uses interpolación (`$"Pedido {id} creado"`), porque convierte todo en un string opaco y rompe la agrupación.

Las métricas se crean con `System.Diagnostics.Metrics.Meter`. Un contador acumula eventos, un histograma mide distribuciones y un observable reporta un valor en el momento de la consulta. OpenTelemetry exporta esas métricas y los traces a un backend.

Los health checks no son monitoreo: son la respuesta a la pregunta puede esta instancia atender tráfico ahora. Conviene separar liveness (el proceso está vivo) de readiness (puede atender, incluidas sus dependencias). Si readiness falla, el balanceador saca la instancia; si liveness falla, el orquestador la reinicia.

Un error clásico es exponer el detalle de los health checks públicamente. La respuesta detallada puede revelar nombres de servidores o versiones. En producción, el endpoint público devuelve solo el estado y el detalle queda en un puerto o ruta interna.

## Ejemplo mínimo

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddCheck("auto", () => HealthCheckResult.Healthy("API operativa"),
        tags: ["live"]);

var app = builder.Build();

app.MapGet("/pedidos/{id:int}", (int id, ILogger<Program> logger) =>
{
    logger.LogInformation("Consultando pedido {PedidoId}", id);
    return Results.Ok(new { id, estado = "pendiente" });
});

app.MapHealthChecks("/salud/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.Run();
```

```json
{"EventId":0,"LogLevel":"Information","Category":"Program","Message":"Consultando pedido 42","PedidoId":42}
```

```text
GET /salud/live  ->  200 Healthy
```

`PedidoId` aparece como propiedad separada, no dentro del texto. Eso permite consultar todos los logs de un pedido sin expresiones regulares.

## Ejemplo aplicado

Una API con base de datos y un servicio externo separa liveness de readiness, agrega una métrica de negocio y registra un identificador de correlación.

```csharp
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("base-datos", tags: ["ready"])
    .AddCheck("auto", () => HealthCheckResult.Healthy(), tags: ["live"]);

builder.Services.AddSingleton<MetricasPedidos>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    string id = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString("N");

    context.Response.Headers["X-Correlation-Id"] = id;

    using (app.Logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = id
    }))
    {
        await next();
    }
});

app.MapPost("/pedidos", async (
    CrearPedido request,
    AppDbContext db,
    MetricasPedidos metricas,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var pedido = new Pedido(request.Cliente, request.Total);
    db.Pedidos.Add(pedido);
    await db.SaveChangesAsync(ct);

    metricas.PedidosCreados.Add(1, new KeyValuePair<string, object?>("moneda", "ARS"));
    logger.LogInformation("Pedido {PedidoId} creado por {Cliente} por {Total}",
        pedido.Id, pedido.Cliente, pedido.Total);

    return Results.Created($"/pedidos/{pedido.Id}", pedido);
});

app.MapHealthChecks("/salud/live", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("live")
});

app.MapHealthChecks("/salud/ready", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            estado = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                nombre = e.Key,
                estado = e.Value.Status.ToString()
            })
        });
    }
});

app.Run();

public sealed class MetricasPedidos
{
    private readonly Counter<long> _pedidosCreados;

    public MetricasPedidos(IMeterFactory factory)
    {
        Meter meter = factory.Create("Cursos.Api.Pedidos");
        _pedidosCreados = meter.CreateCounter<long>(
            "pedidos.creados", unit: "pedidos",
            description: "Cantidad de pedidos creados");
    }

    public Counter<long> PedidosCreados => _pedidosCreados;
}

public sealed record CrearPedido(string Cliente, decimal Total);
public sealed record Pedido(int Id, string Cliente, decimal Total);
```

```text
GET /salud/live   -> {"estado":"Healthy"}
GET /salud/ready  -> {"estado":"Healthy","checks":[{"nombre":"base-datos","estado":"Healthy"}]}
```

```text
fail: Microsoft.Extensions.Diagnostics.HealthChecks
      base-datos: Unhealthy
GET /salud/ready -> 503
```

El middleware de correlación envuelve cada petición con un scope. Todos los logs de esa petición incluyen `CorrelationId`, aun cuando provengan de clases distintas. Cuando readiness falla, el balanceador deja de enviar tráfico sin reiniciar el proceso: eso lo decide liveness.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Los logs salen como texto plano sin propiedades | Se usó interpolación en la plantilla | Pasar valores como argumentos separados |
| El detalle de health checks es público | Se mapeó sin restringir la respuesta | Devolver solo el estado y proteger el detalle |
| El balanceador reinicia la API por lentitud de la base | Readiness y liveness comparten checks | Separar por tags y depender solo de liveness para reinicios |
| No se puede seguir una petición entre servicios | Falta propagación de correlación | Leer o generar `X-Correlation-Id` y propagarlo |
| Las métricas se pierden al reiniciar | Se guardan en memoria y nadie las exporta | Exportar con OpenTelemetry o el agente del proveedor |
| Los logs incluyen contraseñas o tokens | Se registran objetos completos | Registrar solo identificadores y campos no sensibles |

## Buenas prácticas

- Usá `ILogger<T>` con plantillas y propiedades, nunca interpolación.
- No loguees datos personales, tokens ni contraseñas.
- Diferenciá liveness de readiness y no incluyas dependencias externas en liveness.
- Nombrá las métricas con un prefijo estable, por ejemplo `cursos.pedidos.creados`.
- Correlacioná con un id de petición y devolvelo en la respuesta para soporte.

## Ejercicio

Agregá a la API un health check `cache` que consulte un servicio de caché en memoria y devuelva `Degraded` si tarda más de 200 ms, un contador de peticiones por ruta y un log de advertencia cuando la tasa de errores supere el 5 % en una ventana simple de 20 peticiones. Verificá que `/salud/ready` devuelva 200 con `Degraded` y que el log incluya la ruta.

<details>
<summary>Ver solución</summary>

```csharp
public sealed class MetricasHttp
{
    private readonly Counter<long> _peticiones;
    private readonly Counter<long> _errores;

    public MetricasHttp(IMeterFactory factory)
    {
        Meter meter = factory.Create("Cursos.Api.Http");
        _peticiones = meter.CreateCounter<long>("http.peticiones");
        _errores = meter.CreateCounter<long>("http.errores");
    }

    public void Registrar(string ruta, int status)
    {
        var tag = new KeyValuePair<string, object?>("ruta", ruta);
        _peticiones.Add(1, tag);

        if (status >= 500)
        {
            _errores.Add(1, tag);
        }
    }
}
```

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("cache", () =>
    {
        long inicio = Stopwatch.GetTimestamp();
        _ = cache.TryGetValue("ping", out _);
        TimeSpan duracion = Stopwatch.GetElapsedTime(inicio);

        return duracion > TimeSpan.FromMilliseconds(200)
            ? HealthCheckResult.Degraded($"Caché lenta: {duracion.TotalMilliseconds:F0} ms")
            : HealthCheckResult.Healthy();
    }, tags: ["ready"]);
```

```csharp
app.Use(async (context, next) =>
{
    await next();

    string ruta = context.Request.Path.Value ?? "/";
    metricas.Registrar(ruta, context.Response.StatusCode);

    int total = Interlocked.Increment(ref contadorTotal);
    if (context.Response.StatusCode >= 500)
    {
        Interlocked.Increment(ref contadorErrores);
    }

    if (total % 20 == 0 && contadorErrores * 100.0 / total > 5)
    {
        app.Logger.LogWarning(
            "Tasa de errores alta: {Errores} de {Total} en {Ruta}",
            contadorErrores, total, ruta);
    }
});
```

```text
GET /salud/ready -> 200
{"estado":"Degraded","checks":[{"nombre":"cache","estado":"Degraded"}]}

warn: Program[0]
      Tasa de errores alta: 2 de 20 en /pedidos
```

Checklist:

- Liveness sigue en 200 aunque la caché esté lenta.
- Readiness devuelve `Degraded` pero no 503, porque la API todavía puede atender.
- El contador incluye la ruta como tag y no como parte del nombre.
- El log de advertencia no expone datos del usuario.

</details>

## Resumen

- Logs, métricas y traces responden preguntas diferentes; se complementan.
- `ILogger<T>` con plantillas produce logs estructurados y consultables.
- Liveness indica proceso vivo; readiness indica si puede atender tráfico.
- Las métricas se exportan fuera del proceso para sobrevivir a reinicios.
- La correlación permite seguir una petición a través de varios componentes.

## Siguiente paso

Con la aplicación observable, el trabajo pasa al equipo. Empezá a cuidar el código compartido en [Code review y flujo con Git](/modulos/deploy-mundo-laboral/code-review-git).
