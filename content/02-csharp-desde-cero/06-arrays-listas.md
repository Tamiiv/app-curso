---
slug: arrays-listas
title: "Arrays y listas (introducción)"
order: 6
estimatedMinutes: 15
summary: "Guardar varios valores en arrays de tamaño fijo y listas que crecen, y recorrerlos con bucles."
quiz:
  - question: "¿Cuál es el índice del primer elemento de un array en C#?"
    options:
      - "1"
      - "0"
      - "-1"
      - "Depende del tamaño"
    answer: 1
    explanation: "Los arrays y las listas en C# empiezan en el índice 0."
  - question: "¿Qué diferencia principal hay entre un array y una List<T>?"
    options:
      - "El array solo guarda números"
      - "La lista tiene tamaño fijo y el array no"
      - "El array tiene tamaño fijo y la lista puede crecer"
      - "No hay ninguna diferencia"
    answer: 2
    explanation: "Un array se crea con una cantidad fija de elementos; una List<T> puede agregar y quitar elementos."
  - question: "¿Qué propiedad devuelve la cantidad de elementos de una List<T>?"
    options:
      - "Length"
      - "Size"
      - "Count"
      - "Total"
    answer: 2
    explanation: "Las listas usan Count. Los arrays usan Length."
---

# Arrays y listas (introducción)

## Objetivo

Al terminar esta lección vas a poder guardar varios valores en un array o en una lista, acceder a ellos por índice y recorrerlos con bucles. También vas a saber cuándo conviene cada estructura.

## Requisitos previos

Haber leído [Bucles](/modulos/csharp-desde-cero/bucles). Necesitás manejar `for`, `foreach` y variables con tipos.

## Concepto

### Arrays: una cantidad fija de elementos

Un array guarda varios valores del mismo tipo en una sola variable. Su tamaño se define al crearlo y no cambia:

```csharp
int[] notas = new int[4];
notas[0] = 8;
notas[1] = 6;
notas[2] = 10;
notas[3] = 7;
```

También podés inicializarlo con valores:

```csharp
int[] notas = { 8, 6, 10, 7 };
string[] nombres = { "Ana", "Luis", "Marta" };
```

Cada elemento se identifica por un índice que empieza en `0`. El primer elemento es `notas[0]` y el último es `notas[notas.Length - 1]`.

```csharp
Console.WriteLine(notas[0]);              // 8
Console.WriteLine(notas.Length);          // 4
Console.WriteLine(notas[notas.Length - 1]); // 7
```

Si intentás acceder a un índice que no existe, se lanza `IndexOutOfRangeException`.

### Recorrer un array

Con `for` podés usar el índice:

```csharp
for (int i = 0; i < notas.Length; i++)
{
    Console.WriteLine($"Nota {i + 1}: {notas[i]}");
}
```

Con `foreach` recorrés los valores sin preocuparte por la posición:

```csharp
foreach (int nota in notas)
{
    Console.WriteLine(nota);
}
```

### Listas: cuando la cantidad cambia

Una `List<T>` es una colección dinámica. Podés agregar y quitar elementos durante la ejecución. `T` es el tipo de los elementos:

```csharp
var nombres = new List<string>();
nombres.Add("Ana");
nombres.Add("Luis");
nombres.Add("Marta");

Console.WriteLine(nombres.Count); // 3
Console.WriteLine(nombres[1]);    // Luis
```

También se puede crear con valores iniciales:

```csharp
var precios = new List<decimal> { 1200m, 3500m, 800m };
```

Las operaciones más usadas son:

| Operación | Qué hace |
|---|---|
| `Add(elemento)` | Agrega al final |
| `Remove(elemento)` | Quita la primera aparición |
| `RemoveAt(indice)` | Quita por posición |
| `Contains(elemento)` | Indica si existe |
| `Count` | Cantidad de elementos |
| `Clear()` | Vacía la lista |
| `Sort()` | Ordena los elementos |

```csharp
var tareas = new List<string> { "Estudiar", "Practicar", "Descansar" };

tareas.Add("Repasar");
tareas.Remove("Descansar");

Console.WriteLine(tareas.Count);          // 3
Console.WriteLine(tareas.Contains("Practicar")); // True
```

### Arrays o listas

- Usá un array cuando la cantidad de elementos es fija y conocida: los días de la semana, las notas de una materia con cuatro exámenes.
- Usá una `List<T>` cuando no sabés cuántos elementos vas a tener o necesitás agregar y quitar: los productos de un carrito, los nombres que ingresa un usuario.

Las listas son más flexibles; los arrays tienen menos sobrecarga. Para empezar, si dudás, usá `List<T>`.

### Expresiones de colección

C# moderno permite crear arrays y listas con corchetes:

```csharp
int[] numeros = [1, 2, 3];
List<string> colores = ["rojo", "verde", "azul"];
```

Es una sintaxis más corta, disponible desde C# 12. Si trabajás con una versión anterior, usá `new[] { ... }` o `new List<T> { ... }`.

### Colecciones y bucles

Las colecciones se llevan muy bien con los bucles. Por ejemplo, para sumar todos los precios de una lista:

```csharp
decimal total = 0m;

foreach (decimal precio in precios)
{
    total += precio;
}

Console.WriteLine($"Total: {total:F2}");
```

Y para contar cuántos elementos cumplen una condición:

```csharp
int aprobadas = 0;

foreach (int nota in notas)
{
    if (nota >= 6)
    {
        aprobadas++;
    }
}
```

## Ejemplo mínimo

```csharp
int[] notas = { 8, 6, 10, 7 };

Console.WriteLine(notas.Length); // 4
Console.WriteLine(notas[0]);     // 8

var nombres = new List<string> { "Ana", "Luis" };
nombres.Add("Marta");

Console.WriteLine(nombres.Count); // 3
Console.WriteLine(nombres[2]);    // Marta
```

## Ejemplo aplicado

Un carrito de compras guarda los precios en una lista, calcula el total y el promedio, y elimina un producto.

```csharp
var precios = new List<decimal> { 1200m, 3500m, 800m };

decimal total = 0m;

foreach (decimal precio in precios)
{
    total += precio;
}

decimal promedio = total / precios.Count;

Console.WriteLine($"Total: {total:F2}");
Console.WriteLine($"Promedio: {promedio:F2}");

precios.Remove(3500m);
Console.WriteLine($"Artículos después de quitar uno: {precios.Count}");
```

Salida esperada:

```text
Total: 5500.00
Promedio: 1833.33
Artículos después de quitar uno: 2
```

`Remove` quita la primera aparición del valor. Si el carrito pudiera tener dos productos con el mismo precio y quisieras quitar uno específico, necesitarías otra estrategia, como quitar por índice.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `IndexOutOfRangeException` | El índice es igual o mayor que `Length` | Recorrer con `i < array.Length` |
| Se usa `Length` en una lista | `List<T>` no tiene `Length` | Usar `Count` |
| Se intenta agregar a un array lleno | El tamaño del array es fijo | Usar `List<T>` |
| `Remove` quita el elemento equivocado | Hay valores duplicados | Quitar por índice con `RemoveAt` |
| Se modifica una lista dentro de `foreach` | La colección cambia mientras se recorre | Recorrer una copia o usar `for` |

## Buenas prácticas

- Usá nombres en plural para colecciones: `nombres`, `precios`, `notas`.
- Verificá que la colección tenga elementos antes de calcular un promedio.
- Preferí `foreach` cuando no necesitás el índice.
- Usá `List<T>` si la cantidad de elementos puede cambiar.
- No uses `Count` como condición de un `for` que modifica la lista: guardá el valor en una variable.
- Extraé a un método el cálculo de totales y promedios cuando el programa crece.

## Ejercicio

Pedí cinco notas enteras, guardalas en un array, calculá el promedio y armá una lista con las notas aprobadas (mayores o iguales a 6). Mostrá el promedio con dos decimales y las notas aprobadas separadas por comas.

<details>
<summary>Ver solución</summary>

```csharp
int[] notas = new int[5];

for (int i = 0; i < notas.Length; i++)
{
    Console.Write($"Nota {i + 1}: ");
    string? entrada = Console.ReadLine();

    if (!int.TryParse(entrada, out notas[i]))
    {
        Console.WriteLine("La nota no es válida.");
        return;
    }
}

int suma = 0;
var aprobadas = new List<int>();

foreach (int nota in notas)
{
    suma += nota;

    if (nota >= 6)
    {
        aprobadas.Add(nota);
    }
}

double promedio = (double)suma / notas.Length;

Console.WriteLine($"Promedio: {promedio:F2}");
Console.WriteLine($"Aprobadas: {string.Join(", ", aprobadas)}");
```

</details>

## Resumen

- Los arrays tienen tamaño fijo y se acceden por índice desde cero.
- `Length` indica la cantidad de elementos de un array; `Count` la de una lista.
- `List<T>` permite agregar y quitar elementos durante la ejecución.
- `foreach` recorre los valores y `for` sirve cuando necesitás el índice.
- Antes de dividir por una cantidad, verificá que la colección no esté vacía.

## Siguiente paso

En [Métodos y parámetros](/modulos/csharp-desde-cero/metodos) vas a agrupar la lógica repetida en funciones con nombre, para que tus programas sean más fáciles de leer y mantener.
