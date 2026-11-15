---
slug: instalar-dotnet
title: "Instalar .NET y verificar el entorno"
order: 2
estimatedMinutes: 10
summary: "Instalá el SDK, comprobá la versión y creá tu primera aplicación de consola."
quiz:
  - question: "¿Qué comando muestra la versión y el entorno del SDK instalado?"
    options:
      - "dotnet --info"
      - "dotnet new"
      - "dotnet install"
      - "dotnet version"
    answer: 0
    explanation: "dotnet --info muestra la versión del SDK, el sistema operativo y el RID."
  - question: "¿Qué hace dotnet new console -n HolaMundo?"
    options:
      - "Ejecuta un programa existente"
      - "Crea un proyecto de consola llamado HolaMundo"
      - "Publica la aplicación"
      - "Instala el SDK"
    answer: 1
    explanation: "dotnet new console crea un proyecto a partir de la plantilla de consola."
  - question: "¿Qué son las carpetas bin/ y obj/?"
    options:
      - "Carpetas del código fuente"
      - "Carpetas generadas por la compilación que no se versionan"
      - "Paquetes NuGet del proyecto"
      - "Configuraciones del sistema operativo"
    answer: 1
    explanation: "bin/ y obj/ contienen artefactos generados y no deben subirse al repositorio."
---

# Instalar .NET y verificar el entorno

## Objetivo

Dejar el entorno listo para programar: instalar el SDK de .NET 10, verificar que la terminal lo reconoce y crear tu primer proyecto.

## Requisitos

- Una computadora con Windows, macOS o Linux.
- Conexión a internet para la descarga.
- Permisos para instalar software.

## Instalación

### Windows

La forma más simple es con `winget`, el gestor de paquetes de Windows:

```powershell
winget install Microsoft.DotNet.SDK.10
```

También podés descargar el instalador desde [dotnet.microsoft.com](https://dotnet.microsoft.com/download).

### macOS

Con Homebrew:

```bash
brew install --cask dotnet-sdk
```

### Linux

Cada distribución tiene su propio paquete. La guía oficial está en [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download). Para Ubuntu, por ejemplo, se agrega el repositorio de Microsoft y se instala `dotnet-sdk-10.0`.

## Verificar la instalación

Cerrá y volvé a abrir la terminal, y ejecutá:

```bash
dotnet --info
```

Vas a ver algo parecido a esto:

```text
.NET SDK:
 Version:   10.0.404
 Commit:    ...
 OS Name:   Windows
 OS Version: 10.0.26200
 RID:       win-x64
```

Para ver solo los SDK instalados:

```bash
dotnet --list-sdks
```

Si el comando no se reconoce, revisá que la instalación haya terminado y que la terminal sea nueva. En Windows, el instalador suele agregar `dotnet` al `PATH` automáticamente.

## Crear el primer proyecto

Elegí una carpeta de trabajo y ejecutá:

```bash
dotnet new console -n HolaMundo
cd HolaMundo
dotnet run
```

La salida debería ser:

```text
Hello, World!
```

## Qué acaba de pasar

- `dotnet new console` creó un proyecto de consola con un archivo `Program.cs`.
- `dotnet run` compiló el proyecto y ejecutó el resultado.
- Se crearon las carpetas `obj/` y `bin/`: ahí van los archivos generados por la compilación.

Abrí `Program.cs` y vas a encontrar algo así:

```csharp
Console.WriteLine("Hello, World!");
```

Cambiá el texto, guardá y volvé a ejecutar `dotnet run`. Ese es el ciclo básico: editar, compilar, ejecutar.

## Errores comunes

| Error | Causa probable | Solución |
|---|---|---|
| `dotnet: command not found` | La terminal no encuentra el SDK | Cerrar y abrir la terminal, o reinstalar marcando la opción de `PATH` |
| Se ejecuta una versión distinta | Hay varios SDK instalados | Revisar `dotnet --list-sdks` y usar un `global.json` |
| `dotnet run` falla por permisos | Carpeta protegida | Trabajar en una carpeta de usuario, no en `C:\Program Files` |

## Resumen

- El SDK incluye el runtime y las herramientas.
- `dotnet --info` muestra la versión y el entorno.
- `dotnet new console` crea un proyecto.
- `dotnet run` compila y ejecuta.
- `bin/` y `obj/` son carpetas generadas; no se versionan.

## Siguiente paso

En la próxima lección vas a conocer la CLI y la anatomía de un proyecto.
