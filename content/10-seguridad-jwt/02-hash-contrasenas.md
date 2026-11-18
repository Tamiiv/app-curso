---
slug: hash-contrasenas
title: "Hash de contraseñas"
order: 2
estimatedMinutes: 15
summary: "Guardá contraseñas con hash lento y salt, usá PasswordHasher de Identity y elegí entre PBKDF2, BCrypt y Argon2id con criterio."
quiz:
  - question: "¿Por qué no se usa SHA-256 solo para almacenar contraseñas?"
    options:
      - "Porque produce hashes demasiado largos"
      - "Porque es rápido y permite probar millones de candidatas por segundo"
      - "Porque no admite salt"
      - "Porque .NET ya no lo implementa"
    answer: 1
    explanation: "Un hash rápido es fácil de atacar por fuerza bruta. Para contraseñas se usan funciones lentas y ajustables como PBKDF2, BCrypt o Argon2id."
  - question: "¿Qué aporta el salt en un hash de contraseña?"
    options:
      - "Hace que la contraseña sea más corta"
      - "Evita que dos contraseñas iguales produzcan el mismo hash"
      - "Cifra el hash para poder recuperarlo"
      - "Reemplaza la necesidad de un factor de trabajo"
    answer: 1
    explanation: "El salt es un valor aleatorio único por usuario. Impide tablas precalculadas y que dos hashes iguales delaten contraseñas repetidas."
  - question: "¿Qué indica PasswordVerificationResult.RehashNeeded al iniciar sesión?"
    options:
      - "Que la contraseña es incorrecta"
      - "Que el hash se generó con parámetros viejos y conviene regenerarlo"
      - "Que la cuenta está bloqueada"
      - "Que falta configurar el salt"
    answer: 1
    explanation: "Identity devuelve RehashNeeded cuando la verificación fue correcta pero el formato o costo quedó desactualizado. Hay que volver a hashear y guardar."
  - question: "¿Cuál es una ventaja de Argon2id frente a PBKDF2?"
    options:
      - "No necesita salt"
      - "Resiste mejor ataques con GPU porque exige memoria"
      - "Es más rápido de calcular"
      - "Funciona sin contraseña"
    answer: 1
    explanation: "Argon2id es memory-hard: encarece el ataque en hardware paralelo. PBKDF2 sigue siendo aceptable y es el default de Identity."
---

# Hash de contraseñas

## Objetivo

Guardar contraseñas de forma segura usando funciones de hash lentas, con salt único y parámetros de costo actualizables. Vas a usar `PasswordHasher<TUser>` de ASP.NET Core Identity, entender qué guarda realmente en la base y comparar las alternativas BCrypt y Argon2id.

## Requisitos previos

Necesitás entender autenticación, claims y el ciclo de registro y login de la [lección anterior](/modulos/seguridad-jwt/autenticacion-autorizacion). También ayuda conocer inyección de dependencias y algo de EF Core para persistir usuarios.

## Concepto

Una contraseña no se cifra: se hashea. Cifrar implica una clave reversible y, si esa clave se filtra, todas las contraseñas quedan expuestas. Un hash es una función de un solo sentido. Para verificar, se hashea la contraseña ingresada con los mismos parámetros y se compara con el valor guardado.

Un hash genérico como SHA-256 no sirve para contraseñas porque está diseñado para ser rápido. Con una GPU se prueban miles de millones de candidatas por segundo. Las funciones de hash de contraseñas son deliberadamente lentas y ajustables: PBKDF2, BCrypt y Argon2id. El costo se calibra para que una verificación tarde, por ejemplo, entre 100 y 500 ms en el servidor de producción.

El salt es un valor aleatorio distinto por usuario que se concatena con la contraseña antes de hashear. Se guarda junto al hash, no es secreto. Su función es que dos personas con la misma contraseña tengan hashes distintos y que las tablas precalculadas dejen de ser útiles. Un salt global o fijo es casi tan malo como no tener salt.

| Algoritmo | Estado | Notas |
|---|---|---|
| PBKDF2-HMAC-SHA512 | Default de Identity | Iteraciones configurables; disponible sin paquetes extra |
| BCrypt | Recomendado | Work factor ajustable; paquete `BCrypt.Net-Next` |
| Argon2id | Recomendado | Memory-hard; ideal contra GPU; paquete externo |
| SHA-256, MD5, SHA-1 | No usar | Demasiado rápidos para contraseñas |

ASP.NET Core Identity usa `PasswordHasher<TUser>` con PBKDF2. En las versiones actuales el default es HMAC-SHA512 con 210.000 iteraciones, y podés cambiarlo con `PasswordHasherOptions.IterationCount`. El hash resultante es un texto en base64 que incluye versión, salt, iteraciones y subclave. No hace falta guardar el salt por separado.

Un detalle importante es la actualización progresiva. Si mañana subís las iteraciones, los hashes viejos siguen siendo válidos. Al iniciar sesión, `VerifyHashedPassword` devuelve `Success` o `RehashNeeded`; en el segundo caso regenerás el hash con los parámetros nuevos. Así migrás sin pedirle la contraseña a nadie.

El "pepper" es un secreto adicional global que se combina con la contraseña y se guarda fuera de la base, por ejemplo en un gestor de secretos. Suma defensa en profundidad, pero complica la rotación y no reemplaza un buen algoritmo. Si lo usás, tratalo como una clave más del sistema.

## Ejemplo mínimo

`PasswordHasher<TUser>` no necesita una base de datos ni el resto de Identity. Podés usarlo solo para entender el formato y la verificación.

```csharp
using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<Usuario>();
var usuario = new Usuario("ana@ejemplo.local");
string clave = "<contraseña-de-ejemplo-local>";

string hash = hasher.HashPassword(usuario, clave);
Console.WriteLine(hash);

var resultado = hasher.VerifyHashedPassword(usuario, hash, clave);
Console.WriteLine(resultado);

var incorrecta = hasher.VerifyHashedPassword(usuario, hash, "otra-clave");
Console.WriteLine(incorrecta);

public sealed record Usuario(string Email);
```

```text
AQAAAAIAAYagAAAAE...<hash-base64-truncado>...
Success
Failed
```

El hash cambia en cada ejecución aunque la contraseña sea la misma, porque el salt es aleatorio. `Success` confirma la coincidencia; `Failed` no distingue mayúsculas, longitud ni formato, y esa ambigüedad es intencional: no le des pistas al atacante. Nunca registres en logs ni la contraseña ni el hash.

## Ejemplo aplicado

En una API real, Identity administra el hash, las reglas de complejidad y el bloqueo por intentos fallidos. El siguiente registro usa `UserManager` y el login usa `SignInManager` con bloqueo activado.

```csharp
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

public sealed class AppUser : IdentityUser;
public sealed record Registro(string Email, string Password);
```

```csharp
app.MapPost("/registro", async (Registro request, UserManager<AppUser> users) =>
{
    var usuario = new AppUser { UserName = request.Email, Email = request.Email };
    IdentityResult resultado = await users.CreateAsync(usuario, request.Password);

    return resultado.Succeeded
        ? Results.Created($"/usuarios/{usuario.Id}", new { usuario.Id })
        : Results.ValidationProblem(resultado.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
});

app.MapPost("/login", async (Registro request, SignInManager<AppUser> signIn) =>
{
    AppUser? usuario = await signIn.UserManager.FindByEmailAsync(request.Email);
    if (usuario is null)
    {
        return Results.Unauthorized();
    }

    var intento = await signIn.CheckPasswordSignInAsync(
        usuario, request.Password, lockoutOnFailure: true);

    return intento.Succeeded ? Results.Ok() : Results.Unauthorized();
});
```

```text
POST /registro con "clave-corta"        -> 400 con errores de longitud
POST /registro con una clave válida     -> 201
POST /login con la clave correcta       -> 200
POST /login con clave incorrecta        -> 401
POST /login cinco veces mal              -> 401 y cuenta bloqueada 15 minutos
```

Identity hashea dentro de `CreateAsync` y verifica dentro de `CheckPasswordSignInAsync`. No escribas tu propio hashing si ya usás Identity. La respuesta de login no debe revelar si el email existe: devolvé el mismo `401` en ambos casos.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Guardar la contraseña en texto plano | Se prioriza la simplicidad | Persistir solo el hash con `PasswordHasher` o Argon2id |
| Usar MD5 o SHA-1 "con salt" | Se cree que el salt compensa la velocidad | Usar PBKDF2, BCrypt o Argon2id con costo calibrado |
| Comparar hashes con `==` | Se expone a diferencias de tiempo y a errores lógicos | Usar `VerifyHashedPassword` o la verificación de la librería |
| Salt fijo o compartido | Se generó una sola vez para toda la tabla | Dejar que la librería genere un salt aleatorio por hash |
| No manejar `RehashNeeded` | Se ignoran los cambios de parámetros | Regenerar y guardar el hash tras un login exitoso |
| Sin límite de intentos | Un atacante prueba contraseñas sin freno | Lockout, rate limiting y monitoreo de intentos |

## Buenas prácticas

- Dejá que Identity (o una librería madura) genere salt y parámetros; no inventes el formato.
- Calibrá el costo en el hardware real de producción y revisalo cada año.
- Manejá `RehashNeeded` para subir iteraciones sin forzar un reseteo masivo.
- Sumá bloqueo temporal, rate limiting y alertas de intentos anómalos.
- Nunca loguees contraseñas, hashes ni el resultado completo de una verificación.

## Ejercicio

Implementá el registro y el login con Identity, y demostrá con tres casos que una contraseña incorrecta no autentica y que el quinto intento fallido bloquea la cuenta. Mostrá también qué ocurre cuando se sube `IterationCount` y el usuario vuelve a iniciar sesión.

<details>
<summary>Ver solución</summary>

```csharp
builder.Services.Configure<PasswordHasherOptions>(options =>
{
    options.IterationCount = 300_000;
});

app.MapPost("/login", async (Registro request, SignInManager<AppUser> signIn) =>
{
    AppUser? usuario = await signIn.UserManager.FindByEmailAsync(request.Email);
    if (usuario is null)
    {
        return Results.Unauthorized();
    }

    var intento = await signIn.CheckPasswordSignInAsync(
        usuario, request.Password, lockoutOnFailure: true);

    if (intento.RequiresTwoFactor)
    {
        return Results.Ok(new { requiere2fa = true });
    }

    return intento.Succeeded ? Results.Ok() : Results.Unauthorized();
});
```

```text
POST /login con clave correcta          -> 200
POST /login con clave incorrecta        -> 401
POST /login incorrecta x5                -> 401, luego LockedOut
POST /login con clave correcta bloqueada -> 401 hasta que expire el bloqueo
```

`IterationCount` más alto encarece la verificación. El próximo login exitoso devuelve `Success` y, cuando el formato queda viejo, `RehashNeeded`; `SignInManager` se ocupa de regenerar el hash en ese flujo.

</details>

## Resumen

- Las contraseñas se hashean con funciones lentas y ajustables, nunca se cifran.
- El salt aleatorio por usuario evita tablas precalculadas y hashes repetidos.
- Identity usa PBKDF2 y expone `PasswordHasher<TUser>` y `RehashNeeded`.
- BCrypt y Argon2id son alternativas sólidas; Argon2id resiste mejor ataques con GPU.
- El costo debe calibrarse, y el login necesita bloqueo y rate limiting.

## Siguiente paso

Con usuarios y contraseñas resueltos, el próximo desafío es no enviar la contraseña en cada petición. En la lección de [JWT: estructura y validación](/modulos/seguridad-jwt/jwt-estructura) vas a construir tokens firmados y validarlos del lado del servidor.
