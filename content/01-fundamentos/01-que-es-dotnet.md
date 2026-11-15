---
slug: que-es-dotnet
title: "¿Qué es .NET?"
order: 1
estimatedMinutes: 10
summary: "Runtime, SDK y ecosistema: las piezas básicas antes de escribir código."
quiz:
  - question: "¿Qué es .NET?"
    options:
      - "Un lenguaje de programación"
      - "Una plataforma con runtime, herramientas y bibliotecas"
      - "Un editor de código"
      - "Una base de datos"
    answer: 1
    explanation: ".NET es la plataforma; C# es uno de los lenguajes que se ejecutan sobre ella."
  - question: "¿Qué incluye el SDK de .NET?"
    options:
      - "Solo el runtime"
      - "Solo el compilador"
      - "El runtime, la CLI, las plantillas y las herramientas de compilación"
      - "Solo las bibliotecas de terceros"
    answer: 2
    explanation: "El SDK incluye todo lo necesario para desarrollar, además del runtime."
  - question: "¿Qué diferencia hay entre .NET Framework y .NET moderno?"
    options:
      - "Son exactamente iguales"
      - "Framework es multiplataforma y el moderno solo funciona en Windows"
      - "Framework es la versión antigua para Windows; el moderno es multiplataforma y open source"
      - "El moderno ya no se actualiza"
    answer: 2
    explanation: ".NET moderno (antes .NET Core) es multiplataforma, open source y la versión que se usa hoy."
---

# ¿Qué es .NET?

## Objetivo

Entender qué es .NET, qué diferencia hay entre el runtime y el SDK, y qué tipo de aplicaciones se pueden construir con la plataforma.

## La idea en una frase

.NET es una plataforma de desarrollo creada por Microsoft que incluye un runtime, herramientas y bibliotecas para construir aplicaciones con C# y otros lenguajes.

No es un lenguaje: es el entorno donde el lenguaje vive. C# es el lenguaje; .NET es la plataforma que lo ejecuta.

## Las dos piezas que más vas a escuchar

| Pieza | Qué es | Para qué sirve |
|---|---|---|
| **Runtime** | El motor que ejecuta la aplicación | Corre tu programa en la computadora o en el servidor |
| **SDK** | El kit de desarrollo | Compila, ejecuta, prueba y publica tu código |

Cuando instalás el **SDK**, también instalás el runtime. Por eso, para desarrollar, alcanza con el SDK.

## Lenguajes

.NET es multi-lenguaje. Los más usados son:

- **C#**: el lenguaje principal y el que vamos a usar.
- **F#**: funcional, muy expresivo.
- **Visual Basic**: histórico, todavía presente en algunos proyectos.

En este curso nos enfocamos en C# porque es el que se pide en la mayoría de los trabajos con .NET.

## .NET Framework vs. .NET moderno

Vas a encontrar documentación y proyectos con nombres parecidos:

- **.NET Framework**: la versión antigua, solo para Windows. Todavía existe en sistemas legacy.
- **.NET moderno** (antes .NET Core): multiplataforma, open source y el estándar actual. Hoy vamos por **.NET 10 LTS**.

Si un tutorial usa `System.Web` o `Global.asax`, probablemente sea .NET Framework. Para empezar de cero, usá .NET moderno.

## ¿Qué se puede construir?

- APIs y servicios web con ASP.NET Core.
- Aplicaciones web con Blazor, Razor Pages o MVC.
- Aplicaciones de escritorio con WPF, WinForms o .NET MAUI.
- Apps móviles con .NET MAUI.
- Servicios en la nube, trabajos en segundo plano y funciones.
- Videojuegos con Unity, que usa C#.
- Herramientas de línea de comandos.

## Comprobar la versión instalada

Abrí una terminal y ejecutá:

```bash
dotnet --version
```

Si responde con un número como `10.0.404`, tenés el SDK instalado. Si el comando no existe, en la próxima lección vas a ver cómo instalarlo.

## Resumen

- .NET es una plataforma, no un lenguaje.
- El runtime ejecuta; el SDK desarrolla.
- C# es el lenguaje principal del ecosistema.
- .NET moderno es multiplataforma y open source.
- La versión actual de soporte extendido es .NET 10 LTS.

## Siguiente paso

Instalar .NET y verificar que el entorno esté listo.
