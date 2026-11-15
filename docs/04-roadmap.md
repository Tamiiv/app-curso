# Roadmap

**Proyecto:** Ruta .NET (nombre provisorio)  
**Estado:** Fase 5 — Calidad (en curso); deploy de Fase 1 pendiente  
**Última actualización:** 15/11/2026

## Cómo leer este roadmap

Cada fase termina con algo **demostrable**: una URL, una funcionalidad o un conjunto de contenido. No avanzamos a la siguiente hasta cumplir los criterios de aceptación.

Las estimaciones suponen dedicación casi completa y son orientativas. El contenido suele llevar más tiempo que el código.

---

## Fase 0 — Definición

**Estado:** completada.

**Entregables**

- Visión y alcance.
- Arquitectura y stack.
- Temario.
- Roadmap.
- Decisiones registradas.

**Criterio de aceptación:** los documentos están revisados y aprobados.

---

## Fase 1 — Esqueleto desplegable

**Estado:** implementación local completa, incluidas las migraciones de EF Core; falta crear Neon, desplegar en Render y verificar la URL pública.

**Objetivo:** tener una aplicación mínima funcionando y publicada, para validar el camino completo: código → base de datos → deploy.

**Tareas**

1. Crear la solución y el proyecto `AppCurso.Web` con Blazor Web App (.NET 10).
2. Configurar `Nullable`, `ImplicitUsings`, `.editorconfig` y `Directory.Build.props`.
3. Armar el layout base: header, barra lateral, contenido, footer y diseño responsive.
4. Crear la página de inicio provisional.
5. Cargar 2 lecciones de prueba en Markdown y renderizarlas.
6. Crear el proyecto de tests y un primer test.
7. Configurar la rama `dev` de Neon y la conexión con EF Core.
8. Agregar health check `/health`.
9. Crear el `Dockerfile` y desplegar en Render.
10. Configurar GitHub Actions para build y tests.

**Criterio de aceptación**

- La app se ve bien en celular y escritorio.
- Se pueden leer las 2 lecciones de prueba.
- La URL pública de Render responde.
- `/health` responde correctamente.
- El CI pasa en verde.

**Estimación:** 3 a 5 días.

---

## Fase 2 — Lectura y navegación

**Estado:** completada en local.

**Objetivo:** que la plataforma se sienta como una documentación real.

**Tareas**

1. Implementar el catálogo de contenido: módulos, lecciones, orden y slugs.
2. Parsear front matter y renderizar Markdown con Markdig.
3. Resaltado de sintaxis y botón "copiar" en bloques de código.
4. Página de módulo con listado de lecciones y progreso estimado.
5. Página de lección con navegación anterior/siguiente e índice interno.
6. Buscador en memoria con resultados y fragmentos.
7. Modo claro/oscuro sin "flash" inicial.
8. SEO: títulos, descripciones, canonical, Open Graph, `sitemap.xml`, `robots.txt` y 404.
9. Página de inicio definitiva con propuesta de valor y ruta sugerida.
10. Estados "en preparación" para los módulos 5 a 12.

**Criterio de aceptación**

- Se navegan todos los módulos y lecciones sin errores.
- La búsqueda encuentra por título, encabezado y contenido.
- El modo oscuro se mantiene al navegar.
- Lighthouse SEO y accesibilidad por encima de 90 en inicio y lección.
- La experiencia es cómoda en celular.

**Estimación:** 1 a 2 semanas.

---

## Fase 3 — Cuentas y progreso

**Estado:** implementada en local. Falta verificarla contra PostgreSQL en un deploy real.

**Objetivo:** que el usuario pueda crear una cuenta y retomar su aprendizaje.

**Tareas**

1. Integrar ASP.NET Core Identity con EF Core.
2. Páginas de registro, login y logout.
3. Recuperación de contraseña con proveedor de email gratuito.
4. Persistir Data Protection keys en PostgreSQL.
5. Modelo y servicio de progreso: lección vista y completada.
6. Progreso por módulo y barra de avance.
7. "Continuar donde quedé".
8. Marcadores de lecciones.
9. Rate limiting en endpoints de autenticación.
10. Mensajes y estados de UI para usuarios invitados.

**Criterio de aceptación**

- Un usuario se registra, inicia sesión y cierra sesión.
- Puede marcar lecciones como completadas y ver su avance.
- Al volver, continúa donde dejó.
- Los marcadores persisten.
- La recuperación de contraseña envía un email real.
- La sesión sobrevive a un redeploy.

**Estimación:** 1 a 2 semanas.

---

## Fase 4 — Contenido de los módulos 1 a 4

**Estado:** completada en local. Módulos 0 a 4 publicados, con ejercicios y quizzes.

**Objetivo:** completar el contenido que hace valiosa la plataforma.

**Tareas**

1. Escribir el módulo 0 (cómo usar la plataforma).
2. Escribir el módulo 1 (fundamentos de .NET).
3. Escribir el módulo 2 (C# desde cero).
4. Escribir el módulo 3 (POO).
5. Escribir el módulo 4 (colecciones, LINQ y genéricos).
6. Crear quizzes y ejercicios con solución.
7. Compilar y probar todos los ejemplos en un proyecto de muestras.
8. Revisar cada lección con el checklist.
9. Ajustar navegación y enlaces internos.

**Criterio de aceptación**

- Los 4 módulos están completos y publicados.
- Todos los ejemplos compilan.
- Todos los quizzes tienen respuesta y explicación.
- El checklist de revisión está completo.
- Un principiante puede seguir la ruta sin quedar trabado.

**Estimación:** 4 a 6 semanas.

---

## Fase 5 — Calidad y lanzamiento

**Estado:** en curso. Tests unitarios de contenido, progreso y marcadores, más smoke tests e integración de registro, login y logout, completados. Accesibilidad base y cabeceras de seguridad aplicadas; faltan auditoría con lector de pantalla, Lighthouse y backups.

**Objetivo:** publicar una V1 sólida y presentable.

**Tareas**

1. Tests unitarios de contenido, progreso y marcadores.
2. Tests de integración de registro, login y progreso.
3. Auditoría de accesibilidad con teclado y lector de pantalla.
4. Auditoría de rendimiento y tamaño de assets.
5. Revisión de seguridad y dependencias.
6. Configurar backups y monitoreo básico.
7. Revisar textos legales: privacidad, términos y licencia.
8. Dominio propio (opcional).
9. Analítica privacy-friendly (opcional).
10. Lanzamiento y difusión.

**Criterio de aceptación**

- CI en verde y tests pasando.
- Lighthouse por encima de 90 en las páginas principales.
- Sin vulnerabilidades críticas conocidas.
- El sitio está publicado, estable y con backups.
- La documentación está actualizada.

**Estimación:** 1 semana.

---

## Fase 6 — Futuro (post-V1)

Ideas para después del lanzamiento, en orden tentativo:

1. Módulos 5 a 12 completos.
2. Verificación de email.
3. Historial de quizzes y puntajes.
4. Página de perfil y preferencias.
5. Certificados de finalización.
6. Comunidad o comentarios.
7. Ejecución de código en la nube.
8. App móvil o PWA.
9. Multiidioma.

No se implementa nada de esta fase sin evaluar costo, valor y mantenimiento.

---

## Forma de trabajo

### Ciclo de cada fase

1. Revisar y ajustar las tareas de la fase.
2. Implementar en commits chicos.
3. Probar localmente.
4. Actualizar la documentación si cambió una decisión.
5. Desplegar y verificar.
6. Demostrar y cerrar la fase.

### Definition of Done

Una tarea está terminada cuando:

- El código compila sin advertencias.
- Tiene tests cuando aporta valor.
- Funciona en local y en producción.
- La documentación refleja el cambio.
- No deja secretos ni archivos temporales en el repositorio.

### Convenciones

- Commits en formato Conventional Commits (`feat:`, `fix:`, `docs:`, `test:`, `chore:`).
- Ramas cortas por funcionalidad.
- `master` siempre desplegable (el repositorio usa `master`; el CI también acepta `main`).
- Los cambios de alcance se anotan en `docs/`.

---

## Próximo paso

Terminar la Fase 1:

1. Crear el proyecto y la rama `dev` en Neon.
2. Cargar `DATABASE_URL` en local con user-secrets, ejecutar `dotnet ef database update` y verificar `/health/ready`.
3. Subir el repositorio a GitHub y conectar Render con `render.yaml`.
4. Verificar la URL pública, el health check y que `Database__MigrateOnStartup` aplique el esquema.
5. Confirmar que el CI pasa en verde.

Después se continúa la Fase 5 con auditoría manual de accesibilidad con lector de pantalla, Lighthouse, backups y monitoreo.
