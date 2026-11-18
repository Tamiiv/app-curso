---
slug: errores-async
title: "Errores en código asíncrono"
order: 3
estimatedMinutes: 15
summary: "Descubrí dónde quedan las excepciones de una tarea, cómo las propaga await y cómo recolectar todas las fallas de Task.WhenAll."
quiz:
  - question: "Cuando una tarea falla y hacés await, ¿qué excepción se lanza?"
    options:
      - "Siempre un AggregateException"
      - "La excepción original que lanzó la tarea"
      - "Una TaskFaultedException genérica"
      - "Ninguna: la tarea devuelve null"
    answer: 1
    explanation: "await desenvuelve el AggregateException interno y relanza la excepción original, conservando el seguimiento de la pila."
  - question: "¿Dónde están todas las excepciones cuando fallan varias tareas de Task.WhenAll?"
    options:
      - "En la propiedad Exception de la tarea que devuelve WhenAll"
      - "En el primer elemento del array de tareas"
      - "Se pierden, solo queda la primera"
      - "En una variable estática de Task"
    answer: 0
    explanation: "La tarea combinada guarda un AggregateException con las fallas de todas las tareas. Se lee con task.Exception o task.Exception.Flatten()."
  - question: "¿Qué hace Task.WhenAll si una de las tareas falla?"
    options:
      - "Cancela las demás de inmediato"
      - "Espera a que todas terminen, exitosas o fallidas"
      - "Devuelve el resultado de las que funcionaron"
      - "Lanza la excepción antes de que terminen las demás"
    answer: 1
    explanation: "WhenAll espera la finalización de todas. Las excepciones se acumulan y se observan cuando la tarea combinada termina."
  - question: "¿Para qué sirve AggregateException.Flatten()?"
    options:
      - "Para ordenar las excepciones por gravedad"
      - "Para convertir todas en InvalidOperationException"
      - "Para aplanar AggregateException anidados en una sola lista"
      - "Para eliminar las excepciones repetidas"
    answer: 2
    explanation: "Flatten recorre los AggregateException internos y devuelve una colección plana con las excepciones reales, sin jerarquías anidadas."
---

# Errores en código asíncrono

## Objetivo

Entender cómo viajan las excepciones dentro de una `Task`, qué hace `await` al encontrarlas y cómo obtener el detalle completo cuando varias operaciones fallan en paralelo.

## Requisitos previos

Necesitás manejar `Task`, `async` y `await`, y haber usado `Task.WhenAll` con varias operaciones. Si esas ideas no están firmes, repasá [async y await](/modulos/async-errores/async-await) antes de seguir.

## Concepto

Una tarea fallida no lanza la excepción en el momento del error: la guarda. La tarea pasa al estado `Faulted` y la excepción queda almacenada hasta que alguien la observa. Observar significa esperar la tarea con `await`, llamar a `.Wait()` o leer la propiedad `Exception`.

Cuando hacés `await tarea`, el compilador desenvuelve el `AggregateException` interno y relanza la excepción original. Por eso el `catch` habitual funciona con el tipo concreto:

```csharp
try
{
    await tarea;
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}
```

Si en cambio hay varias tareas combinadas con `Task.WhenAll`, la tarea resultante también falla, pero su `Exception` contiene todas las fallas. Hay dos formas de mirarlas:

| Forma | Qué devuelve |
|---|---|
| `await tareaCombinada` | La primera excepción, desenvuelta |
| `tareaCombinada.Exception` | Un `AggregateException` con todas |
| `tareaCombinada.Exception.Flatten().InnerExceptions` | Lista plana de excepciones reales |

`WhenAll` espera a que todas terminen, incluso si algunas fallan. No cancela ni interrumpe a las demás. Eso es útil para informes por lote: querés saber todo lo que salió mal, no solo lo primero.

Un detalle importante: si una tarea falla y nadie la observa, la excepción se considera no observada. En .NET moderno eso no derriba el proceso de inmediato, pero sí pierde información valiosa. Guardar siempre la `Task` y esperarla es la defensa más simple.

`Task.WhenAny` sirve para otro escenario: esperar a la primera que termine, sea exitosa o no. La tarea devuelta nunca falla por sí misma; hay que esperar a la tarea interna para ver el resultado.

## Ejemplo mínimo

```csharp
try
{
    await ConsultarAsync("SKU-9");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Error controlado: {ex.Message}");
}

static async Task ConsultarAsync(string sku)
{
    await Task.Delay(100);
    throw new InvalidOperationException($"El servicio no respondió para {sku}.");
}
```

```text
Error controlado: El servicio no respondió para SKU-9.
```

La excepción se lanza dentro de la tarea, viaja hasta el `await` y recién ahí se puede capturar. El `try` envuelve al `await`, no a la creación de la tarea.

## Ejemplo aplicado

Un lote de consultas puede tener fallas parciales. El objetivo es informar todas y no abortar el resto.

```csharp
Task<int>[] tareas =
[
    ConsultarAsync("SKU-1", 100, true),
    ConsultarAsync("SKU-2", 150, false),
    ConsultarAsync("SKU-3", 200, false)
];

Task<int[]> combinada = Task.WhenAll(tareas);

try
{
    int[] stocks = await combinada;
    Console.WriteLine($"Total: {stocks.Sum()}");
}
catch (Exception ex) when (combinada.Exception is not null)
{
    Console.WriteLine($"Primer error: {ex.Message}");

    foreach (Exception interna in combinada.Exception.Flatten().InnerExceptions)
    {
        Console.WriteLine($"- {interna.Message}");
    }
}

static async Task<int> ConsultarAsync(string sku, int demora, bool responde)
{
    await Task.Delay(demora);

    if (!responde)
    {
        throw new InvalidOperationException($"Sin respuesta para {sku}.");
    }

    return 10;
}
```

```text
Primer error: Sin respuesta para SKU-2.
- Sin respuesta para SKU-2.
- Sin respuesta para SKU-3.
```

El `await` lanza una sola excepción, la primera que quedó registrada. El filtro `when` comprueba que la tarea combinada tenga excepciones y adentro se recorren todas con `Flatten`. Si se necesitan resultados parciales, conviene capturar dentro de cada tarea y devolver un objeto de resultado en lugar de dejar que el lote entero falle.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El `catch` no atrapa nada | El `try` envuelve la creación de la tarea pero no el `await` | Poner el `await` dentro del `try` |
| Solo se ve la primera falla del lote | Se captura la excepción del `await` y no se mira `Exception` | Recorrer `task.Exception.Flatten().InnerExceptions` |
| `AggregateException` aparece en el `catch` | Se espera con `.Wait()` o `.Result` en lugar de `await` | Usar `await`, que desenvuelve la excepción |
| Las tareas siguen ejecutándose tras una falla | Se asumió que `WhenAll` cancela el resto | Aceptar que terminan todas o coordinar un token de cancelación |
| Una excepción no aparece en los logs | La `Task` se descartó sin observarla | Guardar la referencia y esperarla siempre |
| Se captura `Exception` para todo | Se pierde la posibilidad de distinguir fallas esperables | Capturar tipos específicos y dejar el resto para el borde |

## Buenas prácticas

- Envolver el `await` en el `try`, no solo la llamada que crea la tarea.
- Capturar tipos específicos antes que `Exception`.
- Usar `WhenAll` para lotes y revisar `Exception` cuando interese el detalle completo.
- Evitar `async void`: sus excepciones no se pueden observar desde el llamador.
- Registrar las fallas con contexto (operación, identificador, intento) antes de decidir si se reintenta.

## Ejercicio

Escribí un programa que lance cuatro tareas de validación. Dos deben fallar con mensajes distintos y dos deben completarse. Usá `Task.WhenAll`, capturá la falla y mostrá la cantidad total de errores y cada mensaje. Al final, imprimí cuántas validaciones terminaron bien.

<details>
<summary>Ver solución</summary>

```csharp
Task<string>[] tareas =
[
    ValidarAsync("cliente", true),
    ValidarAsync("producto", false),
    ValidarAsync("precio", true),
    ValidarAsync("stock", false)
];

Task<string[]> combinada = Task.WhenAll(tareas);

try
{
    string[] resultados = await combinada;
    Console.WriteLine($"Correctas: {resultados.Length}");
}
catch (Exception) when (combinada.Exception is not null)
{
    Exception[] errores = combinada.Exception.Flatten().InnerExceptions.ToArray();
    Console.WriteLine($"Errores: {errores.Length}");

    foreach (Exception error in errores)
    {
        Console.WriteLine($"- {error.Message}");
    }

    int correctas = tareas.Count(tarea => tarea.IsCompletedSuccessfully);
    Console.WriteLine($"Correctas: {correctas}");
}

static async Task<string> ValidarAsync(string campo, bool valido)
{
    await Task.Delay(Random.Shared.Next(50, 200));

    if (!valido)
    {
        throw new ArgumentException($"El campo {campo} no es válido.", campo);
    }

    return campo;
}
```

```text
Errores: 2
- El campo producto no es válido. (Parameter 'campo')
- El campo stock no es válido. (Parameter 'campo')
Correctas: 2
```

`IsCompletedSuccessfully` distingue las tareas que terminaron sin error de las que fallaron. El orden de los mensajes depende de cuándo terminó cada tarea.

</details>

## Resumen

- Las tareas guardan sus excepciones hasta que alguien las observa.
- `await` relanza la excepción original y conserva la pila.
- `WhenAll` espera todas las tareas y acumula las fallas en `Exception`.
- `Flatten()` aplana los `AggregateException` anidados.
- Una tarea fallida sin observar pierde información y complica el diagnóstico.

## Siguiente paso

Las operaciones largas no siempre deben llegar al final. En la próxima lección vas a ver cómo [cancelar trabajo con CancellationToken](/modulos/async-errores/cancellation-token) de forma cooperativa.
