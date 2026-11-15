---
slug: bucles
title: "Bucles"
order: 5
estimatedMinutes: 15
summary: "Repetir instrucciones con for, while, do-while y foreach, controlando cuándo terminar."
quiz:
  - question: "¿Qué bucle garantiza que su cuerpo se ejecute al menos una vez?"
    options:
      - "for"
      - "while"
      - "do-while"
      - "foreach"
    answer: 2
    explanation: "do-while evalúa la condición después de ejecutar el cuerpo, por eso siempre entra una vez."
  - question: "¿Qué hace la instrucción break dentro de un bucle?"
    options:
      - "Salta a la siguiente iteración"
      - "Termina el bucle por completo"
      - "Reinicia el contador"
      - "Lanza una excepción"
    answer: 1
    explanation: "break corta la ejecución del bucle y continúa con la instrucción siguiente."
  - question: "¿Cuántas veces se ejecuta el cuerpo de for (int i = 0; i < 3; i++)?"
    options:
      - "2"
      - "3"
      - "4"
      - "Infinitas"
    answer: 1
    explanation: "i toma los valores 0, 1 y 2. Cuando llega a 3, la condición i < 3 es falsa y el bucle termina."
---

# Bucles

## Objetivo

Al terminar esta lección vas a poder repetir instrucciones con `for`, `while`, `do-while` y `foreach`. También vas a saber cortar un bucle, saltear una iteración y evitar los errores más comunes.

## Requisitos previos

Haber leído [Condicionales](/modulos/csharp-desde-cero/condicionales). Necesitás manejar condiciones, comparaciones y validación de datos.

## Concepto

### Para qué sirven los bucles

Un bucle repite un bloque de código mientras se cumpla una condición o para cada elemento de una colección. Sin bucles, procesar cien notas requeriría cien líneas casi iguales.

### `for`: cuando sabés cuántas veces repetir

El `for` tiene tres partes: inicialización, condición y actualización.

```csharp
for (int i = 1; i <= 5; i++)
{
    Console.WriteLine($"Vuelta {i}");
}
```

- `int i = 1` se ejecuta una sola vez, al principio.
- `i <= 5` se evalúa antes de cada vuelta. Si es verdadera, entra; si es falsa, termina.
- `i++` se ejecuta al final de cada vuelta.

El contador `i` suele empezar en `0` cuando recorrés índices, porque los arrays y las listas empiezan en cero.

### `while`: cuando la cantidad de vueltas no se conoce

`while` evalúa la condición antes de cada iteración:

```csharp
int numero = 1;

while (numero <= 5)
{
    Console.WriteLine(numero);
    numero++;
}
```

Si la condición es falsa al principio, el cuerpo no se ejecuta nunca. Es útil para leer datos hasta que el usuario ingrese algo válido o hasta que se cumpla una condición externa.

### `do-while`: al menos una vuelta

`do-while` evalúa la condición después de ejecutar el cuerpo. Por eso siempre entra al menos una vez:

```csharp
int opcion;

do
{
    Console.WriteLine("1. Saludar");
    Console.WriteLine("2. Salir");
    Console.Write("Opción: ");
    string? entrada = Console.ReadLine();
    int.TryParse(entrada, out opcion);
}
while (opcion != 2);
```

Es el formato natural para menús: mostrás las opciones, leés la elección y repetís hasta que el usuario decida salir.

### `foreach`: recorrer una colección

`foreach` recorre todos los elementos de un array o una lista sin manejar índices:

```csharp
string[] nombres = { "Ana", "Luis", "Marta" };

foreach (string nombre in nombres)
{
    Console.WriteLine(nombre);
}
```

Es más legible que un `for` cuando no necesitás la posición. Si necesitás el índice, usá `for`.

### `break` y `continue`

`break` termina el bucle por completo. `continue` salta a la siguiente iteración:

```csharp
for (int i = 1; i <= 10; i++)
{
    if (i == 4)
    {
        continue; // no imprime el 4
    }

    if (i == 7)
    {
        break; // corta antes de imprimir el 7
    }

    Console.WriteLine(i);
}
```

Salida:

```text
1
2
3
5
6
```

No abuses de `break` y `continue`: si un bucle tiene demasiados saltos, probablemente se pueda dividir en métodos más claros.

### Bucles anidados

Un bucle dentro de otro recorre combinaciones. Por ejemplo, una tabla de multiplicar:

```csharp
for (int fila = 1; fila <= 3; fila++)
{
    for (int columna = 1; columna <= 3; columna++)
    {
        Console.Write($"{fila * columna,4}");
    }

    Console.WriteLine();
}
```

Salida:

```text
   1   2   3
   2   4   6
   3   6   9
```

El bucle interno se ejecuta completo por cada vuelta del externo. Si anidás tres o cuatro niveles, revisá si hay una forma más simple de resolverlo.

### Cuidado con los bucles infinitos

Un bucle infinito ocurre cuando la condición nunca se vuelve falsa. El caso clásico es olvidar actualizar el contador:

```csharp
int i = 0;

while (i < 5)
{
    Console.WriteLine(i);
    // Falta i++;
}
```

También puede pasar si usás `continue` antes de la actualización. Si el programa se queda colgado, revisá qué variable debería cambiar y no está cambiando.

## Ejemplo mínimo

```csharp
int suma = 0;

for (int i = 1; i <= 5; i++)
{
    suma += i;
}

Console.WriteLine(suma);
```

Salida esperada:

```text
15
```

## Ejemplo aplicado

Validar una edad hasta que el usuario ingrese un valor correcto es uno de los usos más frecuentes de `while`:

```csharp
int edad = -1;

while (edad < 0 || edad > 120)
{
    Console.Write("Edad (0-120): ");
    string? entrada = Console.ReadLine();

    if (!int.TryParse(entrada, out edad))
    {
        Console.WriteLine("Ingresá un número válido.");
        edad = -1;
    }
}

Console.WriteLine($"Edad registrada: {edad}");
```

La variable se inicializa en `-1` para que la condición sea verdadera la primera vez. Si la conversión falla, se vuelve a asignar `-1` y el bucle continúa. Si el usuario cierra la entrada, `ReadLine` devuelve `null`, `TryParse` falla y el programa sigue pidiendo: en una aplicación real conviene agregar una salida explícita.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El bucle no termina | La condición nunca se vuelve falsa | Revisar que el contador o el estado cambien |
| Se recorre un elemento de más | Se usó `<=` con un índice que empieza en `0` | Usar `<` con la longitud |
| Se saltea una iteración sin querer | `continue` antes de la actualización | Actualizar el contador antes del `continue` |
| Se modifica una colección mientras se recorre | `foreach` no permite cambiar la colección | Recorrer una copia o usar `for` |
| El menú no sale nunca | La opción se lee fuera del bucle | Leer la opción dentro del `do-while` |

## Buenas prácticas

- Usá `for` cuando sabés cuántas vueltas hay o necesitás el índice.
- Usá `foreach` cuando solo te interesa cada elemento.
- Usá `while` para condiciones que no dependen de un contador fijo.
- Usá `do-while` para menús y validaciones que deben ejecutarse al menos una vez.
- Poné un límite de intentos cuando leés datos del usuario.
- Extraé el cuerpo de un bucle largo a un método con nombre.

## Ejercicio

Mostrá los números del 1 al 30. Si el número es divisible por 3, mostrá `Fizz`; si es divisible por 5, mostrá `Buzz`; si es divisible por ambos, mostrá `FizzBuzz`. En los demás casos, mostrá el número.

<details>
<summary>Ver solución</summary>

```csharp
for (int i = 1; i <= 30; i++)
{
    if (i % 3 == 0 && i % 5 == 0)
    {
        Console.WriteLine("FizzBuzz");
    }
    else if (i % 3 == 0)
    {
        Console.WriteLine("Fizz");
    }
    else if (i % 5 == 0)
    {
        Console.WriteLine("Buzz");
    }
    else
    {
        Console.WriteLine(i);
    }
}
```

</details>

## Resumen

- `for` sirve para repetir una cantidad conocida de veces.
- `while` evalúa la condición antes de cada vuelta; `do-while` la evalúa después.
- `foreach` recorre colecciones sin manejar índices.
- `break` corta el bucle y `continue` salta a la siguiente iteración.
- Todo bucle necesita una condición de salida que pueda volverse falsa.

## Siguiente paso

En [Arrays y listas](/modulos/csharp-desde-cero/arrays-listas) vas a guardar muchos valores en una sola estructura y recorrerlos con los bucles que acabás de aprender.
