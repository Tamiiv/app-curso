---
slug: https-cors-csrf
title: "HTTPS, CORS y CSRF"
order: 8
estimatedMinutes: 15
summary: "Protegé el transporte con HTTPS, abrí CORS solo a los orígenes necesarios y defendé las cookies de sesión contra peticiones cruzadas."
quiz:
  - question: "¿Qué protege HTTPS?"
    options:
      - "El contenido del servidor frente a errores de programación"
      - "Los datos en tránsito frente a intercepción y modificación"
      - "La base de datos en reposo"
      - "Las contraseñas en el código"
    answer: 1
    explanation: "HTTPS cifra el canal entre cliente y servidor. No protege los datos guardados ni reemplaza la validación o la autorización."
  - question: "¿Qué controla CORS?"
    options:
      - "Qué usuarios pueden iniciar sesión"
      - "Qué orígenes del navegador pueden leer la respuesta de la API"
      - "Qué tablas se pueden consultar"
      - "El tamaño de las respuestas"
    answer: 1
    explanation: "CORS es una política del navegador sobre lectura de respuestas entre orígenes. No es autenticación ni bloquea peticiones del lado del servidor."
  - question: "¿Qué ataque previene principalmente una cookie SameSite=Lax o Strict?"
    options:
      - "Inyección SQL"
      - "CSRF: que otro sitio envíe peticiones autenticadas con la cookie del usuario"
      - "Fuerza bruta de contraseñas"
      - "Denegación de servicio"
    answer: 1
    explanation: "SameSite evita que la cookie se envíe en peticiones iniciadas desde otro sitio, lo que frena el CSRF clásico."
  - question: "¿Por qué no se debe combinar `AllowAnyOrigin` con credenciales?"
    options:
      - "Porque es más lento"
      - "Porque cualquier sitio podría leer respuestas autenticadas del usuario"
      - "Porque rompe HTTPS"
      - "Porque impide usar cookies"
    answer: 1
    explanation: "El navegador prohíbe esa combinación por un motivo: permitiría a cualquier origen leer datos privados usando la sesión de la víctima."
---

# HTTPS, CORS y CSRF

## Objetivo

Configurar transporte seguro con HTTPS y HSTS, definir políticas CORS explícitas y proteger las cookies de sesión contra CSRF sin bloquear a los clientes legítimos.

## Requisitos previos

Haber leído [Secretos y configuración](/modulos/seguridad-jwt/secretos-configuracion). Necesitás manejar middleware, cookies y el pipeline de ASP.NET Core.

## Concepto

Tres protecciones distintas que suelen confundirse:

| Protección | Qué ataca | Dónde se configura |
|---|---|---|
| HTTPS | Intercepción en la red | Transporte, certificados, HSTS |
| CORS | Lectura de respuestas entre orígenes | Middleware del servidor |
| CSRF | Peticiones forjadas con la cookie de la víctima | Cookies `SameSite`, tokens anti-CSRF |

**HTTPS** cifra el canal. Sin él, un token viaja legible por cualquier intermediario. En producción se usa un certificado válido, redirección desde HTTP y HSTS para que el navegador no vuelva a intentar HTTP.

**CORS** es una política del navegador, no del servidor. El navegador envía una petición a otro origen y bloquea la lectura de la respuesta si el servidor no la autoriza con las cabeceras correspondientes. Eso significa dos cosas importantes:

- CORS no protege la API: un cliente que no es un navegador lo ignora. La autenticación y la autorización siguen siendo obligatorias.
- Una política amplia no es "más compatible": expone datos a orígenes que no deberían leerlos.

**CSRF** aprovecha que el navegador envía cookies automáticamente. Si la víctima tiene sesión activa en el banco y visita un sitio malicioso, ese sitio puede disparar una transferencia; el navegador adjunta la cookie. Las defensas:

- Cookies `SameSite=Lax` (o `Strict`) para que no viajen en peticiones cruzadas.
- Tokens anti-CSRF para operaciones que cambian estado.
- Verificar `Origin` o `Referer` en endpoints sensibles.
- No usar `GET` para acciones que modifican datos.

Con APIs que usan `Authorization: Bearer`, CSRF no aplica igual: el atacante no puede leer el token desde otro origen. El riesgo aparece cuando la credencial viaja en cookie.

## Ejemplo mínimo

Pipeline con HTTPS, HSTS y cabeceras de seguridad:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.MapGet("/", () => "OK");

app.Run();
```

```bash
curl -i http://localhost:5137/
```

```text
HTTP/1.1 307 Temporary Redirect
Location: https://localhost:7137/
```

`UseHttpsRedirection` redirige, pero no protege la primera petición: por eso HSTS indica al navegador que use HTTPS directamente durante un período. En producción, HSTS se activa solo cuando el certificado ya funciona; si no, el sitio queda inaccesible.

## Ejemplo aplicado

CORS explícito y cookies seguras para una SPA con sesión:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
        policy.WithOrigins("https://ruta.net", "https://www.ruta.net")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Content-Type", "Authorization", "X-CSRF-TOKEN")
              .AllowCredentials());
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
    });

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/api/perfil", (ClaimsPrincipal user) => TypedResults.Ok(new
{
    nombre = user.Identity?.Name
}))
.RequireAuthorization();

app.MapPost("/api/pedidos", (HttpContext context, PedidoInput input) =>
{
    return TypedResults.Created($"/api/pedidos/{Guid.NewGuid()}", input);
})
.RequireAuthorization()
.RequireAntiforgery();

app.Run();
```

```text
Origen https://ruta.net        → CORS permite y CSRF valida el token
Origen https://otro-sitio.com  → el navegador bloquea la lectura
POST sin token CSRF            → 400, la operación no se ejecuta
```

Puntos que hacen la diferencia:

- `WithOrigins` con dominios exactos, no comodines.
- `AllowCredentials` exige orígenes explícitos; el navegador rechaza la combinación con `*`.
- `SameSite=Lax` cubre la mayoría de los casos; `Strict` es más seguro pero complica enlaces entrantes.
- El token anti-CSRF se emite en una cookie legible y se envía en una cabecera; el servidor compara ambos.

En una API consumida solo por servidores o apps móviles, no hace falta CORS ni anti-CSRF. Agregar capas que no aplican al modelo de amenaza solo confunde.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| CORS con `AllowAnyOrigin` y `AllowCredentials` | Se buscó "que funcione" | Orígenes explícitos |
| CORS se usa como seguridad de la API | Se confundió navegador con servidor | Autenticar y autorizar en cada endpoint |
| Cookies sin `Secure` ni `HttpOnly` | Configuración por defecto | Fijar ambas en producción |
| CSRF ignorado en API con cookies | Se asumió que el token Bearer era el único caso | `SameSite` + token anti-CSRF |
| HSTS activado antes del certificado | Se copió la configuración de producción | Activar solo con HTTPS funcionando |
| Token anti-CSRF en una cookie accesible y sin comparación | Se agregó el mecanismo a medias | Comparar cookie y cabecera en el servidor |

## Buenas prácticas

- HTTPS obligatorio; HSTS después de validar el certificado.
- CORS con la lista mínima de orígenes y métodos.
- Cookies de sesión con `HttpOnly`, `Secure` y `SameSite`.
- Token anti-CSRF en todas las operaciones que cambian estado con sesión por cookie.
- No usar `GET` para mutaciones.
- Revisar las cabeceras de seguridad en cada entorno, no solo en producción.

## Ejercicio

Configurá una API con dos orígenes permitidos, cookies `HttpOnly`, `Secure` y `SameSite=Lax`, y protección anti-CSRF en un `POST /api/tareas`. Verificá con `curl` que sin el token la petición devuelve error y con el token funciona.

<details>
<summary>Ver solución</summary>

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
        policy.WithOrigins("https://ruta.net", "https://app.ruta.net")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/api/csrf", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return TypedResults.Ok(new { token = tokens.RequestToken });
});

app.MapPost("/api/tareas", (TareaInput input) => TypedResults.Created("/api/tareas/1", input))
   .RequireAuthorization()
   .RequireAntiforgery();
```

```bash
# Sin token: la petición se rechaza antes del handler.
curl -i -X POST https://localhost:7137/api/tareas \
  -H "Content-Type: application/json" \
  -d '{"titulo":"Estudiar"}'

# Con token: se obtiene en /api/csrf y se envía en la cabecera.
curl -i -X POST https://localhost:7137/api/tareas \
  -H "Content-Type: application/json" \
  -H "X-CSRF-TOKEN: <token>" \
  -d '{"titulo":"Estudiar"}'
```

```text
Sin token → 400 Bad Request
Con token → 201 Created
```

La cookie de sesión viaja sola, pero el token debe leerse y enviarse explícitamente. Esa asimetría es la que frena el CSRF.

</details>

## Resumen

- HTTPS protege el transporte; HSTS evita la primera petición insegura.
- CORS es una política del navegador y no reemplaza la autorización.
- CSRF se defiende con `SameSite`, tokens y verificación de origen.
- Las cookies de sesión necesitan `HttpOnly`, `Secure` y `SameSite`.
- Cada protección responde a un modelo de amenaza distinto.

## Siguiente paso

Cerramos el módulo con el [ejercicio integrador de seguridad](/modulos/seguridad-jwt/ejercicio-integrador): login, tokens, refresh, autorización por roles y revisión de configuración en una sola API.
