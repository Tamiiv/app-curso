---
slug: middleware
title: "Middleware y pipeline"
order: 4
estimatedMinutes: 15
summary: "Entendé cómo viaja una petición por el pipeline, escribí middleware propio y ubicá cada componente en el orden correcto."
quiz:
  - question: "¿Qué debe hacer un middleware para que la petición llegue al endpoint?"
    options:
      - "Devolver siempre un IResult"
      - "Llamar a next y esperar su tarea"
      - "Registrarse como singleton"
      - "Llamar a app.Run()"
    answer: 1
    explanation: "Cada middleware decide si continúa: si no invoca next, corta el pipeline y el endpoint nunca se ejecuta."
  - question: "¿Dónde conviene registrar UseExceptionHandler?"
    options:
      - "Después de los endpoints"
      - "Al principio del pipeline, antes del resto"
      - "Dentro de cada MapGet"
      - "En builder.Services"
    answer: 1
    explanation: "Debe envolver al resto del pipeline para capturar excepciones de cualquier middleware o endpoint posterior."
  - question: "¿Por qué no se inyecta un servicio scoped en el constructor de un middleware?"
    options:
      - "Porque el middleware se construye una sola vez y capturaría una instancia"
      - "Porque los middleware no admiten DI"
      - "Porque el servicio se vuelve singleton"
      - "Porque el constructor solo acepta RequestDelegate"
    answer: 0
    explanation: "El middleware se instancia una vez al arrancar, así que un scoped en el constructor queda capturado; se inyecta en InvokeAsync."
  - question: "El middleware A llama a next y luego escribe una cabecera. ¿Qué pasa si la respuesta ya empezó?"
    options:
      - "La cabecera se agrega igual"
      - "Se lanza una excepción porque las cabeceras son de solo lectura"
      - "Se descarta la cabecera sin error"
      - "Se reinicia la respuesta"
    answer: 1
    explanation: "Una vez enviadas las cabeceras no se pueden modificar; para agregarlas tarde se usa Response.OnStarting."
---

# Middleware y pipeline

## Objetivo

Comprender el pipeline de ASP.NET Core, escribir middleware propio que inspeccione o corte peticiones y ubicar cada componente en el orden que corresponde.

## Requisitos previos

Necesitás saber cómo se registran servicios y qué es el ciclo de vida scoped, visto en [Inyección de dependencias](/modulos/aspnet-core-apis/inyeccion-dependencias), y tener endpoints funcionando con [Routing y parámetros](/modulos/aspnet-core-apis/routing-parametros).

## Concepto

Una petición atraviesa una cadena de componentes llamados middleware. Cada uno recibe `HttpContext`, puede hacer algo antes de continuar, llamar al siguiente y hacer algo después. La respuesta vuelve por el mismo camino en orden inverso.

El pipeline se arma con el orden en que llamás a `Use...`. No es una lista de eventos: es una caja dentro de otra. Si un middleware no llama a `next`, la petición se corta ahí y ningún endpoint se ejecuta.

| Componente | Para qué sirve |
|---|---|
| `UseExceptionHandler` | Captura excepciones y responde un error controlado |
| `UseHttpsRedirection` | Redirige HTTP a HTTPS |
| `UseStaticFiles` | Sirve archivos de `wwwroot` |
| `UseAuthentication` | Identifica al usuario |
| `UseAuthorization` | Verifica permisos |
| `UseMiddleware<T>` | Agrega una clase de middleware propia |

En Minimal APIs, `MapGet` y compañía agregan el middleware de endpoints al final del pipeline. Por eso todo lo que registres antes corre antes del handler.

Hay dos formas de escribir middleware. La inline con `app.Use(async (context, next) => ...)` sirve para lógica corta. La clase con `RequestDelegate` y `InvokeAsync` conviene cuando necesitás dependencias, pruebas o reutilización.

Un middleware es singleton por naturaleza: se construye una vez. Los servicios scoped se piden en `InvokeAsync`, no en el constructor.

## Ejemplo mínimo

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine($"→ {context.Request.Method} {context.Request.Path}");
    await next();
    Console.WriteLine($"← {context.Response.StatusCode}");
});

app.MapGet("/", () => "Hola desde el pipeline");

app.Run();
```

```bash
curl http://localhost:5137/
```

```text
→ GET /
← 200
Hola desde el pipeline
```

El middleware imprime antes y después del endpoint. Si quitás `await next()`, la consola muestra solo la flecha de entrada y la petición queda sin respuesta.

## Ejemplo aplicado

Este middleware asigna un identificador a cada petición, mide cuánto tarda y registra el resultado con `ILogger`.

```csharp
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<TiempoRespuestaMiddleware>();

app.MapGet("/tareas", () => new[] { "Preparar clase", "Corregir TP" });

app.MapGet("/lento", async () =>
{
    await Task.Delay(150);
    return Results.Ok(new { Mensaje = "Listo" });
});

app.Run();

public sealed class TiempoRespuestaMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TiempoRespuestaMiddleware> _logger;

    public TiempoRespuestaMiddleware(RequestDelegate next, ILogger<TiempoRespuestaMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string id = Guid.NewGuid().ToString("N")[..8];
        context.Response.Headers["X-Request-Id"] = id;

        var cronometro = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            cronometro.Stop();
            _logger.LogInformation(
                "[{Id}] {Metodo} {Ruta} → {Codigo} en {Ms} ms",
                id,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                cronometro.ElapsedMilliseconds);
        }
    }
}
```

```bash
curl -i http://localhost:5137/lento
```

```text
HTTP/1.1 200 OK
X-Request-Id: 4f2a91c7
...

[12:30:00 INF] [4f2a91c7] GET /lento → 200 en 168 ms
```

El `try/finally` garantiza que el tiempo se registre aunque el endpoint falle. La cabecera se escribe antes de llamar a `next`, cuando todavía se puede modificar la respuesta.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El endpoint no se ejecuta y la respuesta queda vacía | Un middleware no llama a `next` | Invocar `await next()` salvo que se quiera cortar a propósito |
| `InvalidOperationException: Headers are read-only` | Se intentó escribir una cabecera después de enviar la respuesta | Escribir antes de `next` o usar `Response.OnStarting` |
| El manejador de errores no captura excepciones | Se registró después del endpoint o de otro middleware que falla | Moverlo al inicio del pipeline |
| Un servicio scoped se reutiliza entre peticiones | Se inyectó en el constructor del middleware | Inyectarlo como parámetro de `InvokeAsync` |
| Los estáticos devuelven 404 | `UseStaticFiles` está después de los endpoints o falta `wwwroot` | Ubicarlo antes de los endpoints |
| Se agregan cabeceras CORS y la petición falla | El middleware de CORS quedó después de los endpoints | Registrarlo antes de `UseAuthentication` y de los endpoints |

## Buenas prácticas

- Ordená el pipeline de lo general a lo específico: errores, HTTPS, estáticos, autenticación, autorización, endpoints.
- Escribí middleware con una sola responsabilidad y nombre explícito.
- No leas ni buffers el cuerpo de la petición sin necesidad; rompe escenarios de streaming.
- Logueá con `ILogger` y datos estructurados, no con concatenación de strings.
- Envolvé las llamadas a `next` en `try/finally` cuando midas o liberes recursos.

## Ejercicio

Creá un middleware que rechace con 401 y un JSON `{"error":"Falta la clave"}` toda petición a rutas que empiecen con `/admin` si no trae la cabecera `X-Api-Key`. Para el resto de las rutas debe continuar normalmente. Probá con y sin cabecera.

<details>
<summary>Ver solución</summary>

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Use(async (context, next) =>
{
    bool esAdmin = context.Request.Path.StartsWithSegments("/admin");
    bool tieneClave = context.Request.Headers.ContainsKey("X-Api-Key");

    if (esAdmin && !tieneClave)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { Error = "Falta la clave" });
        return;
    }

    await next();
});

app.MapGet("/admin/reportes", () => new[] { "Ventas", "Stock" });
app.MapGet("/publico", () => "Sin restricciones");

app.Run();
```

```bash
curl -i http://localhost:5137/admin/reportes
curl http://localhost:5137/admin/reportes -H "X-Api-Key: secreta"
curl http://localhost:5137/publico
```

```text
HTTP/1.1 401 Unauthorized
{"error":"Falta la clave"}
["Ventas","Stock"]
Sin restricciones
```

Cortar el pipeline con `return` sin llamar a `next` es la forma correcta de negar el acceso. La respuesta se escribe con `WriteAsJsonAsync` y el estado se fija antes.

</details>

## Resumen

- El pipeline es una cadena de middleware; cada uno decide si continúa.
- El orden de registro define qué envuelve a qué.
- `app.Use` sirve para lógica inline y `UseMiddleware<T>` para clases reutilizables.
- Los servicios scoped se inyectan en `InvokeAsync`, no en el constructor.
- Las cabeceras solo se pueden modificar antes de enviar la respuesta.

## Siguiente paso

Ya podés interceptar peticiones. En [Validación y errores HTTP](/modulos/aspnet-core-apis/validacion-errores) vas a responder con errores claros y consistentes.
