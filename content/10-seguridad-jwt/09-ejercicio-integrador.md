---
slug: ejercicio-integrador
title: "Ejercicio integrador: API segura"
order: 9
estimatedMinutes: 30
summary: "Construí una API con registro, login, JWT, refresh tokens, roles, autorización por recurso y configuración segura, y verificá cada camino con pruebas reales."
quiz:
  - question: "¿Qué se devuelve al cliente cuando el login es correcto?"
    options:
      - "Solo el identificador del usuario"
      - "Un access token corto y un refresh token de mayor duración"
      - "La contraseña hasheada"
      - "El secreto de firma para que el cliente valide"
    answer: 1
    explanation: "El cliente recibe el access token para llamar a la API y el refresh token para renovarlo. El secreto de firma nunca sale del servidor."
  - question: "¿Dónde se verifica que un usuario puede modificar un pedido concreto?"
    options:
      - "En el cliente, ocultando el botón"
      - "En el servidor, comparando el dueño del recurso con la identidad del token"
      - "En la base de datos, sin lógica de aplicación"
      - "En el middleware de CORS"
    answer: 1
    explanation: "La autorización por recurso se decide en el servidor con la identidad del token, no con un id que envía el cliente."
  - question: "¿Qué debe hacer el endpoint de refresh si recibe un token ya revocado?"
    options:
      - "Emitir uno nuevo por comodidad"
      - "Rechazar con 401 y revocar la familia de tokens"
      - "Ignorar el problema"
      - "Devolver la contraseña del usuario"
    answer: 1
    explanation: "La reutilización de un token rotado es señal de robo. Se corta la sesión completa."
  - question: "¿Qué comprobación demuestra que la API no filtra datos entre usuarios?"
    options:
      - "Que el login funcione"
      - "Que un usuario autenticado reciba 403 al pedir un recurso de otro"
      - "Que exista Swagger"
      - "Que el token dure 15 minutos"
    answer: 1
    explanation: "La prueba de acceso horizontal es la que revela si la autorización por recurso está implementada."
---

# Ejercicio integrador: API segura

## Objetivo

Construir una API con registro, login, emisión y renovación de tokens, roles, autorización por recurso y configuración sin secretos en el repositorio, verificando cada camino con pruebas reales.

## Requisitos previos

Haber recorrido el módulo: [Autenticación vs. autorización](/modulos/seguridad-jwt/autenticacion-autorizacion), [Hash de contraseñas](/modulos/seguridad-jwt/hash-contrasenas), [JWT](/modulos/seguridad-jwt/jwt-estructura), [Refresh tokens](/modulos/seguridad-jwt/refresh-tokens), [OAuth y OIDC](/modulos/seguridad-jwt/oauth-oidc), [OWASP](/modulos/seguridad-jwt/owasp-top-10), [Secretos](/modulos/seguridad-jwt/secretos-configuracion) y [HTTPS, CORS y CSRF](/modulos/seguridad-jwt/https-cors-csrf).

## Concepto

El integrador reúne las piezas en una API de pedidos con estas reglas:

- Cualquiera puede registrarse; el rol inicial es `cliente`.
- El login devuelve un access token de 15 minutos y un refresh token de 7 días.
- Un usuario solo puede ver y modificar sus propios pedidos.
- Un usuario con rol `admin` puede ver todos los pedidos.
- El refresh token se rota y se revoca al cerrar sesión.
- La clave de firma y la cadena de conexión llegan por configuración, no por código.
- Todos los errores de autenticación devuelven `401` sin revelar si el email existe.

La separación de responsabilidades:

| Pieza | Responsabilidad |
|---|---|
| Endpoints de auth | Recibir credenciales y emitir tokens |
| `TokenService` | Firmar y validar tokens |
| `UserService` | Registro y verificación de contraseñas |
| `RefreshTokenService` | Emitir, rotar y revocar refresh tokens |
| Endpoints de pedidos | Traducir HTTP y delegar en servicios |

Ningún endpoint debe decidir por sí mismo si un token es válido: eso lo hace el middleware. Lo que sí decide cada endpoint es si la identidad puede operar sobre ese recurso concreto.

## Ejemplo mínimo

Registro y login con hashing y tokens:

```csharp
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddAuthorization();

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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/register", async (RegistroInput input, UserService users) =>
{
    var resultado = await users.RegistrarAsync(input.Email, input.Password);

    return resultado.EsExitoso
        ? TypedResults.Created($"/usuarios/{resultado.UsuarioId}", new { id = resultado.UsuarioId })
        : TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Email"] = [resultado.Error!]
        });
});

app.MapPost("/auth/login", async (LoginInput input, UserService users, TokenService tokens) =>
{
    var usuario = await users.VerificarAsync(input.Email, input.Password);

    if (usuario is null)
    {
        return TypedResults.Unauthorized();
    }

    var accessToken = tokens.CrearAccessToken(usuario);
    return TypedResults.Ok(new { accessToken, expiresIn = 900 });
});

app.Run();
```

```bash
curl -i -X POST http://localhost:5137/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"ana@ejemplo.com","password":"Password123"}'
```

```text
HTTP/1.1 201 Created
{"id":"7c1f5b1e-..."}
```

Los mensajes de error no distinguen entre email inexistente y contraseña incorrecta: el login devuelve `401` en ambos casos. Esa uniformidad evita enumerar cuentas.

## Ejemplo aplicado

Autorización por recurso y rol en los pedidos:

```csharp
app.MapGet("/pedidos", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

    var pedidos = user.IsInRole("admin")
        ? await db.Pedidos.AsNoTracking().ToListAsync()
        : await db.Pedidos.AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync();

    return TypedResults.Ok(pedidos);
})
.RequireAuthorization();

app.MapGet("/pedidos/{id:guid}", async (
    Guid id,
    ClaimsPrincipal user,
    AppDbContext db) =>
{
    var pedido = await db.Pedidos.FindAsync(id);

    if (pedido is null)
    {
        return TypedResults.NotFound();
    }

    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!user.IsInRole("admin") && pedido.UserId != userId)
    {
        return TypedResults.Forbid();
    }

    return TypedResults.Ok(pedido);
})
.RequireAuthorization();

app.MapPost("/pedidos", async (
    PedidoInput input,
    ClaimsPrincipal user,
    AppDbContext db) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var pedido = new Pedido(Guid.NewGuid(), userId, input.Descripcion, input.Total);

    db.Pedidos.Add(pedido);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/pedidos/{pedido.Id}", pedido);
})
.RequireAuthorization();

app.MapDelete("/pedidos/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db) =>
{
    var pedido = await db.Pedidos.FindAsync(id);

    if (pedido is null)
    {
        return TypedResults.NotFound();
    }

    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!user.IsInRole("admin") && pedido.UserId != userId)
    {
        return TypedResults.Forbid();
    }

    db.Pedidos.Remove(pedido);
    await db.SaveChangesAsync();

    return TypedResults.NoContent();
})
.RequireAuthorization();
```

```text
Cliente A pide su pedido        → 200 OK
Cliente A pide el de B          → 403 Forbidden
Admin pide el de B              → 200 OK
Sin token                       → 401 Unauthorized
```

Un detalle importante: `RequireAuthorization()` sin política solo exige identidad. La comprobación de propiedad es la línea que evita el acceso horizontal. Si esa línea falta, dos usuarios autenticados pueden leer los datos del otro.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Dos clientes ven los pedidos del otro | Se filtra solo por ruta, no por dueño | Comparar `UserId` con la identidad del token |
| El id del usuario viene en el cuerpo | Se confió en el cliente | Tomar la identidad de los claims |
| El login revela si el email existe | Mensajes distintos por caso | `401` uniforme y mensaje genérico |
| El refresh token nunca rota | Se simplificó el flujo | Rotar y detectar reutilización |
| La clave de firma está en el repo | Configuración por defecto | user-secrets o variables y rotación |
| Cualquier origen puede leer la API | CORS amplio | Orígenes explícitos y credenciales controladas |
| El token dura días | Comodidad | Access token corto con refresh |

## Buenas prácticas

- Identidad siempre desde el token, nunca desde el cuerpo o la query.
- Autorización por recurso además de por ruta.
- Mensajes de error uniformes en autenticación.
- Contraseñas con un hasher lento, no con SHA-256 simple.
- Refresh tokens opacos, hasheados y rotados.
- Configuración validada al arranque y secretos fuera del repositorio.
- Pruebas de acceso horizontal en la suite de integración.

## Ejercicio

Completá la API con:

1. `POST /auth/refresh` con rotación y detección de reutilización.
2. `POST /auth/logout` que revoque el refresh token y devuelva `204`.
3. Un rol `admin` protegido por política, no solo por `IsInRole` disperso.
4. Tests de integración para: login correcto, contraseña incorrecta, acceso al pedido ajeno y refresh reutilizado.

<details>
<summary>Ver solución</summary>

```csharp
app.MapPost("/auth/refresh", async (RefreshInput input, RefreshTokenService refrescos, TokenService tokens) =>
{
    var resultado = await refrescos.RotarAsync(input.RefreshToken);

    return resultado is null
        ? TypedResults.Unauthorized()
        : TypedResults.Ok(new
        {
            accessToken = tokens.CrearAccessToken(resultado.Usuario),
            refreshToken = resultado.NuevoToken,
            expiresIn = 900
        });
});

app.MapPost("/auth/logout", async (RefreshInput input, RefreshTokenService refrescos) =>
{
    await refrescos.RevocarAsync(input.RefreshToken);
    return TypedResults.NoContent();
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("admin", policy => policy.RequireRole("admin"));
});

app.MapGet("/admin/pedidos", async (AppDbContext db) =>
    TypedResults.Ok(await db.Pedidos.AsNoTracking().ToListAsync()))
   .RequireAuthorization("admin");
```

```csharp
public sealed class SeguridadApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SeguridadApiTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task LoginConPasswordIncorrectaDevuelve401()
    {
        await _client.PostAsJsonAsync("/auth/register",
            new { email = "ana@ejemplo.com", password = "Password123" });

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { email = "ana@ejemplo.com", password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ClienteNoPuedeLeerPedidoAjeno()
    {
        var tokenA = await LoginComo("ana@ejemplo.com");
        var tokenB = await LoginComo("bruno@ejemplo.com");

        var creado = await PostConToken("/pedidos", tokenB, new { descripcion = "Privado", total = 100 });
        var pedido = await creado.Content.ReadFromJsonAsync<PedidoResponse>();

        var response = await GetConToken($"/pedidos/{pedido!.Id}", tokenA);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

```text
Passed! - Failed: 0, Passed: 4, Skipped: 0
```

La suite cubre los caminos que importan: credenciales inválidas, acceso horizontal, renovación y cierre de sesión.

</details>

## Resumen

- El login emite un access token corto y un refresh token rotado.
- La identidad sale del token; la autorización por recurso se verifica en el servidor.
- Los roles no reemplazan la comprobación de propiedad.
- Los secretos y la configuración se validan al arranque.
- Las pruebas de acceso horizontal son parte del contrato de seguridad.

## Siguiente paso

La API es segura y está probada. El siguiente desafío es sostenerla cuando crece: en el módulo de [arquitectura de aplicaciones](/modulos/arquitectura) vas a organizar el código en capas y evaluar cuándo separar servicios.
