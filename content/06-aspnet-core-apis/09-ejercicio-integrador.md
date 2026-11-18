---
slug: ejercicio-integrador
title: "Ejercicio integrador: API de tareas"
order: 9
estimatedMinutes: 25
summary: "Construí una API de tareas completa con Minimal APIs, servicios, validación, códigos de estado, OpenAPI y pruebas manuales con curl."
quiz:
  - question: "¿Qué capa debe decidir si una tarea puede completarse?"
    options:
      - "El handler del endpoint, con un if"
      - "El servicio de dominio, porque la regla no depende del protocolo HTTP"
      - "El cliente, antes de enviar la petición"
      - "OpenAPI, al documentar el endpoint"
    answer: 1
    explanation: "Las reglas de negocio viven en el dominio o en un servicio. El handler traduce HTTP hacia ese servicio y viceversa."
  - question: "¿Dónde se registra un servicio en memoria para que lo use un Minimal API?"
    options:
      - "En el handler, con new"
      - "En builder.Services, con su ciclo de vida"
      - "En app.MapGet"
      - "En el archivo .csproj"
    answer: 1
    explanation: "Los servicios se registran en el contenedor de dependencias y se inyectan como parámetros del handler."
  - question: "¿Qué devuelve un endpoint cuando la tarea existe pero ya estaba completada y la operación es idempotente?"
    options:
      - "500 siempre"
      - "El estado coherente con el contrato: por ejemplo 200 con el recurso o 204 si no hay cuerpo"
      - "404 porque no hubo cambio"
      - "302 a otra ruta"
    answer: 1
    explanation: "Completar dos veces no es un error. Se responde con el estado que represente el resultado real de forma consistente."
  - question: "¿Qué verificación demuestra que la API respeta su contrato?"
    options:
      - "Que compile"
      - "Probar cada código de estado esperado con peticiones reales o tests de integración"
      - "Que el JSON sea grande"
      - "Que OpenAPI exista"
    answer: 1
    explanation: "El contrato se demuestra ejecutando los caminos: éxito, validación, no encontrado y conflicto."
---

# Ejercicio integrador: API de tareas

## Objetivo

Construir una API de tareas completa con Minimal APIs, un servicio en memoria con reglas de negocio, validación, códigos de estado, organización por módulos y documentación OpenAPI.

## Requisitos previos

Haber recorrido el módulo: [Minimal APIs](/modulos/aspnet-core-apis/minimal-apis), [Routing](/modulos/aspnet-core-apis/routing-parametros), [Inyección de dependencias](/modulos/aspnet-core-apis/inyeccion-dependencias), [Middleware](/modulos/aspnet-core-apis/middleware), [Validación](/modulos/aspnet-core-apis/validacion-errores), [Códigos de estado](/modulos/aspnet-core-apis/codigos-estado) y [OpenAPI](/modulos/aspnet-core-apis/openapi).

## Concepto

El integrador une las piezas en una API chica pero realista. Las reglas del ejercicio:

- Una tarea tiene `Id`, `Titulo`, `Completada` y `Prioridad` (1 a 5).
- Se pueden listar, crear, completar y eliminar tareas.
- No se puede completar una tarea inexistente.
- El título es obligatorio y tiene entre 3 y 120 caracteres.
- La prioridad es obligatoria y va de 1 a 5.
- Completar una tarea ya completada no es un error: la operación es idempotente.
- La API se documenta con OpenAPI y expone `/health` para saber si está viva.

La arquitectura mínima separa tres responsabilidades:

| Responsabilidad | Dónde |
|---|---|
| Traducir HTTP | Handlers de endpoints |
| Reglas y almacenamiento | `TareaService` |
| Contrato de datos | Records de entrada y salida |

El servicio no conoce `HttpContext`, ni códigos HTTP, ni JSON. Solo devuelve resultados de dominio. Esa separación permite probarlo sin levantar el servidor y reutilizarlo desde otra interfaz.

## Ejemplo mínimo

Empezá por el servicio y su modelo:

```csharp
public sealed record Tarea(Guid Id, string Titulo, bool Completada, int Prioridad);

public sealed record TareaInput(string Titulo, int Prioridad);

public enum ResultadoTarea
{
    Ok,
    NoEncontrada,
    Inválida
}

public sealed class TareaService
{
    private readonly List<Tarea> _tareas = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<Tarea> Listar()
    {
        lock (_lock)
        {
            return _tareas.ToArray();
        }
    }

    public (ResultadoTarea Resultado, Tarea? Tarea) Crear(TareaInput input)
    {
        lock (_lock)
        {
            var tarea = new Tarea(Guid.NewGuid(), input.Titulo.Trim(), false, input.Prioridad);
            _tareas.Add(tarea);
            return (ResultadoTarea.Ok, tarea);
        }
    }

    public Tarea? Completar(Guid id)
    {
        lock (_lock)
        {
            var indice = _tareas.FindIndex(t => t.Id == id);
            if (indice < 0)
            {
                return null;
            }

            if (!_tareas[indice].Completada)
            {
                _tareas[indice] = _tareas[indice] with { Completada = true };
            }

            return _tareas[indice];
        }
    }

    public bool Eliminar(Guid id)
    {
        lock (_lock)
        {
            return _tareas.RemoveAll(t => t.Id == id) > 0;
        }
    }
}
```

`Lock` es el tipo de sincronización de .NET 9 y posteriores. En una API real el estado viviría en una base de datos y la concurrencia se resolvería allí; acá alcanza para que el ejemplo sea seguro entre peticiones.

## Ejemplo aplicado

La API completa, con validación, DI, resultados tipados y OpenAPI:

```csharp
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TareaService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => TypedResults.Ok(new { estado = "ok" }));

app.MapTareas();

app.Run();

public static class TareasEndpoints
{
    public static IEndpointRouteBuilder MapTareas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/tareas").WithTags("Tareas");

        grupo.MapGet("/", (TareaService service) =>
            TypedResults.Ok(service.Listar()))
            .WithName("ListarTareas");

        grupo.MapPost("/", (TareaInput input, TareaService service) =>
        {
            var errores = Validar(input);
            if (errores.Count > 0)
            {
                return TypedResults.ValidationProblem(errores);
            }

            var (_, tarea) = service.Crear(input);
            return TypedResults.Created($"/tareas/{tarea!.Id}", tarea);
        })
        .WithName("CrearTarea")
        .ProducesValidationProblem();

        grupo.MapPost("/{id:guid}/completar", (Guid id, TareaService service) =>
            service.Completar(id) is { } tarea
                ? TypedResults.Ok(tarea)
                : TypedResults.NotFound())
            .WithName("CompletarTarea");

        grupo.MapDelete("/{id:guid}", (Guid id, TareaService service) =>
            service.Eliminar(id)
                ? TypedResults.NoContent()
                : TypedResults.NotFound())
            .WithName("EliminarTarea");

        return app;
    }

    private static Dictionary<string, string[]> Validar(TareaInput input)
    {
        var errores = new Dictionary<string, string[]>();
        var resultados = new List<ValidationResult>();
        var contexto = new ValidationContext(input);

        if (Validator.TryValidateObject(input, contexto, resultados, validateAllProperties: true))
        {
            return errores;
        }

        foreach (var grupo in resultados.GroupBy(r => r.MemberNames.FirstOrDefault() ?? "General"))
        {
            errores[grupo.Key] = grupo.Select(r => r.ErrorMessage ?? "Valor inválido.").ToArray();
        }

        return errores;
    }
}

public sealed record TareaInput(
    [property: Required, property: StringLength(120, MinimumLength = 3)]
    string Titulo,
    [property: Range(1, 5)]
    int Prioridad);
```

Pruebas manuales con `curl`:

```bash
curl -i -X POST http://localhost:5137/tareas \
  -H "Content-Type: application/json" \
  -d '{"titulo":"Terminar el módulo 6","prioridad":2}'

curl -i http://localhost:5137/tareas
curl -i -X POST http://localhost:5137/tareas/{id}/completar
curl -i -X POST http://localhost:5137/tareas/{id}/completar
curl -i -X DELETE http://localhost:5137/tareas/{id}
```

Resultados esperados:

```text
POST /tareas válido               → 201 Created + Location
POST /tareas con titulo "ab"      → 400 ValidationProblem
POST /tareas/{id}/completar       → 200 con Completada = true
POST /tareas/{id}/completar otra vez → 200 con el mismo estado
DELETE /tareas/{id}               → 204 No Content
DELETE /tareas/{id} otra vez      → 404 Not Found
```

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El servicio guarda estado por petición | Se registró como `Scoped` para datos que deben durar | Usar `Singleton` en memoria o persistir en base de datos |
| Dos peticiones simultáneas corrompen la lista | No hay sincronización | Proteger el estado compartido o usar una base de datos |
| El handler valida reglas de negocio | La lógica se mezcló con HTTP | Mover reglas al servicio y dejar al handler traducir resultados |
| Completar dos veces devuelve 500 | Se lanza una excepción al repetir la operación | Hacer la operación idempotente y devolver el estado actual |
| OpenAPI no muestra los errores | Faltan `ProducesValidationProblem` o tipos de retorno | Completar metadatos y usar `TypedResults` |
| El `Location` apunta a una ruta inexistente | Se inventó la URL al crear | Usar la ruta real del `GET` por id |

## Buenas prácticas

- Mantené el servicio libre de tipos HTTP y de JSON.
- Registrá el ciclo de vida correcto: `Singleton` para estado compartido en memoria, `Scoped` para dependencias por petición.
- Validá en el borde y dejá al servicio solo las reglas que dependen del estado.
- Probá todos los caminos, no solo el feliz.
- Documentá cada estado posible en OpenAPI.

## Ejercicio

Ampliá la API con:

1. `GET /tareas/{id}` que devuelva 200 o 404.
2. `PUT /tareas/{id}` para cambiar título y prioridad, con validación.
3. Un filtro que devuelva `400` si el `Content-Type` no es `application/json` en los `POST` y `PUT`.
4. Un endpoint `/tareas/resumen` que devuelva el total y cuántas están completadas.

<details>
<summary>Ver solución</summary>

```csharp
grupo.MapGet("/{id:guid}", (Guid id, TareaService service) =>
    service.Listar().FirstOrDefault(t => t.Id == id) is { } tarea
        ? TypedResults.Ok(tarea)
        : TypedResults.NotFound());

grupo.MapPut("/{id:guid}", (Guid id, TareaInput input, TareaService service) =>
{
    var errores = Validar(input);
    if (errores.Count > 0)
    {
        return TypedResults.ValidationProblem(errores);
    }

    return service.Actualizar(id, input) is { } tarea
        ? TypedResults.Ok(tarea)
        : TypedResults.NotFound();
});

grupo.MapGet("/resumen", (TareaService service) =>
{
    var tareas = service.Listar();
    return TypedResults.Ok(new
    {
        total = tareas.Count,
        completadas = tareas.Count(t => t.Completada),
        pendientes = tareas.Count(t => !t.Completada)
    });
});
```

Y en el servicio:

```csharp
public Tarea? Actualizar(Guid id, TareaInput input)
{
    lock (_lock)
    {
        var indice = _tareas.FindIndex(t => t.Id == id);
        if (indice < 0)
        {
            return null;
        }

        var actualizada = _tareas[indice] with
        {
            Titulo = input.Titulo.Trim(),
            Prioridad = input.Prioridad
        };

        _tareas[indice] = actualizada;
        return actualizada;
    }
}
```

```text
GET  /tareas/{id}        → 200 o 404
PUT  /tareas/{id}        → 200, 400 o 404
GET  /tareas/resumen     → 200 {"total":3,"completadas":1,"pendientes":2}
```

</details>

## Resumen

- El handler traduce HTTP; el servicio concentra reglas y estado.
- La validación en el borde devuelve `ProblemDetails` antes de ejecutar lógica.
- Los códigos de estado cuentan la historia: 201, 400, 404, 204.
- OpenAPI y las pruebas manuales verifican que el contrato se cumpla.

## Siguiente paso

La API ya funciona, pero guarda todo en memoria y se pierde al reiniciar. En el módulo de [EF Core y bases de datos](/modulos/ef-core) vas a persistir estos datos con un `DbContext`, relaciones y migraciones.
