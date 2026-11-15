---
slug: colecciones-inmutables
title: "Colecciones inmutables y de solo lectura"
order: 8
estimatedMinutes: 12
summary: "Protegé tus datos distinguiendo vistas de solo lectura, copias defensivas y colecciones inmutables que no cambian."
quiz:
  - question: "¿Qué garantiza una propiedad tipada como IReadOnlyList<T>?"
    options:
      - "Que la lista subyacente es inmutable"
      - "Que no se puede modificar a través de esa interfaz"
      - "Que no se puede convertir a List<T>"
      - "Que cada operación crea una copia"
    answer: 1
    explanation: "IReadOnlyList<T> no expone Add ni Remove, pero no impide que el objeto real sea una List<T> mutable o que alguien haga una conversión."
  - question: "¿Qué ocurre si se modifica la lista original después de llamar a AsReadOnly?"
    options:
      - "La vista de solo lectura no cambia"
      - "La vista refleja el cambio, porque envuelve la lista original"
      - "Se lanza una excepción"
      - "La vista se convierte en inmutable"
    answer: 1
    explanation: "AsReadOnly devuelve una vista sobre la misma lista. No es una copia ni una colección inmutable: los cambios de la lista original se ven en la vista."
  - question: "¿Qué devuelve ImmutableList<T>.Add?"
    options:
      - "Modifica la lista actual"
      - "Una nueva lista con el elemento agregado"
      - "Un bool que indica si se agregó"
      - "Una vista de solo lectura"
    answer: 1
    explanation: "Las colecciones inmutables no se modifican: Add devuelve una nueva colección que comparte la mayor parte de la estructura con la anterior."
---

# Colecciones inmutables y de solo lectura

## Objetivo

Distinguir entre una variable de solo lectura, una vista de solo lectura y una colección inmutable, y elegir la protección adecuada para los datos de tu aplicación.

## Requisitos previos

Necesitás manejar `List<T>`, `Dictionary<TKey, TValue>`, interfaces y el operador de colección con corchetes. También ayuda entender la diferencia entre tipos por referencia y por valor del módulo 3.

## Concepto

`readonly` en un campo significa que no podés reasignar la referencia después del constructor. No significa que el objeto sea inmutable: un campo `private readonly List<Producto> _productos` sigue permitiendo `_productos.Add(...)`.

Una interfaz de solo lectura es una vista. `IReadOnlyList<T>` expone `Count`, el indexador y el enumerador, pero no `Add` ni `Remove`. `IReadOnlyDictionary<TKey, TValue>` y `IReadOnlySet<T>` hacen lo mismo para diccionarios y conjuntos. Es una buena forma de decir "no modifiques esto desde acá", aunque no es una garantía absoluta: si el objeto real es una `List<T>`, alguien podría convertir la referencia y modificarla.

`AsReadOnly()` devuelve una `ReadOnlyCollection<T>`, que envuelve la colección original. No es una copia. Si la lista original cambia, la vista muestra el cambio. Sirve para exponer una colección que sigue siendo propiedad de otra clase, pero no para congelar una foto.

Una colección inmutable es distinta: no cambia nunca. `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableDictionary<TKey, TValue>` e `ImmutableHashSet<T>` viven en `System.Collections.Immutable`. Agregar un elemento devuelve una colección nueva y comparte la mayor parte de la estructura con la anterior, así que no copia todo. La original queda intacta.

Para búsquedas de solo lectura muy frecuentes, .NET 8 incorporó `FrozenDictionary<TKey, TValue>` y `FrozenSet<T>`. Se construyen una vez y optimizan las lecturas; no sirven si necesitás modificar los datos todo el tiempo.

Las expresiones de colección de C# 12 permiten escribir `[.. lineas]` para crear una copia a partir de una secuencia. Esa copia es independiente de la original y es la forma más corta de hacer una copia defensiva en un constructor.

## Ejemplo mínimo

```csharp
using System.Collections.Immutable;

var productos = new List<Producto>
{
    new(10, "Teclado", 45_000m),
    new(11, "Mouse", 18_000m)
};

IReadOnlyList<Producto> catalogo = productos;
ImmutableArray<Producto> inmutable = productos.ToImmutableArray();
ImmutableArray<Producto> ampliado = inmutable.Add(new Producto(12, "Monitor", 320_000m));

Console.WriteLine($"Catálogo: {catalogo.Count}");
Console.WriteLine($"Original: {inmutable.Length}, ampliado: {ampliado.Length}");

public sealed record Producto(int Id, string Nombre, decimal Precio);
```

```text
Catálogo: 2
Original: 2, ampliado: 3
```

`catalogo` no permite llamar a `Add`, pero sigue apuntando a la lista mutable. `inmutable` no cambió al agregar el monitor: `ampliado` es una colección nueva.

## Ejemplo aplicado

Un pedido no debería cambiar sus líneas porque alguien conservó la lista original. La clase copia las líneas en el constructor y expone solo una vista de lectura.

```csharp
var lineas = new List<LineaPedido>
{
    new(new Producto(10, "Teclado", 45_000m), 2),
    new(new Producto(11, "Mouse", 18_000m), 1)
};

var pedido = new Pedido(100, 1, new DateOnly(2026, 11, 2), lineas);
lineas.Add(new LineaPedido(new Producto(12, "Monitor", 320_000m), 1));

Console.WriteLine($"Líneas del pedido: {pedido.Lineas.Count}");
Console.WriteLine($"Líneas de la lista original: {lineas.Count}");

public sealed class Pedido
{
    private readonly List<LineaPedido> _lineas;

    public Pedido(int id, int clienteId, DateOnly fecha, IEnumerable<LineaPedido> lineas)
    {
        Id = id;
        ClienteId = clienteId;
        Fecha = fecha;
        _lineas = [.. lineas];
    }

    public int Id { get; }
    public int ClienteId { get; }
    public DateOnly Fecha { get; }
    public IReadOnlyList<LineaPedido> Lineas => _lineas;
}

public sealed record Producto(int Id, string Nombre, decimal Precio);
public sealed record LineaPedido(Producto Producto, int Cantidad)
{
    public decimal Subtotal => Producto.Precio * Cantidad;
}
```

```text
Líneas del pedido: 2
Líneas de la lista original: 3
```

La copia defensiva corta el vínculo con la lista externa. El pedido conserva dos líneas aunque el llamador siga modificando su lista.

Si además querés que las operaciones devuelvan una versión nueva sin tocar la anterior, podés usar `ImmutableArray<T>`:

```csharp
using System.Collections.Immutable;

var carrito = Carrito.Vacio
    .Agregar(new LineaPedido(new Producto(10, "Teclado", 45_000m), 2))
    .Agregar(new LineaPedido(new Producto(11, "Mouse", 18_000m), 1));

Console.WriteLine($"Líneas: {carrito.Lineas.Length}, total: {carrito.Total}");

public sealed class Carrito
{
    public static Carrito Vacio { get; } = new(ImmutableArray<LineaPedido>.Empty);

    private Carrito(ImmutableArray<LineaPedido> lineas) => Lineas = lineas;

    public ImmutableArray<LineaPedido> Lineas { get; }

    public decimal Total => Lineas.Sum(linea => linea.Subtotal);

    public Carrito Agregar(LineaPedido linea) => new(Lineas.Add(linea));
}

public sealed record Producto(int Id, string Nombre, decimal Precio);
public sealed record LineaPedido(Producto Producto, int Cantidad)
{
    public decimal Subtotal => Producto.Precio * Cantidad;
}
```

Cada `Agregar` devuelve un carrito nuevo. El carrito anterior sigue existiendo con sus dos o tres líneas, lo que facilita comparar versiones y evitar efectos colaterales.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Un campo `readonly List<T>` se modifica | `readonly` protege la referencia, no el contenido | Usar una colección inmutable o copiar antes de exponer |
| El llamador agrega elementos a una propiedad pública | Se devolvió el `List<T>` real | Exponer `IReadOnlyList<T>` o una copia |
| La vista de solo lectura cambia sola | `AsReadOnly` envuelve la lista original | Hacer una copia con `[.. lista]` o `ToImmutableArray` |
| Se ignora el resultado de `ImmutableList.Add` | La colección original no se modifica | Asignar el retorno: `lista = lista.Add(item)` |
| Se hacen copias inmutables en un bucle caliente | Cada operación crea una estructura nueva | Usar un `Builder` o una lista mutable local y congelar al final |
| Se asume que `IReadOnlyList<T>` impide todo cambio | El objeto real puede ser una `List<T>` | Combinar la interfaz con copia defensiva o inmutabilidad real |

## Buenas prácticas

- Aceptá `IEnumerable<T>` en constructores y hacé una copia defensiva con `[.. secuencia]`.
- Devolvé `IReadOnlyList<T>` o `IReadOnlyDictionary<TKey, TValue>` en lugar de colecciones mutables.
- Usá colecciones inmutables para snapshots, configuración y objetos de dominio que no deberían cambiar.
- Reservá `AsReadOnly()` para vistas sobre datos que otra clase sigue administrando.
- Para catálogos estáticos de lectura intensiva, considerá `FrozenDictionary` o `FrozenSet`.

## Ejercicio

Implementá una clase `Catalogo` que guarde productos en un `ImmutableArray<Producto>`. Debe permitir agregar un producto devolviendo un catálogo nuevo, exponer los productos como `IReadOnlyList<Producto>` y filtrar por categoría sin modificar el catálogo original.

<details>
<summary>Ver solución</summary>

```csharp
using System.Collections.Immutable;

var catalogo = new Catalogo(new[]
{
    new Producto(10, "Teclado", "Periféricos", 45_000m),
    new Producto(11, "Mouse", "Periféricos", 18_000m)
});

Catalogo ampliado = catalogo.Agregar(
    new Producto(12, "Monitor", "Pantallas", 320_000m));

Console.WriteLine($"Original: {catalogo.Productos.Count}");
Console.WriteLine($"Ampliado: {ampliado.Productos.Count}");

foreach (Producto producto in ampliado.PorCategoria("Periféricos"))
{
    Console.WriteLine($"{producto.Nombre}: {producto.Precio}");
}

public sealed class Catalogo
{
    private readonly ImmutableArray<Producto> _productos;

    public Catalogo(IEnumerable<Producto> productos)
    {
        _productos = productos.ToImmutableArray();
    }

    public IReadOnlyList<Producto> Productos => _productos;

    public Catalogo Agregar(Producto producto) => new(_productos.Add(producto));

    public IReadOnlyList<Producto> PorCategoria(string categoria)
    {
        return _productos
            .Where(producto => producto.Categoria == categoria)
            .OrderBy(producto => producto.Nombre)
            .ToList();
    }
}

public sealed record Producto(int Id, string Nombre, string Categoria, decimal Precio);
```

```text
Original: 2
Ampliado: 3
Mouse: 18000
Teclado: 45000
```

`Agregar` no toca el catálogo original: devuelve uno nuevo. `PorCategoria` materializa el resultado con `ToList` y lo devuelve como interfaz de solo lectura.

</details>

## Resumen

- `readonly` impide reasignar la referencia, no modificar el contenido.
- `IReadOnlyList<T>` y `AsReadOnly()` son vistas; no garantizan inmutabilidad real.
- Las colecciones inmutables devuelven una copia nueva en cada operación.
- Una copia defensiva en el constructor evita que el llamador cambie tus datos.

## Siguiente paso

Ya tenés todas las piezas del módulo. En la última lección vas a combinarlas en un [ejercicio integrador](/modulos/colecciones-linq/ejercicio-integrador) con consultas, reportes y decisiones sobre colecciones.
