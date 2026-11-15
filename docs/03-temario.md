# Temario

**Proyecto:** Ruta .NET (nombre provisorio)  
**Estado:** Fase 4 — Contenido (en curso)  
**Última actualización:** 15/11/2026

## 1. Cómo se organiza

El contenido se organiza en **módulos** y cada módulo en **lecciones**. Los módulos siguen un orden sugerido, pero la navegación es libre: podés entrar a cualquier lección desde el índice o el buscador.

La V1 tiene **12 módulos** (más un módulo 0 de introducción). Los primeros 4 se escriben completos; los demás quedan estructurados con su página de presentación y el estado "en preparación".

### Estados

- **Completo:** todas las lecciones escritas, con ejemplos y quiz.
- **En preparación:** módulo listado, con introducción y temario, sin lecciones completas.
- **Borrador:** contenido en escritura, no publicado.

## 2. Vista general

| # | Módulo | Nivel | Estado V1 |
|---|---|---|---|
| 0 | Cómo usar la plataforma | — | Completo |
| 1 | Fundamentos de .NET | Principiante | Completo |
| 2 | C# desde cero | Principiante | Completo |
| 3 | Programación orientada a objetos | Principiante | Completo |
| 4 | Colecciones, LINQ y genéricos | Principiante | Completo |
| 5 | Async/await y manejo de errores | Intermedio | En preparación |
| 6 | ASP.NET Core: APIs REST | Intermedio | En preparación |
| 7 | EF Core y bases de datos | Intermedio | En preparación |
| 8 | Testing y calidad | Intermedio | En preparación |
| 9 | Código limpio, SOLID y patrones | Intermedio | En preparación |
| 10 | Seguridad y JWT | Intermedio | En preparación |
| 11 | Arquitectura de aplicaciones | Avanzado | En preparación |
| 12 | Deploy y mundo laboral | Avanzado | En preparación |

## 3. Módulo 0 — Cómo usar la plataforma

**Objetivo:** que el usuario aproveche la plataforma y prepare su entorno.

| # | Lección | Minutos |
|---|---|---|
| 1 | Cómo está organizado el contenido | 5 |
| 2 | Progreso, marcadores y ruta sugerida | 5 |
| 3 | Cómo practicar: entorno local y ejemplos | 8 |

## 4. Módulo 1 — Fundamentos de .NET

**Objetivo:** entender qué es .NET, instalar el entorno y crear, ejecutar y publicar un proyecto.

| # | Lección | Minutos |
|---|---|---|
| 1 | ¿Qué es .NET? Runtime, SDK y ecosistema | 10 |
| 2 | Instalar .NET y verificar el entorno | 10 |
| 3 | La CLI de .NET: `new`, `build`, `run`, `test` | 12 |
| 4 | Anatomía de un proyecto de consola | 12 |
| 5 | NuGet y gestión de dependencias | 12 |
| 6 | Debugging y errores comunes | 15 |
| 7 | Git y GitHub para el día a día | 15 |
| 8 | Publicar una aplicación de consola | 10 |
| 9 | Ejercicio integrador y quiz | 20 |

**Resultado esperado:** el usuario crea una app de consola desde cero, la ejecuta, la depura y la publica.

## 5. Módulo 2 — C# desde cero

**Objetivo:** dominar la sintaxis esencial y resolver problemas con código.

| # | Lección | Minutos |
|---|---|---|
| 1 | Variables, tipos y conversiones | 15 |
| 2 | Operadores y expresiones | 12 |
| 3 | Strings y texto | 12 |
| 4 | Condicionales | 12 |
| 5 | Bucles | 15 |
| 6 | Arrays y listas (introducción) | 15 |
| 7 | Métodos y parámetros | 15 |
| 8 | Nullable reference types | 15 |
| 9 | Entrada y salida por consola | 12 |
| 10 | Ejercicio integrador y quiz | 25 |

**Resultado esperado:** el usuario escribe programas completos de consola con funciones, condicionales, bucles y manejo de nulos.

## 6. Módulo 3 — Programación orientada a objetos

**Objetivo:** modelar problemas con clases, interfaces y composición.

| # | Lección | Minutos |
|---|---|---|
| 1 | Clases y objetos | 15 |
| 2 | Constructores y propiedades | 15 |
| 3 | Encapsulamiento y modificadores de acceso | 15 |
| 4 | Herencia | 15 |
| 5 | Polimorfismo: `virtual` y `override` | 15 |
| 6 | Clases abstractas | 12 |
| 7 | Interfaces | 15 |
| 8 | Composición vs. herencia | 15 |
| 9 | Records y tipos por valor vs. referencia | 15 |
| 10 | Ejercicio integrador y quiz | 25 |

**Resultado esperado:** el usuario diseña un pequeño sistema con clases, interfaces y bajo acoplamiento.

## 7. Módulo 4 — Colecciones, LINQ y genéricos

**Objetivo:** trabajar con datos de forma expresiva y eficiente.

| # | Lección | Minutos |
|---|---|---|
| 1 | Colecciones: `List`, `Dictionary`, `HashSet` | 15 |
| 2 | `IEnumerable` e iteradores | 15 |
| 3 | Genéricos | 15 |
| 4 | Lambdas y delegates | 12 |
| 5 | LINQ: `Where`, `Select`, `OrderBy` | 15 |
| 6 | LINQ: `GroupBy`, `Join`, `Aggregate` | 18 |
| 7 | Ejecución diferida y materialización | 15 |
| 8 | Colecciones inmutables y de solo lectura | 12 |
| 9 | Ejercicio integrador y quiz | 25 |

**Resultado esperado:** el usuario transforma y consulta colecciones con LINQ, entiende cuándo se ejecuta una consulta y elige la colección adecuada.

## 8. Módulos 5 a 12 (estructura)

### Módulo 5 — Async/await y manejo de errores
`Task`, `async`/`await`, `CancellationToken`, `try`/`catch`/`finally`, excepciones personalizadas, logging y patrones de resiliencia.

### Módulo 6 — ASP.NET Core: APIs REST
Minimal APIs, controladores, routing, inyección de dependencias, middleware, validación, códigos de estado, OpenAPI/Swagger y versionado.

### Módulo 7 — EF Core y bases de datos
`DbContext`, entidades, relaciones, migrations, consultas, carga de navegación, transacciones, rendimiento y `AsNoTracking`.

### Módulo 8 — Testing y calidad
xUnit, unitarios vs. integración, mocking, TDD, cobertura, `WebApplicationFactory` y pruebas de APIs.

### Módulo 9 — Código limpio, SOLID y patrones
Nombres, funciones chicas, SOLID, DRY/KISS/YAGNI, patrones creacionales, estructurales y de comportamiento, refactoring.

### Módulo 10 — Seguridad y JWT
Autenticación vs. autorización, hash de contraseñas, estructura de JWT, refresh tokens, OAuth/OIDC, OWASP, secretos, HTTPS y CORS.

### Módulo 11 — Arquitectura de aplicaciones
Capas, arquitectura limpia, hexagonal, monolito modular, microservicios, CQRS, mensajería y criterios para elegir.

### Módulo 12 — Deploy y mundo laboral
Entornos, CI/CD, Docker, cloud, observabilidad, code review, Git flow, metodologías ágiles, portafolio y entrevistas.

## 9. Plantilla de una lección

Cada lección debe respetar esta estructura:

```markdown
# Título

## Objetivo
Qué va a saber hacer el usuario al terminar.

## Requisitos previos
Lecciones o conocimientos necesarios.

## Concepto
Explicación clara, con analogías y sin saltos.

## Ejemplo mínimo
El código más chico que demuestra la idea.

## Ejemplo aplicado
Un caso cercano a un proyecto real.

## Errores comunes
Qué suele salir mal y por qué.

## Buenas prácticas
Qué haría un desarrollador experimentado.

## Ejercicio
Una consigna concreta, con solución plegable.

## Quiz
3 a 5 preguntas de opción múltiple con explicación.

## Resumen
Ideas clave en 3 o 4 bullets.

## Siguiente paso
Enlace a la lección siguiente.
```

### Reglas de estilo

- Explicar en español neutro, con ejemplos claros.
- Una idea por párrafo; párrafos cortos.
- Código con nombres descriptivos y comentarios solo cuando aportan.
- Cada ejemplo debe compilar.
- No usar capturas de pantalla para algo que se puede explicar con texto.
- Incluir advertencias de versión cuando algo cambie entre .NET 8, 9 y 10.

## 10. Formato del quiz

- 3 a 5 preguntas por lección.
- Opción múltiple con una sola respuesta correcta.
- Cada pregunta incluye una explicación que se muestra después de responder.
- En la V1 el quiz es de autoevaluación: no se guarda el puntaje.
- Las preguntas evalúan comprensión, no memoria literal.

## 11. Plan de producción de contenido

| Etapa | Contenido | Estimación |
|---|---|---|
| 1 | Módulo 0 y esqueleto del módulo 1 | 1 semana |
| 2 | Módulo 1 completo | 1-2 semanas |
| 3 | Módulo 2 completo | 2 semanas |
| 4 | Módulo 3 completo | 2 semanas |
| 5 | Módulo 4 completo | 2 semanas |
| 6 | Revisión, quizzes y ejercicios | 1 semana |

Cada lección requiere entre 2 y 4 horas de escritura, ejemplos y quiz. La estimación total de los 4 módulos ronda las **80-120 horas** de contenido.

## 12. Checklist de revisión de una lección

- [ ] El objetivo está claro y se cumple.
- [ ] Los ejemplos compilan y producen el resultado mostrado.
- [ ] No hay errores de ortografía ni de tipeo.
- [ ] Los enlaces internos y externos funcionan.
- [ ] El quiz tiene respuestas y explicaciones correctas.
- [ ] El ejercicio tiene solución.
- [ ] La lección se entiende sin leer las anteriores (o indica qué repasar).
- [ ] El título y la descripción sirven para SEO.
- [ ] El contenido es propio y cita fuentes cuando corresponde.
