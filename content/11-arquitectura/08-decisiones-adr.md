---
slug: decisiones-adr
title: "Decisiones y ADRs"
order: 8
estimatedMinutes: 12
summary: "Registrá las decisiones de arquitectura con contexto, alternativas y consecuencias, para que el próximo equipo entienda por qué el sistema es como es."
quiz:
  - question: "¿Qué es un ADR?"
    options:
      - "Un documento breve que registra una decisión de arquitectura y su contexto"
      - "Un archivo de configuración del servidor"
      - "Un tipo de test de integración"
      - "Una herramienta de deploy"
    answer: 0
    explanation: "Architecture Decision Record captura el problema, las alternativas, la decisión y sus consecuencias."
  - question: "¿Qué debe incluir un ADR además de la decisión?"
    options:
      - "Solo el nombre del autor"
      - "Contexto, alternativas consideradas y consecuencias"
      - "El código completo del módulo"
      - "El presupuesto del proyecto"
    answer: 1
    explanation: "Sin contexto y alternativas, el documento no permite entender ni revisar la decisión."
  - question: "¿Qué se hace cuando una decisión cambia?"
    options:
      - "Borrar el ADR anterior"
      - "Escribir uno nuevo que reemplace o superseda al anterior, dejando el historial"
      - "Editar el archivo sin dejar rastro"
      - "Ignorarlo hasta que alguien pregunte"
    answer: 1
    explanation: "Los ADRs son un registro histórico. Un cambio se documenta con uno nuevo que referencia al anterior."
  - question: "¿Cuándo conviene escribir un ADR?"
    options:
      - "Para cada commit"
      - "Para decisiones costosas de revertir o que afectan a varios equipos"
      - "Solo al final del proyecto"
      - "Nunca: el código se explica solo"
    answer: 1
    explanation: "El valor está en decisiones con impacto y costo de reversión: tecnología, estructura, integraciones, seguridad."
---

# Decisiones y ADRs

## Objetivo

Documentar decisiones de arquitectura con contexto, alternativas y consecuencias, mantener un registro histórico y usar ese registro para revisar el sistema con criterio.

## Requisitos previos

Haber leído [CQRS](/modulos/arquitectura/cqrs). Conviene tener una decisión real del proyecto para documentar.

## Concepto

Una decisión de arquitectura es una elección que afecta la estructura del sistema y es costosa de revertir: base de datos, estilo de comunicación, autenticación, separación de módulos, framework.

Un **ADR** (Architecture Decision Record) es un documento corto, versionado junto al código, que registra:

- **Contexto:** la situación y las fuerzas en juego.
- **Decisión:** qué se eligió.
- **Alternativas:** qué se descartó y por qué.
- **Consecuencias:** qué se gana, qué se pierde y qué queda pendiente.

| Elemento | Pregunta que responde |
|---|---|
| Contexto | ¿Qué problema había? |
| Decisión | ¿Qué se hizo? |
| Alternativas | ¿Qué más se evaluó? |
| Consecuencias | ¿Qué implica a partir de ahora? |
| Estado | ¿Está aceptada, reemplazada o descartada? |

El formato más usado es un archivo Markdown numerado en `docs/adr/`:

```text
docs/adr/
├── 0001-usar-postgresql.md
├── 0002-autenticacion-con-jwt.md
└── 0003-monolito-modular.md
```

Un ADR no es una especificación funcional ni un manual. Si ocupa diez páginas, probablemente mezcla cosas. La extensión ideal es una o dos páginas: lo suficiente para que alguien dentro de un año entienda la decisión sin preguntar.

Los ADRs se escriben cuando la decisión se toma, no al final. También se aceptan decisiones tomadas: "usamos X porque ya estaba" es información válida si se documenta el contexto.

## Ejemplo mínimo

```markdown
# 0003. Adoptar monolito modular en lugar de microservicios

- **Estado:** aceptada
- **Fecha:** 2026-03-10
- **Decisores:** equipo de plataforma

## Contexto

El sistema tiene tres áreas: catálogo, pedidos y facturación. El equipo
tiene cuatro personas y un solo pipeline de deploy. Las áreas comparten
transacciones en el flujo de compra y el volumen actual no exige escalar
ninguna por separado.

## Decisión

Se implementa un monolito modular con un módulo por área, contratos
internos explícitos y una base de datos con esquemas separados.

## Alternativas consideradas

1. **Microservicios desde el inicio.** Descartado: el costo operativo y las
   transacciones distribuidas no se justifican con el volumen y el equipo actual.
2. **Monolito sin módulos.** Descartado: ya hay señales de acoplamiento entre
   catálogo y pedidos.

## Consecuencias

- Un solo despliegue y una sola base: menor costo operativo.
- Las fronteras se sostienen con tests de arquitectura.
- Si un módulo necesita escalar por separado, se puede extraer: el contrato ya existe.
- Se revisa esta decisión si el equipo crece o si un módulo requiere un ciclo de deploy distinto.
```

Ese documento responde por qué el sistema no es microservicios sin que nadie tenga que adivinarlo.

## Ejemplo aplicado

Un ADR que reemplaza a otro, manteniendo el historial:

```markdown
# 0007. Migrar la autenticación de sesión a JWT

- **Estado:** aceptada
- **Reemplaza a:** ADR-0002
- **Fecha:** 2026-09-02

## Contexto

El ADR-0002 eligió cookies de sesión para una aplicación web única.
Ahora hay una app móvil y una API consumida por terceros. Las cookies
no cubren esos clientes sin trabajo adicional.

## Decisión

Se adopta JWT de acceso corto con refresh tokens rotados para clientes
externos. La aplicación web mantiene cookie de sesión sobre el mismo backend.

## Consecuencias

- Dos mecanismos conviven: cookie para web, bearer para móvil y terceros.
- Se necesita rotación de claves y revocación de refresh tokens.
- El ADR-0002 queda como historia, no se edita.
```

```text
docs/adr/0002-autenticacion-con-cookies.md   ← estado: reemplazada por 0007
docs/adr/0007-autenticacion-con-jwt.md       ← estado: aceptada
```

La decisión anterior no se borra: se marca como reemplazada. Así se entiende la evolución y se evita repetir discusiones ya resueltas.

Revisión en el flujo de trabajo:

- Un cambio de arquitectura en un pull request incluye su ADR.
- El ADR se revisa junto con el código, no después.
- Si la decisión se revierte, se escribe otro ADR que lo explica.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| ADRs que nadie lee | Documentos largos y sin contexto | Formato corto, decisiones concretas |
| Se borra el ADR anterior | Se quiso "mantener limpio" | Marcar como reemplazado y conservar |
| Se documenta después del lanzamiento | Prisa | Escribir al tomar la decisión |
| No hay alternativas | Se justificó la elección sin comparar | Enumerar al menos dos opciones y por qué se descartaron |
| ADR para cada detalle | Exceso de proceso | Reservarlo a decisiones costosas de revertir |
| Decisiones en un chat o en la cabeza de alguien | No hay registro | ADR en el repositorio, junto al código |

## Buenas prácticas

- Un ADR por decisión, numerado y versionado con el código.
- Contexto primero: la decisión se entiende por el problema.
- Alternativas reales, no paja.
- Consecuencias honestas: lo que se pierde también se escribe.
- Estado explícito y vínculo con ADRs reemplazados.
- Revisar ADRs al inicio de un proyecto nuevo y al planear cambios grandes.

## Ejercicio

Escribí un ADR para una decisión real de tu proyecto: base de datos, autenticación, estilo de comunicación o estructura de módulos. Incluí contexto, dos alternativas y consecuencias. Luego marcá otro ADR como reemplazado si corresponde.

<details>
<summary>Ver solución</summary>

```markdown
# 0011. Usar PostgreSQL como base de datos principal

- **Estado:** aceptada
- **Fecha:** 2026-04-18

## Contexto

La aplicación necesita transacciones, consultas relacionales y despliegue
en un proveedor gestionado. El equipo tiene experiencia con SQL y el
volumen inicial es bajo.

## Decisión

Se usa PostgreSQL 17 con EF Core y migraciones versionadas.

## Alternativas consideradas

1. **SQL Server.** Descartado por costo de licencia y porque el proveedor
   elegido ofrece PostgreSQL gestionado.
2. **MongoDB.** Descartado: el modelo tiene relaciones claras y se necesitan
   transacciones entre pedidos e ítems.

## Consecuencias

- Modelo relacional con migraciones versionadas en CI.
- Dependencia del proveedor gestionado; se documenta el plan de backup.
- Si aparece una necesidad de búsqueda avanzada, se evalúa un índice externo.
```

```text
El ADR queda en docs/adr/0011-postgresql.md y se referencia desde el README
del proyecto para que un integrante nuevo entienda la elección.
```

</details>

## Resumen

- Un ADR registra contexto, decisión, alternativas y consecuencias.
- Se escribe al decidir y vive junto al código.
- Los cambios se documentan con nuevos ADRs, sin borrar el historial.
- No toda decisión merece un ADR: solo las costosas de revertir.
- El registro evita repetir discusiones y explica el sistema a futuro.

## Siguiente paso

Cerramos el módulo con el [ejercicio integrador de arquitectura](/modulos/arquitectura/ejercicio-integrador): diseñar un sistema completo, justificar sus fronteras y documentar las decisiones.
