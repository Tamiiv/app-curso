---
slug: pruebas-apis
title: "Pruebas de APIs"
order: 7
estimatedMinutes: 15
summary: "Verificá contratos HTTP completos: métodos, rutas, códigos de estado, headers, JSON de respuesta y errores de validación."
quiz:
  - question: "¿Qué método de HttpClient envía un objeto como JSON en el cuerpo de un POST?"
    options:
      - "GetAsync"
      - "PostAsJsonAsync"
      - "PostAsync con StringContent vacío"
      - "SendAsync sin contenido"
    answer: 1
    explanation: "PostAsJsonAsync serializa el objeto, configura el Content-Type application/json y envía la petición."
  - question: "¿Qué código de estado corresponde cuando se crea un recurso correctamente?"
    options:
      - "200"
      - "201"
      - "204"
      - "400"
    answer: 1
    explanation: "201 Created indica que el recurso se creó; suele acompañarse del header Location con la URL del nuevo recurso."
  - question: "¿Qué conviene verificar en una respuesta de error de validación?"
    options:
      - "Solo que el status sea 400"
      - "El status, el Content-Type y los errores del ProblemDetails"
      - "El texto exacto de la excepción interna"
      - "Que el servidor no haya registrado nada"
    answer: 1
    explanation: "El contrato de error incluye el código, el tipo de contenido y la estructura de errores; verificar solo el 400 deja pasar regresiones."
  - question: "¿Por qué no conviene que un test dependa del orden de otro?"
    options:
      - "Porque xUnit siempre ejecuta en orden alfabético"
      - "Porque el estado compartido hace que los tests fallen de forma intermitente"
      - "Porque HttpClient no permite dos peticiones"
      - "Porque el runner borra la base entre tests"
    answer: 1
    explanation: "Si un test necesita datos creados por otro, el resultado depende del orden y de la ejecución previa; cada test debe preparar su propio escenario."
---

# Pruebas de APIs

## Objetivo

Probar el contrato HTTP de una API: método, ruta, código de estado, headers, cuerpo JSON y errores de validación. Detectar regresiones que un test unitario del servicio no puede ver.

## Requisitos previos

Necesitás saber levantar la API con `WebApplicationFactory<Program>` y reemplazar servicios. Si no lo hiciste, repasá [WebApplicationFactory](/modulos/testing/webapplicationfactory). También necesitás manejar `HttpClient` y serialización JSON.

## Concepto

Una API es un contrato. El cliente no ve tus clases ni tus servicios: ve métodos HTTP, rutas, códigos de estado, headers y un cuerpo. Un test de API verifica ese contrato desde afuera, como lo haría un consumidor real.

Las dimensiones que conviene cubrir son:

| Dimensión | Ejemplo |
|---|---|
| Método y ruta | `GET /tareas/1`, `POST /tareas` |
| Código de estado | 200, 201, 204, 400, 404 |
| Headers | `Location`, `Content-Type` |
| Cuerpo | JSON con los campos esperados |
| Errores | ProblemDetails con validaciones |
| Casos borde | id inexistente, cuerpo vacío, datos inválidos |

No alcanza con comprobar que la respuesta no sea un error. Un `POST` que devuelve 200 en lugar de 201, o un `GET` que devuelve una lista vacía cuando debería tener un elemento, son regresiones reales aunque el status sea exitoso.

Los tests de API suelen usar `WebApplicationFactory` y un `HttpClient`. La diferencia con la lección anterior es el foco: ahí viste cómo levantar el servidor y reemplazar dependencias; acá vas a verificar el contrato con precisión.

Un aspecto clave es el aislamiento. Si todos los tests comparten un repositorio singleton y uno borra datos, los demás pueden fallar según el orden. Cada test debe crear su propio escenario o usar datos únicos. La opción más segura es crear una factory nueva con un fake nuevo por test, aunque cueste unos milisegundos más.

Para leer JSON usá `ReadFromJsonAsync<T>`, que aplica las mismas convenciones web que la API: nombres en camelCase y comparación sin distinguir mayúsculas. Para escribir, `PostAsJsonAsync` y `PutAsJsonAsync` hacen la serialización y configuran el `Content-Type`.

Los errores de validación en ASP.NET Core se representan con `ProblemDetails` o `ValidationProblemDetails`. Verificá el status 400, el `Content-Type` `application/problem+json` y la presencia de las claves de error. No verifiques el texto exacto salvo que sea parte del contrato.

## Ejemplo mínimo

Una API con un endpoint que busca una tarea por id. Si no existe, responde 404.

```csharp
app.MapGet("/tareas/{id:int}", (int id, ITareaRepositorio repositorio) =>
{
    Tarea? tarea = repositorio.Buscar(id);
    return tarea is null ? Results.NotFound() : Results.Ok(tarea);
});
```

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public sealed class TareaPorIdTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetTarea_Existente_DevuelveOkConElCuerpo()
    {
        HttpResponseMessage respuesta = await _client.GetAsync("/tareas/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);
        Tarea? tarea = await respuesta.Content.ReadFromJsonAsync<Tarea>();
        Assert.NotNull(tarea);
        Assert.Equal(1, tarea.Id);
    }

    [Fact]
    public async Task GetTarea_Inexistente_DevuelveNotFound()
    {
        HttpResponseMessage respuesta = await _client.GetAsync("/tareas/9999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 396 ms
```

El segundo test no necesita leer el cuerpo: el contrato para un recurso inexistente es el status 404. Si la API devolviera 200 con `null`, el test fallaría.

## Ejemplo aplicado

Una API de tareas permite crear, consultar y eliminar. El repositorio es un fake en memoria que se renueva por test.

```csharp
public sealed record Tarea(int Id, string Titulo, bool Completada);

public sealed record CrearTarea(string Titulo);

public interface ITareaRepositorio
{
    Tarea? Buscar(int id);
    IReadOnlyList<Tarea> Todas();
    Tarea Agregar(string titulo);
    bool Eliminar(int id);
}
```

Los endpoints:

```csharp
app.MapGet("/tareas", (ITareaRepositorio repositorio) =>
    Results.Ok(repositorio.Todas()));

app.MapPost("/tareas", (CrearTarea entrada, ITareaRepositorio repositorio) =>
{
    if (string.IsNullOrWhiteSpace(entrada.Titulo))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["titulo"] = ["El título es obligatorio."]
        });
    }

    Tarea tarea = repositorio.Agregar(entrada.Titulo.Trim());
    return Results.Created($"/tareas/{tarea.Id}", tarea);
});

app.MapDelete("/tareas/{id:int}", (int id, ITareaRepositorio repositorio) =>
    repositorio.Eliminar(id) ? Results.NoContent() : Results.NotFound());
```

El test crea una aplicación nueva con un repositorio vacío para cada caso:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public sealed class RepositorioFake : ITareaRepositorio
{
    private readonly Dictionary<int, Tarea> _tareas = [];
    private int _siguienteId = 1;

    public Tarea? Buscar(int id) => _tareas.GetValueOrDefault(id);

    public IReadOnlyList<Tarea> Todas() => _tareas.Values.ToList();

    public Tarea Agregar(string titulo)
    {
        var tarea = new Tarea(_siguienteId++, titulo, false);
        _tareas[tarea.Id] = tarea;
        return tarea;
    }

    public bool Eliminar(int id) => _tareas.Remove(id);
}

public sealed class TareasApiTests
{
    private static WebApplicationFactory<Program> CrearApp()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITareaRepositorio>();
                services.AddSingleton<ITareaRepositorio, RepositorioFake>();
            }));
    }

    [Fact]
    public async Task PostTarea_Valida_DevuelveCreatedConLocation()
    {
        using var app = CrearApp();
        using HttpClient cliente = app.CreateClient();

        HttpResponseMessage respuesta = await cliente.PostAsJsonAsync(
            "/tareas", new CrearTarea("Preparar final"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("/tareas/1", respuesta.Headers.Location?.OriginalString);
        Tarea? tarea = await respuesta.Content.ReadFromJsonAsync<Tarea>();
        Assert.NotNull(tarea);
        Assert.Equal("Preparar final", tarea.Titulo);
        Assert.False(tarea.Completada);
    }

    [Fact]
    public async Task PostTarea_TituloVacio_DevuelveValidationProblem()
    {
        using var app = CrearApp();
        using HttpClient cliente = app.CreateClient();

        HttpResponseMessage respuesta = await cliente.PostAsJsonAsync(
            "/tareas", new CrearTarea("   "));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        ValidationProblemDetails? problema =
            await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problema);
        Assert.True(problema.Errors.ContainsKey("titulo"));
    }

    [Fact]
    public async Task DeleteTarea_Existente_DevuelveNoContentYDejaDeAparecer()
    {
        using var app = CrearApp();
        using HttpClient cliente = app.CreateClient();
        HttpResponseMessage creada = await cliente.PostAsJsonAsync("/tareas", new CrearTarea("Temporal"));
        Tarea? tarea = await creada.Content.ReadFromJsonAsync<Tarea>();

        HttpResponseMessage borrada = await cliente.DeleteAsync($"/tareas/{tarea!.Id}");
        HttpResponseMessage consultada = await cliente.GetAsync($"/tareas/{tarea.Id}");

        Assert.Equal(HttpStatusCode.NoContent, borrada.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consultada.StatusCode);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 641 ms
```

Cada test crea su propia factory y su propio repositorio, así que el orden no importa. El test de borrado encadena tres peticiones porque describe un flujo: crear, borrar y comprobar que ya no existe.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El test pasa pero la API devuelve otra cosa | Solo se verifica que no haya excepción | Assert sobre status, headers y cuerpo |
| Los tests fallan según el orden | Repositorio o base compartidos entre casos | Crear estado nuevo por test |
| `ReadFromJsonAsync` devuelve `null` | El cuerpo no es JSON o el status no es exitoso | Revisar `Content-Type` y el status antes de deserializar |
| Se compara el texto exacto del error | El mensaje cambia con facilidad | Verificar claves de `ProblemDetails` y códigos |
| El `POST` devuelve 200 en vez de 201 | El endpoint no usa `Results.Created` | Ajustar el contrato y cubrirlo con un test |
| Un test tarda segundos | Se levanta un servidor real o se usa una base externa | Usar `WebApplicationFactory` y dobles o SQLite en memoria |

## Buenas prácticas

- Probá el contrato observable: método, ruta, status, headers y JSON.
- Un test por comportamiento; podés encadenar peticiones cuando el flujo sea el comportamiento.
- Creá datos únicos o una aplicación nueva por test para evitar dependencias de orden.
- Verificá `ProblemDetails` en los errores, no solo el código 400.
- Incluí al menos un caso feliz y un caso borde por endpoint crítico.

## Ejercicio

Agregá a la API de tareas un endpoint `PUT /tareas/{id}/completar` que marque la tarea como completada y devuelva 200 con la tarea actualizada, o 404 si no existe. Escribí dos tests: uno para el caso exitoso y otro para el id inexistente. Verificá status y cuerpo.

<details>
<summary>Ver solución</summary>

Endpoint:

```csharp
app.MapPut("/tareas/{id:int}/completar", (int id, ITareaRepositorio repositorio) =>
{
    Tarea? tarea = repositorio.Buscar(id);
    if (tarea is null)
    {
        return Results.NotFound();
    }

    Tarea actualizada = tarea with { Completada = true };
    repositorio.Reemplazar(actualizada);
    return Results.Ok(actualizada);
});
```

Agregá `void Reemplazar(Tarea tarea)` a `ITareaRepositorio` y al fake:

```csharp
public void Reemplazar(Tarea tarea) => _tareas[tarea.Id] = tarea;
```

Tests:

```csharp
using System.Net;
using System.Net.Http.Json;
using Xunit;

public sealed class CompletarTareaTests
{
    [Fact]
    public async Task PutCompletar_Existente_DevuelveOkYCompletada()
    {
        using var app = TareasApiTests.CrearApp();
        using HttpClient cliente = app.CreateClient();
        HttpResponseMessage creada = await cliente.PostAsJsonAsync("/tareas", new CrearTarea("Repasar"));
        Tarea? tarea = await creada.Content.ReadFromJsonAsync<Tarea>();

        HttpResponseMessage respuesta = await cliente.PutAsync(
            $"/tareas/{tarea!.Id}/completar", content: null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Tarea? actualizada = await respuesta.Content.ReadFromJsonAsync<Tarea>();
        Assert.NotNull(actualizada);
        Assert.True(actualizada.Completada);
        Assert.Equal(tarea.Id, actualizada.Id);
    }

    [Fact]
    public async Task PutCompletar_Inexistente_DevuelveNotFound()
    {
        using var app = TareasApiTests.CrearApp();
        using HttpClient cliente = app.CreateClient();

        HttpResponseMessage respuesta = await cliente.PutAsync(
            "/tareas/9999/completar", content: null);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 512 ms
```

`CrearApp` debe ser accesible desde la clase del ejercicio; si está como `private` en `TareasApiTests`, cambiala a `internal static` o movela a una clase auxiliar.

</details>

## Resumen

- Un test de API verifica el contrato HTTP, no los detalles internos del handler.
- Comprobá método, ruta, status, headers, cuerpo y errores de validación.
- `PostAsJsonAsync` y `ReadFromJsonAsync<T>` simplifican el intercambio JSON.
- Cada test debe preparar su propio estado para no depender del orden.
- `ProblemDetails` es el contrato estándar de errores en ASP.NET Core.

## Siguiente paso

La suite funciona, pero ¿cuánto del código estás probando y qué tan buenos son esos tests? En la próxima lección vas a medir [cobertura y calidad](/modulos/testing/cobertura-calidad).
