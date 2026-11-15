---
slug: strings
title: "Strings y texto"
order: 3
estimatedMinutes: 12
summary: "Crear, recorrer y transformar texto con los métodos más útiles de string y StringBuilder."
quiz:
  - question: "¿Qué característica tiene un string en C#?"
    options:
      - "Es mutable: se puede cambiar carácter por carácter"
      - "Es inmutable: cada cambio crea un texto nuevo"
      - "Solo puede contener letras"
      - "Se comporta siempre como un número"
    answer: 1
    explanation: "Los strings son inmutables. Métodos como Replace o ToUpper devuelven un string nuevo y no modifican el original."
  - question: "¿Qué hace la interpolación $\"Hola, {nombre}\"?"
    options:
      - "Ejecuta un método llamado nombre"
      - "Reemplaza {nombre} por el valor de la variable"
      - "Convierte el texto a mayúsculas"
      - "Crea una lista de palabras"
    answer: 1
    explanation: "La interpolación evalúa la expresión entre llaves y la inserta en el texto resultante."
  - question: "¿Cuándo conviene usar StringBuilder?"
    options:
      - "Para guardar un solo string corto"
      - "Para comparar dos strings"
      - "Para concatenar mucho texto dentro de un bucle"
      - "Para convertir texto a int"
    answer: 2
    explanation: "StringBuilder evita crear un string nuevo en cada concatenación, algo importante cuando se repite muchas veces."
---

# Strings y texto

## Objetivo

Al terminar esta lección vas a poder crear strings, combinarlos y transformarlos con los métodos más usados. También vas a saber cuándo conviene usar `StringBuilder` y cómo evitar errores frecuentes al trabajar con texto.

## Requisitos previos

Haber leído [Operadores y expresiones](/modulos/csharp-desde-cero/operadores). Necesitás entender variables, comparaciones e interpolación básica.

## Concepto

### Un string es texto inmutable

Un `string` representa una secuencia de caracteres. Es un tipo por referencia, pero es inmutable: una vez creado, su contenido no cambia. Cuando llamás a un método como `ToUpper` o `Replace`, no se modifica el texto original; se devuelve uno nuevo.

```csharp
string saludo = "hola";
string mayusculas = saludo.ToUpper();

Console.WriteLine(saludo);     // hola
Console.WriteLine(mayusculas); // HOLA
```

Esta característica hace que los strings sean seguros de compartir, pero implica que cada transformación crea un objeto nuevo.

### Formas de escribir un string

Lo más común son las comillas dobles:

```csharp
string nombre = "Ana";
```

Dentro de un string normal, la barra invertida introduce secuencias de escape:

```csharp
string ruta = "C:\\cursos\\dotnet";
string linea = "Primera línea\nSegunda línea";
```

Si hay muchas barras invertidas, el string literal verbatim es más cómodo. Se escribe con `@` y no interpreta las barras:

```csharp
string ruta = @"C:\cursos\dotnet";
```

Para textos de varias líneas sin escapes, C# 11 incorporó los strings raw, delimitados por tres comillas:

```csharp
string mensaje = """
    Hola,
    este texto conserva el formato.
    """;
```

La interpolación combina texto y expresiones:

```csharp
string nombre = "Ana";
int edad = 30;
Console.WriteLine($"{nombre} tiene {edad} años.");
```

También podés alinear y formatear valores: `{edad,5}` reserva cinco caracteres y `{precio:F2}` muestra dos decimales.

### Métodos que vas a usar todo el tiempo

| Método o propiedad | Qué hace |
|---|---|
| `Length` | Cantidad de caracteres |
| `ToUpper()` / `ToLower()` | Cambia mayúsculas y minúsculas |
| `Trim()` | Quita espacios al inicio y al final |
| `Contains(texto)` | Indica si el texto aparece |
| `StartsWith(texto)` / `EndsWith(texto)` | Verifica el inicio o el final |
| `Replace(viejo, nuevo)` | Reemplaza todas las apariciones |
| `IndexOf(texto)` | Devuelve la posición o `-1` |
| `Substring(inicio, largo)` | Extrae una parte |
| `Split(separador)` | Divide el texto en partes |
| `string.Join(separador, partes)` | Une partes en un texto |

Un ejemplo:

```csharp
string frase = "  Aprender C# es divertido  ";
string limpia = frase.Trim();

Console.WriteLine(limpia.Length);              // 24
Console.WriteLine(limpia.Contains("C#"));      // True
Console.WriteLine(limpia.Replace("C#", ".NET")); // Aprender .NET es divertido
Console.WriteLine(limpia.IndexOf("es"));       // 12
```

Los índices empiezan en cero. `Substring(0, 8)` devuelve los primeros ocho caracteres. Si el índice o el largo se salen del rango, se lanza `ArgumentOutOfRangeException`.

### Separar y unir

`Split` y `string.Join` son una pareja muy útil:

```csharp
string linea = "Ana,Luis,Marta";
string[] nombres = linea.Split(',');

foreach (string nombre in nombres)
{
    Console.WriteLine(nombre);
}

Console.WriteLine(string.Join(" | ", nombres)); // Ana | Luis | Marta
```

Si puede haber espacios repetidos, usá `StringSplitOptions.RemoveEmptyEntries`:

```csharp
string[] palabras = "  hola   mundo  ".Split(' ', StringSplitOptions.RemoveEmptyEntries);
Console.WriteLine(palabras.Length); // 2
```

### Comparar texto

En C#, `==` compara el contenido de dos strings:

```csharp
string a = "hola";
string b = "ho" + "la";

Console.WriteLine(a == b); // True
```

Esa comparación distingue mayúsculas y minúsculas. Si querés ignorar esa diferencia, usá:

```csharp
bool iguales = string.Equals("Hola", "hola", StringComparison.OrdinalIgnoreCase);
Console.WriteLine(iguales); // True
```

Para ordenar o comparar texto de usuarios, `StringComparison` deja explícita la intención.

### StringBuilder: cuando concatenás mucho

Concatenar con `+` es cómodo para pocas operaciones. Dentro de un bucle grande, cada `+` crea un string nuevo y puede volverse costoso. Ahí entra `StringBuilder`, una clase mutable que acumula texto de forma eficiente:

```csharp
using System.Text;

var builder = new StringBuilder();

for (int i = 1; i <= 5; i++)
{
    builder.AppendLine($"Línea {i}");
}

Console.Write(builder.ToString());
```

Salida:

```text
Línea 1
Línea 2
Línea 3
Línea 4
Línea 5
```

No hace falta usar `StringBuilder` para armar un mensaje de dos líneas. Es una herramienta para cuando el texto se construye en muchas iteraciones.

## Ejemplo mínimo

```csharp
string nombre = "  Ada Lovelace  ";
string limpio = nombre.Trim();

Console.WriteLine(limpio.Length);              // 12
Console.WriteLine(limpio.ToUpper());           // ADA LOVELACE
Console.WriteLine(limpio.Contains("Love"));    // True
Console.WriteLine(limpio.Replace("Ada", "Augusta")); // Augusta Lovelace
Console.WriteLine(limpio.Split(' ')[1]);       // Lovelace
```

## Ejemplo aplicado

Un caso real: convertir el título de un artículo en un slug para una URL.

```csharp
string titulo = "  C# desde cero: strings y texto  ";

string[] partes = titulo
    .Trim()
    .ToLowerInvariant()
    .Replace(":", "")
    .Replace("#", "sharp")
    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

string slug = string.Join("-", partes);
Console.WriteLine(slug);
```

Salida:

```text
csharp-desde-cero-strings-y-texto
```

`ToLowerInvariant` evita depender del idioma configurado en la máquina. `Split` descarta los espacios repetidos y `string.Join` arma el resultado final.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `NullReferenceException` al llamar a `Length` | La variable contiene `null` | Validar con `string.IsNullOrWhiteSpace` antes de usarla |
| `Replace` no cambia el texto original | Los strings son inmutables | Asignar el resultado: `texto = texto.Replace(...)` |
| `Substring` lanza una excepción | El rango se sale del texto | Verificar `Length` antes de cortar |
| `==` no encuentra un texto con otra capitalización | La comparación distingue mayúsculas | Usar `StringComparison.OrdinalIgnoreCase` |
| El programa se vuelve lento concatenando | Se usa `+` dentro de un bucle grande | Usar `StringBuilder` |

## Buenas prácticas

- Usá `Trim()` en datos que vienen del usuario antes de validarlos o guardarlos.
- Preferí la interpolación sobre la concatenación con `+` para mensajes.
- Usá `StringComparison` cuando el caso o la cultura importan.
- No uses `StringBuilder` para textos cortos: agrega complejidad innecesaria.
- Validá `null` y textos vacíos antes de llamar a métodos del string.
- Evitá `Substring` si podés resolverlo con `Split` o `Replace`.

## Ejercicio

Pedí una frase al usuario y mostrá dos datos: la cantidad de palabras, sin contar espacios repetidos, y las iniciales de cada palabra en mayúscula.

<details>
<summary>Ver solución</summary>

```csharp
using System.Text;

Console.Write("Frase: ");
string? frase = Console.ReadLine();

if (string.IsNullOrWhiteSpace(frase))
{
    Console.WriteLine("La frase está vacía.");
    return;
}

string[] palabras = frase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
var iniciales = new StringBuilder();

foreach (string palabra in palabras)
{
    iniciales.Append(char.ToUpperInvariant(palabra[0]));
}

Console.WriteLine($"Palabras: {palabras.Length}");
Console.WriteLine($"Iniciales: {iniciales}");
```

</details>

## Resumen

- Los strings son inmutables: cada transformación devuelve un texto nuevo.
- `Trim`, `Replace`, `Split` y `string.Join` resuelven la mayoría de las tareas de texto.
- `==` compara contenido, pero distingue mayúsculas y minúsculas.
- `StringBuilder` conviene cuando se concatenan muchas partes, especialmente en bucles.
- Validá `null` y textos vacíos antes de operar con un string.

## Siguiente paso

En [Condicionales](/modulos/csharp-desde-cero/condicionales) vas a usar todo lo aprendido para que el programa tome decisiones según los datos.
