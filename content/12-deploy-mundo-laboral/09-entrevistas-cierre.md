---
slug: entrevistas-cierre
title: "Entrevistas y cierre de la ruta"
order: 9
estimatedMinutes: 20
summary: "Preparate para las etapas de una entrevista .NET, resolvé ejercicios en vivo y cerrá la ruta con un plan de práctica sostenible."
quiz:
  - question: "¿Qué estructura conviene para responder una pregunta de comportamiento?"
    options:
      - "Describir la empresa durante cinco minutos"
      - "Situación, tarea, acción y resultado"
      - "Listar todas las tecnologías que conocés"
      - "Responder solo con el resultado final"
    answer: 1
    explanation: "El formato STAR ordena la respuesta: contexto, responsabilidad, qué hiciste y qué resultado medible obtuviste."
  - question: "En un ejercicio técnico en vivo, ¿qué es más importante?"
    options:
      - "Escribir todo de memoria sin errores"
      - "Pensar en voz alta, aclarar requisitos y probar el código"
      - "Terminar en la mitad del tiempo"
      - "Usar la mayor cantidad de librerías posible"
    answer: 1
    explanation: "El entrevistador evalúa cómo razonás y cómo manejás supuestos y errores, no la velocidad de tipeo."
  - question: "¿Cuándo conviene preguntar por el rango salarial?"
    options:
      - "Nunca"
      - "En la primera conversación, con un rango y expectativas claras"
      - "Solo después de firmar el contrato"
      - "Únicamente por escrito después de la oferta"
    answer: 1
    explanation: "Hablar de expectativas temprano evita procesos largos que terminan en desacuerdo. Un rango fundamentado es una respuesta profesional."
  - question: "¿Qué demuestra mejor que sabés usar EF Core en una entrevista?"
    options:
      - "Recitar la lista completa de métodos"
      - "Explicar un problema de N+1 que resolviste y cómo lo mediste"
      - "Decir que siempre usás SQL puro"
      - "Mencionar todas las versiones del paquete"
    answer: 1
    explanation: "Un caso concreto con diagnóstico y resultado demuestra criterio, que es lo que separa el uso memorizado del conocimiento aplicado."
---

# Entrevistas y cierre de la ruta

## Objetivo

Preparar cada etapa del proceso de selección, resolver ejercicios técnicos con un método claro y armar un plan para seguir creciendo después del curso.

## Requisitos previos

Necesitás el portafolio publicado de la lección anterior y poder explicar al menos un proyecto de punta a punta. Si todavía no lo tenés, [Portafolio de proyectos](/modulos/deploy-mundo-laboral/portafolio) es el paso previo.

## Concepto

Un proceso de selección típico tiene cuatro etapas: filtro inicial con reclutamiento, entrevista técnica, ejercicio o prueba, y conversación final con el equipo. Cada una evalúa algo distinto y conviene prepararlas por separado.

El filtro inicial es breve. La persona de reclutamiento quiere confirmar experiencia, disponibilidad, modalidad y expectativas salariales. Respondé con datos concretos y un rango fundamentado. No hace falta contar toda tu historia: alcanza con decir qué hiciste, con qué tecnologías y qué buscás.

La entrevista técnica mezcla preguntas de concepto con preguntas sobre tus proyectos. No esperan que sepas todo de memoria; esperan que distinguishes lo que usaste de lo que leíste. Si no sabés algo, decilo y explicá cómo lo averiguarías. Inventar una respuesta es peor que reconocer un límite.

El ejercicio en vivo evalúa el proceso, no solo el resultado. Aclarar requisitos, escribir casos de prueba, pensar en voz alta y aceptar correcciones son señales fuertes. Un candidato que pregunta por entradas inválidas suele valer más que uno que escribe rápido y no prueba nada.

Las preguntas de diseño buscan entender cómo tomás decisiones. No hay una arquitectura única correcta. Importa que justifiques trade-offs: por qué una base relacional, por qué un monolito modular, qué pasa si el volumen se multiplica por diez, dónde está el cuello de botella.

Las preguntas de comportamiento se responden con hechos. Usá el formato STAR: situación, tarea, acción y resultado. Elegí conflictos reales con desenlace, no anécdotas idealizadas. El resultado conviene que sea medible: tiempo, error reducido, costo, satisfacción del equipo.

Al final, vos también entrevistás. Preguntá por el proceso de deploy, la cobertura de tests, cómo se revisan los cambios, quién decide prioridades y qué pasa cuando hay un incidente. Esas preguntas muestran experiencia y te ayudan a decidir.

## Ejemplo mínimo

```text
Pregunta: Contame un problema técnico que hayas resuelto.

Respuesta con STAR:

Situación: En la API de pedidos, el endpoint de detalle tardaba 3 segundos
con pedidos de 50 ítems.

Tarea: Reducir el tiempo sin cambiar el contrato de la API.

Acción: Medí con logs y encontré 51 consultas por pedido (N+1).
Reemplacé las consultas por un Include con ThenInclude y AsNoTracking,
y agregué un test de integración que cuenta las consultas ejecutadas.

Resultado: La latencia bajó a 120 ms y el test evita la regresión.
```

```text
Por qué funciona:
- Hay un problema concreto y medible.
- La acción muestra diagnóstico, no solo solución.
- El resultado tiene números.
- Se agregó una protección para el futuro.
```

La misma estructura sirve para preguntas sobre conflictos, priorización o errores. Cambia el contenido, no el formato.

## Ejemplo aplicado

Un ejercicio en vivo frecuente: implementar un endpoint con validación y tests. El método importa más que la solución.

```csharp
// Enunciado: POST /cursos debe validar título único,
// duración positiva y devolver 201 con la ubicación del recurso.

app.MapPost("/cursos", async (
    CrearCurso request,
    CursosDbContext db,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Titulo) || request.Titulo.Length > 80)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["titulo"] = ["El título es obligatorio y admite hasta 80 caracteres."]
        });
    }

    if (request.DuracionHoras <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["duracionHoras"] = ["La duración debe ser mayor que cero."]
        });
    }

    bool existe = await db.Cursos.AnyAsync(c => c.Titulo == request.Titulo, ct);
    if (existe)
    {
        return Results.Conflict(new { mensaje = "Ya existe un curso con ese título." });
    }

    var curso = new Curso(request.Titulo, request.DuracionHoras);
    db.Cursos.Add(curso);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/cursos/{curso.Id}", curso);
});
```

```csharp
[Fact]
public async Task CrearCurso_ConTituloDuplicado_Devuelve409()
{
    await using var factory = new ApiFactory();
    var client = factory.CreateClient();

    await client.PostAsJsonAsync("/cursos", new { titulo = "C#", duracionHoras = 10 });
    var respuesta = await client.PostAsJsonAsync("/cursos", new { titulo = "C#", duracionHoras = 8 });

    Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
}
```

```text
Guion para el vivo:

1. Repetir el enunciado con mis palabras y confirmar supuestos.
2. Preguntar: ¿el título único distingue mayúsculas? ¿hay usuarios concurrentes?
3. Escribir primero el caso feliz.
4. Agregar validaciones y el caso de conflicto.
5. Escribir un test por comportamiento.
6. Ejecutar y mostrar el resultado.
7. Mencionar qué faltaría para producción: índice único, logging, rate limiting.
```

El paso 7 es clave. Reconocer lo que falta demuestra criterio y evita que el entrevistador tenga que señalarlo. Nunca digas que está listo para producción si no lo está.

Preguntas para hacer al equipo, ordenadas por lo que revelan:

```text
- ¿Cómo llega un cambio a producción y cuánto tarda?
- ¿Qué cobertura de tests tienen y qué no testean?
- ¿Cómo se decide la prioridad cuando hay un incidente y una feature?
- ¿Qué pasa cuando alguien se equivoca en un deploy?
- ¿Cómo se reparte el trabajo y quién revisa?
- ¿Qué mejora del proceso están intentando este trimestre?
```

```text
Señales de alerta:
- Nadie sabe cómo se despliega.
- No hay tests y se considera normal.
- Los incidentes se resuelven culpando a personas.
- No hay espacio para preguntar ni para decir "no sé".
```

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Inventar una respuesta técnica | Miedo a parecer junior | Decir que no lo usaste y explicar cómo lo investigarías |
| Ejercicio en vivo sin preguntas | Se asume que preguntar resta puntos | Aclarar requisitos y casos borde antes de codificar |
| Respuestas de comportamiento sin resultado | Se cuenta solo la anécdota | Cerrar con un resultado medible |
| Salario postergado hasta el final | Evitar la incomodidad | Dar un rango fundamentado en la primera conversación |
| No hacer preguntas al equipo | Se prioriza impresionar | Preguntar por proceso, calidad e incidentes |
| Portafolio que no podés explicar | Se copió sin entender | Poder justificar cada decisión y cada línea propia |

## Buenas prácticas

- Practicá en voz alta: explicar un proyecto en cinco minutos es un ejercicio distinto de programarlo.
- Prepará tres historias STAR con resultados concretos y ensayalas.
- En el vivo, empezá por el caso simple y agregá complejidad.
- Si no sabés algo, respondé con un límite claro y un plan de aprendizaje.
- Cerrá cada proceso pidiendo feedback, sea o no una oferta.

## Ejercicio

Simulá una entrevista completa de cuarenta minutos: cinco minutos de presentación, diez de preguntas sobre tu proyecto, quince de ejercicio en vivo y diez de preguntas al equipo. Grabala, mirala y anotá tres mejoras. Después armá tu plan de los próximos noventa días.

<details>
<summary>Ver solución</summary>

```text
Guion de la simulación

0-5 min   Presentación: quién sos, qué construiste, qué buscás.
5-15 min  Proyecto: problema, arquitectura, decisión difícil, resultado.
15-30 min Ejercicio: validar un endpoint y escribir dos tests.
30-40 min Preguntas al equipo y cierre.
```

```markdown
## Autoevaluación

| Aspecto | Nota 1-5 | Evidencia |
|---|---|---|
| Claridad al presentarme | 4 | Tardé 7 minutos, recortar |
| Explicar decisiones | 4 | Supe justificar EF Core |
| Ejercicio en vivo | 3 | Empecé a codificar sin preguntar |
| Tests | 4 | Cubrí caso feliz y conflicto |
| Preguntas al equipo | 3 | Faltó preguntar por incidentes |

## Tres mejoras
1. Preparar una presentación de 90 segundos y una de 5 minutos.
2. Antes de codificar, escribir tres supuestos y confirmarlos.
3. Llevar cinco preguntas escritas para el equipo.
```

```markdown
## Plan de 90 días

Días 1-30:
- Completar tests del proyecto principal y subir cobertura de casos borde.
- Escribir dos historias STAR con resultados medibles.
- Practicar un ejercicio en vivo por semana con timer.

Días 31-60:
- Agregar observabilidad real al proyecto: métricas y alertas.
- Leer el código de una librería open source de .NET y hacer un PR pequeño.
- Hacer dos simulaciones completas con otra persona.

Días 61-90:
- Actualizar portafolio y README con lo aprendido.
- Postularse a cinco puestos por semana con carta adaptada.
- Pedir feedback de cada proceso y ajustar el guion.
```

Checklist de cierre:

- Podés explicar tu proyecto en 90 segundos y en 5 minutos.
- Tenés tres historias STAR con resultado numérico.
- Resolviste un ejercicio en vivo preguntando antes de codificar.
- Tenés preguntas preparadas para el equipo.
- El plan de 90 días tiene acciones semanales verificables.

</details>

## Resumen

- Cada etapa del proceso evalúa algo distinto: preparala por separado.
- En el vivo importa el razonamiento y la comunicación, no la velocidad.
- STAR convierte una anécdota en una respuesta con resultado medible.
- Reconocer un límite con un plan es más profesional que inventar.
- El cierre de la ruta es un plan de práctica, no un certificado.

## Siguiente paso

Terminaste el recorrido completo: fundamentos, C#, POO, colecciones, asincronía, APIs, datos, testing, calidad, seguridad, arquitectura y deploy. Volvé al [índice de módulos](/modulos) para repasar cualquier tema, elegir un proyecto integrador o empezar de nuevo con otra mirada. Lo que sigue ya no depende del curso: depende de cuántas veces lleves una idea hasta producción.
