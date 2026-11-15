---
slug: interfaces
title: "Interfaces"
order: 7
estimatedMinutes: 15
summary: "Contratos, implementación múltiple y bajo acoplamiento: cuándo usar interfaces y cómo aprovechar IDisposable."
quiz:
  - question: "¿Qué define una interfaz?"
    options:
      - "Un contrato de miembros que una clase se compromete a implementar"
      - "Una clase con estado compartido"
      - "Un tipo que no puede heredarse"
      - "Un método privado"
    answer: 0
    explanation: "Una interfaz declara qué operaciones ofrece un tipo, sin decir cómo las implementa."
  - question: "¿Cuántas interfaces puede implementar una clase?"
    options:
      - "Ninguna"
      - "Exactamente una"
      - "Varias"
      - "Solo si son abstractas"
    answer: 2
    explanation: "C# permite herencia simple de clases, pero implementación múltiple de interfaces."
  - question: "¿Qué interfaz conviene implementar para liberar recursos de forma determinista?"
    options:
      - "IComparable"
      - "IDisposable"
      - "IEnumerable"
      - "INotificador"
    answer: 1
    explanation: "IDisposable declara Dispose(), que se invoca al salir de un bloque using o con una llamada explícita."
---

# Interfaces

## Objetivo

Vas a aprender a definir interfaces como contratos, a implementarlas en una o varias clases y a programar contra la interfaz en lugar de contra una clase concreta. También vas a reconocer cuándo una interfaz del BCL como `IDisposable` resuelve un problema real.

## Requisitos previos

Necesitás manejar clases, herencia y polimorfismo. Si te falta alguna base, repasá [Polimorfismo: virtual y override](/modulos/poo/polimorfismo) y [Clases abstractas](/modulos/poo/clases-abstractas).

## Concepto

Una **interfaz** declara un contrato: un conjunto de propiedades, métodos y eventos que un tipo se compromete a ofrecer. No describe el estado interno ni impone una jerarquía de herencia. La convención en C# es que su nombre empiece con `I`: `IPrestable`, `INotificador`, `IDisposable`.

```csharp
public interface IPrestable
{
    string Titulo { get; }
    bool EstaDisponible { get; }
    void Prestar(string socio);
    void Devolver();
}
```

Una clase implementa una interfaz con dos puntos y debe cubrir todos sus miembros:

```csharp
public sealed class Libro : IPrestable
{
    // ...
}
```

A diferencia de una clase abstracta, una clase puede implementar **varias interfaces**. Eso permite componer capacidades: un objeto puede ser prestable, reservable y comparable al mismo tiempo, sin una jerarquía profunda.

La interfaz también sirve para **desacoplar**. Si un método recibe `IPrestable`, no necesita saber si le pasan un libro, una revista o un DVD. Solo usa el contrato. Esa es la base de la inyección de dependencias y de gran parte del diseño de software.

Desde C# 8 las interfaces pueden tener implementaciones por defecto, pero conviene usarlas con cuidado: la mayoría del código se entiende mejor con interfaces que solo declaran el contrato. En esta lección nos quedamos con ese uso clásico.

`IDisposable` es una interfaz del BCL que declara `Dispose()`. Se usa para liberar recursos no administrados o costosos, como archivos, conexiones o flujos. El bloque `using` garantiza que `Dispose()` se llame al salir, incluso si hay una excepción.

## Ejemplo mínimo

```csharp
// IPrestable.cs
public interface IPrestable
{
    string Titulo { get; }
    bool EstaDisponible { get; }
    void Prestar(string socio);
    void Devolver();
}
```

```csharp
// Libro.cs
public sealed class Libro : IPrestable
{
    public Libro(string titulo) => Titulo = titulo;

    public string Titulo { get; }
    public bool EstaDisponible { get; private set; } = true;

    public void Prestar(string socio)
    {
        if (!EstaDisponible)
            throw new InvalidOperationException($"'{Titulo}' ya está prestado.");
        EstaDisponible = false;
    }

    public void Devolver() => EstaDisponible = true;
}
```

```csharp
// Program.cs
IPrestable material = new Libro("El Aleph");
material.Prestar("ana@ejemplo.com");

Console.WriteLine($"{material.Titulo}: disponible = {material.EstaDisponible}");
material.Devolver();
Console.WriteLine($"Después de devolver: {material.EstaDisponible}");
```

Salida:

```text
El Aleph: disponible = False
Después de devolver: True
```

La variable es de tipo `IPrestable`. Solo puede usar los miembros del contrato, aunque el objeto real sea un `Libro`.

## Ejemplo aplicado

Una biblioteca presta libros y revistas. Ambos implementan `IPrestable`, así que el mismo método puede recibir cualquiera de los dos.

```csharp
// IPrestable.cs
public interface IPrestable
{
    string Titulo { get; }
    bool EstaDisponible { get; }
    void Prestar(string socio);
    void Devolver();
}
```

```csharp
// Libro.cs
public sealed class Libro : IPrestable
{
    public Libro(string titulo) => Titulo = titulo;

    public string Titulo { get; }
    public bool EstaDisponible { get; private set; } = true;

    public void Prestar(string socio)
    {
        if (!EstaDisponible)
            throw new InvalidOperationException($"'{Titulo}' ya está prestado.");
        EstaDisponible = false;
    }

    public void Devolver() => EstaDisponible = true;
}
```

```csharp
// Revista.cs
public sealed class Revista : IPrestable
{
    public Revista(string titulo, int numero)
    {
        Titulo = titulo;
        Numero = numero;
    }

    public string Titulo { get; }
    public int Numero { get; }
    public bool EstaDisponible { get; private set; } = true;

    public void Prestar(string socio)
    {
        if (!EstaDisponible)
            throw new InvalidOperationException($"'{Titulo}' ya está prestado.");
        EstaDisponible = false;
    }

    public void Devolver() => EstaDisponible = true;
}
```

```csharp
// Prestamos.cs
public static class Prestamos
{
    public static void Prestar(IPrestable material, string socio)
    {
        material.Prestar(socio);
        Console.WriteLine($"Prestado a {socio}: {material.Titulo}");
    }
}
```

```csharp
// Program.cs
IPrestable[] materiales =
[
    new Libro("El Aleph"),
    new Revista("Revista Ñ", 214)
];

foreach (var material in materiales)
{
    Prestamos.Prestar(material, "ana@ejemplo.com");
}
```

Salida:

```text
Prestado a ana@ejemplo.com: El Aleph
Prestado a ana@ejemplo.com: Revista Ñ
```

`Prestamos.Prestar` no menciona `Libro` ni `Revista`. Si mañana agregás un DVD prestable, el método sigue funcionando sin cambios.

Un ejemplo de `IDisposable` con un recurso que conviene liberar:

```csharp
// LectorCatalogo.cs
public sealed class LectorCatalogo : IDisposable
{
    private readonly StringReader _reader;

    public LectorCatalogo(string contenido) => _reader = new StringReader(contenido);

    public string? LeerLinea() => _reader.ReadLine();

    public void Dispose() => _reader.Dispose();

    public static void Ejemplo()
    {
        using var lector = new LectorCatalogo("El Aleph\nCien años de soledad");
        Console.WriteLine(lector.LeerLinea());
    }
}
```

`LectorCatalogo.Ejemplo()` imprime la primera línea y libera el lector al salir del método.

Salida:

```text
El Aleph
```

Al salir del bloque `using`, se llama a `Dispose()`. También podés usar `using var lector = ...;`, que libera el recurso al final del ámbito.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `CS0535: no implementa el miembro de la interfaz` | Falta un miembro del contrato | Implementar todos los miembros o declarar la clase abstracta |
| `CS1061` al llamar un método extra | La variable es de tipo interfaz y el método no está en el contrato | Usar el tipo concreto o ampliar el contrato |
| Interfaz con demasiados miembros | Se obliga a implementar cosas que no todos necesitan | Dividir en interfaces más pequeñas y cohesivas |
| Poner estado en una interfaz | Las interfaces no guardan estado de instancia | Usar propiedades y clases para el estado |
| No liberar un `IDisposable` | Se olvidó `using` o `Dispose()` | Usar `using` o llamar a `Dispose()` en `finally` |
| Implementación explícita que no se encuentra | El miembro solo es visible a través de la interfaz | Llamarlo desde una variable del tipo interfaz |

## Buenas prácticas

- Nombrá las interfaces con `I` inicial y un sustantivo o adjetivo claro: `IPrestable`, `INotificador`.
- Diseñá interfaces pequeñas, con una responsabilidad. Es mejor varias interfaces chicas que una gigante.
- Hacé que las clases dependan de interfaces cuando necesiten colaborar con otras clases.
- Implementá `IDisposable` solo cuando haya un recurso que liberar, y usá `using`.
- No uses una interfaz solo para “tener una interfaz”: si no aporta un contrato o desacoplamiento, no agregues complejidad.
- Documentá el comportamiento esperado: qué excepciones puede lanzar cada método y qué invariantes respeta.

## Ejercicio

Creá una interfaz `IReservable` con `EstaReservado`, `Reservar(string socio)` y `CancelarReserva()`. Implementala en `Libro`, que además sigue siendo `IPrestable`. Un libro no puede reservarse si está prestado, no puede prestarse a otra persona si está reservado y no puede reservarse dos veces.

Probá reservar un libro, prestarlo al mismo socio y mostrá el estado final.

<details>
<summary>Ver solución</summary>

```csharp
// IPrestable.cs
public interface IPrestable
{
    string Titulo { get; }
    bool EstaDisponible { get; }
    void Prestar(string socio);
    void Devolver();
}
```

```csharp
// IReservable.cs
public interface IReservable
{
    bool EstaReservado { get; }
    void Reservar(string socio);
    void CancelarReserva();
}
```

```csharp
// Libro.cs
public sealed class Libro : IPrestable, IReservable
{
    public Libro(string titulo) => Titulo = titulo;

    public string Titulo { get; }
    public bool EstaDisponible { get; private set; } = true;
    public bool EstaReservado { get; private set; }
    public string? SocioReserva { get; private set; }

    public void Prestar(string socio)
    {
        if (!EstaDisponible)
            throw new InvalidOperationException("El libro ya está prestado.");
        if (EstaReservado && SocioReserva != socio)
            throw new InvalidOperationException($"Reservado por {SocioReserva}.");

        EstaDisponible = false;
        EstaReservado = false;
        SocioReserva = null;
    }

    public void Devolver() => EstaDisponible = true;

    public void Reservar(string socio)
    {
        if (!EstaDisponible)
            throw new InvalidOperationException("No se puede reservar un libro prestado.");
        if (EstaReservado)
            throw new InvalidOperationException("El libro ya está reservado.");

        EstaReservado = true;
        SocioReserva = socio;
    }

    public void CancelarReserva()
    {
        if (!EstaReservado)
            throw new InvalidOperationException("El libro no está reservado.");

        EstaReservado = false;
        SocioReserva = null;
    }
}
```

```csharp
// Program.cs
var libro = new Libro("El Aleph");
libro.Reservar("ana@ejemplo.com");
Console.WriteLine($"Reservado: {libro.EstaReservado} · por {libro.SocioReserva}");

libro.Prestar("ana@ejemplo.com");
Console.WriteLine($"Disponible: {libro.EstaDisponible} · reservado: {libro.EstaReservado}");
```

Salida:

```text
Reservado: True · por ana@ejemplo.com
Disponible: False · reservado: False
```

</details>

## Resumen

- Una interfaz es un contrato de miembros, sin implementación ni estado de instancia.
- Una clase puede implementar varias interfaces y así componer capacidades.
- Programar contra una interfaz reduce el acoplamiento entre clases.
- `IDisposable` y `using` resuelven la liberación determinista de recursos.
- Las interfaces deben ser pequeñas y tener una responsabilidad clara.

## Siguiente paso

Ya sabés crear contratos y desacoplar código. En la próxima lección vas a comparar la herencia con la composición y a decidir cuál conviene en cada caso: [Composición vs. herencia](/modulos/poo/composicion-herencia).
