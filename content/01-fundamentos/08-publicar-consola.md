---
slug: publicar-consola
title: "Publicar una aplicación de consola"
order: 8
estimatedMinutes: 10
summary: "Generar una versión lista para entregar: framework-dependent, self-contained, un solo archivo y para otra plataforma."
quiz:
  - question: "¿Qué comando genera la carpeta lista para entregar?"
    options:
      - "dotnet build -c Release"
      - "dotnet publish -c Release"
      - "dotnet pack"
      - "dotnet run -c Release"
    answer: 1
    explanation: "dotnet publish compila y arma la carpeta de salida con la app, sus dependencias y el runtime si es self-contained."
  - question: "¿Qué significa que una publicación sea self-contained?"
    options:
      - "Que incluye el runtime de .NET y no lo necesita en destino"
      - "Que no necesita código fuente"
      - "Que solo funciona en Windows"
      - "Que comprime el código en un ZIP"
    answer: 0
    explanation: "Self-contained incluye el runtime y las bibliotecas de la plataforma, por eso el destino no necesita tener .NET instalado."
  - question: "¿Qué propiedad genera un único ejecutable?"
    options:
      - "<PublishSingleFile>true</PublishSingleFile>"
      - "<SingleFile>true</SingleFile>"
      - "<OutputType>Single</OutputType>"
      - "<SelfContained>single</SelfContained>"
    answer: 0
    explanation: "PublishSingleFile empaqueta la app, sus dependencias y el runtime en un solo archivo ejecutable."
---

# Publicar una aplicación de consola

## Objetivo

Generar una carpeta lista para entregar, elegir entre framework-dependent y self-contained, y publicar para Windows o Linux.

## Requisitos previos

Haber completado las lecciones anteriores del módulo y tener una app de consola que funcione. Idealmente, el proyecto ya está en un repositorio Git.

## Concepto

`dotnet build` produce archivos para desarrollar. `dotnet publish` arma la carpeta que vas a entregar: tu `.dll`, las dependencias, el `runtimeconfig.json` y un ejecutable nativo (apphost). Desde .NET 8, `dotnet publish` usa `Release` por defecto, pero conviene escribirlo explícito.

Hay dos modos principales:

- **Framework-dependent (FDD)**: la app no incluye el runtime. Es chica (unos pocos MB) y necesita .NET instalado en la máquina destino.
- **Self-contained (SCD)**: incluye el runtime completo. Es más grande (60-80 MB) y corre sin instalar .NET. Requiere un **RID** (`win-x64`, `linux-x64`, `osx-arm64`, etc.).

Un RID identifica sistema operativo y arquitectura. `dotnet --info` muestra el RID de tu máquina.

Otras opciones:

- `PublishSingleFile`: empaqueta todo en un ejecutable.
- `PublishReadyToRun`: precompila para mejorar el arranque; aumenta el tamaño.
- `PublishTrimmed`: quita código no usado; puede romper reflexión y serialización.
- `PublishAot`: compila a código nativo; arranque rápido y binario chico, pero requiere toolchain y tiene limitaciones.

Para una consola simple, self-contained + single-file es la opción más fácil de distribuir. Para servidores donde ya está .NET, FDD es más liviano.

## Ejemplo mínimo

```bash
dotnet new console -n PublicarDemo
cd PublicarDemo
dotnet publish -c Release
```

Salida:

```text
PublicarDemo -> C:\ruta\PublicarDemo\bin\Release\net10.0\publish\
```

Ejecutá lo publicado:

```bash
dotnet bin/Release/net10.0/publish/PublicarDemo.dll
```

En Windows también podés usar el apphost:

```powershell
.\bin\Release\net10.0\publish\PublicarDemo.exe
```

Salida:

```text
Hello, World!
```

La carpeta `publish/` es lo que se entrega. No copies `bin/Debug` ni el código fuente.

## Ejemplo aplicado

Para distribuir a alguien que no tiene .NET, configurá el `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>
  </PropertyGroup>

</Project>
```

```bash
dotnet publish -c Release
```

La salida queda en `bin/Release/net10.0/win-x64/publish/`. Vas a ver un solo `PublicarDemo.exe` (más el `.pdb`, que podés excluir con `<DebugType>none</DebugType>` en Release).

Para Linux, sin cambiar el `.csproj`:

```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

La salida queda en `bin/Release/net10.0/linux-x64/publish/`. El ejecutable se llama `PublicarDemo`, sin extensión. Copialo a una máquina Linux (o a un contenedor) y ejecutalo:

```bash
chmod +x PublicarDemo
./PublicarDemo
```

`chmod +x` hace falta porque el archivo viaja sin permiso de ejecución si lo comprimís en Windows.

Para una versión framework-dependent con apphost:

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

Esa carpeta es más chica, pero en destino hace falta el runtime de .NET 10.

| Modo | ¿Necesita .NET en destino? | Tamaño aproximado |
|---|---|---|
| Framework-dependent | Sí | 1-5 MB |
| Self-contained | No | 60-80 MB |
| Self-contained single-file | No | 60-80 MB |
| AOT | No | 10-20 MB |

Los tamaños dependen de las dependencias y de la plataforma. Medí siempre la carpeta real.

## Errores comunes

| Error | Causa probable | Solución |
|---|---|---|
| Se publicó en Debug | Faltó `-c Release` | `dotnet publish -c Release` |
| `You must install .NET to run this application` | FDD en una máquina sin runtime | Instalar el runtime o publicar self-contained |
| `NETSDK1047` | Falta el target del RID en `project.assets.json` | `dotnet restore -r <rid>` y volver a publicar |
| El ejecutable no encuentra un archivo | Ruta relativa al directorio actual | Usar `AppContext.BaseDirectory` e incluir el archivo con `CopyToOutputDirectory` |
| La app trimeada falla con reflexión | `PublishTrimmed` quitó código usado dinámicamente | Desactivar trimming o agregar `TrimmerRootAssembly` |
| `PublishAot` falla | Falta toolchain de C++ o una dependencia no es compatible | Instalar las herramientas y revisar compatibilidad |
| El `.exe` no corre en otra PC | RID o arquitectura equivocados | Verificar `win-x64` vs. `win-arm64` |
| El antivirus marca el single-file | Autoextracción del ejecutable | Firmar el binario o publicar en carpeta |

## Buenas prácticas

- Publicá desde CI para releases reproducibles; no desde una máquina con cambios locales.
- Probá la carpeta publicada en una máquina o contenedor limpio.
- Usá FDD en servidores que ya tienen .NET; SCD para usuarios finales.
- Fijá `<Version>` en el `.csproj` y etiquetá el commit con la misma versión.
- No commitees `bin/`, `obj/` ni la carpeta `publish/`.
- Evitá `PublishTrimmed` y `PublishAot` hasta tener pruebas que cubran la app.
- Documentá en el README qué RID publicaste y qué requisitos tiene el destino.
- Si usás `InvariantGlobalization`, sabé que perdés formatos y culturas; no lo actives en apps que muestran monedas o fechas locales.

## Ejercicio

Publicá una app `SaludoPublicado` como self-contained single-file para `win-x64` y `linux-x64`. Ejecutá el `.exe` desde otra carpeta y verificá que no dependa del SDK. Mostrá el tamaño de cada archivo.

<details>
<summary>Ver solución</summary>

```bash
dotnet new console -n SaludoPublicado
cd SaludoPublicado
```

`Program.cs`:

```csharp
Console.WriteLine("Listo para publicar.");
```

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

Ejecutá el binario de Windows desde otra carpeta:

```powershell
cd C:\otra-carpeta
C:\ruta\SaludoPublicado\bin\Release\net10.0\win-x64\publish\SaludoPublicado.exe
```

Salida:

```text
Listo para publicar.
```

Tamaños:

```powershell
Get-ChildItem C:\ruta\SaludoPublicado\bin\Release\net10.0\win-x64\publish |
    Select-Object Name, Length
```

El `.exe` ronda los 70 MB y el binario de Linux un tamaño similar. El de Linux no se ejecuta en Windows: copialo a Linux, WSL o un contenedor y corré `chmod +x ./SaludoPublicado` antes de ejecutarlo.

</details>

## Resumen

- `dotnet publish` arma la carpeta entregable; usá `-c Release`.
- FDD es chico y necesita runtime; SCD incluye todo y usa un RID.
- `PublishSingleFile` simplifica la distribución.
- Trimming y AOT mejoran tamaño o arranque, pero exigen pruebas.
- Probá siempre la salida en un entorno limpio.

## Siguiente paso

En [Ejercicio integrador y quiz](/modulos/fundamentos/ejercicio-integrador) vas a combinar todo lo del módulo en un proyecto completo.
