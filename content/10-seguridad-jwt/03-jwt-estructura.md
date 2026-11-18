---
slug: jwt-estructura
title: "JWT: estructura y validación"
order: 3
estimatedMinutes: 18
summary: "Entendé qué contiene un JWT, por qué no está cifrado y cómo validarlo del lado del servidor sin cometer los errores clásicos."
quiz:
  - question: "¿Qué contiene la firma de un JWT?"
    options:
      - "La contraseña del usuario cifrada"
      - "Un valor calculado sobre header y payload con la clave del emisor"
      - "El listado de permisos en texto plano"
      - "La clave privada del servidor"
    answer: 1
    explanation: "La firma permite verificar integridad y origen. No cifra el contenido: el payload se puede leer."
  - question: "¿Qué significa que un JWT esté firmado pero no cifrado?"
    options:
      - "Que cualquiera puede leer el payload, pero no modificarlo sin invalidar la firma"
      - "Que nadie puede leer el payload"
      - "Que no necesita validación"
      - "Que la firma se puede omitir"
    answer: 0
    explanation: "El payload es Base64Url y legible. La firma garantiza que no fue alterado, no que sea secreto."
  - question: "¿Qué validaciones debe hacer el servidor al recibir un JWT?"
    options:
      - "Solo decodificar el payload"
      - "Firma, algoritmo permitido, expiración, emisor y audiencia"
      - "Solo comprobar que tenga tres partes"
      - "Comparar el token con una lista en el cliente"
    answer: 1
    explanation: "Validar la firma sin validar expiración, emisor o audiencia deja la puerta abierta a tokens vencidos o de otro sistema."
  - question: "¿Por qué el ataque `alg: none` es peligroso?"
    options:
      - "Porque el token queda más largo"
      - "Porque si el servidor acepta el algoritmo del token, un atacante puede omitir la firma"
      - "Porque cambia el formato Base64"
      - "Porque impide usar HTTPS"
    answer: 1
    explanation: "El servidor debe fijar los algoritmos aceptados. Si confía en el `alg` del token, acepta tokens sin firma."
---

# JWT: estructura y validación

## Objetivo

Entender la estructura de un JSON Web Token, saber qué se puede y qué no se debe guardar, y configurar la validación del servidor con los controles correctos.

## Requisitos previos

Haber leído [Autenticación vs. autorización](/modulos/seguridad-jwt/autenticacion-autorizacion) y [Hash de contraseñas](/modulos/seguridad-jwt/hash-contrasenas).

## Concepto

Un JWT tiene tres partes separadas por puntos:

```text
header.payload.signature
```

Cada parte es Base64Url. El **header** indica el tipo y el algoritmo:

```json
{ "alg": "HS256", "typ": "JWT" }
```

El **payload** contiene los *claims*: datos sobre el usuario y el token.

```json
{
  "sub": "7c1f5b1e-2a6d-4f0e-9a6e-1f5c7a2b9d10",
  "email": "ana@ejemplo.com",
  "role": "student",
  "iss": "https://api.ruta.net",
  "aud": "https://ruta.net",
  "exp": 1763323200,
  "iat": 1763319600
}
```

La **firma** se calcula sobre `header.payload` con una clave. Si alguien cambia un claim, la firma deja de coincidir.

Lo más importante: **un JWT no está cifrado**. Cualquiera puede decodificar el payload con una herramienta o con dos líneas de código. Nunca guardes contraseñas, números de tarjeta ni datos que no quieras que el cliente vea.

Los claims estándar más usados:

| Claim | Significado |
|---|---|
| `sub` | Identificador del sujeto |
| `iss` | Emisor del token |
| `aud` | Audiencia: para quién es válido |
| `exp` | Expiración (segundos desde epoch) |
| `iat` | Momento de emisión |
| `nbf` | No válido antes de |
| `jti` | Identificador único del token |

Un token firmado con `HS256` usa una clave simétrica compartida. Con `RS256` se firma con clave privada y se valida con la pública, útil cuando varios servicios validan sin poder firmar.

La validación del servidor debe verificar, como mínimo:

1. La firma con la clave correcta.
2. Que el algoritmo esté en la lista permitida (nunca confiar en el `alg` del token).
3. `exp` vigente y `nbf` si existe.
4. `iss` esperado.
5. `aud` esperada si el token es para una audiencia concreta.

Validar solo la firma es como revisar que una puerta tenga cerradura y no probar si la llave abre.

## Ejemplo mínimo

Generar y validar un token en una consola, con la clave en variable de entorno:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

var claveTexto = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? throw new InvalidOperationException("Falta JWT_KEY.");

var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveTexto));
var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, "usuario-42"),
    new Claim(JwtRegisteredClaimNames.Email, "ana@ejemplo.com"),
    new Claim(ClaimTypes.Role, "student")
};

var token = new JwtSecurityToken(
    issuer: "https://api.ruta.net",
    audience: "https://ruta.net",
    claims: claims,
    expires: DateTime.UtcNow.AddMinutes(15),
    signingCredentials: credenciales);

var jwt = new JwtSecurityTokenHandler().WriteToken(token);
Console.WriteLine(jwt);
```

Para validarlo:

```csharp
var parametros = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = "https://api.ruta.net",
    ValidateAudience = true,
    ValidAudience = "https://ruta.net",
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = clave,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromSeconds(30),
    ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
};

var handler = new JwtSecurityTokenHandler();
var principal = handler.ValidateToken(jwt, parametros, out var tokenValidado);

Console.WriteLine(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
```

```text
usuario-42
```

`ClockSkew` contempla relojes ligeramente desfasados. El valor por defecto de cinco minutos es generoso; treinta segundos suele alcanzar.

## Ejemplo aplicado

En ASP.NET Core, la validación se configura una vez y la hace el middleware:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/perfil", (ClaimsPrincipal user) =>
    TypedResults.Ok(new
    {
        id = user.FindFirstValue(ClaimTypes.NameIdentifier),
        email = user.FindFirstValue(ClaimTypes.Email)
    }))
.RequireAuthorization();

app.Run();
```

```bash
curl -i http://localhost:5137/perfil \
  -H "Authorization: Bearer <token>"
```

```text
HTTP/1.1 200 OK
{ "id": "usuario-42", "email": "ana@ejemplo.com" }
```

Sin token o con uno inválido, el middleware responde `401`. Con token válido pero sin el rol requerido, `403`.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Un atacante modifica el payload y funciona | No se valida la firma | Validar firma y algoritmo permitido |
| Tokens vencidos siguen aceptándose | `ValidateLifetime` desactivado | Activarlo y ajustar `ClockSkew` |
| Un token de otro sistema es aceptado | No se valida `iss` ni `aud` | Configurar emisor y audiencia esperados |
| El token se acepta con `alg: none` | Se confía en el header del token | Fijar `ValidAlgorithms` |
| Datos sensibles en el payload | Se creyó que el JWT estaba cifrado | No incluir secretos; usar un id opaco |
| La clave está en el repositorio | Se hardcodeó la clave | Variable de entorno o user-secrets |
| Tokens de larga duración | Se usó `exp` de días | Access token corto y refresh token aparte |

## Buenas prácticas

- Access tokens de 10 a 20 minutos; renovación con refresh token.
- Guardá solo identificadores y claims necesarios, nunca datos sensibles.
- Fijá emisor, audiencia y algoritmos permitidos en el servidor.
- Usá claves de al menos 256 bits para HS256.
- Rotá claves con un `kid` en el header si el sistema lo requiere.
- Enviá siempre el token por HTTPS.

## Ejercicio

Generá un JWT con `sub`, `email` y `role`, expiración de 15 minutos, emisor `https://api.ejemplo.com` y audiencia `https://ejemplo.com`. Luego validalo con un emisor incorrecto y comprobá que la validación falla.

<details>
<summary>Ver solución</summary>

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

var clave = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes("clave-de-desarrollo-de-32-bytes-minimo"));

var token = new JwtSecurityToken(
    issuer: "https://api.ejemplo.com",
    audience: "https://ejemplo.com",
    claims:
    [
        new Claim(JwtRegisteredClaimNames.Sub, "u-1"),
        new Claim(JwtRegisteredClaimNames.Email, "ana@ejemplo.com"),
        new Claim(ClaimTypes.Role, "student")
    ],
    expires: DateTime.UtcNow.AddMinutes(15),
    signingCredentials: new SigningCredentials(clave, SecurityAlgorithms.HmacSha256));

var jwt = new JwtSecurityTokenHandler().WriteToken(token);

var parametros = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = "https://otro-sistema.com",
    ValidateAudience = true,
    ValidAudience = "https://ejemplo.com",
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = clave,
    ValidateLifetime = true,
    ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
};

try
{
    new JwtSecurityTokenHandler().ValidateToken(jwt, parametros, out _);
    Console.WriteLine("Token aceptado");
}
catch (SecurityTokenInvalidIssuerException)
{
    Console.WriteLine("Emisor inválido: token rechazado");
}
```

```text
Emisor inválido: token rechazado
```

La firma era correcta, pero el token no pertenecía al emisor configurado. Validar solo la firma habría aceptado un token de otro sistema.

</details>

## Resumen

- Un JWT tiene header, payload y firma; el payload es legible.
- La firma garantiza integridad, no confidencialidad.
- El servidor valida firma, algoritmo, expiración, emisor y audiencia.
- Los access tokens son cortos y se renuevan con refresh tokens.

## Siguiente paso

Un access token corto obliga a renovarlo con frecuencia. En [Refresh tokens](/modulos/seguridad-jwt/refresh-tokens) vas a diseñar esa renovación sin guardar credenciales de larga duración en el cliente.
