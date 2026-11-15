---
slug: nullable-reference-types
title: "Nullable reference types"
order: 8
estimatedMinutes: 15
summary: "Usar tipos anulables para expresar cuándo falta un valor y evitar NullReferenceException."
quiz:
  - question: "¿Qué significa declarar string? nombre?"
    options:
      - "Que nombre siempre tiene un valor"
      - "Que nombre puede ser null"
      - "Que nombre es un número"
      - "Que nombre no se puede modificar"
    answer: 1
    explanation: "El signo ? indica que la variable puede contener null. El compilador te obliga a considerarlo."
  - question: "¿Qué hace el operador ?. al acceder a un miembro?"
    options:
      - "Lanza una excepción si el objeto es null"
      - "Accede solo si el objeto no es null y devuelve null en caso contrario"
      - "Convierte el objeto a string"
      - "Elimina el objeto"
    answer: 1
    explanation: "El acceso condicional ?. evita la excepción y propaga null cuando el objeto es null."
  - question: "¿Qué ocurre al leer .Value de un int? que contiene null?"
    options:
      - "Devuelve 0"
      - "Devuelve null"
      - "Lanza InvalidOperationException"
      - "El código no compila"
    answer: 2
    explanation: "Value solo es seguro cuando HasValue es true. Si el nullable está vacío, lanza InvalidOperationException."
---

# Nullable reference types

## Objetivo

Al terminar esta lección vas a poder expresar en el tipo cuándo un valor puede faltar, validar `null` antes de usarlo y evitar `NullReferenceException`. También vas a manejar los tipos anulables por valor, como `int?`.

## Requisitos previos

Haber leído [Métodos y parámetros](/modulos/csharp-desde-cero/metodos). Necesitás entender tipos por valor y por referencia, y métodos que devuelven resultados.

## Concepto

### Qué es `null`

`null` significa “no hay valor”. Es distinto de un string vacío `""` y distinto de `0`. Una variable de tipo referencia puede apuntar a un objeto o no apuntar a nada.

```csharp
string? nombre = null;
Console.WriteLine(nombre); // no imprime nada
```

Si intentás usar un miembro de algo que es `null`, el programa falla:

```csharp
string? nombre = null;
Console.WriteLine(nombre.Length); // NullReferenceException
```

### Nullable reference types

Durante mucho tiempo, C# no distinguía entre un string que podía ser `null` y uno que no. Los proyectos modernos activan **nullable reference types**, una función del compilador que hace explícita esa diferencia.

En un proyecto nuevo de .NET, el archivo `.csproj` suele incluir:

```xml
<Nullable>enable</Nullable>
```

Con esa opción activa:

- `string` significa “no debería ser null”.
- `string?` significa “puede ser null”.

El compilador no cambia el runtime: genera advertencias para que revises los casos dudosos. Una advertencia de nulabilidad no siempre impide compilar, pero ignorarla es una mala idea.

### Flujo y validación

El compilador analiza el flujo del programa. Si comprobás que una variable no es null, a partir de ese punto la trata como no anulable:

```csharp
string? nombre = Console.ReadLine();

if (nombre is not null)
{
    Console.WriteLine(nombre.Length);
}
```

Dentro del `if`, `nombre` se considera seguro. Después del bloque vuelve a ser anulable.

También podés usar `string.IsNullOrWhiteSpace`, que cubre `null`, texto vacío y texto con solo espacios:

```csharp
string? entrada = Console.ReadLine();

if (string.IsNullOrWhiteSpace(entrada))
{
    Console.WriteLine("No ingresaste nada.");
    return;
}

Console.WriteLine(entrada.Trim());
```

La anotación `[NotNullWhen(false)]` de `IsNullOrWhiteSpace` le dice al compilador que, si el método devuelve `false`, el argumento no es null. Por eso `entrada.Trim()` compila sin advertencias.

### Operadores para trabajar con null

El operador de coalescencia `??` devuelve el lado derecho cuando el izquierdo es null:

```csharp
string? nombre = null;
string mostrado = nombre ?? "Invitado";
Console.WriteLine(mostrado); // Invitado
```

`??=` asigna solo si la variable es null:

```csharp
string? nombre = null;
nombre ??= "Invitado";
Console.WriteLine(nombre); // Invitado
```

El acceso condicional `?.` llama a un miembro solo si el objeto no es null. Si es null, toda la expresión devuelve null:

```csharp
string? nombre = null;
Console.WriteLine(nombre?.Length); // no imprime nada
Console.WriteLine(nombre?.Length ?? 0); // 0
```

Podés encadenar `?.`:

```csharp
string? ciudad = persona?.Direccion?.Ciudad;
```

Si cualquier eslabón es null, el resultado es null.

### Tipos anulables por valor

Los tipos por valor, como `int`, no aceptan `null` por defecto. Para permitirlo, se usa `Nullable<T>`, que se escribe con `?`:

```csharp
int? edad = null;
Console.WriteLine(edad.HasValue);          // False
Console.WriteLine(edad.GetValueOrDefault()); // 0

edad = 30;
Console.WriteLine(edad.Value); // 30
```

`HasValue` indica si hay valor. `Value` lanza `InvalidOperationException` si el nullable está vacío. Si no estás seguro, usá `GetValueOrDefault()` o el operador `??`:

```csharp
int edadFinal = edad ?? 0;
```

### El operador de perdón `!`

El operador `!` le dice al compilador “confiá en mí, esto no es null”:

```csharp
string? nombre = ObtenerNombre();
Console.WriteLine(nombre!.Length);
```

Si te equivocás, el programa falla igual en tiempo de ejecución. Usalo solo cuando tengas una garantía que el compilador no puede ver, y dejá un comentario que explique por qué.

## Ejemplo mínimo

```csharp
string? nombre = null;

Console.WriteLine(nombre?.Length ?? 0); // 0

nombre ??= "Invitado";
Console.WriteLine(nombre.Length);       // 7
```

## Ejemplo aplicado

Un método busca un correo en una lista y devuelve `null` si no lo encuentra. El tipo de retorno `string?` obliga a quien llama a manejar ese caso.

```csharp
var emails = new List<string> { "ana@empresa.com", "luis@empresa.com" };

string? email = BuscarEmail(emails, "ana");

if (email is not null)
{
    Console.WriteLine(email.ToLowerInvariant());
}
else
{
    Console.WriteLine("No se encontró el correo.");
}

static string? BuscarEmail(List<string> emails, string usuario)
{
    foreach (string email in emails)
    {
        if (email.StartsWith(usuario + "@", StringComparison.OrdinalIgnoreCase))
        {
            return email;
        }
    }

    return null;
}
```

El método no imprime nada ni lanza una excepción: informa con el tipo que el resultado puede faltar. La decisión de qué hacer queda en el llamador.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `NullReferenceException` | Se usó un miembro de una variable null | Validar con `is not null` o `string.IsNullOrWhiteSpace` |
| Advertencia CS8602 | Se desreferencia un valor que puede ser null | Agregar la comprobación o usar `?.` |
| `InvalidOperationException` en `.Value` | El `int?` no tiene valor | Revisar `HasValue` o usar `GetValueOrDefault()` |
| Se silencian todas las advertencias con `!` | Se usa el operador de perdón sin garantía | Validar de verdad o rediseñar el método |
| Se compara `null` con `""` | No son lo mismo | Usar `string.IsNullOrEmpty` o `IsNullOrWhiteSpace` |

## Buenas prácticas

- Marcá con `?` solo lo que realmente puede faltar.
- Dejá que el compilador te avise: no ignores las advertencias de nulabilidad.
- Validá en el borde del programa, cuando entran datos externos.
- Devolvé `null` con intención y documentá qué significa.
- Preferí `is null` e `is not null` sobre `== null` para mayor claridad.
- Evitá `!` salvo que tengas una razón concreta y comprobable.

## Ejercicio

Creá un método `ObtenerEmail` que reciba un `string?` y devuelva el correo en minúsculas si es válido, o `"sin-email"` si es null, está vacío o no contiene `@`. Probá con `null`, con `"  ANA@EMPRESA.COM  "` y con `"texto"`.

<details>
<summary>Ver solución</summary>

```csharp
Console.WriteLine(ObtenerEmail(null));
Console.WriteLine(ObtenerEmail("  ANA@EMPRESA.COM  "));
Console.WriteLine(ObtenerEmail("texto"));

static string ObtenerEmail(string? email)
{
    if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
    {
        return "sin-email";
    }

    return email.Trim().ToLowerInvariant();
}
```

Salida esperada:

```text
sin-email
ana@empresa.com
sin-email
```

</details>

## Resumen

- `null` significa ausencia de valor y no es lo mismo que un string vacío.
- Con nullable reference types, `string?` indica que el valor puede ser null.
- El compilador analiza el flujo y reconoce las validaciones.
- `??`, `??=` y `?.` simplifican el manejo de null.
- `int?` permite representar un número ausente y expone `HasValue` y `Value`.

## Siguiente paso

En [Entrada y salida por consola](/modulos/csharp-desde-cero/entrada-salida-consola) vas a leer datos del usuario, validarlos y mostrar resultados con formato.
