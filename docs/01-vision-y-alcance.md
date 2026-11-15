# Visión y alcance

**Proyecto:** Ruta .NET (nombre provisorio)  
**Carpeta:** `app-curso`  
**Estado:** Fase 0 — Definición  
**Última actualización:** 11/11/2026

## 1. Resumen

Ruta .NET es una plataforma web pública y gratuita, en español, para aprender .NET desde cero hasta un nivel profesional.

El contenido está organizado como una documentación navegable: módulos, lecciones, búsqueda y una ruta sugerida. El objetivo no es solo enseñar el lenguaje, sino las herramientas y prácticas que se usan en un trabajo real: APIs, bases de datos, testing, código limpio, patrones de diseño, seguridad y despliegue.

## 2. El problema

Quien quiere aprender .NET en español suele encontrarse con:

- Material disperso entre videos, blogs, cursos y documentación oficial.
- Contenido mayormente en inglés.
- Cursos que enseñan sintaxis pero no cómo se trabaja en un equipo.
- Falta de una ruta clara: no se sabe qué estudiar primero ni qué es importante.
- Ejemplos desactualizados o desconectados de un proyecto real.
- Plataformas con paywall, distracciones o poca continuidad.

## 3. Público objetivo

- Personas que empiezan desde cero y quieren aprender a programar con .NET.
- Estudiantes de sistemas, análisis de sistemas o carreras afines.
- Autodidactas que buscan cambiar de carrera o conseguir su primer empleo.
- Desarrolladores junior que quieren consolidar bases y aprender buenas prácticas.
- Público hispanohablante, sin importar el país.

No está pensado, en la V1, para desarrolladores senior que buscan temas avanzados muy específicos.

## 4. Propuesta de valor

- **En español y gratuito:** sin paywall ni registro obligatorio para leer.
- **Curado:** una ruta clara, sin obligarte a decidir entre cientos de recursos.
- **De cero a lo laboral:** no se queda en la sintaxis; llega a APIs, EF Core, testing, seguridad, arquitectura y deploy.
- **Práctico:** ejemplos, quizzes y ejercicios con solución.
- **Con continuidad:** cuenta, progreso, marcadores y "continuar donde quedé".
- **Rápido y sin ruido:** lectura cómoda, modo claro/oscuro, sin distracciones.

## 5. Principios del producto

1. **Claridad antes que completitud.** Es mejor explicar bien lo importante que cubrir todo superficialmente.
2. **Una recomendación, con contexto.** Mostramos una forma recomendada de hacer las cosas y explicamos por qué, sin dogmatismo.
3. **Ejemplos reales.** Nada de ejemplos desconectados; cada concepto se conecta con un caso de uso.
4. **Contenido versionado.** El contenido vive en el repositorio, se revisa y se actualiza con el proyecto.
5. **Accesible y rápido.** Funciona en celular, con teclado, con buen contraste y buena velocidad.
6. **Sin distracciones.** Sin publicidad, sin popups, sin gamificación agresiva.
7. **Aprendizaje autónomo.** El usuario decide el ritmo; la plataforma le da estructura y continuidad.

## 6. Alcance de la V1

### Incluido

- Página de inicio con propuesta de valor, ruta sugerida y acceso a los módulos.
- Índice de módulos y página de lección.
- Navegación tipo documentación: barra lateral, anterior/siguiente, índice dentro de la lección.
- Buscador de contenido por título, encabezados y texto.
- Registro, inicio y cierre de sesión con email y contraseña.
- Recuperación de contraseña por email.
- Progreso por lección (vista / completada) y progreso por módulo.
- Marcadores de lecciones.
- "Continuar donde quedé".
- Quizzes de autoevaluación con corrección y explicación.
- Modo claro/oscuro y diseño responsive.
- SEO básico: títulos, descripciones, sitemap, URLs limpias y renderizado en servidor.
- 12 módulos listados; los primeros 4 con contenido completo.
- Deploy público en Render + Neon, con costo $0.

### Fuera de alcance (V1)

- Ejecución de código en la nube o editor integrado.
- Videos.
- Pagos, suscripciones o publicidad.
- Panel de administración o editor de contenido.
- Comentarios, foro o comunidad.
- Certificados o exámenes formales.
- App móvil nativa.
- Multiidioma.
- Tutor con IA.
- Roles y permisos (todos los usuarios son iguales).
- Verificación de email obligatoria.

### V1.1 (primera iteración posterior)

- Verificación de email.
- Historial de quizzes y puntajes.
- Más módulos completos.
- Página de perfil.
- Analítica privacy-friendly.
- Búsqueda con PostgreSQL Full-Text Search si el contenido crece.

## 7. Historias de usuario principales

**Visitante**
- Quiero entender qué ofrece la plataforma en menos de un minuto.
- Quiero explorar los módulos y leer lecciones sin registrarme.
- Quiero buscar un tema puntual y llegar rápido a la lección.
- Quiero registrarme para guardar mi progreso.

**Usuario registrado**
- Quiero iniciar y cerrar sesión de forma segura.
- Quiero marcar una lección como completada.
- Quiero ver cuánto avancé en cada módulo.
- Quiero guardar lecciones en marcadores.
- Quiero volver y continuar donde dejé.
- Quiero responder un quiz y entender mis errores.
- Quiero recuperar mi contraseña si la olvido.
- Quiero leer cómodamente desde el celular y elegir tema claro u oscuro.

**Editor de contenido (nosotros)**
- Quiero escribir lecciones en Markdown y versionarlas con Git.
- Quiero mantener un formato uniforme entre lecciones.
- Quiero publicar contenido nuevo sin tocar la base de datos.

## 8. Métricas de éxito de la V1

- Los 4 primeros módulos están completos, revisados y con quizzes.
- El sitio está publicado y accesible públicamente.
- Un visitante puede registrarse, leer, marcar progreso y volver otro día.
- Lighthouse: SEO y accesibilidad por encima de 90 en las páginas principales.
- Una lección carga en menos de 2 segundos en una conexión razonable.
- Costo operativo: $0.
- Sin vulnerabilidades críticas conocidas.

## 9. Riesgos y supuestos

| Riesgo | Impacto | Mitigación |
|---|---|---|
| El contenido es mucho más trabajo que la app | Alto | Producir por fases, con plantilla y solo 4 módulos completos en V1 |
| Cold starts del hosting gratuito | Medio | Aceptarlo en el MVP; documentarlo; evaluar plan pago o Azure después |
| Cambios de versión de .NET | Medio | Fijar .NET 10 LTS y actualizar de forma planificada |
| Errores o contenido desactualizado | Alto | Compilar y probar los ejemplos; revisión con checklist |
| Competencia de contenido en español | Medio | Enfoque práctico y de calidad, no volumen |
| Dependencia de un solo desarrollador | Medio | Fases chicas, documentación y commits frecuentes |
| Copiar contenido con derechos de autor | Alto | Escribir con palabras propias y citar fuentes |

**Supuestos**

- El contenido será escrito por nosotros y revisado antes de publicarse.
- El tráfico inicial será bajo, compatible con el plan gratuito.
- Los usuarios aceptan crear una cuenta para guardar progreso.
- No se necesita soporte offline.

## 10. Nombre y marca

Nombre provisorio: **Ruta .NET**.

Alternativas a evaluar más adelante: DotNetPath, Aprende .NET, Código .NET, Ruta C#.

El nombre definitivo se decide antes del lanzamiento público; no bloquea el desarrollo.
