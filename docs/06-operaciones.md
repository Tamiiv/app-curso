# Operaciones: deploy, backups y monitoreo

**Estado:** procedimientos definidos; se aplican al crear el entorno de producción (Fase 1 pendiente).

Este documento describe cómo se despliega, se respalda y se monitorea Ruta .NET. Todo lo que dependa de paneles externos se registra acá con el paso concreto, para que no quede solo en la memoria de quien lo configuró.

## Entornos

| Entorno | App | Base de datos | Uso |
|---|---|---|---|
| Local | `dotnet run` | In-memory o Neon `dev` | Desarrollo y tests |
| Producción | Render (Docker) | Neon `main` | Usuarios reales |

- La cadena de conexión se pasa por variable de entorno `ConnectionStrings__Default` (o `DATABASE_URL` según la configuración de Render). Nunca en el repositorio.
- En local se usa `dotnet user-secrets` para esa cadena.
- `Database__MigrateOnStartup=true` solo en el entorno desplegado, y solo mientras el esquema lo controle la propia app.

## Deploy

1. El push a `master` dispara el CI (build, tests y formato).
2. Render reconstruye la imagen desde el `Dockerfile` y arranca el contenedor.
3. Al iniciar, la app aplica migraciones pendientes si `Database__MigrateOnStartup` está activo.
4. Se verifica `GET /health` (proceso vivo) y `GET /health/ready` (base de datos accesible).

### Rollback

- **Código:** desde Render, volver al deploy anterior. Es la vía preferida: es inmediata y no requiere tocar la base.
- **Esquema:** las migraciones no se revierten automáticamente. Si una migración rompe algo, se escribe una migración correctiva hacia adelante. Nunca se ejecuta `database update` hacia atrás en producción con datos reales.
- **Datos:** restaurar desde el punto de recuperación de Neon (ver abajo) en una rama nueva y luego apuntar la app a esa rama.

## Backups

Neon es la fuente de verdad de los datos: usuarios, progreso, marcadores y Data Protection keys.

1. Confirmar en el panel de Neon el plan y la retención de historial (point-in-time recovery) disponible.
2. Si el plan no incluye recuperación a un punto en el tiempo suficiente, programar un `pg_dump` diario.
3. Guardar el dump fuera de Neon (almacenamiento de objetos del proveedor) con fecha en el nombre: `appcurso-YYYY-MM-DD.dump`.
4. Definir retención: 7 diarios y 4 semanales como mínimo.
5. Cifrar el archivo si el destino no es privado.

Comando de referencia para el dump:

```bash
pg_dump "$DATABASE_URL" --format=custom --no-owner --no-privileges \
  --file "appcurso-$(date +%Y-%m-%d).dump"
```

Restauración a una rama nueva:

```bash
createdb --host <host-neon> --username <usuario> appcurso_restore
pg_restore --dbname "postgresql://<usuario>:<clave>@<host>/appcurso_restore" \
  --no-owner --no-privileges appcurso-2026-11-15.dump
```

### Prueba de restauración

Un backup sin probar no es un backup. Una vez por trimestre:

1. Restaurar el último dump en una rama nueva.
2. Apuntar una instancia local a esa rama con `ConnectionStrings__Default`.
3. Verificar que se puede iniciar sesión con un usuario existente y que el progreso aparece.
4. Anotar fecha, duración y resultado en este documento.

| Fecha | Backup usado | Duración | Resultado |
|---|---|---|---|
| — | — | — | Pendiente del primer deploy |

## Monitoreo

### Salud de la app

- Render ejecuta el health check contra `/health`.
- Un monitor externo gratuito (por ejemplo UptimeRobot o Better Stack) consulta `https://<dominio>/health/ready` cada 5 minutos.
- Alertas por email al menos; idealmente también a un canal de chat.

### Logs

- Los logs salen por stdout y Render los captura.
- Nivel por defecto en producción: `Information` para `AppCurso`, `Warning` para `Microsoft.AspNetCore`.
- Buscar periódicamente: `fail:` (errores no controlados), `Unhandled exception`, fallos de autenticación repetidos y timeouts de base de datos.

### Señales de alerta

| Señal | Posible causa | Acción |
|---|---|---|
| `/health/ready` en rojo pero `/health` en verde | Base caída, credenciales vencidas o pool agotado | Revisar Neon y la cadena de conexión |
| Aumento de `401`/`403` en login | Ataque de fuerza bruta o rate limit mal configurado | Revisar logs y el límite por IP |
| Latencia alta en lecciones | Cold start de Render o contenidos grandes | Revisar tamaño de assets y plan de Render |
| Errores 500 tras un deploy | Migración fallida o variable de entorno faltante | Rollback al deploy anterior |

## Secrets

| Secreto | Dónde vive |
|---|---|
| Cadena de conexión | Variables de entorno de Render y user-secrets en local |
| Clave de Data Protection | Persistida en PostgreSQL por la app |
| Token de GitHub | Credential Manager de la máquina, nunca en el repo |

Regla: si un secreto aparece en un archivo versionado, se rota de inmediato, no solo se borra del commit.

## Checklist de lanzamiento

- [ ] CI en verde en `master`.
- [ ] Deploy en Render con `/health` y `/health/ready` en verde.
- [ ] Migraciones aplicadas y verificadas contra PostgreSQL.
- [ ] Registro, login, progreso y marcadores probados en producción.
- [ ] Backups configurados y una restauración de prueba completada.
- [ ] Monitor externo con alerta por email activo.
- [ ] Textos legales publicados.
- [ ] Documentación actualizada.
