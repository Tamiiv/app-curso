---
slug: linq-basico
title: "LINQ: Where, Select, OrderBy"
order: 5
estimatedMinutes: 15
summary: "Filtrá, proyectá y ordená colecciones con LINQ usando consultas claras y composables sobre tus datos."
quiz:
  - question: "¿Qué operador de LINQ sirve para filtrar elementos?"
    options:
      - "Select"
      - "Where"
      - "OrderBy"
      - "ToList"
    answer: 1
    explanation: "Where recibe un predicado y devuelve solo los elementos que lo cumplen."
  - question: "¿Qué hace Select sobre una secuencia?"
    options:
      - "Elimina los duplicados"
      - "Ordena los elementos"
      - "Transforma cada elemento en otro valor"
      - "Cuenta los elementos"
    answer: 2
    explanation: "Select aplica una función a cada elemento y produce una nueva secuencia con los resultados."
  - question: "¿Qué devuelve FirstOrDefault cuando la secuencia está vacía y el tipo es una clase?"
    options:
      - "Lanza InvalidOperationException"
      - "null"
      - "El primer elemento por defecto"
      - "Una lista vacía"
    answer: 1
    explanation: "FirstOrDefault devuelve default(T). Para un tipo por referencia, ese valor es null; para un int, es 0."
  - question: "¿Qué método fuerza la ejecución de una consulta y guarda el resultado en una lista?"
    options:
      - "Where"
      - "Select"
      - "OrderBy"
      - "ToList"
    answer: 3
    explanation: "ToList recorre la secuencia en ese momento y devuelve una List<T> con los resultados ya calculados."
---

# LINQ: Where, Select, OrderBy

## Objetivo

Escribir consultas LINQ para filtrar, transformar y ordenar colecciones, combinando operadores en una pipeline legible y sabiendo cuándo se materializa el resultado.

## Requisitos previos

Necesitás las lambdas de la lección anterior y manejar `List<T>` e `IEnumerable<T>`. Los ejemplos usan el dominio de clientes, productos y pedidos que venimos trabajando.

## Concepto

LINQ significa Language Integrated Query: consultas integradas al lenguaje. Es un conjunto de métodos de extensión sobre `IEnumerable<T>` que permiten describir qué querés obtener en lugar de escribir el bucle que lo obtiene.

`Where` filtra. Recibe un predicado `Func<T, bool>` y devuelve una secuencia con los elementos que lo cumplen. No modifica la colección original.

`Select` proyecta. Recibe una función `Func<T, TResult>` y transforma cada elemento. Podés quedarte con una propiedad, combinar varias o crear un objeto nuevo. Si el resultado es un tipo anónimo, el compilador lo genera por vos.

`OrderBy` ordena de menor a mayor según una clave. `OrderByDescending` hace lo contrario. `ThenBy` y `ThenByDescending` agregan un segundo criterio cuando el primero empata. Todos devuelven una secuencia nueva; no reordenan la lista original.

Estos operadores se encadenan. Cada uno recibe una secuencia y devuelve otra, así que podés componer una pipeline: filtrar, ordenar y proyectar en el orden que tenga sentido para el problema.

LINQ ofrece dos sintaxis equivalentes. La sintaxis de método usa llamadas encadenadas; la sintaxis de consulta se parece a SQL. El compilador traduce la segunda a la primera, así que no hay diferencia de rendimiento.

Una advertencia importante: la mayoría de los operadores son perezosos. La consulta se ejecuta cuando la recorrés o la materializás con `ToList`, `ToArray` o `Count`. En la lección 7 vas a ver este tema en detalle.

## Ejemplo mínimo

```csharp
var numeros = new[] { 5, 12, 8, 21, 3 };

var resultado = numeros
    .Where(numero => numero > 5)
    .OrderBy(numero => numero)
    .Select(numero => numero * 10);

Console.WriteLine(string.Join(", ", resultado));
```

```text
80, 120, 210
```

La pipeline toma los números mayores que 5, los ordena y multiplica cada uno por 10. El array original no cambia.

## Ejemplo aplicado

Queremos armar un catálogo con los productos de hasta 200.000, ordenados por categoría y, dentro de cada categoría, del más caro al más barato.

```csharp
var productos = new List<Producto>
{
    new(10, "Teclado", "Periféricos", 45_000m),
    new(11, "Mouse", "Periféricos", 18_000m),
    new(12, "Monitor", "Pantallas", 320_000m),
    new(13, "Notebook", "Computación", 1_250_000m),
    new(14, "Webcam", "Periféricos", 75_000m)
};

var catalogo = productos
    .Where(producto => producto.Precio <= 200_000m)
    .OrderBy(producto => producto.Categoria)
    .ThenByDescending(producto => producto.Precio)
    .Select(producto => new { producto.Nombre, producto.Categoria, producto.Precio })
    .ToList();

foreach (var producto in catalogo)
{
    Console.WriteLine($"{producto.Categoria}: {producto.Nombre} - {producto.Precio}");
}

Producto? masCaro = productos
    .OrderByDescending(producto => producto.Precio)
    .FirstOrDefault();

Console.WriteLine($"Más caro: {masCaro?.Nombre ?? "sin productos"}");

public sealed record Producto(int Id, string Nombre, string Categoria, decimal Precio);
```

```text
Pantallas: Monitor - 320000
Periféricos: Webcam - 75000
Periféricos: Teclado - 45000
Periféricos: Mouse - 18000
Más caro: Notebook
```

La misma consulta con sintaxis de consulta se lee así:

```csharp
var nombres = from producto in productos
              where producto.Precio > 50_000m
              orderby producto.Nombre
              select producto.Nombre;

Console.WriteLine(string.Join(", ", nombres));
```

```text
Monitor, Notebook, Webcam
```

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `InvalidOperationException` con `First` | La secuencia quedó vacía | Usar `FirstOrDefault` y comprobar el resultado |
| Se espera que `OrderBy` modifique la lista | LINQ devuelve una secuencia nueva | Asignar el resultado o materializarlo con `ToList` |
| La consulta se ejecuta varias veces | Se recorre la misma variable perezosa en varios lugares | Materializar una vez y reutilizar la lista |
| `Count() > 0` para saber si hay elementos | Recorre toda la secuencia | Usar `Any()` |
| Se accede a una propiedad de un `FirstOrDefault` vacío | El resultado puede ser `null` | Comprobar con `is not null` o usar `?.` |
| Filtrar después de proyectar | Se transforman elementos que luego se descartan | Poner `Where` antes de `Select` cuando sea posible |

## Buenas prácticas

- Para pipelines simples, usá sintaxis de método; para consultas con varios `from` o `join`, la sintaxis de consulta puede leerse mejor.
- Filtrá temprano: `Where` antes de `Select` reduce el trabajo y mantiene la consulta clara.
- Materializá en el límite de tu método o componente, no en cada paso intermedio.
- Usá `Any()` para preguntar si existe al menos un elemento y `Count(predicado)` solo cuando necesités el número.
- Evitá efectos secundarios dentro de `Where` o `Select`; una consulta debería describir una transformación, no cambiar el mundo.

## Ejercicio

De la lista de productos, obtené los nombres en mayúsculas de los productos de la categoría `Periféricos` con precio menor a 100.000, ordenados de menor a mayor precio. Mostrá la cantidad de resultados.

<details>
<summary>Ver solución</summary>

```csharp
var productos = new List<Producto>
{
    new(10, "Teclado", "Periféricos", 45_000m),
    new(11, "Mouse", "Periféricos", 18_000m),
    new(12, "Monitor", "Pantallas", 320_000m),
    new(14, "Webcam", "Periféricos", 75_000m)
};

var nombres = productos
    .Where(producto => producto.Categoria == "Periféricos" && producto.Precio < 100_000m)
    .OrderBy(producto => producto.Precio)
    .Select(producto => producto.Nombre.ToUpperInvariant())
    .ToList();

foreach (string nombre in nombres)
{
    Console.WriteLine(nombre);
}

Console.WriteLine($"Cantidad: {nombres.Count}");

public sealed record Producto(int Id, string Nombre, string Categoria, decimal Precio);
```

```text
MOUSE
TECLADO
WEBCAM
Cantidad: 3
```

`ToUpperInvariant` evita diferencias según la cultura del sistema. El `ToList` final materializa la consulta una sola vez y permite usar `Count` sin volver a recorrerla.

</details>

## Resumen

- `Where` filtra, `Select` transforma y `OrderBy` ordena.
- Los operadores se encadenan y devuelven nuevas secuencias sin modificar el origen.
- La sintaxis de consulta y la de método son equivalentes.
- `FirstOrDefault` evita excepciones cuando no hay resultados; `ToList` materializa.

## Siguiente paso

Con filtros y proyecciones ya podés resolver muchas consultas. En la próxima lección vas a agrupar, combinar y reducir datos con [GroupBy, Join y Aggregate](/modulos/colecciones-linq/linq-avanzado).
