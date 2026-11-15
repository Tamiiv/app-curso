---
slug: nuget
title: "NuGet y gestión de dependencias"
order: 5
estimatedMinutes: 12
summary: "Cómo agregar, actualizar y auditar paquetes NuGet sin romper el proyecto ni la compilación."
quiz:
  - question: "¿Qué comando agrega un paquete a un proyecto?"
    options:
      - "dotnet add package Nombre"
      - "dotnet install Nombre"
      - "dotnet new package Nombre"
      - "dotnet restore Nombre"
    answer: 0
    explanation: "dotnet add package agrega una PackageReference al .csproj y restaura el paquete."
  - question: "¿Dónde queda registrada una dependencia directa?"
    options:
      - "En Program.cs"
      - "En el archivo .csproj"
      - "En la carpeta bin/"
      - "En el README"
    answer: 1
    explanation: "La dependencia directa se declara como PackageReference dentro del .csproj; el resto se genera al restaurar."
  - question: "¿Qué comando lista los paquetes con vulnerabilidades conocidas?"
    options:
      - "dotnet list package --vulnerable"
      - "dotnet audit"
      - "dotnet restore --secure"
      - "dotnet nuget scan"
    answer: 0
    explanation: "dotnet list package --vulnerable consulta los orígenes configurados y marca los paquetes con avisos de seguridad."
---

# NuGet y gestión de dependencias

## Objetivo

Agregar un paquete NuGet, entender cómo se registra y se resuelve, y mantener las dependencias actualizadas y auditadas.

## Requisitos previos

Haber creado y compilado un proyecto en [La CLI de .NET](/modulos/fundamentos/cli-dotnet) y entender el `.csproj` en [Anatomía de un proyecto de consola](/modulos/fundamentos/anatomia-proyecto).

## Concepto

NuGet es el gestor de paquetes de .NET. Un paquete es un archivo `.nupkg` con bibliotecas compiladas y metadatos. Cuando agregás uno, NuGet escribe una `PackageReference` en el `.csproj`, lo descarga en la caché global (`%USERPROFILE%\.nuget\packages` en Windows, `~/.nuget/packages` en macOS y Linux) y genera `obj/project.assets.json` con el grafo resuelto.

Hay dependencias **directas** y **transitivas**. Si agregás `Spectre.Console`, esa es directa; las bibliotecas que necesita para funcionar son transitivas. `dotnet list package` muestra las directas y `--include-transitive` muestra todas.

Las versiones siguen SemVer: `MAJOR.MINOR.PATCH`. En un `.csproj`, una versión como `0.57.2` funciona como mínimo: NuGet elige la más baja que cumpla, salvo que otra dependencia exija una mayor. Para fijar una versión exacta se usa `[0.57.2]`. En aplicaciones conviene fijar versiones y, si necesitás reproducibilidad total, activar un lock file.

Los orígenes por defecto incluyen nuget.org. Si tu empresa tiene un feed privado, se configura en `NuGet.config`. Nunca guardes credenciales en ese archivo: usá variables de entorno o el credential provider.

En repos con varios proyectos, la **gestión central de paquetes** evita versiones distintas para el mismo paquete: las versiones viven en `Directory.Packages.props` y los `.csproj` solo declaran el nombre.

## Ejemplo mínimo

```bash
dotnet new console -n ConsolaNuGet
cd ConsolaNuGet
dotnet add package Spectre.Console
```

El `.csproj` queda así:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Spectre.Console" Version="0.57.2" />
  </ItemGroup>

</Project>
```

`Program.cs`:

```csharp
using Spectre.Console;

AnsiConsole.MarkupLine("[bold green]Hola, NuGet![/]");
```

Salida:

```text
Hola, NuGet!
```

El texto se ve en verde en una terminal compatible. La versión puede ser más nueva: `dotnet add package` elige la última estable disponible.

Para revisar lo instalado:

```bash
dotnet list package
dotnet list package --outdated
```

## Ejemplo aplicado

Una app que muestra un reporte de paquetes con una tabla:

```csharp
using Spectre.Console;

var tabla = new Table();
tabla.AddColumn("Paquete");
tabla.AddColumn("Para qué sirve");
tabla.AddRow("Spectre.Console", "Color, tablas y prompts");
tabla.AddRow("Humanizer", "Texto legible para personas");
tabla.AddRow("Serilog", "Logging estructurado");

AnsiConsole.Write(tabla);
```

Salida aproximada:

```text
┌─────────────────┬────────────────────────────────┐
│ Paquete         │ Para qué sirve                 │
├─────────────────┼────────────────────────────────┤
│ Spectre.Console │ Color, tablas y prompts        │
│ Humanizer       │ Texto legible para personas    │
│ Serilog         │ Logging estructurado           │
└─────────────────┴────────────────────────────────┘
```

El ancho se ajusta al contenido, así que los bordes pueden variar.

Antes de sumar un paquete, revisá el grafo y la seguridad:

```bash
dotnet list package --outdated
dotnet list package --vulnerable --include-transitive
dotnet nuget why ConsolaNuGet.csproj Spectre.Console
```

`--outdated` muestra versiones más nuevas disponibles. `--vulnerable` marca paquetes con avisos de seguridad, incluidas las dependencias transitivas. `dotnet nuget why` explica por qué un paquete está en el grafo, algo muy útil cuando aparece una dependencia que no recordás haber agregado.

Para un repositorio con varios proyectos, creá `Directory.Packages.props` en la raíz:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Spectre.Console" Version="0.57.2" />
  </ItemGroup>
</Project>
```

Y en cada `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Spectre.Console" />
</ItemGroup>
```

Así actualizás la versión en un solo lugar.

## Errores comunes

| Error | Causa probable | Solución |
|---|---|---|
| `NU1101` | El paquete no existe o el nombre está mal escrito | Revisar el ID exacto en nuget.org |
| `NU1102` | La versión pedida no existe | Consultar las versiones disponibles y ajustar |
| `NU1605` | Una dependencia pide una versión menor que la instalada | Alinear versiones o actualizar el paquete |
| `NU1903` | El paquete tiene una vulnerabilidad conocida | Actualizar a una versión corregida |
| `NU1301` | No se puede acceder al origen | Revisar red, proxy y `NuGet.config` |
| `dotnet add package` falla | No hay proyecto en la carpeta | Entrar al proyecto o usar `--project` |
| El tipo no se encuentra después de instalar | Falta el `using` o el paquete no soporta `net10.0` | Revisar el namespace y el framework del paquete |
| Dos paquetes definen el mismo tipo | Dependencias duplicadas | Elegir uno y quitar el otro |

## Buenas prácticas

- Agregá solo lo necesario: cada dependencia es código que no controlás.
- Antes de instalar, verificá si la biblioteca base de .NET ya resuelve el problema.
- Fijá versiones en aplicaciones y evitá rangos amplios o `*`.
- Usá gestión central de paquetes en repos con varios proyectos.
- Auditá con `dotnet list package --vulnerable` y actualizá con regularidad.
- Revisá licencia, mantenimiento y compatibilidad antes de adoptar un paquete.
- Commiteá el `.csproj` y el lock file si lo usás; nunca la caché de NuGet.
- No guardes tokens ni contraseñas en `NuGet.config`.

## Ejercicio

Creá una app `ReportePaquetes` que muestre una tabla con tres paquetes y su función. Agregá `Spectre.Console`, ejecutá la app y revisá si hay actualizaciones con `dotnet list package --outdated`. Si aparece una versión nueva, actualizala y volvé a ejecutar.

<details>
<summary>Ver solución</summary>

```bash
dotnet new console -n ReportePaquetes
cd ReportePaquetes
dotnet add package Spectre.Console
```

`Program.cs`:

```csharp
using Spectre.Console;

var tabla = new Table();
tabla.AddColumn("Paquete");
tabla.AddColumn("Para qué sirve");
tabla.AddRow("Spectre.Console", "Color, tablas y prompts");
tabla.AddRow("Humanizer", "Texto legible para personas");
tabla.AddRow("Serilog", "Logging estructurado");

AnsiConsole.Write(tabla);
```

```bash
dotnet run
dotnet list package --outdated
```

Si hay una versión más nueva:

```bash
dotnet add package Spectre.Console
dotnet run
```

Salida esperada, con el ancho ajustado al contenido:

```text
┌─────────────────┬────────────────────────────────┐
│ Paquete         │ Para qué sirve                 │
├─────────────────┼────────────────────────────────┤
│ Spectre.Console │ Color, tablas y prompts        │
│ Humanizer       │ Texto legible para personas    │
│ Serilog         │ Logging estructurado           │
└─────────────────┴────────────────────────────────┘
```

Si `--outdated` no muestra nada, el paquete ya está en la última versión estable.

</details>

## Resumen

- NuGet registra dependencias como `PackageReference` en el `.csproj`.
- Las dependencias pueden ser directas o transitivas.
- `dotnet list package`, `--outdated` y `--vulnerable` te dan visibilidad.
- La gestión central de paquetes unifica versiones en repos grandes.
- Cada paquete que agregás es una responsabilidad que asumís.

## Siguiente paso

En [Debugging y errores comunes](/modulos/fundamentos/debugging) vas a aprender a leer los errores que aparecen cuando el código o las dependencias no se comportan como esperás.
