---
slug: docker
title: "Docker para .NET"
order: 2
estimatedMinutes: 18
summary: "Escribí un Dockerfile multi-stage para .NET 10, construí una imagen liviana y orquestá la API con base de datos usando Docker Compose."
quiz:
  - question: "¿Por qué se usa un Dockerfile multi-stage?"
    options:
      - "Para ejecutar tests dentro de la imagen final"
      - "Para compilar con el SDK y publicar solo el runtime en la imagen final"
      - "Para tener dos contenedores por aplicación"
      - "Porque el SDK no funciona en Linux"
    answer: 1
    explanation: "La etapa de build usa la imagen del SDK; la etapa final copia solo los binarios publicados sobre la imagen de runtime, mucho más chica y sin herramientas de compilación."
  - question: "¿Qué imagen conviene como base de la etapa final para una API ASP.NET Core?"
    options:
      - "mcr.microsoft.com/dotnet/sdk:10.0"
      - "mcr.microsoft.com/dotnet/runtime:10.0"
      - "mcr.microsoft.com/dotnet/aspnet:10.0"
      - "mcr.microsoft.com/dotnet/runtime-deps:10.0"
    answer: 2
    explanation: "La imagen aspnet incluye el runtime de ASP.NET Core y el servidor Kestrel. La imagen runtime sirve para aplicaciones de consola, no para una API web."
  - question: "¿Qué instrucción copia primero los archivos de proyecto antes del resto del código?"
    options:
      - "COPY . ."
      - "COPY *.csproj ./ y luego restore"
      - "ADD bin/ obj/"
      - "RUN dotnet restore --global"
    answer: 1
    explanation: "Copiar solo los .csproj y ejecutar restore antes de copiar el código aprovecha la caché de capas: si no cambian las dependencias, no se restaura de nuevo."
  - question: "¿Para qué sirve EXPOSE 8080 en el Dockerfile?"
    options:
      - "Publica el puerto en el host automáticamente"
      - "Documenta el puerto que escucha el contenedor"
      - "Abre el firewall de la máquina host"
      - "Configura Kestrel para usar HTTPS"
    answer: 1
    explanation: "EXPOSE es documentación y metadatos de la imagen. Para publicar el puerto hay que usar -p en docker run o ports en compose."
---

# Docker para .NET

## Objetivo

Empaquetar una API ASP.NET Core en una imagen reproducible, entender cada etapa del Dockerfile y levantar la aplicación junto a una base de datos con Docker Compose.

## Requisitos previos

Necesitás una API que compile con `dotnet build` y que lea configuración por variables de entorno, como viste en [Entornos y configuración](/modulos/deploy-mundo-laboral/entornos-configuracion). También tener Docker Desktop o Docker Engine instalado.

## Concepto

Docker empaqueta tu aplicación con su runtime y sus dependencias en una imagen inmutable. Esa imagen se ejecuta como contenedor en cualquier máquina con Docker, sin importar si el host usa Windows, Linux o macOS.

En .NET la práctica estándar es un Dockerfile multi-stage. La primera etapa usa la imagen del SDK, que pesa cientos de megabytes y trae el compilador. Allí se restauran paquetes y se publica la aplicación. La segunda etapa parte de la imagen `aspnet`, copia solo los archivos publicados y define el punto de entrada. El resultado pesa una fracción y no incluye herramientas de build.

El orden de las instrucciones afecta la caché. Docker reutiliza una capa si nada de lo que está antes cambió. Por eso se copian primero los `.csproj`, se ejecuta `dotnet restore` y recién después se copia el resto del código. Si cambiás una línea de C#, no se vuelve a restaurar NuGet.

Las imágenes oficiales de .NET 8 y posteriores escuchan en el puerto 8080 y no ejecutan como root. No hace falta configurar `ASPNETCORE_URLS` manualmente; alcanza con mapear el puerto.

Un `.dockerignore` evita enviar al daemon carpetas enormes o sensibles: `bin`, `obj`, `.git`, `node_modules` y archivos de secretos.

Docker Compose describe varios contenedores como un conjunto. Es la forma más simple de levantar la API con SQL Server, variables de entorno y health checks en un solo comando.

## Ejemplo mínimo

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Cursos.Api.csproj", "./"]
RUN dotnet restore "Cursos.Api.csproj"

COPY . .
RUN dotnet publish "Cursos.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cursos.Api.dll"]
```

```text
docker build -t cursos-api:1.0 .
docker run --rm -p 8080:8080 cursos-api:1.0
Now listening on: http://[::]:8080
```

La etapa `build` compila y publica. La etapa `final` solo recibe el resultado. `UseAppHost=false` evita generar un ejecutable nativo que no necesitás dentro del contenedor.

## Ejemplo aplicado

Una API con EF Core necesita una base de datos. Compose permite levantar ambos servicios, esperar a que SQL Server esté sano y pasar la cadena de conexión como variable de entorno.

```yaml
# compose.yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Default: "Server=sqlserver;Database=Cursos;User Id=sa;Password=${SQL_PASSWORD};TrustServerCertificate=True"
      Jwt__Clave: "${JWT_CLAVE}"
    depends_on:
      sqlserver:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -fsS http://localhost:8080/salud || exit 1"]
      interval: 10s
      timeout: 3s
      retries: 5
      start_period: 20s

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${SQL_PASSWORD}"
    ports:
      - "1433:1433"
    volumes:
      - datos-sql:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -Q 'SELECT 1'"]
      interval: 10s
      timeout: 5s
      retries: 10

volumes:
  datos-sql:
```

```bash
# .env local, nunca versionado
SQL_PASSWORD=Cambiar_Esta_Clave_123!
JWT_CLAVE=clave-de-desarrollo-de-32-caracteres

docker compose up --build
docker compose ps
docker compose logs -f api
```

```text
NAME              STATUS                    PORTS
api-1             Up (healthy)              0.0.0.0:8080->8080/tcp
sqlserver-1       Up (healthy)              0.0.0.0:1433->1433/tcp
```

`depends_on` con `condition: service_healthy` no solo espera a que el contenedor exista: espera a que la base responda. Sin eso, la API suele arrancar antes y falla al conectar.

Si tu API aplica migraciones al arrancar, hacelo con cuidado. En un despliegue con varias réplicas, todas intentarían migrar a la vez. Para producción conviene un paso separado, por ejemplo un job que ejecute `dotnet ef database update` antes de levantar las instancias nuevas.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La imagen pesa más de 1 GB | Se usa la imagen del SDK como etapa final | Copiar el publish a `aspnet:10.0` |
| `dotnet restore` se ejecuta en cada build | Se copia todo el código antes del restore | Copiar solo los `.csproj` primero |
| La API no conecta a SQL Server | Se usó `localhost` dentro del contenedor | Usar el nombre del servicio (`sqlserver`) como host |
| `docker compose up` falla por contraseña | El `.env` no existe o no se cargó | Crear `.env` y verificar el nombre de las variables |
| El contenedor arranca y muere | Ruta o nombre de DLL incorrecto en `ENTRYPOINT` | Confirmar el assembly generado por `dotnet publish` |
| Cambios de código no se reflejan | Caché de capas o imagen vieja | Reconstruir con `docker compose up --build` |

## Buenas prácticas

- Usá multi-stage y una etapa final basada en `aspnet:10.0`, no en el SDK.
- Agregá un `.dockerignore` con `bin`, `obj`, `.git`, `.env` y `node_modules`.
- Fijá versiones de imágenes; evitá `latest` en producción.
- No ejecutes como root ni incluyas secretos en la imagen: inyectalos en runtime.
- Definí health checks y `depends_on` con condición `service_healthy` para ordenar el arranque.

## Ejercicio

Escribí el Dockerfile de una solución con dos proyectos: `Cursos.Api` y `Cursos.Dominio`. La API referencia al dominio. El build debe restaurar solo el `.csproj` de la API, publicar en Release y ejecutar la imagen final en el puerto 8080. Después levantala con Compose junto a SQL Server y verificá `/salud`.

<details>
<summary>Ver solución</summary>

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Cursos.Api/Cursos.Api.csproj", "Cursos.Api/"]
COPY ["Cursos.Dominio/Cursos.Dominio.csproj", "Cursos.Dominio/"]
RUN dotnet restore "Cursos.Api/Cursos.Api.csproj"

COPY . .
WORKDIR /src/Cursos.Api
RUN dotnet publish "Cursos.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cursos.Api.dll"]
```

```text
# .dockerignore
**/bin
**/obj
**/.vs
.git
.env
*.user
```

Checklist de verificación:

- `docker build -t cursos-api:local .` termina sin errores.
- `docker image ls cursos-api` muestra una imagen de menos de 300 MB.
- Cambiar un archivo `.cs` y reconstruir no vuelve a descargar paquetes NuGet.
- `docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development cursos-api:local` responde en `http://localhost:8080/salud`.
- `docker compose up` deja ambos servicios en estado `healthy`.

</details>

## Resumen

- Multi-stage separa compilación de ejecución y reduce tamaño y superficie de ataque.
- Copiar los `.csproj` antes del código aprovecha la caché de capas.
- Las imágenes oficiales de .NET 10 escuchan en 8080 y corren sin privilegios.
- Compose orquesta API y base de datos con variables de entorno y health checks.
- Las migraciones automáticas necesitan cuidado cuando hay varias réplicas.

## Siguiente paso

Ya tenés una imagen reproducible. El próximo paso es generarla automáticamente en cada push. Seguí con [CI/CD con GitHub Actions](/modulos/deploy-mundo-laboral/ci-cd).
