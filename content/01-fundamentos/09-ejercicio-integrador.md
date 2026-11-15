---
slug: ejercicio-integrador
title: "Ejercicio integrador y quiz"
order: 9
estimatedMinutes: 20
summary: "Un proyecto completo de principio a fin: CLI, NuGet, debugging, Git y publicación en un solo ejercicio."
quiz:
  - question: "¿Cuál es el orden lógico del ciclo de un proyecto?"
    options:
      - "Crear, agregar dependencias, escribir código, depurar, versionar y publicar"
      - "Publicar, crear, depurar, versionar y agregar dependencias"
      - "Versionar, publicar, crear, depurar y agregar dependencias"
      - "Agregar dependencias, publicar, crear y versionar"
    answer: 0
    explanation: "Primero se crea el proyecto, después se suman dependencias y código, se depura, se versiona y recién al final se publica."
  - question: "¿Qué comando agrega Spectre.Console antes de compilar?"
    options:
      - "dotnet add package Spectre.Console"
      - "dotnet publish Spectre.Console"
      - "dotnet new Spectre.Console"
      - "git add Spectre.Console"
    answer: 0
    explanation: "dotnet add package modifica el .csproj y restaura la dependencia para que el código pueda usarla."
  - question: "Ante un FormatException al convertir la entrada del usuario, ¿qué conviene hacer?"
    options:
      - "Usar TryParse y validar el valor"
      - "Ignorar la excepción y seguir"
      - "Borrar las carpetas bin y obj"
      - "Compilar en Release"
    answer: 0
    explanation: "TryParse devuelve false en lugar de lanzar una excepción, así podés pedir el dato de nuevo o mostrar un mensaje claro."
  - question: "¿Qué comando publica self-contained para linux-x64?"
    options:
      - "dotnet publish -c Release -r linux-x64 --self-contained true"
      - "dotnet build -r linux-x64"
      - "dotnet run -r linux-x64"
      - "dotnet pack -r linux-x64"
    answer: 0
    explanation: "dotnet publish con -r linux-x64 y --self-contained true genera una carpeta que incluye el runtime para Linux."
---

# Ejercicio integrador y quiz

## Objetivo

Recorrer el ciclo completo de un proyecto: crearlo con la CLI, agregar una dependencia, depurarlo, versionarlo con Git y publicarlo como ejecutable.

## Requisitos previos

Haber completado las lecciones 3 a 8. Si alguna te quedó floja, repasala antes: acá se usan todas.

## Concepto

Un proyecto real no se construye en línea recta, pero este orden reduce sorpresas:

1. **Crear**: `dotnet new console`, `dotnet new gitignore`, `git init`.
2. **Dependencias**: `dotnet add package`, revisar el `.csproj`.
3. **Código**: `Program.cs` y archivos por responsabilidad.
4. **Depurar**: `dotnet build`, `dotnet run`, breakpoints, entradas inválidas.
5. **Versionar**: commits chicos, rama, push.
6. **Publicar**: `dotnet publish -c Release` con el RID correcto.

Cada etapa tiene una señal para avanzar: el build pasa, la app responde, las pruebas pasan, el commit compila, el ejecutable publicado arranca. Si una falla, no sigas: arreglala primero.

La CLI y Git son herramientas de feedback. `dotnet build` te dice si el código está bien; `git diff` te dice qué cambiaste; el stack trace te dice dónde falló. El objetivo del módulo no es memorizar comandos, sino saber cuál usar cuando algo no funciona.

## Ejemplo mínimo

El pipeline más corto de punta a punta:

```bash
dotnet new console -n PipelineDemo
cd PipelineDemo
dotnet new gitignore
git init
git add .
git commit -m "chore: proyecto inicial"
dotnet add package Spectre.Console
```

`Program.cs`:

```csharp
using Spectre.Console;

AnsiConsole.MarkupLine("[bold]Pipeline completo[/]");
```

```bash
dotnet build
dotnet run
dotnet publish -c Release
```

Salida:

```text
Pipeline completo
```

En pocos comandos pasaste por creación, Git, dependencias, compilación, ejecución y publicación. El `build` intermedio es tu red de seguridad: si falla, no llegues al commit ni al publish.

## Ejemplo aplicado

Una calculadora de propina con entrada validada, tabla y publicación:

```csharp
using System.Globalization;
using Spectre.Console;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");

Console.Write("Total de la cuenta: ");
if (!decimal.TryParse(Console.ReadLine(), out var total) || total <= 0)
{
    AnsiConsole.MarkupLine("[red]Ingresá un monto válido.[/]");
    return;
}

Console.Write("Porcentaje de propina (ej. 10): ");
if (!decimal.TryParse(Console.ReadLine(), out var porcentaje) || porcentaje < 0)
{
    AnsiConsole.MarkupLine("[red]Ingresá un porcentaje válido.[/]");
    return;
}

var propina = total * porcentaje / 100;
var final = total + propina;

var tabla = new Table();
tabla.AddColumn("Concepto");
tabla.AddColumn(new TableColumn("Monto").RightAligned());
tabla.AddRow("Cuenta", total.ToString("N2"));
tabla.AddRow("Propina", propina.ToString("N2"));
tabla.AddRow("Total", final.ToString("N2"));
AnsiConsole.Write(tabla);
```

Con `10000` y `15`, la salida es:

```text
┌──────────┬────────────┐
│ Concepto │      Monto │
├──────────┼────────────┤
│ Cuenta   │  10.000,00 │
│ Propina  │   1.500,00 │
│ Total    │  11.500,00 │
└──────────┴────────────┘
```

El ancho de la tabla se ajusta al contenido, así que los bordes pueden verse un poco distintos.

El recorrido completo:

```bash
dotnet new console -n PropinaApp
cd PropinaApp
dotnet new gitignore
git init
git add .
git commit -m "chore: estructura inicial"

dotnet add package Spectre.Console
# crear Program.cs con el código anterior
dotnet build
dotnet run

git add .
git commit -m "feat: calcular propina"
```

Durante la primera versión usamos `decimal.Parse`. Al probar con Enter vacío, la app terminó con `FormatException`. El stack trace apuntó a la línea del parseo; cambiamos a `TryParse` y validamos. Ese cambio merece su propio commit:

```bash
git add Program.cs
git commit -m "fix: validar montos con TryParse"
```

Para cerrar:

```bash
git tag -a v1.0.0 -m "Calculadora de propina"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

```powershell
.\bin\Release\net10.0\win-x64\publish\PropinaApp.exe
```

Si el ejecutable arranca desde otra carpeta, la publicación está bien. Si falla, revisá el RID y los archivos que la app espera encontrar.

## Errores comunes

| Error | Causa probable | Solución |
|---|---|---|
| Avanzar sin compilar | Los errores se acumulan y cuesta aislarlos | `dotnet build` después de cada cambio |
| Commitear `bin/` u `obj/` | Faltó `.gitignore` antes del primer commit | `dotnet new gitignore` y `git rm -r --cached bin obj` |
| El paquete no aparece | Se agregó a otro proyecto o falta restaurar | Revisar el `.csproj` y correr `dotnet restore` |
| Probar solo el caso feliz | No se validaron entradas vacías, negativas o con letras | Usar `TryParse` y probar cada borde |
| `FormatException` | Se usó `Parse` con entrada inválida | Cambiar a `TryParse` |
| Publicar en Debug | Faltó `-c Release` | `dotnet publish -c Release` |
| El `.exe` no corre en otra máquina | FDD sin runtime o RID incorrecto | Publicar self-contained con el RID correcto |
| `push` rechazado | El remoto tiene commits nuevos | `git pull --rebase origin main` y volver a pushear |
| El publish usa archivos viejos | Quedó una salida anterior | `dotnet clean` y volver a publicar |

## Buenas prácticas

- Un commit por etapa: estructura, funcionalidad, corrección, publicación.
- Etiquetá la versión publicada y usá la misma en `<Version>`.
- Probá entradas inválidas antes de publicar.
- Dejá `Program.cs` legible; si crece, mové la lógica a clases.
- Auditá las dependencias con `dotnet list package --vulnerable` antes de entregar.
- Escribí un README con los comandos de ejecución y publicación.
- Publicá desde CI cuando el proyecto sea compartido.
- Guardá la carpeta `publish/` fuera del repositorio.

## Ejercicio

Construí `PresupuestoApp` desde cero. Debe:

1. Crearse con la CLI, con `.gitignore` y repositorio Git.
2. Usar `Spectre.Console` para la salida.
3. Pedir nombre, ingreso mensual, alquiler, comida y transporte. Validar que los montos sean números mayores o iguales a cero.
4. Calcular el total de gastos, el saldo disponible y el porcentaje del ingreso gastado.
5. Mostrar una tabla con los montos y un mensaje verde si hay saldo, rojo si el saldo es negativo.
6. Provocar un error a propósito con una entrada inválida, leer el mensaje, corregirlo y registrar el arreglo en un commit `fix:`.
7. Hacer al menos tres commits y subir el proyecto a GitHub.
8. Publicar self-contained single-file para `win-x64` y `linux-x64`, y ejecutar el de Windows.

<details>
<summary>Ver solución</summary>

Comandos:

```bash
dotnet new console -n PresupuestoApp
cd PresupuestoApp
dotnet new gitignore
git init
git add .
git commit -m "chore: estructura inicial"

dotnet add package Spectre.Console
```

`Program.cs`:

```csharp
using System.Globalization;
using Spectre.Console;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");

var nombre = LeerTexto("Tu nombre: ");
var ingreso = LeerMonto("Ingreso mensual: ");
var alquiler = LeerMonto("Alquiler: ");
var comida = LeerMonto("Comida: ");
var transporte = LeerMonto("Transporte: ");

var totalGastos = alquiler + comida + transporte;
var disponible = ingreso - totalGastos;
var porcentajeGastado = ingreso == 0 ? 0 : totalGastos / ingreso * 100;

var tabla = new Table();
tabla.AddColumn("Concepto");
tabla.AddColumn(new TableColumn("Monto").RightAligned());
tabla.AddRow("Ingreso", ingreso.ToString("N2"));
tabla.AddRow("Alquiler", alquiler.ToString("N2"));
tabla.AddRow("Comida", comida.ToString("N2"));
tabla.AddRow("Transporte", transporte.ToString("N2"));
tabla.AddRow("Total gastos", totalGastos.ToString("N2"));
tabla.AddRow("Disponible", disponible.ToString("N2"));
AnsiConsole.Write(tabla);

AnsiConsole.MarkupLine($"Gastaste el [bold]{porcentajeGastado:N1}%[/] de tu ingreso.");
if (disponible >= 0)
{
    AnsiConsole.MarkupLine($"[green]{Markup.Escape(nombre)}, te quedan {disponible:N2} este mes.[/]");
}
else
{
    AnsiConsole.MarkupLine($"[red]{Markup.Escape(nombre)}, te pasaste por {Math.Abs(disponible):N2}.[/]");
}

static string LeerTexto(string mensaje)
{
    Console.Write(mensaje);
    var valor = Console.ReadLine();
    return string.IsNullOrWhiteSpace(valor) ? "invitado" : valor.Trim();
}

static decimal LeerMonto(string mensaje)
{
    while (true)
    {
        Console.Write(mensaje);
        if (decimal.TryParse(Console.ReadLine(), out var monto) && monto >= 0)
        {
            return monto;
        }

        AnsiConsole.MarkupLine("[red]Ingresá un número mayor o igual a cero.[/]");
    }
}
```

El `while` de `LeerMonto` repite hasta recibir un valor válido; lo vas a profundizar en el módulo 2.

Para reproducir el paso 6, cambiá temporalmente `TryParse` por `decimal.Parse`, ejecutá la app y presioná Enter sin escribir nada. Vas a ver `System.FormatException` y un stack trace que señala la línea del parseo. Después volvé a `TryParse` y registrá el arreglo:

```bash
git add Program.cs
git commit -m "fix: validar montos con TryParse"
```

Verificación y Git:

```bash
dotnet build
dotnet run

git add .
git commit -m "feat: calcular presupuesto mensual"

git switch -c feature/porcentaje
git add Program.cs
git commit -m "feat: mostrar porcentaje gastado"
git switch main
git merge feature/porcentaje

gh repo create PresupuestoApp --public --source=. --remote=origin --push
git tag -a v1.0.0 -m "Presupuesto mensual"
git push --tags
```

Publicación:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

```powershell
.\bin\Release\net10.0\win-x64\publish\PresupuestoApp.exe
```

Ejemplo de ejecución:

```text
Tu nombre: Ana
Ingreso mensual: 1000000
Alquiler: 350000
Comida: 200000
Transporte: 50000
```

```text
┌──────────────┬──────────────┐
│ Concepto     │        Monto │
├──────────────┼──────────────┤
│ Ingreso      │ 1.000.000,00 │
│ Alquiler     │   350.000,00 │
│ Comida       │   200.000,00 │
│ Transporte   │    50.000,00 │
│ Total gastos │   600.000,00 │
│ Disponible   │   400.000,00 │
└──────────────┴──────────────┘
Gastaste el 60,0% de tu ingreso.
Ana, te quedan 400.000,00 este mes.
```

</details>

## Resumen

- El ciclo completo es crear, depender, codificar, depurar, versionar y publicar.
- Cada etapa tiene una verificación: build, run, test, commit y ejecutable.
- `TryParse` y las entradas inválidas son parte del trabajo, no un extra.
- Git guarda la historia; `publish` produce el artefacto que se entrega.

## Siguiente paso

Con el módulo 1 completo, seguí con [Variables, tipos y conversiones](/modulos/csharp-desde-cero/variables-tipos) para empezar a escribir programas cada vez más expresivos.
