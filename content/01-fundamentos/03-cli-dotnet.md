---
slug: cli-dotnet
title: "La CLI de .NET: new, build, run, test"
order: 3
estimatedMinutes: 12
summary: "Los comandos que vas a usar todos los días para crear, compilar, ejecutar y probar proyectos .NET."
quiz:
  - question: "¿Qué comando crea un proyecto de consola llamado MiApp?"
    options:
      - "dotnet new console -n MiApp"
      - "dotnet console new MiApp"
      - "dotnet run console MiApp"
      - "dotnet build MiApp"
    answer: 0
    explanation: "dotnet new console usa la plantilla de consola y -n define el nombre del proyecto y de la carpeta."
  - question: "¿Qué hace dotnet build?"
    options:
      - "Ejecuta el programa sin compilar"
      - "Compila el proyecto y genera los binarios"
      - "Solo descarga los paquetes NuGet"
      - "Publica la app para producción"
    answer: 1
    explanation: "dotnet build restaura dependencias, compila el código y deja el resultado en bin/, pero no ejecuta la aplicación."
  - question: "¿Para qué sirve dotnet test?"
    options:
      - "Para compilar la app en Release"
      - "Para ejecutar las pruebas del proyecto"
      - "Para crear un proyecto de pruebas"
      - "Para borrar las carpetas bin y obj"
    answer: 1
    explanation: "dotnet test descubre, compila y ejecuta las pruebas del proyecto o la solución que le indiques."
---

# La CLI de .NET: new, build, run, test

## Objetivo

Al terminar vas a poder crear proyectos y soluciones desde la terminal, compilarlos, ejecutarlos y correr pruebas con `dotnet`. También vas a saber qué comando usar en cada momento y cómo consultar la ayuda.

## Requisitos previos

Haber instalado el SDK de .NET 10 y creado tu primer proyecto en [Instalar .NET y verificar el entorno](/modulos/fundamentos/instalar-dotnet). Necesitás una terminal y un editor de texto.

## Concepto

`dotnet` es la interfaz de línea de comandos (CLI) del SDK. Todos los comandos siguen la forma `dotnet <verbo> [opciones]`. Los verbos que vas a usar a diario son `new`, `build`, `run`, `test`, `add`, `restore`, `clean` y `publish`.

La CLI no es un reemplazo del IDE: es la misma herramienta que usa Visual Studio o VS Code por debajo. Saber usarla te sirve para automatizar tareas, trabajar en servidores y entender qué pasa cuando un botón "no compila".

`dotnet new` crea proyectos a partir de plantillas. `dotnet build` compila y deja los binarios en `bin/`. `dotnet run` compila y ejecuta en un solo paso. `dotnet test` compila y ejecuta las pruebas. `dotnet restore` descarga las dependencias; casi siempre se ejecuta solo, como parte de `build`, `run` o `test`.

Una **solución** agrupa varios proyectos. En .NET 10, `dotnet new sln` crea por defecto un archivo `.slnx` (el formato XML nuevo); si necesitás el clásico `.sln`, usá `dotnet new sln --format sln`. Una solución no compila nada por sí sola: es una lista de proyectos.

## Ejemplo mínimo

```bash
dotnet new console -n CliDemo
cd CliDemo
dotnet build
dotnet run
```

Salida:

```text
Hello, World!
```

`dotnet new console -n CliDemo` crea la carpeta `CliDemo` con `CliDemo.csproj` y `Program.cs`. `dotnet build` compila y muestra la ruta del `.dll`. `dotnet run` hace lo mismo y además ejecuta el programa.

Para ver todas las plantillas disponibles:

```bash
dotnet new list
```

Y para consultar cualquier comando:

```bash
dotnet build --help
```

## Ejemplo aplicado

Supongamos que querés separar la lógica en una biblioteca y probarla. Creá una solución con dos proyectos:

```bash
dotnet new sln -n Calculadora
dotnet new classlib -n Calculadora.Core
dotnet new xunit -n Calculadora.Tests
dotnet sln add Calculadora.Core/Calculadora.Core.csproj Calculadora.Tests/Calculadora.Tests.csproj
dotnet add Calculadora.Tests/Calculadora.Tests.csproj reference Calculadora.Core/Calculadora.Core.csproj
```

Reemplazá `Class1.cs` por `Operaciones.cs`:

```csharp
namespace Calculadora.Core;

public static class Operaciones
{
    public static int Sumar(int a, int b) => a + b;
}
```

Y escribí la prueba en `Calculadora.Tests/OperacionesTests.cs`:

```csharp
using Calculadora.Core;

namespace Calculadora.Tests;

public class OperacionesTests
{
    [Fact]
    public void Sumar_DosEnteros_DevuelveLaSuma()
    {
        Assert.Equal(5, Operaciones.Sumar(2, 3));
    }
}
```

Ejecutá las pruebas:

```bash
dotnet test
```

`dotnet test` descubre el proyecto de pruebas de la solución, lo compila y corre cada `[Fact]`. La salida termina con algo así:

```text
Correctas! - Con error: 0, Superado: 1, Omitido: 0, Total: 1
```

Opciones que vas a necesitar seguido:

| Comando | Para qué |
|---|---|
| `dotnet build -c Release` | Compilar optimizado para producción |
| `dotnet run --project App/App.csproj` | Ejecutar un proyecto específico |
| `dotnet run -- --verbose` | Pasar argumentos a tu programa |
| `dotnet test --filter "FullyQualifiedName~Operaciones"` | Correr solo algunas pruebas |
| `dotnet clean` | Borrar los artefactos de la última compilación |

Cuando hay varios proyectos en la carpeta, `dotnet run` necesita que le digas cuál usar con `--project`. Una biblioteca de clases no tiene punto de entrada, así que no se ejecuta directamente.

## Errores comunes

| Error | Causa probable | Solución |
|---|---|---|
| `MSB1003: Specify a project or solution file` | La carpeta no tiene un proyecto o hay varios | Entrar a la carpeta del proyecto o usar `--project` |
| `Couldn't find a project to run` | `dotnet run` en el lugar equivocado | Pasar la ruta con `--project` |
| `dotnet test` no encuentra pruebas | Se ejecutó en un proyecto sin tests o el filtro no coincide | Apuntar al proyecto de pruebas y revisar el filtro |
| La app usa código viejo | Quedó una compilación anterior | `dotnet clean` y volver a compilar |
| `dotnet new` dice que la carpeta ya existe | El nombre ya está en uso | Elegir otro `-n` o usar `-o` |
| Se compila el proyecto equivocado | Hay varios `.csproj` en la carpeta | Ser explícito con `--project` |

## Buenas prácticas

- Usá una solución por repositorio y un proyecto por responsabilidad.
- Compilá en `Release` antes de publicar; `Debug` es para desarrollar.
- Corré `dotnet test` antes de cada commit.
- No edites nada dentro de `bin/` ni `obj/`: son carpetas generadas.
- Usá `dotnet format` para mantener el estilo y `dotnet format --verify-no-changes` en CI.
- Cuando un comando falle, leé el código de error (`MSB...`, `CS...`, `NU...`) y buscá ese código, no el mensaje completo.

## Ejercicio

Creá una solución `HolaCli` con un proyecto de consola `HolaCli.App` y un proyecto de pruebas `HolaCli.Tests`. Agregá al proyecto de consola una clase `Saludador` con el método `Saludar(string nombre)` que devuelva `Hola, {nombre}!`. Escribí una prueba que verifique `Saludar("Ana")` y ejecutá `dotnet test` y `dotnet run --project HolaCli.App`.

<details>
<summary>Ver solución</summary>

Comandos:

```bash
dotnet new sln -n HolaCli
dotnet new console -n HolaCli.App
dotnet new xunit -n HolaCli.Tests
dotnet sln add HolaCli.App/HolaCli.App.csproj HolaCli.Tests/HolaCli.Tests.csproj
dotnet add HolaCli.Tests/HolaCli.Tests.csproj reference HolaCli.App/HolaCli.App.csproj
```

`HolaCli.App/Saludador.cs`:

```csharp
namespace HolaCli.App;

public static class Saludador
{
    public static string Saludar(string nombre) => $"Hola, {nombre}!";
}
```

`HolaCli.App/Program.cs`:

```csharp
using HolaCli.App;

Console.WriteLine(Saludador.Saludar("Ana"));
```

`HolaCli.Tests/SaludadorTests.cs`:

```csharp
using HolaCli.App;

namespace HolaCli.Tests;

public class SaludadorTests
{
    [Fact]
    public void Saludar_ConNombre_DevuelveSaludo()
    {
        Assert.Equal("Hola, Ana!", Saludador.Saludar("Ana"));
    }
}
```

Borrá `HolaCli.Tests/UnitTest1.cs` para no dejar la prueba vacía. Después:

```bash
dotnet test
dotnet run --project HolaCli.App
```

Salida esperada:

```text
Correctas! - Con error: 0, Superado: 1, Omitido: 0, Total: 1
Hola, Ana!
```

</details>

## Resumen

- `dotnet new` crea proyectos; `dotnet sln` los agrupa.
- `dotnet build` compila, `dotnet run` compila y ejecuta, `dotnet test` corre pruebas.
- `--project`, `-c` y `--filter` ajustan qué se ejecuta y cómo.
- Los códigos `CS`, `MSB` y `NU` indican de dónde viene el problema.

## Siguiente paso

Ahora que sabés moverte con la CLI, en [Anatomía de un proyecto de consola](/modulos/fundamentos/anatomia-proyecto) vas a ver qué hay dentro de esos archivos que `dotnet new` creó.
