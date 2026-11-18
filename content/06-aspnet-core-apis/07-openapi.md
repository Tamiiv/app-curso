---
slug: openapi
title: "OpenAPI y documentación"
order: 7
estimatedMinutes: 12
summary: "Generá una especificación OpenAPI de tu API, publicá una interfaz navegable y mantené la documentación pegada al código."
quiz:
  - question: "¿Qué es OpenAPI?"
    options:
      - "Un lenguaje de programación para APIs"
      - "Una especificación que describe endpoints, parámetros y respuestas de una API HTTP"
      - "Una base de datos de documentación"
      - "Un protocolo binario alternativo a HTTP"
    answer: 1
    explanation: "OpenAPI es una especificación en JSON o YAML que describe la API de forma legible por humanos y máquinas."
  - question: "¿Qué valor aporta publicar la especificación OpenAPI?"
    options:
      - "Reemplaza a los tests"
      - "Permite generar clientes, documentación interactiva y contratos verificables"
      - "Mejora el rendimiento del servidor"
      - "Oculta los endpoints internos"
    answer: 1
    explanation: "Con la especificación se pueden generar clientes, docs interactivas y validar que el contrato no se rompa."
  - question: "En .NET 10, ¿qué paquete agrega la generación de documentos OpenAPI a Minimal APIs?"
    options:
      - "Microsoft.AspNetCore.OpenApi"
      - "Swashbuckle.AspNetCore"
      - "Microsoft.OpenApi.Readers"
      - "System.Text.Json"
    answer: 0
    explanation: "Microsoft.AspNetCore.OpenApi ofrece AddOpenApi y MapOpenApi para describir la API dentro del propio framework."
  - question: "¿Cómo se mantiene la documentación confiable?"
    options:
      - "Escribiéndola a mano en un wiki aparte"
      - "Generándola desde el código y revisando los cambios en el contrato"
      - "Documentando solo los endpoints nuevos"
      - "Ocultando los errores en la especificación"
    answer: 1
    explanation: "La documentación generada desde el código se actualiza con cada cambio. Si además se revisa el diff del contrato, no queda desactualizada."
---

# OpenAPI y documentación

## Objetivo

Agregar documentación OpenAPI a una API de Minimal APIs, publicar una interfaz para probarla y usar el contrato como parte del código.

## Requisitos previos

Haber leído [Códigos de estado y resultados](/modulos/aspnet-core-apis/codigos-estado). Necesitás una API con endpoints y `TypedResults`.

## Concepto

Una API sin documentación obliga a cada cliente a leer el código o a preguntarle a alguien. OpenAPI resuelve eso con una especificación que describe:

- Las rutas y los métodos HTTP.
- Los parámetros de ruta, query y cuerpo.
- Los esquemas de los datos.
- Los códigos de respuesta posibles.
- Los requisitos de seguridad.

Esa especificación se puede consumir de varias formas: una página interactiva para probar endpoints, un generador de clientes tipados, un validador de contrato en CI o una colección de pruebas.

En .NET 10 el soporte viene en el framework con `Microsoft.AspNetCore.OpenApi`:

```csharp
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

app.Run();
```

La especificación queda en `/openapi/v1.json`. Con `AddOpenApi` y `MapOpenApi` no hacen falta paquetes de terceros para lo básico. Para una interfaz visual se puede sumar Scalar o Swagger UI, pero la fuente de verdad sigue siendo el JSON generado.

La documentación se mantiene honesta cuando se genera desde el código. Si un endpoint devuelve `201`, `400` y `409`, esos estados deben aparecer en la especificación. Eso se logra con `TypedResults` y, cuando hace falta, con metadatos explícitos.

## Ejemplo mínimo

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var tareas = new List<Tarea>
{
    new(Guid.NewGuid(), "Leer sobre OpenAPI", true)
};

app.MapGet("/tareas", () => TypedResults.Ok(tareas))
   .WithName("ListarTareas")
   .WithSummary("Devuelve todas las tareas")
   .WithTags("Tareas");

app.MapGet("/tareas/{id:guid}", (Guid id) =>
    tareas.FirstOrDefault(t => t.Id == id) is { } tarea
        ? TypedResults.Ok(tarea)
        : TypedResults.NotFound())
   .WithName("ObtenerTarea")
   .WithTags("Tareas");

app.Run();

public sealed record Tarea(Guid Id, string Titulo, bool Completada);
```

```bash
curl -s http://localhost:5137/openapi/v1.json | jq '.paths | keys'
```

```text
[
  "/tareas",
  "/tareas/{id}"
]
```

`WithName` da un nombre estable para generar clientes. `WithSummary` y `WithTags` mejoran la documentación sin duplicar la lógica.

## Ejemplo aplicado

Para documentar respuestas de error y esquemas de entrada se declaran explícitamente:

```csharp
app.MapPost("/tareas", (TareaInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Titulo))
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Titulo"] = ["El título es obligatorio."]
        });
    }

    var tarea = new Tarea(Guid.NewGuid(), input.Titulo, false);
    tareas.Add(tarea);

    return TypedResults.Created($"/tareas/{tarea.Id}", tarea);
})
.WithName("CrearTarea")
.WithTags("Tareas")
.ProducesValidationProblem()
.Produces<Tarea>(StatusCodes.Status201Created);
```

En la práctica, cuando el handler tiene varios caminos conviene declarar el tipo de retorno en la firma —por ejemplo `Results<Created<Tarea>, ValidationProblem>`— o extraer una función local con ese tipo. `Produces` y `ProducesProblem` completan lo que el compilador no puede inferir y dejan la especificación alineada con la realidad.

Además, en CI se puede guardar el JSON generado y compararlo con el anterior. Si el contrato cambia sin querer —por ejemplo, un campo que desaparece— el diff lo muestra antes de que lo sufra un cliente.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La especificación está vacía | Falta `MapOpenApi` o se publica solo en otro entorno | Mapear el endpoint en el entorno correcto |
| Los errores 400 no aparecen | Se devuelven `IResult` sueltos | Usar `TypedResults` y `ProducesValidationProblem` |
| El cliente generado no compila | Nombres de operación duplicados o esquemas inconsistentes | Usar `WithName` único y revisar los records |
| La documentación expone endpoints internos | Se publica la especificación en producción sin filtro | Restringir el acceso o separar la API interna |
| El contrato cambia sin aviso | No se revisa el JSON en CI | Guardar el documento y comparar el diff |

## Buenas prácticas

- Generá la especificación desde el código; no la escribas a mano.
- Documentá los códigos de error, no solo el camino feliz.
- Nombrá las operaciones con `WithName` para que los clientes generados sean estables.
- Publicá la interfaz interactiva solo en desarrollo o detrás de autenticación.
- Revisá el diff del contrato en los pull requests que tocan la API.

## Ejercicio

Agregá OpenAPI a una API de tareas con `GET /tareas`, `POST /tareas` y `DELETE /tareas/{id}`. Documentá los estados 200, 201, 204, 400 y 404 con `TypedResults` y `Produces`. Verificá que el JSON de `/openapi/v1.json` incluya los cinco estados.

<details>
<summary>Ver solución</summary>

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var tareas = new List<Tarea>();

app.MapGet("/tareas", () => TypedResults.Ok(tareas))
   .WithName("ListarTareas");

app.MapPost("/tareas", (TareaInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Titulo))
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Titulo"] = ["El título es obligatorio."]
        });
    }

    var tarea = new Tarea(Guid.NewGuid(), input.Titulo);
    tareas.Add(tarea);
    return TypedResults.Created($"/tareas/{tarea.Id}", tarea);
})
.WithName("CrearTarea")
.ProducesValidationProblem();

app.MapDelete("/tareas/{id:guid}", (Guid id) =>
{
    var tarea = tareas.FirstOrDefault(t => t.Id == id);
    if (tarea is null)
    {
        return TypedResults.NotFound();
    }

    tareas.Remove(tarea);
    return TypedResults.NoContent();
})
.WithName("EliminarTarea");

app.Run();

public sealed record TareaInput(string Titulo);
public sealed record Tarea(Guid Id, string Titulo);
```

Al abrir `/openapi/v1.json` aparecen las tres operaciones y sus respuestas. Si falta un estado, el problema está en el tipo de retorno o en los metadatos.

</details>

## Resumen

- OpenAPI describe la API en un formato que consumen herramientas y personas.
- En .NET 10 se genera con `AddOpenApi` y `MapOpenApi`.
- `TypedResults`, `Produces` y `WithName` mantienen la documentación alineada con el código.
- El contrato se revisa en CI como cualquier otro cambio de código.

## Siguiente paso

Cuando la API crece, un archivo `Program.cs` de mil líneas deja de ser sostenible. En [Versionado y organización de endpoints](/modulos/aspnet-core-apis/versionado-organizacion) vas a ordenar los endpoints y planear sus versiones.
