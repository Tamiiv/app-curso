---
slug: como-practicar
title: "Cómo practicar: entorno local y ejemplos"
order: 3
estimatedMinutes: 8
summary: "Prepará una carpeta de práctica, ejecutá los ejemplos y adoptá un ciclo de estudio activo que deje resultados."
quiz:
  - question: "¿Dónde se ejecutan los ejemplos de la plataforma?"
    options:
      - "En el navegador, dentro de la página"
      - "En tu computadora, con el SDK de .NET"
      - "En la base de datos de la plataforma"
      - "En un servidor externo que ejecuta tu código"
    answer: 1
    explanation: "La V1 no incluye ejecución de código en la nube; los ejemplos se corren en tu entorno local."
  - question: "¿Cuál es el mejor primer paso al estudiar una lección con código?"
    options:
      - "Copiar el ejemplo y pasar a la siguiente"
      - "Leer todo sin abrir la terminal"
      - "Escribir y ejecutar el ejemplo mínimo"
      - "Buscar una solución en otro sitio"
    answer: 2
    explanation: "Escribir y ejecutar desde el ejemplo mínimo confirma que el entorno y el concepto funcionan."
  - question: "¿Qué conviene hacer después de lograr que un ejemplo funcione?"
    options:
      - "Borrarlo y no volver a mirarlo"
      - "Cambiar valores, nombres o tipos para ver qué pasa"
      - "Agregar todas las funciones posibles"
      - "Memorizar el código línea por línea"
    answer: 1
    explanation: "Modificar el ejemplo a propósito ayuda a entender qué hace cada parte y a detectar suposiciones."
---

# Cómo practicar: entorno local y ejemplos

## Objetivo

Al terminar esta lección vas a tener una carpeta de práctica, vas a saber ejecutar un ejemplo mínimo y vas a contar con un ciclo de estudio que convierte la lectura en código funcionando.

## Requisitos previos

Haber leído [Progreso, marcadores y ruta sugerida](/modulos/como-usar/progreso-marcadores). Todavía no necesitás el SDK instalado: en el módulo 1 vas a preparar el entorno paso a paso. Si ya lo tenés, podés practicar desde ahora.

## Concepto

Leer sobre programación es necesario, pero no alcanza. La plataforma está pensada para que cada ejemplo lo ejecutes en tu computadora.

En la V1 **no hay ejecución de código en el navegador**. Eso es una decisión deliberada: aprender a usar una terminal, un editor y el SDK forma parte del trabajo real.

### El ciclo de práctica

Usá este ciclo en cada lección con código:

1. **Leer:** entendé la idea antes de mirar el ejemplo completo.
2. **Escribir:** tipeá el ejemplo mínimo en tu editor. No lo pegues.
3. **Ejecutar:** corré el programa y compará la salida.
4. **Romper:** cambiá un valor, un nombre o un tipo y observá el resultado.
5. **Explicar:** escribí en tus palabras qué hace el código y por qué.

Si un paso falla, no avances: el error es parte del aprendizaje. Leé el mensaje, buscá la línea y corregí.

### Una carpeta para todo

Creá una carpeta de trabajo, por ejemplo `PracticaDotNet`, y un proyecto por lección o por experimento. No mezcles ejercicios en el mismo proyecto: cuando algo se rompe, es más fácil aislar el problema.

```text
PracticaDotNet/
├── 00-como-usar/
│   └── PrimerPrograma/
└── 01-fundamentos/
    └── HolaMundo/
```

## Ejemplo mínimo

Creá un proyecto de consola y ejecutalo:

```bash
dotnet new console -n PrimerPrograma
cd PrimerPrograma
dotnet run
```

La salida esperada es:

```text
Hello, World!
```

Ahora reemplazá el contenido de `Program.cs` por este:

```csharp
Console.WriteLine("Estoy practicando .NET");
Console.WriteLine($"Hoy es {DateTime.Today:dd/MM/yyyy}");
```

Volvé a ejecutar con `dotnet run`. La salida debería mostrar dos líneas con el mensaje y la fecha actual.

## Ejemplo aplicado

Un ejercicio de práctica no tiene que ser grande. Este programa calcula el promedio de tres notas:

```csharp
var notas = new[] { 8, 7, 9 };
var promedio = notas.Average();

Console.WriteLine($"Promedio: {promedio:F1}");

if (promedio >= 6)
{
    Console.WriteLine("Aprobado");
}
else
{
    Console.WriteLine("A repasar");
}
```

Salida esperada:

```text
Promedio: 8.0
Aprobado
```

Después de hacerlo funcionar, probá cambiar las notas, agregar una cuarta y bajar el promedio para ver el otro camino del `if`.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `dotnet: command not found` | SDK no instalado o terminal vieja | Instalá el SDK y abrí una terminal nueva |
| El programa no refleja los cambios | Editás un archivo fuera del proyecto | Verificá la carpeta y volvé a ejecutar `dotnet run` |
| Copiar y pegar sin entender | Avanzar rápido | Tipeá el ejemplo y explicá cada línea |
| Acumular ejercicios en un solo proyecto | Falta de orden | Un proyecto por lección o experimento |
| Abandonar al primer error | Miedo a romper algo | Leé el mensaje completo y corregí una cosa por vez |

## Buenas prácticas

- Mantené una libreta o un archivo de apuntes con errores y soluciones.
- Hacé commits chicos si ya usás Git; te permiten volver atrás.
- Ejecutá el ejemplo mínimo antes de intentar el ejercicio.
- No mires la solución hasta haber intentado el ejercicio al menos una vez.
- Repetí un ejercicio días después sin mirar el código anterior.

## Ejercicio

Creá un proyecto llamado `Practica00` que imprima tres líneas: tu nombre, el módulo que estás cursando y una meta para la semana. Ejecutalo y verificá la salida.

<details>
<summary>Ver solución</summary>

```bash
dotnet new console -n Practica00
cd Practica00
```

Contenido de `Program.cs`:

```csharp
Console.WriteLine("Nombre: Ana");
Console.WriteLine("Módulo: Cómo usar la plataforma");
Console.WriteLine("Meta: completar el módulo 1 esta semana");
```

Ejecución:

```bash
dotnet run
```

Salida esperada:

```text
Nombre: Ana
Módulo: Cómo usar la plataforma
Meta: completar el módulo 1 esta semana
```

</details>

## Resumen

- La práctica ocurre en tu computadora, no dentro del navegador.
- El ciclo leer, escribir, ejecutar, romper y explicar convierte la lectura en aprendizaje.
- Una carpeta ordenada y un proyecto por experimento evitan confusiones.
- Los errores son parte del proceso: se leen y se corrigen de a uno.

## Siguiente paso

Con el hábito de práctica armado, empezá la ruta real en [¿Qué es .NET?](/modulos/fundamentos/que-es-dotnet).
