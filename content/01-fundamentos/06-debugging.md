---
slug: debugging
title: "Debugging y errores comunes"
order: 6
estimatedMinutes: 15
summary: "Leer errores del compilador, interpretar stack traces y usar el depurador para encontrar la causa de un bug."
quiz:
  - question: "¿Qué significa el error CS0103?"
    options:
      - "Falta un punto y coma"
      - "El nombre no existe en el contexto actual"
      - "La conversión de tipos no es válida"
      - "No se encontró el archivo del proyecto"
    answer: 1
    explanation: "CS0103 indica que el compilador no reconoce un nombre: puede ser un typo, un using faltante o una variable fuera de alcance."
  - question: "¿Qué parte del stack trace te dice dónde falló tu código?"
    options:
      - "La primera línea del mensaje"
      - "El frame que muestra tu archivo y el número de línea"
      - "La última línea del archivo"
      - "El número de versión del runtime"
    answer: 1
    explanation: "El stack trace lista la cadena de llamadas; el frame con tu archivo y la línea señala el punto exacto de la excepción."
  - question: "¿Qué hace un breakpoint?"
    options:
      - "Detiene la ejecución en esa línea para inspeccionar el estado"
      - "Borra la línea antes de compilar"
      - "Compila el proyecto en Release"
      - "Ignora la excepción y continúa"
    answer: 0
    explanation: "Un breakpoint pausa el programa en la línea marcada y te permite ver variables, avanzar paso a paso y revisar la pila de llamadas."
---

# Debugging y errores comunes

## Objetivo

Leer e interpretar errores de compilación y ejecución, y usar el depurador para encontrar la causa de un bug en lugar de adivinar.

## Requisitos previos

Haber creado, compilado y ejecutado proyectos en las lecciones anteriores. Para seguir el ejemplo con breakpoints necesitás VS Code con la extensión de C# o Visual Studio.

## Concepto

Hay dos momentos en los que algo falla: al compilar y al ejecutar. Los errores de compilación traen archivo, línea y un código como `CS0103`. Las excepciones traen un mensaje y un **stack trace** con la cadena de llamadas. El mensaje dice qué pasó; el stack trace, dónde; la excepción interna, por qué.

Las advertencias no impiden compilar, pero suelen anticipar bugs. `CS8602` (posible referencia nula) es la más común cuando trabajás con nullable.

Un **breakpoint** detiene la ejecución en una línea. Ahí podés ver el valor de cada variable, avanzar paso a paso (`Step Over`, `Step Into`, `Step Out`) y revisar la pila de llamadas. Es más preciso que llenar el código de `Console.WriteLine`.

`Debug` y `Release` no son lo mismo: `Debug` conserva símbolos y no optimiza; `Release` optimiza, puede alinear métodos y quita las llamadas marcadas con `[Conditional("DEBUG")]`, como `Debug.Assert`. Depurá en `Debug` y publicá en `Release`.

`dotnet run` compila en `Debug` por defecto. `dotnet watch run` reinicia la app cuando cambiás un archivo y aplica hot reload cuando puede.

## Ejemplo mínimo

```csharp
var numeros = new[] { 10, 20, 30 };
var indice = 3;

Console.WriteLine(numeros[indice]);
```

Salida:

```text
Unhandled exception. System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at Program.<Main>$(String[] args) in C:\ruta\Program.cs:line 4
```

El mensaje dice que el índice está fuera del arreglo. El stack trace señala `Program.cs`, línea 4. El arreglo tiene tres elementos, con índices `0`, `1` y `2`; pedir el `3` falla. Cambiá `indice` a `2` o validá antes de acceder.

Un error de compilación se ve así:

```csharp
Console.WriteLine(mensaje);
```

```text
error CS0103: El nombre 'mensaje' no existe en el contexto actual.
```

La solución es declararlo antes: `var mensaje = "Hola";`.

## Ejemplo aplicado

Programa que pide una edad y calcula la edad en 10 años:

```csharp
Console.Write("Edad: ");
var entrada = Console.ReadLine();

if (!int.TryParse(entrada, out var edad))
{
    Console.WriteLine("Ingresá un número entero.");
    return;
}

Console.WriteLine($"En 10 años vas a tener {edad + 10}.");
```

`int.TryParse` devuelve `false` si el texto no es un número y no lanza excepción. Si usaras `int.Parse`, una entrada vacía produciría `FormatException`.

Para depurar con VS Code, creá `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET: lanzar consola",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/bin/Debug/net10.0/DebugDemo.dll",
      "cwd": "${workspaceFolder}",
      "console": "integratedTerminal"
    }
  ]
}
```

Poné un breakpoint en la línea del `if`, presioná F5 e ingresá un valor. Cuando se detenga, mirá `entrada` y `edad` en la sección de variables. Si `entrada` es `"abc"`, `TryParse` devuelve `false` y el programa entra en la rama de error.

Flujo recomendado:

1. `dotnet build` y corregir el primer error.
2. Reproducir el fallo con la entrada más chica posible.
3. Leer el mensaje y el stack trace.
4. Poner un breakpoint antes del fallo e inspeccionar.
5. Corregir, volver a ejecutar y agregar una prueba si el bug puede volver.

## Errores comunes

| Error | Causa probable | Solución |
|---|---|---|
| `CS0103` | Nombre inexistente | Revisar ortografía, `using` y alcance |
| `CS1002` | Falta un `;` | Agregar el punto y coma o cerrar paréntesis |
| `CS0029` | Conversión implícita inválida | Convertir explícitamente o cambiar el tipo |
| `CS0246` | Tipo o namespace no encontrado | Agregar `using`, referencia o paquete |
| `CS7036` | Falta un argumento | Revisar la firma del método |
| `CS8602` | Posible referencia nula | Validar con `if`, `?.` o `??` |
| `CS0019` | Operador incompatible | Revisar los tipos de ambos lados |
| `NullReferenceException` | Se usó un objeto nulo | Inicializar o validar antes de acceder |
| `FormatException` | Texto que no es número | Usar `TryParse` y validar |
| `IndexOutOfRangeException` | Índice fuera del rango | Usar `Length - 1` o validar |
| `StackOverflowException` | Recursión sin caso base | Agregar una condición de corte |

## Buenas prácticas

- Corregí el primer error y recompilá; los siguientes suelen ser consecuencia.
- Leé el código de error y la línea, no solo el texto.
- Reproducí con la entrada mínima y simplificá hasta aislar el problema.
- Usá breakpoints y la ventana de variables antes que `Console.WriteLine`.
- No dejes prints de debug en el commit.
- Activá nullable y tratá las advertencias como errores en CI (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).
- Escribí una prueba que falle con el bug y pase con la corrección.
- Usá `Debug.Assert` para invariantes que deberían cumplirse siempre.

## Ejercicio

El siguiente programa tiene dos errores de compilación y, una vez corregidos, falla en ejecución. Encontrá los tres problemas, explicá cada uno y dejalo funcionando.

```csharp
var precios = new[] { 1200, 3500, 800 };
var total = 0;

for (var i = 0; i <= precios.Length; i++)
{
    total += precios[i];
}

var descuento = "10";
total -= descuento;

Console.WriteLine($"Total: {totál}");
```

<details>
<summary>Ver solución</summary>

1. `totál` no existe: el compilador devuelve `CS0103`. La variable correcta es `total`.
2. `descuento` es `string` y `total` es `int`: `-=` no compila (`CS0019`). Hay que declararlo como `int`.
3. El `for` usa `<=`; cuando `i` llega a `precios.Length`, accede fuera del arreglo y lanza `IndexOutOfRangeException`. Debe ser `<`.

```csharp
var precios = new[] { 1200, 3500, 800 };
var total = 0;

for (var i = 0; i < precios.Length; i++)
{
    total += precios[i];
}

var descuento = 10;
total -= descuento;

Console.WriteLine($"Total: {total}");
```

Salida:

```text
Total: 5490
```

</details>

## Resumen

- Los errores de compilación traen archivo, línea y un código `CS`.
- Las excepciones traen mensaje y stack trace; buscá el frame de tu código.
- Un breakpoint es más útil que un print para inspeccionar el estado.
- `TryParse` evita `FormatException` y nullable ayuda con `NullReferenceException`.
- Depurá en `Debug`; publicá en `Release`.

## Siguiente paso

En [Git y GitHub para el día a día](/modulos/fundamentos/git-github) vas a guardar el proyecto y su historia para no perder ninguna corrección.
