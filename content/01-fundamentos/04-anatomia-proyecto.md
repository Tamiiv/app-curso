---
slug: anatomia-proyecto
title: "Anatomía de un proyecto de consola"
order: 4
estimatedMinutes: 12
summary: "Qué hay dentro de un proyecto: el .csproj, Program.cs, las carpetas obj y bin, y cómo se organiza el código."
quiz:
  - question: "¿Qué archivo define el proyecto y sus propiedades de compilación?"
    options:
      - "Program.cs"
      - "El archivo .csproj"
      - "obj/project.assets.json"
      - "global.json"
    answer: 1
    explanation: "El .csproj es el archivo MSBuild que describe el framework, el tipo de salida y las dependencias del proyecto."
  - question: "¿Cuál de estas carpetas NO se debe versionar en Git?"
    options:
      - "src/"
      - "docs/"
      - "bin/"
      - "tests/"
    answer: 2
    explanation: "bin/ contiene el resultado de la compilación y se regenera; al igual que obj/, no debe subirse al repositorio."
  - question: "¿Qué hace la propiedad Nullable con valor enable?"
    options:
      - "Activa las advertencias sobre posibles referencias nulas"
      - "Impide compilar si hay algún nulo"
      - "Borra las variables sin usar"
      - "Convierte todos los strings en null"
    answer: 0
    explanation: "Nullable enable activa el análisis de nulos del compilador y genera advertencias como CS8602 cuando hay un posible null."
---

# Anatomía de un proyecto de consola

## Objetivo

Entender qué archivos forman un proyecto .NET, para qué sirve cada uno y cómo organizar el código en carpetas sin romper la compilación.

## Requisitos previos

Haber creado y ejecutado un proyecto con la CLI en [La CLI de .NET](/modulos/fundamentos/cli-dotnet).

## Concepto

Un proyecto .NET es una carpeta con un archivo `.csproj` que describe cómo compilarla. MSBuild lee ese XML. El atributo `Sdk` define el tipo de proyecto; `TargetFramework`, la versión de .NET; `OutputType`, si es un ejecutable o una biblioteca.

`Program.cs` es el punto de entrada. Con **top-level statements**, el compilador genera un método `Main` por vos. Todos los archivos `.cs` de la carpeta y sus subcarpetas se incluyen automáticamente: no hay que listarlos en el `.csproj`.

`obj/` guarda archivos intermedios: la restauración de NuGet, el grafo de dependencias y el código generado. `bin/` guarda el resultado final: el `.dll`, el `.exe`, el `runtimeconfig.json` y las dependencias copiadas. Ninguna de las dos se versiona.

`Properties/launchSettings.json` aparece en proyectos web y de worker, no en una consola. `global.json` fija la versión del SDK para todo el repositorio. `Directory.Build.props` comparte propiedades entre varios proyectos.

El flujo de una compilación es: restaurar paquetes, compilar el código y copiar todo a `bin/<configuración>/<framework>/`.

## Ejemplo mínimo

El `.csproj` de una consola recién creada:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

- `OutputType=Exe`: genera un ejecutable.
- `TargetFramework=net10.0`: compila para .NET 10.
- `ImplicitUsings=enable`: agrega `using System;`, `System.Collections.Generic` y otros.
- `Nullable=enable`: activa las advertencias de nulos.

`Program.cs`:

```csharp
Console.WriteLine("Anatomía de un proyecto");
```

Salida:

```text
Anatomía de un proyecto
```

Al compilar, la estructura queda así:

```text
MiProyecto/
├── MiProyecto.csproj
├── Program.cs
├── obj/
│   ├── project.assets.json
│   └── Debug/net10.0/
└── bin/
    └── Debug/net10.0/
        ├── MiProyecto.dll
        ├── MiProyecto.exe
        └── MiProyecto.runtimeconfig.json
```

## Ejemplo aplicado

A medida que el programa crece, conviene separar responsabilidades en carpetas. Una estructura simple:

```text
Presupuesto/
├── Presupuesto.csproj
├── Program.cs
└── Servicios/
    └── Formato.cs
```

`Servicios/Formato.cs`:

```csharp
using System.Globalization;

namespace Presupuesto.Servicios;

public static class Formato
{
    public static string Moneda(decimal monto) =>
        $"$ {monto.ToString("N2", CultureInfo.GetCultureInfo("es-AR"))}";
}
```

`Program.cs`:

```csharp
using Presupuesto.Servicios;

var alquiler = 150_000m;
var comida = 45_000m;
var transporte = 12_500m;
var total = alquiler + comida + transporte;

Console.WriteLine($"Total: {Formato.Moneda(total)}");
```

Salida:

```text
Total: $ 207.500,00
```

La carpeta no cambia el namespace por arte de magia: la convención es que `Servicios/Formato.cs` use `namespace Presupuesto.Servicios;`. Mantener esa correspondencia hace que encontrar un tipo sea inmediato.

Para fijar el SDK del repositorio, creá un `global.json`:

```bash
dotnet new globaljson --sdk-version 10.0.100
```

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

`rollForward` permite usar una banda de características más nueva dentro de .NET 10 sin saltar a .NET 11.

## Errores comunes

| Error | Causa probable | Solución |
|---|---|---|
| `CS0017: More than one entry point` | Hay dos archivos con top-level statements | Dejar uno solo o convertir el otro en una clase |
| `NETSDK1045` | El SDK no soporta `net10.0` | Instalar el SDK 10 o bajar `TargetFramework` |
| `CS0246` | Falta un `using`, una referencia o un paquete | Agregar el `using` o la referencia |
| Los cambios no aparecen | Se editó un archivo en `bin/` u `obj/` | Editar el fuente y recompilar |
| `dotnet run` toma otro proyecto | Hay varios `.csproj` en la carpeta | Usar `--project` |
| `global.json` apunta a un SDK inexistente | La versión fijada no está instalada | Revisar `dotnet --list-sdks` y ajustar |

## Buenas prácticas

- Un proyecto por carpeta; el nombre del `.csproj` en PascalCase.
- `Program.cs` chico: la lógica va en clases con nombres claros.
- Carpetas y namespaces alineados (`Modelos`, `Servicios`, `Datos`).
- No versionar `bin/` ni `obj/`; usá `dotnet new gitignore`.
- Usá `global.json` en repos con varias personas o varios SDK.
- Usá `Directory.Build.props` para propiedades comunes, en lugar de repetirlas en cada `.csproj`.
- No edites archivos generados: se sobrescriben en la próxima compilación.

## Ejercicio

Reorganizá una app de consola en tres archivos: `Program.cs`, `Modelos/Producto.cs` y `Servicios/CalculadoraDescuento.cs`. El programa debe crear un producto de `$ 100.000` con 15% de descuento y mostrar `Teclado: $ 85.000,00`.

<details>
<summary>Ver solución</summary>

Comandos:

```bash
dotnet new console -n Tienda
cd Tienda
mkdir Modelos Servicios
```

`Modelos/Producto.cs`:

```csharp
namespace Tienda.Modelos;

public sealed class Producto
{
    public Producto(string nombre, decimal precio)
    {
        Nombre = nombre;
        Precio = precio;
    }

    public string Nombre { get; }
    public decimal Precio { get; }
}
```

`Servicios/CalculadoraDescuento.cs`:

```csharp
using Tienda.Modelos;

namespace Tienda.Servicios;

public static class CalculadoraDescuento
{
    public static decimal PrecioFinal(Producto producto, decimal porcentaje) =>
        producto.Precio * (1 - porcentaje / 100);
}
```

`Program.cs`:

```csharp
using System.Globalization;
using Tienda.Modelos;
using Tienda.Servicios;

var producto = new Producto("Teclado", 100_000m);
var precioFinal = CalculadoraDescuento.PrecioFinal(producto, 15);

Console.WriteLine($"{producto.Nombre}: $ {precioFinal.ToString("N2", CultureInfo.GetCultureInfo("es-AR"))}");
```

Salida:

```text
Teclado: $ 85.000,00
```

</details>

## Resumen

- El `.csproj` describe el proyecto; MSBuild lo compila.
- `Program.cs` es el punto de entrada y los `.cs` se incluyen solos.
- `obj/` es intermedio y `bin/` es la salida; no se versionan.
- Carpetas y namespaces alineados mantienen el código navegable.

## Siguiente paso

En [NuGet y gestión de dependencias](/modulos/fundamentos/nuget) vas a aprender a sumar bibliotecas externas al proyecto sin perder el control de las versiones.
