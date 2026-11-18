---
slug: portafolio
title: "Portafolio de proyectos"
order: 8
estimatedMinutes: 15
summary: "Elegí dos o tres proyectos que demuestren criterio, publicalos con demo y README claro, y evitá el clásico repositorio de tutorial sin identidad."
quiz:
  - question: "¿Cuántos proyectos conviene destacar en un portafolio para una primera búsqueda?"
    options:
      - "Diez o más, para mostrar volumen"
      - "Dos o tres bien terminados"
      - "Uno solo, siempre"
      - "Ninguno, alcanza con el CV"
    answer: 1
    explanation: "Dos o tres proyectos terminados, desplegados y documentados demuestran más que diez ejercicios a medio hacer."
  - question: "¿Qué debe responder primero el README de un proyecto?"
    options:
      - "Qué problema resuelve y cómo ejecutarlo"
      - "La historia completa de cada commit"
      - "Los nombres de todos los archivos"
      - "La biografía del autor"
    answer: 0
    explanation: "El README abre con el problema, la solución y las instrucciones para correr o probar el proyecto. El detalle técnico viene después."
  - question: "¿Qué señal da una demo en vivo con enlace roto?"
    options:
      - "Que el proyecto es muy avanzado"
      - "Descuido en la entrega y falta de verificación"
      - "Que usa microservicios"
      - "Que el autor prioriza el código sobre el deploy"
    answer: 1
    explanation: "Un enlace roto sugiere que nadie verificó lo publicado. Es mejor no incluir demo que incluir una que no funciona."
  - question: "¿Qué hace que un proyecto de práctica se diferencie de un clon de tutorial?"
    options:
      - "Cambiar el nombre del proyecto"
      - "Agregar decisiones propias, datos reales y un problema concreto"
      - "Usar más librerías"
      - "Copiar el README original"
    answer: 1
    explanation: "La diferencia está en el criterio: un dominio propio, restricciones reales, decisiones justificadas y algo que el autor pueda explicar en detalle."
---

# Portafolio de proyectos

## Objetivo

Construir una muestra breve y creíble de tu trabajo, con proyectos desplegados, documentados y defendibles en una entrevista técnica.

## Requisitos previos

Necesitás al menos una API propia y el pipeline funcionando, como viste en [CI/CD con GitHub Actions](/modulos/deploy-mundo-laboral/ci-cd) y [Deploy en la nube](/modulos/deploy-mundo-laboral/cloud). Si todavía no tenés proyecto, elegí uno de los que se proponen abajo.

## Concepto

Un portafolio no es un archivo de ejercicios. Es evidencia de que podés llevar una idea hasta algo que otra persona usa. Por eso pesa más un proyecto chico desplegado que cinco repositorios con el código de un curso sin cambios.

La regla práctica es dos o tres proyectos con identidad propia. Uno puede ser una API completa con autenticación, base de datos y tests. Otro puede profundizar en algo específico: procesamiento en background, integración con un servicio externo o una interfaz web que consuma tu API.

Cada proyecto necesita tres cosas: demo accesible, código legible y README útil. La demo puede ser una URL pública o un video corto con el flujo principal. El código debe compilar y tener tests que se ejecuten con un comando. El README debe permitir que alguien lo levante sin preguntarte nada.

El README se escribe para dos audiencias. La primera quiere entender qué hace el proyecto en treinta segundos. La segunda quiere evaluar cómo lo hiciste: arquitectura, decisiones, tests, deploy. Por eso conviene una sección corta de presentación y otra técnica, en ese orden.

Un proyecto de curso se convierte en proyecto propio cuando le agregás una restricción real. Cambiar el dominio, usar datos que te importen, sumar una regla de negocio que el tutorial no tenía y poder explicar por qué la resolviste así. El entrevistador va a preguntar por las decisiones, no por la cantidad de endpoints.

Cuidá el alcance. Un proyecto terminado con tres funcionalidades vale más que uno ambicioso con veinte a medias. Si el tiempo es limitado, recortá features antes que calidad: tests, manejo de errores y deploy son parte del mínimo.

## Ejemplo mínimo

```markdown
# Cursos API

API para gestionar cursos, lecciones e inscripciones, con autenticación JWT
y panel de administración.

## Demo
https://cursos-demo.ejemplo.com
Usuario de prueba: demo@ejemplo.com / Demo1234!

## Stack
- .NET 10, ASP.NET Core Minimal APIs
- EF Core + SQL Server
- xUnit + Testcontainers
- Docker + GitHub Actions

## Cómo ejecutar
```bash
cp .env.example .env
docker compose up --build
```

La API queda en `http://localhost:8080` y la documentación en `/swagger`.

## Decisiones
- Minimal APIs en lugar de controllers: menos ceremonia para un dominio chico.
- Migraciones como job separado: evita carreras con varias réplicas.
- Tests de integración con Testcontainers: mismo motor que producción.

## Estado
Funcional. Pendiente: notificaciones por correo.
```

```text
Tiempo de lectura: menos de un minuto para entender qué es.
Instrucciones probadas en una máquina limpia.
Credenciales de demo sin permisos destructivos.
```

El README no promete lo que no hay. La sección de estado dice qué falta, y eso genera confianza en lugar de restarla.

## Ejemplo aplicado

Un plan de tres proyectos que cubren distintas capacidades sin repetir lo mismo.

```text
Proyecto 1: API de gestión (curso base)
- CRUD con reglas de negocio, JWT, EF Core, tests, Docker, deploy.
- Demuestra: backend completo y criterio de modelado.

Proyecto 2: Procesador de tareas en background
- Cola de trabajos, reintentos, idempotencia, métricas, health checks.
- Demuestra: async, resiliencia y observabilidad.

Proyecto 3: Integración con un servicio externo
- Consumo de una API pública, caché, manejo de errores y límites de tasa.
- Demuestra: diseño de integraciones y lectura de documentación.
```

```markdown
## Plantilla de README

# Nombre
Una frase con el problema que resuelve.

## Demo
Enlace y credenciales de prueba.

## Capturas o flujo principal
Tres pasos para ver el valor.

## Stack y arquitectura
Tecnologías y un diagrama simple de componentes.

## Cómo ejecutar
Comandos copiables, variables necesarias y puerto.

## Tests
Comando único y qué cubren.

## Decisiones y trade-offs
Tres o cuatro decisiones con su motivo.

## Estado y próximos pasos
Qué funciona y qué falta.
```

```text
Checklist de publicación:
- [ ] El repositorio tiene descripción y topics.
- [ ] El README abre con el problema y la demo.
- [ ] `docker compose up` funciona desde un clon limpio.
- [ ] Los tests corren en CI y el badge está actualizado.
- [ ] No hay secretos ni archivos `.env` versionados.
- [ ] La demo responde y tiene datos de ejemplo.
- [ ] Hay un video de dos minutos como respaldo.
- [ ] El historial de commits muestra trabajo incremental.
```

La demo con credenciales de solo lectura evita que un visitante borre datos. Si el proyecto permite escritura, usá un usuario restringido y un límite de datos por sesión.

Un video de dos minutos es un buen respaldo: muestra el flujo aunque la demo esté caída y obliga a practicar la explicación. Grabarlo también revela fricciones de usabilidad que el código no muestra.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Muchos repositorios incompletos | Se empezó uno nuevo ante cada dificultad | Terminar dos y archivar el resto |
| README que solo dice cómo compilar | Se escribió para uno mismo | Explicar problema, demo y decisiones |
| Demo caída o sin datos | No se verificó después del deploy | Monitorear y sembrar datos de ejemplo |
| Proyecto idéntico al tutorial | Falta dominio y decisiones propias | Cambiar el problema y justificar elecciones |
| Sin tests ni CI | Se priorizó la funcionalidad visible | Agregar un proyecto de tests y un workflow mínimo |
| Credenciales reales en el repo | Se usó `.env` versionado | Rotar y agregar `.env` al `.gitignore` |

## Buenas prácticas

- Elegí un problema que puedas explicar en una frase y defender en detalle.
- Publicá solo proyectos que puedas ejecutar en vivo durante una entrevista.
- Documentá decisiones y trade-offs, no solo instrucciones.
- Mantené los datos de demo separados y con permisos limitados.
- Actualizá enlaces y dependencias antes de cada postulación.

## Ejercicio

Elegí uno de tus proyectos, escribí el README con la plantilla, agregá un `.env.example`, verificá el flujo desde un clon limpio en otra carpeta y grabá un video de dos minutos mostrando el caso de uso principal. Al final, pedile a alguien que intente levantarlo sin ayuda y anotá dónde se trabó.

<details>
<summary>Ver solución</summary>

```bash
# Verificación desde cero
git clone https://github.com/mi-usuario/cursos-api.git /tmp/verificacion
cd /tmp/verificacion
cp .env.example .env
docker compose up --build -d
curl -fsS http://localhost:8080/salud/live
dotnet test
```

```text
{"estado":"Healthy"}
Passed! - Failed: 0, Passed: 42, Skipped: 0, Total: 42
```

```markdown
## Registro de la prueba con otra persona

1. Abrió el README y entendió el propósito: sí.
2. Encontró el comando de arranque: sí, en la sección Cómo ejecutar.
3. Se trabó en: no sabía qué valor poner en JWT__CLAVE.
4. Corrección: el README ahora incluye un valor de ejemplo para desarrollo
   y explica que en producción se genera uno distinto.
5. Segundo intento: levantó el proyecto sin ayuda en 4 minutos.
```

Checklist final:

- El clon limpio funciona sin pasos ocultos.
- `.env.example` lista todas las variables requeridas con valores de ejemplo no sensibles.
- El video cubre login, caso principal y un error controlado.
- El README enlaza la demo, el pipeline y los tests.
- Otro desarrollador completó el arranque sin asistencia.

</details>

## Resumen

- Dos o tres proyectos terminados superan a muchos proyectos a medias.
- Demo, código y README son las tres piezas de cada proyecto.
- El README abre con el problema y la demo, y luego explica decisiones.
- Un dominio propio y restricciones reales diferencian un proyecto de un tutorial.
- Verificar el flujo desde cero evita enlaces y pasos roto.

## Siguiente paso

Con el portafolio listo, queda la conversación que lo pone en valor. Terminá la ruta con [Entrevistas y cierre](/modulos/deploy-mundo-laboral/entrevistas-cierre).
