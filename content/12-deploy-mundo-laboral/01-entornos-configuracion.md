---
slug: entornos-configuracion
title: "Entornos y configuración"
order: 1
estimatedMinutes: 12
summary: "Separá Development, Staging y Production con appsettings por entorno, variables de entorno y secretos fuera del repositorio."
quiz:
  - question: "¿Qué variable decide el entorno activo en ASP.NET Core?"
    options:
      - "DOTNET_ENVIRONMENT_ONLY"
      - "ASPNETCORE_ENVIRONMENT"
      - "APP_ENV_NAME"
      - "DOTNET_RUN_MODE"
    answer: 1
    explanation: "ASPNETCORE_ENVIRONMENT define el nombre del entorno y selecciona appsettings.{Environment}.json. Si no está definida, se usa Production."
  - question: "¿Qué proveedor de configuración tiene prioridad más alta por defecto?"
    options:
      - "appsettings.json"
      - "appsettings.{Environment}.json"
      - "Variables de entorno"
      - "Argumentos de línea de comandos"
    answer: 3
    explanation: "Los proveedores se agregan en orden y el último gana. Los argumentos de línea de comandos se agregan después de las variables de entorno."
  - question: "¿Para qué sirve el proveedor de User Secrets?"
    options:
      - "Para cifrar la base de datos en producción"
      - "Para guardar secretos locales fuera del árbol del proyecto"
      - "Para publicar secretos en el repositorio"
      - "Para reemplazar appsettings.json en producción"
    answer: 1
    explanation: "User Secrets guarda valores en el perfil del usuario durante el desarrollo, así no se versionan junto al código."
  - question: "¿Qué filename de configuración se carga solo en el entorno Staging?"
    options:
      - "appsettings.Staging.json"
      - "staging.appsettings.json"
      - "appsettings.json.staging"
      - "config.Staging.yaml"
    answer: 0
    explanation: "La convención es appsettings.{Environment}.json, donde {Environment} coincide con el valor de ASPNETCORE_ENVIRONMENT."
---

# Entornos y configuración

## Objetivo

Separar la configuración de desarrollo, staging y producción sin tocar código ni versionar secretos, y validar las opciones al arrancar para fallar temprano.

## Requisitos previos

Necesitás una API con `Program.cs` y al menos un `appsettings.json`. Si terminaste el [ejercicio integrador de arquitectura](/modulos/arquitectura/ejercicio-integrador), ya tenés una aplicación con capas y configuración básica que podés reutilizar.

## Concepto

Una aplicación no se comporta igual en tu máquina que en un servidor. Cambian la cadena de conexión, la URL del frontend, el nivel de logs y las claves de terceros. Si esos valores viven en el código, cada cambio exige recompilar y cada secreto queda en el historial de Git.

ASP.NET Core resuelve esto con un sistema de configuración en capas. Cada proveedor aporta pares clave-valor y el último que se registra gana. El orden por defecto es: `appsettings.json`, `appsettings.{Environment}.json`, secretos del usuario, variables de entorno y argumentos de línea de comandos.

El entorno activo sale de `ASPNETCORE_ENVIRONMENT`. Los valores habituales son `Development`, `Staging` y `Production`. Si la variable no existe, el valor es `Production`, un default seguro para que nadie despliegue por accidente con datos de prueba.

La convención de nombres importa. Un archivo `appsettings.Staging.json` solo se carga cuando el entorno es exactamente `Staging`. En producción podés sobreescribir cualquier valor con variables de entorno usando doble guion bajo como separador de secciones: `ConnectionStrings__Default` equivale a `ConnectionStrings:Default`.

Para el código, no leas `IConfiguration` en todas partes. Definí una clase de opciones, enlazala con `IOptions<T>` y validala con Data Annotations. Así el resto del sistema recibe un objeto tipado y el arranque falla si falta un valor obligatorio.

| Entorno | Uso | Datos |
|---|---|---|
| `Development` | Tu máquina | Locales, logs detallados |
| `Staging` | Pruebas previas | Copia controlada de producción |
| `Production` | Usuarios reales | Reales, logs acotados |

## Ejemplo mínimo

```json
// appsettings.json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "Api": {
    "Nombre": "Cursos API",
    "MaximoIntentos": 3
  }
}
```

```json
// appsettings.Production.json
{
  "Logging": { "LogLevel": { "Default": "Warning" } },
  "Api": { "MaximoIntentos": 5 }
}
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection("Api"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

app.MapGet("/config", (IOptions<ApiOptions> options) =>
{
    ApiOptions api = options.Value;
    return new { api.Nombre, api.MaximoIntentos };
});

app.Run();

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, 10)]
    public int MaximoIntentos { get; set; } = 3;
}
```

```text
# Development
{"nombre":"Cursos API","maximoIntentos":3}

# Production (con ASPNETCORE_ENVIRONMENT=Production)
{"nombre":"Cursos API","maximoIntentos":5}
```

El mismo binario responde distinto según el entorno porque la configuración se resuelve en tiempo de ejecución. `ValidateOnStart` convierte un error de configuración en un fallo de arranque, no en una excepción rara la primera vez que alguien usa el endpoint.

## Ejemplo aplicado

Una API necesita una cadena de conexión y una clave de firma. En desarrollo usa User Secrets; en el servidor, variables de entorno inyectadas por la plataforma.

```bash
# Solo desarrollo: guarda fuera del repositorio
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Database=cursos;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Clave" "clave-local-de-al-menos-32-caracteres"
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => o.Clave.Length >= 32,
        "La clave JWT debe tener al menos 32 caracteres.")
    .ValidateOnStart();

string? cadena = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(cadena))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings:Default. Configurala por variables de entorno.");
}

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cadena));

var app = builder.Build();
app.MapGet("/salud", () => Results.Ok(new { estado = "ok" }));
app.Run();

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Emisor { get; set; } = "cursos-api";
    public string Clave { get; set; } = string.Empty;
}
```

```text
# En el servidor
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=Server=...;Database=...;User Id=...;Password=...
Jwt__Clave=<valor inyectado por el gestor de secretos>
```

Notá que el código no distingue entornos con `if`. Solo lee claves; el entorno decide qué archivo y qué variables aportan los valores. Ese es el objetivo: una sola rama de código, muchas configuraciones.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La app usa `Production` sin querer | Falta `ASPNETCORE_ENVIRONMENT` en la máquina | Definirla en `launchSettings.json` o en el shell |
| Los cambios de `appsettings.Production.json` no aparecen | El entorno activo no es `Production` | Verificar la variable y el nombre exacto del archivo |
| Las variables de entorno no sobreescriben nada | Se usó `:` en lugar de `__` | Usar doble guion bajo: `Api__MaximoIntentos` |
| La app arranca con configuración inválida | No hay validación de opciones | `.ValidateDataAnnotations().ValidateOnStart()` |
| Un secreto quedó en Git | Se guardó en `appsettings.json` o en el repo | Rotar el secreto y moverlo a variables o al gestor de la plataforma |
| `IOptions` devuelve valores viejos | Se esperaba recarga en caliente | Usar `IOptionsMonitor<T>` si necesitás cambios en caliente |

## Buenas prácticas

- Versioná `appsettings.json` y `appsettings.Development.json`; nunca archivos con secretos reales.
- Tratá los secretos como inyectables: variables de entorno en el servidor y User Secrets en desarrollo.
- Validá las opciones al arrancar con `ValidateOnStart` y mensajes claros.
- Usá `IOptions<T>` para valores fijos y `IOptionsMonitor<T>` solo cuando la recarga sea un requisito real.
- Documentá en el README qué variables exige la aplicación y qué pasa si faltan.

## Ejercicio

Configurá una sección `Correo` con `Remitente`, `Host` y `Puerto`. En desarrollo debe venir de `appsettings.Development.json`; en producción, de variables de entorno. El arranque debe fallar si `Remitente` está vacío o el puerto está fuera del rango 1-65535. Mostrá los valores efectivos en `/config/correo`.

<details>
<summary>Ver solución</summary>

```json
// appsettings.Development.json
{
  "Correo": {
    "Remitente": "no-responder@cursos.local",
    "Host": "localhost",
    "Puerto": 1025
  }
}
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<CorreoOptions>()
    .Bind(builder.Configuration.GetSection(CorreoOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

app.MapGet("/config/correo", (IOptions<CorreoOptions> opciones) =>
{
    CorreoOptions correo = opciones.Value;
    return Results.Ok(new
    {
        correo.Remitente,
        correo.Host,
        correo.Puerto,
        Entorno = app.Environment.EnvironmentName
    });
});

app.Run();

public sealed class CorreoOptions
{
    public const string SectionName = "Correo";

    [Required, EmailAddress]
    public string Remitente { get; set; } = string.Empty;

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Puerto { get; set; } = 587;
}
```

```bash
# Producción: sin tocar archivos
Correo__Remitente=no-responder@misitio.com
Correo__Host=smtp.misitio.com
Correo__Puerto=587
```

```text
{"remitente":"no-responder@misitio.com","host":"smtp.misitio.com","puerto":587,"entorno":"Production"}
```

Checklist de verificación:

- Con `Remitente` vacío la app no arranca y el mensaje nombra la propiedad.
- Con `Puerto=70000` falla la validación antes de aceptar tráfico.
- Cambiar `ASPNETCORE_ENVIRONMENT` cambia los valores sin recompilar.
- Ningún secreto aparece en `git status` ni en el historial.

</details>

## Resumen

- La configuración se arma por capas y el último proveedor gana.
- `ASPNETCORE_ENVIRONMENT` selecciona `appsettings.{Environment}.json` y por defecto vale `Production`.
- Las variables de entorno usan `__` para representar secciones anidadas.
- User Secrets mantiene los valores locales fuera del repositorio.
- Validar opciones al arrancar evita fallos tardíos y difíciles de diagnosticar.

## Siguiente paso

Con la configuración bajo control, el próximo desafío es que la aplicación se ejecute igual en cualquier máquina. En la siguiente lección vas a empaquetarla con [Docker](/modulos/deploy-mundo-laboral/docker).
