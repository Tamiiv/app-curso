---
slug: autenticacion-autorizacion
title: "Autenticación vs. autorización"
order: 1
estimatedMinutes: 12
summary: "Diferenciá quién es un usuario de qué puede hacer, y aplicá ambos conceptos con Identity, claims y políticas en ASP.NET Core."
quiz:
  - question: "¿Qué significa una respuesta HTTP 403?"
    options:
      - "El usuario no envió credenciales"
      - "El usuario está autenticado pero no tiene permiso"
      - "El recurso no existe"
      - "El servidor tuvo un error interno"
    answer: 1
    explanation: "401 indica que falta autenticación; 403 indica que la identidad se reconoció pero la autorización falló."
  - question: "¿Dónde se evalúan las políticas de autorización?"
    options:
      - "En el navegador, antes de enviar la petición"
      - "En el servidor, en cada petición"
      - "Solo al iniciar sesión"
      - "En la base de datos, con un trigger"
    answer: 1
    explanation: "Ocultar botones en el cliente es solo cosmético. La decisión real se toma en el servidor por cada petición."
  - question: "¿Qué componente llena HttpContext.User durante una petición?"
    options:
      - "El middleware de autorización"
      - "El middleware de autenticación"
      - "El motor de routing"
      - "El middleware de archivos estáticos"
    answer: 1
    explanation: "El middleware de autenticación valida las credenciales y construye el ClaimsPrincipal que queda en HttpContext.User."
---

# Autenticación vs. autorización

## Objetivo

Distinguir con precisión los dos conceptos que sostienen toda la seguridad de una aplicación: autenticación, que responde quién sos, y autorización, que responde qué podés hacer. Vas a implementar ambos en ASP.NET Core con cookies, claims y políticas, y a reconocer dónde se toma cada decisión.

## Requisitos previos

Necesitás manejar minimal APIs, inyección de dependencias y el pipeline de middleware. Conviene haber cerrado el módulo anterior con el [ejercicio integrador de refactoring](/modulos/codigo-limpio/refactoring-ejercicio-integrador): acá vas a aplicar esas mismas ideas de responsabilidades separadas al código de seguridad.

## Concepto

Autenticación y autorización son dos pasos distintos y en ese orden. Primero el servidor intenta saber quién llama; después decide si esa identidad puede ejecutar la acción. Si el primero falla, la respuesta correcta es `401 Unauthorized`. Si el segundo falla, es `403 Forbidden`.

| Concepto | Pregunta que responde | Respuesta si falla |
|---|---|---|
| Autenticación | ¿Quién sos? | `401 Unauthorized` |
| Autorización | ¿Podés hacer esto? | `403 Forbidden` |
| Claim | ¿Qué dato se afirma de tu identidad? | No aplica |
| Política | ¿Qué regla nombrada se exige? | `403 Forbidden` |

Al autenticarse, el usuario se representa con un `ClaimsPrincipal`, que contiene identidades y cada identidad contiene claims. Un claim es un par nombre-valor: `sub=ana`, `role=admin`, `permiso=pedidos:cerrar`. Los emite el servidor al crear la sesión o el token; nunca los dicta el cuerpo de la petición.

El esquema de autenticación define el mecanismo: cookie, JWT bearer u OpenID Connect. Acá empezás con cookies y después pasás a JWT; la autorización trabaja igual con cualquier esquema porque se apoya en el principal ya construido.

Las políticas son la forma recomendada de expresar reglas: tienen un nombre estable, como `SoloAdministradores`, y condiciones que deben cumplirse. Conviene nombrarlas por intención de negocio y no por el rol concreto, porque `PuedeCerrarPedido` comunica mejor que `EsSupervisor`. El orden del pipeline importa: `UseRouting`, luego `UseAuthentication`, después `UseAuthorization` y al final los endpoints. Si los invertís, la autorización se ejecuta sin identidad y todo devuelve `401`. Con `FallbackPolicy`, toda ruta queda protegida por defecto y solo se abre lo que marques explícitamente.

## Ejemplo mínimo

Este ejemplo usa autenticación por cookie y una política por rol. El login es solo para desarrollo local: crea una sesión sin contraseña para concentrarse en el flujo de autorización. No lo copies a producción.

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-sesion";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("SoloAdministradores", policy => policy.RequireRole("admin"));

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/panel", (ClaimsPrincipal user) => $"Hola {user.Identity?.Name}")
    .RequireAuthorization("SoloAdministradores");

if (app.Environment.IsDevelopment())
{
    app.MapGet("/login-demo", async (HttpContext context, string rol) =>
    {
        var identidad = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "ana"), new Claim(ClaimTypes.Role, rol)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad));
        return Results.Ok($"Sesión iniciada con rol {rol}");
    });
}

app.Run();```

```text
GET /panel sin cookie                     -> 401
GET /login-demo?rol=admin                 -> 200
GET /panel con la cookie emitida          -> 200 "Hola ana"
GET /login-demo?rol=lector y luego /panel -> 403
```

La cookie viaja en cada petición y el middleware reconstruye el principal. La política no mira el nombre del usuario: exige el rol `admin`. Si el rol falta, la identidad existe igual y por eso la respuesta es `403`, no `401`.

## Ejemplo aplicado

En un sistema de pedidos no alcanza con roles: un cliente puede ver sus pedidos, pero no los de otro. Esa regla depende del recurso concreto y se resuelve con autorización basada en recursos.

```csharp
public sealed record Pedido(int Id, string Propietario, decimal Total);

public sealed class PropietarioRequirement : IAuthorizationRequirement;

public sealed class PropietarioHandler
    : AuthorizationHandler<PropietarioRequirement, Pedido>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PropietarioRequirement requirement,
        Pedido pedido)
    {
        string? usuario = context.User.Identity?.Name;
        if (context.User.IsInRole("admin") || pedido.Propietario == usuario)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

El handler y el requerimiento se registran como `scoped`, y el endpoint consulta el servicio después de cargar el pedido:

```csharp
builder.Services.AddScoped<PropietarioRequirement>();
builder.Services.AddScoped<IAuthorizationHandler, PropietarioHandler>();
var pedidos = new Dictionary<int, Pedido>
{
    [1] = new(1, "ana", 1500m), [2] = new(2, "bruno", 900m)
};

app.MapGet("/pedidos/{id:int}", async (
    int id, ClaimsPrincipal user, IAuthorizationService autorizacion) =>
{
    if (!pedidos.TryGetValue(id, out Pedido? pedido))
    {
        return Results.NotFound();
    }

    var resultado = await autorizacion.AuthorizeAsync(
        user, pedido, new PropietarioRequirement());

    return resultado.Succeeded ? Results.Ok(pedido) : Results.Forbid();
});
```

```text
ana consulta /pedidos/1   -> 200 { "id": 1, "propietario": "ana", "total": 1500 }
ana consulta /pedidos/2   -> 403
admin consulta /pedidos/2 -> 200
```

La clave es que el handler recibe el pedido ya cargado: la decisión combina identidad y dato. Ser admin alcanza; un usuario común solo pasa si es el propietario. No devuelvas `404` para ocultar un `403`: confunde el diagnóstico y complica el soporte.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Todo devuelve `401` aunque el usuario esté logueado | `UseAuthorization` corre antes que `UseAuthentication` | Respetar el orden del pipeline |
| Se devuelve `403` a un usuario anónimo | Se confunde falta de identidad con falta de permiso | Dejar que el desafío produzca el `401` |
| Ocultar botones y creer que eso protege la acción | La interfaz no es un límite de seguridad | Validar de nuevo en el servidor con una política |
| Políticas que solo verifican roles | Los roles son gruesos y no cubren propiedad ni estado | Combinar roles con claims y recursos |
| `RequireRole` y `RequireClaim` dejan pasar a menos gente | Las condiciones se combinan con AND | Usar `RequireAssertion` si la regla es OR |

## Buenas prácticas

- Adoptá denegar por defecto: configurá `FallbackPolicy` y abrí explícitamente solo lo público.
- Nombrá las políticas por intención (`PuedeCerrarPedido`) y no por el rol que hoy las cumple.
- Usá roles para grupos amplios y claims o recursos para reglas finas de propiedad.
- Registrá los rechazos con usuario, ruta y política, sin volcar datos sensibles.

## Ejercicio

Creá la política `PuedeCerrarPedido`, que debe permitir la acción si el usuario tiene el rol `supervisor` **o** el claim `permiso` con valor `pedidos:cerrar`. Protegé `POST /pedidos/{id:int}/cerrar` y verificá los cuatro casos: anónimo, lector, supervisor y usuario con permiso.

<details>
<summary>Ver solución</summary>

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PuedeCerrarPedido", policy => policy
        .RequireAssertion(context =>
            context.User.IsInRole("supervisor") ||
            context.User.HasClaim("permiso", "pedidos:cerrar")));

app.MapPost("/pedidos/{id:int}/cerrar", (int id) =>
        Results.Ok($"Pedido {id} cerrado"))
    .RequireAuthorization("PuedeCerrarPedido");
```

```text
POST /pedidos/1/cerrar sin cookie      -> 401
POST /pedidos/1/cerrar como lector     -> 403
POST /pedidos/1/cerrar como supervisor -> 200 "Pedido 1 cerrado"
POST /pedidos/1/cerrar con permiso     -> 200 "Pedido 1 cerrado"
```

`RequireRole` y `RequireClaim` encadenados habrían exigido ambas condiciones. Como el enunciado propone una alternativa, la expresión correcta es una aserción con `||`.

</details>

## Resumen

- Autenticación responde quién sos; autorización, qué podés hacer.
- El fallo de autenticación es `401`; el de autorización, `403`.
- El middleware de autenticación construye `HttpContext.User` con identidades y claims.
- Las políticas expresan reglas nombradas y se evalúan en el servidor en cada petición.

## Siguiente paso

Con el flujo claro, toca el punto más delicado: cómo guardar contraseñas. En la próxima lección vas a ver [hash de contraseñas](/modulos/seguridad-jwt/hash-contrasenas) con Identity, BCrypt y Argon2id, y por qué nunca se guarda una contraseña en texto plano.
