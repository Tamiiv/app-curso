---
slug: resiliencia
title: "Timeouts, reintentos y resiliencia"
order: 8
estimatedMinutes: 15
summary: "Limitá esperas con timeouts, reintentá solo fallas transitorias con espera creciente y evitá que un servicio lento arrastre toda la aplicación."
quiz:
  - question: "¿Qué hace exactamente tarea.WaitAsync(TimeSpan.FromSeconds(2))?"
    options:
      - "Cancela la operación original a los dos segundos"
      - "Deja de esperar a los dos segundos y lanza TimeoutException"
      - "Reinicia la operación si tarda más de dos segundos"
      - "Devuelve null si no termina"
    answer: 1
    explanation: "WaitAsync limita la espera, pero no cancela el trabajo subyacente. Para detenerlo de verdad hay que pasarle un CancellationToken."
  - question: "¿Qué tipo de error conviene reintentar?"
    options:
      - "Un argumento inválido"
      - "Un timeout o un servicio temporalmente no disponible"
      - "Un error de compilación"
      - "Una validación de negocio fallida"
    answer: 1
    explanation: "Solo se reintentan fallas transitorias. Reintentar un error de validación produce el mismo resultado y agrega demora."
  - question: "¿Para qué se agrega jitter a la espera entre reintentos?"
    options:
      - "Para que el código sea más corto"
      - "Para evitar que muchos clientes reintenten al mismo tiempo"
      - "Para aumentar la cantidad de intentos"
      - "Para cancelar la operación más rápido"
    answer: 1
    explanation: "El jitter distribuye los reintentos en el tiempo y evita picos sincronizados contra un servicio que se está recuperando."
  - question: "¿Qué debe pasar si el usuario cancela durante la espera entre reintentos?"
    options:
      - "Se ignora y se sigue reintentando"
      - "Se espera el tiempo completo y después se corta"
      - "La cancelación se propaga y el bucle termina"
      - "Se convierte en un error crítico"
    answer: 2
    explanation: "El token debe pasarse a Task.Delay y a la operación. Si se cancela, OperationCanceledException corta el bucle de inmediato."
---

# Timeouts, reintentos y resiliencia

## Objetivo

Proteger la aplicación frente a servicios lentos o inestables con timeouts, reintentos acotados y esperas crecientes, sin ocultar errores permanentes ni bloquear la cancelación.

## Requisitos previos

Necesitás manejar `Task`, `async`/`await`, `CancellationToken` y excepciones. Si alguna de esas piezas no está clara, repasá [CancellationToken](/modulos/async-errores/cancellation-token) y [errores en código asíncrono](/modulos/async-errores/errores-async).

## Concepto

Un servicio externo puede tardar más de lo razonable. Sin un límite, una llamada colgada consume recursos y deja al usuario esperando. El timeout convierte una espera indefinida en una falla controlada.

Hay dos mecanismos frecuentes:

| Mecanismo | Qué hace | Cuándo usarlo |
|---|---|---|
| `WaitAsync(TimeSpan)` | Deja de esperar y lanza `TimeoutException` | Cuando solo importa no bloquear al llamador |
| `CancellationTokenSource` con `CancelAfter` | Cancela la operación de verdad | Cuando querés detener el trabajo y liberar recursos |

`WaitAsync` no cancela la tarea original. Si la operación sigue viva, puede terminar y consumir recursos sin que nadie mire el resultado. Para un timeout real, creá un origen vinculado al token del usuario y usá `CancelAfter`.

Los reintentos sirven solo para fallas transitorias: timeouts, conexiones rechazadas, respuestas 503. Un argumento inválido o un 404 no se arreglan repitiendo la llamada. Reintentar todo es una forma cara de empeorar el problema.

La espera entre intentos debe crecer. Si reintentás de inmediato, golpeás un servicio que ya está saturado. Un patrón simple es esperar 100 ms, 200 ms y 400 ms. El backoff exponencial multiplica la espera en cada intento.

El jitter agrega una variación aleatoria a esa espera. Sin jitter, miles de clientes reintentan en el mismo instante y generan otro pico. Con jitter, la carga se reparte.

Todo reintento debe respetar el token de cancelación. Si el usuario cerró la pantalla, no tiene sentido seguir intentando durante diez segundos. `Task.Delay(espera, token)` corta la espera y propaga `OperationCanceledException`.

La resiliencia no es solo reintentar. También incluye limitar la cantidad de intentos, fallar rápido cuando el error es permanente, no reintentar operaciones no idempotentes y considerar un cortacircuito cuando el servicio lleva mucho tiempo caído.

## Ejemplo mínimo

```csharp
try
{
    string resultado = await ConsultarAsync().WaitAsync(TimeSpan.FromSeconds(1));
    Console.WriteLine(resultado);
}
catch (TimeoutException)
{
    Console.WriteLine("La consulta superó el límite de 1 segundo");
}

static async Task<string> ConsultarAsync()
{
    await Task.Delay(3_000);
    return "respuesta";
}
```

```text
La consulta superó el límite de 1 segundo
```

El programa deja de esperar al segundo, aunque la consulta interna siga corriendo. Para cortarla de verdad, habría que pasarle un token cancelable y usar `CancelAfter`.

## Ejemplo aplicado

Un cálculo falla dos veces por una condición transitoria y funciona al tercer intento. El bucle aplica espera creciente y respeta la cancelación.

```csharp
using var presupuesto = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var servicio = new ServicioInestable();

try
{
    decimal total = await ReintentarAsync(
        token => servicio.CalcularAsync(token),
        intentos: 3,
        presupuesto.Token);

    Console.WriteLine($"Total: {total:N0}");
}
catch (Exception ex) when (ex is TimeoutException or ServicioNoDisponibleException)
{
    Console.WriteLine($"Sin resultado: {ex.Message}");
}

static async Task<T> ReintentarAsync<T>(
    Func<CancellationToken, Task<T>> operacion,
    int intentos,
    CancellationToken token)
{
    for (int intento = 1; intento <= intentos; intento++)
    {
        try
        {
            return await operacion(token).WaitAsync(TimeSpan.FromSeconds(1), token);
        }
        catch (Exception ex) when
            ((ex is TimeoutException or ServicioNoDisponibleException) && intento < intentos)
        {
            int espera = 100 * intento;
            Console.WriteLine($"Intento {intento} falló: {ex.Message} Reintento en {espera} ms.");
            await Task.Delay(espera, token);
        }
    }

    throw new ServicioNoDisponibleException("Se agotaron los intentos.");
}

public sealed class ServicioInestable
{
    private int _llamadas;

    public async Task<decimal> CalcularAsync(CancellationToken token)
    {
        await Task.Delay(100, token);
        _llamadas++;

        if (_llamadas < 3)
        {
            throw new ServicioNoDisponibleException($"Falla simulada número {_llamadas}.");
        }

        return 42_000m;
    }
}

public sealed class ServicioNoDisponibleException(string message) : Exception(message);
```

```text
Intento 1 falló: Falla simulada número 1. Reintento en 100 ms.
Intento 2 falló: Falla simulada número 2. Reintento en 200 ms.
Total: 42.000
```

El helper recibe una función que produce una tarea, así sirve para cualquier operación. El filtro del `catch` combina dos condiciones: tipo transitorio y que queden intentos. Si el token se cancela, `WaitAsync` o `Task.Delay` lanzan `OperationCanceledException`, que no coincide con el filtro y corta el bucle.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Se reintenta un error permanente | El `catch` atrapa cualquier excepción | Filtrar solo fallas transitorias |
| El servicio se satura con reintentos | No hay espera entre intentos | Aplicar backoff y jitter |
| El timeout no detiene el trabajo | Solo se usó `WaitAsync` | Usar `CancelAfter` con un token vinculado |
| La cancelación tarda diez segundos | `Task.Delay` no recibe el token | Pasarlo en cada espera y en la operación |
| Se reintenta un cobro sin idempotencia | La operación no se puede repetir sin efectos | Usar una clave de idempotencia o no reintentar |
| El bucle nunca termina | No hay límite de intentos | Definir un máximo y fallar con contexto |

## Buenas prácticas

- Reintentar solo errores transitorios y operaciones idempotentes.
- Limitar la cantidad de intentos y el tiempo total.
- Usar espera creciente con jitter para no sincronizar clientes.
- Propagar el token de cancelación a la operación y a cada espera.
- Registrar cada intento con su identificador para poder diagnosticar.

## Ejercicio

Implementá `EjecutarConResilienciaAsync` que reciba una operación `Func<CancellationToken, Task<string>>`, intente hasta tres veces con esperas de 100, 200 y 300 milisegundos, y aplique un timeout de 500 milisegundos por intento. Simulá un servicio que falla una vez y después responde. Si se agotan los intentos, lanzá `ServicioNoDisponibleException`.

<details>
<summary>Ver solución</summary>

```csharp
using var presupuesto = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var servicio = new ServicioIntermitente();

try
{
    string resultado = await EjecutarConResilienciaAsync(servicio.ObtenerAsync, presupuesto.Token);
    Console.WriteLine($"Resultado: {resultado}");
}
catch (Exception ex) when (ex is TimeoutException or ServicioNoDisponibleException)
{
    Console.WriteLine($"No se pudo completar: {ex.Message}");
}

static async Task<string> EjecutarConResilienciaAsync(
    Func<CancellationToken, Task<string>> operacion,
    CancellationToken token)
{
    for (int intento = 1; intento <= 3; intento++)
    {
        try
        {
            return await operacion(token).WaitAsync(TimeSpan.FromMilliseconds(500), token);
        }
        catch (Exception ex) when
            ((ex is TimeoutException or ServicioNoDisponibleException) && intento < 3)
        {
            int espera = 100 * intento;
            Console.WriteLine($"Intento {intento} falló. Espero {espera} ms.");
            await Task.Delay(espera, token);
        }
    }

    throw new ServicioNoDisponibleException("Los tres intentos fallaron.");
}

public sealed class ServicioIntermitente
{
    private int _llamadas;

    public async Task<string> ObtenerAsync(CancellationToken token)
    {
        await Task.Delay(50, token);
        _llamadas++;

        if (_llamadas == 1)
        {
            throw new ServicioNoDisponibleException("El servicio no estaba listo.");
        }

        return "datos actualizados";
    }
}

public sealed class ServicioNoDisponibleException(string message) : Exception(message);
```

```text
Intento 1 falló. Espero 100 ms.
Resultado: datos actualizados
```

El segundo intento responde bien. Si los tres fallaran, el bucle saldría por la excepción final y el `catch` del programa principal la mostraría.

</details>

## Resumen

- Un timeout evita esperas indefinidas; `WaitAsync` limita la espera y un token cancelable detiene el trabajo.
- Solo se reintentan fallas transitorias e idempotentes.
- El backoff creciente y el jitter reducen la presión sobre el servicio.
- La cancelación debe atravesar la operación y las esperas entre intentos.
- Los reintentos necesitan un límite y un error final con contexto.

## Siguiente paso

Ya tenés todas las piezas del módulo. En el [ejercicio integrador](/modulos/async-errores/ejercicio-integrador) vas a combinarlas en un cotizador que trabaja en paralelo, tolera fallas y respeta un presupuesto de tiempo.
