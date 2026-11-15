---
slug: variables-tipos
title: "Variables, tipos y conversiones"
order: 1
estimatedMinutes: 15
summary: "Cómo guardar datos, elegir el tipo correcto y convertir valores sin perder información."
quiz:
  - question: "¿Qué tipo conviene para guardar una edad?"
    options:
      - "string"
      - "int"
      - "bool"
      - "DateTime"
    answer: 1
    explanation: "Una edad es un número entero, por eso int es la opción natural."
  - question: "¿Qué hace una conversión explícita?"
    options:
      - "La hace el compilador sola"
      - "Requiere escribir el tipo de destino"
      - "Solo funciona con strings"
      - "Elimina el valor original"
    answer: 1
    explanation: "La conversión explícita, o cast, indica el tipo de destino y asume la responsabilidad de la conversión."
  - question: "¿Cuál es la forma segura de convertir texto a número cuando el dato viene del usuario?"
    options:
      - "int.Parse"
      - "Convert.ToInt32"
      - "int.TryParse"
      - "Un cast directo"
    answer: 2
    explanation: "TryParse devuelve false si el texto no es válido y evita que el programa termine con una excepción."
---

# Variables, tipos y conversiones

## Objetivo

Al terminar esta lección vas a poder declarar variables, elegir el tipo adecuado para cada dato y convertir valores entre tipos sin perder información. También vas a saber cuándo conviene usar `var` y cuándo escribir el tipo explícito.

## Requisitos previos

Haber creado y ejecutado un proyecto de consola. Si todavía no lo hiciste, pasá por [Instalar .NET y verificar el entorno](/modulos/fundamentos/instalar-dotnet).

## Concepto

### Variables: darle nombre a un dato

Una variable es un espacio con nombre donde guardás un valor que puede cambiar durante la ejecución. En C# siempre se declara con un tipo:

```csharp
int edad = 30;
string nombre = "Ana";
```

La primera línea dice: creá un espacio llamado `edad`, que solo acepta números enteros, y guardá `30`. La segunda hace lo mismo con un texto.

El tipo forma parte de la declaración. No podés guardar un texto en una variable `int`: el compilador lo rechaza antes de ejecutar el programa. Esa es una de las ventajas de C#: muchos errores aparecen temprano.

### Los tipos que más vas a usar

| Tipo | Para qué sirve | Ejemplo |
|---|---|---|
| `int` | Números enteros | `int edad = 30;` |
| `long` | Enteros muy grandes | `long poblacion = 8_000_000_000;` |
| `double` | Decimales de uso general | `double temperatura = 21.5;` |
| `decimal` | Dinero y cálculos exactos | `decimal precio = 19.99m;` |
| `bool` | Verdadero o falso | `bool activo = true;` |
| `char` | Un solo carácter | `char inicial = 'A';` |
| `string` | Texto | `string nombre = "Ana";` |
| `DateTime` | Fecha y hora | `DateTime hoy = DateTime.Today;` |

Los literales `decimal` llevan el sufijo `m`. Sin ese sufijo, `19.99` se interpreta como `double` y el compilador no lo asigna a un `decimal` sin una conversión explícita.

### `var` no significa “sin tipo”

`var` le pide al compilador que infiera el tipo a partir del valor inicial:

```csharp
var edad = 30;        // int
var precio = 19.99m;  // decimal
var nombre = "Ana";   // string
```

El tipo se decide al compilar y no cambia. Esto no compila:

```csharp
var edad = 30;
edad = "treinta"; // Error: no se puede convertir string a int
```

Usá `var` cuando el tipo se entiende mirando la línea. Si no se entiende, escribilo explícito: el código se lee más rápido.

### Tipos por valor y tipos por referencia

Los tipos por valor guardan el dato directamente. `int`, `double`, `decimal`, `bool`, `char` y `DateTime` son tipos por valor. Al asignar una variable a otra, se copia el valor:

```csharp
int original = 10;
int copia = original;
copia = 20;

Console.WriteLine(original); // 10
```

Los tipos por referencia guardan una referencia al objeto. `string`, los arrays, las listas y las clases entran en esta categoría. Al asignar, se copia la referencia, no el contenido:

```csharp
int[] primero = { 1, 2 };
int[] segundo = primero;
segundo[0] = 99;

Console.WriteLine(primero[0]); // 99
```

`string` es un caso especial: es un tipo por referencia, pero es inmutable. Sus métodos devuelven un texto nuevo y no modifican el original. Por eso se comporta como si fuera un valor.

### Conversiones

Hay conversiones que el compilador hace solo porque no pierden información:

```csharp
int entero = 10;
long grande = entero;      // implícita
double decimales = entero; // implícita
```

Otras requieren que escribas el tipo de destino. Eso se llama conversión explícita o *cast*:

```csharp
double promedio = 8.7;
int entero = (int)promedio; // 8, se trunca la parte decimal
```

El cast no redondea: corta los decimales. Si necesitás redondear, usá `Math.Round`.

Para convertir texto a número hay tres caminos:

- `int.Parse("42")` funciona, pero lanza `FormatException` si el texto no es un número.
- `Convert.ToInt32("42")` también lanza una excepción si el texto es inválido.
- `int.TryParse("42", out int numero)` devuelve `true` o `false` y nunca lanza una excepción por un formato incorrecto.

Cuando el dato viene del usuario, `TryParse` casi siempre es la mejor opción:

```csharp
string texto = "42";

if (int.TryParse(texto, out int numero))
{
    Console.WriteLine(numero + 1); // 43
}
else
{
    Console.WriteLine("No es un número válido.");
}
```

La variable `numero` se declara en la misma línea con `out`. Si la conversión falla, queda en `0`.

### Interpolación de strings

La forma más cómoda de armar texto con variables es la interpolación:

```csharp
string nombre = "Ana";
int edad = 30;
Console.WriteLine($"{nombre} tiene {edad} años.");
```

Dentro de las llaves podés aplicar formato: `{precio:F2}` muestra dos decimales.

## Ejemplo mínimo

```csharp
int edad = 30;
decimal precio = 19.99m;
bool activo = true;
string nombre = "Ana";
var cantidad = 3;

Console.WriteLine($"{nombre} tiene {edad} años.");
Console.WriteLine($"Compró {cantidad} productos por {precio * cantidad:F2}.");
Console.WriteLine($"Cuenta activa: {activo}");
```

Salida esperada:

```text
Ana tiene 30 años.
Compró 3 productos por 59.97.
Cuenta activa: True
```

## Ejemplo aplicado

Un caso típico: calcular el total de una compra con IVA a partir de datos ingresados por el usuario.

```csharp
Console.Write("Precio unitario: ");
string? entrada = Console.ReadLine();

if (!decimal.TryParse(entrada, out decimal precioUnitario))
{
    Console.WriteLine("El precio no es válido.");
    return;
}

const decimal Iva = 0.21m;
int cantidad = 3;

decimal subtotal = precioUnitario * cantidad;
decimal total = subtotal * (1 + Iva);

Console.WriteLine($"Subtotal: {subtotal:F2}");
Console.WriteLine($"Total con IVA: {total:F2}");
```

Si el usuario escribe `19.99`, la salida es:

```text
Subtotal: 59.97
Total con IVA: 72.56
```

Fijate en tres decisiones: `decimal` para el dinero, `const` para el IVA que no cambia y `TryParse` para no romper el programa con un dato inválido.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `FormatException` al convertir | Se usó `Parse` con un texto inválido | Usar `TryParse` y validar el resultado |
| El total tiene centavos inesperados | Se usó `double` para dinero | Usar `decimal` |
| `Use of unassigned local variable` | La variable se declaró pero no se inicializó | Asignarle un valor antes de usarla |
| `(int)19.99` devuelve `19` | El cast trunca, no redondea | Usar `Math.Round` si hace falta redondear |
| Un número grande se vuelve negativo | Desbordamiento de `int` | Usar `long` o `checked` para detectarlo |

## Buenas prácticas

- Usá nombres que expliquen el contenido: `precioUnitario` es mejor que `p`.
- Para dinero, usá `decimal`; para medidas y cálculos científicos, `double` suele alcanzar.
- Declará constantes con `const` cuando el valor no cambia.
- Usá `var` solo cuando el tipo sea evidente por el lado derecho.
- Validá con `TryParse` todo dato que venga de afuera del programa.
- No reutilices una variable para guardar cosas de distinto significado.

## Ejercicio

Pedí al usuario el precio unitario de un producto y la cantidad. Calculá el subtotal, el IVA del 21% y el total. Validá ambos datos con `TryParse` y mostrá los importes con dos decimales.

<details>
<summary>Ver solución</summary>

```csharp
Console.Write("Precio unitario: ");
string? entradaPrecio = Console.ReadLine();

Console.Write("Cantidad: ");
string? entradaCantidad = Console.ReadLine();

if (!decimal.TryParse(entradaPrecio, out decimal precio) || precio < 0)
{
    Console.WriteLine("El precio no es válido.");
    return;
}

if (!int.TryParse(entradaCantidad, out int cantidad) || cantidad <= 0)
{
    Console.WriteLine("La cantidad no es válida.");
    return;
}

const decimal Iva = 0.21m;
decimal subtotal = precio * cantidad;
decimal impuesto = subtotal * Iva;
decimal total = subtotal + impuesto;

Console.WriteLine($"Subtotal: {subtotal:F2}");
Console.WriteLine($"IVA (21%): {impuesto:F2}");
Console.WriteLine($"Total: {total:F2}");
```

</details>

## Resumen

- Una variable tiene un tipo fijo que se define al compilar.
- `var` infiere el tipo; no es un tipo dinámico.
- Los tipos por valor copian el dato; los tipos por referencia copian la referencia.
- `decimal` es la opción para dinero y `TryParse` para convertir datos externos sin excepciones.
- La interpolación `$"..."` arma texto legible y permite aplicar formato.

## Siguiente paso

Ahora que podés guardar datos, en [Operadores y expresiones](/modulos/csharp-desde-cero/operadores) vas a combinarlos para calcular resultados y tomar decisiones.
