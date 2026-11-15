---
slug: ejercicio-integrador
title: "Ejercicio integrador y quiz"
order: 10
estimatedMinutes: 25
summary: "Un programa completo que combina variables, condicionales, bucles, listas, métodos y nulos."
quiz:
  - question: "¿Qué colección conviene cuando no sabés cuántos elementos vas a guardar?"
    options:
      - "Un array de tamaño fijo"
      - "Una List<T>"
      - "Un string"
      - "Un bool"
    answer: 1
    explanation: "List<T> puede crecer y reducirse durante la ejecución, por eso sirve cuando la cantidad no se conoce de antemano."
  - question: "¿Qué indica el tipo string? en la firma de un método?"
    options:
      - "Que el método siempre devuelve texto"
      - "Que el resultado puede ser null"
      - "Que el método no devuelve nada"
      - "Que el texto es inmutable"
    answer: 1
    explanation: "El signo ? indica que el valor puede faltar. Quien llama debe comprobarlo antes de usarlo."
  - question: "¿Cuál es la forma segura de convertir a número un dato ingresado por consola?"
    options:
      - "int.Parse"
      - "Convert.ToInt32"
      - "int.TryParse"
      - "Un cast directo"
    answer: 2
    explanation: "TryParse devuelve false si el texto no es válido y evita una excepción."
---

# Ejercicio integrador y quiz

## Objetivo

Al terminar esta lección vas a haber escrito un programa completo de consola que lee datos, los valida, los guarda en listas, aplica condicionales y bucles, delega lógica en métodos y maneja valores nulos. Es el cierre del módulo: todo lo anterior trabajando junto.

## Requisitos previos

Haber leído las lecciones anteriores del módulo, en especial [Entrada y salida por consola](/modulos/csharp-desde-cero/entrada-salida-consola), [Métodos y parámetros](/modulos/csharp-desde-cero/metodos) y [Nullable reference types](/modulos/csharp-desde-cero/nullable-reference-types).

## Concepto

Un programa real no usa una sola idea aislada. Lee datos, decide qué hacer con ellos, repite tareas y organiza la lógica en métodos. Este es el mapa de lo que vas a combinar:

- **Variables y tipos** para representar nombres, notas e importes.
- **Condicionales** para validar datos y clasificar resultados.
- **Bucles** para leer hasta que el usuario termine y para recorrer colecciones.
- **Listas** para guardar una cantidad variable de elementos.
- **Métodos** para separar cálculo, búsqueda y presentación.
- **Nulos** para representar entradas ausentes y resultados que pueden no existir.

La clave es dividir el problema en pasos chicos. Primero leés y validás. Después calculás. Al final mostrás. Cada paso puede ser un método o un bloque con una responsabilidad clara.

## Ejemplo mínimo

Este programa pide un nombre y una nota, valida ambos datos y usa un método para clasificar el resultado.

```csharp
Console.Write("Nombre: ");
string? nombre = Console.ReadLine();

if (string.IsNullOrWhiteSpace(nombre))
{
    Console.WriteLine("El nombre es obligatorio.");
    return;
}

Console.Write("Nota (0-10): ");
string? entrada = Console.ReadLine();

if (!double.TryParse(entrada, out double nota) || nota is < 0 or > 10)
{
    Console.WriteLine("La nota no es válida.");
    return;
}

Console.WriteLine($"{nombre.Trim()}: {Clasificar(nota)}");

static string Clasificar(double nota) => nota switch
{
    >= 8 => "Excelente",
    >= 6 => "Aprobado",
    _ => "A recuperatorio"
};
```

El ejemplo ya tiene las tres partes básicas: entrada validada, decisión y salida. El método `Clasificar` no sabe nada de la consola; solo recibe un número y devuelve un texto.

## Ejemplo aplicado

Un curso necesita cargar alumnos y notas hasta que el usuario deje el nombre vacío. Después muestra la cantidad de alumnos, el promedio, el mejor alumno y el estado general.

```csharp
var nombres = new List<string>();
var notas = new List<double>();

while (true)
{
    Console.Write("Nombre del alumno (Enter para terminar): ");
    string? nombre = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(nombre))
    {
        break;
    }

    double nota;

    while (true)
    {
        Console.Write($"Nota de {nombre.Trim()} (0-10): ");
        string? entrada = Console.ReadLine();

        if (double.TryParse(entrada, out nota) && nota is >= 0 and <= 10)
        {
            break;
        }

        Console.WriteLine("Nota inválida. Probá de nuevo.");
    }

    nombres.Add(nombre.Trim());
    notas.Add(nota);
}

if (nombres.Count == 0)
{
    Console.WriteLine("No se cargaron alumnos.");
    return;
}

double promedio = CalcularPromedio(notas);
string? mejor = ObtenerMejorAlumno(nombres, notas);

Console.WriteLine();
Console.WriteLine($"Alumnos: {nombres.Count}");
Console.WriteLine($"Promedio: {promedio:F2}");
Console.WriteLine($"Mejor alumno: {mejor ?? "Sin datos"}");
Console.WriteLine($"Estado del curso: {Clasificar(promedio)}");

static double CalcularPromedio(List<double> notas)
{
    double suma = 0;

    foreach (double nota in notas)
    {
        suma += nota;
    }

    return suma / notas.Count;
}

static string? ObtenerMejorAlumno(List<string> nombres, List<double> notas)
{
    if (nombres.Count == 0 || nombres.Count != notas.Count)
    {
        return null;
    }

    int indiceMejor = 0;

    for (int i = 1; i < notas.Count; i++)
    {
        if (notas[i] > notas[indiceMejor])
        {
            indiceMejor = i;
        }
    }

    return nombres[indiceMejor];
}

static string Clasificar(double promedio) => promedio switch
{
    >= 8 => "Excelente",
    >= 6 => "Aprobado",
    _ => "A recuperatorio"
};
```

Puntos para revisar:

- `Console.ReadLine()` devuelve `string?`; por eso se valida con `IsNullOrWhiteSpace` antes de usar `Trim`.
- El bucle externo termina cuando el nombre está vacío.
- El bucle interno no deja avanzar hasta que la nota sea válida.
- Las listas mantienen nombres y notas en el mismo orden.
- `ObtenerMejorAlumno` devuelve `string?` porque puede no haber datos.
- La clasificación final usa una expresión `switch` con patrones.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| División por cero al calcular el promedio | La lista está vacía | Comprobar `Count == 0` antes de dividir |
| Nombres y notas quedan desalineados | Se agregó a una lista pero no a la otra | Agregar ambos datos en el mismo bloque |
| El programa falla con una nota vacía | Se usó `Parse` en lugar de `TryParse` | Validar con `TryParse` |
| Se muestra `null` en la salida | No se manejó el resultado anulable | Usar `??` o comprobar con `is not null` |
| El bucle no termina | La condición de salida no cambia | Revisar la lectura y el `break` |
| El promedio da un entero | Se dividieron dos `int` | Convertir a `double` o usar notas `double` |

## Buenas prácticas

- Validá cada dato apenas entra y no sigas con valores inválidos.
- Separá lectura, cálculo y presentación en métodos distintos.
- Usá listas cuando la cantidad de elementos no se conoce de antemano.
- Devolvé `null` solo cuando la ausencia de resultado tenga sentido.
- Probá el programa con listas vacías, entradas inválidas y valores límite.
- Mantené los nombres de variables en plural para las colecciones y en singular para los elementos.

## Ejercicio

Creá un control de gastos. Pedí una descripción y un monto mayor que cero hasta que la descripción esté vacía. Guardá los datos en dos listas, calculá el total y el promedio, encontrá el gasto mayor y clasificá el total en “Gasto bajo”, “Gasto medio” o “Gasto alto”. Usá métodos y manejá el caso de que no se cargue ningún gasto.

<details>
<summary>Ver solución</summary>

```csharp
var descripciones = new List<string>();
var montos = new List<decimal>();

while (true)
{
    Console.Write("Descripción del gasto (Enter para terminar): ");
    string? descripcion = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(descripcion))
    {
        break;
    }

    decimal monto;

    while (true)
    {
        Console.Write($"Monto de {descripcion.Trim()}: ");
        string? entrada = Console.ReadLine();

        if (decimal.TryParse(entrada, out monto) && monto > 0)
        {
            break;
        }

        Console.WriteLine("Monto inválido. Ingresá un número mayor que cero.");
    }

    descripciones.Add(descripcion.Trim());
    montos.Add(monto);
}

if (montos.Count == 0)
{
    Console.WriteLine("No se cargaron gastos.");
    return;
}

decimal total = CalcularTotal(montos);
decimal promedio = total / montos.Count;
string? mayor = ObtenerMayor(descripciones, montos);

Console.WriteLine();
Console.WriteLine($"Gastos: {montos.Count}");
Console.WriteLine($"Total: {total:F2}");
Console.WriteLine($"Promedio: {promedio:F2}");
Console.WriteLine($"Mayor gasto: {mayor ?? "Sin datos"}");
Console.WriteLine($"Clasificación: {Clasificar(total)}");

static decimal CalcularTotal(List<decimal> montos)
{
    decimal total = 0m;

    foreach (decimal monto in montos)
    {
        total += monto;
    }

    return total;
}

static string? ObtenerMayor(List<string> descripciones, List<decimal> montos)
{
    if (descripciones.Count == 0 || descripciones.Count != montos.Count)
    {
        return null;
    }

    int indiceMayor = 0;

    for (int i = 1; i < montos.Count; i++)
    {
        if (montos[i] > montos[indiceMayor])
        {
            indiceMayor = i;
        }
    }

    return descripciones[indiceMayor];
}

static string Clasificar(decimal total) => total switch
{
    >= 100000m => "Gasto alto",
    >= 50000m => "Gasto medio",
    _ => "Gasto bajo"
};
```

</details>

## Resumen

- Un programa completo combina entrada, validación, cálculo y salida.
- Las listas guardan datos cuya cantidad no se conoce de antemano.
- Los métodos separan responsabilidades y hacen el código más fácil de probar.
- `string?` y `??` permiten representar y manejar resultados ausentes.
- Probar con datos inválidos y colecciones vacías es parte del trabajo.

## Siguiente paso

Terminaste el módulo de C# desde cero. En [Programación orientada a objetos](/modulos/poo) vas a aprender a modelar problemas con clases, propiedades e interfaces.
