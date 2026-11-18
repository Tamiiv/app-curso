---
slug: logging
title: "Logging estructurado con ILogger"
order: 7
estimatedMinutes: 15
summary: "Registrá eventos con niveles, plantillas de mensaje y datos con nombre usando ILogger, para diagnosticar sin depender de textos concatenados."
quiz:
  - question: "¿Por qué conviene usar plantillas como \"Pedido {PedidoId}\" en lugar de interpolación?"
    options:
      - "Porque la interpolación no compila en .NET 10"
      - "Porque conserva los valores como datos estructurados y evita formatear si el nivel está deshabilitado"
      - "Porque ocupa más espacio en el archivo de log"
      - "Porque ILogger no acepta strings"
    answer: 1
    explanation: "La plantilla separa el mensaje de los valores. Los proveedores pueden capturarlos como campos y el formateo se evita cuando el nivel no está habilitado."
  - question: "¿Qué nivel usar para una falla que impide completar una operación importante?"
    options:
      - "Trace"
      - "Debug"
      - "Information"
      - "Error"
    answer: 3
    explanation: "Error registra fallas que impiden completar la operación actual. Critical se reserva para fallas que comprometen todo el proceso."
  - question: "¿Qué representa el parámetro T de ILogger<T>?"
    options:
      - "El tipo del mensaje"
      - "La categoría del logger, que suele ser la clase que lo usa"
      - "El nivel mínimo de log"
      - "El proveedor de destino"
    answer: 1
    explanation: "T identifica la categoría. Así se puede filtrar por clase o espacio de nombres en la configuración."
  - question: "¿Cómo se registra una excepción junto al mensaje?"
    options:
      - "Concatenando ex.ToString() al mensaje"
      - "Pasándola como primer argumento de LogError"
      - "Llamando a Console.WriteLine por separado"
      - "Guardándola en una variable estática"
    answer: 1
    explanation: "Las sobrecargas de LogError y LogCritical reciben la excepción como primer argumento, y los proveedores la registran con su pila."
---

# Logging estructurado con ILogger

## Objetivo

Registrar eventos con `ILogger`, elegir el nivel adecuado, escribir mensajes estructurados con datos con nombre y adjuntar excepciones sin perder contexto.

## Requisitos previos

Necesitás manejar interfaces, inyección de dependencias por constructor y excepciones. Si venís de la lección anterior, ya viste cómo crear [excepciones personalizadas](/modulos/async-errores/excepciones-personalizadas) que conviene registrar con sus datos.

## Concepto

`ILogger<T>` es la abstracción de logging de .NET. No escribe en un archivo ni en la consola: entrega el evento a uno o más proveedores, que deciden el destino. `T` es la categoría, casi siempre la clase que usa el logger.

Los niveles indican la importancia:

| Nivel | Uso típico |
|---|---|
| `Trace` | Detalle muy fino, solo para diagnóstico profundo |
| `Debug` | Información útil durante el desarrollo |
| `Information` | Eventos normales del negocio |
| `Warning` | Algo raro que no impide continuar |
| `Error` | Una operación falló |
| `Critical` | El proceso o una parte esencial quedó inutilizable |

La configuración decide qué niveles se emiten. En producción suele quedar `Information` o `Warning`; en desarrollo se baja a `Debug`.

El logging estructurado usa plantillas con marcadores entre llaves:

```csharp
logger.LogInformation("Pedido {PedidoId} creado para {Cliente}", pedidoId, cliente);
```

Los valores no se concatenan. El proveedor recibe el mensaje y los datos por separado, y puede guardarlos como campos consultables. La interpolación (`$"Pedido {pedidoId}"`) pierde esa estructura y además formatea aunque el nivel esté deshabilitado.

Los nombres de los marcadores importan: `{PedidoId}` y `{pedidoId}` son claves distintas en muchos proveedores. Usá `PascalCase` y sé consistente.

Para excepciones, usá la sobrecarga que las recibe como primer argumento:

```csharp
logger.LogError(ex, "No se pudo cobrar el pedido {PedidoId}", pedidoId);
```

Así el proveedor guarda el tipo, el mensaje y la pila. No hace falta `ex.ToString()` dentro del mensaje.

Los ámbitos (`BeginScope`) agregan datos a todos los eventos dentro de un bloque, por ejemplo el identificador de correlación de una solicitud. En una aplicación ASP.NET Core se configuran desde el host; en una biblioteca solo se declaran.

`ILogger<T>` viene de `Microsoft.Extensions.Logging.Abstractions`. En ASP.NET Core ya está incluido en el framework compartido; en una aplicación de consola pura se agrega el paquete de Microsoft correspondiente. No hace falta ninguna dependencia de terceros.

## Ejemplo mínimo

```csharp
using Microsoft.Extensions.Logging;

ILogger<Program> logger = new ConsolaLogger<Program>();

logger.LogInformation("Pedido {PedidoId} creado para {Cliente}", 42, "Ana Torres");
logger.LogWarning("Stock bajo para {Sku}: quedan {Unidades}", "SKU-7", 3);

public sealed class ConsolaLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        Console.WriteLine($"[{logLevel}] {typeof(T).Name}: {formatter(state, exception)}");
    }
}
```

```text
[Information] Program: Pedido 42 creado para Ana Torres
[Warning] Program: Stock bajo para SKU-7: quedan 3
```

La plantilla produce el texto final, pero los valores viajan por separado hasta el proveedor. El logger de ejemplo imprime el nivel y la categoría; uno real agrega fecha, ámbito y destino.

## Ejemplo aplicado

Un servicio de pagos registra el inicio, el resultado y las fallas. La excepción se pasa como argumento, no se concatena.

```csharp
using Microsoft.Extensions.Logging;

ILogger<ServicioPagos> logger = new ConsolaLogger<ServicioPagos>();
var servicio = new ServicioPagos(logger);

servicio.Cobrar("PED-1", 15_000m);
servicio.Cobrar("PED-2", -500m);

public sealed class ServicioPagos(ILogger<ServicioPagos> logger)
{
    public void Cobrar(string pedido, decimal monto)
    {
        logger.LogInformation("Iniciando cobro de {Monto} para {Pedido}", monto, pedido);

        if (monto <= 0)
        {
            var error = new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser positivo.");
            logger.LogError(error, "Cobro rechazado para {Pedido}", pedido);
            return;
        }

        logger.LogInformation("Cobro aprobado para {Pedido} por {Monto}", pedido, monto);
    }
}

public sealed class ConsolaLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        Console.WriteLine($"[{logLevel}] {typeof(T).Name}: {formatter(state, exception)}");

        if (exception is not null)
        {
            Console.WriteLine($"  {exception.GetType().Name}: {exception.Message}");
        }
    }
}
```

```text
[Information] ServicioPagos: Iniciando cobro de 15000 para PED-1
[Information] ServicioPagos: Cobro aprobado para PED-1 por 15000
[Information] ServicioPagos: Iniciando cobro de -500 para PED-2
[Error] ServicioPagos: Cobro rechazado para PED-2
  ArgumentOutOfRangeException: El monto debe ser positivo. (Parameter 'monto')
```

Cada evento tiene categoría, nivel y datos con nombre. En un proveedor real, `{Pedido}` y `{Monto}` se pueden filtrar o agregar sin analizar el texto.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Se usa interpolación en el mensaje | Se pierden los campos estructurados | Usar plantilla con `{Nombre}` y argumentos |
| Se registra la misma falla en cada capa | Cada `catch` vuelve a loguear | Registrar una vez, en el borde que decide |
| La excepción va dentro del texto | Se concatena `ex.ToString()` | Pasarla como primer argumento |
| Se escribe en `Information` todo | El log se vuelve ruido | Reservar `Information` para eventos de negocio |
| Se registran datos sensibles | Se copia el objeto completo | Loguear solo identificadores y datos necesarios |
| El logger se crea con `new` en cada método | Se evita la configuración y el filtrado | Inyectar `ILogger<T>` por constructor |

## Buenas prácticas

- Usar plantillas con nombres claros y consistentes, no interpolación.
- Elegir el nivel según la importancia real del evento.
- Incluir identificadores de correlación para seguir un flujo entre capas.
- Registrar excepciones como argumento y una sola vez por operación.
- No loguear contraseñas, tokens ni datos personales innecesarios.

## Ejercicio

Escribí un `ServicioInventario` que reciba `ILogger<ServicioInventario>` y tenga un método `Reservar(sku, cantidad)`. Si la cantidad supera el stock, debe registrar un `Warning` con el SKU y las unidades disponibles; si todo sale bien, un `Information`; si el SKU está vacío, un `Error` con una `ArgumentException`. Mostrá la salida con un logger de consola.

<details>
<summary>Ver solución</summary>

```csharp
using Microsoft.Extensions.Logging;

ILogger<ServicioInventario> logger = new ConsolaLogger<ServicioInventario>();
var inventario = new ServicioInventario(logger);

inventario.Reservar("SKU-1", 2);
inventario.Reservar("SKU-2", 10);
inventario.Reservar("", 1);

public sealed class ServicioInventario(ILogger<ServicioInventario> logger)
{
    private readonly Dictionary<string, int> _stock = new()
    {
        ["SKU-1"] = 5,
        ["SKU-2"] = 3
    };

    public void Reservar(string sku, int cantidad)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            var error = new ArgumentException("El SKU es obligatorio.", nameof(sku));
            logger.LogError(error, "Reserva rechazada por SKU inválido");
            return;
        }

        int disponible = _stock.GetValueOrDefault(sku);

        if (cantidad > disponible)
        {
            logger.LogWarning(
                "Stock insuficiente para {Sku}: pediste {Cantidad} y hay {Disponible}",
                sku, cantidad, disponible);
            return;
        }

        _stock[sku] = disponible - cantidad;
        logger.LogInformation("Reserva confirmada de {Cantidad} unidades de {Sku}", cantidad, sku);
    }
}

public sealed class ConsolaLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");

        if (exception is not null)
        {
            Console.WriteLine($"  {exception.GetType().Name}: {exception.Message}");
        }
    }
}
```

```text
[Information] Reserva confirmada de 2 unidades de SKU-1
[Warning] Stock insuficiente para SKU-2: pediste 10 y hay 3
[Error] Reserva rechazada por SKU inválido
  ArgumentException: El SKU es obligatorio. (Parameter 'sku')
```

Cada caso usa el nivel adecuado y los datos quedan como campos separados. El logger de ejemplo no incluye fecha ni ámbito, pero la interfaz es la misma que usa ASP.NET Core.

</details>

## Resumen

- `ILogger<T>` abstrae el destino y usa `T` como categoría.
- Los niveles permiten filtrar por importancia sin cambiar el código.
- Las plantillas con `{Nombre}` conservan datos estructurados.
- Las excepciones se pasan como primer argumento de `LogError`.
- Registrar una sola vez y con contexto evita duplicados y ruido.

## Siguiente paso

Un log te dice qué falló, pero no evita que vuelva a fallar. En la próxima lección vas a aplicar [timeouts, reintentos y resiliencia](/modulos/async-errores/resiliencia) para tolerar fallas temporales.
