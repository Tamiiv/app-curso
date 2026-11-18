---
slug: versionado-organizacion
title: "Versionado y organización de endpoints"
order: 8
estimatedMinutes: 15
summary: "Dividí una API que crece en módulos con extensiones, elegí una estrategia de versionado y evitás romper a los clientes existentes."
quiz:
  - question: "¿Qué problema resuelve el versionado de una API?"
    options:
      - "Mejorar la velocidad de respuesta"
      - "Permitir cambios incompatibles sin romper a los clientes actuales"
      - "Reducir el tamaño del JSON"
      - "Evitar el uso de HTTPS"
    answer: 1
    explanation: "Una nueva versión convive con la anterior mientras los clientes migran, en lugar de romperlos de golpe."
  - question: "¿Qué cambio es incompatible y suele requerir una nueva versión?"
    options:
      - "Agregar un campo opcional a la respuesta"
      - "Renombrar o eliminar un campo existente"
      - "Agregar un endpoint nuevo"
      - "Mejorar un mensaje de error"
    answer: 1
    explanation: "Renombrar o quitar campos rompe contratos. Agregar campos opcionales o endpoints nuevos suele ser compatible."
  - question: "¿Para qué sirven las extensiones de `IEndpointRouteBuilder` en una API grande?"
    options:
      - "Para comprimir respuestas"
      - "Para agrupar endpoints por módulo y mantener Program.cs legible"
      - "Para reemplazar la inyección de dependencias"
      - "Para generar migraciones"
    answer: 1
    explanation: "Cada módulo expone un método MapX que registra sus rutas, y Program.cs queda como una lista corta de módulos."
  - question: "¿Cuál es la estrategia de versionado más visible para un cliente?"
    options:
      - "Cabecera Accept personalizada"
      - "Versión en la ruta, por ejemplo /v2/tareas"
      - "Query string opcional"
      - "Cambiar el puerto"
    answer: 1
    explanation: "La versión en la ruta es explícita y fácil de probar, aunque alarga la URL. Las cabeceras son más limpias pero menos visibles."
---

# Versionado y organización de endpoints

## Objetivo

Organizar una API en módulos con métodos de extensión, decidir cuándo un cambio requiere una nueva versión y aplicar una estrategia de versionado coherente.

## Requisitos previos

Haber leído [OpenAPI y documentación](/modulos/aspnet-core-apis/openapi). Necesitás manejar Minimal APIs, `IEndpointRouteBuilder` y grupos de rutas.

## Concepto

Al principio todo vive en `Program.cs` y funciona. Con veinte endpoints empieza el problema: el archivo crece, los nombres chocan y cada cambio obliga a recorrer cientos de líneas.

La solución no es un patrón mágico: es separar por módulo funcional. Cada módulo expone una extensión:

```csharp
public static class TareasEndpoints
{
    public static IEndpointRouteBuilder MapTareas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/tareas").WithTags("Tareas");
        grupo.MapGet("/", Listar);
        grupo.MapPost("/", Crear);
        return app;
    }
}
```

`Program.cs` queda como un índice:

```csharp
app.MapTareas();
app.MapClientes();
app.MapSalud();
```

Dentro del módulo se aplican las mismas reglas: un grupo, una ruta base y handlers con nombres claros. Si un módulo necesita dependencias propias, se registran en su propia extensión de servicios.

El versionado es una decisión de contrato. La pregunta no es "¿cuándo subo el número?" sino **¿esto rompe a un cliente existente?**

| Cambio | ¿Compatible? |
|---|---|
| Agregar un campo opcional a la respuesta | Sí |
| Agregar un endpoint | Sí |
| Agregar un parámetro opcional | Sí |
| Hacer obligatorio un campo que era opcional | No |
| Renombrar un campo | No |
| Cambiar el tipo de un campo | No |
| Eliminar un endpoint | No |
| Cambiar el significado de un código de estado | No |

Hay tres estrategias comunes de versionado:

- **Ruta:** `/v1/tareas`. Explícita, fácil de cachear y de probar. Es la más usada en APIs públicas simples.
- **Cabecera:** `Accept: application/vnd.ruta.v2+json`. Mantiene las URLs limpias, pero es menos visible y complica el cacheo.
- **Query string:** `?api-version=2`. Sencilla de probar, pero se mezcla con otros parámetros.

Sin importar la elegida, la regla es la misma: una versión publicada no cambia de forma incompatible. Se agrega una nueva y se mantiene la anterior durante una ventana de migración.

## Ejemplo mínimo

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapTareas();

app.Run();

public static class TareasEndpoints
{
    public static IEndpointRouteBuilder MapTareas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/tareas").WithTags("Tareas");

        grupo.MapGet("/", () => TypedResults.Ok(Tareas));
        grupo.MapGet("/{id:guid}", (Guid id) =>
            Tareas.FirstOrDefault(t => t.Id == id) is { } tarea
                ? TypedResults.Ok(tarea)
                : TypedResults.NotFound());

        return app;
    }

    private static readonly List<Tarea> Tareas =
    [
        new(Guid.NewGuid(), "Organizar endpoints", true)
    ];
}

public sealed record Tarea(Guid Id, string Titulo, bool Completada);
```

```text
GET /tareas      → 200 con la lista
GET /tareas/{id} → 200 con la tarea o 404
```

`MapGroup` define la ruta base y los metadatos comunes. Los endpoints del grupo no repiten `/tareas` y heredan el tag para OpenAPI.

## Ejemplo aplicado

Una API con dos versiones convive mientras los clientes migran:

```csharp
var app = builder.Build();

app.MapGroup("/v1").MapTareasV1();
app.MapGroup("/v2").MapTareasV2();

app.Run();

public static class TareasV1
{
    public static IEndpointRouteBuilder MapTareasV1(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tareas", () => TypedResults.Ok(new[]
        {
            new { id = 1, titulo = "Versión 1" }
        }));

        return app;
    }
}

public static class TareasV2
{
    public static IEndpointRouteBuilder MapTareasV2(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tareas", () => TypedResults.Ok(new[]
        {
            new { id = 1, titulo = "Versión 2", completada = false, prioridad = 3 }
        }));

        return app;
    }
}
```

```bash
curl http://localhost:5137/v1/tareas
curl http://localhost:5137/v2/tareas
```

```text
[{"id":1,"titulo":"Versión 1"}]
[{"id":1,"titulo":"Versión 2","completada":false,"prioridad":3}]
```

La v1 sigue respondiendo con su contrato original. Cuando ya nadie la usa, se anuncia su retiro con una fecha y se elimina. Mientras tanto, la lógica compartida se extrae a servicios para no duplicar reglas.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `Program.cs` vuelve a crecer | Los módulos se crean pero se registran con lambdas gigantes | Mover la lógica a clases de endpoints y dejar una línea por módulo |
| Dos versiones duplican toda la lógica | Se copia el módulo completo | Extraer servicios compartidos y dejar en cada versión solo el contrato |
| El versionado se define al final | Se agregaron cambios incompatibles sin plan | Definir la estrategia antes del primer cliente externo |
| Los clientes no saben qué versión usar | No hay documentación ni cabecera de aviso | Documentar versiones y anunciar deprecaciones con fecha |
| Un cambio "menor" rompe clientes | Se cambió un tipo o se hizo obligatorio un campo | Revisar la tabla de compatibilidad antes de publicar |

## Buenas prácticas

- Un módulo por área funcional, con su extensión `MapX` y sus servicios.
- Versioná solo cuando el cambio es incompatible; no por cada release.
- Anunciá deprecaciones con fecha y mantén la versión anterior el tiempo suficiente.
- Compartí la lógica entre versiones con servicios, no con copias.
- Documentá en OpenAPI qué versión describe cada documento.

## Ejercicio

Separá una API de tareas y clientes en dos módulos con extensiones `MapTareas` y `MapClientes`, cada uno con su ruta base y su tag. Luego agregá `/v2/tareas` que devuelva un campo extra `prioridad` sin tocar la v1.

<details>
<summary>Ver solución</summary>

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGroup("/v1").MapTareasV1().MapClientes();
app.MapGroup("/v2").MapTareasV2();

app.Run();

public static class TareasV1
{
    public static IEndpointRouteBuilder MapTareasV1(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tareas", () => TypedResults.Ok(new[]
        {
            new { id = 1, titulo = "Estudiar", completada = false }
        }))
        .WithTags("Tareas v1");

        return app;
    }
}

public static class TareasV2
{
    public static IEndpointRouteBuilder MapTareasV2(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tareas", () => TypedResults.Ok(new[]
        {
            new { id = 1, titulo = "Estudiar", completada = false, prioridad = 3 }
        }))
        .WithTags("Tareas v2");

        return app;
    }
}

public static class ClientesEndpoints
{
    public static IEndpointRouteBuilder MapClientes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/clientes", () => TypedResults.Ok(new[]
        {
            new { id = 1, nombre = "Ana" }
        }))
        .WithTags("Clientes");

        return app;
    }
}
```

`/v1/tareas` mantiene su forma, `/v2/tareas` agrega `prioridad` y `/v1/clientes` sigue funcionando. La v1 se retira cuando los clientes migran.

</details>

## Resumen

- Un módulo por área funcional mantiene `Program.cs` legible.
- Las extensiones `MapX` agrupan rutas, tags y configuración.
- El versionado protege a los clientes de cambios incompatibles.
- La estrategia se elige antes del primer cliente externo y se documenta.

## Siguiente paso

Con la API organizada, es momento de integrar todo en el [ejercicio integrador](/modulos/aspnet-core-apis/ejercicio-integrador): una API de tareas completa con validación, estados y documentación.
