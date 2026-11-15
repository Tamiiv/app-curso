---
slug: polimorfismo
title: "Polimorfismo: virtual y override"
order: 5
estimatedMinutes: 15
summary: "virtual, override y despacho dinámico: un mismo mensaje con respuestas distintas según el objeto."
quiz:
  - question: "¿Qué permite la palabra virtual en un método?"
    options:
      - "Que el método pueda sobrescribirse en clases derivadas"
      - "Que el método no pueda llamarse"
      - "Que una clase no pueda heredar"
      - "Que un campo sea constante"
    answer: 0
    explanation: "virtual marca un método como extensible: las clases derivadas pueden redefinirlo con override."
  - question: "¿Qué palabra se usa en la clase derivada para redefinir un método virtual?"
    options:
      - "new"
      - "override"
      - "base"
      - "sealed"
    answer: 1
    explanation: "override reemplaza la implementación virtual para ese tipo y participa del despacho dinámico."
  - question: "¿Qué determina qué versión de un método virtual se ejecuta?"
    options:
      - "El tipo declarado de la variable"
      - "El tipo real del objeto en tiempo de ejecución"
      - "El nombre de la variable"
      - "El espacio de nombres"
    answer: 1
    explanation: "El polimorfismo se resuelve con el tipo real del objeto, no con el tipo estático de la variable."
---

# Polimorfismo: virtual y override

## Objetivo

Vas a aprender a declarar métodos `virtual` y a redefinirlos con `override` para que un mismo mensaje produzca comportamientos distintos según el objeto. También vas a distinguir la sobrescritura real de la ocultación con `new` y a usar `base` y `sealed override` con criterio.

## Requisitos previos

Necesitás entender herencia, constructores y clases derivadas. Si no los tenés frescos, volvé a [Herencia](/modulos/poo/herencia).

## Concepto

El **polimorfismo** permite tratar objetos de distintos tipos a través de un tipo común y obtener el comportamiento específico de cada uno. En C#, la herramienta principal es el despacho dinámico de métodos virtuales.

Un método `virtual` declara una implementación que las clases derivadas pueden reemplazar. La derivada usa `override` para reemplazarla. Cuando llamás al método a través de una variable del tipo base, se ejecuta la versión del **tipo real** del objeto, no la del tipo de la variable.

```csharp
MaterialBiblioteca material = new Libro("El Aleph");
Console.WriteLine(material.DiasDePrestamo()); // ejecuta Libro.DiasDePrestamo
```

La variable es de tipo `MaterialBiblioteca`, pero el objeto es un `Libro`. El runtime consulta el tipo real y elige la implementación correcta. Eso es lo que hace que una lista de materiales pueda procesarse de forma uniforme.

Hay dos palabras que se confunden:

- `override` reemplaza un miembro virtual y participa del polimorfismo.
- `new` oculta un miembro heredado. No es polimorfismo: si llamás a través del tipo base, se ejecuta la versión de la base.

`base` permite llamar a la implementación de la clase base desde una sobrescritura. Es útil para extender el comportamiento en lugar de reemplazarlo por completo.

`sealed override` cierra una sobrescritura: las clases que hereden de esa derivada ya no podrán volver a redefinir el método. Se usa cuando una implementación debe quedar fija en esa rama de la jerarquía.

## Ejemplo mínimo

```csharp
// MaterialBiblioteca.cs
public class MaterialBiblioteca
{
    public MaterialBiblioteca(string titulo) => Titulo = titulo;

    public string Titulo { get; }

    public virtual int DiasDePrestamo() => 7;

    public virtual string Ficha() => $"{Titulo} · {DiasDePrestamo()} días";
}
```

```csharp
// Libro.cs
public class Libro : MaterialBiblioteca
{
    public Libro(string titulo) : base(titulo) { }

    public override int DiasDePrestamo() => 21;
}
```

```csharp
// Revista.cs
public class Revista : MaterialBiblioteca
{
    public Revista(string titulo) : base(titulo) { }

    public sealed override int DiasDePrestamo() => 7;
}
```

```csharp
// Pelicula.cs
public sealed class Pelicula : MaterialBiblioteca
{
    public Pelicula(string titulo) : base(titulo) { }

    public override int DiasDePrestamo() => 3;

    public override string Ficha() => $"{base.Ficha()} · audiovisual";
}
```

```csharp
// Program.cs
MaterialBiblioteca[] materiales =
[
    new Libro("El Aleph"),
    new Revista("Revista Ñ"),
    new Pelicula("El secreto de sus ojos")
];

foreach (var material in materiales)
{
    Console.WriteLine(material.Ficha());
}
```

Salida:

```text
El Aleph · 21 días
Revista Ñ · 7 días
El secreto de sus ojos · 3 días · audiovisual
```

El mismo recorrido llama a `Ficha()` en todos los objetos, pero cada uno responde según su tipo. `Pelicula` además reutiliza `base.Ficha()` y le agrega información.

## Ejemplo aplicado

Una biblioteca cobra multas por día de atraso. La tarifa depende del tipo de material, pero el cálculo general es siempre el mismo.

```csharp
// MaterialBiblioteca.cs
public class MaterialBiblioteca
{
    public MaterialBiblioteca(string titulo) => Titulo = titulo;

    public string Titulo { get; }

    public virtual int DiasDePrestamo() => 7;

    public virtual decimal TarifaPorDia() => 50m;
}
```

```csharp
// Libro.cs
public class Libro : MaterialBiblioteca
{
    public Libro(string titulo) : base(titulo) { }

    public override int DiasDePrestamo() => 21;
    public override decimal TarifaPorDia() => 20m;
}
```

```csharp
// Revista.cs
public class Revista : MaterialBiblioteca
{
    public Revista(string titulo) : base(titulo) { }

    public override decimal TarifaPorDia() => 50m;
}
```

```csharp
// Pelicula.cs
public sealed class Pelicula : MaterialBiblioteca
{
    public Pelicula(string titulo) : base(titulo) { }

    public override int DiasDePrestamo() => 3;
    public override decimal TarifaPorDia() => 100m;
}
```

```csharp
// CalculadoraMultas.cs
public static class CalculadoraMultas
{
    public static decimal Calcular(MaterialBiblioteca material, int diasDeAtraso)
    {
        if (diasDeAtraso <= 0)
            return 0m;

        return diasDeAtraso * material.TarifaPorDia();
    }
}
```

```csharp
// Program.cs
var materiales = new List<MaterialBiblioteca>
{
    new Libro("El Aleph"),
    new Revista("Revista Ñ"),
    new Pelicula("El secreto de sus ojos")
};

foreach (var material in materiales)
{
    var multa = CalculadoraMultas.Calcular(material, 4);
    Console.WriteLine($"{material.Titulo}: {multa:0.00}");
}
```

Salida:

```text
El Aleph: 80.00
Revista Ñ: 200.00
El secreto de sus ojos: 400.00
```

`CalculadoraMultas` no tiene un `if` por tipo de material. Si mañana agregás un `Dvd` con su propia tarifa, no hace falta tocarla: alcanza con que herede y sobrescriba `TarifaPorDia()`.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `CS0506: no se puede sobrescribir un miembro no virtual` | La base no declaró el método como `virtual` | Agregar `virtual` en la base o no usar `override` |
| `CS0114: oculta el miembro heredado` | Se usó `new` en lugar de `override` | Usar `override` si se busca polimorfismo |
| La llamada ejecuta la versión de la base | El método no es virtual o se ocultó con `new` | Revisar `virtual` y `override` |
| `CS0507: no se puede cambiar el modificador de acceso` | La sobrescritura reduce la visibilidad | Mantener la misma visibilidad que el método base |
| Llamar a un método virtual en el constructor | La derivada todavía no terminó de inicializarse | Evitar virtuales en constructores |
| `sealed override` en el lugar equivocado | Se intenta seguir heredando de una rama cerrada | Quitar `sealed` o no heredar de esa clase |

## Buenas prácticas

- Marcá como `virtual` solo los métodos que estén pensados como puntos de extensión.
- Documentá el contrato: qué debe cumplir una sobrescritura.
- Usá `base` para extender, no para duplicar lógica.
- Preferí `sealed override` cuando una implementación no deba cambiar más.
- No llames a métodos virtuales desde constructores: el objeto derivado todavía no está completo.
- Si una jerarquía empieza a necesitar muchos `if` por tipo, probablemente falte polimorfismo o una interfaz.

## Ejercicio

Creá una jerarquía de socios. La clase base `Socio` tiene `Nombre` y un método virtual `Descuento()` que devuelve `0m`. `SocioEstudiante` devuelve `0.20m` y `SocioDocente` devuelve `0.10m`. Agregá un método `CuotaMensual(decimal cuotaBase)` en la base que aplique el descuento.

Recorré una lista de socios y mostrá cuánto paga cada uno sobre una cuota de 10.000.

<details>
<summary>Ver solución</summary>

```csharp
// Socio.cs
public class Socio
{
    public Socio(string nombre) => Nombre = nombre;

    public string Nombre { get; }

    public virtual decimal Descuento() => 0m;

    public decimal CuotaMensual(decimal cuotaBase) =>
        cuotaBase * (1 - Descuento());
}
```

```csharp
// SocioEstudiante.cs
public sealed class SocioEstudiante : Socio
{
    public SocioEstudiante(string nombre) : base(nombre) { }

    public override decimal Descuento() => 0.20m;
}
```

```csharp
// SocioDocente.cs
public sealed class SocioDocente : Socio
{
    public SocioDocente(string nombre) : base(nombre) { }

    public override decimal Descuento() => 0.10m;
}
```

```csharp
// Program.cs
Socio[] socios =
[
    new Socio("Ana"),
    new SocioEstudiante("Luis"),
    new SocioDocente("Marta")
];

foreach (var socio in socios)
{
    Console.WriteLine($"{socio.Nombre}: {socio.CuotaMensual(10_000m):0.00}");
}
```

Salida:

```text
Ana: 10000.00
Luis: 8000.00
Marta: 9000.00
```

`CuotaMensual` está en la base y llama a `Descuento()`. Gracias al despacho dinámico, cada socio aplica su propio descuento sin que el recorrido sepa de qué tipo se trata.

</details>

## Resumen

- `virtual` permite que una clase derivada redefina un método con `override`.
- El método que se ejecuta depende del tipo real del objeto, no del tipo de la variable.
- `base` llama a la implementación heredada y permite extenderla.
- `new` oculta, pero no participa del polimorfismo.
- `sealed override` impide seguir sobrescribiendo en clases más profundas.

## Siguiente paso

El polimorfismo funciona, pero la clase base todavía se puede instanciar y no obliga a las derivadas a implementar nada. En la próxima lección vas a usar clases abstractas para expresar contratos incompletos: [Clases abstractas](/modulos/poo/clases-abstractas).
