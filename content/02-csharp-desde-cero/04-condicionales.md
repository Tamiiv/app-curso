---
slug: condicionales
title: "Condicionales"
order: 4
estimatedMinutes: 12
summary: "Tomar decisiones con if, else, switch y patrones para que el programa responda a cada caso."
quiz:
  - question: "¿Qué operador se usa para comparar dos valores por igualdad?"
    options:
      - "="
      - "=="
      - "=>"
      - "!="
    answer: 1
    explanation: "== compara valores. El operador = asigna un valor a una variable."
  - question: "En una switch expression, ¿qué representa el patrón _?"
    options:
      - "El primer caso"
      - "Un valor nulo obligatorio"
      - "El caso por defecto para cualquier valor no contemplado"
      - "Una variable sin usar"
    answer: 2
    explanation: "El guion bajo es un patrón de descarte que actúa como caso por defecto."
  - question: "Si edad vale 15, ¿qué categoría devuelve un switch con los casos < 13, < 18, < 65 y _?"
    options:
      - "Niño"
      - "Adolescente"
      - "Adulto"
      - "Adulto mayor"
    answer: 1
    explanation: "15 no es menor que 13, pero sí es menor que 18, así que coincide con el segundo caso."
---

# Condicionales

## Objetivo

Al terminar esta lección vas a poder escribir programas que eligen entre distintos caminos según los datos. Vas a usar `if`, `else if`, `else`, `switch` y expresiones `switch` con patrones.

## Requisitos previos

Haber leído [Strings y texto](/modulos/csharp-desde-cero/strings) y [Operadores y expresiones](/modulos/csharp-desde-cero/operadores). Necesitás manejar comparaciones, operadores lógicos y conversión de texto a número.

## Concepto

### `if`: el camino principal

Un `if` ejecuta un bloque solo si una condición es verdadera:

```csharp
int edad = 20;

if (edad >= 18)
{
    Console.WriteLine("Podés entrar.");
}
```

La condición siempre tiene que producir un `bool`. No sirve un número ni un string: C# no convierte valores a verdadero o falso de forma implícita.

### `else` y `else if`: los caminos alternativos

`else` cubre el caso contrario. `else if` permite encadenar varias condiciones:

```csharp
int nota = 8;

if (nota >= 9)
{
    Console.WriteLine("Excelente");
}
else if (nota >= 7)
{
    Console.WriteLine("Aprobado");
}
else if (nota >= 4)
{
    Console.WriteLine("A recuperatorio");
}
else
{
    Console.WriteLine("Desaprobado");
}
```

El orden importa. Las condiciones se evalúan de arriba hacia abajo y se ejecuta el primer bloque que coincide. Si hubieras puesto `nota >= 4` primero, cualquier nota mayor o igual a 4 habría caído ahí y nunca llegaría a “Aprobado”.

### Llaves y alcance

Las llaves delimitan el bloque. Una variable declarada dentro de un bloque solo existe ahí:

```csharp
if (edad >= 18)
{
    string mensaje = "Mayor de edad";
    Console.WriteLine(mensaje);
}

// Console.WriteLine(mensaje); // Error: mensaje no existe acá
```

Aunque un `if` tenga una sola instrucción, acostumbrate a usar llaves. Evitan errores cuando después agregás otra línea y el bloque cambia de significado.

### Condiciones compuestas

Podés combinar condiciones con `&&` y `||`:

```csharp
int edad = 25;
bool tieneEntrada = true;

if (edad >= 18 && tieneEntrada)
{
    Console.WriteLine("Acceso permitido.");
}
```

Si una condición se vuelve difícil de leer, separala en una variable con nombre:

```csharp
bool esMayor = edad >= 18;
bool puedeEntrar = esMayor && tieneEntrada;

if (puedeEntrar)
{
    Console.WriteLine("Acceso permitido.");
}
```

### `switch`: varios casos sobre un mismo valor

Cuando comparás una misma variable contra varios valores, `switch` suele ser más claro que una cadena de `if`:

```csharp
int dia = 3;

switch (dia)
{
    case 1:
        Console.WriteLine("Lunes");
        break;
    case 2:
        Console.WriteLine("Martes");
        break;
    case 3:
        Console.WriteLine("Miércoles");
        break;
    default:
        Console.WriteLine("Día inválido");
        break;
}
```

Cada sección termina con `break` para salir del `switch`. `default` cubre los valores que no coinciden con ningún caso.

### Expresiones `switch` y patrones

Una expresión `switch` devuelve un valor y permite escribir la decisión de forma más compacta. Además, acepta patrones relacionales:

```csharp
int edad = 15;

string categoria = edad switch
{
    < 13 => "Niño",
    < 18 => "Adolescente",
    < 65 => "Adulto",
    _ => "Adulto mayor"
};

Console.WriteLine(categoria); // Adolescente
```

Se evalúa de arriba hacia abajo, igual que un `else if`. El patrón `_` es el caso por defecto. También podés usar patrones con tipos y propiedades, pero eso lo vas a ver más adelante.

### `if` o `switch`: cómo elegir

- Usá `if` cuando la decisión depende de condiciones distintas, rangos o combinaciones.
- Usá `switch` cuando comparás un mismo valor contra varias opciones conocidas.
- Usá una expresión `switch` cuando querés asignar el resultado de la decisión a una variable.

## Ejemplo mínimo

```csharp
int nota = 8;

if (nota >= 9)
{
    Console.WriteLine("Excelente");
}
else if (nota >= 7)
{
    Console.WriteLine("Aprobado");
}
else
{
    Console.WriteLine("A recuperatorio");
}
```

Salida esperada:

```text
Aprobado
```

## Ejemplo aplicado

Un cine cobra distintas tarifas según la edad. El programa valida el dato y usa una expresión `switch` para calcular el precio.

```csharp
Console.Write("Edad: ");
string? entrada = Console.ReadLine();

if (!int.TryParse(entrada, out int edad) || edad < 0 || edad > 120)
{
    Console.WriteLine("La edad no es válida.");
    return;
}

decimal precioBase = 5000m;

decimal precio = edad switch
{
    < 3 => 0m,
    < 13 => precioBase * 0.5m,
    < 65 => precioBase,
    _ => precioBase * 0.7m
};

Console.WriteLine($"Precio de la entrada: {precio:F2}");
```

La primera condición usa `||`, así que si el texto no es un número, el resto de la validación no se evalúa. El `switch` cubre todos los rangos posibles y no deja casos sin resolver.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El programa no compila en `if (edad = 18)` | Se usó asignación en lugar de comparación | Usar `==` |
| Se ejecuta el caso equivocado | Las condiciones están en el orden incorrecto | Ordenar de más específica a más general |
| `else` se asocia al `if` equivocado | Faltan llaves | Usar llaves siempre |
| Un caso del `switch` no termina | Falta `break` | Agregar `break` o `return` |
| Un valor no entra en ningún caso | No hay `default` ni patrón `_` | Agregar el caso por defecto |

## Buenas prácticas

- Usá llaves aunque el bloque tenga una sola línea.
- Escribí las condiciones en positivo cuando sea posible: `if (activo)` se lee mejor que `if (!inactivo)`.
- No anides más de dos o tres `if`; si hace falta más, separá en métodos.
- Usá nombres claros para las condiciones: `puedeEntrar`, `esMayorDeEdad`.
- En un `switch`, cubrí siempre el caso por defecto.
- Evitá repetir la misma condición en varios lugares: calculala una vez.

## Ejercicio

Un museo cobra 0 a los menores de 3 años, la mitad a los menores de 13, el precio completo a los menores de 65 y un 30% menos a los mayores. Pedí la edad, validala y mostrá el precio final con dos decimales. El precio base es de 5000.

<details>
<summary>Ver solución</summary>

```csharp
Console.Write("Edad: ");
string? entrada = Console.ReadLine();

if (!int.TryParse(entrada, out int edad) || edad < 0 || edad > 120)
{
    Console.WriteLine("La edad no es válida.");
    return;
}

decimal precioBase = 5000m;

decimal precio = edad switch
{
    < 3 => 0m,
    < 13 => precioBase * 0.5m,
    < 65 => precioBase,
    _ => precioBase * 0.7m
};

Console.WriteLine($"Precio final: {precio:F2}");
```

</details>

## Resumen

- `if` ejecuta un bloque cuando una condición booleana es verdadera.
- `else if` y `else` permiten cubrir los caminos alternativos.
- El orden de las condiciones cambia el resultado.
- `switch` es útil para comparar un valor contra varias opciones.
- Las expresiones `switch` con patrones relacionales devuelven un valor y el patrón `_` cubre el resto.

## Siguiente paso

En [Bucles](/modulos/csharp-desde-cero/bucles) vas a repetir instrucciones para procesar muchos datos sin escribir el mismo código una y otra vez.
