---
slug: lambdas-delegates
title: "Lambdas y delegates"
order: 4
estimatedMinutes: 12
summary: "Pasá comportamiento como argumento con Func, Action y expresiones lambda, y entendé cómo funcionan las closures."
quiz:
  - question: "¿Qué delegate representa una función que recibe un producto y devuelve true o false?"
    options:
      - "Action<Producto>"
      - "Func<Producto>"
      - "Predicate<Producto>"
      - "Func<bool, Producto>"
    answer: 2
    explanation: "Predicate<T> es un delegate que recibe un T y devuelve bool. Func<Producto, bool> también serviría, pero Predicate<T> es el tipo específico para predicados."
  - question: "¿Qué significa Func<decimal, decimal>?"
    options:
      - "Una función que recibe dos decimales"
      - "Una función que recibe un decimal y devuelve un decimal"
      - "Un action sin valor de retorno"
      - "Una lista de decimales"
    answer: 1
    explanation: "En Func<T, TResult>, el último parámetro de tipo es el resultado. Los anteriores son los argumentos."
  - question: "¿Qué captura una lambda cuando usa una variable local del método que la contiene?"
    options:
      - "Una copia inmutable del valor en ese momento"
      - "La variable, de modo que ve sus cambios posteriores"
      - "Nada; el código no compila"
      - "Solo el valor si la variable es de tipo string"
    answer: 1
    explanation: "La closure captura la variable, no una foto del valor. Si la variable cambia antes de ejecutar la lambda, la lambda ve el nuevo valor."
---

# Lambdas y delegates

## Objetivo

Entender qué es un delegate, escribir lambdas con `Func` y `Action`, y usar closures sin sorpresas. Estas piezas son la base de LINQ y de muchas APIs de .NET.

## Requisitos previos

Necesitás manejar métodos, parámetros y colecciones. Los genéricos de la lección anterior ayudan a leer `Func<T, TResult>` sin que los ángulos asusten.

## Concepto

Un delegate es un tipo que describe la firma de un método. Así como `int` describe un número, `Func<decimal, decimal>` describe "algo que recibe un decimal y devuelve un decimal". Una variable de ese tipo puede apuntar a un método nombrado o a una lambda.

`Func<T, TResult>` es el delegate genérico para funciones que devuelven un valor. El último parámetro de tipo es el resultado. `Func<int, string>` recibe un entero y devuelve un texto; `Func<decimal, decimal, decimal>` recibe dos decimales y devuelve uno.

`Action<T>` es el delegate para métodos que no devuelven nada. Se usa para efectos: registrar un pedido, mostrar un mensaje, guardar en un log. `Predicate<T>` es un caso especial que recibe un `T` y devuelve `bool`; es el tipo que espera `List<T>.FindAll`.

Una expresión lambda es una función anónima y compacta: `precio => precio * 0.9m`. La parte izquierda son los parámetros y la derecha es el cuerpo. Si el cuerpo necesita varias líneas, usá llaves: `precio => { var final = precio * 0.9m; return final; }`.

Las lambdas pueden capturar variables del método que las rodea. Eso se llama closure. La lambda no guarda una copia del valor: guarda la variable. Si esa variable cambia antes de ejecutar la lambda, la lambda ve el valor nuevo. Es potente para configurar callbacks, pero exige cuidado cuando la variable se modifica en un bucle o en otro hilo.

## Ejemplo mínimo

```csharp
Func<int, int> duplicar = numero => numero * 2;
Func<decimal, decimal, decimal> sumar = (a, b) => a + b;
Action<string> mostrar = texto => Console.WriteLine(texto);

mostrar($"El doble de 21 es {duplicar(21)}");
mostrar($"45.000 + 18.000 = {sumar(45_000m, 18_000m)}");
```

```text
El doble de 21 es 42
45.000 + 18.000 = 63000
```

## Ejemplo aplicado

En el catálogo queremos filtrar productos caros, ordenarlos de mayor a menor y calcular un precio final combinando descuento e impuesto.

```csharp
var productos = new List<Producto>
{
    new(10, "Teclado", 45_000m),
    new(11, "Mouse", 18_000m),
    new(12, "Monitor", 320_000m),
    new(13, "Notebook", 1_250_000m)
};

Predicate<Producto> esCaro = producto => producto.Precio > 100_000m;
List<Producto> caros = productos.FindAll(esCaro);
caros.Sort((a, b) => b.Precio.CompareTo(a.Precio));

foreach (Producto producto in caros)
{
    Console.WriteLine($"{producto.Nombre}: {producto.Precio}");
}

static Func<decimal, decimal> Componer(
    Func<decimal, decimal> primero,
    Func<decimal, decimal> segundo)
{
    return valor => segundo(primero(valor));
}

Func<decimal, decimal> descuento = total => total * 0.9m;
Func<decimal, decimal> impuesto = total => total * 1.21m;
Func<decimal, decimal> precioFinal = Componer(descuento, impuesto);

Console.WriteLine($"Total final: {precioFinal(100_000m)}");

public sealed record Producto(int Id, string Nombre, decimal Precio);
```

```text
Notebook: 1250000
Monitor: 320000
Total final: 108900.00
```

`FindAll` recibe un `Predicate<Producto>`; `Sort` recibe un `Comparison<Producto>`, que es otra forma de delegate: recibe dos elementos y devuelve un entero negativo, cero o positivo. `Componer` devuelve una nueva lambda que ejecuta primero un paso y después el otro.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `var f = x => x * 2;` no compila | La lambda no tiene un tipo destino del que inferir parámetros | Declarar `Func<int, int> f = x => x * 2;` |
| Todas las lambdas de un bucle ven el mismo valor | Capturan la variable del bucle, no una copia | Guardar una copia local dentro del bucle |
| La lambda modifica una lista y el resultado sorprende | La closure captura estado mutable | Preferir lambdas puras y sin efectos secundarios |
| `Action` no devuelve nada y se intenta usar su resultado | `Action` es `void` | Usar `Func` si necesitás un valor de retorno |
| El código se vuelve ilegible con lambdas enormes | Se metió lógica compleja dentro de una expresión | Extraer un método con nombre descriptivo |
| Se crean delegates en un bucle muy caliente | Cada lambda puede generar una asignación | Reutilizar el delegate o usar `static` cuando no capture variables |

## Buenas prácticas

- Usá `Func` y `Action` en lugar de declarar un delegate propio, salvo que el nombre aporte significado de dominio.
- Mantené las lambdas cortas. Si ocupan más de tres o cuatro líneas, probablemente merezcan un método.
- No pongas efectos secundarios en predicados: un `Predicate<T>` debería responder una pregunta, no modificar datos.
- Usá method groups cuando ya existe un método: `Action<string> log = Console.WriteLine;`.
- Marcá como `static` las lambdas que no capturan variables: `Func<int, int> f = static x => x * 2;`. Así el compilador evita generar una closure innecesaria.

## Ejercicio

Escribí un método `Componer` que reciba varios pasos `Func<decimal, decimal>` y devuelva una función que los aplique en orden. Usalo para aplicar un descuento del 10 %, otro del 5 % y luego un IVA del 21 % a un total de 200.000.

<details>
<summary>Ver solución</summary>

```csharp
static Func<decimal, decimal> Componer(params Func<decimal, decimal>[] pasos)
{
    return valor =>
    {
        decimal actual = valor;
        foreach (Func<decimal, decimal> paso in pasos)
        {
            actual = paso(actual);
        }
        return actual;
    };
}

Func<decimal, decimal> descuentoDiez = total => total * 0.90m;
Func<decimal, decimal> descuentoCinco = total => total * 0.95m;
Func<decimal, decimal> iva = total => total * 1.21m;

Func<decimal, decimal> precioFinal = Componer(descuentoDiez, descuentoCinco, iva);

Console.WriteLine($"Precio final: {precioFinal(200_000m)}");
```

```text
Precio final: 206910.00
```

El orden importa: primero se aplican los descuentos y después el impuesto. Si se cambiara el orden, el resultado sería distinto.

</details>

## Resumen

- Un delegate es un tipo que representa una firma de método.
- `Func<T, TResult>` devuelve un valor; `Action<T>` no devuelve nada.
- Las lambdas son funciones anónimas que se convierten al delegate que espera el contexto.
- Las closures capturan variables, no copias, y eso puede cambiar el resultado si la variable se modifica.

## Siguiente paso

Con lambdas ya tenés la pieza que falta para escribir consultas declarativas sobre colecciones. En la próxima lección vas a usar [LINQ: Where, Select y OrderBy](/modulos/colecciones-linq/linq-basico).
