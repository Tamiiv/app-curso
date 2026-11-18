---
slug: metodologias-agiles
title: "Metodologías ágiles"
order: 7
estimatedMinutes: 12
summary: "Entendé cómo Scrum y Kanban organizan el trabajo de un equipo .NET, escribí historias claras y usá métricas sin convertirlas en vigilancia."
quiz:
  - question: "¿Qué es una historia de usuario bien escrita?"
    options:
      - "Una especificación técnica de 20 páginas"
      - "Una necesidad expresada con rol, objetivo y valor"
      - "Una lista de tareas asignadas por el jefe"
      - "Un diagrama de base de datos"
    answer: 1
    explanation: "La forma habitual es: como rol quiero objetivo para beneficio. Describe necesidad y valor, no la implementación."
  - question: "¿Qué mide el límite de trabajo en curso (WIP) en Kanban?"
    options:
      - "Cuántas personas hay en el equipo"
      - "Cuántos ítems pueden estar en una columna a la vez"
      - "Cuántas horas dura la daily"
      - "Cuántos commits por día se permiten"
    answer: 1
    explanation: "El WIP limita ítems simultáneos en una etapa para reducir cambios de contexto y terminar antes lo empezado."
  - question: "¿Cuál es el propósito de la retrospectiva?"
    options:
      - "Reportar horas al cliente"
      - "Identificar mejoras concretas del proceso y acordar acciones"
      - "Asignar culpas por los errores"
      - "Estimar el próximo trimestre"
    answer: 1
    explanation: "La retro es un espacio del equipo para revisar cómo trabajó y elegir pocas acciones de mejora verificables."
  - question: "¿Qué significa que una historia esté lista según la Definition of Ready?"
    options:
      - "Que ya está implementada"
      - "Que tiene contexto, criterios de aceptación y dependencias resueltas"
      - "Que fue aprobada por el cliente final"
      - "Que tiene código escrito"
    answer: 1
    explanation: "Una historia lista se puede estimar y empezar sin bloqueos: objetivo claro, criterios verificables y dependencias identificadas."
---

# Metodologías ágiles

## Objetivo

Entender cómo se planifica y se sigue el trabajo en un equipo de software, escribir historias verificables y elegir el marco que mejor se adapta al contexto.

## Requisitos previos

Necesitás haber trabajado con ramas, pull requests y un pipeline de CI, vistos en [Code review y flujo con Git](/modulos/deploy-mundo-laboral/code-review-git). Esa base técnica es la que las metodologías ordenan.

## Concepto

Agile no es una ceremonia ni una herramienta. Es un conjunto de principios: entregar valor en iteraciones cortas, responder al cambio y colaborar de cerca con quien usa el producto. Los marcos concretos son formas de aplicar esos principios.

Scrum organiza el trabajo en sprints de una a cuatro semanas. Hay un product owner que prioriza, un equipo que construye y un scrum master que facilita. El sprint empieza con planificación, tiene una daily breve y termina con review y retrospectiva. El resultado es un incremento potencialmente desplegable.

Kanban no impone iteraciones. Hace visible el flujo en un tablero con columnas y limita el trabajo en curso. Si hay demasiados ítems en revisión y pocos en desarrollo, el tablero lo muestra. La métrica central es el tiempo de ciclo: cuánto tarda un ítem desde que se empieza hasta que se entrega.

En la práctica muchos equipos combinan ambos: sprints con tablero y límites de WIP. Lo importante no es el nombre del marco sino que el trabajo sea visible, las prioridades claras y las interrupciones controladas.

Una historia de usuario describe una necesidad, no una solución técnica. El formato clásico es: como rol, quiero objetivo, para beneficio. Los criterios de aceptación la vuelven verificable. Sin criterios, la historia no está lista y la discusión se traslada al momento de la implementación, cuando es más cara.

La Definition of Done es el acuerdo de calidad del equipo. Incluye tests, review, documentación y deploy en staging, por ejemplo. Una historia no está terminada porque el código compila: está terminada cuando cumple la DoD.

Las estimaciones son relativas. Los puntos de historia comparan tamaño y complejidad, no horas. Sirven para planificar capacidad y detectar desvíos, no para medir productividad individual.

## Ejemplo mínimo

```markdown
# Historia: descuento por volumen

Como cliente mayorista
quiero que el descuento se aplique automáticamente desde 11 unidades
para no tener que solicitarlo por correo.

## Criterios de aceptación
- Con 10 unidades o menos, el descuento es 0.
- Desde 11 unidades, el descuento es del 10 %.
- El total se redondea a dos decimales.
- El descuento aparece discriminado en el detalle del pedido.

## Fuera de alcance
- Descuentos por categoría.
- Cupones manuales.

## Notas técnicas
Impacta en PedidoService y en el reporte de ventas.
```

```text
Estimación del equipo: 3 puntos.
Riesgo: el reporte de ventas también calcula totales y podría quedar inconsistente.
Acción: incluir una tarea de verificación del reporte en la misma historia.
```

La historia tiene valor, criterios y límites. El equipo puede estimarla y probarla sin adivinar. El riesgo detectado durante el refinamiento evita una sorpresa en producción.

## Ejemplo aplicado

Un tablero Kanban con límites de WIP y una política explícita para cada columna.

```text
Backlog | Listo (5) | En curso (2) | Review (2) | Staging (3) | Hecho
--------|-----------|--------------|------------|-------------|------
#142    | #150      | #151         | #149       | #147        | #140
#143    | #152      | #153         | #148       |             | #141
#144    |           |              |            |             |
#145    |           |              |            |             |
#146    |           |              |            |             |
```

```markdown
## Políticas de columna

Listo: historia con criterios de aceptación y dependencias resueltas.
En curso: máximo 2. Una persona no toma un ítem nuevo si ya tiene uno sin terminar.
Review: máximo 2. El PR tiene menos de 400 líneas y CI verde.
Staging: máximo 3. Incluye prueba manual contra datos de staging.
Hecho: cumple la Definition of Done y está desplegado en producción.
```

```text
Métricas de la última semana:
- Tiempo de ciclo promedio: 4,2 días.
- Tiempo de espera en Review: 1,1 días (subió desde 0,4).
- Ítems terminados: 9.

Acción de la retro: reducir el tamaño de los PR para bajar la espera en Review.
```

El tablero muestra dónde se acumula el trabajo. Si Review es el cuello de botella, agregar desarrolladores no ayuda: hay que revisar más rápido o en pares. Esa lectura es más valiosa que cualquier reporte de horas.

En Scrum, el equivalente sería revisar el burn-down del sprint y ajustar el alcance en la daily, sin esperar al final. La retro elige una o dos acciones concretas y verificables para el próximo sprint, no una lista de deseos.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La daily se convierte en reporte de estado | Se habla para el jefe, no para el equipo | Enfocar en bloqueos y coordinación, no en justificar horas |
| Historias de 13 puntos que duran un sprint entero | No se dividieron | Partir por flujo o por regla de negocio |
| El tablero no refleja la realidad | Se actualiza una vez por semana | Mover ítems al cambiar de estado, no al final del día |
| Sprint sin incremento desplegable | Se planificó de más o hay dependencias externas | Reducir alcance y resolver dependencias antes |
| Métricas usadas para evaluar personas | Se confunde gestión con vigilancia | Medir flujo del sistema, nunca individuos |
| La retro no genera cambios | Se eligen demasiadas acciones | Una o dos acciones con responsable y fecha |

## Buenas prácticas

- Escribí criterios de aceptación verificables antes de empezar.
- Limitá el trabajo en curso: terminar es más valioso que empezar.
- Hacé visible el bloqueo en el tablero el mismo día que aparece.
- Usá la retro para acordar pocas acciones y revisarlas en la siguiente.
- Medí tiempo de ciclo y espera, no horas por persona.

## Ejercicio

Tomá una funcionalidad de tu portafolio, escribí la historia con criterios de aceptación, dividila en al menos dos ítems más chicos y armá un tablero con columnas y límites de WIP. Después registrá durante tres días en qué columna pasa más tiempo cada ítem y proponé una acción de mejora.

<details>
<summary>Ver solución</summary>

```markdown
# Historia original (demasiado grande)
Como administrador quiero gestionar cursos completos.

# División
1. Como administrador quiero crear un curso con título y descripción
   para publicarlo en el catálogo.
   Criterios: título obligatorio de 5 a 80 caracteres, estado inicial Borrador, 409 si el título existe.

2. Como administrador quiero publicar un curso en borrador
   para que aparezca en el catálogo público.
   Criterios: solo se publica si tiene al menos una lección,
   el catálogo lo muestra en menos de un minuto, se registra fecha de publicación.
```

```text
Tablero:
Backlog | Listo (3) | En curso (1) | Review (1) | Hecho
#1      | #2        | #1           | -          | -

Límites: En curso 1, Review 1.
```

```text
Registro de tres días:
Día 1: #1 pasa a En curso.
Día 2: #1 pasa a Review y espera todo el día sin revisor.
Día 3: #1 vuelve a En curso por un comentario; se corrige y se mergea.

Observación: el mayor tiempo se fue en espera de review (1,5 días).
Acción: acordar una ventana diaria de review de 30 minutos
y no abrir un ítem nuevo hasta revisar el que está en Review.
```

Checklist:

- La historia tiene criterios que se pueden probar con un test.
- Los ítems divididos se pueden terminar en menos de dos días.
- El tablero tiene límites explícitos.
- La acción de mejora tiene responsable y fecha de revisión.

</details>

## Resumen

- Agile prioriza entregas cortas, colaboración y respuesta al cambio.
- Scrum organiza en sprints; Kanban optimiza el flujo con límites de WIP.
- Una historia describe necesidad y valor, con criterios verificables.
- La Definition of Done fija el estándar de calidad del equipo.
- Las métricas sirven para mejorar el sistema, no para vigilar personas.

## Siguiente paso

Ya sabés cómo se trabaja en equipo y cómo se entrega. Falta mostrar ese trabajo. Continuá con [Portafolio de proyectos](/modulos/deploy-mundo-laboral/portafolio).
