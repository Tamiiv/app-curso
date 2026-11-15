---
slug: ejecucion-diferida
title: "Ejecución diferida y materialización"
order: 7
estimatedMinutes: 15
summary: "Entendé cuándo se ejecuta una consulta LINQ, cómo evitar recorridos repetidos y cuándo conviene materializar resultados."
quiz:
  - question: "¿Cuándo se ejecuta una consulta LINQ definida con Where y Select?"
    options:
      - "Al asignarla a una variable"
      - "Al compilar el proyecto"
      - "Cuando se enumera o se materializa"
      - "Solo si se usa OrderBy"
    answer: 2
    explanation: "Los operadores de LINQ sobre IEnumerable<T> son perezosos: la consulta se evalúa cuando se recorre con foreach o se materializa con ToList, Count, First y similares."
  - question: "¿Qué método materializa una consulta en una lista?"
    options:
      - "Where"
      - "Select"
      - "AsEnumerable"
      - "ToList"
    answer: 3
    explanation: "ToList recorre la secuencia en ese momento y devuelve una List<T> con los resultados ya calculados."
  - question: "¿Qué hace AsEnumerable sobre una consulta?"
    options:
      - "La ejecuta inmediatamente"
      - "La convierte en una secuencia en memoria para los operadores siguientes"
      - "La ordena por defecto"
      - "Elimina los duplicados"
    answer: 1
    explanation: "AsEnumerable no materializa: cambia el tipo a IEnumerable<T>. En una consulta de IQueryable, eso hace que los operadores posteriores se ejecuten en memoria."
  - question: "¿Por qué conviene evitar enumerar dos veces la misma consulta costosa?"
    options:
      - "Porque LINQ lanza una excepción"
      - "Porque el segundo recorrido vuelve a ejecutar todo el trabajo"
      - "Porque la consulta se borra después del primer uso"
      - "Porque ToList deja de funcionar"
    answer: 1
    explanation: "Cada enumeración reinicia la consulta. Si el origen es costoso, el trabajo se repite; materializar una vez evita ese problema."
---

# Ejecución diferida y materialización

## Objetivo

Entender la diferencia entre describir una consulta y ejecutarla, reconocer los operadores que materializan y decidir cuándo guardar un resultado para no repetir trabajo.

## Requisitos previos

Necesitás manejar `IEnumerable<T>`, iteradores y los operadores LINQ de las lecciones 5 y 6. Esta lección explica el comportamiento que ya viste en los ejemplos: la consulta no corre hasta que alguien la recorre.

## Concepto

Cuando escribís `var consulta = pedidos.Where(p => p.Estado == EstadoPedido.Entregado);`, no se filtró nada todavía. La variable `consulta` guarda una descripción: "cuando me recorras, tomá los pedidos y devolveme los entregados". El trabajo ocurre en el `foreach`, en `Count()`, en `First()` o en `ToList()`.

Eso se llama ejecución diferida. Es una virtud porque permite componer consultas sin pagar cada paso y porque el origen puede cambiar entre la definición y la ejecución. También es una trampa si asumís que la consulta ya se ejecutó o si la recorrés varias veces sin darte cuenta.

Los operadores se dividen en dos grupos. Los de streaming, como `Where` y `Select`, procesan un elemento por vez y no necesitan toda la secuencia en memoria. Los de buffering, como `OrderBy`, `GroupBy` y `Reverse`, tienen que leer todo antes de producir el primer resultado. Por eso una consulta ordenada puede ser más costosa de lo que parece.

Materializar significa convertir la secuencia en una estructura concreta: `ToList`, `ToArray`, `ToDictionary`, `ToHashSet`. También ejecutan `Count`, `First`, `Single`, `Any`, `All`, `Sum`, `Average`, `Min` y `Max`. Después de materializar, el resultado es una foto: si el origen cambia, la lista no.

`AsEnumerable` no materializa. Solo cambia el tipo estático a `IEnumerable<T>`. Se usa cuando tenés un `IQueryable<T>` y querés que los operadores siguientes se ejecuten en memoria en lugar de traducirse al proveedor. `IQueryable<T>` aparece con EF Core y otros proveedores; en este módulo solo lo mencionamos.

La regla práctica es: construí la consulta con operadores perezosos y materializá una vez, en el límite donde el resultado se va a usar o se va a devolver.

## Ejemplo mínimo

```csharp
var productos = new List<Producto>
{
    new(10, "Teclado", 45_000m),
    new(11, "Mouse", 18_000m),
    new(12, "Monitor", 320_000m)
};

var consulta = productos.Where(producto =>
{
    Console.WriteLine($"Evaluando {producto.Nombre}");
    return producto.Precio > 50_000m;
});

Console.WriteLine("Antes de enumerar");

foreach (Producto producto in consulta)
{
    Console.WriteLine($"Resultado: {producto.Nombre}");
}

public sealed record Producto(int Id, string Nombre, decimal Precio);
```

```text
Antes de enumerar
Evaluando Teclado
Resultado: Teclado
Evaluando Mouse
Evaluando Monitor
Resultado: Monitor
```

La frase "Antes de enumerar" aparece primero: definir la consulta no ejecutó el filtro. Cada producto se evalúa recién cuando el `foreach` lo pide.

## Ejemplo aplicado

La tienda arma un informe de pedidos entregados. La consulta se define una vez y se materializa cuando el informe está listo para mostrarse.

```csharp
var pedidos = new List<Pedido>
{
    new(100, 1, EstadoPedido.Entregado, [new(new Producto(10, "Teclado", 45_000m), 2)]),
    new(101, 2, EstadoPedido.Pendiente, [new(new Producto(12, "Monitor", 320_000m), 1)]),
    new(102, 3, EstadoPedido.Entregado, [new(new Producto(11, "Mouse", 18_000m), 1)])
};

var consulta = pedidos
    .Where(pedido => pedido.Estado == EstadoPedido.Entregado)
    .Select(pedido => new
    {
        pedido.Id,
        Total = pedido.Lineas.Sum(linea => linea.Subtotal)
    })
    .OrderByDescending(fila => fila.Total);

Console.WriteLine("Consulta definida");

var informe = consulta.ToList();

Console.WriteLine($"Pedidos entregados: {informe.Count}");
foreach (var fila in informe)
{
    Console.WriteLine($"{fila.Id}: {fila.Total}");
}

public enum EstadoPedido { Pendiente, Enviado, Entregado }
public sealed record Producto(int Id, string Nombre, decimal Precio);
public sealed record LineaPedido(Producto Producto, int Cantidad)
{
    public decimal Subtotal => Producto.Precio * Cantidad;
}
public sealed record Pedido(int Id, int ClienteId, EstadoPedido Estado, List<LineaPedido> Lineas);
```

```text
Consulta definida
Pedidos entregados: 2
100: 90000
102: 18000
```

`ToList` ejecutó el filtro, la proyección y el orden en ese momento. A partir de ahí, `informe` es una lista con dos elementos ya calculados.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La consulta parece vacía hasta que se recorre | Se esperaba ejecución al asignarla | Recordar que `Where` y `Select` son perezosos |
| El resultado no refleja cambios posteriores | La consulta ya se materializó | Volver a consultar o materializar después del cambio |
| El origen cambió y la consulta devuelve otra cosa | La consulta se ejecuta con el estado actual | Materializar antes de modificar el origen |
| El trabajo se repite dos veces | Se usó `Count()` y después `foreach` sobre la misma consulta | Materializar una vez y reutilizar la lista |
| `ObjectDisposedException` al recorrer una consulta de EF Core | La consulta se devolvió sin materializar y el contexto se cerró | Materializar dentro del ámbito del contexto |
| Se pierde composición por materializar demasiado pronto | Se llamó `ToList` en cada paso | Materializar solo en el límite del método o componente |

## Buenas prácticas

- Construí la consulta con `Where`, `Select` y `OrderBy`, y materializá una sola vez al final.
- Devolvé `IReadOnlyList<T>` desde métodos públicos cuando ya materializaste el resultado.
- Usá `Any()` para saber si existe un elemento y reservá `Count()` para cuando necesités el número.
- No devuelvas consultas perezosas que dependan de un recurso que se libera al salir del método.
- Si una consulta se recorre varias veces, guardala con `ToList`; si se recorre una sola vez, dejá que fluya.

## Ejercicio

Escribí un método `ObtenerEntregados` que reciba una secuencia de pedidos, filtre los entregados, los ordene por fecha descendente, proyecte id y total, y devuelva `IReadOnlyList<T>` con el resultado ya materializado. Explicá en un comentario cuántas veces se ejecuta la consulta.

<details>
<summary>Ver solución</summary>

```csharp
static IReadOnlyList<ResumenPedido> ObtenerEntregados(IEnumerable<Pedido> pedidos)
{
    // La consulta se ejecuta una sola vez, al llamar a ToList.
    return pedidos
        .Where(pedido => pedido.Estado == EstadoPedido.Entregado)
        .OrderByDescending(pedido => pedido.Fecha)
        .Select(pedido => new ResumenPedido(
            pedido.Id,
            pedido.Lineas.Sum(linea => linea.Subtotal)))
        .ToList();
}

var pedidos = new List<Pedido>
{
    new(100, new DateOnly(2026, 11, 2), EstadoPedido.Entregado,
        [new(new Producto(10, "Teclado", 45_000m), 2)]),
    new(101, new DateOnly(2026, 11, 3), EstadoPedido.Pendiente,
        [new(new Producto(12, "Monitor", 320_000m), 1)]),
    new(102, new DateOnly(2026, 11, 4), EstadoPedido.Entregado,
        [new(new Producto(11, "Mouse", 18_000m), 1)])
};

IReadOnlyList<ResumenPedido> resumen = ObtenerEntregados(pedidos);

foreach (ResumenPedido fila in resumen)
{
    Console.WriteLine($"{fila.Id}: {fila.Total}");
}

public sealed record ResumenPedido(int Id, decimal Total);
public enum EstadoPedido { Pendiente, Enviado, Entregado }
public sealed record Producto(int Id, string Nombre, decimal Precio);
public sealed record LineaPedido(Producto Producto, int Cantidad)
{
    public decimal Subtotal => Producto.Precio * Cantidad;
}
public sealed record Pedido(int Id, DateOnly Fecha, EstadoPedido Estado, List<LineaPedido> Lineas);
```

```text
102: 18000
100: 90000
```

El `ToList` dentro del método es la única ejecución de la consulta. Quien recibe `resumen` puede recorrerlo las veces que quiera sin volver a filtrar ni ordenar.

</details>

## Resumen

- Una consulta LINQ es una descripción que se ejecuta al enumerarse.
- `ToList`, `ToArray` y `Count` materializan; `Where` y `Select` no.
- `AsEnumerable` cambia el tipo y el modo de ejecución, pero no materializa.
- Materializá una vez en el límite y evitá repetir consultas costosas.

## Siguiente paso

Ya sabés cuándo se ejecuta una consulta y cómo guardar resultados. En la próxima lección vas a proteger esos datos con [colecciones inmutables y de solo lectura](/modulos/colecciones-linq/colecciones-inmutables).
