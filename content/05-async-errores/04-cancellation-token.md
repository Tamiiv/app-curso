---
slug: cancellation-token
title: "CancellationToken"
order: 4
estimatedMinutes: 15
summary: "Cancelá operaciones asíncronas de forma cooperativa con CancellationToken, distinguí una cancelación de una falla y combiná varios orígenes."
quiz:
  - question: "¿Qué hace un CancellationToken por sí solo?"
    options:
      - "Interrumpe el hilo que está ejecutando la tarea"
      - "Notifica que se pidió cancelar; el código debe revisarlo"
      - "Lanza una excepción automáticamente en cada await"
      - "Libera los recursos de la tarea"
    answer: 1
    explanation: "La cancelación es cooperativa: el token solo expone el estado IsCancellationRequested. La operación decide cuándo revisarlo y cómo detenerse."
  - question: "¿Qué método conviene llamar dentro de un bucle para detenerse al cancelar?"
    options:
      - "token.Dispose()"
      - "token.ThrowIfCancellationRequested()"
      - "Task.Run(token)"
      - "token.WaitHandle.Close()"
    answer: 1
    explanation: "ThrowIfCancellationRequested lanza OperationCanceledException si el token fue cancelado, y así el método termina de forma ordenada."
  - question: "¿Qué permite CreateLinkedTokenSource?"
    options:
      - "Cancelar un token desde otro proceso"
      - "Combinar varios tokens para que cualquiera cancele la operación"
      - "Convertir un token en una Task"
      - "Duplicar el tiempo de espera"
    answer: 1
    explanation: "El token vinculado se cancela cuando cualquiera de los tokens de origen se cancela. Es ideal para unir la cancelación del usuario con un timeout."
  - question: "¿Qué relación hay entre TaskCanceledException y OperationCanceledException?"
    options:
      - "Son exactamente el mismo tipo"
      - "TaskCanceledException deriva de OperationCanceledException"
      - "OperationCanceledException deriva de TaskCanceledException"
      - "No tienen relación"
    answer: 1
    explanation: "TaskCanceledException es una especialización de OperationCanceledException. Capturar el tipo base cubre ambos casos."
---

# CancellationToken

## Objetivo

Propagar la cancelación por una cadena asíncrona, detener bucles y operaciones de E/S de forma ordenada, y distinguir una cancelación intencional de un error real.

## Requisitos previos

Necesitás manejar `Task`, `async`/`await` y el manejo de excepciones en tareas. También conviene haber visto cómo [los errores viajan en código asíncrono](/modulos/async-errores/errores-async).

## Concepto

La cancelación en .NET es cooperativa. Un `CancellationToken` no mata hilos ni interrumpe código a la fuerza: solo comunica que alguien pidió cancelar. La operación que lo recibe debe revisar ese pedido en puntos seguros y detenerse.

El patrón tiene tres piezas:

| Pieza | Rol |
|---|---|
| `CancellationTokenSource` | Emite la señal y administra el token |
| `CancellationToken` | Estructura liviana que se pasa a las operaciones |
| `OperationCanceledException` | Excepción que indica que la operación se detuvo por cancelación |

El origen se crea una vez y se pasa el token hacia abajo. El token es un `struct` y se puede copiar sin costo; el origen no debería compartirse a la ligera.

Hay dos formas habituales de reaccionar:

- Llamar a `token.ThrowIfCancellationRequested()` y dejar que la excepción corte el flujo.
- Pasar el token a una API que ya sabe cancelarse, como `Task.Delay(..., token)` o `ReadAsStringAsync(token)`.

Las APIs de .NET aceptan el token como último parámetro. Si la operación está esperando, el token la despierta con `OperationCanceledException`; si todavía no empezó, la cancela antes de arrancar.

`CancellationTokenSource` implementa `IDisposable`. Conviene liberarlo con `using`, pero solo después de que las operaciones que usan el token hayan terminado. Liberarlo antes puede provocar errores al consultar el token.

Para timeouts se usa `CancelAfter` o el constructor con `TimeSpan`. Para combinar la cancelación del usuario con un límite de tiempo se usa `CreateLinkedTokenSource`.

Una cancelación no es una falla. Si el usuario cerró la ventana, no tiene sentido registrar un error ni reintentar. El código de borde debe capturar `OperationCanceledException`, revisar qué origen se activó y responder en consecuencia.

## Ejemplo mínimo

```csharp
using var origen = new CancellationTokenSource();
origen.CancelAfter(TimeSpan.FromMilliseconds(350));

try
{
    await TrabajarAsync(origen.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Trabajo cancelado");
}

static async Task TrabajarAsync(CancellationToken token)
{
    for (int paso = 1; paso <= 10; paso++)
    {
        token.ThrowIfCancellationRequested();
        await Task.Delay(100, token);
        Console.WriteLine($"Paso {paso}");
    }
}
```

```text
Paso 1
Paso 2
Paso 3
Trabajo cancelado
```

A los 350 milisegundos se dispara la cancelación. El `Task.Delay` del cuarto paso se interrumpe y lanza `OperationCanceledException`, que se captura en el borde.

## Ejemplo aplicado

Una sincronización tiene un límite de dos segundos, pero el usuario también puede cancelarla. Los dos orígenes se combinan en un token vinculado.

```csharp
using var usuario = new CancellationTokenSource();
using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(2));
using var vinculado = CancellationTokenSource.CreateLinkedTokenSource(usuario.Token, limite.Token);

try
{
    Task<string>[] tareas =
    [
        SincronizarAsync("Centro", 500, vinculado.Token),
        SincronizarAsync("Norte", 3_000, vinculado.Token),
        SincronizarAsync("Sur", 800, vinculado.Token)
    ];

    string[] completadas = await Task.WhenAll(tareas);
    Console.WriteLine($"Sincronizadas: {string.Join(", ", completadas)}");
}
catch (OperationCanceledException)
{
    string motivo = limite.IsCancellationRequested ? "límite de tiempo" : "pedido del usuario";
    Console.WriteLine($"Sincronización cancelada por {motivo}");
}

static async Task<string> SincronizarAsync(string sucursal, int demora, CancellationToken token)
{
    await Task.Delay(demora, token);
    return sucursal;
}
```

```text
Sincronización cancelada por límite de tiempo
```

Las sucursales Centro y Sur terminan, pero Norte tarda tres segundos y el límite corta la espera. `WhenAll` propaga la cancelación y el `catch` distingue el motivo consultando cada origen.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La operación ignora la cancelación | Se pasa el token pero nunca se revisa | Llamar a `ThrowIfCancellationRequested` o pasarlo a las APIs |
| `ObjectDisposedException` al consultar el token | Se liberó el origen antes de terminar las tareas | Mantener el `using` hasta que todas terminen |
| Se registra una cancelación como error | El `catch` general atrapa todo sin distinguir | Capturar `OperationCanceledException` y revisar `IsCancellationRequested` |
| El timeout no corta la operación | Se creó el token pero no se pasó a la tarea | Propagar el token hasta la API que espera |
| No se puede distinguir timeout de cancelación | Se usa un solo origen | Usar `CreateLinkedTokenSource` y consultar cada origen |
| El bucle sigue trabajando tras cancelar | Se revisa el token solo al principio | Revisarlo en cada iteración y dentro de las esperas |

## Buenas prácticas

- Aceptar un `CancellationToken` como último parámetro en los métodos asíncronos públicos.
- Revisar el token en bucles y antes de operaciones costosas.
- Pasar el token a `Task.Delay`, `HttpClient` y APIs de archivos.
- Usar `CreateLinkedTokenSource` para combinar usuario, timeout y apagado de la aplicación.
- No tratar la cancelación como error: no reintentar ni registrar como falla inesperada.

## Ejercicio

Escribí un método `ProcesarLoteAsync` que recorra diez ítems, simule 100 milisegundos de trabajo por ítem y acepte un token. El programa debe cancelar a los 450 milisegundos, informar cuántos ítems se procesaron y terminar sin lanzar la excepción al usuario.

<details>
<summary>Ver solución</summary>

```csharp
using var origen = new CancellationTokenSource();
origen.CancelAfter(TimeSpan.FromMilliseconds(450));

var progreso = new Progreso();

try
{
    await ProcesarLoteAsync(progreso, origen.Token);
    Console.WriteLine($"Lote completo: {progreso.Procesados} ítems");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Lote interrumpido por cancelación");
}
finally
{
    Console.WriteLine($"Ítems procesados: {progreso.Procesados}");
}

static async Task ProcesarLoteAsync(Progreso progreso, CancellationToken token)
{
    for (int item = 1; item <= 10; item++)
    {
        token.ThrowIfCancellationRequested();
        await Task.Delay(100, token);
        progreso.Procesados++;
        Console.WriteLine($"Ítem {item} listo");
    }
}

public sealed class Progreso
{
    public int Procesados { get; set; }
}
```

```text
Ítem 1 listo
Ítem 2 listo
Ítem 3 listo
Ítem 4 listo
Lote interrumpido por cancelación
Ítems procesados: 4
```

El progreso se guarda en un objeto que existe fuera de la tarea. Si la cancelación corta el método, el valor parcial sigue disponible para el `finally` y para el informe.

</details>

## Resumen

- La cancelación es cooperativa: el token avisa y el código decide cuándo parar.
- `CancellationTokenSource` emite la señal; el token se propaga hacia abajo.
- `ThrowIfCancellationRequested` corta el flujo con `OperationCanceledException`.
- `CancelAfter` implementa timeouts y `CreateLinkedTokenSource` combina orígenes.
- La cancelación se maneja como un caso esperado, no como un error.

## Siguiente paso

La cancelación es una excepción especial, pero no la única. En la próxima lección vas a ordenar el manejo de errores con [try/catch/finally y filtros de excepción](/modulos/async-errores/try-catch-finally).
