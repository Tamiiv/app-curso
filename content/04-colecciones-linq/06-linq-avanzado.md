---
slug: linq-avanzado
title: "LINQ: GroupBy, Join, Aggregate"
order: 6
estimatedMinutes: 18
summary: "Agrupá datos, combiná colecciones por clave y reducí secuencias a un solo valor con consultas LINQ más expresivas."
quiz:
  - question: "¿Qué devuelve GroupBy cuando agrupás una secuencia?"
    options:
      - "Una lista de listas"
      - "Una secuencia de IGrouping<TKey, TElement>"
      - "Un Dictionary<TKey, TValue>"
      - "Un solo valor agregado"
    answer: 1
    explanation: "GroupBy devuelve una secuencia de grupos. Cada IGrouping expone su Key y se puede recorrer como una secuencia de elementos."
  - question: "¿Qué ocurre con los elementos que no tienen coincidencia en un Join?"
    options:
      - "Se incluyen con valores null"
      - "Se descartan, porque Join es una unión interna"
      - "Lanzan una excepción"
      - "Se agregan al primer grupo"
    answer: 1
    explanation: "Join produce solo las parejas que cumplen la condición. Para conservar los elementos sin coincidencia hace falta un left join con GroupJoin y DefaultIfEmpty."
  - question: "En Aggregate(seed, func), ¿qué representa seed?"
    options:
      - "El primer elemento de la secuencia"
      - "El valor inicial del acumulador"
      - "La cantidad de elementos"
      - "La clave de agrupación"
    answer: 1
    explanation: "seed es el valor inicial que recibe el acumulador en la primera llamada a func."
  - question: "¿Qué pasa al usar Average sobre una secuencia vacía?"
    options:
      - "Devuelve 0"
      - "Devuelve null"
      - "Lanza InvalidOperationException"
      - "Devuelve el valor mínimo del tipo"
    answer: 2
    explanation: "Average no tiene un valor neutro para una secuencia vacía, por eso lanza InvalidOperationException. Sum, en cambio, devuelve 0."
---

# LINQ: GroupBy, Join, Aggregate

## Objetivo

Agrupar elementos por una clave, combinar dos colecciones mediante una clave común y reducir una secuencia a un único valor con `Aggregate`, eligiendo el operador adecuado para cada reporte.

## Requisitos previos

Necesitás manejar `Where`, `Select` y `OrderBy` de la lección anterior, además de lambdas y colecciones. Vamos a usar el mismo dominio de clientes, productos y pedidos.

## Concepto

`GroupBy` separa una secuencia en grupos según una clave. El resultado es una secuencia de `IGrouping<TKey, TElement>`: cada grupo tiene una propiedad `Key` y se puede recorrer como cualquier secuencia. Es la herramienta natural para responder "cuánto vendí por categoría" o "cuántos pedidos hizo cada cliente".

Dentro de cada grupo podés usar funciones de agregación: `Sum`, `Average`, `Min`, `Max` y `Count`. Son operadores que recorren el grupo y devuelven un solo valor. `Count` sin predicado cuenta elementos; `Count(predicado)` cuenta los que cumplen la condición.

`Join` combina dos secuencias por una clave, como un `JOIN` de SQL. Recibe la segunda secuencia, dos selectores de clave y una función que arma el resultado. Es una unión interna: si un pedido no tiene cliente o un cliente no tiene pedidos, esa fila no aparece. Para conservar todos los elementos de un lado se usa `GroupJoin` con `DefaultIfEmpty`, que es un left join.

`Aggregate` es la operación de reducción general. Recibe un valor inicial, llamado seed, y una función que combina el acumulador con cada elemento. `Sum` es un caso particular de `Aggregate`; usá `Aggregate` cuando la acumulación no sea una simple suma, por ejemplo para contar y totalizar al mismo tiempo.

También podés escribir `group` y `join` con sintaxis de consulta. El compilador las traduce a los mismos métodos, así que elegí la que se lea mejor para tu equipo.

## Ejemplo mínimo

```csharp
var ventas = new[]
{
    new { Categoria = "Periféricos", Monto = 45_000m },
    new { Categoria = "Pantallas", Monto = 320_000m },
    new { Categoria = "Periféricos", Monto = 18_000m }
};

var porCategoria = ventas
    .GroupBy(venta => venta.Categoria)
    .Select(grupo => new
    {
        Categoria = grupo.Key,
        Total = grupo.Sum(venta => venta.Monto),
        Operaciones = grupo.Count()
    })
    .OrderByDescending(grupo => grupo.Total);

foreach (var grupo in porCategoria)
{
    Console.WriteLine($"{grupo.Categoria}: {grupo.Total} en {grupo.Operaciones} operaciones");
}
```

```text
Pantallas: 320000 en 1 operaciones
Periféricos: 63000 en 2 operaciones
```

## Ejemplo aplicado

La tienda tiene clientes y pedidos separados. Queremos un reporte con el nombre del cliente, el total de cada pedido y la venta acumulada por categoría.

```csharp
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
    new(13, "Notebook", "Computación", 1_250_000m)
};

var pedidos = new List<Pedido>
{
    new(100, 1, [new(productos[0], 2), new(productos[1], 1)]),
    new(101, 2, [new(productos[2], 1)]),
    new(102, 1, [new(productos[3], 1)]),
    new(103, 3, [new(productos[2], 1), new(productos[0], 1)])
};

var reporte = pedidos
    .Join(
        clientes,
        pedido => pedido.ClienteId,
        cliente => cliente.Id,
        (pedido, cliente) => new
        {
            cliente.Nombre,
            pedido.Id,
            Total = pedido.Lineas.Sum(linea => linea.Subtotal)
        })
    .OrderByDescending(fila => fila.Total);

foreach (var fila in reporte)
{
    Console.WriteLine($"{fila.Id} - {fila.Nombre} - {fila.Total}");
}

var porCategoria = pedidos
    .SelectMany(pedido => pedido.Lineas)
    .GroupBy(linea => linea.Producto.Categoria)
    .Select(grupo => new
    {
        Categoria = grupo.Key,
        Unidades = grupo.Sum(linea => linea.Cantidad),
        Total = grupo.Sum(linea => linea.Subtotal)
    })
    .OrderByDescending(grupo => grupo.Total);

foreach (var grupo in porCategoria)
{
    Console.WriteLine($"{grupo.Categoria}: {grupo.Unidades} unidades, {grupo.Total}");
}

var resumen = pedidos.Aggregate(
    seed: (Cantidad: 0, Total: 0m),
    func: (acumulado, pedido) => (
        acumulado.Cantidad + 1,
        acumulado.Total + pedido.Lineas.Sum(linea => linea.Subtotal)));

Console.WriteLine($"Resumen: {resumen.Cantidad} pedidos, {resumen.Total}");

public sealed record Cliente(int Id, string Nombre, string Ciudad);
public sealed record Producto(int Id, string Nombre, string Categoria, decimal Precio);
public sealed record LineaPedido(Producto Producto, int Cantidad)
{
    public decimal Subtotal => Producto.Precio * Cantidad;
}
public sealed record Pedido(int Id, int ClienteId, List<LineaPedido> Lineas);
```

```text
102 - Ana Torres - 1250000
103 - Carla Ruiz - 365000
101 - Bruno Díaz - 320000
100 - Ana Torres - 108000
Computación: 1 unidades, 1250000
Pantallas: 2 unidades, 640000
Periféricos: 4 unidades, 153000
Resumen: 4 pedidos, 2043000
```

`SelectMany` aplana las líneas de todos los pedidos en una sola secuencia. Sin ese paso, `GroupBy` agruparía pedidos y no productos.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Faltan filas después de un `Join` | La unión es interna y no hay clave coincidente | Revisar los datos o usar `GroupJoin` con `DefaultIfEmpty` |
| `Join` no compila por tipos distintos | La clave de un lado es `int` y la del otro es `string` | Unificar el tipo o convertir explícitamente antes de unir |
| `Aggregate` lanza una excepción con una secuencia vacía | Se usó la sobrecarga sin seed | Pasar un valor inicial o comprobar si hay elementos |
| `Average` falla con una lista vacía | No existe promedio de cero elementos | Validar antes o usar `DefaultIfEmpty` |
| Los totales tienen centavos inesperados | Se usó `double` para dinero | Usar `decimal` en precios y totales |
| El reporte se recalcula en cada recorrido | La consulta agrupada quedó sin materializar | Guardar el resultado con `ToList` si se reutiliza |

## Buenas prácticas

- Usá `Sum` para totales y reservá `Aggregate` para reducciones que no tienen un operador específico.
- Nombrá las claves y los resultados con el vocabulario del negocio: `TotalPorCategoria`, `Cliente`, `Unidades`.
- Para claves compuestas, usá un tipo anónimo: `new { pedido.ClienteId, pedido.Fecha }`.
- Materializá los reportes que vas a recorrer varias veces; así evitás recalcular grupos y uniones.
- Si necesitás conservar clientes sin pedidos, no uses `Join` a secas: planteá un left join con `GroupJoin`.

## Ejercicio

Usando las listas del ejemplo, calculá cuánto gastó cada cliente y cuántos pedidos hizo. Mostrá el resultado de mayor a menor total. Resolvelo con `Join` y `GroupBy`.

<details>
<summary>Ver solución</summary>

```csharp
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
    new(13, "Notebook", "Computación", 1_250_000m)
};

var pedidos = new List<Pedido>
{
    new(100, 1, [new(productos[0], 2), new(productos[1], 1)]),
    new(101, 2, [new(productos[2], 1)]),
    new(102, 1, [new(productos[3], 1)]),
    new(103, 3, [new(productos[2], 1), new(productos[0], 1)])
};

var porCliente = pedidos
    .Join(
        clientes,
        pedido => pedido.ClienteId,
        cliente => cliente.Id,
        (pedido, cliente) => new
        {
            cliente.Nombre,
            Total = pedido.Lineas.Sum(linea => linea.Subtotal)
        })
    .GroupBy(fila => fila.Nombre)
    .Select(grupo => new
    {
        Cliente = grupo.Key,
        Pedidos = grupo.Count(),
        Total = grupo.Sum(fila => fila.Total)
    })
    .OrderByDescending(fila => fila.Total)
    .ToList();

foreach (var fila in porCliente)
{
    string etiqueta = fila.Pedidos == 1 ? "pedido" : "pedidos";
    Console.WriteLine($"{fila.Cliente}: {fila.Total} en {fila.Pedidos} {etiqueta}");
}

public sealed record Cliente(int Id, string Nombre, string Ciudad);
public sealed record Producto(int Id, string Nombre, string Categoria, decimal Precio);
public sealed record LineaPedido(Producto Producto, int Cantidad)
{
    public decimal Subtotal => Producto.Precio * Cantidad;
}
public sealed record Pedido(int Id, int ClienteId, List<LineaPedido> Lineas);
```

```text
Ana Torres: 1358000 en 2 pedidos
Carla Ruiz: 365000 en 1 pedido
Bruno Díaz: 320000 en 1 pedido
```

El `Join` agrega el nombre del cliente y el `GroupBy` acumula los totales por nombre. El `ToList` final evita recalcular el reporte al recorrerlo.

</details>

## Resumen

- `GroupBy` crea grupos con una clave y permite agregar cada grupo por separado.
- `Join` combina secuencias por clave y descarta las filas sin coincidencia.
- `Aggregate` reduce una secuencia con un acumulador y un valor inicial.
- `Sum`, `Average`, `Min`, `Max` y `Count` cubren las agregaciones más comunes.

## Siguiente paso

Ya sabés expresar consultas complejas. Falta entender con precisión cuándo se ejecutan y cómo evitar trabajo repetido: seguí con [ejecución diferida y materialización](/modulos/colecciones-linq/ejecucion-diferida).
