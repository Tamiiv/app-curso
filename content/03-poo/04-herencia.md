---
slug: herencia
title: "Herencia"
order: 4
estimatedMinutes: 15
summary: "Reutilizá estado y comportamiento con clases base, constructores con base(...) y el modificador sealed."
quiz:
  - question: "¿Qué indica : MaterialBiblioteca en la declaración de una clase?"
    options:
      - "Que implementa una interfaz"
      - "Que hereda de esa clase"
      - "Que contiene esa clase"
      - "Que es una clase abstracta"
    answer: 1
    explanation: "Los dos puntos seguidos de un tipo indican la clase base de la que se hereda."
  - question: "¿Para qué se usa base(...) en un constructor?"
    options:
      - "Para llamar a un método de la clase base"
      - "Para invocar el constructor de la clase base"
      - "Para crear una interfaz"
      - "Para sellar la clase"
    answer: 1
    explanation: "base(...) encadena la construcción: primero se inicializa la parte heredada y después la propia."
  - question: "¿Qué impide el modificador sealed en una clase?"
    options:
      - "Que se pueda instanciar"
      - "Que se pueda heredar de ella"
      - "Que tenga propiedades"
      - "Que implemente interfaces"
    answer: 1
    explanation: "Una clase sealed no puede ser clase base de otra. Se puede instanciar normalmente."
---

# Herencia

## Objetivo

Vas a aprender a extraer estado y comportamiento comunes en una clase base y a reutilizarlos en clases derivadas. También vas a manejar la cadena de constructores con `base(...)`, el acceso `protected` y el modificador `sealed`.

## Requisitos previos

Necesitás dominar clases, constructores, propiedades y encapsulamiento. Si alguna de esas ideas no está firme, repasá [Encapsulamiento y modificadores de acceso](/modulos/poo/encapsulamiento).

## Concepto

La **herencia** permite declarar que una clase es una versión especializada de otra. La clase derivada recibe los miembros accesibles de la clase base y puede agregar los propios. La relación se lee como **es un**: un `Libro` es un `MaterialBiblioteca`, una `Revista` es un `MaterialBiblioteca`.

En C# una clase solo puede heredar de una clase base. Eso se llama **herencia simple**. Lo que sí se puede hacer es implementar varias interfaces, tema de la lección 7. Todas las clases heredan, directa o indirectamente, de `object`.

La sintaxis es:

```csharp
public class Revista : MaterialBiblioteca
{
}
```

La clase base puede declarar miembros `protected`, que son accesibles desde ella y desde sus derivadas, pero no desde el resto del programa. Es un punto intermedio entre `private` y `public`; conviene usarlo con moderación.

Los constructores no se heredan. Si la clase base no tiene un constructor sin parámetros, la derivada debe llamar a uno de sus constructores con `base(...)`. Esa llamada ocurre antes de ejecutar el cuerpo del constructor derivado.

```csharp
public Revista(string titulo, int numero) : base(titulo)
{
    Numero = numero;
}
```

`sealed` en una clase impide que otros hereden de ella. Es útil para cerrar jerarquías cuando la clase no fue diseñada para extensión. También existe `sealed override`, que impide seguir sobrescribiendo un método en clases más profundas; lo vas a ver en la próxima lección.

## Ejemplo mínimo

```csharp
// MaterialBiblioteca.cs
public class MaterialBiblioteca
{
    public MaterialBiblioteca(string titulo)
    {
        Titulo = titulo;
    }

    public string Titulo { get; }
}
```

```csharp
// Revista.cs
public sealed class Revista : MaterialBiblioteca
{
    public Revista(string titulo, int numero) : base(titulo)
    {
        Numero = numero;
    }

    public int Numero { get; }

    public string Ficha() => $"{Titulo} · N.º {Numero}";
}
```

```csharp
// Program.cs
var revista = new Revista("Revista Ñ", 214);
Console.WriteLine(revista.Ficha());
```

Salida:

```text
Revista Ñ · N.º 214
```

`Revista` usa `Titulo`, que fue declarado en `MaterialBiblioteca`, y agrega `Numero`. La llamada `base(titulo)` inicializa la parte heredada.

## Ejemplo aplicado

Una biblioteca mezcla libros y películas. Ambos comparten un código, un título y un año, pero cada uno aporta datos propios.

```csharp
// MaterialBiblioteca.cs
public class MaterialBiblioteca
{
    public MaterialBiblioteca(string codigo, string titulo, int anio)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código es obligatorio.", nameof(codigo));
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título es obligatorio.", nameof(titulo));

        Codigo = codigo;
        Titulo = titulo;
        Anio = anio;
    }

    public string Codigo { get; }
    public string Titulo { get; }
    public int Anio { get; }

    public string Ficha() => $"{Codigo} · {Titulo} ({Anio})";
}
```

```csharp
// Libro.cs
public sealed class Libro : MaterialBiblioteca
{
    public Libro(string codigo, string titulo, int anio, string autor)
        : base(codigo, titulo, anio)
    {
        Autor = autor;
    }

    public string Autor { get; }

    public string FichaCompleta() => $"{Ficha()} · {Autor}";
}
```

```csharp
// Pelicula.cs
public sealed class Pelicula : MaterialBiblioteca
{
    public Pelicula(string codigo, string titulo, int anio, int duracionMinutos)
        : base(codigo, titulo, anio)
    {
        DuracionMinutos = duracionMinutos;
    }

    public int DuracionMinutos { get; }

    public string FichaCompleta() => $"{Ficha()} · {DuracionMinutos} min";
}
```

```csharp
// Program.cs
var materiales = new List<MaterialBiblioteca>
{
    new Libro("L-001", "El Aleph", 1949, "Jorge Luis Borges"),
    new Pelicula("P-007", "El secreto de sus ojos", 2009, 129)
};

foreach (var material in materiales)
{
    Console.WriteLine(material.Ficha());
}
```

Salida:

```text
L-001 · El Aleph (1949)
P-007 · El secreto de sus ojos (2009)
```

La lista es de `MaterialBiblioteca`, así que solo puede llamar a los miembros de esa clase. `FichaCompleta()` queda fuera porque pertenece a cada derivada. En la próxima lección vas a hacer que `Ficha()` se comporte distinto según el objeto real, sin perder el tipo común.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `CS7036` en el constructor derivado | La base no tiene constructor sin parámetros y falta `base(...)` | Llamar al constructor base con los argumentos correctos |
| `CS0122` al acceder a un miembro | El miembro es `private` en la base | Usar una propiedad pública o evaluar `protected` |
| Heredar solo para reutilizar código | No existe una relación es un | Preferir composición, como verás en la lección 8 |
| Jerarquías demasiado profundas | Cada nivel agrega acoplamiento | Mantener jerarquías cortas y cohesivas |
| Marcar todo `protected` | Se expone estado interno a las derivadas | Empezar con `private` y abrir solo lo necesario |
| Ocultar un método con `new` | Se confunde con sobrescritura | Usar `virtual` y `override` cuando corresponda |

## Buenas prácticas

- Usá herencia solo cuando la relación **es un** sea verdadera y estable.
- Mantené la clase base pequeña y con una responsabilidad clara.
- Preferí `private` y exponé `protected` solo para lo que las derivadas realmente necesitan.
- Marcá como `sealed` las clases de las que no querés que se herede.
- No uses la herencia como mecanismo para compartir código suelto. Para eso existen la composición y las interfaces.
- Documentá qué miembros están pensados para sobrescribirse y cuáles no.

## Ejercicio

Creá una clase base `Empleado` con `Nombre`, `Legajo` y un método `Ficha()`. Después creá dos clases derivadas: `Bibliotecario`, con un `Turno`, y `Becario`, con una `Universidad`. Cada derivada debe agregar su propio método `FichaCompleta()` que reutilice `Ficha()`.

Armá una lista de empleados, mostrá la ficha común de cada uno y después la ficha completa según su tipo.

<details>
<summary>Ver solución</summary>

```csharp
// Empleado.cs
public class Empleado
{
    public Empleado(string nombre, string legajo)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        if (string.IsNullOrWhiteSpace(legajo))
            throw new ArgumentException("El legajo es obligatorio.", nameof(legajo));

        Nombre = nombre;
        Legajo = legajo;
    }

    public string Nombre { get; }
    public string Legajo { get; }

    public string Ficha() => $"{Legajo} · {Nombre}";
}
```

```csharp
// Bibliotecario.cs
public sealed class Bibliotecario : Empleado
{
    public Bibliotecario(string nombre, string legajo, string turno)
        : base(nombre, legajo)
    {
        Turno = turno;
    }

    public string Turno { get; }

    public string FichaCompleta() => $"{Ficha()} · Turno {Turno}";
}
```

```csharp
// Becario.cs
public sealed class Becario : Empleado
{
    public Becario(string nombre, string legajo, string universidad)
        : base(nombre, legajo)
    {
        Universidad = universidad;
    }

    public string Universidad { get; }

    public string FichaCompleta() => $"{Ficha()} · {Universidad}";
}
```

```csharp
// Program.cs
var empleados = new List<Empleado>
{
    new Bibliotecario("Ana Gómez", "E-001", "Mañana"),
    new Becario("Luis Pérez", "E-002", "Universidad de Buenos Aires")
};

foreach (var empleado in empleados)
{
    Console.WriteLine(empleado.Ficha());
}

var bibliotecario = (Bibliotecario)empleados[0];
var becario = (Becario)empleados[1];

Console.WriteLine(bibliotecario.FichaCompleta());
Console.WriteLine(becario.FichaCompleta());
```

Salida:

```text
E-001 · Ana Gómez
E-002 · Luis Pérez
E-001 · Ana Gómez · Turno Mañana
E-002 · Luis Pérez · Universidad de Buenos Aires
```

Los casts funcionan porque cada objeto es realmente del tipo derivado. Si la lista tuviera un empleado de otro tipo, el cast fallaría en tiempo de ejecución.

</details>

## Resumen

- La herencia expresa una relación **es un** y permite reutilizar miembros de una clase base.
- Los constructores no se heredan: la derivada encadena con `base(...)`.
- `protected` abre el acceso solo a la clase y sus derivadas.
- `sealed` impide heredar de una clase.
- La herencia no es la única forma de reutilizar; la composición suele ser más flexible.

## Siguiente paso

Ya podés compartir estructura entre clases. En la próxima lección vas a hacer que un mismo método se comporte distinto según el objeto real: [Polimorfismo: virtual y override](/modulos/poo/polimorfismo).
