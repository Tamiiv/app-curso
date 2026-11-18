---
slug: ejercicio-integrador
title: "Ejercicio integrador"
order: 9
estimatedMinutes: 25
summary: "Combiná tareas en paralelo, reintentos, timeouts, cancelación y excepciones personalizadas en un cotizador que resiste fallas parciales."
quiz:
  - question: "¿Cómo evitás que un SKU fallido aborte la cotización completa?"
    options:
      - "Usando Task.WhenAny en lugar de WhenAll"
      - "Capturando el error dentro de la tarea de cada SKU y devolviendo un resultado"
      - "Envolviendo todo el lote en un solo try/catch"
      - "Reintentando el lote completo desde cero"
    answer: 1
    explanation: "Si cada tarea convierte su falla en un resultado, WhenAll termina normalmente y el lote conserva los éxitos aunque haya fallas parciales."
  - question: "¿Para qué se crea un CancellationTokenSource vinculado dentro de cada intento?"
    options:
      - "Para duplicar el token global"
      - "Para combinar el presupuesto global con un timeout propio del intento"
      - "Para cancelar los otros SKU"
      - "Para reiniciar el contador de intentos"
    answer: 1
    explanation: "CreateLinkedTokenSource une la cancelación general con el límite de tiempo de ese intento, y cualquiera de los dos puede cortarlo."
  - question: "Si se agota el presupuesto global, ¿qué debe ocurrir?"
    options:
      - "Los resultados parciales se devuelven igual"
      - "La cancelación se propaga y el lote se interrumpe"
      - "Se ignoran los tokens y se termina el trabajo"
      - "Se reintenta con un presupuesto mayor"
    answer: 1
    explanation: "El token global representa el límite de la operación completa. Al cancelarse, la excepción sube y el llamador decide si muestra lo que había."
  - question: "¿Cuál es el orden correcto de las protecciones?"
    options:
      - "Reintento global, timeout por SKU y cancelación por intento"
      - "Timeout por intento dentro del reintento, y presupuesto global por encima del lote"
      - "Solo timeout global, sin reintentos"
      - "Primero reintentar y después aplicar cualquier timeout"
    answer: 1
    explanation: "Cada intento tiene su límite, el reintento envuelve esos intentos y el presupuesto global envuelve todo el lote."
---

# Ejercicio integrador

## Objetivo

Integrar tareas, errores, cancelación, excepciones personalizadas, logging y resiliencia en un programa que cotiza varios productos en paralelo y sobrevive a fallas parciales.

## Requisitos previos

Necesitás las ocho lecciones anteriores: [`Task`](/modulos/async-errores/task-asincronia), [`async`/`await`](/modulos/async-errores/async-await), [errores asíncronos](/modulos/async-errores/errores-async), [cancelación](/modulos/async-errores/cancellation-token), [try/catch/finally](/modulos/async-errores/try-catch-finally), [excepciones personalizadas](/modulos/async-errores/excepciones-personalizadas), [logging](/modulos/async-errores/logging) y [resiliencia](/modulos/async-errores/resiliencia).

## Concepto

La tienda Ruta .NET necesita cotizar una lista de SKU contra un proveedor externo. El proveedor es lento, a veces falla y otras veces no responde. El informe debe incluir todos los SKU que se pudieron cotizar y explicar qué pasó con los demás.

El diseño separa tres niveles de protección:

| Nivel | Responsabilidad | Herramienta |
|---|---|---|
| Lote | Ejecutar todos los SKU y reunir resultados | `Task.WhenAll` |
| SKU | Reintentar fallas transitorias | Bucle con backoff |
| Intento | No esperar más de dos segundos | Token vinculado con `CancelAfter` |

La cancelación global se suma por encima: si se agota el presupuesto total, el lote se interrumpe. Un timeout por intento no debe confundirse con esa cancelación; el primero es una falla transitoria, la segunda es una decisión del llamador.

La clave para tolerar fallas parciales es no dejar que una excepción salga de la tarea de un SKU. Cada tarea captura lo esperable y devuelve un `ResultadoCotizacion` con precio o con error. `WhenAll` solo falla si alguna tarea deja escapar una excepción no controlada.

Las excepciones personalizadas aportan el tipo y los datos. El logging deja rastro de los reintentos. La resiliencia decide cuándo dejar de intentar.

## Ejemplo mínimo

```csharp
var servicio = new ServicioPrecios();

try
{
    decimal precio = await servicio.ObtenerAsync("SKU-4");
    Console.WriteLine($"Precio: {precio:N0}");
}
catch (ServicioNoDisponibleException ex)
{
    Console.WriteLine($"No se pudo cotizar: {ex.Message}");
}

public sealed class ServicioPrecios
{
    public async Task<decimal> ObtenerAsync(string sku)
    {
        await Task.Delay(100);

        if (sku == "SKU-4")
        {
            throw new ServicioNoDisponibleException($"El proveedor no tiene {sku}.");
        }

        return 12_500m;
    }
}

public sealed class ServicioNoDisponibleException(string message) : Exception(message);
```

```text
No se pudo cotizar: El proveedor no tiene SKU-4.
```

La excepción personalizada distingue una falla esperable del proveedor. En el lote, esa excepción se convierte en un resultado con error para que los demás SKU continúen.

## Ejemplo aplicado

Aislar la falla de cada SKU es lo que permite devolver un informe parcial.

```csharp
var servicio = new ServicioPrecios();
string[] skus = ["SKU-1", "SKU-2", "SKU-4"];

Task<ResultadoCotizacion>[] tareas = skus
    .Select(sku => CotizarSeguroAsync(sku, servicio))
    .ToArray();

ResultadoCotizacion[] resultados = await Task.WhenAll(tareas);

foreach (ResultadoCotizacion resultado in resultados)
{
    Console.WriteLine(resultado.Exito
        ? $"{resultado.Sku}: {resultado.Precio:N0}"
        : $"{resultado.Sku}: {resultado.Error}");
}

static async Task<ResultadoCotizacion> CotizarSeguroAsync(string sku, ServicioPrecios servicio)
{
    try
    {
        decimal precio = await servicio.ObtenerAsync(sku);
        return new ResultadoCotizacion(sku, precio, null);
    }
    catch (ServicioNoDisponibleException ex)
    {
        return new ResultadoCotizacion(sku, null, ex.Message);
    }
}

public sealed record ResultadoCotizacion(string Sku, decimal? Precio, string? Error)
{
    public bool Exito => Error is null;
}

public sealed class ServicioPrecios
{
    public async Task<decimal> ObtenerAsync(string sku)
    {
        await Task.Delay(100);

        if (sku == "SKU-4")
        {
            throw new ServicioNoDisponibleException($"El proveedor no tiene {sku}.");
        }

        return 12_500m;
    }
}

public sealed class ServicioNoDisponibleException(string message) : Exception(message);
```

```text
SKU-1: 12.500
SKU-2: 12.500
SKU-4: El proveedor no tiene SKU-4.
```

El `catch` está dentro de `CotizarSeguroAsync`, no alrededor de `WhenAll`. Así cada tarea resuelve su propio error y el lote entero no se cae por un SKU.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Un SKU fallido corta el lote | La excepción escapa de la tarea | Capturar dentro de la tarea y devolver un resultado |
| El timeout de un intento cancela todo | Se usa el token global en lugar de uno vinculado | Crear un origen vinculado por intento |
| Se pierden los resultados exitosos | Se captura la excepción del `WhenAll` | Modelar fallas como resultados parciales |
| Los reintentos no respetan la cancelación | `Task.Delay` sin token | Propagar el token en cada espera |
| El informe no explica las fallas | Solo se guarda el precio | Incluir el motivo en el resultado |
| Se espera el lote sin límite global | No hay presupuesto total | Envolver con un token con `CancelAfter` |

## Buenas prácticas

- Modelar el resultado de cada ítem antes de lanzar el lote.
- Separar el timeout por intento del presupuesto global.
- Reintentar solo fallas transitorias y con un máximo claro.
- Registrar reintentos y fallas con identificadores del dominio.
- Devolver un informe parcial en lugar de perder todo el trabajo.

## Ejercicio

Implementá un `Cotizador` que reciba una lista de SKU y un `CancellationToken` global. Por cada SKU debe intentar hasta tres veces, con un timeout de dos segundos por intento y esperas de 100 y 200 milisegundos. Si un SKU no se puede cotizar, debe quedar en el informe con el motivo, sin abortar los demás. El programa debe usar un presupuesto global de diez segundos, mostrar una tabla con SKU, precio y estado, e informar el total de los precios obtenidos.

<details>
<summary>Ver solución</summary>

```csharp
using var presupuesto = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var servicio = new ServicioPrecios();
var cotizador = new Cotizador(servicio);

try
{
    IReadOnlyList<ResultadoCotizacion> resultados = await cotizador.CotizarAsync(
        ["SKU-1", "SKU-2", "SKU-3", "SKU-4"], presupuesto.Token);

    Console.WriteLine();
    Console.WriteLine($"{"SKU",-8} {"Precio",-10} Estado");

    foreach (ResultadoCotizacion resultado in resultados)
    {
        string precio = resultado.Exito ? resultado.Precio!.Value.ToString("N0") : "-";
        Console.WriteLine($"{resultado.Sku,-8} {precio,-10} {resultado.Estado}");
    }

    decimal total = resultados.Where(r => r.Exito).Sum(r => r.Precio!.Value);
    Console.WriteLine($"Total cotizado: {total:N0}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("La cotización se canceló antes de terminar.");
}

public sealed record ResultadoCotizacion(string Sku, decimal? Precio, string? Error)
{
    public bool Exito => Error is null;
    public string Estado => Exito ? "OK" : Error!;
}

public sealed class ServicioPrecios
{
    private int _fallasSku2;

    public async Task<decimal> ObtenerPrecioAsync(string sku, CancellationToken token)
    {
        await Task.Delay(150, token);

        if (sku == "SKU-2" && Interlocked.Increment(ref _fallasSku2) == 1)
        {
            throw new ServicioNoDisponibleException("El proveedor no respondió al primer intento.");
        }

        if (sku == "SKU-4")
        {
            throw new ServicioNoDisponibleException("El SKU-4 no está disponible.");
        }

        return sku switch
        {
            "SKU-1" => 12_500m,
            "SKU-2" => 8_900m,
            "SKU-3" => 45_000m,
            _ => 0m
        };
    }
}

public sealed class ServicioNoDisponibleException(string message) : Exception(message);

public sealed class Cotizador(ServicioPrecios servicio)
{
    public async Task<IReadOnlyList<ResultadoCotizacion>> CotizarAsync(
        IEnumerable<string> skus,
        CancellationToken token)
    {
        Task<ResultadoCotizacion>[] tareas = skus
            .Select(sku => CotizarUnoAsync(sku, token))
            .ToArray();

        return await Task.WhenAll(tareas);
    }

    private async Task<ResultadoCotizacion> CotizarUnoAsync(string sku, CancellationToken token)
    {
        for (int intento = 1; intento <= 3; intento++)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(2));

                decimal precio = await servicio.ObtenerPrecioAsync(sku, timeout.Token);
                return new ResultadoCotizacion(sku, precio, null);
            }
            catch (ServicioNoDisponibleException) when (intento < 3)
            {
                Console.WriteLine($"Reintento {intento} para {sku}");
                await Task.Delay(100 * intento, token);
            }
            catch (ServicioNoDisponibleException ex)
            {
                return new ResultadoCotizacion(sku, null, ex.Message);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                return new ResultadoCotizacion(sku, null, "Timeout");
            }
        }

        return new ResultadoCotizacion(sku, null, "No disponible");
    }
}
```

```text
Reintento 1 para SKU-2
Reintento 1 para SKU-4
Reintento 2 para SKU-4

SKU      Precio     Estado
SKU-1    12.500     OK
SKU-2    8.900      OK
SKU-3    45.000     OK
SKU-4    -          El SKU-4 no está disponible.
Total cotizado: 66.400
```

Cada SKU resuelve su propio resultado, así que SKU-4 no afecta a los demás. El token vinculado convierte un intento lento en un timeout controlado, y el presupuesto global sigue siendo la última palabra.

</details>

## Resumen

- `Task.WhenAll` permite cotizar en paralelo y reunir los resultados en orden.
- Aislar las fallas por ítem conserva los éxitos del lote.
- El timeout por intento se combina con el token global mediante un origen vinculado.
- Las excepciones personalizadas y los resultados explícitos explican cada falla.
- Un presupuesto global evita que la operación completa quede colgada.

## Siguiente paso

Con asincronía, errores y logging bajo control, el próximo paso es exponer todo esto como un servicio web. Continuá con el módulo de [ASP.NET Core y APIs](/modulos/aspnet-core-apis) para construir endpoints que usen estas mismas herramientas.
