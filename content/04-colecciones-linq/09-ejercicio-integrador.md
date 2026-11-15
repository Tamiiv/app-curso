---
slug: ejercicio-integrador
title: "Ejercicio integrador y quiz"
order: 9
estimatedMinutes: 25
summary: "Combiná colecciones, LINQ, ejecución diferida e inmutabilidad en un reporte completo de clientes, productos y pedidos."
quiz:
  - question: "¿Qué colección conviene para obtener las ciudades únicas de una lista de clientes?"
    options:
      - "List<string>"
      - "HashSet<string>"
      - "Dictionary<string, string>"
      - "string[]"
    answer: 1
    explanation: "HashSet<string> elimina duplicados y responde rápido a la pregunta de pertenencia."
  - question: "¿Cuándo se ejecuta una consulta LINQ guardada en una variable?"
    options:
      - "Al asignarla"
      - "Al salir del método"
      - "Al enumerarla o materializarla"
      - "Al crear el objeto que la contiene"
    answer: 2
    explanation: "La consulta es una descripción perezosa. Se ejecuta cuando se recorre o cuando se llama a un operador que materializa, como ToList o Count."
  - question: "¿Qué operador permite combinar pedidos con clientes usando el id del cliente?"
    options:
      - "GroupBy"
      - "Select"
      - "Join"
      - "Aggregate"
    answer: 2
    explanation: "Join recibe dos secuencias, una clave de cada lado y una función que arma el resultado con las parejas coincidentes."
  - question: "Si se crea un ImmutableArray<Pedido> y después se agrega un pedido a la lista original, ¿qué contiene el snapshot?"
    options:
      - "El pedido nuevo también"
      - "Los pedidos que existían al crear el snapshot"
      - "Una lista vacía"
      - "Solo los pedidos entregados"
    answer: 1
    explanation: "El snapshot es una copia inmutable: conserva el estado del momento en que se creó y no refleja cambios posteriores en la lista original."
---

# Ejercicio integrador y quiz

## Objetivo

Integrar todo el módulo en un programa que transforma y consulta pedidos con LINQ, explica la ejecución diferida y justifica qué colección conviene en cada parte del problema.

## Requisitos previos

Necesitás las ocho lecciones anteriores: `List`, `Dictionary` y `HashSet`; `IEnumerable` e iteradores; genéricos; lambdas; LINQ básico y avanzado; ejecución diferida; y colecciones inmutables. Si alguna te quedó floja, es buen momento para repasarla.

## Concepto

La tienda Ruta .NET vende productos a clientes y registra pedidos con varias líneas. Cada pedido guarda el id del cliente, no el objeto completo. Esa decisión evita duplicar datos y hace que el `Join` sea la herramienta natural para obtener el nombre.

Antes de escribir consultas, conviene decidir las colecciones:

| Necesidad | Elección | Motivo |
|---|---|---|
| Catálogo de productos | `List<Producto>` | Importa el orden y se recorre completo |
| Buscar un cliente por id | `Dictionary<int, Cliente>` | Acceso rápido por clave |
| Ciudades únicas | `HashSet<string>` | Evita duplicados |
| Productos que se vendieron | `HashSet<int>` | Pertenencia rápida por id |
| Foto de los pedidos | `ImmutableArray<Pedido>` | No cambia aunque el origen siga vivo |
| Resultado que devuelve una API | `IReadOnlyList<T>` | No expone operaciones de escritura |

El flujo del reporte es siempre el mismo: filtrar lo que interesa, combinar datos, agrupar, proyectar y materializar una vez. La consulta se arma con operadores perezosos; el `ToList` o el `Aggregate` marca el momento en que se ejecuta.

## Ejemplo mínimo

```csharp
var pedidos = new List<Pedido>
{
    new(100, EstadoPedido.Entregado, 108_000m),
    new(101, EstadoPedido.Pendiente, 320_000m),
    new(102, EstadoPedido.Entregado, 18_000m)
};

var consulta = pedidos.Where(pedido => pedido.Estado == EstadoPedido.Entregado);

decimal total = consulta.Aggregate(0m, (acumulado, pedido) => acumulado + pedido.Total);
Console.WriteLine($"Entregados: {consulta.Count()}, total: {total}");

public enum EstadoPedido { Pendiente, Enviado, Entregado }
public sealed record Pedido(int Id, EstadoPedido Estado, decimal Total);
```

```text
Entregados: 2, total: 126000
```

`Where` no ejecutó nada al definirse. `Aggregate` y `Count` recorrieron la consulta y produjeron los valores. Si la consulta fuera costosa, convendría materializarla una vez y reutilizar la lista.

## Ejemplo aplicado

Este programa arma dos reportes: pedidos entregados con nombre de cliente y ventas por categoría, sin contar los pedidos pendientes.

```csharp
using System.Globalization;

var clientes = new List<Cliente>
{
    new(1, "Ana Torres", "Rosario"),
    new(2, "Bruno Díaz", "Córdoba"),
    new(3, "Carla Ruiz", "Rosario")
};

var productos = new List<Producto>
{
    new(10, "Teclado", "Periféricos", 45_000m),
    new(11, "Mouse", "Periféricos", 18_000m),
    new(12, "Monitor", "Pantallas", 320_000m),
    new(13, "Notebook", "Computación", 1_250_000m),
    new(14, "Webcam", "Periféricos", 75_000m),
    new(15, "Auriculares", "Audio", 60_000m)
};

var pedidos = new List<Pedido>
{
    new(100, 1, new DateOnly(2026, 11, 2), EstadoPedido.Entregado,
        [new(productos[0], 2), new(productos[1], 1)]),
    new(101, 2, new DateOnly(2026, 11, 3), EstadoPedido.Entregado,
        [new(productos[2], 1)]),
    new(102, 1, new DateOnly(2026, 11, 4), EstadoPedido.Pendiente,
        [new(productos[3], 1)]),
    new(103, 3, new DateOnly(2026, 11, 5), EstadoPedido.Entregado,
        [new(productos[4], 2), new(productos[1], 1)]),
    new(104, 3, new DateOnly(2026, 11, 6), EstadoPedido.Enviado,
        [new(productos[2], 1), new(productos[0], 1)])
};

var consultaEntregados = pedidos
    .Where(pedido => pedido.Estado == EstadoPedido.Entregado)
    .Join(
        clientes,
        pedido => pedido.ClienteId,
        cliente => cliente.Id,
        (pedido, cliente) => new
        {
            pedido.Id,
            Cliente = cliente.Nombre,
            pedido.Total
        })
    .OrderByDescending(fila => fila.Total);

Console.WriteLine("Antes de materializar");
var entregados = consultaEntregados.ToList();

Console.WriteLine($"Entregados: {entregados.Count}");
foreach (var fila in entregados)
{
    Console.WriteLine($"{fila.Id} - {fila.Cliente} - {Formato(fila.Total)}");
}

var porCategoria = pedidos
    .Where(pedido => pedido.Estado != EstadoPedido.Pendiente)
    .SelectMany(pedido => pedido.Lineas)
    .GroupBy(linea => linea.Producto.Categoria)
    .Select(grupo => new
    {
        Categoria = grupo.Key,
        Unidades = grupo.Sum(linea => linea.Cantidad),
        Total = grupo.Sum(linea => linea.Subtotal)
    })
    .OrderByDescending(grupo => grupo.Total)
    .ToList();

Console.WriteLine("\nVentas por categoría (sin pendientes):");
foreach (var grupo in porCategoria)
{
    Console.WriteLine($"{grupo.Categoria}: {grupo.Unidades} unidades - {Formato(grupo.Total)}");
}

static string Formato(decimal monto) =>
    monto.ToString("N0", CultureInfo.InvariantCulture);

public enum EstadoPedido { Pendiente, Enviado, Entregado }
public sealed record Cliente(int Id, string Nombre, string Ciudad);
public sealed record Producto(int Id, string Nombre, string Categoria, decimal Precio);
public sealed record LineaPedido(Producto Producto, int Cantidad)
{
    public decimal Subtotal => Producto.Precio * Cantidad;
}
public sealed record Pedido(
    int Id,
    int ClienteId,
    DateOnly Fecha,
    EstadoPedido Estado,
    IReadOnlyList<LineaPedido> Lineas)
{
    public decimal Total => Lineas.Sum(linea => linea.Subtotal);
}
```

```text
Antes de materializar
Entregados: 3
101 - Bruno Díaz - 320,000
103 - Carla Ruiz - 168,000
100 - Ana Torres - 108,000

Ventas por categoría (sin pendientes):
Pantallas: 2 unidades - 640,000
Periféricos: 7 unidades - 321,000
```

`consultaEntregados` no ejecutó el filtro al asignarse. La frase "Antes de materializar" se imprime primero y el `ToList` dispara el trabajo. `porCategoria` también se materializa una sola vez para no recalcular los grupos al recorrerlos.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El reporte incluye el pedido agregado después | La consulta quedó perezosa y se ejecutó más tarde | Materializar antes de modificar el origen |
| Un cliente sin pedidos no aparece | `Join` es una unión interna | Usar `GroupJoin` con `DefaultIfEmpty` o aceptar la regla del reporte |
| Los productos sin ventas se buscan con `List.Contains` | Cada búsqueda recorre la lista | Construir un `HashSet<int>` con los ids vendidos |
| El snapshot cambia junto con la lista | Se guardó la referencia, no una copia | Crear un `ImmutableArray<T>` o copiar con `[.. lista]` |
| Los totales se recalculan en cada `foreach` | Se recorrió una consulta sin materializar | Guardar el resultado con `ToList` |
| `FirstOrDefault` devuelve `null` y se accede a una propiedad | La secuencia podía estar vacía | Comprobar el resultado o usar `?.` |
| Se usa `double` para dinero | Pérdida de precisión | Usar `decimal` en precios y totales |

## Buenas prácticas

- Modelá las relaciones con ids y resolvelas con `Join` o un diccionario, sin duplicar objetos.
- Armá la consulta en pasos con nombres claros: filtrar, unir, agrupar, proyectar y ordenar.
- Materializá en el límite del método y devolvé `IReadOnlyList<T>` si el resultado ya está calculado.
- Usá `HashSet<T>` para pertenencia y unicidad; no lo reemplaces con búsquedas lineales.
- Guardá snapshots inmutables cuando necesites comparar el estado anterior con el actual.

## Ejercicio

Completá el programa anterior con estos reportes:

1. Total comprado por cliente, con cantidad de pedidos, usando `Join` y `GroupBy`.
2. Producto más vendido por unidades, usando `GroupBy` y `FirstOrDefault`.
3. Total facturado de pedidos entregados, usando `Aggregate`.
4. Ciudades únicas y productos sin ventas, usando `HashSet`.
5. Un `ImmutableArray<Pedido>` creado antes de agregar un pedido nuevo, y un comentario que explique por qué el snapshot no incluye ese pedido y cuándo se ejecuta cada consulta.

<details>
<summary>Ver solución</summary>

```csharp
using System.Collections.Immutable;
using System.Globalization;

var clientes = new List<Cliente>
{
    new(1, "Ana Torres", "Rosario"),
    new(2, "Bruno Díaz", "Córdoba"),
    new(3, "Carla Ruiz", "Rosario")
};

var productos = new List<Producto>
{
    new(10, "Teclado", "Periféricos", 45_000m),
    new(11, "Mouse", "Periféricos", 18_000m),
    new(12, "Monitor", "Pantallas", 320_000m),
    new(13, "Notebook", "Computación", 1_250_000m),
    new(14, "Webcam", "Periféricos", 75_000m),
    new(15, "Auriculares", "Audio", 60_000m)
};

var pedidos = new List<Pedido>
{
    new(100, 1, new DateOnly(2026, 11, 2), EstadoPedido.Entregado,
        [new(productos[0], 2), new(productos[1], 1)]),
    new(101, 2, new DateOnly(2026, 11, 3), EstadoPedido.Entregado,
        [new(productos[2], 1)]),
    new(102, 1, new DateOnly(2026, 11, 4), EstadoPedido.Pendiente,
        [new(productos[3], 1)]),
    new(103, 3, new DateOnly(2026, 11, 5), EstadoPedido.Entregado,
        [new(productos[4], 2), new(productos[1], 1)]),
    new(104, 3, new DateOnly(2026, 11, 6), EstadoPedido.Enviado,
        [new(productos[2], 1), new(productos[0], 1)])
};

// La consulta es perezosa: se ejecuta en el ToList.
var consultaEntregados = pedidos
    .Where(pedido => pedido.Estado == EstadoPedido.Entregado)
    .Join(
        clientes,
        pedido => pedido.ClienteId,
        cliente => cliente.Id,
        (pedido, cliente) => new
        {
            pedido.Id,
            Cliente = cliente.Nombre,
            pedido.Total
        })
    .OrderByDescending(fila => fila.Total);

Console.WriteLine("Antes de materializar");
var entregados = consultaEntregados.ToList();

Console.WriteLine($"Entregados: {entregados.Count}");
foreach (var fila in entregados)
{
    Console.WriteLine($"{fila.Id} - {fila.Cliente} - {Formato(fila.Total)}");
}

// También se materializa una vez para no recalcular los grupos.
var porCategoria = pedidos
    .Where(pedido => pedido.Estado != EstadoPedido.Pendiente)
    .SelectMany(pedido => pedido.Lineas)
    .GroupBy(linea => linea.Producto.Categoria)
    .Select(grupo => new
    {
        Categoria = grupo.Key,
        Unidades = grupo.Sum(linea => linea.Cantidad),
        Total = grupo.Sum(linea => linea.Subtotal)
    })
    .OrderByDescending(grupo => grupo.Total)
    .ToList();

Console.WriteLine("\nVentas por categoría (sin pendientes):");
foreach (var grupo in porCategoria)
{
    Console.WriteLine($"{grupo.Categoria}: {grupo.Unidades} unidades - {Formato(grupo.Total)}");
}

// Join + GroupBy: total y cantidad de pedidos por cliente.
var porCliente = pedidos
    .Join(
        clientes,
        pedido => pedido.ClienteId,
        cliente => cliente.Id,
        (pedido, cliente) => new { cliente.Nombre, pedido.Total })
    .GroupBy(fila => fila.Nombre)
    .Select(grupo => new
    {
        Cliente = grupo.Key,
        Pedidos = grupo.Count(),
        Total = grupo.Sum(fila => fila.Total)
    })
    .OrderByDescending(fila => fila.Total)
    .ToList();

Console.WriteLine("\nCompras por cliente:");
foreach (var fila in porCliente)
{
    string etiqueta = fila.Pedidos == 1 ? "pedido" : "pedidos";
    Console.WriteLine($"{fila.Cliente}: {Formato(fila.Total)} en {fila.Pedidos} {etiqueta}");
}

// GroupBy + FirstOrDefault: producto con más unidades vendidas.
var masVendido = pedidos
    .SelectMany(pedido => pedido.Lineas)
    .GroupBy(linea => linea.Producto.Nombre)
    .Select(grupo => new
    {
        Producto = grupo.Key,
        Unidades = grupo.Sum(linea => linea.Cantidad)
    })
    .OrderByDescending(fila => fila.Unidades)
    .FirstOrDefault();

// Aggregate se ejecuta al asignar porque produce un solo valor.
decimal facturado = pedidos
    .Where(pedido => pedido.Estado == EstadoPedido.Entregado)
    .Aggregate(0m, (total, pedido) => total + pedido.Total);

// El snapshot copia el estado actual: no cambia con el Add posterior.
ImmutableArray<Pedido> snapshot = pedidos.ToImmutableArray();
pedidos.Add(new Pedido(105, 2, new DateOnly(2026, 11, 7), EstadoPedido.Pendiente,
    [new(productos[1], 1)]));

// HashSet para unicidad y pertenencia.
var ciudades = clientes.Select(cliente => cliente.Ciudad).ToHashSet();
var productosVendidos = pedidos
    .SelectMany(pedido => pedido.Lineas)
    .Select(linea => linea.Producto.Id)
    .ToHashSet();
var productosSinVentas = productos
    .Where(producto => !productosVendidos.Contains(producto.Id))
    .Select(producto => producto.Nombre)
    .ToList();

Console.WriteLine($"\nMás vendido: {masVendido?.Producto} ({masVendido?.Unidades} unidades)");
Console.WriteLine($"Facturado entregado: {Formato(facturado)}");
Console.WriteLine($"Snapshot: {snapshot.Length} pedidos; lista actual: {pedidos.Count}");
Console.WriteLine($"Ciudades únicas: {string.Join(", ", ciudades.OrderBy(ciudad => ciudad))}");
Console.WriteLine($"Sin ventas: {string.Join(", ", productosSinVentas)}");

static string Formato(decimal monto) =>
    monto.ToString("N0", CultureInfo.InvariantCulture);

public enum EstadoPedido { Pendiente, Enviado, Entregado }

public sealed record Cliente(int Id, string Nombre, string Ciudad);

public sealed record Producto(int Id, string Nombre, string Categoria, decimal Precio);

public sealed record LineaPedido(Producto Producto, int Cantidad)
{
    public decimal Subtotal => Producto.Precio * Cantidad;
}

public sealed record Pedido(
    int Id,
    int ClienteId,
    DateOnly Fecha,
    EstadoPedido Estado,
    IReadOnlyList<LineaPedido> Lineas)
{
    public decimal Total => Lineas.Sum(linea => linea.Subtotal);
}
```

```text
Antes de materializar
Entregados: 3
101 - Bruno Díaz - 320,000
103 - Carla Ruiz - 168,000
100 - Ana Torres - 108,000

Ventas por categoría (sin pendientes):
Pantallas: 2 unidades - 640,000
Periféricos: 7 unidades - 321,000

Compras por cliente:
Ana Torres: 1,358,000 en 2 pedidos
Carla Ruiz: 533,000 en 2 pedidos
Bruno Díaz: 320,000 en 1 pedido

Más vendido: Teclado (3 unidades)
Facturado entregado: 596,000
Snapshot: 5 pedidos; lista actual: 6
Ciudades únicas: Córdoba, Rosario
Sin ventas: Auriculares
```

La consulta de entregados y la de categorías se ejecutan en sus respectivos `ToList`. `Aggregate` se ejecuta al asignar `facturado`. El `Add` del pedido 105 ocurre después del snapshot, así que `snapshot` conserva cinco pedidos; las consultas que se materializaron antes tampoco lo incluyen. Si `consultaEntregados` se recorriera después del `Add`, el pedido 105 no aparecería porque está pendiente, pero una consulta sin filtro de estado sí lo vería.

</details>

## Resumen

- Elegí la colección según el acceso: orden con `List<T>`, clave con `Dictionary<TKey, TValue>`, pertenencia con `HashSet<T>`.
- LINQ permite filtrar, unir, agrupar y agregar sin escribir bucles manuales.
- Las consultas son perezosas; materializá una vez en el límite del método.
- Las colecciones inmutables y las vistas de solo lectura protegen los datos que exponés.
- Un buen reporte combina el modelo correcto con consultas legibles y medibles.

## Siguiente paso

Terminaste el módulo 4. Ya podés transformar y consultar colecciones con criterio. El próximo módulo profundiza en async/await y manejo de errores, dos herramientas que vas a necesitar en cualquier aplicación real.
