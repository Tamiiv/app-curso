---
slug: validacion-errores
title: "Validación y errores HTTP"
order: 5
estimatedMinutes: 15
summary: "Validá la entrada en el borde de la API y devolvé errores claros con ProblemDetails, sin usar excepciones para flujos esperados."
quiz:
  - question: "¿Dónde conviene validar los datos que llegan a una API?"
    options:
      - "En la base de datos, con restricciones únicamente"
      - "En el borde de la API, antes de ejecutar la lógica de negocio"
      - "En el cliente, porque el servidor no puede validar"
      - "Solo cuando se produce una excepción"
    answer: 1
    explanation: "La validación en el borde evita trabajo innecesario y devuelve un error claro antes de tocar la base o el dominio."
  - question: "¿Qué formato estándar conviene usar para errores de una API HTTP?"
    options:
      - "Un string suelto con el mensaje"
      - "ProblemDetails (RFC 9457)"
      - "Una página HTML con el error"
      - "Siempre un código 200 con el error en el cuerpo"
    answer: 1
    explanation: "ProblemDetails define campos estándar como type, title, status y detail, y permite extensiones como errors por campo."
  - question: "¿Cuándo está justificado lanzar una excepción en una API?"
    options:
      - "Cuando falta un campo obligatorio"
      - "Cuando el id no existe"
      - "Cuando ocurre un fallo inesperado que no se puede manejar en el flujo normal"
      - "Cuando el usuario escribe mal un email"
    answer: 2
    explanation: "Los errores esperados se responden con resultados HTTP concretos. Las excepciones se reservan para fallos inesperados."
  - question: "¿Qué estado HTTP corresponde a un cuerpo inválido bien formado pero con datos que no cumplen las reglas?"
    options:
      - "400 Bad Request"
      - "401 Unauthorized"
      - "404 Not Found"
      - "500 Internal Server Error"
    answer: 0
    explanation: "400 indica que la petición no es válida. 422 se usa en algunos diseños, pero 400 es la opción más extendida para validación de entrada."
---

# Validación y errores HTTP

## Objetivo

Validar la entrada de una API con DataAnnotations, devolver errores consistentes con `ProblemDetails` y distinguir los errores esperados de los fallos inesperados.

## Requisitos previos

Venís de [Middleware y pipeline](/modulos/aspnet-core-apis/middleware). Necesitás manejar Minimal APIs, records y el patrón de resultados `Results`/`TypedResults`.

## Concepto

Una API recibe datos que no controla. El cliente puede mandar campos vacíos, números fuera de rango, un email mal formado o un JSON con una forma distinta a la esperada. Validar no es desconfiar del usuario: es definir el contrato y proteger el sistema.

Hay tres niveles de validación y cada uno tiene su lugar:

| Nivel | Qué valida | Dónde |
|---|---|---|
| Sintaxis | JSON bien formado, tipos correctos | Framework, antes del handler |
| Entrada | Campos obligatorios, rangos, formatos | Borde de la API |
| Reglas de negocio | Stock disponible, email único, permisos | Dominio o servicio |

Este orden importa. No tiene sentido consultar la base para saber si un email ya existe cuando el campo llegó vacío.

Para los errores se usa un formato estándar: **ProblemDetails** (RFC 9457). Un cuerpo típico:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Titulo": ["El título es obligatorio."],
    "Prioridad": ["La prioridad debe estar entre 1 y 5."]
  }
}
```

La ventaja no es estética: clientes, proxies y herramientas de observabilidad entienden el mismo formato en todos los endpoints.

La regla práctica es simple: **los errores esperados se devuelven, los inesperados se lanzan**. Un email duplicado es un `409 Conflict`, no una excepción. Una base de datos caída sí es una excepción, y el middleware de errores la convierte en un `500` sin filtrar detalles internos.

## Ejemplo mínimo

```csharp
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/tareas", (TareaInput input) =>
{
    var errores = new Dictionary<string, string[]>();
    var contexto = new ValidationContext(input);
    var resultados = new List<ValidationResult>();

    if (!Validator.TryValidateObject(input, contexto, resultados, validateAllProperties: true))
    {
        foreach (var grupo in resultados.GroupBy(r => r.MemberNames.FirstOrDefault() ?? "General"))
        {
            errores[grupo.Key] = grupo.Select(r => r.ErrorMessage ?? "Valor inválido.").ToArray();
        }

        return Results.ValidationProblem(errores);
    }

    var tarea = new Tarea(Guid.NewGuid(), input.Titulo, input.Prioridad);
    return Results.Created($"/tareas/{tarea.Id}", tarea);
});

app.Run();

public sealed record TareaInput(
    [property: Required(ErrorMessage = "El título es obligatorio.")]
    [property: StringLength(120, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 120 caracteres.")]
    string Titulo,
    [property: Range(1, 5, ErrorMessage = "La prioridad debe estar entre 1 y 5.")]
    int Prioridad);

public sealed record Tarea(Guid Id, string Titulo, int Prioridad);
```

```bash
curl -i -X POST http://localhost:5137/tareas \
  -H "Content-Type: application/json" \
  -d '{"titulo":"","prioridad":9}'
```

```text
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Titulo": ["El título debe tener entre 3 y 120 caracteres.", "El título es obligatorio."],
    "Prioridad": ["La prioridad debe estar entre 1 y 5."]
  }
}
```

`Results.ValidationProblem` arma el `ProblemDetails` con estado 400 y el diccionario de errores. Los atributos viven en el record de entrada, no en la entidad de dominio.

## Ejemplo aplicado

Repetir el bloque de validación en cada endpoint es ruido. Un filtro de endpoint lo centraliza:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/tareas", (TareaInput input) =>
{
    var tarea = new Tarea(Guid.NewGuid(), input.Titulo, input.Prioridad);
    return Results.Created($"/tareas/{tarea.Id}", tarea);
})
.AddEndpointFilter(ValidarEntrada);

app.MapPut("/tareas/{id:guid}", (Guid id, TareaInput input) =>
{
    var tarea = new Tarea(id, input.Titulo, input.Prioridad);
    return Results.Ok(tarea);
})
.AddEndpointFilter(ValidarEntrada);

app.Run();

static async ValueTask<object?> ValidarEntrada(
    EndpointFilterInvocationContext contexto,
    EndpointFilterDelegate siguiente)
{
    var errores = new Dictionary<string, string[]>();

    foreach (var argumento in contexto.Arguments)
    {
        if (argumento is null)
        {
            continue;
        }

        var resultados = new List<ValidationResult>();
        var validationContext = new ValidationContext(argumento);

        if (Validator.TryValidateObject(argumento, validationContext, resultados, validateAllProperties: true))
        {
            continue;
        }

        foreach (var grupo in resultados.GroupBy(r => r.MemberNames.FirstOrDefault() ?? "General"))
        {
            errores[grupo.Key] = grupo.Select(r => r.ErrorMessage ?? "Valor inválido.").ToArray();
        }
    }

    if (errores.Count > 0)
    {
        return Results.ValidationProblem(errores);
    }

    return await siguiente(contexto);
}

public sealed record TareaInput(
    [property: Required, property: StringLength(120, MinimumLength = 3)]
    string Titulo,
    [property: Range(1, 5)]
    int Prioridad);

public sealed record Tarea(Guid Id, string Titulo, int Prioridad);
```

El filtro se ejecuta antes del handler y devuelve el error sin repetir código. En una API real también conviene agregar un límite de tamaño del cuerpo y validar que el `Content-Type` sea JSON, que el framework ya controla en gran medida.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El error devuelve 500 con stack trace | Se dejó una excepción sin manejar | Usar `UseExceptionHandler` y no exponer detalles en producción |
| La validación nunca falla | Faltan los atributos o no se ejecuta `TryValidateObject` | Revisar que los atributos estén en el record de entrada y que el validador se llame |
| Los errores llegan como un string suelto | Se devolvió `Results.BadRequest("...")` a mano | Usar `Results.ValidationProblem` para mantener el contrato |
| Se usan excepciones para campos vacíos | Se trata la validación como un fallo del sistema | Devolver 400 antes de ejecutar la lógica |
| El cliente no distingue qué campo falló | El diccionario usa claves genéricas o mensajes únicos | Agrupar por nombre de propiedad |
| Se filtra el mensaje de la base de datos | La excepción se serializa tal cual | Registrar el detalle y devolver un `ProblemDetails` genérico |

## Buenas prácticas

- Validá en el borde y volvé a validar en el dominio solo las reglas que dependen del estado.
- Usá nombres de propiedad del contrato en las claves de error, no nombres internos de la entidad.
- Devolvé `409` para conflictos de unicidad y `404` cuando el recurso no existe.
- No expongas stack traces ni mensajes de SQL en producción.
- Documentá en OpenAPI los códigos de error de cada endpoint.

## Ejercicio

Implementá un endpoint `POST /productos` que reciba `Nombre`, `Precio` y `Stock`. Validá que el nombre tenga entre 2 y 80 caracteres, que el precio sea mayor que cero y que el stock no sea negativo. Devolvé `201 Created` con el producto o `400` con los errores por campo.

<details>
<summary>Ver solución</summary>

```csharp
app.MapPost("/productos", (ProductoInput input) =>
{
    var producto = new Producto(Guid.NewGuid(), input.Nombre, input.Precio, input.Stock);
    return Results.Created($"/productos/{producto.Id}", producto);
})
.AddEndpointFilter(ValidarEntrada);

public sealed record ProductoInput(
    [property: Required, property: StringLength(80, MinimumLength = 2)]
    string Nombre,
    [property: Range(typeof(decimal), "0.01", "999999999")]
    decimal Precio,
    [property: Range(0, int.MaxValue)]
    int Stock);

public sealed record Producto(Guid Id, string Nombre, decimal Precio, int Stock);
```

```bash
curl -X POST http://localhost:5137/productos \
  -H "Content-Type: application/json" \
  -d '{"nombre":"A","precio":0,"stock":-1}'
```

```text
HTTP/1.1 400 Bad Request
{
  "status": 400,
  "errors": {
    "Nombre": ["The field Nombre must be a string with a minimum length of 2 and a maximum length of 80."],
    "Precio": ["The field Precio must be between 0.01 and 999999999."],
    "Stock": ["The field Stock must be between 0 and 2147483647."]
  }
}
```

Reutilizar el filtro evita duplicar la validación y mantiene el mismo formato de error en toda la API.

</details>

## Resumen

- Validá en el borde: sintaxis, entrada y luego reglas de negocio.
- `ProblemDetails` es el contrato estándar para errores HTTP.
- Los errores esperados se devuelven con un estado concreto; las excepciones se reservan para fallos inesperados.
- Un filtro de endpoint centraliza la validación sin repetir código.

## Siguiente paso

Con la entrada controlada, el próximo tema es elegir el [código de estado y el tipo de resultado](/modulos/aspnet-core-apis/codigos-estado) correcto para cada respuesta.
