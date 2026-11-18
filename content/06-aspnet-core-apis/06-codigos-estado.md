---
slug: codigos-estado
title: "Códigos de estado y resultados"
order: 6
estimatedMinutes: 12
summary: "Elegí el código HTTP correcto para cada caso y usá TypedResults para que el contrato de la API sea explícito y verificable."
quiz:
  - question: "¿Qué código corresponde cuando se crea un recurso con éxito?"
    options:
      - "200 OK"
      - "201 Created"
      - "204 No Content"
      - "302 Found"
    answer: 1
    explanation: "201 Created indica que el recurso se creó y suele incluir la cabecera Location con la URL del recurso nuevo."
  - question: "¿Qué código indica que la petición es válida pero el usuario no está autenticado?"
    options:
      - "400 Bad Request"
      - "401 Unauthorized"
      - "403 Forbidden"
      - "404 Not Found"
    answer: 1
    explanation: "401 significa que faltan credenciales válidas. 403 significa que el usuario está autenticado pero no tiene permiso."
  - question: "¿Qué diferencia hay entre TypedResults y Results?"
    options:
      - "TypedResults es más lento"
      - "TypedResults devuelve tipos concretos que mejoran la documentación y las pruebas"
      - "Results solo funciona con JSON"
      - "No hay diferencia práctica"
    answer: 1
    explanation: "TypedResults declara el tipo de respuesta, lo que permite que OpenAPI y los tests conozcan los estados posibles."
  - question: "¿Cuándo conviene devolver 204 No Content?"
    options:
      - "Cuando se crea un recurso"
      - "Cuando la operación se completó y no hay cuerpo que devolver"
      - "Cuando falta autenticación"
      - "Cuando el servidor falla"
    answer: 1
    explanation: "204 confirma el éxito sin cuerpo, típico de un DELETE o de una actualización que no devuelve datos."
---

# Códigos de estado y resultados

## Objetivo

Elegir el código de estado HTTP adecuado para cada situación y escribir handlers con `TypedResults` que declaren su contrato de respuesta.

## Requisitos previos

Haber leído [Validación y errores HTTP](/modulos/aspnet-core-apis/validacion-errores). Necesitás manejar Minimal APIs y el formato `ProblemDetails`.

## Concepto

El código de estado es parte de la API, no un detalle. Un cliente decide qué hacer según el estado: reintentar, pedir credenciales, mostrar un error o seguir adelante. Usar `200` para todo obliga al cliente a leer el cuerpo para adivinar qué pasó.

Los códigos que vas a usar casi todos los días:

| Código | Cuándo |
|---|---|
| `200 OK` | La operación salió bien y hay cuerpo |
| `201 Created` | Se creó un recurso; incluye `Location` |
| `204 No Content` | Salió bien y no hay cuerpo |
| `400 Bad Request` | La entrada no es válida |
| `401 Unauthorized` | Faltan credenciales válidas |
| `403 Forbidden` | Hay identidad, pero no permiso |
| `404 Not Found` | El recurso no existe |
| `409 Conflict` | Choca con el estado actual, por ejemplo un duplicado |
| `422 Unprocessable Content` | La forma es válida pero las reglas no |
| `500 Internal Server Error` | Fallo inesperado del servidor |

`401` y `403` se confunden todo el tiempo. La pregunta que los separa es: **¿sabemos quién es?** Si no lo sabemos, `401`. Si lo sabemos y no puede, `403`.

`400` y `422` también se solapan. Para validación de campos, `400` con `ProblemDetails` es la opción más simple y extendida. `422` aparece cuando el cuerpo está bien formado pero viola una regla de negocio.

En Minimal APIs hay dos formas de devolver respuestas:

- `Results`: métodos estáticos que devuelven `IResult`.
- `TypedResults`: igual, pero con tipos concretos como `Ok<T>`, `Created<T>` o `NotFound`.

`TypedResults` es preferible porque el compilador, OpenAPI y los tests conocen los estados posibles. Un handler que devuelve `Results<Ok<Tarea>, NotFound>` documenta su contrato sin escribir comentarios.

## Ejemplo mínimo

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var tareas = new Dictionary<Guid, Tarea>();

app.MapGet("/tareas/{id:guid}", (Guid id) =>
{
    return tareas.TryGetValue(id, out var tarea)
        ? TypedResults.Ok(tarea)
        : TypedResults.NotFound();
});

app.MapPost("/tareas", (TareaInput input) =>
{
    var tarea = new Tarea(Guid.NewGuid(), input.Titulo);
    tareas[tarea.Id] = tarea;

    return TypedResults.Created($"/tareas/{tarea.Id}", tarea);
});

app.MapDelete("/tareas/{id:guid}", (Guid id) =>
{
    return tareas.Remove(id)
        ? TypedResults.NoContent()
        : TypedResults.NotFound();
});

app.Run();

public sealed record TareaInput(string Titulo);
public sealed record Tarea(Guid Id, string Titulo);
```

```bash
curl -i -X POST http://localhost:5137/tareas \
  -H "Content-Type: application/json" -d '{"titulo":"Estudiar"}'
```

```text
HTTP/1.1 201 Created
Location: /tareas/7c1f5b1e-2a6d-4f0e-9a6e-1f5c7a2b9d10
Content-Type: application/json

{"id":"7c1f5b1e-2a6d-4f0e-9a6e-1f5c7a2b9d10","titulo":"Estudiar"}
```

`Created` recibe la URL del recurso nuevo. Esa cabecera `Location` permite al cliente consultarlo sin adivinar el id.

## Ejemplo aplicado

Un handler con varios caminos declara todos sus resultados posibles:

```csharp
app.MapPut("/tareas/{id:guid}", (
    Guid id,
    TareaInput input,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(input.Titulo))
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Titulo"] = ["El título es obligatorio."]
        });
    }

    if (!tareas.TryGetValue(id, out var existente))
    {
        return TypedResults.NotFound();
    }

    if (existente.Titulo == input.Titulo)
    {
        return TypedResults.Conflict(new ProblemDetails
        {
            Title = "No hay cambios para aplicar.",
            Status = StatusCodes.Status409Conflict
        });
    }

    var actualizada = existente with { Titulo = input.Titulo };
    tareas[id] = actualizada;

    logger.LogInformation("Tarea {Id} actualizada", id);
    return TypedResults.Ok(actualizada);
});
```

El tipo de retorno se infiere como `Results<ValidationProblem, NotFound, Conflict<ProblemDetails>, Ok<Tarea>>`. Esa firma es documentación ejecutable: no hay estados escondidos.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Todo devuelve 200 con `{ "ok": false }` | Se ignoran los códigos HTTP | Usar el estado que corresponda y reservar el cuerpo para los datos |
| Un 401 aparece cuando el usuario ya inició sesión | El permiso falta, no la identidad | Revisar si corresponde `403` |
| El cliente no encuentra el recurso creado | Falta la cabecera `Location` | Devolver `TypedResults.Created(url, recurso)` |
| Se devuelve 500 por un dato faltante | Se lanza una excepción para un caso esperado | Devolver `400` o `404` según el caso |
| Un `DELETE` devuelve 200 con `null` | Se quiere confirmar sin cuerpo | Usar `204 No Content` |
| OpenAPI muestra solo 200 | El handler devuelve `IResult` o `object` | Usar `TypedResults` y `Results<...>` para declarar los estados |

## Buenas prácticas

- Modelá cada caso con un resultado explícito, no con banderas booleanas.
- Devolvé `201` con `Location` al crear recursos.
- Usá `204` en operaciones exitosas sin cuerpo.
- No devuelvas detalles internos en `500`.
- Documentá los estados con `Produces` o `TypedResults` para que OpenAPI no adivine.
- Mantené la coherencia: el mismo tipo de situación siempre con el mismo código.

## Ejercicio

Implementá `GET`, `POST` y `DELETE` para `/clientes`. `GET /clientes/{id}` devuelve 200 o 404. `POST` devuelve 201 con `Location` y 409 si el email ya existe. `DELETE` devuelve 204 o 404. Usá `TypedResults` en los tres.

<details>
<summary>Ver solución</summary>

```csharp
var clientes = new Dictionary<Guid, Cliente>();
var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

app.MapGet("/clientes/{id:guid}", (Guid id) =>
    clientes.TryGetValue(id, out var cliente)
        ? TypedResults.Ok(cliente)
        : TypedResults.NotFound());

app.MapPost("/clientes", (ClienteInput input) =>
{
    if (!emails.Add(input.Email))
    {
        return TypedResults.Conflict(new ProblemDetails
        {
            Title = "El email ya está registrado.",
            Status = StatusCodes.Status409Conflict
        });
    }

    var cliente = new Cliente(Guid.NewGuid(), input.Nombre, input.Email);
    clientes[cliente.Id] = cliente;
    return TypedResults.Created($"/clientes/{cliente.Id}", cliente);
});

app.MapDelete("/clientes/{id:guid}", (Guid id) =>
    clientes.Remove(id)
        ? TypedResults.NoContent()
        : TypedResults.NotFound());

public sealed record ClienteInput(string Nombre, string Email);
public sealed record Cliente(Guid Id, string Nombre, string Email);
```

```text
POST /clientes  {"nombre":"Ana","email":"ana@ejemplo.com"}  → 201 Created
POST /clientes  {"nombre":"Ana","email":"ana@ejemplo.com"}  → 409 Conflict
DELETE /clientes/{id-inexistente}                          → 404 Not Found
DELETE /clientes/{id-existente}                            → 204 No Content
```

</details>

## Resumen

- El código de estado comunica el resultado sin que el cliente lea el cuerpo.
- `401` es falta de identidad; `403` es falta de permiso.
- `201` incluye `Location`; `204` confirma sin cuerpo.
- `TypedResults` hace explícito el contrato de la API.

## Siguiente paso

Con el contrato definido, falta documentarlo: en [OpenAPI y documentación](/modulos/aspnet-core-apis/openapi) vas a generar una referencia navegable de tu API.
