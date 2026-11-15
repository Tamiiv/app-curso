# Arquitectura

**Proyecto:** Ruta .NET (nombre provisorio)  
**Estado:** Fase 5 — Calidad (en curso); deploy pendiente  
**Última actualización:** 15/11/2026

> Este documento describe la arquitectura del proyecto. El esqueleto ya está implementado en local; falta publicarlo en Render con la base de datos de Neon.

## 1. Resumen de decisiones

| Decisión | Elección | Motivo |
|---|---|---|
| Lenguaje | C# / .NET 10 LTS | Un solo lenguaje, soporte a largo plazo |
| Framework web | ASP.NET Core + Blazor Web App | SSR para SEO, interactividad puntual, coherencia con el contenido |
| Renderizado | SSR estático + Interactive Server en islas | Buen SEO y primera carga rápida; interactividad solo donde hace falta |
| Base de datos | PostgreSQL en Neon | Administrada, gratuita, relacional, integración natural con EF Core |
| ORM | EF Core | Estándar de .NET, migrations y consultas tipadas |
| Autenticación | ASP.NET Core Identity + cookies | Simple, segura y del lado del servidor |
| Contenido | Markdown versionado en el repositorio | Sin CMS, fácil de revisar y desplegar |
| Búsqueda | Índice en memoria | Suficiente para el volumen de la V1 |
| Hosting | Render (plan gratuito) | Costo $0, soporta contenedores .NET |
| CI | GitHub Actions | Build, tests y formato en cada push |

## 2. Diagrama de componentes

```
┌─────────────────────────────┐
│          Navegador          │
│  HTML SSR + islas Blazor    │
└──────────────┬──────────────┘
               │ HTTPS
               ▼
┌──────────────────────────────────────────┐
│  Render — ASP.NET Core + Blazor Web App  │
│                                          │
│  ┌────────────┐  ┌────────────────────┐  │
│  │  Contenido │  │  Identity + cookie │  │
│  │  Markdown  │  │  progreso, bookmarks│ │
│  └────────────┘  └─────────┬──────────┘  │
│                            │ EF Core      │
└────────────────────────────┼─────────────┘
                             │ SSL
                             ▼
                  ┌─────────────────────┐
                  │  Neon PostgreSQL    │
                  │  usuarios, progreso │
                  └─────────────────────┘
```

El contenido **no** vive en la base de datos. La base guarda usuarios, progreso, marcadores y claves de Data Protection.

## 3. Estructura del repositorio

Propuesta inicial (monolito modular, un solo proyecto web + tests):

```
app-curso/
├── docs/                        # Documentación del proyecto
├── content/                     # Contenido en Markdown
│   ├── 00-como-usar/
│   │   ├── module.json
│   │   └── 01-....md
│   ├── 01-fundamentos/
│   └── ...
├── src/
│   └── AppCurso.Web/
│       ├── Components/
│       │   ├── Layout/          # MainLayout, NavMenu, ThemeToggle
│       │   ├── Pages/           # Home, Modules, Lesson, Search, Account
│       │   └── Shared/          # CodeBlock, Quiz, ProgressBar
│       ├── Features/
│       │   ├── Content/         # Catálogo, parser, búsqueda
│       │   ├── Progress/        # Progreso por lección y módulo
│       │   ├── Bookmarks/       # Marcadores
│       │   └── Account/         # Registro, login, recuperación
│       ├── Data/                # DbContext, configuraciones, migrations
│       ├── Domain/              # Entidades e interfaces
│       ├── Services/            # Servicios de aplicación
│       ├── wwwroot/             # CSS, JS mínimo, imágenes
│       ├── Program.cs
│       └── AppCurso.Web.csproj
├── tests/
│   └── AppCurso.Tests/
├── AppCurso.slnx
├── Directory.Build.props
├── .editorconfig
└── README.md
```

**Por qué un solo proyecto web:** para la V1 es más simple de navegar y mantener. Usamos carpetas por feature (no por tipo de archivo) para que cada funcionalidad esté junta. Si el proyecto crece, se pueden extraer `Domain` e `Infrastructure` a proyectos separados sin reescribir la lógica.

## 4. Capas y responsabilidades

| Capa | Responsabilidad |
|---|---|
| `Components` | UI Blazor: páginas, layouts y componentes reutilizables. Sin lógica de negocio. |
| `Features/Content` | Leer Markdown, construir el catálogo, renderizar HTML, indexar para búsqueda. |
| `Features/Progress` | Registrar y consultar progreso por usuario y lección. |
| `Features/Bookmarks` | Alta, baja y consulta de marcadores. |
| `Features/Account` | Registro, login, logout y recuperación de contraseña. |
| `Data` | `AppDbContext`, configuraciones de EF Core y migrations. |
| `Domain` | Entidades (`LessonProgress`, `Bookmark`) y enums. Sin dependencias de infraestructura. |
| `Services` | Casos de uso que combinan contenido, progreso y usuario. |

Regla: los componentes no acceden directamente al `DbContext`; usan servicios.

## 5. Modelo de contenido

### Estructura

```
content/
├── 00-como-usar/
│   ├── module.json
│   ├── 01-como-usar-la-plataforma.md
│   └── 02-como-practicar.md
├── 01-fundamentos/
│   ├── module.json
│   ├── 01-que-es-dotnet.md
│   └── ...
└── 02-csharp-desde-cero/
    ├── module.json
    └── ...
```

### Metadatos del módulo (`module.json`)

```json
{
  "slug": "fundamentos",
  "title": "Fundamentos de .NET",
  "order": 1,
  "level": "Principiante",
  "summary": "Qué es .NET, cómo instalarlo y cómo crear tu primer proyecto.",
  "status": "published"
}
```

### Metadatos de la lección (front matter YAML)

```yaml
---
slug: que-es-dotnet
title: "¿Qué es .NET?"
order: 1
estimatedMinutes: 10
summary: "Runtime, SDK y ecosistema: las piezas básicas antes de escribir código."
quiz:
  - question: "¿Qué incluye el SDK de .NET?"
    options:
      - "Solo el runtime"
      - "El runtime, las herramientas y las plantillas"
      - "Solo el compilador"
    answer: 1
    explanation: "El SDK incluye el runtime, la CLI, las plantillas y las herramientas de compilación."
---
```

### Reglas

- El `slug` de la lección es único dentro del módulo.
- El `order` define el orden de lectura y navegación.
- El contenido se lee al iniciar la aplicación y se cachea en memoria.
- Los cambios de contenido requieren un nuevo deploy (no hay edición en caliente).
- Las imágenes van en `wwwroot/images/content/` y se referencian con rutas relativas.
- Los bloques de código se renderizan con Markdig y resaltado de sintaxis del lado del cliente.
- El Markdown es contenido propio y confiable; no se acepta Markdown de usuarios.

### Formato de una lección

Todas las lecciones siguen la misma estructura para que la experiencia sea predecible:

1. Objetivo de la lección.
2. Requisitos previos.
3. Explicación conceptual.
4. Ejemplo mínimo.
5. Ejemplo aplicado.
6. Errores comunes.
7. Buenas prácticas.
8. Ejercicio.
9. Quiz.
10. Resumen y siguiente paso.

## 6. Modelo de datos

La base guarda solo lo que cambia por usuario. El contenido se referencia por `LessonSlug`.

### Tablas de Identity

ASP.NET Core Identity crea sus tablas estándar (`AspNetUsers`, `AspNetUserClaims`, `AspNetUserLogins`, etc.). No agregamos roles en la V1.

### `LessonProgress`

| Columna | Tipo | Notas |
|---|---|---|
| `Id` | `uuid` | PK |
| `UserId` | `text` | FK a `AspNetUsers.Id`, indexado |
| `LessonSlug` | `varchar(200)` | Slug de la lección |
| `Status` | `int` | `Started = 0`, `Completed = 1` |
| `StartedAt` | `timestamptz` | UTC |
| `LastViewedAt` | `timestamptz` | UTC; se actualiza al abrir la lección |
| `CompletedAt` | `timestamptz?` | UTC |

Índice único: `(UserId, LessonSlug)`.

### `Bookmark`

| Columna | Tipo | Notas |
|---|---|---|
| `Id` | `uuid` | PK |
| `UserId` | `text` | FK a `AspNetUsers.Id`, indexado |
| `LessonSlug` | `varchar(200)` | Slug de la lección |
| `CreatedAt` | `timestamptz` | UTC |

Índice único: `(UserId, LessonSlug)`.

### `DataProtectionKeys`

Tabla estándar de `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`. Es necesaria para que las cookies de sesión sobrevivan a reinicios y deploys en un entorno con disco efímero.

### Reglas

- `LessonSlug` no es una FK porque el contenido vive en archivos. Al leer, se valida contra el catálogo; si no existe, se ignora.
- Todas las fechas se guardan en UTC.
- Las operaciones de progreso son idempotentes: marcar dos veces una lección como completada no duplica filas.
- El progreso de un módulo se calcula contando lecciones completadas sobre el total de lecciones publicadas.

## 7. Autenticación y sesión

- **ASP.NET Core Identity** con email y contraseña.
- **Cookies** de autenticación (HttpOnly, Secure, SameSite=Lax).
- Contraseña mínima: 8 caracteres, sin reglas de composición obligatorias; bloqueo tras 5 intentos fallidos.
- Recuperación de contraseña por email con token de Identity.
- Email transaccional: Brevo o Resend (plan gratuito). Se configura por variables de entorno.
- Verificación de email: no obligatoria en la V1.
- Sin roles ni panel de administración.
- Data Protection keys persistidas en PostgreSQL.

### ¿Por qué no JWT en la app?

La app es un sitio web renderizado en el servidor. Una cookie de sesión es más simple y más segura: no expone tokens al JavaScript del cliente y el navegador la envía automáticamente. JWT se enseña en el módulo de seguridad y se usaría si en el futuro exponemos una API pública o una app móvil.

## 8. Búsqueda

Para la V1:

- Al iniciar, se construye un índice en memoria con título, encabezados y texto plano de cada lección.
- Se normalizan mayúsculas y acentos, y se eliminan palabras vacías del español.
- Ranking simple: coincidencia en título > encabezados > cuerpo.
- Se muestran hasta 10 resultados con un fragmento de contexto.
- La búsqueda corre en el servidor (componente interactivo), sin servicios externos.

Si el contenido crece mucho, se migra a PostgreSQL Full-Text Search o a un índice dedicado.

## 9. SEO y rendimiento

- Renderizado en servidor (SSR) para todo el contenido público.
- Interactividad solo en islas: botón de progreso, marcador, quiz y búsqueda.
- URLs limpias: `/modulos/{modulo}/{leccion}`.
- `<title>`, meta description, canonical y Open Graph por página.
- `sitemap.xml` generado desde el catálogo de contenido.
- `robots.txt` y página 404 personalizada.
- HTML semántico (`header`, `nav`, `main`, `article`, `aside`).
- CSS crítico liviano, sin frameworks pesados; JavaScript mínimo.
- Imágenes con dimensiones explícitas y `loading="lazy"` cuando corresponda.
- Modo oscuro con variables CSS y preferencia guardada, evitando el "flash" inicial.

## 10. Entornos y deploy

| Entorno | App | Base de datos |
|---|---|---|
| Desarrollo | `dotnet run` en local | Rama `dev` de Neon |
| Producción | Render (Docker) | Rama `production` de Neon |

- Secretos en local: `dotnet user-secrets`. Nunca en el repositorio.
- Secretos en producción: variables de entorno de Render.
- Conexión a PostgreSQL: `ConnectionStrings:Default` o `DATABASE_URL`. Se acepta el formato URI de Neon y se convierte al de Npgsql.
- Migrations: se aplican al arrancar solo si `Database__MigrateOnStartup=true` (definido en `render.yaml`); en local se ejecuta `dotnet ef database update` de forma manual.
- Health checks: `/health` (liveness) y `/health/ready` (incluye PostgreSQL).
- Logs: salida estándar, visibles en Render.
- Data Protection keys: en PostgreSQL.
- Una sola instancia en la V1. Si se escala horizontalmente, se necesita sticky session o un backplane para Blazor Server.

### Limitaciones del plan gratuito

- Render duerme el servicio tras un período de inactividad; la primera visita puede tardar.
- Neon suspende la base por inactividad; la primera consulta puede tardar.
- Se acepta para el MVP. Si el proyecto crece, se evalúa un plan pago o Azure.

## 11. Seguridad

- HTTPS obligatorio y HSTS en producción.
- Identity maneja hash de contraseñas y tokens.
- Anti-forgery en formularios.
- Rate limiting en login, registro y recuperación de contraseña.
- Validación de entrada en servidor.
- Cookies seguras y Data Protection persistente.
- Sin secretos en el repositorio ni en logs.
- Dependencias actualizadas y revisadas.
- Contenido Markdown propio y confiable; si algún día se acepta contenido de usuarios, se sanitiza.
- Principios de OWASP Top 10 como checklist.

## 12. Calidad y testing

- `Nullable` habilitado e `ImplicitUsings` habilitado.
- Advertencias tratadas como errores en CI.
- `.editorconfig` y `dotnet format`.
- Tests unitarios con xUnit para:
  - Parseo de front matter y slugs.
  - Cálculo de progreso.
  - Reglas de marcadores.
- Tests de integración con `WebApplicationFactory` para:
  - Registro y login.
  - Marcar progreso.
  - Acceso a páginas protegidas.
- Los ejemplos de código del contenido se compilan y prueban en un proyecto de muestras.
- CI en GitHub Actions: restore, build, test y verificación de formato.

## 13. Riesgos técnicos

| Riesgo | Mitigación |
|---|---|
| Cold start de Render | Aceptarlo en el MVP; evaluar plan pago o keep-alive |
| Pérdida de sesión por reinicios | Data Protection keys en PostgreSQL |
| Blazor Server depende de conexión para islas | Mantener la interactividad al mínimo; el contenido se lee sin conexión activa |
| Crecimiento del contenido | Búsqueda en memoria primero; migrar a Postgres FTS después |
| Migraciones en producción | Aplicarlas como paso controlado del deploy, con backup |
| Límites de Neon | Monitorear almacenamiento y conexiones; el volumen esperado es bajo |

## 14. Registro de decisiones (ADR)

| ID | Decisión | Estado |
|---|---|---|
| ADR-001 | Blazor Web App (SSR + Interactive Server) en lugar de React/Next.js | Aceptada |
| ADR-002 | PostgreSQL (Neon) en lugar de Firebase/Firestore | Aceptada |
| ADR-003 | Contenido en Markdown versionado, sin CMS ni panel de administración | Aceptada |
| ADR-004 | Identity + cookies; JWT solo como tema de contenido | Aceptada |
| ADR-005 | Render gratuito, aceptando cold starts | Aceptada |
| ADR-006 | Monolito modular en un proyecto web, no microservicios | Aceptada |
| ADR-007 | Sin ejecución de código en la nube en la V1 | Aceptada |
| ADR-008 | Búsqueda en memoria en la V1 | Aceptada |
| ADR-009 | Recuperación de contraseña por email desde la V1 | Aceptada |
| ADR-010 | Sin verificación de email obligatoria en la V1 | Aceptada |
