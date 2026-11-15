---
slug: entrada-salida-consola
title: "Entrada y salida por consola"
order: 9
estimatedMinutes: 12
summary: "Leer datos del usuario, validarlos y mostrar resultados con formato en una app de consola."
quiz:
  - question: "¿Qué devuelve Console.ReadLine cuando no hay más entrada disponible?"
    options:
      - "Una cadena vacía"
      - "null"
      - "0"
      - "Lanza siempre una excepción"
    answer: 1
    explanation: "ReadLine devuelve null al final de la entrada. Por eso conviene tratarlo como string? y validarlo."
  - question: "¿Cuál es la forma recomendada de convertir a número un dato ingresado por el usuario?"
    options:
      - "int.Parse"
      - "Convert.ToInt32"
      - "int.TryParse"
      - "Un cast directo"
    answer: 2
    explanation: "TryParse devuelve false ante un formato inválido y evita que el programa termine con una excepción."
  - question: "¿Qué hace el formato :F2 en una interpolación?"
    options:
      - "Muestra el valor con dos decimales"
      - "Convierte el valor a mayúsculas"
      - "Redondea a dos enteros"
      - "Agrega dos espacios"
    answer: 0
    explanation: "F2 es un formato numérico de punto fijo con dos decimales."
---

# Entrada y salida por consola

## Objetivo

Al terminar esta lección vas a poder pedir datos al usuario, leerlos de forma segura y mostrar resultados con formato. También vas a saber qué hacer cuando la entrada es inválida o cuando `ReadLine` devuelve `null`.

## Requisitos previos

Haber leído [Nullable reference types](/modulos/csharp-desde-cero/nullable-reference-types). Necesitás manejar `string?`, validaciones y conversiones con `TryParse`.

## Concepto

### Escribir en la consola

`Console.WriteLine` escribe un texto y agrega un salto de línea. `Console.Write` escribe sin saltear:

```csharp
Console.Write("¿Cómo te llamás? ");
string? nombre = Console.ReadLine();
Console.WriteLine($"Hola, {nombre ?? "invitado"}!");
```

Usá `Write` para las preguntas y `WriteLine` para los resultados. Así el usuario escribe en la misma línea que la pregunta y la salida queda ordenada.

### Leer una línea

`Console.ReadLine` devuelve un `string?`. Puede devolver `null` si no hay más entrada, por ejemplo cuando el programa recibe datos desde un archivo o cuando se cierra la entrada estándar. Nunca asumas que el texto no es null:

```csharp
string? entrada = Console.ReadLine();

if (entrada is null)
{
    Console.WriteLine("No se recibió entrada.");
    return;
}
```

Un string vacío (`""`) también es una entrada válida desde el punto de vista de `ReadLine`, pero probablemente no sea un dato útil. Para cubrir ambos casos usá `string.IsNullOrWhiteSpace`.

### Convertir lo que el usuario escribe

Todo lo que llega por consola es texto. Si necesitás un número, convertilo con `TryParse`:

```csharp
Console.Write("Edad: ");
string? entrada = Console.ReadLine();

if (!int.TryParse(entrada, out int edad))
{
    Console.WriteLine("Eso no es un número entero.");
    return;
}

Console.WriteLine($"Tenés {edad} años.");
```

`TryParse` no lanza una excepción cuando el formato es incorrecto. Devuelve `false` y deja la variable de salida en su valor predeterminado. Para `double` y `decimal` funciona igual, pero tené en cuenta la cultura: en algunos países se usa la coma como separador decimal y en otros el punto.

### Validar hasta que el dato sea correcto

En una aplicación interactiva conviene repetir la pregunta hasta obtener un valor válido. Un `while` con una condición de salida clara resuelve el problema:

```csharp
int edad;

while (true)
{
    Console.Write("Edad (0-120): ");
    string? entrada = Console.ReadLine();

    if (int.TryParse(entrada, out edad) && edad is >= 0 and <= 120)
    {
        break;
    }

    Console.WriteLine("Ingresá un número entre 0 y 120.");
}
```

El patrón `edad is >= 0 and <= 120` combina dos comparaciones. Es equivalente a `edad >= 0 && edad <= 120`, pero se lee como una sola condición.

### Formato de salida

La interpolación permite formatear números, fechas y alinear columnas:

```csharp
decimal precio = 1234.5m;
DateTime hoy = DateTime.Today;

Console.WriteLine($"{precio:F2}");          // 1234,50 o 1234.50
Console.WriteLine($"{precio,10:F2}");       // alineado a la derecha en 10 caracteres
Console.WriteLine($"{precio:C}");           // formato de moneda según la cultura
Console.WriteLine($"{hoy:yyyy-MM-dd}");     // 2026-11-11
```

Algunos formatos útiles:

| Formato | Resultado |
|---|---|
| `F2` | Dos decimales fijos |
| `N2` | Separador de miles y dos decimales |
| `C` | Moneda según la configuración regional |
| `P0` | Porcentaje sin decimales |
| `yyyy-MM-dd` | Fecha en formato ISO |

La salida de `C`, `N2` y los separadores depende de la cultura de la máquina. Si necesitás un formato estable para una API o un archivo, especificá `CultureInfo.InvariantCulture`. Para una app de consola local, la cultura del sistema suele ser lo esperado.

### Salida de error

Cuando el programa se ejecuta en una terminal, `Console.Error` escribe en la salida de error estándar. Es útil para separar mensajes de diagnóstico de la salida normal:

```csharp
Console.Error.WriteLine("No se pudo leer el archivo.");
```

Para un ejercicio de consola no es obligatorio, pero es una buena costumbre cuando la aplicación va a ejecutarse en un servidor.

## Ejemplo mínimo

```csharp
Console.Write("¿Cómo te llamás? ");
string? nombre = Console.ReadLine();

Console.WriteLine($"Hola, {nombre ?? "invitado"}!");
```

Si el usuario escribe `Ana`, la salida es:

```text
Hola, Ana!
```

## Ejemplo aplicado

Una calculadora sencilla pide dos números y una operación, valida la entrada y evita dividir por cero.

```csharp
Console.Write("Primer número: ");
string? entrada1 = Console.ReadLine();

Console.Write("Segundo número: ");
string? entrada2 = Console.ReadLine();

if (!double.TryParse(entrada1, out double a) || !double.TryParse(entrada2, out double b))
{
    Console.WriteLine("Los números no son válidos.");
    return;
}

Console.Write("Operación (+, -, *, /): ");
string? operacion = Console.ReadLine();

double? resultado = operacion switch
{
    "+" => a + b,
    "-" => a - b,
    "*" => a * b,
    "/" when b != 0 => a / b,
    "/" => null,
    _ => null
};

if (resultado is null)
{
    Console.WriteLine("Operación inválida o división por cero.");
}
else
{
    Console.WriteLine($"Resultado: {resultado.Value:F2}");
}
```

La expresión `switch` devuelve un `double?`: un número cuando la operación es válida y `null` cuando no lo es. El `if` final decide qué mostrar. Fijate que la división por cero se trata como un caso más, sin excepciones.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `NullReferenceException` al usar la entrada | `ReadLine` devolvió `null` | Validar con `is null` o `IsNullOrWhiteSpace` |
| El programa termina con `FormatException` | Se usó `int.Parse` con texto inválido | Usar `TryParse` |
| El decimal se interpreta mal | La cultura usa coma en lugar de punto | Probar con el separador local o usar `InvariantCulture` |
| La pregunta y la respuesta quedan en líneas separadas | Se usó `WriteLine` para la pregunta | Usar `Console.Write` |
| El menú no termina | La opción se lee fuera del bucle | Leer y validar dentro del bucle |

## Buenas prácticas

- Tratá siempre `ReadLine` como `string?`.
- Validá el dato antes de usarlo y mostrá un mensaje claro cuando falla.
- Usá `TryParse` en lugar de `Parse` para entradas externas.
- Mostrá las unidades o el rango esperado en la pregunta.
- Formateá los importes con `F2` o `C` según el contexto.
- No mezcles la lógica de cálculo con la lectura y la escritura: separá en métodos cuando el programa crezca.

## Ejercicio

Pedí una temperatura en grados Celsius, validá que sea un número y convertila a Fahrenheit con la fórmula `F = C * 9 / 5 + 32`. Mostrá el resultado con un decimal. Si el dato no es numérico, mostrá un mensaje claro.

<details>
<summary>Ver solución</summary>

```csharp
Console.Write("Temperatura en Celsius: ");
string? entrada = Console.ReadLine();

if (!double.TryParse(entrada, out double celsius))
{
    Console.WriteLine("La temperatura no es un número válido.");
    return;
}

double fahrenheit = celsius * 9 / 5 + 32;
Console.WriteLine($"{celsius:F1} °C equivalen a {fahrenheit:F1} °F.");
```

</details>

## Resumen

- `Console.Write` no agrega salto de línea; `WriteLine` sí.
- `ReadLine` devuelve `string?` y puede ser `null`.
- `TryParse` convierte texto a número sin lanzar excepciones.
- Los bucles permiten insistir hasta obtener un dato válido.
- La interpolación con formatos como `F2` y `C` mejora la presentación de los resultados.

## Siguiente paso

En [Ejercicio integrador y quiz](/modulos/csharp-desde-cero/ejercicio-integrador) vas a combinar variables, condicionales, bucles, colecciones, métodos y nulos en un programa completo.
