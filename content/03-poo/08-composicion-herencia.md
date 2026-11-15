---
slug: composicion-herencia
title: "Composición vs. herencia"
order: 8
estimatedMinutes: 15
summary: "Composición vs. herencia: modelá relaciones has-a, inyectá dependencias y evitá jerarquías frágiles."
quiz:
  - question: "¿Qué relación modela la composición?"
    options:
      - "Es un (is-a)"
      - "Tiene un (has-a)"
      - "Implementa un contrato"
      - "Hereda de una clase base"
    answer: 1
    explanation: "En la composición un objeto contiene o usa a otro; la relación se describe como tiene un."
  - question: "¿Cuál es una ventaja de la composición frente a la herencia?"
    options:
      - "Acopla más las clases"
      - "Permite cambiar la implementación sin modificar la clase que la usa"
      - "Obliga a una jerarquía profunda"
      - "Elimina la necesidad de interfaces"
    answer: 1
    explanation: "Al depender de una interfaz y recibir la implementación desde afuera, se puede cambiar el comportamiento sin tocar el consumidor."
  - question: "¿Qué es inyectar una dependencia?"
    options:
      - "Crear la dependencia con new dentro de la clase"
      - "Recibir la dependencia desde afuera, normalmente por constructor"
      - "Heredar de la dependencia"
      - "Copiar el código de la dependencia"
    answer: 1
    explanation: "La inyección por constructor recibe la dependencia ya construida y facilita el desacoplamiento y las pruebas."
---

# Composición vs. herencia

## Objetivo

Vas a aprender a distinguir cuándo una relación es **es un** y cuándo es **tiene un**, a reemplazar jerarquías innecesarias por composición y a inyectar dependencias para reducir el acoplamiento. También vas a reconocer cuándo la herencia sigue siendo la herramienta correcta.

## Requisitos previos

Necesitás manejar herencia, polimorfismo e interfaces. Si alguno de esos temas no está claro, repasá [Herencia](/modulos/poo/herencia) y [Interfaces](/modulos/poo/interfaces).

## Concepto

La **herencia** dice que una clase derivada **es un** caso particular de la base. La **composición** dice que un objeto **tiene un** colaborador o lo usa para hacer su trabajo.

La herencia acopla fuerte: la derivada depende de los detalles de la base, y un cambio en la base puede romper a todas las derivadas. Además, C# solo permite heredar de una clase. La composición es más flexible: podés cambiar la pieza que se usa, reemplazarla por otra implementación o probarla de forma aislada.

Un error típico es heredar solo para reutilizar código:

```csharp
// Mal diseño: una biblioteca no es una lista de libros
public class Biblioteca : List<Libro>
{
}
```

La biblioteca **tiene** libros, no **es** una lista. Al heredar de `List<Libro>`, expone todos los métodos de la lista y mezcla dos responsabilidades. La versión compuesta es más clara:

```csharp
public class Biblioteca
{
    private readonly List<Libro> _libros = [];

    public IReadOnlyList<Libro> Libros => _libros;

    public void Agregar(Libro libro) => _libros.Add(libro);
}
```

La composición también permite **inyectar dependencias**. En lugar de que una clase cree su colaborador con `new`, lo recibe desde afuera, normalmente por constructor. Así el consumidor depende de una interfaz y no de una implementación concreta.

```csharp
public sealed class PrestamoService
{
    private readonly INotificador _notificador;

    public PrestamoService(INotificador notificador) => _notificador = notificador;
}
```

Eso facilita cambiar `NotificadorConsola` por `NotificadorEnMemoria` en una prueba, sin tocar `PrestamoService`.

## Ejemplo mínimo

```csharp
// Libro.cs
public sealed class Libro
{
    public Libro(string titulo) => Titulo = titulo;

    public string Titulo { get; }
}
```

```csharp
// Biblioteca.cs
public sealed class Biblioteca
{
    private readonly List<Libro> _libros = [];

    public IReadOnlyList<Libro> Libros => _libros;

    public void Agregar(Libro libro) => _libros.Add(libro);
}
```

```csharp
// Program.cs
var biblioteca = new Biblioteca();
biblioteca.Agregar(new Libro("El Aleph"));
biblioteca.Agregar(new Libro("Cien años de soledad"));

Console.WriteLine($"Libros: {biblioteca.Libros.Count}");
```

Salida:

```text
Libros: 2
```

`Biblioteca` contiene libros y controla cómo se agregan. No hereda de `List<Libro>`, así que no arrastra métodos que no forman parte de su responsabilidad.

## Ejemplo aplicado

Un servicio de préstamos necesita avisar al socio. En vez de escribir en la consola directamente, depende de una interfaz `INotificador`. La implementación se elige al crear el servicio.

```csharp
// IPrestable.cs
public interface IPrestable
{
    string Titulo { get; }
    void Prestar(string socio);
}

// INotificador.cs
public interface INotificador
{
    void Enviar(string destino, string mensaje);
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
            throw new InvalidOperationException("El libro ya está prestado.");
        EstaDisponible = false;
    }
}
```

```csharp
// NotificadorConsola.cs
public sealed class NotificadorConsola : INotificador
{
    public void Enviar(string destino, string mensaje) =>
        Console.WriteLine($"→ {destino}: {mensaje}");
}
```

```csharp
// NotificadorEnMemoria.cs
public sealed class NotificadorEnMemoria : INotificador
{
    public List<string> Mensajes { get; } = [];

    public void Enviar(string destino, string mensaje) =>
        Mensajes.Add($"{destino}: {mensaje}");
}
```

```csharp
// PrestamoService.cs
public sealed class PrestamoService
{
    private readonly INotificador _notificador;

    public PrestamoService(INotificador notificador) => _notificador = notificador;

    public void Prestar(IPrestable material, string socio)
    {
        material.Prestar(socio);
        _notificador.Enviar(socio, $"Prestado: {material.Titulo}");
    }
}
```

```csharp
// Program.cs
var servicio = new PrestamoService(new NotificadorConsola());
servicio.Prestar(new Libro("El Aleph"), "ana@ejemplo.com");

var memoria = new NotificadorEnMemoria();
var servicioDePrueba = new PrestamoService(memoria);
servicioDePrueba.Prestar(new Libro("Cien años de soledad"), "luis@ejemplo.com");

Console.WriteLine($"Mensajes en memoria: {memoria.Mensajes.Count}");
```

Salida:

```text
→ ana@ejemplo.com: Prestado: El Aleph
Mensajes en memoria: 1
```

`PrestamoService` no sabe si el aviso va a la consola, a un archivo o a una base de datos. Solo conoce el contrato. Esa es la diferencia entre depender de una implementación y depender de una abstracción.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Heredar para reutilizar código | No existe una relación es un | Usar composición y delegar |
| Jerarquía profunda y frágil | Cada nivel agrega acoplamiento | Preferir piezas pequeñas que se combinan |
| `new` dentro de la clase para crear dependencias | La clase queda atada a una implementación | Recibir la dependencia por constructor |
| Composición sin interfaz | El consumidor depende de una clase concreta | Extraer un contrato cuando haya varias implementaciones |
| Una clase que hace todo | Mezcla catálogo, préstamos y notificaciones | Separar responsabilidades en colaboradores |
| Inyectar demasiadas dependencias | La clase tiene demasiadas razones para cambiar | Dividir la clase o agrupar responsabilidades |

## Buenas prácticas

- Empezá por composición; usá herencia cuando la relación **es un** sea verdadera y estable.
- Hacé que las clases dependan de interfaces cuando necesiten colaborar con otras.
- Inyectá dependencias por constructor y guardalas en campos `readonly`.
- No expongas la lista interna: devolvé `IReadOnlyList<T>` o una vista de solo lectura.
- Mantené las clases chicas y con una sola razón para cambiar.
- Marcá como `sealed` las clases que no estén diseñadas para herencia.

## Ejercicio

Implementá `SocioService`, que da de alta socios y les envía un mensaje de bienvenida. El servicio debe depender de `INotificador` y recibirlo por constructor. Creá dos implementaciones: `NotificadorConsola`, que imprime, y `NotificadorEnMemoria`, que guarda los mensajes en una lista.

Dá de alta un socio con cada implementación y comprobá que el servicio no cambia.

<details>
<summary>Ver solución</summary>

```csharp
// INotificador.cs
public interface INotificador
{
    void Enviar(string destino, string mensaje);
}
```

```csharp
// NotificadorConsola.cs
public sealed class NotificadorConsola : INotificador
{
    public void Enviar(string destino, string mensaje) =>
        Console.WriteLine($"[{destino}] {mensaje}");
}
```

```csharp
// NotificadorEnMemoria.cs
public sealed class NotificadorEnMemoria : INotificador
{
    public List<string> Mensajes { get; } = [];

    public void Enviar(string destino, string mensaje) =>
        Mensajes.Add($"[{destino}] {mensaje}");
}
```

```csharp
// Socio.cs
public sealed class Socio
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
    public string Email { get; }
}
```

```csharp
// SocioService.cs
public sealed class SocioService
{
    private readonly INotificador _notificador;

    public SocioService(INotificador notificador) => _notificador = notificador;

    public void DarDeAlta(Socio socio) =>
        _notificador.Enviar(socio.Email, $"Bienvenido/a, {socio.Nombre}.");
}
```

```csharp
// Program.cs
var consola = new SocioService(new NotificadorConsola());
consola.DarDeAlta(new Socio("Ana Gómez", "ana@ejemplo.com"));

var memoria = new NotificadorEnMemoria();
var prueba = new SocioService(memoria);
prueba.DarDeAlta(new Socio("Luis Pérez", "luis@ejemplo.com"));

Console.WriteLine($"Mensajes guardados: {memoria.Mensajes.Count}");
```

Salida:

```text
[ana@ejemplo.com] Bienvenido/a, Ana Gómez.
Mensajes guardados: 1
```

</details>

## Resumen

- La herencia modela **es un**; la composición modela **tiene un**.
- La composición es más flexible y evita jerarquías frágiles.
- Inyectar dependencias permite cambiar implementaciones sin tocar al consumidor.
- Depender de interfaces reduce el acoplamiento.
- La herencia sigue siendo útil cuando hay una relación de subtipo real y estable.

## Siguiente paso

Ya sabés combinar objetos y desacoplar colaboradores. En la próxima lección vas a conocer los records y a entender qué se copia realmente en un tipo por valor y en uno por referencia: [Records y tipos por valor vs. referencia](/modulos/poo/records-valor-referencia).
