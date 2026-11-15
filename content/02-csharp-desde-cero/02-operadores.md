---
slug: operadores
title: "Operadores y expresiones"
order: 2
estimatedMinutes: 12
summary: "Aritmética, comparaciones y lógica para construir expresiones que resuelvan problemas."
quiz:
  - question: "¿Cuál es el resultado de 7 / 2 si ambos operandos son int?"
    options:
      - "3.5"
      - "3"
      - "4"
      - "Error de compilación"
    answer: 1
    explanation: "La división entre enteros descarta la parte decimal, por eso el resultado es 3."
  - question: "¿Qué operador lógico evalúa el segundo operando solo si hace falta?"
    options:
      - "&"
      - "|"
      - "&&"
      - "^"
    answer: 2
    explanation: "&& usa cortocircuito: si el primer operando es false, ya sabe que el resultado es false y no evalúa el segundo."
  - question: "¿Qué devuelve la expresión 17 % 5?"
    options:
      - "3"
      - "2"
      - "3.4"
      - "12"
    answer: 1
    explanation: "El operador % devuelve el resto de la división entera: 17 dividido 5 da 3 y sobra 2."
---

# Operadores y expresiones

## Objetivo

Al terminar esta lección vas a poder combinar variables y valores con operadores aritméticos, de comparación y lógicos. También vas a entender la precedencia, el cortocircuito y cuándo conviene usar el operador ternario.

## Requisitos previos

Haber leído [Variables, tipos y conversiones](/modulos/csharp-desde-cero/variables-tipos). Necesitás saber declarar variables y convertir tipos básicos.

## Concepto

### Operadores aritméticos

C# tiene los operadores habituales:

| Operador | Significado | Ejemplo | Resultado |
|---|---|---|---|
| `+` | Suma | `5 + 2` | `7` |
| `-` | Resta | `5 - 2` | `3` |
| `*` | Multiplicación | `5 * 2` | `10` |
| `/` | División | `5 / 2` | `2` |
| `%` | Resto o módulo | `5 % 2` | `1` |

La división merece atención. Si ambos operandos son enteros, el resultado también es entero y se descarta la parte decimal:

```csharp
int a = 7;
int b = 2;

Console.WriteLine(a / b); // 3
```

Para obtener `3.5`, al menos uno de los dos operandos tiene que ser decimal:

```csharp
Console.WriteLine(7.0 / 2);   // 3.5
Console.WriteLine(7 / 2.0);   // 3.5
Console.WriteLine((double)7 / 2); // 3.5
```

El operador `%` devuelve el resto. Sirve para saber si un número es par, para repartir en grupos y para calcular vueltas:

```csharp
Console.WriteLine(10 % 2); // 0, es par
Console.WriteLine(11 % 2); // 1, es impar
```

### Precedencia y paréntesis

La multiplicación y la división se resuelven antes que la suma y la resta. Si querés otra cosa, usá paréntesis:

```csharp
Console.WriteLine(2 + 3 * 4);   // 14
Console.WriteLine((2 + 3) * 4); // 20
```

No memorices toda la tabla de precedencia. Cuando haya duda, agregá paréntesis: el código queda más claro para quien lo lee.

### Comparaciones

Los operadores de comparación producen un `bool`:

```csharp
int edad = 18;

Console.WriteLine(edad == 18); // True
Console.WriteLine(edad != 18); // False
Console.WriteLine(edad > 17);  // True
Console.WriteLine(edad <= 17); // False
```

Cuidado con no confundir `=` con `==`. `=` asigna; `==` compara.

### Operadores lógicos

Para combinar condiciones usás `&&` (y), `||` (o) y `!` (no):

```csharp
int edad = 25;
bool tieneEntrada = true;

bool puedeEntrar = edad >= 18 && tieneEntrada;
Console.WriteLine(puedeEntrar); // True
```

`&&` y `||` usan cortocircuito. En `a && b`, si `a` es `false`, el resultado ya es `false` y `b` no se evalúa. En `a || b`, si `a` es `true`, `b` no se evalúa. Eso permite escribir validaciones seguras:

```csharp
string? nombre = null;

if (nombre is not null && nombre.Length > 0)
{
    Console.WriteLine(nombre);
}
```

Si se usara `&` en lugar de `&&`, se evaluarían ambos lados y el acceso a `nombre.Length` podría fallar cuando `nombre` es `null`.

### Asignación compuesta e incrementos

Hay atajos para actualizar una variable:

```csharp
int total = 10;
total += 5;  // total = total + 5
total -= 3;  // total = total - 3
total *= 2;  // total = total * 2
total++;     // total = total + 1
```

`++` suma uno y `--` resta uno. Se pueden usar como instrucción o dentro de una expresión, pero para empezar conviene usarlos en una línea propia: así no te enredás con la diferencia entre `i++` y `++i`.

### El operador ternario

Cuando una decisión simple tiene dos resultados, el operador ternario resume el `if` en una expresión:

```csharp
int edad = 20;
string mensaje = edad >= 18 ? "Mayor de edad" : "Menor de edad";
Console.WriteLine(mensaje);
```

Se lee así: si la condición es verdadera, devolvé lo que está antes de `:`; si es falsa, devolvé lo que está después. Usalo cuando la condición y los dos resultados entren cómodos en una línea. Si no, un `if` normal se entiende mejor.

### Texto y operadores

El operador `+` también concatena strings:

```csharp
string nombre = "Ana";
Console.WriteLine("Hola, " + nombre);
```

Para mezclar texto y valores, la interpolación suele ser más legible:

```csharp
Console.WriteLine($"Hola, {nombre}. Tenés {edad} años.");
```

En C#, `==` compara el contenido de dos strings, no la referencia:

```csharp
string a = "hola";
string b = "ho";
string c = b + "la";

Console.WriteLine(a == c); // True
```

Para comparar sin distinguir mayúsculas y minúsculas, usá `string.Equals` con `StringComparison`:

```csharp
bool iguales = string.Equals("Hola", "hola", StringComparison.OrdinalIgnoreCase);
```

### Cuidado con los decimales binarios

`double` y `float` no representan todos los decimales con exactitud. Por eso esto puede sorprender:

```csharp
Console.WriteLine(0.1 + 0.2 == 0.3); // False
```

Para dinero usá `decimal`. Para comparar `double`, verificá que la diferencia sea menor que una tolerancia, en lugar de buscar igualdad exacta.

## Ejemplo mínimo

```csharp
int a = 7;
int b = 2;

Console.WriteLine(a + b);       // 9
Console.WriteLine(a - b);       // 5
Console.WriteLine(a * b);       // 14
Console.WriteLine(a / b);       // 3
Console.WriteLine(a % b);       // 1
Console.WriteLine((a + b) * 2); // 18
```

Salida esperada:

```text
9
5
14
3
1
18
```

## Ejemplo aplicado

Un carrito de compras aplica un descuento a socios y cobra envío solo si el subtotal no llega a un mínimo.

```csharp
decimal precio = 1200m;
int cantidad = 3;
bool esSocio = true;

decimal subtotal = precio * cantidad;
decimal descuento = esSocio ? subtotal * 0.10m : 0m;
decimal envio = subtotal >= 3000m ? 0m : 250m;
decimal total = subtotal - descuento + envio;

Console.WriteLine($"Subtotal: {subtotal:F2}");
Console.WriteLine($"Descuento: {descuento:F2}");
Console.WriteLine($"Envío: {envio:F2}");
Console.WriteLine($"Total: {total:F2}");
```

Salida esperada:

```text
Subtotal: 3600.00
Descuento: 360.00
Envío: 0.00
Total: 3240.00
```

El ejemplo combina aritmética, comparación y dos operadores ternarios. Cada decisión es chica y se lee en una línea.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `5 / 2` devuelve `2` | División entre enteros | Convertir uno de los operandos a `double` o `decimal` |
| El programa no compila en `if (x = 5)` | Se usó asignación en lugar de comparación | Usar `==` |
| `NullReferenceException` en una condición | Se usó `&` en lugar de `&&` | Usar `&&` para aprovechar el cortocircuito |
| `0.1 + 0.2` no es igual a `0.3` | Representación binaria de `double` | Usar `decimal` o comparar con tolerancia |
| Un descuento se aplica mal | Faltan paréntesis en una expresión larga | Separar en variables intermedias o agregar paréntesis |

## Buenas prácticas

- Dividí las expresiones largas en variables con nombre: `subtotal`, `descuento`, `total`.
- Usá `decimal` para importes y `double` para medidas.
- Preferí `&&` y `||` sobre `&` y `|` cuando trabajás con condiciones.
- No abuses del operador ternario: si ocupa demasiado, usá `if`.
- Compará strings con `StringComparison` cuando el caso o la cultura importan.
- Evitá comparar `double` con `==`.

## Ejercicio

Escribí un programa que pida un monto entero en pesos y calcule cuántos billetes de 20.000, 10.000, 5.000 y 1.000 se necesitan. Mostrá cuánto sobra si el monto no es múltiplo de 1.000.

<details>
<summary>Ver solución</summary>

```csharp
Console.Write("Monto a retirar: ");
string? entrada = Console.ReadLine();

if (!int.TryParse(entrada, out int monto) || monto <= 0)
{
    Console.WriteLine("El monto no es válido.");
    return;
}

int resto = monto;

int billetes20000 = resto / 20000;
resto %= 20000;

int billetes10000 = resto / 10000;
resto %= 10000;

int billetes5000 = resto / 5000;
resto %= 5000;

int billetes1000 = resto / 1000;
resto %= 1000;

Console.WriteLine($"Billetes de 20.000: {billetes20000}");
Console.WriteLine($"Billetes de 10.000: {billetes10000}");
Console.WriteLine($"Billetes de 5.000: {billetes5000}");
Console.WriteLine($"Billetes de 1.000: {billetes1000}");

if (resto > 0)
{
    Console.WriteLine($"Sobrante: {resto}");
}
```

</details>

## Resumen

- La división entre enteros descarta los decimales; el módulo `%` devuelve el resto.
- `&&` y `||` usan cortocircuito y evitan evaluar condiciones innecesarias.
- El operador ternario sirve para decisiones simples con dos resultados.
- `==` compara valores; en strings compara el contenido.
- `decimal` evita los errores de precisión de `double` en cálculos con dinero.

## Siguiente paso

En [Strings y texto](/modulos/csharp-desde-cero/strings) vas a profundizar en el manejo de texto, desde métodos útiles hasta `StringBuilder`.
