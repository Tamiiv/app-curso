---
slug: clases-abstractas
title: "Clases abstractas"
order: 6
estimatedMinutes: 12
summary: "Clases que no se instancian y miembros abstractos que obligan a las derivadas a implementar el contrato."
quiz:
  - question: "¿Qué caracteriza a una clase abstracta?"
    options:
      - "No se puede instanciar directamente"
      - "No puede tener métodos"
      - "No puede heredarse"
      - "Solo puede tener campos"
    answer: 0
    explanation: "Una clase abstracta está pensada como base: se puede heredar, pero no crear un objeto directamente con new."
  - question: "¿Qué debe hacer una clase derivada con un miembro abstracto?"
    options:
      - "Ignorarlo"
      - "Implementarlo con override"
      - "Ocultarlo con new"
      - "Declararlo static"
    answer: 1
    explanation: "Los miembros abstractos no tienen implementación en la base; la primera clase concreta debe cubrirlos con override."
  - question: "¿Puede una clase abstracta tener métodos con implementación?"
    options:
      - "No, todos deben ser abstractos"
      - "Sí, puede combinar métodos concretos y abstractos"
      - "Solo si son static"
      - "Solo si son private"
    answer: 1
    explanation: "Una clase abstracta puede tener código compartido y, al mismo tiempo, declarar miembros que las derivadas deben completar."
---

# Clases abstractas

## Objetivo

Vas a aprender a declarar clases abstractas con miembros abstractos y concretos, a impedir su instanciación y a usarlas como base de una jerarquía que comparte código y exige un contrato. También vas a distinguir cuándo conviene una clase abstracta y cuándo una interfaz.

## Requisitos previos

Necesitás manejar herencia, `virtual` y `override`. Si no los tenés claros, repasá [Polimorfismo: virtual y override](/modulos/poo/polimorfismo).

## Concepto

Una **clase abstracta** es una clase incompleta a propósito. No se puede crear un objeto directamente con `new`, porque está pensada para que otras clases la completen. Se declara con `abstract`:

```csharp
public abstract class MaterialBiblioteca
{
}
```

Puede tener dos tipos de miembros:

- **Concretos**: propiedades, métodos y constructores con implementación, que las derivadas heredan.
- **Abstractos**: miembros sin cuerpo, que cada clase concreta debe implementar con `override`.

```csharp
public abstract class MaterialBiblioteca
{
    public abstract int DiasDePrestamo();
    public abstract string Categoria { get; }
}
```

Una clase que hereda de una abstracta debe implementar todos los miembros abstractos o declararse también abstracta. La primera clase concreta de la jerarquía cierra el contrato.

La combinación de métodos concretos que llaman a métodos abstractos se conoce como **template method**. La base define el esqueleto del algoritmo y las derivadas completan los pasos variables. En el ejemplo, `Ficha()` arma el texto y delega en `Categoria` y `DiasDePrestamo()`.

Las clases abstractas se diferencian de las interfaces en que pueden tener estado, constructores y código compartido. Una clase solo puede heredar de una abstracta, pero puede implementar varias interfaces. En la próxima lección vas a profundizar en las interfaces.

## Ejemplo mínimo

```csharp
// MaterialBiblioteca.cs
public abstract class MaterialBiblioteca
{
    protected MaterialBiblioteca(string titulo) => Titulo = titulo;

    public string Titulo { get; }

    public abstract int DiasDePrestamo();

    public string Ficha() => $"{Titulo} · {DiasDePrestamo()} días";
}
```

```csharp
// Libro.cs
public sealed class Libro : MaterialBiblioteca
{
    public Libro(string titulo) : base(titulo) { }

    public override int DiasDePrestamo() => 21;
}
```

```csharp
// Program.cs
// var material = new MaterialBiblioteca("X"); // error: no se puede instanciar

var libro = new Libro("El Aleph");
Console.WriteLine(libro.Ficha());
```

Salida:

```text
El Aleph · 21 días
```

La clase base no sabe cuántos días dura un préstamo en general. Sabe que todo material debe responder esa pregunta, pero deja la respuesta a cada tipo concreto.

## Ejemplo aplicado

La biblioteca necesita clasificar sus materiales y mostrar una ficha uniforme. La clase abstracta concentra el formato y obliga a cada material a declarar su categoría y su plazo.

```csharp
// MaterialBiblioteca.cs
public abstract class MaterialBiblioteca
{
    protected MaterialBiblioteca(string codigo, string titulo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código es obligatorio.", nameof(codigo));
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título es obligatorio.", nameof(titulo));

        Codigo = codigo;
        Titulo = titulo;
    }

    public string Codigo { get; }
    public string Titulo { get; }

    public abstract string Categoria { get; }
    public abstract int DiasDePrestamo();

    public string Ficha() => $"[{Categoria}] {Codigo} · {Titulo} · {DiasDePrestamo()} días";
}
```

```csharp
// Libro.cs
public sealed class Libro : MaterialBiblioteca
{
    public Libro(string codigo, string titulo, string autor)
        : base(codigo, titulo)
    {
        Autor = autor;
    }

    public string Autor { get; }
    public override string Categoria => "Libro";
    public override int DiasDePrestamo() => 21;
}
```

```csharp
// Revista.cs
public sealed class Revista : MaterialBiblioteca
{
    public Revista(string codigo, string titulo, int numero)
        : base(codigo, titulo)
    {
        Numero = numero;
    }

    public int Numero { get; }
    public override string Categoria => "Revista";
    public override int DiasDePrestamo() => 7;
}
```

```csharp
// Pelicula.cs
public sealed class Pelicula : MaterialBiblioteca
{
    public Pelicula(string codigo, string titulo, int duracionMinutos)
        : base(codigo, titulo)
    {
        DuracionMinutos = duracionMinutos;
    }

    public int DuracionMinutos { get; }
    public override string Categoria => "Película";
    public override int DiasDePrestamo() => 3;
}
```

```csharp
// Program.cs
var materiales = new List<MaterialBiblioteca>
{
    new Libro("L-001", "El Aleph", "Jorge Luis Borges"),
    new Revista("R-014", "Revista Ñ", 214),
    new Pelicula("P-007", "El secreto de sus ojos", 129)
};

foreach (var material in materiales)
{
    Console.WriteLine(material.Ficha());
}
```

Salida:

```text
[Libro] L-001 · El Aleph · 21 días
[Revista] R-014 · Revista Ñ · 7 días
[Película] P-007 · El secreto de sus ojos · 3 días
```

Ninguna derivada repite el formato de la ficha ni la validación del código y el título. La base aporta lo común y exige lo específico.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `CS0144: no se puede crear una instancia de la clase abstracta` | Se intenta usar `new` con la base | Instanciar una clase concreta derivada |
| `CS0534: no implementa el miembro abstracto heredado` | La derivada no sobrescribe todo | Implementar con `override` o marcar la derivada como abstracta |
| `CS0513: el miembro abstracto no puede ser private` | Un miembro abstracto debe poder implementarse | Usar `public` o `protected` |
| Poner `abstract` en una clase concreta sin necesidad | Se impide instanciarla sin motivo | Quitar `abstract` si la clase ya está completa |
| Creer que abstracta significa sin código | Se duplica lógica en cada derivada | Mover el código común a métodos concretos de la base |
| Usar una clase abstracta para todo | Se pierde la flexibilidad de las interfaces | Elegir según haya estado compartido o solo un contrato |

## Buenas prácticas

- Usá una clase abstracta cuando haya estado o comportamiento común que valga la pena compartir.
- Mantené pocos miembros abstractos: cada uno es una obligación para todas las derivadas.
- Declará los constructores de la base como `protected` si solo se usan desde derivadas.
- Implementá en la base los métodos que no varían y dejá abstractos solo los pasos que cambian.
- Documentá qué debe cumplir cada miembro abstracto.
- Si solo necesitás un contrato sin estado ni código, preferí una interfaz.

## Ejercicio

Creá una clase abstracta `Empleado` con `Nombre`, `Legajo`, una propiedad abstracta `Puesto`, una propiedad abstracta `SueldoBase` y un método concreto `Resumen()` que arme un texto con todos esos datos. Después creá `Bibliotecario` y `Becario` con sus propios valores.

Mostrá el resumen de cada empleado desde una lista de `Empleado`.

<details>
<summary>Ver solución</summary>

```csharp
// Empleado.cs
public abstract class Empleado
{
    protected Empleado(string nombre, string legajo)
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

    public abstract string Puesto { get; }
    public abstract decimal SueldoBase { get; }

    public string Resumen() =>
        $"{Legajo} · {Nombre} · {Puesto} · ${SueldoBase:0.00}";
}
```

```csharp
// Bibliotecario.cs
public sealed class Bibliotecario : Empleado
{
    public Bibliotecario(string nombre, string legajo)
        : base(nombre, legajo) { }

    public override string Puesto => "Bibliotecario";
    public override decimal SueldoBase => 1_200_000m;
}
```

```csharp
// Becario.cs
public sealed class Becario : Empleado
{
    public Becario(string nombre, string legajo)
        : base(nombre, legajo) { }

    public override string Puesto => "Becario";
    public override decimal SueldoBase => 450_000m;
}
```

```csharp
// Program.cs
var empleados = new List<Empleado>
{
    new Bibliotecario("Ana Gómez", "E-001"),
    new Becario("Luis Pérez", "E-002")
};

foreach (var empleado in empleados)
{
    Console.WriteLine(empleado.Resumen());
}
```

Salida:

```text
E-001 · Ana Gómez · Bibliotecario · $1200000.00
E-002 · Luis Pérez · Becario · $450000.00
```

</details>

## Resumen

- Una clase abstracta no se puede instanciar y sirve como base de una jerarquía.
- Los miembros abstractos no tienen implementación y obligan a las derivadas a cubrirlos con `override`.
- Puede combinar código concreto con miembros abstractos.
- El template method deja en la base el esqueleto y en las derivadas los pasos variables.
- Si no hay estado ni código compartido, una interfaz suele ser mejor opción.

## Siguiente paso

Las clases abstractas sirven para compartir una base, pero una clase solo puede heredar de una. En la próxima lección vas a usar interfaces para definir contratos que cualquier clase puede implementar: [Interfaces](/modulos/poo/interfaces).
