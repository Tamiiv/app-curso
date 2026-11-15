---
slug: encapsulamiento
title: "Encapsulamiento y modificadores de acceso"
order: 3
estimatedMinutes: 15
summary: "Ocultá el estado interno, validá invariantes y elegí bien entre public, private, protected e internal."
quiz:
  - question: "¿Para qué sirve encapsular?"
    options:
      - "Para que todos los campos sean públicos"
      - "Para proteger invariantes y controlar el acceso al estado"
      - "Para heredar de más clases"
      - "Para evitar usar propiedades"
    answer: 1
    explanation: "Encapsular significa ocultar los detalles internos y exponer solo operaciones que mantengan el objeto válido."
  - question: "¿Qué significa private set en una propiedad?"
    options:
      - "Cualquiera puede leer y escribir"
      - "Todos pueden leer, pero solo la clase puede escribir"
      - "Nadie puede leer la propiedad"
      - "Solo las clases derivadas pueden escribir"
    answer: 1
    explanation: "El get sigue siendo público y el set queda restringido a la propia clase."
  - question: "¿Por qué conviene exponer IReadOnlyList<T> en lugar de List<T>?"
    options:
      - "Porque ocupa menos memoria"
      - "Porque evita que el exterior modifique la lista"
      - "Porque es más rápido de recorrer"
      - "Porque permite agregar elementos"
    answer: 1
    explanation: "IReadOnlyList<T> permite leer y recorrer, pero no agregar ni quitar elementos desde afuera."
---

# Encapsulamiento y modificadores de acceso

## Objetivo

Vas a aprender a ocultar el estado interno de una clase y a exponer solo lo necesario, de modo que nadie pueda dejar el objeto en un estado inválido. También vas a elegir con criterio entre `public`, `private`, `protected`, `internal` y sus combinaciones.

## Requisitos previos

Necesitás manejar clases, propiedades y constructores. Si todavía no los tenés claros, repasá [Constructores y propiedades](/modulos/poo/constructores-propiedades) antes de seguir.

## Concepto

**Encapsular** es esconder los detalles de implementación detrás de una interfaz controlada. La clase decide qué se puede ver y qué no, y protege sus reglas internas, llamadas **invariantes**.

Un ejemplo de invariante: la cantidad de copias disponibles de un libro nunca puede ser negativa ni superar el total de copias. Si el campo es público, cualquiera puede romper esa regla con una asignación. Si es privado y solo se modifica mediante métodos, la clase puede verificarla en un solo lugar.

Los **modificadores de acceso** definen desde dónde se puede usar un miembro:

| Modificador | Acceso |
|---|---|
| `public` | Desde cualquier lugar |
| `private` | Solo desde la clase que lo declara |
| `protected` | Desde la clase y sus clases derivadas |
| `internal` | Desde el mismo ensamblado o proyecto |
| `protected internal` | Desde el mismo ensamblado o desde clases derivadas |
| `private protected` | Desde clases derivadas del mismo ensamblado |

En una clase, los miembros son `private` por defecto. Los tipos de nivel superior, como una clase suelta en un archivo, son `internal` por defecto. Igual conviene escribir el modificador de forma explícita para que la intención quede clara.

Una propiedad puede tener accesores con distinta visibilidad:

```csharp
public string Email
{
    get => _email;
    private set
    {
        if (!value.Contains('@'))
            throw new ArgumentException("Email inválido.", nameof(value));
        _email = value;
    }
}
```

El `get` es público, el `set` es privado. Todos pueden leer el email, pero solo la clase decide cuándo y cómo cambiarlo. Esa combinación se usa mucho para estado que evoluciona mediante métodos de negocio.

También existe `readonly` para campos: una vez asignados en la declaración o en el constructor, no pueden reasignarse. Es útil para dependencias y colecciones internas que no deben reemplazarse.

## Ejemplo mínimo

```csharp
// Libro.cs
public class Libro
{
    private int _copiasDisponibles;

    public Libro(string titulo, int copias)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título es obligatorio.", nameof(titulo));
        if (copias < 0)
            throw new ArgumentOutOfRangeException(nameof(copias), "No puede ser negativo.");

        Titulo = titulo;
        _copiasDisponibles = copias;
    }

    public string Titulo { get; }
    public int CopiasDisponibles => _copiasDisponibles;
    public bool HayEjemplares => _copiasDisponibles > 0;

    public void Prestar()
    {
        if (!HayEjemplares)
            throw new InvalidOperationException("No hay ejemplares disponibles.");
        _copiasDisponibles--;
    }

    public void Devolver() => _copiasDisponibles++;
}
```

```csharp
// Program.cs
var libro = new Libro("El Aleph", 2);
libro.Prestar();

Console.WriteLine($"{libro.Titulo}: quedan {libro.CopiasDisponibles}");
libro.Devolver();
Console.WriteLine($"Disponible: {libro.HayEjemplares}");
```

Salida:

```text
El Aleph: quedan 1
Disponible: True
```

Nadie puede escribir `libro.CopiasDisponibles = 100`. El único camino para cambiar el valor es `Prestar()` o `Devolver()`, y ambos pasan por las validaciones de la clase.

## Ejemplo aplicado

Un socio tiene un email validado y una lista de préstamos que no debería poder modificar desde afuera. La lista se guarda en un campo privado y se expone como solo lectura.

```csharp
// Prestamo.cs
public class Prestamo
{
    public Prestamo(string titulo, DateOnly fecha)
    {
        Titulo = titulo;
        Fecha = fecha;
    }

    public string Titulo { get; }
    public DateOnly Fecha { get; }
}
```

```csharp
// Socio.cs
public class Socio
{
    private readonly List<Prestamo> _prestamos = [];
    private string _email = "";

    public Socio(string nombre, string email)
    {
        Nombre = nombre;
        Email = email;
    }

    public string Nombre { get; }

    public string Email
    {
        get => _email;
        private set
        {
            if (!value.Contains('@'))
                throw new ArgumentException("El email no es válido.", nameof(value));
            _email = value;
        }
    }

    public IReadOnlyList<Prestamo> Prestamos => _prestamos;

    public void RegistrarPrestamo(Prestamo prestamo) => _prestamos.Add(prestamo);

    public void CambiarEmail(string nuevoEmail) => Email = nuevoEmail;
}
```

```csharp
// Program.cs
var socio = new Socio("Ana Gómez", "ana@ejemplo.com");
socio.RegistrarPrestamo(new Prestamo("El Aleph", DateOnly.FromDateTime(DateTime.Today)));

Console.WriteLine($"{socio.Nombre} · {socio.Email}");
Console.WriteLine($"Préstamos: {socio.Prestamos.Count}");

socio.CambiarEmail("ana.gomez@ejemplo.com");
Console.WriteLine($"Nuevo email: {socio.Email}");

// socio.Email = "otro@ejemplo.com"; // error: el set es privado
// socio.Prestamos.Add(...);        // error: IReadOnlyList no tiene Add
```

Salida:

```text
Ana Gómez · ana@ejemplo.com
Préstamos: 1
Nuevo email: ana.gomez@ejemplo.com
```

`IReadOnlyList<T>` permite leer, recorrer y consultar `Count`, pero no agregar ni quitar elementos. La lista real sigue siendo privada y solo `RegistrarPrestamo()` puede modificarla.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Campos públicos en el dominio | Se expone el estado sin control | Convertirlos en propiedades con `private set` o métodos |
| Devolver la lista interna | El llamador puede modificarla | Exponer `IReadOnlyList<T>` o una copia |
| Validar solo en el setter público | El constructor asigna al campo y saltea la validación | Validar en el constructor y en el setter |
| Usar `protected` para todo | Se abre la clase a las derivadas sin necesidad | Empezar con `private` y ampliar solo si hace falta |
| `internal` mal entendido | Se cree que protege entre proyectos del mismo equipo | `internal` es por ensamblado, no por carpeta |
| Propiedad pública que devuelve un objeto mutable | El exterior cambia el objeto interno | Devolver una copia o una vista inmutable |

## Buenas prácticas

- Empezá con lo más restrictivo: `private` por defecto y `public` solo cuando sea necesario.
- Validá las invariantes en un único lugar, normalmente en el constructor y en los métodos que cambian estado.
- Marcá como `readonly` los campos que no deberían reasignarse.
- Exponé colecciones como `IReadOnlyList<T>`, `IReadOnlyCollection<T>` o `IEnumerable<T>`.
- No agregues `get` y `set` automáticos por costumbre. Preguntate si el `set` debe ser público.
- Recordá que encapsular no es solo ocultar: también es ofrecer operaciones con nombres claros que expresen la intención del dominio.

## Ejercicio

Implementá una clase `Libro` con un total de copias fijo y copias disponibles. Agregá métodos `Prestar()`, `Devolver()` y `Reponer(int cantidad)`. Ninguna operación puede dejar las copias disponibles en negativo ni superar el total. El total solo se asigna en el constructor.

Probá prestar dos ejemplares de un libro con tres copias, mostrá el estado y reponé uno.

<details>
<summary>Ver solución</summary>

```csharp
// Libro.cs
public class Libro
{
    private readonly int _copiasTotales;
    private int _copiasDisponibles;

    public Libro(string titulo, int copiasTotales)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título es obligatorio.", nameof(titulo));
        if (copiasTotales <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(copiasTotales),
                "Debe haber al menos un ejemplar.");

        Titulo = titulo;
        _copiasTotales = copiasTotales;
        _copiasDisponibles = copiasTotales;
    }

    public string Titulo { get; }
    public int CopiasTotales => _copiasTotales;
    public int CopiasDisponibles => _copiasDisponibles;

    public void Prestar()
    {
        if (_copiasDisponibles == 0)
            throw new InvalidOperationException("No hay ejemplares disponibles.");
        _copiasDisponibles--;
    }

    public void Devolver()
    {
        if (_copiasDisponibles == _copiasTotales)
            throw new InvalidOperationException("Todos los ejemplares ya están en la biblioteca.");
        _copiasDisponibles++;
    }

    public void Reponer(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser positiva.");
        if (_copiasDisponibles + cantidad > _copiasTotales)
            throw new InvalidOperationException("No se puede superar el total de ejemplares.");

        _copiasDisponibles += cantidad;
    }
}
```

```csharp
// Program.cs
var libro = new Libro("El Aleph", 3);
libro.Prestar();
libro.Prestar();

Console.WriteLine($"{libro.Titulo}: {libro.CopiasDisponibles}/{libro.CopiasTotales}");
libro.Reponer(1);
Console.WriteLine($"Después de reponer: {libro.CopiasDisponibles}/{libro.CopiasTotales}");
```

Salida:

```text
El Aleph: 1/3
Después de reponer: 2/3
```

</details>

## Resumen

- Encapsular es ocultar el estado y exponer operaciones que respeten las invariantes.
- `private set` permite lectura pública y escritura controlada por la clase.
- `readonly` protege campos que no deben reasignarse.
- Exponer `IReadOnlyList<T>` evita que el exterior modifique una colección interna.
- Elegí el modificador de acceso más restrictivo que funcione.

## Siguiente paso

Ya tenés clases que protegen su estado. En la próxima lección vas a reutilizar comportamiento entre clases con herencia: [Herencia](/modulos/poo/herencia).
