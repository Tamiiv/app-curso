---
slug: ci-cd
title: "CI/CD con GitHub Actions"
order: 3
estimatedMinutes: 15
summary: "Automatizá build, tests y publicación de una API .NET con workflows de GitHub Actions, caché de NuGet y secretos bien administrados."
quiz:
  - question: "¿Qué archivo define un workflow de GitHub Actions?"
    options:
      - ".github/workflows/build.yml"
      - ".github/actions/build.yaml"
      - "workflows/build.yml"
      - ".gitlab-ci.yml"
    answer: 0
    explanation: "GitHub Actions lee los workflows desde .github/workflows. La extensión puede ser .yml o .yaml."
  - question: "¿Qué acción oficial instala el SDK de .NET?"
    options:
      - "actions/setup-dotnet@v4"
      - "dotnet/setup@v2"
      - "actions/install-dotnet@v1"
      - "microsoft/sdk@latest"
    answer: 0
    explanation: "actions/setup-dotnet es la acción oficial. Con el parámetro dotnet-version se elige la versión, por ejemplo 10.0.x."
  - question: "¿Para qué sirve cache: true en setup-dotnet?"
    options:
      - "Para cachear la imagen de Docker"
      - "Para reutilizar paquetes NuGet entre ejecuciones"
      - "Para guardar los logs del workflow"
      - "Para publicar el artefacto compilado"
    answer: 1
    explanation: "La opción cache usa el lock file o los archivos de proyecto para cachear el directorio global de NuGet y acelerar el restore."
  - question: "¿Dónde deberían guardarse las claves usadas por el workflow?"
    options:
      - "En el YAML, para que quede documentado"
      - "En un archivo appsettings.json"
      - "En GitHub Secrets o en environments protegidos"
      - "En un comentario del pull request"
    answer: 2
    explanation: "Los secretos se guardan cifrados en GitHub y se exponen al workflow como variables en tiempo de ejecución, nunca en el código."
---

# CI/CD con GitHub Actions

## Objetivo

Automatizar la verificación de cada cambio con build y tests, publicar artefactos y entender cómo separar integración continua de despliegue continuo.

## Requisitos previos

Necesitás el proyecto en un repositorio de GitHub y una solución que compile con `dotnet build`. También conviene tener tests automatizados del módulo de testing y, si vas a construir imágenes, el [Dockerfile](/modulos/deploy-mundo-laboral/docker) de la lección anterior.

## Concepto

La integración continua (CI) ejecuta build y tests cada vez que alguien sube cambios. Si algo se rompe, el equipo lo sabe en minutos y no días después. El despliegue continuo (CD) agrega la publicación automática a un entorno cuando la verificación pasa.

GitHub Actions organiza el trabajo en workflows, definidos en YAML dentro de `.github/workflows`. Un workflow tiene disparadores (`on`), trabajos (`jobs`) y pasos (`steps`). Cada job corre en un runner limpio, por defecto `ubuntu-latest`. Los pasos pueden ser comandos de shell o acciones reutilizables publicadas en el marketplace.

Un punto clave es que los jobs son independientes y corren en paralelo salvo que declares `needs`. Cada uno empieza en una máquina vacía, así que si un job necesita el resultado de otro debe descargarlo como artefacto.

Los secretos se configuran en Settings > Secrets and variables > Actions y se referencian con `${{ secrets.NOMBRE }}`. Nunca se imprimen en los logs. Para despliegues conviene usar environments con reglas de protección, por ejemplo aprobación manual antes de producción.

La caché acelera el restore de NuGet. `actions/setup-dotnet` con `cache: true` detecta los archivos de proyecto y guarda el directorio global de paquetes entre ejecuciones.

Un buen pipeline falla rápido: primero restore, después build con `--no-restore`, luego tests con `--no-build`. Así no se repite trabajo y el error aparece en el paso correcto.

## Ejemplo mínimo

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

permissions:
  contents: read

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
          cache: true

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build -c Release --logger "trx;LogFileName=resultados.trx"

      - name: Publicar resultados
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: resultados-tests
          path: "**/TestResults/*.trx"
```

```text
✓ Restore (12s)
✓ Build (28s)
✓ Test (35s)
✓ Publicar resultados (3s)
```

`if: always()` hace que el artefacto se suba incluso si los tests fallan, que es justo cuando más lo necesitás para diagnosticar.

## Ejemplo aplicado

Un pipeline completo construye la imagen Docker solo si los tests pasan, y la publica en GitHub Container Registry únicamente en `main`.

```yaml
name: Build, test y publicar imagen

on:
  push:
    branches: [main]
    tags: ["v*"]
  pull_request:
    branches: [main]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  verificar:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
          cache: true

      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release

  imagen:
    needs: verificar
    if: github.event_name == 'push'
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4

      - uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - uses: docker/metadata-action@v5
        id: meta
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=sha
            type=ref,event=branch
            type=semver,pattern={{version}}

      - uses: docker/build-push-action@v6
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
```

```text
# En un pull request: solo corre el job "verificar".
# En un push a main: verificar -> imagen, con tags main y sha-<commit>.
# Al crear el tag v1.2.0: además se publica la tag 1.2.0.
```

`GITHUB_TOKEN` es un token temporal que GitHub genera para cada ejecución. No hay que crearlo a mano; solo hay que otorgarle el permiso `packages: write`. El job `imagen` declara `needs: verificar`, así que no se ejecuta si los tests fallaron.

Para un despliegue real, agregá un job con `environment: production`. Ese environment puede exigir revisores, restringir ramas y guardar secretos propios, de modo que el pipeline no pueda publicar sin aprobación.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El workflow no aparece en Actions | El archivo no está en `.github/workflows` o el YAML es inválido | Verificar ruta y sintaxis; revisar la pestaña Actions |
| `dotnet: command not found` | Falta `setup-dotnet` | Agregar `actions/setup-dotnet@v4` antes de los comandos |
| Los tests pasan local y fallan en CI | Dependencias del entorno, cultura o rutas | Ejecutar en Linux, no depender de rutas absolutas ni de zona horaria |
| El job de imagen corre sin tests | Falta `needs: verificar` | Declarar la dependencia entre jobs |
| El push a GHCR devuelve 403 | Faltan permisos o no se hizo login | `permissions: packages: write` y `docker/login-action` |
| Un secreto aparece en los logs | Se imprimió con `echo` o se pasó como argumento visible | Usar variables de entorno y nunca mostrar el valor |

## Buenas prácticas

- Corré build y tests en cada pull request; publicá artefactos solo desde ramas principales o tags.
- Usá `--no-restore` y `--no-build` para no repetir trabajo entre pasos.
- Fijá versiones de acciones (`@v4`, `@v6`) en lugar de usar `@main`.
- Declará `permissions` mínimos y usá environments con aprobación para producción.
- Guardá los resultados de tests como artefactos y mostralos en el resumen del job.

## Ejercicio

Agregá al workflow un job `cobertura` que dependa de `verificar`, corra los tests con `--collect:"XPlat Code Coverage"` y suba la carpeta `TestResults` como artefacto. El job debe ejecutarse aunque los tests fallen. Después explicá por qué no conviene publicar la imagen en un pull request desde un fork.

<details>
<summary>Ver solución</summary>

```yaml
  cobertura:
    needs: verificar
    if: always()
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
          cache: true

      - run: dotnet restore
      - run: dotnet test --no-restore -c Release \
          --collect:"XPlat Code Coverage" \
          --results-directory ./TestResults

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: cobertura
          path: TestResults/**/coverage.cobertura.xml
          retention-days: 7
```

Checklist de verificación:

- El job `cobertura` figura como dependiente de `verificar` en el grafo del workflow.
- Al forzar un test fallido, el artefacto se sube igual.
- `coverage.cobertura.xml` aparece descargable desde la ejecución.
- El workflow no imprime secretos en ningún paso.

Sobre el fork: los pull requests desde forks no reciben secretos por seguridad y el `GITHUB_TOKEN` es de solo lectura. Publicar imágenes desde ahí permitiría a un tercero escribir en tu registro. La práctica correcta es construir y verificar en el PR, y publicar solo después del merge en una rama confiable.

</details>

## Resumen

- CI verifica cada cambio con build y tests; CD publica cuando la verificación pasa.
- Los workflows viven en `.github/workflows` y se componen de jobs y pasos.
- `actions/setup-dotnet` instala el SDK y puede cachear NuGet.
- Los jobs son independientes: se comunican con `needs` y artefactos.
- Los secretos van en GitHub Secrets o environments, nunca en el YAML.

## Siguiente paso

El pipeline ya produce una imagen verificada. Falta elegir dónde ejecutarla. Continuá con [Deploy en la nube](/modulos/deploy-mundo-laboral/cloud).
