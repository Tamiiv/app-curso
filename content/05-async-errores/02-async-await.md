---
slug: async-await
title: "async y await"
order: 2
estimatedMinutes: 15
summary: "Escribí métodos asíncronos legibles con async y await, elegí el tipo de retorno correcto y evitá bloquear el hilo mientras esperás."
quiz:
  - question: "¿Qué tipo de retorno conviene para un método asíncrono que no devuelve datos?"
    options:
      - "void"
      - "Task"
      - "Task<T>"
      - "ValueTask siempre"
    answer: 1
    explanation: "Task representa trabajo asíncrono sin resultado. void solo se reserva para manejadores de eventos y no permite esperar ni capturar errores."
  - question: "¿Qué hace el compilador cuando encuentra un await?"
    options:
      - "Bloquea el hilo hasta que la tarea termine"
      - "Convierte el método en una máquina de estados que retoma al completarse la tarea"
      - "Crea un hilo nuevo por cada espera"
      - "Ejecuta la tarea dos veces para verificar el resultado"
    answer: 1
    explanation: "El compilador genera una máquina de estados: guarda el estado local, devuelve el control al llamador y retoma cuando la tarea esperada termina."
  - question: "¿Qué problema tiene declarar async void en un método que no es un evento?"
    options:
      - "No compila en .NET 10"
      - "El llamador no puede esperar el resultado ni enterarse de las excepciones"
      - "Consume más memoria que async Task"
      - "Solo funciona con métodos estáticos"
    answer: 1
    explanation: "Sin una Task que observar, el llamador no sabe cuándo terminó y una excepción no capturada puede terminar el proceso."
  - question: "Si lanzás tres tareas y las esperás una por una con await, ¿qué ocurre?"
    options:
      - "Se ejecutan en paralelo y esperás la más lenta"
      - "Se ejecutan en secuencia y el tiempo total es la suma"
      - "El compilador las convierte en un solo hilo"
      - "La segunda tarea se cancela automáticamente"
    answer: 1
    explanation: "Al escribir await tarea1; await tarea2; la segunda no se lanza hasta que la primera termina. Para concurrencia hay que crear las tareas antes y usar WhenAll."
---

# async y await

## Objetivo

Escribir métodos asíncronos con la sintaxis `async`/`await`, elegir entre `Task`, `Task<T>` y `ValueTask`, y distinguir la ejecución secuencial de la concurrente.

## Requisitos previos

Necesitás entender qué es una `Task` y cómo se espera. Si venís de la lección anterior, ya viste [`Task` y asincronía](/modulos/async-errores/task-asincronia), incluido `Task.WhenAll` y el problema de bloquear con `.Result`.

## Concepto

`async` es un modificador que habilita `await` dentro del método y le indica al compilador que genere una máquina de estados. `await` marca un punto de suspensión: si la tarea esperada todavía no terminó, el método devuelve el control a quien lo llamó y retoma más tarde, cuando la tarea se completa.

El método no se ejecuta en otro hilo por arte de magia. La primera parte corre de forma síncrona hasta el primer `await` que realmente espera. Lo que cambia es que, durante la espera, el hilo queda libre para atender otra cosa.

El tipo de retorno depende de lo que devuelva el método:

| Retorno | Uso |
|---|---|
| `Task` | Trabajo asíncrono sin resultado |
| `Task<T>` | Trabajo asíncrono que produce un `T` |
| `ValueTask` / `ValueTask<T>` | Casos optimizados donde muchas veces no hay espera real |
| `async void` | Solo manejadores de eventos |

`async void` no se puede esperar y sus excepciones no se propagan al llamador. Fuera de un evento, casi siempre es un error.

Un método marcado `async` que no contiene ningún `await` compila con la advertencia `CS1998`. En ese caso no hay nada asíncrono: conviene quitar `async` o devolver `Task.FromResult`.

La regla de oro es no mezclar mundos. Si la cadena completa es asíncrona, usá `await` de punta a punta. Si necesitás esperar desde código síncrono, hacelo en el borde de la aplicación, no en el medio de una biblioteca. En bibliotecas también se usa `ConfigureAwait(false)` para no volver al contexto original, aunque en aplicaciones de consola y ASP.NET Core moderno el contexto no suele ser un problema.

## Ejemplo mínimo

```csharp
Console.WriteLine($"Inicio {DateTime.Now:HH:mm:ss.fff}");

string mensaje = await ProcesarPedidoAsync(42);
Console.WriteLine(mensaje);

Console.WriteLine($"Fin {DateTime.Now:HH:mm:ss.fff}");

static async Task<string> ProcesarPedidoAsync(int id)
{
    await Task.Delay(400);
    return $"Pedido {id} confirmado";
}
```

```text
Inicio 10:15:02.120
Pedido 42 confirmado
Fin 10:15:02.530
```

Entre el inicio y el fin pasan unos 400 milisegundos, pero el hilo no queda bloqueado: solo se suspende este método. La máquina de estados conserva la variable `id` y retoma justo después del `await`.

## Ejemplo aplicado

El mismo par de consultas se puede escribir de dos maneras. La diferencia no es cosmética: cambia el tiempo total.

```csharp
using System.Diagnostics;

var reloj = Stopwatch.StartNew();

string clientes = await ConsultarAsync("clientes");
string productos = await ConsultarAsync("productos");
string pedidos = await ConsultarAsync("pedidos");

Console.WriteLine($"Secuencial: {reloj.ElapsedMilliseconds} ms, {clientes}, {productos}, {pedidos}");

reloj.Restart();

Task<string> tClientes = ConsultarAsync("clientes");
Task<string> tProductos = ConsultarAsync("productos");
Task<string> tPedidos = ConsultarAsync("pedidos");

string[] resultados = await Task.WhenAll(tClientes, tProductos, tPedidos);

Console.WriteLine($"Concurrente: {reloj.ElapsedMilliseconds} ms, {string.Join(", ", resultados)}");

static async Task<string> ConsultarAsync(string recurso)
{
    await Task.Delay(300);
    return recurso;
}
```

```text
Secuencial: ~900 ms, clientes, productos, pedidos
Concurrente: ~300 ms, clientes, productos, pedidos
```

En la versión secuencial, cada `await` impide que la siguiente consulta arranque. En la concurrente, las tres tareas se crean antes de esperar y avanzan juntas. Los tiempos exactos varían según la máquina.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `CS4014`: la llamada no se espera | Se invoca un método asíncrono y se ignora la tarea | Agregar `await` o guardar y esperar la `Task` |
| `CS1998`: método async sin await | Se marcó `async` pero no hay ninguna espera | Quitar `async` o agregar la operación asíncrona que falta |
| La aplicación se congela | Se usa `.Result` o `.Wait()` en un contexto con sincronización | Propagar `async` hasta el borde y usar `await` |
| Las excepciones desaparecen | El método es `async void` | Cambiar a `async Task` y esperar la tarea |
| Todo tarda la suma de las esperas | Los `await` están uno detrás de otro | Crear las tareas primero y usar `Task.WhenAll` |
| `await` dentro de un `lock` no compila | No se puede suspender mientras se tiene el monitor | Hacer la espera fuera del bloqueo y volver a entrar para modificar estado |

## Buenas prácticas

- Propagar `async`/`await` por toda la cadena en lugar de bloquear en el medio.
- Usar `async Task` como valor predeterminado y reservar `async void` para eventos.
- Crear las tareas independientes antes de esperarlas para aprovechar la concurrencia.
- Nombrar los métodos con el sufijo `Async` y documentar si aceptan cancelación.
- No envolver E/S con `Task.Run`; usá la API asíncrona nativa.

## Ejercicio

Escribí un método `CargarPanelAsync` que consulte en paralelo el clima, las ventas del día y las alertas. Cada consulta debe demorar 200, 300 y 400 milisegundos respectivamente. El método debe devolver un `Panel` con los tres valores y el programa debe imprimir el tiempo total.

<details>
<summary>Ver solución</summary>

```csharp
using System.Diagnostics;

var reloj = Stopwatch.StartNew();
Panel panel = await CargarPanelAsync();
reloj.Stop();

Console.WriteLine($"Clima: {panel.Clima}");
Console.WriteLine($"Ventas: {panel.Ventas:N0}");
Console.WriteLine($"Alertas: {panel.Alertas}");
Console.WriteLine($"Tiempo: {reloj.ElapsedMilliseconds} ms");

static async Task<Panel> CargarPanelAsync()
{
    Task<string> clima = ConsultarClimaAsync();
    Task<decimal> ventas = ConsultarVentasAsync();
    Task<int> alertas = ConsultarAlertasAsync();

    await Task.WhenAll(clima, ventas, alertas);

    return new Panel(await clima, await ventas, await alertas);
}

static async Task<string> ConsultarClimaAsync()
{
    await Task.Delay(200);
    return "Soleado, 24 °C";
}

static async Task<decimal> ConsultarVentasAsync()
{
    await Task.Delay(300);
    return 1_250_000m;
}

static async Task<int> ConsultarAlertasAsync()
{
    await Task.Delay(400);
    return 2;
}

public sealed record Panel(string Clima, decimal Ventas, int Alertas);
```

```text
Clima: Soleado, 24 °C
Ventas: 1.250.000
Alertas: 2
Tiempo: ~400 ms
```

El tiempo ronda los 400 milisegundos porque las tres consultas avanzan a la vez. Si se escribieran con `await` consecutivos, el total sería de unos 900 milisegundos.

</details>

## Resumen

- `async` habilita `await` y genera una máquina de estados.
- `await` suspende el método sin bloquear el hilo.
- `Task` y `Task<T>` son los tipos de retorno habituales; `async void` queda para eventos.
- Los `await` consecutivos ejecutan en serie; las tareas creadas antes y esperadas con `WhenAll` ejecutan en paralelo.
- Ignorar una `Task` oculta errores y rompe el flujo de la aplicación.

## Siguiente paso

Cuando varias tareas fallan al mismo tiempo, hay que saber dónde quedan las excepciones y cómo observarlas. Eso es exactamente lo que vas a ver en [errores en código asíncrono](/modulos/async-errores/errores-async).
