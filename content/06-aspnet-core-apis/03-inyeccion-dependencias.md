---
slug: inyeccion-dependencias
title: "Inyección de dependencias"
order: 3
estimatedMinutes: 15
summary: "Registrá servicios en el contenedor, elegí el ciclo de vida correcto y recibí dependencias directamente en los handlers de tu API."
quiz:
  - question: "¿Qué ciclo de vida crea una instancia nueva por cada petición HTTP?"
    options:
      - "Singleton"
      - "Scoped"
      - "Transient"
      - "Static"
    answer: 1
    explanation: "Scoped crea una instancia por scope, y en ASP.NET Core cada petición HTTP abre un scope propio."
  - question: "¿Dónde se registran los servicios que usará la aplicación?"
    options:
      - "En app.Run()"
      - "En builder.Services antes de builder.Build()"
      - "Dentro de cada MapGet"
      - "En appsettings.json"
    answer: 1
    explanation: "builder.Services es el IServiceCollection donde se declaran las dependencias antes de construir la aplicación."
  - question: "Un singleton depende de un servicio scoped. ¿Qué ocurre?"
    options:
      - "Funciona sin problemas"
      - "El contenedor crea un scope por petición"
      - "Se lanza una excepción de scope inválido en desarrollo"
      - "El scoped se vuelve singleton automáticamente"
    answer: 2
    explanation: "El singleton captura una única instancia scoped y rompe el ciclo de vida; el contenedor lanza InvalidOperationException al validar scopes."
---

# Inyección de dependencias

## Objetivo

Separar la lógica de negocio de los endpoints, registrar servicios en el contenedor y elegir entre `Singleton`, `Scoped` y `Transient` sin romper el ciclo de vida.

## Requisitos previos

Necesitás manejar interfaces e implementaciones, como se vio en el módulo de POO, y haber creado endpoints con parámetros en [Routing y parámetros](/modulos/aspnet-core-apis/routing-parametros).

## Concepto

La inyección de dependencias es un patrón donde un objeto no crea sus colaboradores: los recibe. En ASP.NET Core, el contenedor se configura en `builder.Services` y resuelve las dependencias en tiempo de ejecución.

Un handler declara lo que necesita como parámetro. Si el tipo está registrado, el contenedor lo entrega; si no, la petición falla. Esto permite reemplazar una implementación en memoria por una base de datos sin tocar los endpoints.

| Ciclo de vida | Instancia | Uso típico |
|---|---|---|
| `AddSingleton` | Una para toda la aplicación | Configuración, caché, repositorios en memoria |
| `AddScoped` | Una por petición HTTP | DbContext, servicios con estado por request |
| `AddTransient` | Una por cada resolución | Servicios livianos y sin estado |

La regla de oro es que un servicio de vida más larga no puede depender de uno más corto. Un singleton que pide un scoped captura esa instancia para siempre. Tampoco busques servicios con `app.Services.GetRequiredService<T>()` dentro de un handler: pedilos en la firma.

## Ejemplo mínimo

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.MapGet("/hora", (TimeProvider reloj) => new { Ahora = reloj.GetUtcNow() });
app.MapGet("/saludo/{nombre}", (string nombre, TimeProvider reloj) =>
    $"Hola, {nombre}. Son las {reloj.GetUtcNow():HH:mm} UTC.");

app.Run();
```

```bash
curl http://localhost:5137/hora
curl http://localhost:5137/saludo/Ana
```

```text
{"ahora":"2026-11-15T12:30:00+00:00"}
Hola, Ana. Son las 12:30 UTC.
```

`TimeProvider` es un servicio del propio framework: se registra una vez y ambos handlers lo reciben por parámetro. En las pruebas podés registrar un `TimeProvider` fijo con `FakeTimeProvider` o una subclase propia.

## Ejemplo aplicado

El repositorio guarda tareas y el servicio aplica las reglas. El endpoint solo coordina.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITareaRepositorio, TareaRepositorioEnMemoria>();
builder.Services.AddScoped<ServicioTareas>();

var app = builder.Build();

app.MapGet("/tareas", (ServicioTareas servicio) => servicio.Listar());

app.MapPost("/tareas", (TareaEntrada entrada, ServicioTareas servicio) =>
    servicio.Crear(entrada.Titulo) is Tarea tarea
        ? Results.Created($"/tareas/{tarea.Id}", tarea)
        : Results.BadRequest("El título es obligatorio."));

app.Run();

public sealed record Tarea(int Id, string Titulo, bool Completada);
public sealed record TareaEntrada(string Titulo);

public interface ITareaRepositorio
{
    IReadOnlyList<Tarea> Todas { get; }
    Tarea Agregar(string titulo);
}

public sealed class TareaRepositorioEnMemoria : ITareaRepositorio
{
    private readonly List<Tarea> _tareas = [];
    public IReadOnlyList<Tarea> Todas => _tareas;

    public Tarea Agregar(string titulo)
    {
        var tarea = new Tarea(_tareas.Count + 1, titulo, false);
        _tareas.Add(tarea);
        return tarea;
    }
}

public sealed class ServicioTareas(ITareaRepositorio repositorio)
{
    public IReadOnlyList<Tarea> Listar() => repositorio.Todas;
    public Tarea? Crear(string titulo) =>
        string.IsNullOrWhiteSpace(titulo) ? null : repositorio.Agregar(titulo.Trim());
}
```

```bash
curl -X POST http://localhost:5137/tareas \
  -H "Content-Type: application/json" \
  -d '{"titulo":"Estudiar DI"}'
```

```text
{"id":1,"titulo":"Estudiar DI","completada":false}
```

`ITareaRepositorio` es singleton porque el estado sobrevive entre peticiones. `ServicioTareas` es scoped porque representa una unidad de trabajo por request. Si mañana el repositorio usa EF Core, cambia a scoped y los endpoints no se enteran.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `Unable to resolve service for type 'IX'` | La interfaz no está registrada | Agregar `builder.Services.AddScoped<IX, X>()` |
| `Cannot consume scoped service from singleton` | Un singleton depende de un scoped | Cambiar el consumidor a scoped o inyectar `IServiceScopeFactory` |
| Los datos se pierden entre peticiones | El repositorio se registró como transient | Usar singleton si el estado es en memoria |
| Cambiar la implementación obliga a tocar endpoints | Se depende de la clase concreta | Depender de una interfaz y registrar la implementación |

## Buenas prácticas

- Programá contra interfaces y registrá la implementación concreta en un solo lugar.
- Dejá que el constructor reciba lo necesario; no resuelvas servicios dentro del cuerpo.
- No inyectes el contenedor (`IServiceProvider`) en clases de negocio.
- Validá la configuración de DI al arrancar: los errores de scope deben aparecer en desarrollo.

## Ejercicio

Creá `CatalogoService`, registrado como scoped, con `Buscar(string texto)`, que devuelva productos cuyo nombre contenga el texto. Debe depender de `IProductoRepositorio` (singleton, tres productos en memoria) y de `ILogger<CatalogoService>`. Exponé `GET /productos/buscar?texto=...`.

<details>
<summary>Ver solución</summary>

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IProductoRepositorio, ProductoRepositorio>();
builder.Services.AddScoped<CatalogoService>();

var app = builder.Build();

app.MapGet("/productos/buscar", (string texto, CatalogoService catalogo) =>
    string.IsNullOrWhiteSpace(texto)
        ? Results.BadRequest("Indicá un texto de búsqueda.")
        : Results.Ok(catalogo.Buscar(texto)));

app.Run();

public sealed record Producto(int Id, string Nombre, decimal Precio);
public interface IProductoRepositorio { IReadOnlyList<Producto> Todos { get; } }

public sealed class ProductoRepositorio : IProductoRepositorio
{
    public IReadOnlyList<Producto> Todos { get; } =
    [
        new(1, "Teclado mecánico", 85000m),
        new(2, "Mouse inalámbrico", 42000m),
        new(3, "Monitor 27 pulgadas", 310000m)
    ];
}

public sealed class CatalogoService(IProductoRepositorio repositorio, ILogger<CatalogoService> logger)
{
    public IReadOnlyList<Producto> Buscar(string texto)
    {
        logger.LogInformation("Buscando productos con {Texto}", texto);
        return repositorio.Todos
            .Where(p => p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
```

```bash
curl "http://localhost:5137/productos/buscar?texto=mouse"
```

```text
[{"id":2,"nombre":"Mouse inalámbrico","precio":42000}]
```

`ILogger<T>` ya viene registrado por el host. El servicio es scoped y el repositorio singleton: cada petición obtiene su propio `CatalogoService`, pero comparte los productos.

</details>

## Resumen

- El contenedor se configura en `builder.Services` antes de `Build()`.
- Los handlers reciben servicios como parámetros, sin crear instancias ni usar service locator.
- `Singleton` dura toda la app, `Scoped` una petición y `Transient` cada resolución.
- Un servicio de vida larga no debe depender de uno de vida corta.

## Siguiente paso

Ya separaste la lógica de los endpoints. Ahora vas a ver qué ocurre entre la petición y el handler en [Middleware y pipeline](/modulos/aspnet-core-apis/middleware).
