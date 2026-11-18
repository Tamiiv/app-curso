---
slug: refresh-tokens
title: "Refresh tokens"
order: 4
estimatedMinutes: 15
summary: "Renová access tokens sin pedir la contraseña de nuevo, con rotación, revocación y almacenamiento seguro del refresh token."
quiz:
  - question: "¿Para qué sirve un refresh token?"
    options:
      - "Para reemplazar la contraseña del usuario"
      - "Para obtener un nuevo access token sin volver a enviar las credenciales"
      - "Para firmar el access token"
      - "Para cifrar la base de datos"
    answer: 1
    explanation: "El refresh token es una credencial de larga duración que permite emitir access tokens cortos sin pedir login otra vez."
  - question: "¿Qué es la rotación de refresh tokens?"
    options:
      - "Cambiar el algoritmo de firma cada hora"
      - "Emitir un refresh token nuevo en cada renovación e invalidar el anterior"
      - "Guardar el token en el navegador"
      - "Eliminar la expiración"
    answer: 1
    explanation: "La rotación limita el daño de un token robado: si se reutiliza uno viejo, se detecta y se revoca la familia."
  - question: "¿Dónde conviene guardar un refresh token en una aplicación web?"
    options:
      - "En localStorage, para que JavaScript lo lea"
      - "En una cookie HttpOnly, Secure y SameSite"
      - "En la URL"
      - "En un comentario del HTML"
    answer: 1
    explanation: "Una cookie HttpOnly no es accesible desde JavaScript y reduce el impacto de XSS. Secure y SameSite agregan protección."
  - question: "¿Qué debe hacer el servidor si detecta un refresh token revocado en uso?"
    options:
      - "Emitir otro token sin revisar"
      - "Rechazar la petición y revocar toda la familia de tokens de esa sesión"
      - "Ignorarlo"
      - "Cambiar la contraseña automáticamente"
    answer: 1
    explanation: "La reutilización de un token rotado es señal de robo. Se invalida la familia completa para cortar el acceso."
---

# Refresh tokens

## Objetivo

Diseñar un flujo de renovación con access tokens cortos y refresh tokens rotados, revocables y almacenados de forma segura.

## Requisitos previos

Haber leído [JWT: estructura y validación](/modulos/seguridad-jwt/jwt-estructura). Necesitás manejar autenticación con `JwtBearer` y persistencia.

## Concepto

Un access token corto limita el daño si se filtra, pero obliga a renovarlo. Pedir la contraseña cada quince minutos es inviable. El refresh token resuelve eso: es una credencial de larga duración que solo sirve para obtener un access token nuevo.

El flujo básico:

1. El usuario inicia sesión con email y contraseña.
2. El servidor devuelve un access token (15 minutos) y un refresh token (días o semanas).
3. Cuando el access token vence, el cliente llama a `/auth/refresh` con el refresh token.
4. El servidor valida el refresh token, lo invalida y emite un par nuevo.
5. Si el refresh token no es válido, se cierra la sesión.

Reglas de diseño que evitan problemas:

- **El refresh token no es un JWT con datos.** Es un valor opaco, aleatorio y suficientemente largo. Se guarda su hash en la base, no el valor.
- **Se rota en cada uso.** Se emite uno nuevo y se marca el anterior como usado o revocado.
- **Se puede revocar.** Cerrar sesión, cambiar contraseña o detectar abuso invalida los tokens activos.
- **Tiene expiración absoluta y, opcionalmente, inactividad.** Un token no vive para siempre.
- **Se transporta por un canal seguro.** En web, cookie `HttpOnly`, `Secure` y `SameSite`; nunca `localStorage` si se puede evitar.

La rotación con detección de reutilización es la parte que muchos olvidan. Si un atacante roba el refresh token y lo usa, y luego el cliente legítimo usa el original, el servidor detecta dos usos del mismo token. La respuesta correcta es revocar toda la familia de esa sesión.

## Ejemplo mínimo

El modelo de persistencia:

```csharp
public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string UserId { get; init; }
    public required string TokenHash { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid FamilyId { get; init; } = Guid.NewGuid();
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
```

El servicio genera un token aleatorio y guarda solo el hash:

```csharp
using System.Security.Cryptography;
using System.Text;

public sealed class RefreshTokenService(AppDbContext db)
{
    public (string Token, RefreshToken Registro) Crear(string userId, Guid? familyId = null)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var registro = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hashear(token),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            FamilyId = familyId ?? Guid.NewGuid()
        };

        db.RefreshTokens.Add(registro);
        return (token, registro);
    }

    public static string Hashear(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
```

```csharp
var servicio = new RefreshTokenService(db);
var (token, registro) = servicio.Crear("usuario-42");

Console.WriteLine(token.Length);
Console.WriteLine(registro.TokenHash.Length);
```

```text
88
64
```

El token plano se devuelve al cliente una sola vez. La base guarda el hash: si se filtra, no se puede reutilizar directamente.

## Ejemplo aplicado

Endpoint de refresh con rotación y detección de reutilización:

```csharp
app.MapPost("/auth/refresh", async (
    RefreshRequest request,
    AppDbContext db,
    ITokenService tokens,
    TimeProvider reloj) =>
{
    var hash = RefreshTokenService.Hashear(request.RefreshToken);
    var actual = await db.RefreshTokens
        .SingleOrDefaultAsync(t => t.TokenHash == hash);

    if (actual is null)
    {
        return TypedResults.Unauthorized();
    }

    if (actual.RevokedAt is not null)
    {
        // Token ya usado: posible robo. Se revoca la familia completa.
        var familia = db.RefreshTokens.Where(t => t.FamilyId == actual.FamilyId);
        foreach (var token in familia)
        {
            token.RevokedAt ??= reloj.GetUtcNow();
        }

        await db.SaveChangesAsync();
        return TypedResults.Unauthorized();
    }

    if (actual.ExpiresAt <= reloj.GetUtcNow())
    {
        return TypedResults.Unauthorized();
    }

    actual.RevokedAt = reloj.GetUtcNow();

    var (nuevoToken, nuevoRegistro) = new RefreshTokenService(db)
        .Crear(actual.UserId, actual.FamilyId);

    await db.SaveChangesAsync();

    var accessToken = tokens.CrearAccessToken(actual.UserId);

    return TypedResults.Ok(new
    {
        accessToken,
        refreshToken = nuevoToken,
        expiresIn = 900
    });
});

public sealed record RefreshRequest(string RefreshToken);
```

```bash
curl -i -X POST http://localhost:5137/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<token-del-cliente>"}'
```

```text
HTTP/1.1 200 OK
{"accessToken":"eyJ...","refreshToken":"abc...","expiresIn":900}
```

La segunda llamada con el mismo token devuelve `401` y revoca la familia. El cliente debe volver a iniciar sesión.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El refresh token se guarda en claro | Se persiste el valor original | Guardar solo un hash |
| Un token robado sigue funcionando | No hay rotación ni revocación | Rotar en cada uso y detectar reutilización |
| El refresh token vive para siempre | Falta expiración absoluta | Fijar una fecha máxima y revocar al cerrar sesión |
| El token se guarda en `localStorage` | Comodidad del cliente | Usar cookie `HttpOnly` en web |
| El endpoint de refresh no limita intentos | Fuerza bruta | Rate limiting y bloqueo por usuario |
| Se devuelve el mismo refresh token | Se simplificó la rotación | Emitir uno nuevo e invalidar el anterior |
| El logout no revoca nada | Solo se borra el token del cliente | Revocar el registro en el servidor |

## Buenas prácticas

- Refresh tokens opacos, largos y aleatorios; nunca JWT con datos.
- Guardar hash, no el valor plano.
- Rotar en cada renovación y revocar la familia ante reutilización.
- Expiración absoluta de días o semanas, según el riesgo.
- Cookie `HttpOnly` + `Secure` + `SameSite` en aplicaciones web.
- Revocar en logout, cambio de contraseña y eventos de seguridad.

## Ejercicio

Implementá `POST /auth/logout` que reciba el refresh token, lo revoque y devuelva `204`. Luego verificá que un refresh posterior con ese token devuelva `401`.

<details>
<summary>Ver solución</summary>

```csharp
app.MapPost("/auth/logout", async (RefreshRequest request, AppDbContext db) =>
{
    var hash = RefreshTokenService.Hashear(request.RefreshToken);
    var actual = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash);

    if (actual is null)
    {
        return TypedResults.NoContent();
    }

    actual.RevokedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();

    return TypedResults.NoContent();
});
```

```bash
curl -i -X POST http://localhost:5137/auth/logout \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<token>"}'

curl -i -X POST http://localhost:5137/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<mismo-token>"}'
```

```text
HTTP/1.1 204 No Content
HTTP/1.1 401 Unauthorized
```

El logout devuelve `204` incluso si el token no existía: no revela si era válido y la operación es idempotente.

</details>

## Resumen

- El refresh token permite renovar sin pedir credenciales.
- Se guarda hasheado, se rota y se revoca.
- La reutilización de un token rotado revoca la familia completa.
- En web se transporta en cookie `HttpOnly`, no en `localStorage`.

## Siguiente paso

Cuando la aplicación necesita delegar el login a Google, Microsoft o GitHub, entra en juego un estándar. En [OAuth 2.0 y OpenID Connect](/modulos/seguridad-jwt/oauth-oidc) vas a entender los flujos y cuándo usar cada uno.
