---
slug: constructores-propiedades
title: "Constructores y propiedades"
order: 2
estimatedMinutes: 15
summary: "Constructores que validan, propiedades con get, set e init, y required para crear objetos siempre válidos."
quiz:
  - question: "¿Cuándo se ejecuta un constructor?"
    options:
      - "Al declarar la clase"
      - "Al crear una instancia con new"
      - "Al compilar el proyecto"
      - "Al llamar a cualquier método"
    answer: 1
    explanation: "El constructor se ejecuta una vez por cada instancia, justo después de reservar la memoria."
  - question: "¿Qué permite el modificador required en una propiedad?"
    options:
      - "Obligar a inicializarla al crear el objeto"
      - "Hacer que la propiedad sea de solo lectura"
      - "Crear un constructor vacío automáticamente"
      - "Ocultar la propiedad desde otras clases"
    answer: 0
    explanation: "required obliga al código que crea el objeto a asignar esa propiedad; si falta, el compilador da error."
  - question: "¿Qué diferencia hay entre init y set?"
    options:
      - "init solo permite asignar durante la inicialización del objeto"
      - "set solo permite asignar dentro del constructor"
      - "Son exactamente iguales"
      - "init se usa en campos y set en propiedades"
    answer: 0
    explanation: "init permite asignar al crear el objeto o dentro de su constructor, pero no después. set permite asignar en cualquier momento."
---

# Constructores y propiedades

## Objetivo

Vas a aprender a crear objetos que nazcan en un estado válido usando constructores, y a controlar la lectura y escritura de sus datos con propiedades `get`, `set`, `init` y `required`. También vas a ver cómo calcular valores derivados sin duplicar información.

## Requisitos previos

Necesitás haber leído [Clases y objetos](/modulos/poo/clases-objetos) y manejar métodos con parámetros. No hace falta saber herencia todavía.

## Concepto

En la lección anterior, un `Libro` podía crearse sin título o con una cantidad negativa de páginas. Eso deja objetos inválidos circulando por el programa. El **constructor** existe para evitarlo: es un método especial que se ejecuta al crear la instancia y deja el objeto listo para usarse.

Un constructor tiene el mismo nombre que la clase, no declara tipo de retorno y se invoca con `new`. Si escribís un constructor propio, el constructor vacío que C# agregaba por defecto desaparece. Podés tener varios constructores con distintas listas de parámetros; eso se llama **sobrecarga de constructores**.

Las **propiedades** son la forma recomendada de exponer datos. Una propiedad puede ser:

- `get; set;`: lectura y escritura libres.
- `get; private set;`: todos leen, solo la clase escribe.
- `get;`: solo lectura, se asigna en el constructor.
- `get; init;`: solo lectura después de la inicialización.
- Calculada: `public string Ficha => ...;`, sin campo propio.

Desde C# 11, `required` obliga a asignar una propiedad en el inicializador de objeto. Se combina muy bien con `init` para crear objetos inmutables sin escribir un constructor lleno de parámetros.

```csharp
public class LibroDigital
{
    public required string Titulo { get; init; }
    public required string Url { get; init; }
    public int Paginas { get; init; }
}

var ebook = new LibroDigital
{
    Titulo = "El Aleph",
    Url = "https://ejemplo.com/aleph",
    Paginas = 176
};

// ebook.Titulo = "Otro"; // error: init solo permite asignar al inicializar
```

`required` se verifica en tiempo de compilación. `init` se verifica con las reglas del compilador: el setter solo puede ejecutarse durante la creación del objeto o desde su constructor.

## Ejemplo mínimo

```csharp
// Libro.cs
public class Libro
{
    public Libro(string titulo, string autor, int paginas)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título es obligatorio.", nameof(titulo));
        if (string.IsNullOrWhiteSpace(autor))
            throw new ArgumentException("El autor es obligatorio.", nameof(autor));
        if (paginas <= 0)
            throw new ArgumentOutOfRangeException(nameof(paginas), "Debe ser mayor que cero.");

        Titulo = titulo;
        Autor = autor;
        Paginas = paginas;
    }

    public string Titulo { get; }
    public string Autor { get; }
    public int Paginas { get; }
    public bool Disponible { get; private set; } = true;

    public string Ficha => $"{Titulo} — {Autor} ({Paginas} págs.)";

    public void Prestar() => Disponible = false;
    public void Devolver() => Disponible = true;
}
```

```csharp
// Program.cs
var libro = new Libro("El Aleph", "Jorge Luis Borges", 176);
Console.WriteLine(libro.Ficha);
Console.WriteLine($"Disponible: {libro.Disponible}");

libro.Prestar();
Console.WriteLine($"Después de prestar: {libro.Disponible}");
```

Salida:

```text
El Aleph — Jorge Luis Borges (176 págs.)
Disponible: True
Después de prestar: False
```

`Disponible` tiene `private set`: se lee desde cualquier lado, pero solo `Libro` decide cuándo cambia. Las demás propiedades son de solo lectura y se asignan en el constructor.

## Ejemplo aplicado

Un préstamo tiene fechas y un vencimiento que se calcula solo. El constructor recibe los datos obligatorios y valida que tengan sentido.

```csharp
// Prestamo.cs
public class Prestamo
{
    public Prestamo(string socio, string titulo, DateOnly fechaPrestamo, int dias)
    {
        if (string.IsNullOrWhiteSpace(socio))
            throw new ArgumentException("El socio es obligatorio.", nameof(socio));
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título es obligatorio.", nameof(titulo));
        if (dias <= 0)
            throw new ArgumentOutOfRangeException(nameof(dias), "Los días deben ser mayores que cero.");

        Socio = socio;
        Titulo = titulo;
        FechaPrestamo = fechaPrestamo;
        Dias = dias;
    }

    public string Socio { get; }
    public string Titulo { get; }
    public DateOnly FechaPrestamo { get; }
    public int Dias { get; }

    public DateOnly FechaVencimiento => FechaPrestamo.AddDays(Dias);

    public bool EstaVencido =>
        DateOnly.FromDateTime(DateTime.Today) > FechaVencimiento;

    public string Estado => EstaVencido ? "Vencido" : "En plazo";
}
```

```csharp
// Program.cs
var prestamo = new Prestamo(
    socio: "ana@ejemplo.com",
    titulo: "El Aleph",
    fechaPrestamo: DateOnly.FromDateTime(DateTime.Today),
    dias: 14);

Console.WriteLine($"Vence: {prestamo.FechaVencimiento}");
Console.WriteLine($"Estado: {prestamo.Estado}");
```

Salida aproximada, si lo ejecutás el 11 de noviembre de 2026:

```text
Vence: 2026-11-25
Estado: En plazo
```

`FechaVencimiento` y `Estado` no se guardan: se calculan a partir de `FechaPrestamo` y `Dias`. Así no hay dos fuentes de verdad que puedan contradecirse. Los argumentos con nombre (`socio:`, `dias:`) hacen más legible la llamada cuando hay varios parámetros del mismo tipo.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `CS7036: no se ha dado ningún argumento` | Se agregó un constructor y se intenta usar `new Libro()` | Pasar los argumentos o agregar un constructor sin parámetros |
| `CS9035: miembro requerido` | Falta una propiedad `required` en el inicializador | Asignarla al crear el objeto |
| `CS8852: init-only property` | Se intenta modificar una propiedad `init` después de crear el objeto | Usar `set`, `private set` o crear una copia |
| Validar solo en el setter | El constructor asigna directo al campo y evita la validación | Validar en el constructor o centralizar la validación |
| Propiedad calculada que devuelve datos viejos | Se guardó el resultado en un campo y no se actualizó | Calcularla con una expresión cada vez que se lee |

## Buenas prácticas

- Validá en el constructor los datos que no pueden faltar. Un objeto que existe debería ser válido.
- Usá `get;` para datos que no cambian después de crear el objeto.
- Usá `private set;` para estado que solo la propia clase debe modificar, como `Disponible`.
- Usá `required` e `init` para objetos de configuración o DTOs inmutables.
- No hagas entrada/salida, llamadas a red ni consultas a base de datos dentro de un constructor. Un constructor debe ser rápido y predecible.
- Evitá constructores con demasiados parámetros. Si superan cuatro o cinco, probablemente haya una clase escondida ahí.

## Ejercicio

Creá una clase `Socio` con `Nombre`, `Email`, `FechaAlta`, `Activo` y un método `CambiarEmail`. El constructor debe rechazar un nombre vacío y un email sin `@`. `FechaAlta` debe poder asignarse al crear el socio, con la fecha actual como valor por defecto. `Activo` empieza en `true` y solo se modifica con un método `DarDeBaja()`.

Mostrá los datos del socio, cambiá el email y dalo de baja.

<details>
<summary>Ver solución</summary>

```csharp
// Socio.cs
public class Socio
{
    public Socio(string nombre, string email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        if (!email.Contains('@'))
            throw new ArgumentException("El email no es válido.", nameof(email));

        Nombre = nombre;
        Email = email;
    }

    public string Nombre { get; }
    public string Email { get; private set; }
    public DateOnly FechaAlta { get; init; } = DateOnly.FromDateTime(DateTime.Today);
    public bool Activo { get; private set; } = true;

    public void CambiarEmail(string nuevoEmail)
    {
        if (!nuevoEmail.Contains('@'))
            throw new ArgumentException("El email no es válido.", nameof(nuevoEmail));
        Email = nuevoEmail;
    }

    public void DarDeBaja() => Activo = false;
}
```

```csharp
// Program.cs
var socio = new Socio("Ana Gómez", "ana@ejemplo.com");
Console.WriteLine($"{socio.Nombre} · {socio.Email} · alta {socio.FechaAlta} · activo: {socio.Activo}");

socio.CambiarEmail("ana.gomez@ejemplo.com");
socio.DarDeBaja();
Console.WriteLine($"{socio.Email} · activo: {socio.Activo}");
```

Salida aproximada:

```text
Ana Gómez · ana@ejemplo.com · alta 2026-11-11 · activo: True
ana.gomez@ejemplo.com · activo: False
```

`FechaAlta` usa `init`, así que puede asignarse al crear el socio, pero no después. `Email` usa `private set` porque cambia mediante un método que valida.

</details>

## Resumen

- El constructor se ejecuta al crear la instancia y deja el objeto en un estado válido.
- `get;`, `private set;`, `init;` y `required` controlan quién lee y cuándo se escribe.
- Las propiedades calculadas evitan duplicar información.
- Validar en el constructor es más seguro que confiar en que quien usa la clase complete todo bien.

## Siguiente paso

Ya controlás el estado de un objeto, pero todavía podés exponer demasiado. En la próxima lección vas a profundizar en el encapsulamiento y los modificadores de acceso: [Encapsulamiento y modificadores de acceso](/modulos/poo/encapsulamiento).
