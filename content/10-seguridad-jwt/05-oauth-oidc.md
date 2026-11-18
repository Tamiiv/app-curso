---
slug: oauth-oidc
title: "OAuth 2.0 y OpenID Connect"
order: 5
estimatedMinutes: 15
summary: "Distinguí autorización de identidad, conocé los flujos vigentes de OAuth 2.0 y usá OpenID Connect para delegar el login sin manejar contraseñas ajenas."
quiz:
  - question: "¿Qué agrega OpenID Connect sobre OAuth 2.0?"
    options:
      - "Un mecanismo de cifrado de la base de datos"
      - "Una capa de identidad con un ID token que describe al usuario autenticado"
      - "Un reemplazo de HTTPS"
      - "Un formato de compresión"
    answer: 1
    explanation: "OAuth 2.0 delega autorización. OIDC agrega autenticación estandarizada con un ID token y el endpoint de userinfo."
  - question: "¿Qué flujo se recomienda para una aplicación web con servidor?"
    options:
      - "Implicit"
      - "Authorization Code con PKCE"
      - "Resource Owner Password Credentials"
      - "Client Credentials con contraseña de usuario"
    answer: 1
    explanation: "Authorization Code con PKCE es el flujo recomendado. Implicit y password están obsoletos para login de usuarios."
  - question: "¿Cuándo se usa el flujo Client Credentials?"
    options:
      - "Cuando un usuario inicia sesión desde el navegador"
      - "Cuando un servicio se autentica contra otro sin usuario final"
      - "Cuando se quiere evitar el refresh token"
      - "Cuando la API es pública"
    answer: 1
    explanation: "Client Credentials es para comunicación máquina a máquina, sin identidad de usuario."
  - question: "¿Qué es el estado (`state`) en el flujo de autorización?"
    options:
      - "Un valor para cachear respuestas"
      - "Un valor aleatorio que protege contra CSRF y correlaciona la respuesta"
      - "La contraseña del cliente"
      - "El identificador del usuario"
    answer: 1
    explanation: "El cliente genera `state`, lo guarda y verifica que vuelva igual. Sin él, un atacante puede inyectar un código ajeno."
---

# OAuth 2.0 y OpenID Connect

## Objetivo

Diferenciar autorización de identidad, elegir el flujo OAuth correcto y entender cómo OpenID Connect delega el login sin que la aplicación maneje contraseñas de terceros.

## Requisitos previos

Haber leído [Refresh tokens](/modulos/seguridad-jwt/refresh-tokens). Necesitás manejar el flujo de login y el concepto de token.

## Concepto

OAuth 2.0 es un **marco de autorización delegada**. Permite que una aplicación acceda a recursos de un usuario en otro servicio sin conocer su contraseña. El ejemplo clásico es "permitir que esta app lea tus repositorios de GitHub".

OpenID Connect (OIDC) es una **capa de identidad** sobre OAuth 2.0. Agrega un `ID token` (un JWT) que dice quién es el usuario y un endpoint `userinfo`. Cuando ves "Iniciar sesión con Google", el estándar detrás es OIDC.

Roles del ecosistema:

| Rol | Quién es |
|---|---|
| Resource Owner | El usuario |
| Client | Tu aplicación |
| Authorization Server | Google, Microsoft, tu IdP |
| Resource Server | La API que expone datos |

Los flujos que importan hoy:

- **Authorization Code + PKCE:** el recomendado para aplicaciones web y móviles. El usuario se autentica en el proveedor, vuelve con un código y el backend lo canjea por tokens. PKCE evita que un código interceptado sirva a otro cliente.
- **Client Credentials:** para servicios sin usuario final. El servicio se autentica con su client id y secret.
- **Device Code:** para dispositivos sin navegador, como una TV.

Los flujos `Implicit` y `Password` se consideran obsoletos para login de usuarios. Implicit expone tokens en la URL; Password obliga a la aplicación a manipular credenciales.

En OIDC, el `ID token` se valida como cualquier JWT: firma, emisor, audiencia y expiración. No se usa para llamar a la API; para eso está el access token.

## Ejemplo mínimo

Configuración de OIDC en ASP.NET Core con un proveedor externo:

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = builder.Configuration["Oidc:Authority"];
        options.ClientId = builder.Configuration["Oidc:ClientId"];
        options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.GetClaimsFromUserInfoEndpoint = true;
        options.TokenValidationParameters.NameClaimType = "name";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Inicio");
app.MapGet("/cuenta", (ClaimsPrincipal user) => $"Hola {user.Identity?.Name}")
   .RequireAuthorization();

app.Run();
```

```text
GET /cuenta sin sesión → redirige al proveedor
GET /cuenta con sesión → "Hola Ana"
```

`Authority` apunta al emisor OIDC. El middleware descubre los endpoints, valida el `ID token` y crea la cookie local.

## Ejemplo aplicado

En una API que solo valida tokens emitidos por un proveedor, no hace falta el flujo interactivo: alcanza con validar el JWT.

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Oidc:Authority"];
        options.Audience = builder.Configuration["Oidc:ApiAudience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidAlgorithms = ["RS256"]
        };
    });
```

El proveedor publica sus claves en `/.well-known/openid-configuration` y `jwks_uri`. El middleware las descarga y las rota cuando corresponde.

Puntos que se verifican en un code review de integración OIDC:

- `state` generado y validado.
- PKCE activo en clientes públicos.
- Scopes mínimos: no pedir `offline_access` si no se usan refresh tokens.
- Redirección registrada exactamente, sin comodines.
- `aud` y `iss` validados.
- Sesión local con expiración coherente con el proveedor.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Se usa `Implicit` para una app nueva | Costumbre vieja | Authorization Code + PKCE |
| El `ID token` se envía a la API como credencial | Se confundió identidad con autorización | Usar el access token para la API |
| No se valida el `state` | Se copió un ejemplo incompleto | Generar, guardar y comparar `state` |
| Se piden todos los scopes | "Por si acaso" | Pedir solo lo necesario y justificarlo |
| El client secret está en el frontend | Se trató un cliente público como confidencial | Los clientes públicos no llevan secret; usan PKCE |
| La redirección acepta cualquier URL | Configuración laxa | Registrar URLs exactas en el proveedor |

## Buenas prácticas

- Usá OIDC para login y OAuth para acceso delegado.
- Preferí Authorization Code con PKCE en web y móvil.
- Validá `iss`, `aud`, firma y expiración del `ID token`.
- Manejá el `state` y exigí HTTPS en todas las redirecciones.
- No guardes tokens de terceros en el cliente si el backend puede custodiarlos.

## Ejercicio

Explicá qué flujo usarías en cada caso y por qué:

1. Una web con backend que permite login con Google.
2. Una app móvil que consume una API propia con login de GitHub.
3. Un job que sincroniza datos entre dos servicios sin usuario.

<details>
<summary>Ver solución</summary>

```text
1. Authorization Code + PKCE con OpenID Connect.
   El backend custodia los tokens y crea una cookie de sesión local.

2. Authorization Code + PKCE desde la app móvil, con un redirect URI propio.
   El access token se envía a la API en la cabecera Authorization.

3. Client Credentials.
   No hay usuario: el servicio se autentica con su client id y secret
   y recibe un token con los scopes necesarios.
```

En los tres casos, el flujo elegido evita exponer credenciales del usuario y limita el alcance de los tokens.

</details>

## Resumen

- OAuth 2.0 delega autorización; OIDC agrega identidad.
- Authorization Code + PKCE es el flujo recomendado para usuarios.
- Client Credentials es para servicios sin usuario final.
- `state`, PKCE, `iss` y `aud` son controles obligatorios, no opcionales.

## Siguiente paso

Con los estándares claros, toca revisar los errores más frecuentes. En [OWASP Top 10 en .NET](/modulos/seguridad-jwt/owasp-top-10) vas a mapear riesgos concretos a defensas del framework.
