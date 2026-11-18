---
slug: webapplicationfactory
title: "WebApplicationFactory"
order: 6
estimatedMinutes: 15
summary: "Probá tu API en memoria con WebApplicationFactory, compartí el servidor de pruebas y reemplazá servicios reales por dobles."
quiz:
  - question: "¿Qué tipo se usa como argumento genérico de WebApplicationFactory en una Minimal API?"
    options:
      - "El nombre del proyecto de pruebas"
      - "La clase Program del proyecto web"
      - "HttpClient"
      - "WebApplicationBuilder"
    answer: 1
    explanation: "WebApplicationFactory<Program> necesita un punto de entrada conocido del proyecto web para arrancar la aplicación en memoria."
  - question: "¿Para qué sirve agregar `public partial class Program { }` al final de Program.cs?"
    options:
      - "Para que la clase Program sea accesible desde el ensamblado de pruebas"
      - "Para habilitar HTTPS"
      - "Para registrar más endpoints"
      - "Para compilar en modo Release"
    answer: 0
    explanation: "Las top-level statements generan una clase Program interna. La declaración parcial pública la expone para que los tests puedan referenciarla."
  - question: "¿Qué hace IClassFixture<WebApplicationFactory<Program>>?"
    options:
      - "Crea una base de datos por test"
      - "Comparte una instancia de la factory entre los tests de la clase"
      - "Ejecuta los tests en paralelo"
      - "Reemplaza automáticamente todos los servicios"
    answer: 1
    explanation: "El fixture se construye una vez por clase y se inyecta en el constructor, lo que evita levantar el servidor en cada test."
  - question: "¿Cómo se reemplaza un servicio registrado para un test de integración?"
    options:
      - "Editando Program.cs antes de cada test"
      - "Con WithWebHostBuilder y ConfigureServices, quitando el registro y agregando el doble"
      - "Cambiando la cadena de conexión en producción"
      - "Usando Assert.Equal sobre el contenedor"
    answer: 1
    explanation: "WithWebHostBuilder permite ajustar la configuración del host de pruebas; dentro de ConfigureServices se quita el servicio real y se registra el fake."
---

# WebApplicationFactory

## Objetivo

Probar una API ASP.NET Core completa sin abrir un puerto ni un proceso externo, compartir el servidor de pruebas entre tests y reemplazar servicios reales por dobles cuando haga falta.

## Requisitos previos

Necesitás saber crear una Minimal API con .NET 10, manejar inyección de dependencias y escribir tests de integración. Si querés repasar la diferencia entre unitario e integración, mirá [Unitarios vs. integración](/modulos/testing/unitarios-vs-integracion). También conviene tener fresco `HttpClient`.

## Concepto

`WebApplicationFactory<TEntryPoint>` arranca tu aplicación en memoria usando un servidor de pruebas. No abre un puerto TCP ni ejecuta `dotnet run`: crea el host, ejecuta `Program.cs`, configura el pipeline y te entrega un `HttpClient` que habla con esa aplicación.

El parámetro genérico es el punto de entrada, normalmente `Program`. Como las Minimal API usan top-level statements, el compilador genera una clase `Program` interna. Para que el proyecto de pruebas pueda verla, agregás al final de `Program.cs`:

```csharp
public partial class Program { }
```

La forma más simple de usarla es con un fixture compartido:

```csharp
public sealed class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
}
```

`IClassFixture<T>` le dice a xUnit que cree una sola instancia de `T` por clase de prueba y la inyecte en el constructor. Todos los tests de esa clase comparten la factory, pero cada uno sigue recibiendo una instancia nueva de la clase de prueba. El servidor se levanta una vez y se libera al terminar la clase.

`CreateClient()` devuelve un `HttpClient` configurado con una `BaseAddress` interna. Las peticiones no salen a la red: el servidor de pruebas las procesa directamente. Eso hace que los tests sean rápidos y que no dependan de puertos libres.

La factory también permite personalizar el host con `WithWebHostBuilder`. Dentro podés cambiar configuración, registrar servicios o modificar el pipeline. Es la herramienta habitual para reemplazar una base de datos real por un repositorio falso o por SQLite en memoria.

Un detalle importante: el contenedor de dependencias de la aplicación se construye al crear el cliente. Si registrás un servicio después de esa creación, el cambio no se aplica. Por eso los reemplazos se hacen dentro de `ConfigureServices`, antes de pedir el `HttpClient`.

## Ejemplo mínimo

Una API con un endpoint de saludo y un servicio inyectado.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ISaludoService, SaludoService>();

var app = builder.Build();

app.MapGet("/saludo", (ISaludoService servicio) =>
    Results.Ok(new SaludoResponse(servicio.Saludar("mundo"))));

app.Run();

public interface ISaludoService
{
    string Saludar(string nombre);
}

public sealed class SaludoService : ISaludoService
{
    public string Saludar(string nombre) => $"Hola, {nombre}";
}

public sealed record SaludoResponse(string Mensaje);

public partial class Program { }
```

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public sealed class SaludoApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetSaludo_DevuelveOkYElMensajeEsperado()
    {
        HttpResponseMessage respuesta = await _client.GetAsync("/saludo");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        SaludoResponse? cuerpo = await respuesta.Content.ReadFromJsonAsync<SaludoResponse>();
        Assert.NotNull(cuerpo);
        Assert.Equal("Hola, mundo", cuerpo.Mensaje);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 412 ms
```

El test recorre ruteo, inyección de dependencias, ejecución del handler y serialización JSON. Es mucho más que una prueba de la clase `SaludoService`, y por eso tarda más que un unitario.

## Ejemplo aplicado

Una API de productos expone `GET /productos`. En producción usa un repositorio real; en el test se reemplaza por un fake con dos productos.

```csharp
public sealed record Producto(int Id, string Nombre, decimal Precio);

public interface IProductoRepositorio
{
    IReadOnlyList<Producto> Todos();
}

public sealed class ProductoRepositorioEnMemoria : IProductoRepositorio
{
    private readonly List<Producto> _productos =
    [
        new(1, "Teclado", 25_000m),
        new(2, "Mouse", 12_000m)
    ];

    public IReadOnlyList<Producto> Todos() => _productos;
}
```

En `Program.cs`:

```csharp
builder.Services.AddSingleton<IProductoRepositorio, ProductoRepositorioEnMemoria>();
app.MapGet("/productos", (IProductoRepositorio repositorio) =>
    Results.Ok(repositorio.Todos()));
```

El test reemplaza el registro por un fake distinto para controlar los datos:

```csharp
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public sealed class ProductoFake : IProductoRepositorio
{
    public IReadOnlyList<Producto> Todos() =>
        [new Producto(99, "Monitor de prueba", 1m)];
}

public sealed class ProductosApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetProductos_ConRepositorioFalso_DevuelveSoloElProductoDelFake()
    {
        using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IProductoRepositorio>();
                services.AddSingleton<IProductoRepositorio, ProductoFake>();
            });
        });

        using HttpClient cliente = app.CreateClient();
        List<Producto>? productos = await cliente.GetFromJsonAsync<List<Producto>>("/productos");

        Assert.NotNull(productos);
        Producto unico = Assert.Single(productos);
        Assert.Equal("Monitor de prueba", unico.Nombre);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 389 ms
```

`RemoveAll<IProductoRepositorio>()` elimina todos los registros previos de ese tipo, no solo el último. Es importante porque un servicio puede estar registrado varias veces o mediante una extensión que agrega más de una implementación.

Si preferís no reemplazar servicios, podés usar una base SQLite en memoria y configurarla en `ConfigureServices`. La factory también acepta configuración adicional por `builder.UseSetting` o `builder.ConfigureAppConfiguration`.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `Program` no es accesible | Las top-level statements generan una clase interna | Agregar `public partial class Program { }` al final de `Program.cs` |
| El test usa la base de datos real | No se reemplazó el `DbContext` ni el repositorio | Quitar el registro y agregar SQLite en memoria o un fake |
| El reemplazo no tiene efecto | Se registró después de crear el cliente | Configurar dentro de `WithWebHostBuilder` antes de `CreateClient()` |
| Varios registros del mismo servicio | `RemoveAll` no se usó o el servicio se agregó con otra extensión | Usar `services.RemoveAll<T>()` y volver a registrar |
| El test falla por redirección HTTPS | El pipeline redirige y el cliente no sigue la redirección | Ajustar `AllowAutoRedirect` o probar la URL correcta |
| La factory se crea por test y todo va lento | El fixture no está configurado o se usa `using` en cada test | Implementar `IClassFixture<WebApplicationFactory<Program>>` |

## Buenas prácticas

- Compartí una factory por clase con `IClassFixture` y creá un cliente por test.
- Reemplazá solo lo que el test necesita controlar; no conviertas todo en un fake.
- Aislá los datos: cada test debe poder ejecutarse solo y en cualquier orden.
- Verificá status, cuerpo y headers relevantes, no la implementación interna del handler.
- Si el test necesita autenticación, configurá un esquema de prueba en lugar de saltarte el middleware.

## Ejercicio

Creá una Minimal API con `GET /tareas` que devuelva una lista de tareas desde `ITareaRepositorio`. Escribí un test con `WebApplicationFactory<Program>` que reemplace el repositorio por un fake con una sola tarea y verifique el status 200 y el contenido JSON. Después agregá un segundo test que compruebe que la aplicación real devuelve las tareas configuradas por defecto.

<details>
<summary>Ver solución</summary>

```csharp
public sealed record Tarea(int Id, string Titulo, bool Completada);

public interface ITareaRepositorio
{
    IReadOnlyList<Tarea> Todas();
}

public sealed class TareaRepositorio : ITareaRepositorio
{
    public IReadOnlyList<Tarea> Todas() =>
    [
        new(1, "Estudiar xUnit", true),
        new(2, "Escribir tests", false)
    ];
}
```

En `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITareaRepositorio, TareaRepositorio>();
var app = builder.Build();

app.MapGet("/tareas", (ITareaRepositorio repositorio) =>
    Results.Ok(repositorio.Todas()));

app.Run();

public partial class Program { }
```

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public sealed class TareaFake : ITareaRepositorio
{
    public IReadOnlyList<Tarea> Todas() => [new Tarea(10, "Tarea falsa", false)];
}

public sealed class TareasApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetTareas_ConFake_DevuelveLaTareaDelFake()
    {
        using var app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITareaRepositorio>();
                services.AddSingleton<ITareaRepositorio, TareaFake>();
            }));

        using HttpClient cliente = app.CreateClient();
        HttpResponseMessage respuesta = await cliente.GetAsync("/tareas");
        List<Tarea>? tareas = await respuesta.Content.ReadFromJsonAsync<List<Tarea>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Tarea tarea = Assert.Single(tareas!);
        Assert.Equal("Tarea falsa", tarea.Titulo);
        Assert.False(tarea.Completada);
    }

    [Fact]
    public async Task GetTareas_SinReemplazos_DevuelveLasDosTareasReales()
    {
        using HttpClient cliente = factory.CreateClient();

        List<Tarea>? tareas = await cliente.GetFromJsonAsync<List<Tarea>>("/tareas");

        Assert.Equal(2, tareas!.Count);
        Assert.Contains(tareas, t => t.Titulo == "Estudiar xUnit");
    }
}
```

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 438 ms
```

El segundo test no reemplaza nada y comprueba la configuración por defecto. Es útil para detectar si el registro real se rompió al modificar `Program.cs`.

</details>

## Resumen

- `WebApplicationFactory<Program>` levanta la API en memoria y entrega un `HttpClient`.
- `public partial class Program { }` expone el punto de entrada al proyecto de pruebas.
- `IClassFixture` comparte una factory por clase y reduce el tiempo de ejecución.
- `WithWebHostBuilder` y `ConfigureServices` permiten reemplazar servicios reales.
- `RemoveAll<T>()` evita registros duplicados al sustituir dependencias.

## Siguiente paso

Ya podés levantar la API y reemplazar servicios. En la próxima lección vas a profundizar en [pruebas de APIs](/modulos/testing/pruebas-apis): códigos de estado, cuerpos JSON, validación y autenticación.
