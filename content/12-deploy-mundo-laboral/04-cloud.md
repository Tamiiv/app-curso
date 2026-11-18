---
slug: cloud
title: "Deploy en la nube"
order: 4
estimatedMinutes: 15
summary: "Elegí un servicio de hosting para tu API, configurá variables y secretos, desplegá con contenedores y aplicá un plan de rollback."
quiz:
  - question: "¿Qué ventaja principal ofrece un servicio PaaS frente a una máquina virtual?"
    options:
      - "Cuesta siempre menos que todo lo demás"
      - "La plataforma gestiona sistema operativo, parches y escalado"
      - "Permite acceso root al servidor"
      - "No requiere configuración de aplicación"
    answer: 1
    explanation: "En PaaS la plataforma se encarga del sistema operativo, el runtime y el escalado. Vos solo desplegás la aplicación y su configuración."
  - question: "¿Cómo se pasa una cadena de conexión a un contenedor en Azure App Service?"
    options:
      - "Se escribe en el Dockerfile"
      - "Se configura como app setting y se lee por variables de entorno"
      - "Se sube al repositorio en appsettings.json"
      - "Se incluye en el nombre de la imagen"
    answer: 1
    explanation: "Los app settings se exponen como variables de entorno. Con el separador __ representás secciones de configuración."
  - question: "¿Qué es un deployment slot?"
    options:
      - "Una copia de la aplicación con su propio hostname para probar antes de intercambiar"
      - "Un puerto adicional del servidor"
      - "Una base de datos de solo lectura"
      - "Un tipo de contenedor privilegiado"
    answer: 0
    explanation: "Los slots permiten desplegar una versión nueva, calentarla y luego intercambiarla con producción para reducir el tiempo de indisponibilidad."
  - question: "¿Qué conviene hacer antes de aplicar migraciones automáticamente en producción?"
    options:
      - "Ejecutarlas desde cada réplica al mismo tiempo"
      - "Ejecutarlas como paso único y compatible hacia atrás"
      - "Borrarlas para simplificar"
      - "Desactivar los backups"
    answer: 1
    explanation: "Las migraciones deben ejecutarse una sola vez y ser compatibles con la versión anterior para permitir despliegues sin corte."
---

# Deploy en la nube

## Objetivo

Publicar una API .NET en un servicio administrado, configurar secretos y variables sin tocar la imagen, y planificar actualizaciones y rollback con el menor riesgo posible.

## Requisitos previos

Necesitás una imagen Docker funcionando y un pipeline que la publique, vistos en [Docker](/modulos/deploy-mundo-laboral/docker) y [CI/CD con GitHub Actions](/modulos/deploy-mundo-laboral/ci-cd). También una cuenta en un proveedor cloud y, para los ejemplos, la CLI de Azure.

## Concepto

La nube ofrece tres niveles de control. En infraestructura como servicio (IaaS) alquilás máquinas virtuales y administrás todo. En plataforma como servicio (PaaS) desplegás código o contenedores y el proveedor se ocupa del sistema operativo, los parches y el escalado. En serverless pagás por ejecución y el arranque en frío es parte del trato.

Para una API .NET con contenedor, las opciones más directas son Azure App Service, Azure Container Apps y servicios equivalentes de otros proveedores que aceptan una imagen OCI. Todos comparten la misma idea: la imagen es inmutable y la configuración entra por variables de entorno.

La configuración sensible no debe estar en la imagen. Un app setting como `ConnectionStrings__Default` se inyecta al arrancar y sobreescribe lo que diga `appsettings.json`. Si el proveedor ofrece un gestor de secretos, mejor: la aplicación lee una referencia y el valor nunca aparece en el portal ni en los logs.

Todo deploy necesita un plan de vuelta atrás. Una imagen con tag inmutable permite volver a la versión anterior con un comando. Un tag `latest` no: una vez que apunta a otra imagen, perdiste la referencia exacta.

El escalado horizontal exige que la aplicación sea sin estado. Sesiones en memoria, cachés locales y archivos temporales se rompen cuando hay varias instancias. Los datos compartidos van a una base o a un servicio de caché distribuida.

Las migraciones de base de datos son el punto más delicado. Aplicarlas al arrancar funciona en desarrollo, pero con varias réplicas todas compiten. En producción se ejecutan una sola vez, como un job previo, y deben ser compatibles con la versión anterior durante la transición.

## Ejemplo mínimo

```bash
# Variables locales para no repetir valores
RG=rg-cursos-prod
APP=api-cursos-prod
PLAN=plan-cursos-linux

az group create --name $RG --location eastus

az appservice plan create \
  --name $PLAN \
  --resource-group $RG \
  --is-linux \
  --sku B1

az webapp create \
  --name $APP \
  --resource-group $RG \
  --plan $PLAN \
  --deployment-container-image-name ghcr.io/mi-usuario/cursos-api:sha-abc1234

az webapp config appsettings set \
  --name $APP \
  --resource-group $RG \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Server=tcp:mi-servidor.database.windows.net,1433;Database=cursos;User Id=app;Password=$DB_PASSWORD;Encrypt=True"
```

```text
# La app queda disponible en:
https://api-cursos-prod.azurewebsites.net/salud
{"estado":"ok"}
```

Cada deploy usa un tag distinto, derivado del commit. Ese detalle es lo que hace posible volver atrás sin adivinar qué código estaba corriendo.

## Ejemplo aplicado

Un flujo con slot de staging permite probar la versión nueva contra la configuración de producción y luego intercambiar.

```bash
# 1. Desplegar la imagen nueva en el slot de staging
az webapp deployment slot create \
  --name $APP \
  --resource-group $RG \
  --slot staging

az webapp config container set \
  --name $APP \
  --resource-group $RG \
  --slot staging \
  --docker-custom-image-name ghcr.io/mi-usuario/cursos-api:sha-def5678

# 2. Verificar el slot antes del intercambio
curl -fsS https://api-cursos-prod-staging.azurewebsites.net/salud

# 3. Intercambiar staging con producción
az webapp deployment slot swap \
  --name $APP \
  --resource-group $RG \
  --slot staging \
  --target-slot production

# 4. Rollback: volver a intercambiar
az webapp deployment slot swap \
  --name $APP \
  --resource-group $RG \
  --slot production \
  --target-slot staging
```

```yaml
# Job de despliegue en GitHub Actions
deploy:
  needs: imagen
  if: github.ref == 'refs/heads/main'
  runs-on: ubuntu-latest
  environment: production
  steps:
    - uses: azure/login@v2
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Actualizar imagen
      run: |
        az webapp config container set \
          --name api-cursos-prod \
          --resource-group rg-cursos-prod \
          --docker-custom-image-name ghcr.io/${{ github.repository }}:sha-${{ github.sha }}

    - name: Esperar salud
      run: |
        for i in $(seq 1 20); do
          if curl -fsS https://api-cursos-prod.azurewebsites.net/salud; then
            exit 0
          fi
          sleep 10
        done
        exit 1
```

```text
Waiting for health check...
{"estado":"ok"}
Deploy OK: sha-def5678
```

El `for` con reintentos evita marcar como fallido un deploy que todavía está arrancando. Si tras veinte intentos la API no responde, el job falla y queda claro que hay que revertir.

Para las migraciones, creá un job separado que corra antes del deploy de la imagen nueva:

```bash
az containerapp job create \
  --name migraciones-cursos \
  --resource-group $RG \
  --trigger-type Manual \
  --image ghcr.io/mi-usuario/cursos-api:sha-def5678 \
  --command "dotnet" "ef" "database" "update"
```

No inventes un endpoint público para migrar. Un job con permisos acotados es más fácil de auditar y no queda expuesto a internet.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La app arranca con `Production` y falla la conexión | Falta la cadena o el firewall bloquea al servicio | Configurar el app setting y habilitar acceso desde los servicios de Azure |
| Cada deploy baja la versión anterior sin vuelta | Se usa `latest` como tag | Publicar tags inmutables por commit o versión |
| El intercambio de slot rompe sesiones | Hay estado en memoria | Mover sesiones y caché a servicios compartidos |
| Las migraciones fallan con varias réplicas | Todas intentan migrar a la vez | Ejecutar un job único antes del rollout |
| El contenedor no escucha en el puerto esperado | Se fijó un puerto distinto a 8080 | Configurar `WEBSITES_PORT=8080` o respetar el puerto de la imagen |
| La API tarda en arrancar y el health check falla | `start_period` o reintentos demasiado cortos | Aumentar la ventana de espera en el deploy |

## Buenas prácticas

- Versioná imágenes con tags inmutables y desplegá siempre por tag, nunca por `latest`.
- Mantené los secretos en el gestor del proveedor o en GitHub Environments, no en el repositorio.
- Separá el deploy de las migraciones y ejecutalas una sola vez por versión.
- Usá slots o revisiones para calentar la versión nueva antes de recibir tráfico real.
- Definí presupuesto y alertas de costo desde el primer día.

## Ejercicio

Desplegá la API en un App Service Linux con un slot `staging`. Configurá `ASPNETCORE_ENVIRONMENT=Production` y una cadena de conexión ficticia por app settings, verificá `/salud` en el slot y hacé el swap. Después simulá un rollback con otro swap y escribí el checklist que usarías antes de aprobar el intercambio.

<details>
<summary>Ver solución</summary>

```bash
APP=api-cursos-prod
RG=rg-cursos-prod

# Crear slot
az webapp deployment slot create --name $APP --resource-group $RG --slot staging

# Configuración del slot
az webapp config appsettings set \
  --name $APP --resource-group $RG --slot staging \
  --settings ASPNETCORE_ENVIRONMENT=Production \
             ConnectionStrings__Default="Server=tcp:placeholder.database.windows.net,1433;Database=cursos;User Id=app;Password=PLACEHOLDER;Encrypt=True"

# Verificar
curl -fsS https://$APP-staging.azurewebsites.net/salud

# Swap y rollback
az webapp deployment slot swap --name $APP --resource-group $RG --slot staging --target-slot production
az webapp deployment slot swap --name $APP --resource-group $RG --slot production --target-slot staging
```

Checklist antes del swap:

- El slot responde `/salud` con estado sano.
- Los logs del slot no muestran excepciones ni reintentos de base.
- La versión de la imagen coincide con el commit aprobado.
- Las migraciones ya se aplicaron y son compatibles con la versión anterior.
- Existe un tag anterior identificable para rollback.
- Las alertas de error y latencia están activas.

```text
Swap completado. Producción responde {"estado":"ok"}.
Rollback verificado: la versión anterior vuelve a responder en menos de un minuto.
```

</details>

## Resumen

- PaaS delega sistema operativo, parches y escalado; vos aportás imagen y configuración.
- La configuración sensible entra por variables de entorno o gestor de secretos.
- Los tags inmutables hacen posible un rollback real.
- Los slots permiten probar y calentar antes de intercambiar tráfico.
- Las migraciones se ejecutan una vez y deben ser compatibles con la versión previa.

## Siguiente paso

La aplicación ya está publicada, pero no sabés qué pasa dentro. En la próxima lección vas a instrumentarla con [logs, métricas y health checks](/modulos/deploy-mundo-laboral/observabilidad).
