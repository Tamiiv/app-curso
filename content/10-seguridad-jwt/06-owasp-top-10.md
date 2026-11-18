---
slug: owasp-top-10
title: "OWASP Top 10 en .NET"
order: 6
estimatedMinutes: 18
summary: "Recorré los riesgos más frecuentes de seguridad web y las defensas concretas que ofrece ASP.NET Core: autorización, validación, secretos, dependencias y logs."
quiz:
  - question: "¿Qué es un control de acceso roto (Broken Access Control)?"
    options:
      - "Que el usuario no recuerda su contraseña"
      - "Que un usuario autenticado puede acceder a recursos o acciones que no le corresponden"
      - "Que el servidor no tiene HTTPS"
      - "Que la base de datos está lenta"
    answer: 1
    explanation: "La autenticación dice quién sos; el control de acceso decide qué podés hacer. Fallar en lo segundo permite leer o modificar datos ajenos."
  - question: "¿Qué defensa reduce el riesgo de inyección SQL?"
    options:
      - "Concatenar el texto de la consulta"
      - "Usar consultas parametrizadas o LINQ con parámetros"
      - "Ocultar los errores de la base"
      - "Desactivar HTTPS"
    answer: 1
    explanation: "Los parámetros separan datos de instrucciones. Concatenar texto permite que la entrada cambie la consulta."
  - question: "¿Qué significa una mala configuración de seguridad?"
    options:
      - "Que el código tiene muchos comentarios"
      - "Que quedaron valores por defecto inseguros, directorios expuestos o detalles de error visibles"
      - "Que el proyecto usa pocas dependencias"
      - "Que el equipo tiene pocos integrantes"
    answer: 1
    explanation: "La configuración incluye CORS, cabeceras, entornos, usuarios por defecto y mensajes de error. Un descuido expone el sistema sin necesidad de un bug."
  - question: "¿Por qué es importante el logging de eventos de seguridad?"
    options:
      - "Porque mejora el rendimiento"
      - "Porque permite detectar y responder a intentos de acceso y abuso"
      - "Porque reemplaza a los tests"
      - "Porque oculta los errores"
    answer: 1
    explanation: "Sin registros de login fallido, cambios de permisos o errores de autorización, un incidente puede pasar inadvertido durante meses."
---

# OWASP Top 10 en .NET

## Objetivo

Reconocer los riesgos web más frecuentes, mapearlos a defensas concretas de ASP.NET Core y revisar una aplicación con una lista de verificación práctica.

## Requisitos previos

Haber leído [OAuth 2.0 y OpenID Connect](/modulos/seguridad-jwt/oauth-oidc). Necesitás entender autenticación, autorización y validación de entrada.

## Concepto

OWASP publica una lista de las categorías de riesgo más frecuentes en aplicaciones web. No es un checklist de cumplimiento: es un mapa para priorizar.

| Categoría | Qué significa | Defensa en .NET |
|---|---|---|
| Control de acceso roto | Acciones o datos sin verificar permisos | `RequireAuthorization`, políticas, filtros por recurso |
| Fallos criptográficos | Datos sensibles sin proteger | HTTPS, hashing de contraseñas, cifrado en reposo |
| Inyección | Entrada que altera instrucciones | Consultas parametrizadas, validación, codificación de salida |
| Diseño inseguro | Reglas de negocio evitables | Modelado de amenazas, límites y flujos seguros |
| Mala configuración | Valores por defecto inseguros | Entornos, cabeceras, CORS, errores controlados |
| Componentes vulnerables | Dependencias con CVEs | Actualización y escaneo continuo |
| Fallos de identificación | Login débil o sesiones mal gestionadas | Identity, bloqueo, MFA, cookies seguras |
| Fallos de integridad | Datos o actualizaciones sin verificar | Firmas, versiones, controles de origen |
| Logging insuficiente | Incidentes sin rastro | Logs estructurados, alertas y auditoría |
| SSRF | El servidor consulta URLs controladas por el atacante | Lista de destinos permitidos, sin seguir redirecciones |

La defensa no es una capa: es la combinación de autorización por recurso, validación de entrada, configuración segura y observabilidad.

## Ejemplo mínimo

Control de acceso por recurso, no solo por ruta:

```csharp
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

    if (pedido.UserId != user.FindFirstValue(ClaimTypes.NameIdentifier))
    {
        return TypedResults.Forbid();
    }

    return TypedResults.Ok(pedido);
})
.RequireAuthorization();
```

```text
Dueño del pedido        → 200 OK
Otro usuario autenticado → 403 Forbidden
Sin sesión              → 401 Unauthorized
```

`RequireAuthorization` solo verifica que haya identidad. La comprobación de propiedad es la que evita el acceso horizontal.

## Ejemplo aplicado

Configuración segura de errores, cabeceras y CORS:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
        policy.WithOrigins("https://ruta.net")
              .WithMethods("GET", "POST")
              .WithHeaders("Content-Type", "Authorization"));
});

builder.Services.AddHealthChecks();

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
    context.Response.Headers["Content-Security-Policy"] =
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/error", () => Results.Problem(
    title: "Ocurrió un error.",
    statusCode: StatusCodes.Status500InternalServerError));

app.Run();
```

```text
Producción:
- Sin stack traces.
- HSTS activo.
- CORS limitado al origen real.
- Cabeceras de protección en todas las respuestas.
```

En desarrollo se puede mostrar el detalle; en producción, no. La diferencia se controla por entorno, no con un comentario.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Un usuario ve pedidos de otro | Solo se validó el rol, no la propiedad | Verificar el dueño del recurso en cada acceso |
| El listado filtra por id del cliente | El cliente envía el id y el servidor confía | Tomar el id de la identidad del token, no del request |
| CORS con `AllowAnyOrigin` y credenciales | Comodidad en desarrollo | Orígenes explícitos y sin comodín con cookies |
| Stack traces en producción | Entorno mal configurado | `UseExceptionHandler` y `ProblemDetails` |
| Dependencia con CVE crítica | No se escanea | `dotnet list package --vulnerable` en CI |
| Sin logs de login fallido | No hay auditoría | Registrar eventos de seguridad sin datos sensibles |
| Redirect seguido sin validar | SSRF | Lista blanca de hosts y no seguir redirecciones |

## Buenas prácticas

- Autorizá por recurso, no solo por ruta.
- Tomá la identidad del token, nunca de un campo del cuerpo.
- Parametrizá consultas y validá la entrada en el borde.
- Mantené dependencias actualizadas y escaneadas en CI.
- Registrá eventos de seguridad con id de correlación, sin tokens ni contraseñas.
- Revisá la configuración de producción como parte del deploy.

## Ejercicio

Revisá una API propia con esta lista y anotá al menos una mejora por categoría:

1. ¿Cada endpoint verifica que el recurso pertenece al usuario?
2. ¿Hay consultas construidas con concatenación?
3. ¿Los errores muestran detalles internos en producción?
4. ¿Las dependencias tienen CVEs conocidos?
5. ¿Se registran login fallido y accesos denegados?

<details>
<summary>Ver solución</summary>

```text
1. Endpoint /pedidos/{id}: faltaba comprobar pedido.UserId == usuario.
   Mejora: devolver 403 si no coincide.

2. Búsqueda: usaba FromSqlRaw con interpolación.
   Mejora: FromSqlInterpolated o consultas LINQ parametrizadas.

3. Producción mostraba el stack trace.
   Mejora: UseExceptionHandler + ProblemDetails genérico.

4. dotnet list package --vulnerable encontró un paquete desactualizado.
   Mejora: actualizar y agregar el chequeo al CI.

5. No había logs de autorización.
   Mejora: registrar accesos denegados con id de usuario y ruta.
```

Cada mejora es concreta y verificable. No hace falta reescribir la aplicación para reducir riesgo.

</details>

## Resumen

- La autorización por recurso evita accesos horizontales.
- Las consultas parametrizadas y la validación frenan la inyección.
- La configuración segura incluye errores, cabeceras, CORS y entornos.
- El logging de seguridad convierte incidentes invisibles en eventos detectables.

## Siguiente paso

Muchos incidentes empiezan con una clave en el lugar equivocado. En [Secretos y configuración](/modulos/seguridad-jwt/secretos-configuracion) vas a manejar claves sin filtrarlas en el repositorio.
