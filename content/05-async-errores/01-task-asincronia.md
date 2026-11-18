---
slug: task-asincronia
title: "Task y asincronía"
order: 1
estimatedMinutes: 15
summary: "Entendé qué representa una Task, cómo modela trabajo pendiente y por qué la asincronía evita bloquear el hilo mientras esperás entrada o salida."
quiz:
  - question: "¿Qué representa una Task en .NET?"
    options:
      - "Un hilo nuevo del sistema operativo"
      - "Una operación que puede completarse en el futuro"
      - "Una colección de resultados ya calculados"
      - "Un bloqueo explícito del hilo actual"
    answer: 1
    explanation: "Una Task es una promesa de trabajo: representa una operación en curso o pendiente y avisa cuándo terminó, si falló o si se canceló."
  - question: "¿Cuál es la diferencia principal entre Task y Task<T>?"
    options:
      - "Task<T> produce un valor cuando termina; Task no"
      - "Task solo sirve para trabajo en CPU y Task<T> para E/S"
      - "Task<T> crea un hilo y Task usa el pool"
      - "No hay diferencia, son alias del mismo tipo"
    answer: 0
    explanation: "Task<T> deriva de Task y agrega un resultado tipado disponible al completarse, por ejemplo Task<decimal> para un precio."
  - question: "¿Qué hace await Task.Delay(500) dentro de un método asíncrono?"
    options:
      - "Bloquea el hilo durante 500 milisegundos"
      - "Crea un hilo que duerme medio segundo"
      - "Libera el hilo mientras espera y retoma la ejecución después"
      - "Cancela la tarea actual a los 500 milisegundos"
    answer: 2
    explanation: "Task.Delay no bloquea: devuelve una tarea que se completa por temporizador. Con await, el hilo queda libre para otro trabajo mientras tanto."
  - question: "¿Para qué conviene usar Task.Run?"
    options:
      - "Para esperar una llamada HTTP"
      - "Para mover trabajo intensivo de CPU fuera del hilo actual"
      - "Para leer un archivo de forma asíncrona"
      - "Para reemplazar siempre a async y await"
    answer: 1
    explanation: "Task.Run encola trabajo en el pool de hilos. Sirve para cálculo pesado; para E/S ya existen APIs asíncronas que no necesitan un hilo extra."
---

# Task y asincronía

## Objetivo

Entender qué es una `Task`, cómo se compone con otras tareas y cuándo conviene usarla en lugar de bloquear el hilo actual. Al terminar, vas a poder leer y escribir código asíncrono básico sin crear hilos a mano.

## Requisitos previos

Necesitás manejar métodos, clases, interfaces y colecciones. También ayuda haber resuelto el [ejercicio integrador de colecciones y LINQ](/modulos/colecciones-linq/ejercicio-integrador): allí usaste delegados y `Func<T, TResult>`, que vuelven a aparecer en las APIs asíncronas.

## Concepto

Una `Task` representa una operación que todavía no terminó. No es un hilo: es una promesa de trabajo con estado. Podés consultar si terminó, esperar su resultado o encadenar trabajo cuando se complete. Muchas tareas pueden compartir el mismo hilo.

La distinción más importante es entre trabajo de E/S y trabajo de CPU:

| Tipo de trabajo | Ejemplo | API típica |
|---|---|---|
| E/S de red | Llamar a una API | `HttpClient.GetStringAsync` |
| E/S de disco | Leer un archivo | `File.ReadAllTextAsync` |
| Espera por tiempo | Pausar sin bloquear | `Task.Delay` |
| CPU intensivo | Calcular un hash pesado | `Task.Run` |
| Trabajo ya resuelto | Devolver una constante | `Task.FromResult` |

El trabajo de E/S no necesita un hilo propio: el sistema operativo avisa cuando hay datos. El trabajo de CPU sí necesita un hilo que ejecute instrucciones, y ahí `Task.Run` tiene sentido.

Las tareas tienen estados observables: `WaitingForActivation`, `RanToCompletion`, `Faulted` y `Canceled`. Completada no significa exitosa: una tarea fallida también está terminada. Por eso no conviene preguntar solo por `IsCompleted`, sino esperar y dejar que el error aparezca.

También existen tareas sin resultado, como `Task`, y tareas con resultado, como `Task<decimal>`. `Task.CompletedTask` representa trabajo ya terminado sin valor, útil para implementar métodos que no hacen nada.

La asincronía no hace que el código sea más rápido por sí sola. Hace que el hilo no quede ocupado esperando, lo que permite atender más trabajo con la misma cantidad de hilos. La ganancia aparece cuando hay esperas reales o tareas independientes que pueden avanzar en paralelo.

## Ejemplo mínimo

```csharp
Console.WriteLine("Inicio");

Task trabajo = Task.Run(() =>
{
    Thread.Sleep(300);
    Console.WriteLine("Cálculo terminado");
});

await trabajo;

Task<int> calculo = CalcularAsync(21);
Console.WriteLine($"Resultado: {await calculo}");

Console.WriteLine("Fin");

static async Task<int> CalcularAsync(int valor)
{
    await Task.Delay(200);
    return valor * 2;
}
```

```text
Inicio
Cálculo terminado
Resultado: 42
Fin
```

`Task.Run` recibe un delegado y lo ejecuta en el pool de hilos. El `await` espera sin bloquear. `CalcularAsync` devuelve `Task<int>` porque produce un número al terminar.

## Ejemplo aplicado

Un cotizador necesita el precio y el stock de un producto. Son dos consultas independientes, así que conviene lanzarlas juntas y esperar una sola vez.

```csharp
var cotizacion = await CotizarAsync("SKU-100");
Console.WriteLine($"{cotizacion.Sku}: {cotizacion.Precio:N0} con {cotizacion.Stock} unidades");

static async Task<Cotizacion> CotizarAsync(string sku)
{
    Task<decimal> precio = ObtenerPrecioAsync(sku);
    Task<int> stock = ObtenerStockAsync(sku);

    await Task.WhenAll(precio, stock);

    return new Cotizacion(sku, await precio, await stock);
}

static async Task<decimal> ObtenerPrecioAsync(string sku)
{
    await Task.Delay(150);
    return 12_500m;
}

static async Task<int> ObtenerStockAsync(string sku)
{
    await Task.Delay(200);
    return 8;
}

public sealed record Cotizacion(string Sku, decimal Precio, int Stock);
```

```text
SKU-100: 12.500 con 8 unidades
```

`Task.WhenAll` devuelve una tarea que termina cuando ambas consultas terminaron. Después de ese `await`, los resultados ya están disponibles y se leen con otro `await`, que se completa de inmediato.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `await tarea.Result` bloquea la aplicación | `.Result` y `.Wait()` esperan de forma síncrona | Usar `await tarea` en un método asíncrono |
| Se crea un hilo para cada llamada HTTP | Se confunde `Task` con `Thread` | Usar las APIs `...Async` y dejar que la E/S no ocupe un hilo |
| La tarea se pierde y nadie ve el error | Se lanza sin guardar la referencia ni esperarla | Guardar la `Task` y esperarla, o pasarla a `Task.WhenAll` |
| `Task.Run` envuelve una operación de E/S | Se cree que hace falta un hilo para esperar | Llamar directamente a la API asíncrona |
| El código es más lento que la versión síncrona | Se esperan tareas de una en una | Lanzar las tareas independientes y esperarlas juntas |
| `IsCompleted` se usa como sinónimo de éxito | Una tarea fallida también está completada | Esperar la tarea y capturar la excepción |

## Buenas prácticas

- Nombrá los métodos asíncronos con el sufijo `Async` y devolvé `Task` o `Task<T>`.
- No uses `Task.Run` para envolver E/S: ya existe una API asíncrona para eso.
- Lanzá varias operaciones independientes y esperalas con `Task.WhenAll`.
- No bloquees con `.Result` ni `.Wait()` dentro de código asíncrono.
- Tratá la cancelación como un estado normal de la tarea, no como un error inesperado.

## Ejercicio

Escribí un programa que consulte tres sucursales en paralelo. Cada consulta debe simular una demora distinta con `Task.Delay` y devolver un stock. Medí el tiempo total con `Stopwatch` y compará la ejecución secuencial con la concurrente.

<details>
<summary>Ver solución</summary>

```csharp
using System.Diagnostics;

string[] sucursales = ["Centro", "Norte", "Sur"];

var reloj = Stopwatch.StartNew();
int totalSecuencial = 0;

foreach (string sucursal in sucursales)
{
    totalSecuencial += await ConsultarStockAsync(sucursal);
}

Console.WriteLine($"Secuencial: {reloj.ElapsedMilliseconds} ms, total {totalSecuencial}");

reloj.Restart();

Task<int>[] tareas = sucursales.Select(ConsultarStockAsync).ToArray();
int[] stocks = await Task.WhenAll(tareas);

Console.WriteLine($"Concurrente: {reloj.ElapsedMilliseconds} ms, total {stocks.Sum()}");

static async Task<int> ConsultarStockAsync(string sucursal)
{
    int demora = sucursal switch
    {
        "Centro" => 300,
        "Norte" => 200,
        _ => 250
    };

    await Task.Delay(demora);
    return demora / 10;
}
```

```text
Secuencial: 750 ms, total 75
Concurrente: 300 ms, total 75
```

La versión concurrente tarda lo que la consulta más lenta porque las tres avanzan al mismo tiempo. Los milisegundos exactos pueden variar según la máquina.

</details>

## Resumen

- Una `Task` representa trabajo pendiente; no es un hilo.
- `Task<T>` agrega un resultado tipado que se obtiene con `await`.
- La asincronía libera el hilo durante las esperas de E/S.
- `Task.WhenAll` espera varias tareas y habilita trabajo concurrente.
- Bloquear con `.Result` o `.Wait()` anula los beneficios y puede causar interbloqueos.

## Siguiente paso

Ya sabés qué es una tarea. En la próxima lección vas a ver cómo [`async` y `await`](/modulos/async-errores/async-await) transforman un método común en uno asíncrono y cómo se encadena la continuación.
