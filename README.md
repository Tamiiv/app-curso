# Ruta .NET

Plataforma web pública y gratuita para aprender .NET de cero a nivel profesional, en español.

> **Estado:** Fase 4 completa (13 módulos publicados). Fase 5 — Calidad en curso; deploy público de Fase 1 pendiente.

## Stack

| Pieza | Tecnología |
|---|---|
| Lenguaje | C# / .NET 10 (LTS) |
| Web | ASP.NET Core + Blazor Web App (SSR + Interactive Server) |
| Base de datos | PostgreSQL (Neon) |
| ORM | EF Core + Npgsql |
| Autenticación | ASP.NET Core Identity (cookies) — Fase 3 |
| Contenido | Markdown + Markdig |
| Hosting | Render (plan gratuito) |
| CI | GitHub Actions |

## Estructura

```
app-curso/
├── content/                  # Módulos y lecciones en Markdown
├── docs/                     # Visión, arquitectura, temario y roadmap
├── src/AppCurso.Web/         # Aplicación Blazor
│   ├── Components/           # Layout y páginas
│   ├── Data/                 # DbContext, conexión y migrations
│   ├── Features/Content/     # Catálogo, parser y búsqueda
│   └── wwwroot/              # CSS y JavaScript
├── tests/AppCurso.Tests/     # Tests con xUnit
├── Dockerfile
└── render.yaml
```

## Requisitos

- [.NET SDK 10.0.404 o superior](https://dotnet.microsoft.com/download) (ver `global.json`).
- PostgreSQL opcional para desarrollo. Sin base de datos, la app arranca igual.
- Docker opcional para probar la imagen de producción.

## Correr en local

```bash
dotnet restore
dotnet run --project src/AppCurso.Web --launch-profile http
```

La app queda en `http://localhost:5270`.

### Conectar una base de datos local

Las credenciales se guardan con user-secrets, nunca en el repositorio:

```bash
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5432;Database=ruta_dotnet;Username=postgres;Password=postgres" \
  --project src/AppCurso.Web
```

También se acepta la variable `DATABASE_URL` con el formato de Neon:

```text
postgresql://usuario:password@host/base?sslmode=require
```

La app convierte ese formato al de Npgsql automáticamente.

### Migraciones

El esquema se versiona con EF Core Migrations. Para aplicarlas en local:

```bash
dotnet ef database update --project src/AppCurso.Web
```

En producción se aplican al arrancar si `Database__MigrateOnStartup=true`. El
`render.yaml` ya define esa variable; conviene mantener una sola instancia
durante el arranque para evitar carreras entre migraciones.

## Contenido

Cada módulo vive en `content/<orden>-<slug>/`:

- `module.json`: título, orden, nivel, resumen, estado y temas.
- `NN-<slug>.md`: lecciones con front matter YAML.

El contenido se copia al directorio de salida al compilar y se carga en memoria al iniciar. No se guarda en la base de datos.

## Endpoints

| Ruta | Qué hace |
|---|---|
| `/health` | Liveness: la app responde. |
| `/health/ready` | Readiness: incluye la conexión a PostgreSQL. |

## Tests y formato

```bash
dotnet test AppCurso.slnx
dotnet format AppCurso.slnx --verify-no-changes
```

El CI de GitHub Actions corre restore, build, tests y verificación de formato en cada push y pull request.

## Docker

```bash
docker build -t ruta-dotnet .
docker run --rm -p 8080:8080 -e PORT=8080 ruta-dotnet
```

La imagen escucha en el puerto indicado por la variable `PORT` y expone `8080` por defecto.

## Deploy

1. Crear un proyecto en [Neon](https://neon.tech) y copiar la cadena de conexión.
2. Subir el repositorio a GitHub.
3. En [Render](https://render.com), crear un **Blueprint** y conectar el repositorio. Render lee `render.yaml`.
4. Cargar `DATABASE_URL` como secreto en el servicio.
5. Verificar que `https://<servicio>.onrender.com/health` responda `Healthy`.

Notas del plan gratuito:

- Render suspende el servicio por inactividad; la primera visita puede tardar.
- Neon suspende la base por inactividad; la primera consulta puede tardar.
- Conviene elegir la misma región en Render y Neon para reducir latencia.

## Documentación

- [Visión y alcance](docs/01-vision-y-alcance.md)
- [Arquitectura](docs/02-arquitectura.md)
- [Temario](docs/03-temario.md)
- [Roadmap](docs/04-roadmap.md)
- [Operaciones: deploy, backups y monitoreo](docs/06-operaciones.md)

## Licencia

A definir (código y contenido pueden tener licencias distintas).
