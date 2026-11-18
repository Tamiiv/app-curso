---
slug: secretos-configuracion
title: "Secretos y configuración"
order: 7
estimatedMinutes: 15
summary: "Separá configuración de código, manejá claves y cadenas de conexión sin filtrarlas en el repositorio y rotá secretos sin redeploy manual."
quiz:
  - question: "¿Dónde conviene guardar la clave de firma de JWT en desarrollo local?"
    options:
      - "En appsettings.json versionado en el repositorio"
      - "En user-secrets o en una variable de entorno"
      - "En un comentario dentro de Program.cs"
      - "En el README del proyecto"
    answer: 1
    explanation: "user-secrets guarda valores fuera de la carpeta del proyecto y las variables de entorno no se versionan. appsettings.json sí se commitea."
  - question: "¿Qué archivo de configuración debería quedar versionado en el repositorio?"
    options:
      - "appsettings.Production.json con la clave real"
      - "appsettings.json con valores por defecto no sensibles"
      - ".env con todas las contraseñas"
      - "El archivo de user-secrets"
    answer: 1
    explanation: "appsettings.json documenta las claves y valores no sensibles. Los valores reales de cada entorno llegan por variables, secretos o un gestor externo."
  - question: "¿Qué indica que un secreto quedó expuesto?"
    options:
      - "Que el build falla"
      - "Que apareció en el historial de Git o en un log, incluso si ya se borró del archivo actual"
      - "Que el proyecto tiene tests"
      - "Que la app corre en contenedor"
    answer: 1
    explanation: "El historial conserva los commits. Un secreto commiteado una vez debe rotarse, no solo eliminarse del archivo actual."
  - question: "¿Qué ventaja ofrece un gestor de secretos en la nube frente a variables de entorno sueltas?"
    options:
      - "Compila más rápido"
      - "Centraliza el acceso, la auditoría y la rotación de secretos"
      - "Elimina la necesidad de HTTPS"
      - "Reemplaza a la autenticación"
    answer: 1
    explanation: "Un gestor como Azure Key Vault, AWS Secrets Manager o Vault permite controlar quién lee cada secreto, auditar accesos y rotarlos."
---

# Secretos y configuración

## Objetivo

Separar la configuración del código, manejar secretos por entorno sin filtrarlos en el repositorio y responder correctamente cuando una clave se expone.

## Requisitos previos

Haber leído [OWASP Top 10 en .NET](/modulos/seguridad-jwt/owasp-top-10). Necesitás manejar `IConfiguration`, el archivo `appsettings.json` y variables de entorno.

## Concepto

La configuración es todo lo que cambia entre entornos: cadenas de conexión, URLs de servicios, claves de firma, niveles de log. El código es lo que no cambia. Mezclarlos produce dos problemas: el mismo binario no sirve para dos entornos y los secretos terminan en el repositorio.

ASP.NET Core construye la configuración en capas. Cada proveedor puede sobrescribir al anterior:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User secrets (solo desarrollo)
4. Variables de entorno
5. Argumentos de línea de comandos

La última fuente que define una clave gana. Ese orden permite tener valores por defecto en el repositorio y sobrescribirlos en cada entorno sin tocar código.

| Tipo de valor | Dónde va |
|---|---|
| Valor por defecto no sensible | `appsettings.json` |
| Valor por entorno | `appsettings.{Environment}.json` o variable |
| Secreto de desarrollo | `dotnet user-secrets` |
| Secreto de producción | Variables del proveedor o gestor de secretos |

Un secreto commiteado no se arregla borrando la línea. Sigue en el historial de Git, en los forks y en las cachés. La respuesta correcta es **rotarlo**: generar uno nuevo, invalidar el anterior y recién después limpiar el archivo.

Los logs merecen la misma atención. Un `LogInformation` que imprime el objeto de configuración completo puede volcar la cadena de conexión. Los logs estructurados con plantillas y propiedades explícitas son más seguros que `ToString()` de objetos.

## Ejemplo mínimo

Acceso tipado a configuración con validación al arrancar:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

app.MapGet("/config", (IOptions<JwtOptions> options) =>
    TypedResults.Ok(new
    {
        emisor = options.Value.Issuer,
        expiracionMinutos = options.Value.ExpirationMinutes
    }));

app.Run();

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required, MinLength(32)]
    public string Key { get; init; } = string.Empty;

    [Range(1, 120)]
    public int ExpirationMinutes { get; init; } = 15;
}
```

```json
{
  "Jwt": {
    "Issuer": "https://api.ruta.net",
    "Audience": "https://ruta.net",
    "ExpirationMinutes": 15
  }
}
```

`ValidateOnStart` hace que la aplicación no arranque si falta la clave o es demasiado corta. Es mejor fallar al inicio que en la primera petición de un usuario.

Para desarrollo local, la clave se carga sin tocar el archivo:

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<clave-de-32-bytes-o-mas>"
```

```text
The secret for 'app-curso' has been saved.
```

El archivo de user-secrets vive en el perfil del usuario, fuera del repositorio. `dotnet user-secrets list` permite revisar qué hay cargado.

## Ejemplo aplicado

Configuración por entorno en un pipeline de deploy:

```yaml
# .github/workflows/deploy.yml (fragmento)
jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4

      - name: Configurar .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Publicar
        run: dotnet publish src/AppCurso.Web -c Release -o out

      - name: Deploy
        env:
          ConnectionStrings__Default: ${{ secrets.DB_CONNECTION }}
          Jwt__Key: ${{ secrets.JWT_KEY }}
        run: ./deploy.sh
```

ASP.NET Core traduce `ConnectionStrings__Default` a `ConnectionStrings:Default`: el doble guion bajo reemplaza a los dos puntos, porque muchos shells no permiten `:` en nombres de variables.

Buenas prácticas del pipeline:

- Los secretos se guardan en el gestor del repositorio o del proveedor, nunca en el YAML.
- Cada entorno tiene su propio conjunto de valores.
- El log del pipeline no imprime las variables de entorno completas.
- Un secreto que se filtra en un log de CI se rota igual que uno commiteado.

Para producción de mayor escala, un gestor dedicado evita repartir secretos en cada servicio:

```csharp
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://{builder.Configuration["KeyVault:Name"]}.vault.azure.net/"),
        new DefaultAzureCredential());
}
```

La identidad del servicio accede al vault; nadie copia claves a mano. La rotación se hace en el vault y la aplicación toma el valor nuevo al reiniciar o al refrescar la configuración.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La clave aparece en el repositorio | Se escribió en `appsettings.json` | Moverla a user-secrets o variables y rotarla |
| La app arranca sin configuración crítica | No se valida al inicio | `ValidateOnStart` y `ValidateDataAnnotations` |
| Distintos entornos usan la misma base | Se copió el mismo archivo | Configuración por entorno o por proveedor |
| El secreto se imprimió en un log | Se logueó el objeto completo | Loguear propiedades explícitas y no sensibles |
| Se borró el secreto del archivo y se siguió usando | Quedó en el historial y en cachés | Rotar, invalidar y luego limpiar |
| Todos los servicios comparten una clave maestra | Falta separación de identidades | Una credencial por servicio y permisos mínimos |

## Buenas prácticas

- Ningún secreto en el repositorio, ni siquiera en un commit que se piensa revertir.
- Validá la configuración al arrancar, no en el primer uso.
- Una credencial por servicio, con el permiso mínimo necesario.
- Rotá claves de forma periódica y documentá el procedimiento.
- Revisá los logs para asegurarte de que no exponen valores sensibles.
- Versioná un `.env.example` o `appsettings.example.json` con claves vacías y documentación.

## Ejercicio

Tomá una API con la clave de JWT en `appsettings.json`. Movela a user-secrets para desarrollo, configurala por variable de entorno para producción y agregá validación al arranque para que falle si la clave mide menos de 32 caracteres.

<details>
<summary>Ver solución</summary>

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "clave-de-desarrollo-de-32-bytes-minimo"
dotnet user-secrets list
```

```text
Jwt:Key = clave-de-desarrollo-de-32-bytes-minimo
```

```csharp
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException("Falta la sección Jwt.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true
        };
    });
```

```powershell
$env:Jwt__Key = "clave-de-produccion-de-32-bytes-o-mas"
dotnet run --project src/AppCurso.Web
```

Si la clave falta o es corta, la aplicación no arranca y el mensaje indica qué opción falló. El archivo versionado ya no contiene secretos.

</details>

## Resumen

- La configuración cambia por entorno; el código no.
- El orden de proveedores permite valores por defecto y sobrescrituras seguras.
- Los secretos viven fuera del repositorio: user-secrets, variables o un gestor dedicado.
- Un secreto expuesto se rota; no alcanza con borrar el archivo.
- Validar al arranque evita fallos tardíos y difíciles de diagnosticar.

## Siguiente paso

Con los secretos bajo control, falta proteger el canal y el navegador. En [HTTPS, CORS y CSRF](/modulos/seguridad-jwt/https-cors-csrf) vas a configurar transporte seguro y las defensas que evitan peticiones cruzadas maliciosas.
