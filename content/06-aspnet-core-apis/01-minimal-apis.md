---
slug: minimal-apis
title: "Minimal APIs: tu primera API"
order: 1
estimatedMinutes: 15
summary: "Creá tu primera API web con Minimal APIs: un proyecto, un archivo Program.cs y endpoints que responden JSON sin ceremonias."
quiz:
  - question: "¿Qué objeto crea y configura la aplicación web en un proyecto de Minimal APIs?"
    options:
      - "new HttpClient()"
      - "WebApplication.CreateBuilder(args)"
      - "new Startup()"
      - "Host.CreateDefaultBuilder()"
    answer: 1
    explanation: "CreateBuilder devuelve un WebApplicationBuilder donde se registran servicios y se construye la app con Build()."
  - question: "¿Qué método registra un endpoint que responde a GET /tareas?"
    options:
      - "app.MapPost(\"/tareas\", ...)"
      - "app.Use(\"/tareas\", ...)"
      - "app.MapGet(\"/tareas\", ...)"
      - "app.AddRoute(\"/tareas\", ...)"
    answer: 2
    explanation: "MapGet asocia una plantilla de ruta con un delegate que se ejecuta cuando llega un GET a esa ruta."
  - question: "¿Qué devuelve un handler que retorna un objeto record en un MapGet?"
    options:
      - "Un archivo de texto plano"
      - "HTML generado automáticamente"
      - "JSON serializado con el objeto"
      - "Una redirección 302"
    answer: 2
    explanation: "Minimal APIs serializa automáticamente a JSON los objetos devueltos y responde con estado 200 si no se indica otra cosa."
  - question: "¿Para qué sirve app.Run() al final de Program.cs?"
    options:
      - "Compila el proyecto antes de ejecutarlo"
      - "Inicia el servidor y queda escuchando peticiones"
      - "Ejecuta una sola petición de prueba"
      - "Cierra la aplicación después de configurar rutas"
    answer: 1
    explanation: "Run arranca el servidor Kestrel y bloquea la ejecución atendiendo peticiones hasta que la aplicación se detiene."
---

# Minimal APIs: tu primera API

## Objetivo

Crear un proyecto web con .NET 10, entender el archivo `Program.cs` y publicar tus primeros endpoints que devuelven JSON sin usar controladores ni configuración extra.

## Requisitos previos

Necesitás el SDK de .NET 10 instalado y saber ejecutar `dotnet` desde la terminal. También conviene que tengas fresco el manejo de errores y tareas asincrónicas de la lección [Ejercicio integrador de async y errores](/modulos/async-errores/ejercicio-integrador), porque una API es, en el fondo, código que responde a eventos externos.

## Concepto

Una Minimal API es una aplicación ASP.NET Core donde los endpoints se declaran directamente en `Program.cs` con `MapGet`, `MapPost` y compañía. No hay clases `Controller`, no hay atributos de ruteo y no hay carpeta `Controllers`. Para una API chica, eso significa menos archivos y menos indirección.

Todo proyecto web moderno tiene tres piezas:

| Pieza | Rol |
|---|---|
| `WebApplicationBuilder` | Registra servicios, configuración y logging |
| `WebApplication` | Arma el pipeline y declara los endpoints |
| Kestrel | Servidor HTTP que atiende las peticiones |

El flujo es siempre el mismo: creás el builder, registrás lo que necesitás, construís la app, mapeás rutas y llamás a `app.Run()`. Lo que está antes de `Run` es configuración; lo que ejecuta un handler ocurre en cada petición.

Un handler puede ser una función lambda o un método local. Recibe parámetros que ASP.NET Core completa por vos: parámetros de ruta, query string, cuerpo JSON, servicios inyectados o `HttpContext`. Si devolvés un objeto, se serializa a JSON con estado 200. Si devolvés un `IResult`, vos decidís el estado y el contenido.

## Ejemplo mínimo

Creá el proyecto y ejecutalo:

```bash
dotnet new web -n MiPrimeraApi
cd MiPrimeraApi
dotnet run
```

El template genera un `Program.cs` muy corto. Reemplazalo por esto:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "API de ejemplo funcionando");

app.MapGet("/saludo/{nombre}", (string nombre) =>
    $"Hola, {nombre}");

app.Run();
```

La consola muestra la URL donde escucha Kestrel, por ejemplo `http://localhost:5137`. Probá desde otra terminal:

```bash
curl http://localhost:5137/
curl http://localhost:5137/saludo/Ana
```

```text
API de ejemplo funcionando
Hola, Ana
```

Los textos planos se devuelven como `text/plain`. El parámetro `{nombre}` de la ruta llega al handler como `string` porque no tiene restricciones: cualquier segmento es válido.

## Ejemplo aplicado

Una lista de tareas en memoria alcanza para ver el ciclo completo: leer, buscar por id y crear.

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var tareas = new List<Tarea>
{
    new(1, "Preparar la clase", false),
    new(2, "Corregir ejercicios", true)
};

app.MapGet("/tareas", () => tareas);

app.MapGet("/tareas/{id:int}", (int id) =>
    tareas.FirstOrDefault(t => t.Id == id) is Tarea tarea
        ? Results.Ok(tarea)
        : Results.NotFound());

app.MapPost("/tareas", (TareaEntrada entrada) =>
{
    int id = tareas.Count == 0 ? 1 : tareas.Max(t => t.Id) + 1;
    var tarea = new Tarea(id, entrada.Titulo, false);
    tareas.Add(tarea);

    return Results.Created($"/tareas/{tarea.Id}", tarea);
});

app.Run();

record Tarea(int Id, string Titulo, bool Completada);
record TareaEntrada(string Titulo);
```

```bash
curl http://localhost:5137/tareas
curl -X POST http://localhost:5137/tareas \
  -H "Content-Type: application/json" \
  -d '{"titulo":"Escribir el informe"}'
```

```text
[{"id":1,"titulo":"Preparar la clase","completada":false},{"id":2,"titulo":"Corregir ejercicios","completada":true}]
{"id":3,"titulo":"Escribir el informe","completada":false}
```

El cuerpo JSON se convierte a `TareaEntrada` automáticamente porque es un tipo complejo. `Results.Created` responde 201 y agrega la cabecera `Location` con la URL del recurso nuevo.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `Unable to resolve service for type ...` | El handler pide un servicio que no está registrado | Registrarlo con `builder.Services.Add...` antes de `Build()` |
| La API responde 404 en todas las rutas | `app.Run()` está antes de los `MapGet` | Mover `app.Run()` al final del archivo |
| El POST devuelve 415 | Falta `Content-Type: application/json` | Enviar la cabecera o configurar bien el cliente |
| El POST devuelve 400 con cuerpo vacío | El JSON no coincide con el record esperado | Revisar nombres y tipos de las propiedades |
| Cambios en el código no aparecen | El proceso sigue corriendo con la versión anterior | Detener con Ctrl+C y volver a `dotnet run` |
| `Program` no compila por tipos al final | Los tipos se declararon antes del código de nivel superior | Declararlos después de `app.Run()` |

## Buenas prácticas

- Mantené `Program.cs` legible: si supera las 150 líneas, separá los endpoints en métodos de extensión.
- Devolvé `IResult` cuando el estado HTTP importe; un objeto suelto siempre responde 200.
- Nombrá las rutas en plural y con sustantivos: `/tareas`, `/clientes`, `/pedidos`.
- No guardes estado en variables estáticas pensando en producción: el ejemplo en memoria sirve solo para aprender.
- Verificá cada endpoint con `curl` o un cliente HTTP antes de seguir agregando funcionalidad.

## Ejercicio

Creá una API con dos endpoints: `GET /api/peliculas` que devuelva una lista de tres películas y `GET /api/peliculas/{id:int}` que devuelva una película o 404 si no existe. Cada película debe tener `Id`, `Titulo` y `Anio`.

<details>
<summary>Ver solución</summary>

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var peliculas = new List<Pelicula>
{
    new(1, "El secreto de sus ojos", 2009),
    new(2, "Relatos salvajes", 2014),
    new(3, "Argentina, 1985", 2022)
};

app.MapGet("/api/peliculas", () => peliculas);

app.MapGet("/api/peliculas/{id:int}", (int id) =>
    peliculas.FirstOrDefault(p => p.Id == id) is Pelicula pelicula
        ? Results.Ok(pelicula)
        : Results.NotFound());

app.Run();

record Pelicula(int Id, string Titulo, int Anio);
```

```bash
curl http://localhost:5137/api/peliculas/2
curl -i http://localhost:5137/api/peliculas/9
```

```text
{"id":2,"titulo":"Relatos salvajes","anio":2014}

HTTP/1.1 404 Not Found
```

El patrón `is Pelicula pelicula` evita dos búsquedas y deja el handler en una sola expresión. Con `-i`, `curl` muestra el estado HTTP además del cuerpo.

</details>

## Resumen

- Una Minimal API declara endpoints directamente en `Program.cs` con `MapGet`, `MapPost` y similares.
- `WebApplication.CreateBuilder` configura la app y `app.Run()` inicia Kestrel.
- Los objetos devueltos se serializan a JSON; `IResult` permite controlar el estado.
- Los parámetros de ruta y el cuerpo JSON se enlazan automáticamente al handler.

## Siguiente paso

Ya tenés una API que responde. Ahora vas a aprender a construir rutas con parámetros, restricciones y grupos en [Routing y parámetros](/modulos/aspnet-core-apis/routing-parametros).
