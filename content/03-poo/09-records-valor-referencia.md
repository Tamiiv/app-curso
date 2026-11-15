---
slug: records-valor-referencia
title: "Records y tipos por valor vs. referencia"
order: 9
estimatedMinutes: 15
summary: "Records, igualdad por valor, with y record struct: cuándo usar cada tipo y qué se copia realmente."
quiz:
  - question: "¿Qué igualdad usan los records por defecto?"
    options:
      - "Igualdad por referencia"
      - "Igualdad por valor de sus miembros"
      - "Solo igualdad de tipo"
      - "Ninguna igualdad"
    answer: 1
    explanation: "El compilador genera Equals y == comparando los valores de todas las propiedades del record."
  - question: "¿Qué hace la expresión with en un record?"
    options:
      - "Modifica el record original"
      - "Crea una copia con algunos miembros cambiados"
      - "Elimina el record"
      - "Compara dos records"
    answer: 1
    explanation: "with produce una nueva instancia a partir de la original y reemplaza solo las propiedades indicadas."
  - question: "¿Qué diferencia hay entre record y record struct?"
    options:
      - "record es por referencia y record struct por valor"
      - "record struct no puede tener propiedades"
      - "record no puede tener métodos"
      - "Son exactamente iguales"
    answer: 0
    explanation: "record class (o record a secas) es un tipo por referencia; record struct es un tipo por valor."
---

# Records y tipos por valor vs. referencia

## Objetivo

Vas a entender la diferencia entre tipos por valor y por referencia, y a usar `record` y `record struct` para modelar datos inmutables con igualdad por valor. También vas a saber cuándo conviene un record, cuándo una clase y qué hace exactamente `with`.

## Requisitos previos

Necesitás manejar clases, propiedades, `init` y `required`. Si no los tenés frescos, repasá [Constructores y propiedades](/modulos/poo/constructores-propiedades) y [Composición vs. herencia](/modulos/poo/composicion-herencia).

## Concepto

En C# hay dos grandes familias de tipos:

- **Tipos por valor**: guardan el dato directamente. Asignar una variable a otra copia el valor. Ejemplos: `int`, `bool`, `DateTime`, `record struct`.
- **Tipos por referencia**: guardan una referencia a un objeto. Asignar copia la referencia, no el objeto. Ejemplos: clases, arrays, `string` (con matices) y `record` class.

```csharp
var a = new Libro("El Aleph");
var b = a;              // copia la referencia
b.Titulo = "Otro";      // modifica el mismo objeto
Console.WriteLine(a.Titulo); // Otro
```

Para evitar ese efecto, podés usar un **record**. Un `record` es un tipo por referencia que genera automáticamente:

- Igualdad por valor: `==` y `Equals` comparan las propiedades.
- `ToString()` legible.
- Un constructor posicional si usás la forma corta.
- Soporte para `with`, que crea una copia con cambios.

```csharp
public record LibroResumen(string Titulo, string Autor, int Anio);
```

Ese one-liner equivale a una clase con propiedades `init`, constructor, igualdad y `ToString`. Es ideal para DTOs, mensajes, resultados y objetos de valor.

`record struct` es la versión por valor. Se copia al asignar y no usa el montón administrado para la instancia. `readonly record struct` agrega inmutabilidad: todas sus propiedades son de solo lectura. Es útil para valores chicos, como coordenadas o identificadores.

La igualdad por valor también tiene límites: si un record contiene una lista, la igualdad de esa lista sigue siendo por referencia. Dos records con listas distintas pero con los mismos elementos no son iguales.

## Ejemplo mínimo

```csharp
// LibroResumen.cs
public record LibroResumen(string Titulo, string Autor, int Anio);
```

```csharp
// Program.cs
var original = new LibroResumen("El Aleph", "Jorge Luis Borges", 1949);
var copia = new LibroResumen("El Aleph", "Jorge Luis Borges", 1949);
var editado = original with { Anio = 1952 };

Console.WriteLine(original == copia);   // True
Console.WriteLine(original == editado); // False
Console.WriteLine(editado);
```

Salida:

```text
True
False
LibroResumen { Titulo = El Aleph, Autor = Jorge Luis Borges, Anio = 1952 }
```

`with` no modifica `original`: crea un objeto nuevo. Esa es la base de la inmutabilidad práctica.

## Ejemplo aplicado

Un préstamo es un dato que interesa comparar y copiar sin efectos colaterales. Un `record` expresa esa idea mejor que una clase mutable.

```csharp
// Prestamo.cs
public record Prestamo(string Socio, string Titulo, DateOnly Fecha, int Dias)
{
    public DateOnly VenceEl => Fecha.AddDays(Dias);
}
```

```csharp
// Ubicacion.cs
public readonly record struct Ubicacion(int Estante, int Nivel);
```

```csharp
// Program.cs
var prestamo = new Prestamo(
    "ana@ejemplo.com",
    "El Aleph",
    new DateOnly(2026, 11, 11),
    14);

var renovado = prestamo with { Dias = 21 };

Console.WriteLine(prestamo.VenceEl);      // 2026-11-25
Console.WriteLine(renovado.VenceEl);      // 2026-12-02
Console.WriteLine(prestamo == renovado);  // False

var ubicacion = new Ubicacion(3, 2);
var otra = ubicacion with { Nivel = 4 };

Console.WriteLine(ubicacion); // Ubicacion { Estante = 3, Nivel = 2 }
Console.WriteLine(otra);      // Ubicacion { Estante = 3, Nivel = 4 }
```

Salida:

```text
2026-11-25
2026-12-02
False
Ubicacion { Estante = 3, Nivel = 2 }
Ubicacion { Estante = 3, Nivel = 4 }
```

`Prestamo` es un record por referencia: la variable guarda una referencia, pero la igualdad es por valor. `Ubicacion` es un `readonly record struct`: se copia al asignar y no puede modificarse después de crearse.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Esperar igualdad por valor en una clase | Las clases usan igualdad por referencia por defecto | Usar un record o sobrescribir `Equals` y `GetHashCode` |
| Creer que `with` modifica el original | `with` siempre crea una copia | Asignar el resultado a una variable nueva |
| Usar records mutables | Se pierde la ventaja de la inmutabilidad | Preferir propiedades `init` o `readonly record struct` |
| Meter listas en un record y comparar | La lista se compara por referencia | Comparar manualmente o usar una estructura inmutable |
| Confundir record con record struct | Uno es por referencia y el otro por valor | Elegir según el tamaño y la semántica del dato |
| Usar record para entidades con identidad | Dos entidades con los mismos datos no son la misma | Usar una clase con un identificador |

## Buenas prácticas

- Usá `record` para DTOs, mensajes, resultados y objetos de valor.
- Usá `record struct` o `readonly record struct` para valores chicos y frecuentes.
- Preferí records inmutables: `init` y `with` en lugar de setters.
- No uses records para entidades que tienen identidad y ciclo de vida propio; ahí conviene una clase.
- Evitá colecciones mutables dentro de un record si esperás igualdad por valor.
- No abuses de `with` para cambios masivos: si un objeto cambia mucho, quizá no sea un buen candidato a record.

## Ejercicio

Creá un `record Reserva` con `Socio`, `Titulo` y `Fecha`, y una propiedad calculada `VenceEl` que sume tres días a la fecha. Creá dos reservas iguales, una reserva extendida con `with` y mostrá la igualdad y las fechas de vencimiento.

<details>
<summary>Ver solución</summary>

```csharp
// Reserva.cs
public record Reserva(string Socio, string Titulo, DateOnly Fecha)
{
    public DateOnly VenceEl => Fecha.AddDays(3);
}
```

```csharp
// Program.cs
var reserva = new Reserva(
    "ana@ejemplo.com",
    "El Aleph",
    new DateOnly(2026, 11, 11));

var misma = new Reserva(
    "ana@ejemplo.com",
    "El Aleph",
    new DateOnly(2026, 11, 11));

var extendida = reserva with { Fecha = reserva.Fecha.AddDays(2) };

Console.WriteLine(reserva == misma);      // True
Console.WriteLine(reserva == extendida);  // False
Console.WriteLine(reserva.VenceEl);       // 2026-11-14
Console.WriteLine(extendida.VenceEl);     // 2026-11-16
```

Salida:

```text
True
False
2026-11-14
2026-11-16
```

</details>

## Resumen

- Los tipos por valor copian el dato; los tipos por referencia copian la referencia.
- Los records generan igualdad por valor, `ToString` y soporte para `with`.
- `record struct` es un tipo por valor; `readonly record struct` es inmutable.
- `with` crea una copia y no modifica el original.
- Usá records para datos; usá clases para entidades con identidad y comportamiento propio.

## Siguiente paso

Ya recorriste todas las piezas de la orientación a objetos. En la última lección vas a integrarlas en un sistema pequeño con clases, interfaces, herencia, composición y bajo acoplamiento: [Ejercicio integrador y quiz](/modulos/poo/ejercicio-integrador).
