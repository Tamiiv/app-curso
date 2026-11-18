---
slug: try-catch-finally
title: "try/catch/finally y filtros de excepción"
order: 5
estimatedMinutes: 15
summary: "Estructurá el manejo de errores con catch específicos, filtros when y limpieza garantizada en finally, sin perder la información original."
quiz:
  - question: "¿Cuándo se evalúa la condición de un filtro when?"
    options:
      - "Después de ejecutar el bloque catch"
      - "Antes de entrar al catch, mientras la excepción todavía está en vuelo"
      - "Solo cuando el catch vuelve a lanzar la excepción"
      - "Al compilar, porque debe ser una constante"
    answer: 1
    explanation: "El filtro se evalúa antes de desenrollar la pila. Si devuelve false, el catch no se ejecuta y la búsqueda de otro catch continúa."
  - question: "¿Qué diferencia hay entre throw; y throw ex; dentro de un catch?"
    options:
      - "Ninguna, son equivalentes"
      - "throw; conserva el seguimiento de la pila original y throw ex; lo reinicia"
      - "throw ex; conserva la pila y throw; la borra"
      - "throw; solo funciona en finally"
    answer: 1
    explanation: "throw; relanza la misma excepción sin tocar el seguimiento. throw ex; se comporta como una excepción nueva y pierde el origen real del error."
  - question: "¿Se ejecuta el bloque finally si el try hace return?"
    options:
      - "No, el return corta todo"
      - "Sí, siempre que el proceso no termine de forma abrupta"
      - "Solo si hay una excepción"
      - "Solo si el return es de tipo void"
    answer: 1
    explanation: "finally se ejecuta al salir del bloque por cualquier camino: return, excepción o final normal. Es el lugar para liberar recursos."
  - question: "¿En qué orden conviene escribir los catch?"
    options:
      - "Del más general al más específico"
      - "Del más específico al más general"
      - "En cualquier orden, el compilador elige"
      - "Primero Exception y después los tipos concretos"
    answer: 1
    explanation: "Si un catch general va primero, tapa a los específicos y el compilador marca los posteriores como inalcanzables."
---

# try/catch/finally y filtros de excepción

## Objetivo

Ordenar el manejo de errores para capturar solo lo que sabés resolver, usar filtros para decidir con precisión y garantizar la limpieza de recursos con `finally`.

## Requisitos previos

Necesitás entender cómo se propagan las excepciones en métodos síncronos y asíncronos. Si venís del módulo, ya viste [errores en código asíncrono](/modulos/async-errores/errores-async) y [cancelación con tokens](/modulos/async-errores/cancellation-token).

## Concepto

Un bloque `try` delimita el código que puede fallar. Los `catch` deciden qué hacer con cada tipo de error y `finally` ejecuta la limpieza pase lo que pase.

El orden importa. Los `catch` se evalúan de arriba hacia abajo y gana el primero compatible. Si ponés `catch (Exception)` primero, ningún `catch` posterior se alcanza. La regla es capturar primero los tipos más específicos.

Los filtros `when` agregan una condición:

```csharp
catch (ArgumentException ex) when (ex.ParamName == "cantidad")
{
    Console.WriteLine("La cantidad es inválida.");
}
```

La ventaja es que la excepción sigue su curso si el filtro no se cumple. No hace falta capturarla, revisar y relanzar, lo que evita ensuciar la pila y perder información. Además, el depurador se detiene en el punto original cuando el filtro falla.

`finally` se ejecuta al salir del `try` por cualquier camino: terminación normal, `return`, `break` o excepción. No se ejecuta si el proceso se cae de forma abrupta, pero para el código administrado cubre la limpieza habitual.

Para recursos que implementan `IDisposable`, `using` es más conciso que `finally`. Un `using var` declara el recurso y lo libera al final del ámbito, incluso si hay una excepción. En código asíncrono existe `await using` para recursos `IAsyncDisposable`.

Dos errores clásicos: tragarse la excepción sin hacer nada y relanzarla con `throw ex;`. El primero oculta fallas; el segundo reinicia el seguimiento de la pila. Si necesitás relanzar, usá `throw;`. Si necesitás agregar contexto, usá `throw new MiException("contexto", ex);` y guardá la excepción original como interna.

Un `catch` no debería existir solo para registrar y continuar como si nada. Si no podés reparar el problema, dejá que suba o relanzá con contexto. Capturar es una decisión de diseño, no un reflejo.

## Ejemplo mínimo

```csharp
try
{
    int cantidad = int.Parse("muchos");
    Console.WriteLine($"Cantidad: {cantidad}");
}
catch (FormatException ex)
{
    Console.WriteLine($"Formato inválido: {ex.GetType().Name}");
}
finally
{
    Console.WriteLine("Limpieza completada");
}
```

```text
Formato inválido: FormatException
Limpieza completada
```

El `catch` captura solo el error de formato. Una falla distinta, como un valor nulo, no entraría en ese bloque y seguiría subiendo. El `finally` se ejecuta igual.

## Ejemplo aplicado

Un pedido se procesa dentro de una sesión que debe cerrarse siempre. Los filtros distinguen el argumento que falló sin capturar y relanzar.

```csharp
using var sesion = new Sesion("pedido-42");

try
{
    Procesar("SKU-1", 5);
    Procesar("SKU-2", -3);
}
catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "cantidad")
{
    Console.WriteLine($"Cantidad inválida: {ex.Message}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Argumento inválido: {ex.ParamName}");
}
finally
{
    Console.WriteLine("Cierre de sesión");
}

static void Procesar(string sku, int cantidad)
{
    if (string.IsNullOrWhiteSpace(sku))
    {
        throw new ArgumentException("El SKU es obligatorio.", nameof(sku));
    }

    if (cantidad <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(cantidad), cantidad, "La cantidad debe ser positiva.");
    }

    Console.WriteLine($"Pedido: {cantidad} x {sku}");
}

public sealed class Sesion : IDisposable
{
    private readonly string _nombre;

    public Sesion(string nombre)
    {
        _nombre = nombre;
        Console.WriteLine($"Sesión {_nombre} abierta");
    }

    public void Dispose() => Console.WriteLine($"Sesión {_nombre} cerrada");
}
```

```text
Sesión pedido-42 abierta
Pedido: 5 x SKU-1
Cantidad inválida: La cantidad debe ser positiva. (Parameter 'cantidad')
Actual value was -3.
Cierre de sesión
Sesión pedido-42 cerrada
```

El primer `catch` atrapa el error de cantidad y el segundo quedaría para otros argumentos inválidos. El `using` cierra la sesión después del `finally`, como corresponde al orden de los ámbitos.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Los `catch` específicos nunca se ejecutan | Hay un `catch (Exception)` antes | Ordenar de específico a general |
| Se pierde el origen del error | Se relanza con `throw ex;` | Usar `throw;` o envolver con la excepción interna |
| La excepción desaparece | El `catch` no registra ni relanza | Registrar con contexto y relanzar o devolver un resultado explícito |
| El recurso queda abierto | La limpieza está solo en el camino feliz | Usar `finally` o `using` |
| El filtro tiene efectos secundarios pesados | Se llama a un servicio dentro de `when` | Mantener los filtros simples y predecibles |
| Se captura `OperationCanceledException` como error | No se distingue cancelación de falla | Filtrar o revisar `IsCancellationRequested` |

## Buenas prácticas

- Capturar solo las excepciones que podés manejar de verdad.
- Escribir los `catch` del tipo más específico al más general.
- Usar `when` para filtrar por propiedades en lugar de capturar, comparar y relanzar.
- Liberar recursos con `using`, `await using` o `finally`.
- Al agregar contexto, conservar la excepción original como `innerException`.

## Ejercicio

Escribí una calculadora que divida dos números ingresados como texto. Debe capturar `FormatException` para entradas no numéricas y `DivideByZeroException` para el divisor cero, mostrar un mensaje distinto en cada caso y ejecutar un `finally` que imprima "Operación finalizada". Probá con `"10"` y `"2"`, y después con `"10"` y `"0"`.

<details>
<summary>Ver solución</summary>

```csharp
Probar("10", "2");
Probar("10", "0");
Probar("diez", "2");

static void Probar(string textoA, string textoB)
{
    try
    {
        int a = int.Parse(textoA);
        int b = int.Parse(textoB);
        Console.WriteLine($"Resultado: {a / b}");
    }
    catch (DivideByZeroException)
    {
        Console.WriteLine("No se puede dividir por cero.");
    }
    catch (FormatException)
    {
        Console.WriteLine("Alguno de los valores no es un número entero.");
    }
    finally
    {
        Console.WriteLine("Operación finalizada");
    }
}
```

```text
Resultado: 5
Operación finalizada
No se puede dividir por cero.
Operación finalizada
Alguno de los valores no es un número entero.
Operación finalizada
```

El orden de los `catch` importa: primero el caso más específico de la división y después el problema de formato. El `finally` se ejecuta en los tres casos, incluso cuando el método termina por una excepción capturada.

</details>

## Resumen

- `try` delimita el código riesgoso, `catch` decide qué hacer y `finally` limpia.
- Los `catch` se evalúan en orden y ganan los específicos.
- Los filtros `when` permiten capturar solo cuando se cumple una condición.
- `throw;` conserva la pila; `throw ex;` la reinicia.
- `using` y `await using` reemplazan a `finally` para liberar recursos.

## Siguiente paso

Cuando los errores del dominio necesitan datos propios, una excepción genérica no alcanza. En la próxima lección vas a crear [excepciones personalizadas](/modulos/async-errores/excepciones-personalizadas) con información útil para quien las captura.
