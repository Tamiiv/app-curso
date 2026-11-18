---
slug: routing-parametros
title: "Routing y parámetros"
order: 2
estimatedMinutes: 15
summary: "Armá rutas claras con parámetros, restricciones y query strings, y agrupá endpoints emparentados con MapGroup."
quiz:
  - question: "¿Qué hace la restricción {id:int} en una plantilla de ruta?"
    options:
      - "Convierte el parámetro a string"
      - "Solo acepta la ruta si el segmento es un entero válido"
      - "Hace que el parámetro sea opcional"
      - "Responde 500 si el valor no es numérico"
    answer: 1
    explanation: "Las restricciones filtran rutas: si el segmento no se puede interpretar como int, la plantilla no coincide y la API responde 404."
  - question: "En GET /productos?categoria=libros&pagina=2, ¿cómo llegan categoria y pagina al handler?"
    options:
      - "Como parámetros de ruta"
      - "Como parte del cuerpo JSON"
      - "Como parámetros del handler enlazados desde el query string"
      - "Solo mediante HttpContext.Items"
    answer: 2
    explanation: "Los tipos simples que no figuran en la plantilla se enlazan desde el query string por nombre."
  - question: "¿Para qué sirve app.MapGroup(\"/api/tareas\")?"
    options:
      - "Para crear una base de datos"
      - "Para agrupar endpoints bajo un prefijo y metadatos comunes"
      - "Para habilitar HTTPS"
      - "Para registrar servicios en el contenedor DI"
    answer: 1
    explanation: "MapGroup devuelve un RouteGroupBuilder que comparte prefijo, filtros, tags y convenciones entre los endpoints del grupo."
---

# Routing y parámetros

## Objetivo

Diseñar plantillas de ruta con parámetros obligatorios, opcionales y restringidos, leer datos del query string y agrupar endpoints relacionados sin repetir prefijos.

## Requisitos previos

Necesitás haber creado una API con `MapGet` y `MapPost`, como se muestra en [Minimal APIs: tu primera API](/modulos/aspnet-core-apis/minimal-apis). También ayuda entender tipos anulables y valores por defecto.

## Concepto

El routing compara la URL entrante con las plantillas registradas. Una plantilla es un texto con segmentos fijos y segmentos entre llaves: `/tareas/{id}`. Cuando hay coincidencia, ASP.NET Core arma los argumentos del handler.

Cada parámetro del handler se completa según su origen. El orden de prioridad es: ruta, query string y cuerpo. Los tipos simples (`int`, `string`, `bool`, `Guid`, `DateTime`) salen de la ruta o del query; los tipos complejos salen del cuerpo JSON.

| Restricción | Acepta |
|---|---|
| `{id:int}` | Enteros |
| `{id:guid}` | GUID con formato válido |
| `{id:min(1)}` | Valores mayores o iguales a 1 |
| `{anio:range(2000,2100)}` | Valores dentro del rango |
| `{slug:alpha}` | Solo letras |
| `{codigo:length(3)}` | Exactamente 3 caracteres |
| `{nombre:regex(^[a-z]+$)}` | Coincidencia con la expresión regular |

Las restricciones se pueden encadenar: `{anio:int:range(2000,2100)}`. Si el valor no cumple, la ruta no coincide y el resultado es 404, no un error de conversión.

Un parámetro opcional se marca con `?` y debe ser anulable o tener valor por defecto: `{pagina:int?}`. Un catch-all se marca con `*` y captura el resto de la URL, barras incluidas: `{*ruta}`.

`MapGroup` agrupa endpoints bajo un prefijo común y permite aplicar metadatos, filtros y convenciones una sola vez. El grupo no crea un endpoint por sí mismo: es un builder sobre el que se siguen llamando `MapGet` y `MapPost`.

## Ejemplo mínimo

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/productos/{id:int}", (int id) =>
    $"Producto número {id}");

app.MapGet("/productos/{id:int}/precio", (int id, decimal? descuento) =>
{
    decimal basePrice = 1000m;
    decimal final = descuento is null ? basePrice : basePrice * (1 - descuento.Value);
    return new { Id = id, Precio = final };
});

app.MapGet("/archivos/{*ruta}", (string ruta) =>
    $"Pediste el archivo: {ruta}");

app.Run();
```

```bash
curl http://localhost:5137/productos/7
curl "http://localhost:5137/productos/7/precio?descuento=0.1"
curl http://localhost:5137/archivos/2026/informes/enero.pdf
```

```text
Producto número 7
{"id":7,"precio":900.0}
Pediste el archivo: 2026/informes/enero.pdf
```

`descuento` no está en la plantilla, así que se toma del query string. Al ser `decimal?`, si no se envía queda en `null` y el handler usa el precio base.

## Ejemplo aplicado

Un grupo concentra todo lo relacionado con pedidos. El prefijo se declara una vez y cada endpoint agrega su parte.

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var pedidos = new List<Pedido>
{
    new(1, "Ana Torres", 25000m, EstadoPedido.Pendiente),
    new(2, "Bruno Díaz", 8400m, EstadoPedido.Enviado)
};

var grupo = app.MapGroup("/api/pedidos");

grupo.MapGet("/", (EstadoPedido? estado) =>
    estado is null
        ? pedidos
        : pedidos.Where(p => p.Estado == estado).ToList());

grupo.MapGet("/{id:int:min(1)}", (int id) =>
    pedidos.FirstOrDefault(p => p.Id == id) is Pedido pedido
        ? Results.Ok(pedido)
        : Results.NotFound());

app.Run();

enum EstadoPedido { Pendiente, Enviado, Entregado }
record Pedido(int Id, string Cliente, decimal Monto, EstadoPedido Estado);
```

```bash
curl http://localhost:5137/api/pedidos
curl "http://localhost:5137/api/pedidos?estado=Enviado"
```

```text
[{"id":1,"cliente":"Ana Torres","monto":25000,"estado":0},{"id":2,"cliente":"Bruno Díaz","monto":8400,"estado":1}]
[{"id":2,"cliente":"Bruno Díaz","monto":8400,"estado":1}]
```

El prefijo `/api/pedidos` vive en una sola línea. El filtro por estado llega desde el query string y la búsqueda por id usa una restricción numérica.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La ruta válida responde 404 | La restricción no admite el valor enviado | Revisar el tipo y el rango de la restricción |
| `{id?}` no compila | El parámetro no es anulable | Declararlo como `int? id` o darle valor por defecto |
| El handler recibe `null` en un parámetro obligatorio | El nombre del parámetro no coincide con la plantilla ni el query | Igualar nombres o usar `[FromQuery]` explícito |
| Dos rutas entran en conflicto | Plantillas ambiguas para la misma URL | Hacerlas más específicas con restricciones |
| El catch-all devuelve solo el primer segmento | Se usó `{ruta}` en lugar de `{*ruta}` | Agregar el asterisco para capturar el resto |
| El grupo duplica el prefijo | Se repite `/api/pedidos` dentro del `MapGet` | Dentro del grupo usar rutas relativas |

## Buenas prácticas

- Usá restricciones para descartar temprano valores inválidos y evitar validaciones manuales de formato.
- Mantené los nombres de plantilla iguales a los parámetros del handler; el enlace implícito es más claro.
- Agrupá por recurso con `MapGroup` y reservá el prefijo de versión para el nivel superior.
- Documentá los parámetros opcionales con valores por defecto explícitos en la firma.
- Evitá catch-all salvo para proxies o archivos; suelen esconder rutas mal diseñadas.

## Ejercicio

Creá un grupo `/api/libros` con tres endpoints: listar todos, filtrar por `genero` desde el query string y obtener uno por `{id:int:min(1)}`. Si el id no existe, respondé 404. Agregá además `/api/libros/{id:int}/paginas/{numero:int}` que valide que el número de página sea positivo con una restricción.

<details>
<summary>Ver solución</summary>

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var libros = new List<Libro>
{
    new(1, "Rayuela", "Novela", 736),
    new(2, "Ficciones", "Cuento", 174),
    new(3, "El túnel", "Novela", 160)
};

var grupo = app.MapGroup("/api/libros");

grupo.MapGet("/", (string? genero) =>
    genero is null
        ? libros
        : libros.Where(l => l.Genero.Equals(genero, StringComparison.OrdinalIgnoreCase)).ToList());

grupo.MapGet("/{id:int:min(1)}", (int id) =>
    libros.FirstOrDefault(l => l.Id == id) is Libro libro
        ? Results.Ok(libro)
        : Results.NotFound());

grupo.MapGet("/{id:int}/paginas/{numero:int:min(1)}", (int id, int numero) =>
    libros.FirstOrDefault(l => l.Id == id) is Libro libro
        ? numero > libro.Paginas
            ? Results.BadRequest($"El libro tiene {libro.Paginas} páginas.")
            : Results.Ok(new { libro.Titulo, Pagina = numero })
        : Results.NotFound());

app.Run();

record Libro(int Id, string Titulo, string Genero, int Paginas);
```

```bash
curl "http://localhost:5137/api/libros?genero=novela"
curl http://localhost:5137/api/libros/2/paginas/10
curl -i http://localhost:5137/api/libros/2/paginas/500
```

```text
[{"id":1,"titulo":"Rayuela","genero":"Novela","paginas":736},{"id":3,"titulo":"El túnel","genero":"Novela","paginas":160}]
{"titulo":"Ficciones","pagina":10}
HTTP/1.1 400 Bad Request
```

`min(1)` descarta números de página no positivos antes de entrar al handler; el límite contra la cantidad real de páginas se valida adentro porque depende del libro.

</details>

## Resumen

- Las plantillas combinan segmentos fijos con parámetros entre llaves.
- Las restricciones filtran rutas y responden 404 cuando el valor no cumple.
- Los tipos simples se enlazan desde la ruta o el query string; los complejos, desde el cuerpo.
- `MapGroup` comparte prefijo y metadatos entre endpoints emparentados.

## Siguiente paso

Con las rutas resueltas, el próximo problema es dónde vive la lógica. En [Inyección de dependencias](/modulos/aspnet-core-apis/inyeccion-dependencias) vas a conectar servicios con los handlers.
