---
slug: ejercicio-integrador
title: "Ejercicio integrador: suite de pruebas"
order: 9
estimatedMinutes: 25
summary: "Armá una suite completa para una API pequeña: unitarios del dominio, integración con WebApplicationFactory y pruebas de los caminos de error."
quiz:
  - question: "¿Qué conviene probar con un test unitario y no con integración?"
    options:
      - "El código de estado de un endpoint"
      - "Una regla de negocio pura, sin base de datos ni HTTP"
      - "El pipeline completo de middleware"
      - "La serialización JSON"
    answer: 1
    explanation: "Las reglas puras se prueban rápido y aisladas. La integración se reserva para verificar que las piezas conectadas funcionan juntas."
  - question: "¿Qué clase base aporta WebApplicationFactory para probar una API real?"
    options:
      - "Una base de datos en memoria"
      - "Un servidor en memoria con el pipeline completo de la aplicación"
      - "Un navegador automatizado"
      - "Un generador de mocks"
    answer: 1
    explanation: "Levanta la app en memoria y permite enviar peticiones HTTP reales sin abrir un puerto externo."
  - question: "¿Por qué cada test de integración debe preparar su propio estado?"
    options:
      - "Para que el runner los ejecute más rápido"
      - "Porque el orden de ejecución no está garantizado y el estado compartido genera fallos intermitentes"
      - "Porque xUnit no permite compartir estado"
      - "Para aumentar la cobertura"
    answer: 1
    explanation: "Tests independientes son repetibles y se pueden ejecutar en paralelo sin interferencias."
  - question: "¿Qué debe verificar un test cuando se envía un cuerpo inválido?"
    options:
      - "Solo que la respuesta no sea 500"
      - "El código 400, el formato ProblemDetails y los campos con error"
      - "Que el servidor siga vivo"
      - "Que el JSON tenga el mismo tamaño"
    answer: 1
    explanation: "El contrato incluye el estado y la forma del error. Verificar ambos evita que un cambio rompa al cliente."
---

# Ejercicio integrador: suite de pruebas

## Objetivo

Construir una suite de pruebas para una API de tareas que combine tests unitarios del dominio, tests de integración con `WebApplicationFactory` y pruebas de los caminos de error.

## Requisitos previos

Haber recorrido el módulo: [xUnit](/modulos/testing/xunit-primeros-pasos), [Arrange, Act, Assert](/modulos/testing/arrange-act-assert), [Unitarios vs. integración](/modulos/testing/unitarios-vs-integracion), [Dobles de prueba](/modulos/testing/dobles-de-prueba), [TDD](/modulos/testing/tdd), [WebApplicationFactory](/modulos/testing/webapplicationfactory) y [Pruebas de APIs](/modulos/testing/pruebas-apis).

## Concepto

Una suite sana tiene forma de pirámide: muchos tests unitarios rápidos, algunos de integración y pocos de extremo a extremo. Este integrador arma la base para una API de tareas.

La estrategia:

| Nivel | Qué prueba | Herramienta |
|---|---|---|
| Unitario | Reglas del servicio: crear, completar, eliminar | xUnit + asserts |
| Integración | Contrato HTTP: estados, validación, JSON | `WebApplicationFactory<Program>` |
| Casos de error | 400, 404 y duplicados | Peticiones reales |

El proyecto de tests necesita:

- Referencia al proyecto web.
- `Microsoft.NET.Test.Sdk`.
- `xunit` y `xunit.runner.visualstudio`.
- `Microsoft.AspNetCore.Mvc.Testing`.
- `coverlet.collector` para cobertura.

La app debe exponer `public partial class Program;` al final de `Program.cs` para que la factory encuentre el punto de entrada.

## Ejemplo mínimo

Empezá por el test unitario del servicio, sin HTTP:

```csharp
using AppCurso.Api;

namespace AppCurso.Tests;

public sealed class TareaServiceTests
{
    [Fact]
    public void CrearAgregaLaTarea()
    {
        var service = new TareaService();

        var (resultado, tarea) = service.Crear(new TareaInput("Estudiar testing", 3));

        Assert.Equal(ResultadoTarea.Ok, resultado);
        Assert.NotNull(tarea);
        Assert.False(tarea.Completada);
        Assert.Single(service.Listar());
    }

    [Fact]
    public void CompletarEsIdempotente()
    {
        var service = new TareaService();
        var (_, tarea) = service.Crear(new TareaInput("Repasar", 2));

        var primera = service.Completar(tarea!.Id);
        var segunda = service.Completar(tarea.Id);

        Assert.True(primera?.Completada);
        Assert.True(segunda?.Completada);
        Assert.Single(service.Listar());
    }

    [Fact]
    public void CompletarInexistenteDevuelveNull()
    {
        var service = new TareaService();

        Assert.Null(service.Completar(Guid.NewGuid()));
    }
}
```

```text
Passed! - Failed: 0, Passed: 3, Skipped: 0
```

Estos tests corren en milisegundos y no necesitan servidor ni base de datos.

## Ejemplo aplicado

La integración verifica el contrato HTTP completo:

```csharp
using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace AppCurso.Tests;

public sealed class TareasApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TareasApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task CrearTareaDevuelve201ConLocation()
    {
        var response = await _client.PostAsJsonAsync(
            "/tareas",
            new { titulo = "Terminar la suite", prioridad = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var tarea = await response.Content.ReadFromJsonAsync<TareaResponse>();
        Assert.NotNull(tarea);
        Assert.False(tarea.Completada);
    }

    [Fact]
    public async Task TituloCortoDevuelveProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/tareas",
            new { titulo = "ab", prioridad = 2 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problema = await response.Content.ReadFromJsonAsync<ValidationResponse>();
        Assert.NotNull(problema);
        Assert.Contains("Titulo", problema.Errors.Keys);
    }

    [Fact]
    public async Task CompletarInexistenteDevuelve404()
    {
        var response = await _client.PostAsync(
            $"/tareas/{Guid.NewGuid()}/completar",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EliminarDosVecesDevuelve204Y404()
    {
        var created = await _client.PostAsJsonAsync(
            "/tareas",
            new { titulo = "Efímera", prioridad = 1 });

        var tarea = await created.Content.ReadFromJsonAsync<TareaResponse>();
        Assert.NotNull(tarea);

        var primera = await _client.DeleteAsync($"/tareas/{tarea.Id}");
        var segunda = await _client.DeleteAsync($"/tareas/{tarea.Id}");

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, segunda.StatusCode);
    }

    private sealed record TareaResponse(Guid Id, string Titulo, bool Completada, int Prioridad);

    private sealed record ValidationResponse(Dictionary<string, string[]> Errors);
}
```

```text
Passed! - Failed: 0, Passed: 4, Skipped: 0
```

Si el estado global del servicio es compartido, cada test de integración debe usar una factory propia o limpiar el estado antes de correr. Con `IClassFixture` se comparte la app entre tests de la clase: evitá depender del orden.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Los tests pasan solos y fallan juntos | Estado compartido entre tests | Aislar datos o crear una factory por clase |
| `WebApplicationFactory` no encuentra `Program` | Falta `public partial class Program;` | Agregarlo al final de `Program.cs` |
| Un test unitario abre una base de datos | El servicio depende de infraestructura | Extraer una interfaz y usar un doble |
| Los tests de error solo verifican 400 | No se revisa el cuerpo | Comprobar `ProblemDetails` y los campos |
| La suite tarda minutos | Demasiada integración para lógica pura | Mover reglas a unitarios |
| Un test depende de otro | Orden implícito | Preparar el estado dentro de cada test |

## Buenas prácticas

- Un test, un comportamiento: el nombre debe describir qué se espera.
- Arrange, Act y Assert separados y visibles.
- Probá el camino feliz y al menos un error por endpoint.
- No uses `Thread.Sleep`: esperá la respuesta o usá utilidades deterministas.
- Corré la suite en cada pull request y arreglá los tests rotos de inmediato.
- Medí cobertura después de tener tests útiles, no antes.

## Ejercicio

Completá la suite con:

1. Un unitario que verifique que `Crear` recorta espacios del título.
2. Un unitario que confirme que `Eliminar` devuelve `false` para un id inexistente.
3. Un test de integración para `PUT /tareas/{id}` con prioridad fuera de rango: debe devolver `400`.
4. Un test de integración para `GET /tareas/{id}` inexistente: debe devolver `404`.

<details>
<summary>Ver solución</summary>

```csharp
[Fact]
public void CrearRecortaElTitulo()
{
    var service = new TareaService();

    var (_, tarea) = service.Crear(new TareaInput("  Con espacios  ", 3));

    Assert.Equal("Con espacios", tarea!.Titulo);
}

[Fact]
public void EliminarInexistenteDevuelveFalse()
{
    var service = new TareaService();

    Assert.False(service.Eliminar(Guid.NewGuid()));
}

[Fact]
public async Task PrioridadInvalidaDevuelve400()
{
    var created = await _client.PostAsJsonAsync(
        "/tareas",
        new { titulo = "Válida", prioridad = 1 });

    var tarea = await created.Content.ReadFromJsonAsync<TareaResponse>();

    var response = await _client.PutAsJsonAsync(
        $"/tareas/{tarea!.Id}",
        new { titulo = "Válida", prioridad = 9 });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task ObtenerInexistenteDevuelve404()
{
    var response = await _client.GetAsync($"/tareas/{Guid.NewGuid()}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

```text
Passed! - Failed: 0, Passed: 11, Skipped: 0
```

La suite cubre dominio, contrato HTTP, validación y errores. Esa base permite refactorizar sin miedo.

</details>

## Resumen

- Los unitarios prueban reglas puras y rápido.
- La integración verifica el contrato HTTP real.
- Cada test prepara su estado y no depende del orden.
- Probar los errores es parte del contrato, no un extra.

## Siguiente paso

Con la suite en verde, el próximo módulo trata sobre [código limpio, SOLID y patrones](/modulos/codigo-limpio): cómo mantener el código legible a medida que el proyecto crece.
