---
slug: metodos
title: "Métodos y parámetros"
order: 7
estimatedMinutes: 15
summary: "Agrupar lógica en métodos con parámetros y valores de retorno para reutilizar código y mantenerlo legible."
quiz:
  - question: "¿Qué indica el tipo void en la declaración de un método?"
    options:
      - "Que el método no recibe parámetros"
      - "Que el método no devuelve un valor"
      - "Que el método es privado"
      - "Que el método siempre falla"
    answer: 1
    explanation: "void significa que el método ejecuta acciones pero no devuelve un resultado."
  - question: "¿Qué es un parámetro opcional?"
    options:
      - "Un parámetro que se puede omitir porque tiene un valor por defecto"
      - "Un parámetro que siempre vale null"
      - "Un parámetro que se pasa por referencia"
      - "Un método sin nombre"
    answer: 0
    explanation: "Un parámetro opcional tiene un valor por defecto y puede omitirse en la llamada."
  - question: "¿Qué palabra clave se usa para devolver un valor desde un método?"
    options:
      - "break"
      - "yield"
      - "return"
      - "out"
    answer: 2
    explanation: "return devuelve el control y, si el método tiene un tipo de retorno, entrega el valor."
---

# Métodos y parámetros

## Objetivo

Al terminar esta lección vas a poder dividir un programa en métodos con nombre, pasarles datos mediante parámetros y devolver resultados. Vas a saber cuándo usar `void`, cómo aprovechar parámetros opcionales y cómo evitar efectos secundarios inesperados.

## Requisitos previos

Haber leído [Arrays y listas](/modulos/csharp-desde-cero/arrays-listas). Necesitás manejar variables, condicionales, bucles y colecciones.

## Concepto

### Por qué usar métodos

Un método agrupa instrucciones que resuelven una tarea y le pone un nombre. Eso permite reutilizar la lógica, probarla por separado y leer el programa principal como una lista de pasos.

En un proyecto de consola moderno podés declarar funciones locales estáticas después de las instrucciones principales:

```csharp
Console.WriteLine(Sumar(2, 3));

static int Sumar(int a, int b)
{
    return a + b;
}
```

La primera línea llama al método. Las siguientes lo declaran. `static` indica que el método no necesita una instancia de una clase para ejecutarse; en este curso, mientras trabajamos con programas de consola, es lo que vas a usar.

### Partes de un método

```csharp
static decimal CalcularTotal(decimal precio, int cantidad)
{
    decimal subtotal = precio * cantidad;
    return subtotal;
}
```

- `static` es el modificador.
- `decimal` es el tipo de retorno.
- `CalcularTotal` es el nombre.
- `precio` y `cantidad` son los parámetros.
- `return` devuelve el resultado.

Los valores que pasás al llamar se llaman argumentos:

```csharp
decimal total = CalcularTotal(1200m, 3);
```

### `void`: métodos que no devuelven nada

Si el método solo ejecuta una acción, su tipo de retorno es `void`:

```csharp
MostrarSaludo("Ana");

static void MostrarSaludo(string nombre)
{
    Console.WriteLine($"Hola, {nombre}!");
}
```

Un método `void` puede usar `return;` para salir antes, pero no devuelve un valor.

### Parámetros opcionales y argumentos nombrados

Podés darle un valor por defecto a un parámetro. Los parámetros opcionales van al final:

```csharp
static decimal CalcularTotal(decimal precio, int cantidad, decimal iva = 0.21m)
{
    decimal subtotal = precio * cantidad;
    return subtotal * (1 + iva);
}

Console.WriteLine(CalcularTotal(1000m, 2));          // usa 0.21
Console.WriteLine(CalcularTotal(1000m, 2, 0.10m));   // usa 0.10
```

Los argumentos nombrados hacen la llamada más clara cuando hay varios parámetros:

```csharp
decimal total = CalcularTotal(precio: 1000m, cantidad: 2, iva: 0.10m);
```

### Métodos que devuelven varios datos con `out`

`TryParse` es el ejemplo más conocido de un método con parámetro `out`:

```csharp
if (int.TryParse("42", out int numero))
{
    Console.WriteLine(numero);
}
```

El método devuelve un `bool` e informa el número convertido a través de `out`. No inventes métodos con muchos `out`: si necesitás devolver varios valores relacionados, más adelante vas a usar una clase o un record.

### Sobrecarga

Dos métodos pueden tener el mismo nombre si cambian sus parámetros. Eso se llama sobrecarga:

```csharp
public static class Calculadora
{
    public static double Area(double lado) => lado * lado;

    public static double Area(double ancho, double alto) => ancho * alto;
}
```

El compilador elige la versión según la cantidad y el tipo de argumentos. La sobrecarga sirve cuando la misma operación tiene distintas formas de entrada, no para cambiar el significado del método.

### Métodos con cuerpo de expresión

Cuando el cuerpo es una sola expresión, podés usar `=>`:

```csharp
static int Doble(int numero) => numero * 2;
static string Saludar(string nombre) => $"Hola, {nombre}";
```

Es solo una forma más corta de escribir lo mismo. Usala cuando la expresión se entiende de un vistazo.

### Paso por valor y por referencia

Los parámetros de tipos por valor reciben una copia:

```csharp
int numero = 10;
Duplicar(numero);
Console.WriteLine(numero); // 10

static void Duplicar(int valor)
{
    valor *= 2;
}
```

Los parámetros de tipos por referencia reciben una copia de la referencia. Si modificás el objeto, el cambio se ve desde afuera:

```csharp
int[] numeros = { 1, 2 };
DuplicarPrimero(numeros);
Console.WriteLine(numeros[0]); // 2

static void DuplicarPrimero(int[] valores)
{
    valores[0] *= 2;
}
```

Pero si reasignás el parámetro a otro objeto, el llamador no ve ese cambio. La diferencia entre modificar el contenido y reasignar la referencia es clave.

## Ejemplo mínimo

```csharp
Console.WriteLine(Sumar(2, 3));

static int Sumar(int a, int b)
{
    return a + b;
}
```

Salida esperada:

```text
5
```

## Ejemplo aplicado

Un método con un parámetro opcional calcula el total de una compra y permite cambiar el IVA cuando hace falta.

```csharp
Console.Write("Precio unitario: ");
string? entrada = Console.ReadLine();

if (!decimal.TryParse(entrada, out decimal precio))
{
    Console.WriteLine("El precio no es válido.");
    return;
}

decimal total = CalcularTotal(precio, 3);
Console.WriteLine($"Total: {total:F2}");

static decimal CalcularTotal(decimal precio, int cantidad, decimal iva = 0.21m)
{
    decimal subtotal = precio * cantidad;
    return subtotal * (1 + iva);
}
```

El programa principal valida la entrada y delega el cálculo. Si mañana cambia la forma de calcular el total, tocás un solo lugar.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `not all code paths return a value` | Falta un `return` en algún camino | Asegurar que todos los caminos devuelvan un valor |
| Se esperaba que el método modificara una variable | El parámetro es un tipo por valor | Devolver el resultado y asignarlo, o usar `ref` si realmente hace falta |
| El método no compila por parámetros opcionales | Un parámetro obligatorio está después de uno opcional | Mover los opcionales al final |
| Se confunde el orden de los argumentos | Varios parámetros del mismo tipo | Usar argumentos nombrados |
| Un método hace demasiadas cosas | Mezcla cálculo, entrada y salida | Dividirlo en métodos más chicos |

## Buenas prácticas

- Poné nombres que describan la acción: `CalcularTotal`, `MostrarResumen`, `EsValido`.
- Hacé que cada método resuelva una sola tarea.
- Preferí devolver un resultado antes que modificar variables globales.
- Usá parámetros opcionales solo cuando el valor por defecto tenga sentido.
- No uses `out` para devolver muchos valores; considerá una clase cuando el grupo de datos crezca.
- Mantené los métodos cortos: si no entran en una pantalla, probablemente hagan demasiado.

## Ejercicio

Creá dos métodos: uno que reciba un array de notas y devuelva el promedio, y otro que reciba el promedio y devuelva una clasificación. Si el array está vacío, el promedio debe ser `0`. Probá con las notas `8, 6, 10, 7` y mostrá ambos resultados.

<details>
<summary>Ver solución</summary>

```csharp
int[] notas = { 8, 6, 10, 7 };

double promedio = CalcularPromedio(notas);
Console.WriteLine($"Promedio: {promedio:F2}");
Console.WriteLine($"Estado: {Clasificar(promedio)}");

static double CalcularPromedio(int[] numeros)
{
    if (numeros.Length == 0)
    {
        return 0;
    }

    int suma = 0;

    foreach (int numero in numeros)
    {
        suma += numero;
    }

    return (double)suma / numeros.Length;
}

static string Clasificar(double promedio) => promedio switch
{
    >= 9 => "Excelente",
    >= 7 => "Muy bien",
    >= 6 => "Aprobado",
    _ => "A recuperatorio"
};
```

</details>

## Resumen

- Un método agrupa lógica y le da un nombre reutilizable.
- El tipo de retorno indica qué devuelve; `void` significa que no devuelve nada.
- Los parámetros opcionales tienen un valor por defecto y van al final.
- Los tipos por valor se pasan por copia; los tipos por referencia comparten el objeto.
- Un método debería tener una responsabilidad clara y un nombre que la describa.

## Siguiente paso

En [Nullable reference types](/modulos/csharp-desde-cero/nullable-reference-types) vas a aprender a expresar con el tipo cuándo un valor puede faltar y a evitar los temidos `NullReferenceException`.
