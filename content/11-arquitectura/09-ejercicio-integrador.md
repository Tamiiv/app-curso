---
slug: ejercicio-integrador
title: "Ejercicio integrador: diseñar un sistema"
order: 9
estimatedMinutes: 30
summary: "Diseñá una aplicación completa: elegí la estructura, definí fronteras, justificá la comunicación entre módulos y documentá las decisiones con ADRs."
quiz:
  - question: "¿Cuál es el primer paso al diseñar la arquitectura de un sistema?"
    options:
      - "Elegir microservicios"
      - "Entender el dominio, los casos de uso y las restricciones del equipo"
      - "Escoger la base de datos"
      - "Crear un diagrama de despliegue"
    answer: 1
    explanation: "La arquitectura responde a un contexto: reglas del negocio, volumen, equipo y plazos. Sin eso, cualquier estructura es una suposición."
  - question: "¿Qué justifica extraer un módulo a un servicio independiente?"
    options:
      - "Que el módulo tenga muchas clases"
      - "Una necesidad operativa: escalar, desplegar o fallar de forma independiente"
      - "Que use otra base de datos"
      - "Que el equipo prefiera otro lenguaje"
    answer: 1
    explanation: "La separación física tiene un costo alto; debe resolver un problema operativo real."
  - question: "¿Cómo se protege una frontera entre módulos en un monolito modular?"
    options:
      - "Con documentación informal"
      - "Con contratos públicos y tests de arquitectura que detectan accesos indebidos"
      - "Con contraseñas por módulo"
      - "Con ramas separadas en Git"
    answer: 1
    explanation: "La frontera se sostiene con reglas verificables automáticamente, no con buenas intenciones."
  - question: "¿Qué debe incluir el diseño final de un sistema?"
    options:
      - "Solo el diagrama de clases"
      - "Contexto, módulos, contratos, decisiones, riesgos y plan de evolución"
      - "El código completo"
      - "La lista de todos los patrones usados"
    answer: 1
    explanation: "Un diseño útil explica el porqué, las fronteras, los riesgos y cómo podría cambiar."
---

# Ejercicio integrador: diseñar un sistema

## Objetivo

Diseñar una aplicación desde cero aplicando lo visto en el módulo: capas y arquitectura limpia, fronteras modulares, comunicación entre piezas, CQRS donde aporte y ADRs para las decisiones.

## Requisitos previos

Haber recorrido el módulo: [Capas](/modulos/arquitectura/capas), [Arquitectura limpia](/modulos/arquitectura/arquitectura-limpia), [Hexagonal](/modulos/arquitectura/hexagonal), [Monolito modular](/modulos/arquitectura/monolito-modular), [Microservicios](/modulos/arquitectura/microservicios), [Comunicación](/modulos/arquitectura/comunicacion-servicios), [CQRS](/modulos/arquitectura/cqrs) y [ADRs](/modulos/arquitectura/decisiones-adr).

## Concepto

El integrador no escribe todo el código: diseña y justifica. El escenario:

> Una plataforma de cursos en línea vende cursos, gestiona inscripciones y emite certificados. Hay un equipo de cinco personas, un solo pipeline y un proveedor cloud con base gestionada. El catálogo se lee mucho más de lo que se escribe. La emisión de certificados tiene requisitos de auditoría. Se espera crecer, pero no hay fecha ni volumen que justifiquen varios equipos todavía.

El diseño debe responder:

1. ¿Qué módulos existen y cuáles son sus fronteras?
2. ¿Qué contratos cruzan esas fronteras?
3. ¿Qué se comunica de forma síncrona y qué de forma asíncrona?
4. ¿Dónde aporta CQRS y dónde sería ceremonia?
5. ¿Qué decisiones se documentan y cuáles quedan abiertas?

Un buen diseño no es el más complejo: es el que resuelve el problema con el menor costo total, incluyendo el costo de cambiarlo después.

## Ejemplo mínimo

Punto de partida: módulos y responsabilidades.

```text
Plataforma de cursos (monolito modular)

┌─────────────┐  ┌──────────────┐  ┌───────────────┐
│ Catálogo    │  │ Inscripciones│  │ Certificados  │
│ cursos      │  │ alumnos      │  │ auditoría     │
└──────┬──────┘  └──────┬───────┘  └───────┬───────┘
       │                │                  │
       └──── contratos internos ───────────┘
```

```text
Catálogo
  - Publica cursos, precios y disponibilidad.
  - Contrato: ICursoReader (consulta), ICursoAdmin (escritura).

Inscripciones
  - Crea inscripciones y controla cupos.
  - Contrato: IInscripcionService.

Certificados
  - Emite certificados tras completar un curso.
  - Consume el evento CursoCompletado.
```

Cada módulo tiene su esquema, su `DbContext` y su API pública. `Certificados` no lee las tablas de `Inscripciones`: recibe un evento con los datos mínimos.

## Ejemplo aplicado

Contratos, eventos y decisiones documentadas:

```csharp
namespace AppCurso.Modules.Catalogo.Contracts;

public sealed record CursoResumen(Guid Id, string Titulo, decimal Precio, bool Publicado);

public interface ICursoReader
{
    Task<CursoResumen?> ObtenerAsync(Guid cursoId, CancellationToken cancellationToken);
}
```

```csharp
namespace AppCurso.Modules.Inscripciones.Contracts;

public sealed record CursoCompletado(Guid InscripcionId, Guid CursoId, Guid AlumnoId, DateTimeOffset CompletadoEn);

public interface IInscripcionService
{
    Task<Guid?> InscribirAsync(Guid cursoId, Guid alumnoId, CancellationToken cancellationToken);
}
```

```csharp
// Certificados consume el evento. No conoce el modelo de Inscripciones.
public sealed class EmitirCertificadoHandler(
    CertificadosDbContext db,
    ILogger<EmitirCertificadoHandler> logger)
{
    public async Task HandleAsync(CursoCompletado evento, CancellationToken cancellationToken)
    {
        var existente = await db.Certificados
            .AnyAsync(c => c.InscripcionId == evento.InscripcionId, cancellationToken);

        if (existente)
        {
            logger.LogInformation("Certificado ya emitido para {InscripcionId}", evento.InscripcionId);
            return;
        }

        db.Certificados.Add(new Certificado(
            Guid.NewGuid(),
            evento.InscripcionId,
            evento.CursoId,
            evento.AlumnoId,
            DateTimeOffset.UtcNow));

        await db.SaveChangesAsync(cancellationToken);
    }
}
```

```text
POST /inscripciones           → síncrono, responde con el id
Curso completado              → evento interno
GET /certificados/{alumnoId}  → consulta con proyección

Fallo al emitir certificado → reintento idempotente, no bloquea la inscripción
```

Y el ADR que registra la decisión principal:

```markdown
# 0001. Monolito modular para la plataforma de cursos

- **Estado:** aceptada
- **Fecha:** 2026-05-20

## Contexto

Equipo de cinco personas, un pipeline, tres áreas funcionales y
transacciones entre inscripción y cupos. No hay equipos ni volúmenes
que exijan despliegues independientes.

## Decisión

Monolito modular con módulos Catálogo, Inscripciones y Certificados,
contratos internos explícitos y eventos para efectos secundarios.

## Alternativas consideradas

1. **Microservicios.** Descartado: costo operativo y transacciones
   distribuidas sin necesidad actual.
2. **Monolito sin módulos.** Descartado: el acoplamiento ya es visible.

## Consecuencias

- Un despliegue y una base con esquemas separados.
- Certificados se procesa por evento: tolera fallos sin bloquear la inscripción.
- Extraer Certificados a servicio es posible si la auditoría lo exige.
- Se revisa si el equipo se divide o si un módulo necesita escalar solo.
```

```text
Riesgos identificados:
- Crecimiento del módulo de Inscripciones bajo picos de matrícula.
  Mitigación: CQRS ligero en lecturas y caché del catálogo.
- Emisión de certificados duplicada.
  Mitigación: idempotencia por InscripcionId.
```

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Empezar por la tecnología | Se eligió base o framework antes del dominio | Modelar casos de uso y fronteras primero |
| Microservicios sin necesidad operativa | Moda | Monolito modular y extracción posterior |
| Módulos que comparten tablas | Atajo | Contratos y esquemas separados |
| CQRS en todas partes | Se aplicó el patrón por defecto | Usarlo donde lectura y escritura difieren |
| Diseño sin decisiones escritas | Todo en la cabeza del arquitecto | ADRs con contexto y consecuencias |
| Diagramas sin justificación | Se dibujó la caja feliz | Explicar por qué cada frontera existe |
| Ignorar los riesgos | Se documentó solo lo que funciona | Incluir riesgos y plan de evolución |

## Buenas prácticas

- Empezá por el dominio y las restricciones, no por el diagrama de despliegue.
- Definí fronteras por capacidad de negocio.
- Un contrato por necesidad, con datos mínimos.
- Síncrono para lo que necesita respuesta; asíncrono para efectos.
- Documentá decisiones y riesgos en el repositorio.
- Dejá explícito qué cambiaría la decisión.

## Ejercicio

Diseñá la arquitectura de la plataforma de cursos con:

1. Tres módulos y sus contratos públicos.
2. Un flujo síncrono y uno asíncrono justificados.
3. Un punto donde CQRS ligero aporte y otro donde no.
4. Dos ADRs: estructura general y comunicación de certificados.
5. Una lista de riesgos con su mitigación.

<details>
<summary>Ver solución</summary>

```text
Módulos:
- Catalogo: cursos, precios, publicación. Contrato ICursoReader.
- Inscripciones: matrícula y cupos. Contrato IInscripcionService.
- Certificados: emisión y auditoría. Consume CursoCompletado.

Flujo síncrono:
POST /inscripciones → Inscripciones valida cupo con ICursoReader
y responde 201. Es síncrono porque el usuario necesita saber si quedó inscripto.

Flujo asíncrono:
Curso completado → evento → Certificados emite el certificado.
Es asíncrono porque la emisión puede reintentarse y no debe bloquear
la finalización del curso.

CQRS:
- Aporta en el catálogo: lectura frecuente, proyección con precio y disponibilidad.
- No aporta en certificados: se leen igual que se escriben, volumen bajo.

ADRs:
- 0001 Monolito modular (estructura).
- 0002 Certificados por evento (comunicación).

Riesgos:
- Pico de matrícula → CQRS de lectura y caché.
- Certificado duplicado → idempotencia por InscripcionId.
- Crecimiento del equipo → evaluar extraer Certificados.
```

El diseño se puede implementar con un solo deploy, una base con esquemas separados y dos contratos internos. No hay infraestructura que no esté justificada.

</details>

## Resumen

- La arquitectura se diseña desde el dominio y las restricciones.
- Las fronteras se definen por capacidad de negocio y se protegen con contratos.
- Síncrono y asíncrono se eligen por necesidad, no por preferencia.
- CQRS se aplica donde lectura y escritura difieren de verdad.
- Los ADRs y los riesgos forman parte del diseño.

## Siguiente paso

La aplicación está diseñada y segura. Falta llevarla al mundo real: en el módulo de [Deploy y mundo laboral](/modulos/deploy-mundo-laboral) vas a publicarla, automatizar el despliegue y preparar tu portafolio.
