---
slug: clases-objetos
title: "Clases y objetos"
order: 1
estimatedMinutes: 15
summary: "Qué es una clase, cómo crear objetos y cómo modelar tus primeros datos con propiedades y métodos en C#."
quiz:
  - question: "¿Qué relación hay entre una clase y un objeto?"
    options:
      - "Son exactamente lo mismo"
      - "La clase es la definición y el objeto es una instancia"
      - "El objeto es la definición y la clase una instancia"
      - "La clase solo existe en tiempo de ejecución"
    answer: 1
    explanation: "La clase describe el molde; el objeto es una instancia concreta creada a partir de ese molde."
  - question: "¿Qué hace la expresión new Libro()?"
    options:
      - "Declara la clase Libro"
      - "Crea una instancia y devuelve una referencia al objeto"
      - "Copia un objeto que ya existe"
      - "Elimina el objeto de la memoria"
    answer: 1
    explanation: "new reserva memoria para el objeto, ejecuta su constructor y devuelve una referencia."
  - question: "¿Qué miembro describe mejor el estado de un libro?"
    options:
      - "El método Prestar"
      - "La propiedad Titulo"
      - "La clase Program"
      - "Un comentario dentro del código"
    answer: 1
    explanation: "Las propiedades guardan el estado del objeto. Los métodos describen lo que el objeto puede hacer."
---

# Clases y objetos

## Objetivo

Vas a entender qué es una clase, cómo crear objetos a partir de ella y cómo combinar propiedades y métodos para modelar datos del mundo real. Al terminar, vas a poder escribir tus propias clases y usarlas desde un programa de consola.

## Requisitos previos

Conviene haber completado el módulo 2, sobre todo las lecciones de tipos, métodos, arrays y listas. También ayuda tener claro qué es un proyecto de consola y cómo ejecutarlo con `dotnet run`.

## Concepto

Una **clase** es una definición: describe qué datos va a tener un tipo de objeto y qué operaciones va a poder hacer. Un **objeto** es una instancia concreta de esa clase, creada en tiempo de ejecución.

Pensá en una ficha bibliográfica. La ficha vacía, con sus campos impresos, sería la clase. Cada ficha completada con un libro específico sería un objeto. Todos los objetos de la clase `Libro` comparten la misma estructura, pero cada uno tiene sus propios valores.

Una clase agrupa dos cosas:

- **Estado**: los datos del objeto, normalmente en propiedades como `Titulo` o `Paginas`.
- **Comportamiento**: las acciones que el objeto puede realizar, en métodos como `Ficha()` o `Prestar()`.

En C#, las clases son **tipos por referencia**. La variable no guarda el objeto completo: guarda una referencia a él. Cuando creás un objeto con `new`, la memoria se reserva en el montón administrado y la variable apunta a esa ubicación. Si dos variables apuntan al mismo objeto, los cambios hechos desde una se ven desde la otra. Esta idea vuelve con más detalle en la lección de records.

Las propiedades se declaran con `{ get; set; }`. `get` permite leer el valor y `set` permite asignarlo. Los métodos se declaran como cualquier función, pero dentro de la clase y con acceso directo a sus propiedades.

## Ejemplo mínimo

```csharp
// Libro.cs
public class Libro
{
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public int Paginas { get; set; }
    public bool Disponible { get; set; } = true;

    public string Ficha() => $"{Titulo} — {Autor} ({Paginas} págs.)";
}
```

```csharp
// Program.cs
var libro = new Libro
{
    Titulo = "El Aleph",
    Autor = "Jorge Luis Borges",
    Paginas = 176
};

Console.WriteLine(libro.Ficha());
Console.WriteLine($"Disponible: {libro.Disponible}");
```

La salida esperada es:

```text
El Aleph — Jorge Luis Borges (176 págs.)
Disponible: True
```

La sintaxis `new Libro { ... }` se llama **inicializador de objeto**. Crea la instancia y asigna las propiedades indicadas en un solo paso.

## Ejemplo aplicado

Una biblioteca necesita mostrar su catálogo. Podemos crear una clase que agrupe libros y se encargue de listarlos.

```csharp
// Libro.cs
public class Libro
{
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public int Paginas { get; set; }
    public bool Disponible { get; set; } = true;

    public string Ficha() => $"{Titulo} — {Autor} ({Paginas} págs.)";
}
```

```csharp
// Biblioteca.cs
public class Biblioteca
{
    private readonly List<Libro> _libros = [];

    public void Agregar(Libro libro) => _libros.Add(libro);

    public void MostrarCatalogo()
    {
        foreach (var libro in _libros)
        {
            Console.WriteLine(libro.Ficha());
        }
    }
}
```

```csharp
// Program.cs
var biblioteca = new Biblioteca();

biblioteca.Agregar(new Libro
{
    Titulo = "El Aleph",
    Autor = "Jorge Luis Borges",
    Paginas = 176
});

biblioteca.Agregar(new Libro
{
    Titulo = "Cien años de soledad",
    Autor = "Gabriel García Márquez",
    Paginas = 471
});

biblioteca.MostrarCatalogo();
```

Salida:

```text
El Aleph — Jorge Luis Borges (176 págs.)
Cien años de soledad — Gabriel García Márquez (471 págs.)
```

`Biblioteca` no sabe cómo se imprime un libro: le pide a cada objeto que se muestre con `Ficha()`. Esa división de responsabilidades es el comienzo de un buen diseño orientado a objetos.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `NullReferenceException` al usar el objeto | La variable se declaró pero no se creó con `new` | Crear la instancia antes de acceder a sus miembros |
| Confundir clase con objeto | Se intenta usar la clase como si tuviera datos | Pensar en la clase como el molde y en el objeto como la instancia |
| Propiedades sin valor inicial | Los `string` quedan en `null` | Inicializar con `= ""` o usar un constructor |
| Esperar que `==` compare contenidos | En clases, `==` compara referencias por defecto | Comparar propiedades o usar un `record` cuando corresponda |
| Una clase hace demasiadas cosas | Se mezclan catálogo, impresión y persistencia | Separar responsabilidades en clases más pequeñas |

## Buenas prácticas

- Usá nombres en singular para las clases: `Libro`, no `Libros`.
- Preferí propiedades públicas antes que campos públicos. Las propiedades permiten validar y evolucionar sin romper el código que las usa.
- Inicializá los `string` para evitar `null` inesperados.
- Mantené cada clase enfocada en una responsabilidad. Si una clase crece demasiado, probablemente haya que dividirla.
- No pongas lógica de entrada o salida dentro de una clase de dominio si podés evitarlo. `Libro` debería describir un libro, no decidir cómo se imprime la consola.

## Ejercicio

Creá una clase `Pelicula` con las propiedades `Titulo`, `Director`, `Anio` y `DuracionMinutos`. Agregá un método `Ficha()` que devuelva un texto descriptivo y un método `EsLarga()` que devuelva `true` cuando dure más de 120 minutos.

Después, creá una película, mostrá su ficha y su clasificación por duración.

<details>
<summary>Ver solución</summary>

```csharp
// Pelicula.cs
public class Pelicula
{
    public string Titulo { get; set; } = "";
    public string Director { get; set; } = "";
    public int Anio { get; set; }
    public int DuracionMinutos { get; set; }

    public string Ficha() =>
        $"{Titulo} ({Anio}) — {Director}, {DuracionMinutos} min";

    public bool EsLarga() => DuracionMinutos > 120;
}
```

```csharp
// Program.cs
var pelicula = new Pelicula
{
    Titulo = "El secreto de sus ojos",
    Director = "Juan José Campanella",
    Anio = 2009,
    DuracionMinutos = 129
};

Console.WriteLine(pelicula.Ficha());
Console.WriteLine(pelicula.EsLarga() ? "Es larga" : "Duración estándar");
```

Salida:

```text
El secreto de sus ojos (2009) — Juan José Campanella, 129 min
Es larga
```

</details>

## Resumen

- Una clase define estado y comportamiento; un objeto es una instancia de esa clase.
- `new` crea el objeto y devuelve una referencia.
- Las propiedades guardan el estado y los métodos ejecutan acciones.
- Las clases son tipos por referencia: dos variables pueden apuntar al mismo objeto.

## Siguiente paso

Ya sabés crear objetos, pero nada impide que queden en un estado inválido. En la próxima lección vas a usar constructores, propiedades `init` y `required` para crear objetos siempre válidos: [Constructores y propiedades](/modulos/poo/constructores-propiedades).
