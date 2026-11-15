---
slug: ejercicio-integrador
title: "Ejercicio integrador y quiz"
order: 10
estimatedMinutes: 25
summary: "Diseñá un sistema de biblioteca con clases, interfaces, herencia, composición y bajo acoplamiento."
quiz:
  - question: "¿Por qué conviene que PrestamoService dependa de IPrestable y no de Libro?"
    options:
      - "Para acoplarlo a una clase concreta"
      - "Para poder trabajar con cualquier material prestable"
      - "Para evitar interfaces"
      - "Para heredar de Libro"
    answer: 1
    explanation: "Al depender del contrato, el servicio acepta libros, revistas o cualquier tipo futuro que sea prestable."
  - question: "¿Qué clase conviene para compartir código y exigir implementaciones comunes?"
    options:
      - "Una clase abstracta"
      - "Un record struct"
      - "Una clase sealed"
      - "Un método static"
    answer: 0
    explanation: "La clase abstracta puede aportar estado y comportamiento común y, a la vez, declarar miembros que las derivadas deben implementar."
  - question: "¿Qué tipo es adecuado para un registro inmutable de préstamo?"
    options:
      - "Una clase mutable"
      - "Un record"
      - "Una interfaz"
      - "Un campo público"
    answer: 1
    explanation: "Un record ofrece igualdad por valor, ToString legible y una forma cómoda de crear copias con with."
---

# Ejercicio integrador y quiz

## Objetivo

Vas a integrar clases, encapsulamiento, herencia, polimorfismo, interfaces, composición y records en un sistema pequeño de biblioteca. El resultado va a ser un diseño con bajo acoplamiento, fácil de extender y con una separación clara entre datos, comportamiento y notificaciones.

## Requisitos previos

Haber completado las lecciones anteriores del módulo. Especialmente conviene tener claros [Clases abstractas](/modulos/poo/clases-abstractas), [Interfaces](/modulos/poo/interfaces) y [Composición vs. herencia](/modulos/poo/composicion-herencia).

## Concepto

La biblioteca necesita:

- Registrar materiales: libros y revistas.
- Saber si un material está disponible.
- Prestar y devolver materiales.
- Calcular el plazo de cada tipo.
- Avisar al socio cuando se presta o se devuelve.
- Llevar un registro de préstamos.

El diseño se apoya en cuatro decisiones:

1. **Una clase abstracta** `MaterialBiblioteca` para el estado y el comportamiento común: código, título, año, categoría y ficha.
2. **Una interfaz** `IPrestable` para la capacidad de prestar. No todos los materiales tienen por qué ser prestables.
3. **Una interfaz** `INotificador` para desacoplar los avisos. El servicio no sabe si se escribe en consola, en memoria o en otro medio.
4. **Un record** `Prestamo` para representar el registro inmutable de cada préstamo.

La estructura queda así:

```text
MaterialBiblioteca (abstracta)
├── Libro : IPrestable
└── Revista : IPrestable

Biblioteca ──contiene──> List<MaterialBiblioteca>

PrestamoService ──usa──> IPrestable
                └──────> INotificador

Prestamo (record)
```

`Biblioteca` se ocupa del catálogo. `PrestamoService` se ocupa de las operaciones de préstamo y de avisar. Ninguna de las dos conoce los detalles de la otra. Eso es bajo acoplamiento: cada clase tiene una responsabilidad y colabora a través de contratos.

## Ejemplo mínimo

El esqueleto esencial tiene una base abstracta, un contrato de préstamo y una implementación concreta.

```csharp
// MaterialBiblioteca.cs
public abstract class MaterialBiblioteca
{
    protected MaterialBiblioteca(string codigo, string titulo)
    {
        Codigo = codigo;
        Titulo = titulo;
    }

    public string Codigo { get; }
    public string Titulo { get; }
    public abstract string Categoria { get; }
    public abstract int DiasDePrestamo { get; }

    public string Ficha() => $"{Codigo} · {Titulo} · {Categoria} · {DiasDePrestamo} días";
}
```

```csharp
// IPrestable.cs
public interface IPrestable
{
    string Codigo { get; }
    string Titulo { get; }
    int DiasDePrestamo { get; }
    bool EstaDisponible { get; }
    void Prestar(string socio);
    void Devolver();
}
```

```csharp
// Libro.cs
public sealed class Libro : MaterialBiblioteca, IPrestable
{
    public Libro(string codigo, string titulo, string autor)
        : base(codigo, titulo)
    {
        Autor = autor;
    }

    public string Autor { get; }
    public override string Categoria => "Libro";
    public override int DiasDePrestamo => 21;
    public bool EstaDisponible { get; private set; } = true;

    public void Prestar(string socio)
    {
        if (!EstaDisponible)
            throw new InvalidOperationException("El libro ya está prestado.");
        EstaDisponible = false;
    }

    public void Devolver() => EstaDisponible = true;

    public override string Ficha() => $"{base.Ficha()} · {Autor}";
}
```

```csharp
// Program.cs
var libro = new Libro("L-001", "El Aleph", "Jorge Luis Borges");
Console.WriteLine(libro.Ficha());
libro.Prestar("ana@ejemplo.com");
Console.WriteLine($"Disponible: {libro.EstaDisponible}");
```

Salida:

```text
L-001 · El Aleph · Libro · 21 días · Jorge Luis Borges
Disponible: False
```

Ese esqueleto ya muestra las piezas principales. Ahora falta el catálogo, el servicio y el registro.

## Ejemplo aplicado

El sistema completo en un solo archivo. Podés crear un proyecto de consola, reemplazar `Program.cs` y ejecutarlo con `dotnet run`.

```csharp
using System;
using System.Collections.Generic;

public abstract class MaterialBiblioteca
{
    protected MaterialBiblioteca(string codigo, string titulo, int anio)
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

    public abstract string Categoria { get; }
    public abstract int DiasDePrestamo { get; }

    public virtual string Ficha() => $"{Codigo} · {Titulo} ({Anio}) · {Categoria}";
}

public interface IPrestable
{
    string Codigo { get; }
    string Titulo { get; }
    int DiasDePrestamo { get; }
    bool EstaDisponible { get; }
    void Prestar(string socio);
    void Devolver();
}

public interface INotificador
{
    void Enviar(string destino, string mensaje);
}

public sealed class Libro : MaterialBiblioteca, IPrestable
{
    public Libro(string codigo, string titulo, int anio, string autor)
        : base(codigo, titulo, anio)
    {
        Autor = autor;
    }

    public string Autor { get; }
    public override string Categoria => "Libro";
    public override int DiasDePrestamo => 21;
    public bool EstaDisponible { get; private set; } = true;

    public void Prestar(string socio)
    {
        if (!EstaDisponible)
            throw new InvalidOperationException($"'{Titulo}' ya está prestado.");
        if (string.IsNullOrWhiteSpace(socio))
            throw new ArgumentException("El socio es obligatorio.", nameof(socio));

        EstaDisponible = false;
    }

    public void Devolver() => EstaDisponible = true;

    public override string Ficha() => $"{base.Ficha()} · {Autor}";
}

public sealed class Revista : MaterialBiblioteca, IPrestable
{
    public Revista(string codigo, string titulo, int anio, int numero)
        : base(codigo, titulo, anio)
    {
        Numero = numero;
    }

    public int Numero { get; }
    public override string Categoria => "Revista";
    public override int DiasDePrestamo => 7;
    public bool EstaDisponible { get; private set; } = true;

    public void Prestar(string socio)
    {
        if (!EstaDisponible)
            throw new InvalidOperationException($"'{Titulo}' ya está prestado.");
        if (string.IsNullOrWhiteSpace(socio))
            throw new ArgumentException("El socio es obligatorio.", nameof(socio));

        EstaDisponible = false;
    }

    public void Devolver() => EstaDisponible = true;

    public override string Ficha() => $"{base.Ficha()} · N.º {Numero}";
}

public sealed record Prestamo(string CodigoMaterial, string Socio, DateOnly Fecha, int Dias);

public sealed class Biblioteca
{
    private readonly List<MaterialBiblioteca> _materiales = [];

    public IReadOnlyList<MaterialBiblioteca> Materiales => _materiales;

    public void Agregar(MaterialBiblioteca material)
    {
        ArgumentNullException.ThrowIfNull(material);
        _materiales.Add(material);
    }

    public MaterialBiblioteca? Buscar(string codigo)
    {
        foreach (var material in _materiales)
        {
            if (string.Equals(material.Codigo, codigo, StringComparison.OrdinalIgnoreCase))
                return material;
        }

        return null;
    }
}

public sealed class PrestamoService
{
    private readonly INotificador _notificador;
    private readonly List<Prestamo> _registro = [];

    public PrestamoService(INotificador notificador) => _notificador = notificador;

    public IReadOnlyList<Prestamo> Registro => _registro;

    public void Prestar(IPrestable material, string socio)
    {
        material.Prestar(socio);

        var prestamo = new Prestamo(
            material.Codigo,
            socio,
            DateOnly.FromDateTime(DateTime.Today),
            material.DiasDePrestamo);

        _registro.Add(prestamo);
        _notificador.Enviar(
            socio,
            $"Prestado: {material.Titulo}. Vence en {material.DiasDePrestamo} días.");
    }

    public void Devolver(IPrestable material, string socio)
    {
        material.Devolver();
        _notificador.Enviar(socio, $"Devuelto: {material.Titulo}.");
    }
}

public sealed class NotificadorConsola : INotificador
{
    public void Enviar(string destino, string mensaje) =>
        Console.WriteLine($"→ {destino}: {mensaje}");
}

public static class Program
{
    public static void Main()
    {
        var biblioteca = new Biblioteca();
        biblioteca.Agregar(new Libro("L-001", "Cien años de soledad", 1967, "Gabriel García Márquez"));
        biblioteca.Agregar(new Libro("L-002", "El Aleph", 1949, "Jorge Luis Borges"));
        biblioteca.Agregar(new Revista("R-014", "Revista Ñ", 2026, 214));

        var servicio = new PrestamoService(new NotificadorConsola());

        foreach (var material in biblioteca.Materiales)
        {
            Console.WriteLine(material.Ficha());
        }

        var libro = biblioteca.Buscar("L-001");
        if (libro is IPrestable prestable)
        {
            servicio.Prestar(prestable, "ana@ejemplo.com");
            servicio.Devolver(prestable, "ana@ejemplo.com");
            Console.WriteLine($"Disponible otra vez: {prestable.EstaDisponible}");
        }

        Console.WriteLine($"Préstamos registrados: {servicio.Registro.Count}");
    }
}
```

Salida:

```text
L-001 · Cien años de soledad (1967) · Libro · Gabriel García Márquez
L-002 · El Aleph (1949) · Libro · Jorge Luis Borges
R-014 · Revista Ñ (2026) · Revista · N.º 214
→ ana@ejemplo.com: Prestado: Cien años de soledad. Vence en 21 días.
→ ana@ejemplo.com: Devuelto: Cien años de soledad.
Disponible otra vez: True
Préstamos registrados: 1
```

El sistema cumple las reglas: `Biblioteca` encapsula el catálogo, `MaterialBiblioteca` comparte estado y contrato, `IPrestable` define la capacidad, `PrestamoService` coordina sin conocer las clases concretas y `Prestamo` guarda un registro inmutable. Si mañana agregás un DVD, no hace falta tocar el servicio ni el catálogo.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `PrestamoService` recibe `Libro` | Se acopla a una implementación concreta | Recibir `IPrestable` |
| `Biblioteca` hereda de `List<MaterialBiblioteca>` | Se confunde tener con ser | Componer con una lista privada |
| Validar el socio solo en el servicio | Otro llamador podría saltear la regla | Validar también en `Prestar` o en el constructor correspondiente |
| Exponer `List<Prestamo>` | El exterior puede modificar el registro | Exponer `IReadOnlyList<Prestamo>` |
| Agregar lógica de notificación dentro de `Libro` | Se mezclan dominio e infraestructura | Usar `INotificador` inyectado en el servicio |
| Duplicar la ficha en cada clase | Se repite el formato común | Poner el formato en la base y sobrescribir solo lo específico |

## Buenas prácticas

- Asigná una responsabilidad por clase: catálogo, préstamo, notificación y dato.
- Dependé de interfaces en los límites entre componentes.
- Mantené las entidades válidas desde el constructor y validá también las transiciones de estado.
- Exponé colecciones internas como solo lectura.
- Usá records para datos inmutables y clases para entidades con identidad.
- Dejá las clases abiertas a extensión pero cerradas a modificación: agregar un `Dvd` no debería requerir cambiar `PrestamoService`.
- Escribí pruebas para las reglas de negocio: prestar dos veces, devolver, buscar y notificar.

## Ejercicio

Extendé el sistema con un `Dvd` que herede de `MaterialBiblioteca`, implemente `IPrestable`, tenga una duración en minutos y un plazo de 3 días. Agregalo al catálogo y prestalo con el mismo `PrestamoService`, sin modificar el servicio.

<details>
<summary>Ver solución</summary>

```csharp
// Dvd.cs
public sealed class Dvd : MaterialBiblioteca, IPrestable
{
    public Dvd(string codigo, string titulo, int anio, int duracionMinutos)
        : base(codigo, titulo, anio)
    {
        DuracionMinutos = duracionMinutos;
    }

    public int DuracionMinutos { get; }
    public override string Categoria => "DVD";
    public override int DiasDePrestamo => 3;
    public bool EstaDisponible { get; private set; } = true;

    public void Prestar(string socio)
    {
        if (!EstaDisponible)
            throw new InvalidOperationException($"'{Titulo}' ya está prestado.");
        if (string.IsNullOrWhiteSpace(socio))
            throw new ArgumentException("El socio es obligatorio.", nameof(socio));

        EstaDisponible = false;
    }

    public void Devolver() => EstaDisponible = true;

    public override string Ficha() =>
        $"{base.Ficha()} · {DuracionMinutos} min";
}
```

```csharp
// Program.cs
var biblioteca = new Biblioteca();
biblioteca.Agregar(new Dvd("D-003", "El secreto de sus ojos", 2009, 129));

var servicio = new PrestamoService(new NotificadorConsola());

foreach (var material in biblioteca.Materiales)
{
    Console.WriteLine(material.Ficha());
}

if (biblioteca.Buscar("D-003") is IPrestable dvd)
{
    servicio.Prestar(dvd, "luis@ejemplo.com");
}
```

Salida:

```text
D-003 · El secreto de sus ojos (2009) · DVD · 129 min
→ luis@ejemplo.com: Prestado: El secreto de sus ojos. Vence en 3 días.
```

`PrestamoService` no cambió. Esa es la prueba de que el diseño estaba abierto a extensión y cerrado a modificación.

</details>

## Resumen

- Una clase abstracta concentra estado y comportamiento común; una interfaz define una capacidad.
- La composición permite que `Biblioteca` y `PrestamoService` colaboren sin heredar entre sí.
- Inyectar `INotificador` desacopla los avisos de la lógica de préstamos.
- Los records son buenos para registros inmutables como `Prestamo`.
- Un buen diseño agrega tipos nuevos sin modificar el código existente.

## Siguiente paso

Terminaste el módulo de orientación a objetos. En el módulo 4 vas a trabajar con colecciones, LINQ y genéricos para consultar y transformar datos de forma expresiva.
