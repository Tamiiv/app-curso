# Guía de contenido

**Proyecto:** Ruta .NET  
**Última actualización:** 11/11/2026

Esta guía define el formato y el estilo de las lecciones. Todo el contenido nuevo debe respetarla.

## 1. Archivos

Cada módulo vive en `content/<orden>-<slug>/`:

```text
content/02-csharp-desde-cero/
├── module.json
├── 01-variables-tipos.md
├── 02-operadores.md
└── ...
```

- El nombre del archivo empieza con el orden en dos dígitos y el slug de la lección.
- El `slug` del front matter debe coincidir con el nombre del archivo (sin el número).
- No se renombran lecciones ya publicadas sin actualizar todos los enlaces internos.

## 2. Front matter

Todas las lecciones empiezan con este bloque YAML:

```yaml
---
slug: variables-tipos
title: "Variables, tipos y conversiones"
order: 1
estimatedMinutes: 15
summary: "Cómo guardar datos, elegir el tipo correcto y convertir valores sin perder información."
quiz:
  - question: "¿Qué tipo conviene para guardar una edad?"
    options:
      - "string"
      - "int"
      - "bool"
      - "DateTime"
    answer: 1
    explanation: "Una edad es un número entero, por eso int es la opción natural."
  - question: "¿Qué hace una conversión explícita?"
    options:
      - "La hace el compilador sola"
      - "Requiere escribir el tipo de destino"
      - "Solo funciona con strings"
      - "Elimina el valor original"
    answer: 1
    explanation: "La conversión explícita (cast) indica el tipo de destino y asume la responsabilidad de la conversión."
---
```

Reglas del quiz:

- Entre 3 y 5 preguntas por lección.
- Exactamente 4 opciones por pregunta.
- `answer` es el índice **base 0** de la opción correcta (`0` es la primera).
- La explicación se muestra después de responder.
- No incluir una sección `## Quiz` en el cuerpo: el quiz se renderiza desde el front matter.

## 3. Cuerpo de la lección

Seguir este orden, ajustando lo que no aplique:

```markdown
# Título

## Objetivo

Qué va a saber hacer el usuario al terminar. Dos o tres oraciones.

## Requisitos previos

Qué conviene haber leído antes. Si no hace falta nada, decirlo.

## Concepto

Explicación clara, con analogías y sin saltos. Una idea por párrafo.

## Ejemplo mínimo

El código más chico que demuestra la idea, con la salida esperada.

## Ejemplo aplicado

Un caso cercano a un proyecto real.

## Errores comunes

Tabla o lista con el error, la causa y la solución.

## Buenas prácticas

Qué haría un desarrollador experimentado y por qué.

## Ejercicio

Una consigna concreta y verificable. La solución va plegada:

<details>
<summary>Ver solución</summary>

```csharp
// solución completa
```

</details>

## Resumen

- Tres o cuatro ideas clave.

## Siguiente paso

Una o dos oraciones con el enlace a la próxima lección.
```

## 4. Estilo

- Español neutro con voseo rioplatense suave: "podés", "tenés", "creá", "ejecutá".
- Párrafos cortos. Una idea por párrafo.
- Nombres de código descriptivos. Comentarios solo cuando aportan.
- Código en bloques con lenguaje: `csharp`, `bash`, `text`, `json`, `xml`.
- Cada ejemplo debe compilar con **.NET 10 / C# 14**.
- Mostrar la salida esperada en un bloque `text` cuando ayude.
- No usar capturas de pantalla para algo que se explica con texto.
- Advertir diferencias de versión cuando algo cambie entre .NET 8, 9 y 10.
- No inventar APIs. Si hay dudas, usar la documentación oficial de Microsoft.
- Los enlaces internos usan rutas absolutas: `/modulos/<modulo>/<leccion>`.

## 5. Ejercicios y soluciones

- El ejercicio debe poder resolverse con lo visto en la lección.
- La solución va dentro de `<details>` para no arruinar el desafío.
- Incluir el código completo, no fragmentos.
- Si el ejercicio tiene varias soluciones válidas, aclararlo.

## 6. Checklist antes de terminar

- [ ] El objetivo se cumple.
- [ ] Los ejemplos compilan y la salida coincide.
- [ ] El quiz tiene 3 a 5 preguntas con `answer` correcto.
- [ ] El ejercicio tiene solución plegable.
- [ ] No hay errores de ortografía ni enlaces roto.
- [ ] La lección se entiende sin depender de contenido no publicado.
- [ ] El `summary` sirve como meta description (entre 80 y 160 caracteres).
