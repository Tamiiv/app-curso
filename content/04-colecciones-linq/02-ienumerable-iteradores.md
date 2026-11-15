---
slug: ienumerable-iteradores
title: "IEnumerable e iteradores"
order: 2
estimatedMinutes: 15
summary: "Cómo representar secuencias, escribir iteradores con yield return y entender cuándo se ejecuta realmente cada recorrido."
quiz:
  - question: "¿Cuándo se ejecuta el cuerpo de un método que usa yield return?"
    options:
      - "Cuando se declara el método"
      - "Cuando se recorre la secuencia devuelta"
      - "Cuando se compila el proyecto"
      - "Solo cuando se llama a ToList"
    answer: 1
    explanation: "Los iteradores son perezosos: el código se ejecuta a medida que el consumidor pide cada elemento con MoveNext."
  - question: "¿Qué hace yield break dentro de un iterador?"
    options:
      - "Reinicia la secuencia desde el principio"
      - "Devuelve el último elemento dos veces"
      - "Termina la secuencia y no entrega más elementos"
      - "Lanza una excepción"
    answer: 2
    explanation: "yield break finaliza el iterador en ese punto; el consumidor deja de recibir elementos."
  - question: "¿Qué interfaz garantiza poder recorrer una secuencia con foreach?"
    options:
      - "IEnumerable<T>"
      - "IReadOnlyList<T>"
      - "Dictionary<TKey, TValue>"
      - "Action<T>"
    answer: 0
    explanation: "IEnumerable<T> expone GetEnumerator, que es lo que necesita foreach. IReadOnlyList<T> agrega índice y cantidad, pero no es la base del recorrido."
---

# IEnumerable e iteradores

## Objetivo

Entender qué representa `IEnumerable<T>`, escribir métodos que produzcan elementos con `yield return` y reconocer cuándo una secuencia se ejecuta de verdad.

## Requisitos previos

Necesitás manejar `List<T>` y `foreach`, vistos en la lección anterior. También ayuda recordar métodos y parámetros del módulo 2.

## Concepto

`IEnumerable<T>` es la abstracción más importante de las colecciones en .NET. Representa algo que se puede recorrer elemento por elemento, sin prometer que sea una lista, un array o un diccionario. Solo promete que existe un enumerador para pedir el siguiente elemento.

Cuando escribís `foreach (var item in coleccion)`, el compilador pide un enumerador y llama a `MoveNext` hasta que devuelve `false`. Ese mecanismo funciona con cualquier tipo que implemente `IEnumerable<T>`, incluidos `List<T>`, los arrays y las consultas de LINQ.

La interfaz no dice nada sobre el orden, la cantidad ni el acceso por índice. Por eso `IEnumerable<T>` es ideal para aceptar datos de entrada: tu método no obliga a quien lo llama a construir una lista. Si necesitás cantidad e índice, usá `IReadOnlyList<T>`; si necesitás buscar por clave, `IReadOnlyDictionary<TKey, TValue>`.

Un iterador es un método que devuelve `IEnumerable<T>` y usa `yield return`. Cada vez que el consumidor pide un elemento, el método avanza hasta el próximo `yield return`, entrega el valor y queda pausado. No se genera toda la secuencia de golpe. `yield break` corta la secuencia antes de terminar el bucle.

Esa pausa es la base de la ejecución diferida: podés describir una secuencia costosa y pagarla solo cuando la recorrés. También permite trabajar con secuencias muy grandes, o incluso infinitas, siempre que el consumidor corte en algún momento.

## Ejemplo mínimo

```csharp
static IEnumerable<int> ContarHasta(int limite)
{
    for (int numero = 1; numero <= limite; numero++)
    {
        yield return numero;
    }
}

foreach (int numero in ContarHasta(3))
{
    Console.WriteLine(numero);
}
```

```text
1
2
3
```

El método `ContarHasta` no crea una lista. Entrega un número, espera a que se lo pidan de nuevo y continúa desde donde quedó.

## Ejemplo aplicado

En la tienda, un pedido guarda el id del cliente. Queremos recorrer solo los pedidos entregados de un cliente, sin construir una lista intermedia.

```csharp
static IEnumerable<Pedido> EntregadosDe(IEnumerable<Pedido> pedidos, int clienteId)
{
    foreach (Pedido pedido in pedidos)
    {
        if (pedido.ClienteId == clienteId && pedido.Estado == EstadoPedido.Entregado)
        {
            yield return pedido;
        }
    }
}

var pedidos = new List<Pedido>
{
    new(100, 1, EstadoPedido.Entregado, [new(new Producto(10, "Teclado", 45_000m), 2)]),
    new(101, 1, EstadoPedido.Pendiente, [new(new Producto(12, "Monitor", 320_000m), 1)]),
    new(102, 1, EstadoPedido.Entregado, [new(new Producto(11, "Mouse", 18_000m), 1)])
};

foreach (Pedido pedido in EntregadosDe(pedidos, clienteId: 1))
{
    decimal total = 0m;
    foreach (LineaPedido linea in pedido.Lineas)
    {
        total += linea.Subtotal;
    }
    Console.WriteLine($"Pedido {pedido.Id}: {total}");
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
Pedido 100: 90000
Pedido 102: 18000
```

La llamada a `EntregadosDe` es instantánea: todavía no recorrió nada. El trabajo ocurre dentro del `foreach`, a medida que se piden los pedidos.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Recorrer dos veces una secuencia costosa | Cada `foreach` reinicia el iterador y vuelve a ejecutar el trabajo | Materializar una vez con `ToList` o guardar el resultado |
| Esperar una excepción de validación al llamar al método | El cuerpo del iterador se ejecuta recién al enumerar | Validar en un método público que devuelva el iterador interno |
| `InvalidOperationException` al modificar la colección original | El iterador detecta que la colección cambió | Recorrer una copia o terminar el recorrido antes de modificar |
| Devolver `null` para indicar "sin resultados" | Obliga a quien llama a preguntar por `null` | Devolver una secuencia vacía con `Array.Empty<T>()` o `Enumerable.Empty<T>()` |
| Pensar que `IEnumerable<T>` tiene índice o cantidad | Es solo la abstracción de recorrido | Usar `IReadOnlyList<T>` o materializar la secuencia |

## Buenas prácticas

- Aceptá `IEnumerable<T>` cuando solo necesites recorrer; es la firma más flexible para quien llama.
- Devolvé `IReadOnlyList<T>` o `IReadOnlyCollection<T>` cuando ya materializaste el resultado.
- Separá la validación del iterador: un método público valida y delega en un método privado con `yield return`.
- No enumeres dos veces la misma consulta sin saber cuánto cuesta. En la lección 7 vas a profundizar en este tema.
- Mantené los iteradores chicos y con una sola responsabilidad; son fáciles de componer.

## Ejercicio

Escribí un método genérico `Paginar<T>` que reciba una secuencia, un número de página y un tamaño, y devuelva solo los elementos de esa página. La primera página es la 1. Validá los argumentos antes de comenzar a iterar y probalo con una lista de productos.

<details>
<summary>Ver solución</summary>

```csharp
static IEnumerable<T> Paginar<T>(IEnumerable<T> origen, int pagina, int tamano)
{
    ArgumentNullException.ThrowIfNull(origen);
    if (pagina < 1)
    {
        throw new ArgumentOutOfRangeException(nameof(pagina));
    }
    if (tamano < 1)
    {
        throw new ArgumentOutOfRangeException(nameof(tamano));
    }

    return PaginarInterno(origen, pagina, tamano);
}

static IEnumerable<T> PaginarInterno<T>(IEnumerable<T> origen, int pagina, int tamano)
{
    int desde = (pagina - 1) * tamano;
    int indice = 0;
    int entregados = 0;

    foreach (T item in origen)
    {
        if (indice++ < desde)
        {
            continue;
        }

        if (entregados >= tamano)
        {
            yield break;
        }

        entregados++;
        yield return item;
    }
}

var productos = new[] { "Teclado", "Mouse", "Monitor", "Notebook", "Webcam", "Auriculares" };

foreach (string producto in Paginar(productos, pagina: 2, tamano: 3))
{
    Console.WriteLine(producto);
}
```

```text
Notebook
Webcam
Auriculares
```

La validación se ejecuta al llamar a `Paginar`. El recorrido real ocurre en `PaginarInterno`, cuando el `foreach` pide los elementos.

</details>

## Resumen

- `IEnumerable<T>` representa una secuencia recorrible, no una lista con índice.
- `yield return` entrega un elemento y pausa el iterador; `yield break` termina la secuencia.
- El código de un iterador se ejecuta durante la enumeración, no al invocarlo.
- Validá antes de iterar y evitá enumerar dos veces la misma secuencia.

## Siguiente paso

Las secuencias y las colecciones pueden guardar cualquier tipo, pero escribirlas una y otra vez para cada tipo sería repetitivo. En la próxima lección vas a ver cómo resolverlo con [genéricos](/modulos/colecciones-linq/genericos).
