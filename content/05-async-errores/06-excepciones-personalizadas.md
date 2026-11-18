---
slug: excepciones-personalizadas
title: "Excepciones personalizadas"
order: 6
estimatedMinutes: 12
summary: "Creá excepciones de dominio con nombre claro, datos propios y constructores bien diseñados para que quien las capture pueda tomar decisiones."
quiz:
  - question: "¿De qué tipo conviene heredar una excepción personalizada?"
    options:
      - "ApplicationException"
      - "Exception"
      - "SystemException"
      - "InvalidOperationException siempre"
    answer: 1
    explanation: "Exception es la base recomendada. ApplicationException existe por compatibilidad histórica y no aporta nada nuevo."
  - question: "¿Por qué conviene que una excepción personalizada sea sealed?"
    options:
      - "Para que herede de Exception"
      - "Para evitar jerarquías profundas y facilitar el mantenimiento"
      - "Porque el compilador lo exige"
      - "Para que no se pueda capturar"
    answer: 1
    explanation: "Si no hay un motivo claro para heredar de ella, sealed evita extensiones inesperadas y deja la jerarquía bajo control."
  - question: "¿Cuándo NO conviene crear una excepción personalizada?"
    options:
      - "Cuando el llamador necesita distinguir un error de dominio"
      - "Cuando el error tiene datos asociados, como un saldo o un SKU"
      - "Cuando el flujo normal de validación puede resolverse con un resultado"
      - "Cuando querés capturarla con un filtro por código"
    answer: 2
    explanation: "Las excepciones son para situaciones excepcionales. Una validación esperable se comunica mejor con un resultado explícito o un bool."
  - question: "¿Cómo se conserva la excepción original al envolverla en una personalizada?"
    options:
      - "Concatenando el mensaje anterior"
      - "Pasándola como innerException en el constructor"
      - "Guardándola en una variable estática"
      - "Volviendo a lanzarla con throw ex;"
    answer: 1
    explanation: "El parámetro innerException mantiene la cadena completa y permite diagnosticar la causa raíz sin perder información."
---

# Excepciones personalizadas

## Objetivo

Diseñar excepciones de dominio con información útil, constructores coherentes y nombres claros, y decidir cuándo conviene crear una en lugar de usar una existente.

## Requisitos previos

Necesitás manejar herencia, constructores, propiedades y el bloque `try/catch`. También conviene repasar [try/catch/finally y filtros](/modulos/async-errores/try-catch-finally), porque las excepciones personalizadas se capturan igual que las del framework.

## Concepto

Una excepción personalizada representa un error propio del dominio que no se explica bien con los tipos genéricos de .NET. `InvalidOperationException` sirve para muchos casos, pero no comunica que faltó stock ni permite leer cuántas unidades había.

Crear una excepción propia tiene sentido cuando se cumplen varias condiciones:

- Quien llama necesita distinguirla de otros errores para decidir.
- El error lleva datos asociados, como un SKU, un saldo o un límite.
- El nombre del tipo agrega claridad al flujo.

No tiene sentido crearla para una validación esperable que puede devolver un resultado, ni para reemplazar `ArgumentNullException` cuando el argumento es nulo.

La convención de nombres es clara: termina en `Exception` y describe el problema. `StockInsuficienteException` se entiende; `ErrorPedido` no.

Una excepción personalizada hereda de `Exception` y suele ofrecer:

| Constructor | Uso |
|---|---|
| `(string message)` | Mensaje simple |
| `(string message, Exception inner)` | Envolver otra excepción |
| Uno específico con datos | Conservar el contexto del error |

Los constructores de serialización quedaron obsoletos en .NET moderno y ya no se recomiendan. Si necesitás transportar excepciones entre procesos, usá un modelo propio, no la serialización binaria.

Las propiedades deben ser de solo lectura y describir el estado que causó el error. Eso permite filtros como:

```csharp
catch (StockInsuficienteException ex) when (ex.Solicitado > 10)
```

También podés agrupar errores relacionados con una clase base abstracta y un código. Así el llamador captura la familia completa o filtra por código sin conocer cada tipo.

Cuidado con dos extremos. Una excepción por cada condición vuelve el dominio difícil de usar. Una sola excepción con un string libre obliga a comparar mensajes, que es frágil. El punto medio son pocas excepciones, con datos tipados y nombres precisos.

## Ejemplo mínimo

```csharp
try
{
    Cobrar("SKU-9", 12);
}
catch (StockInsuficienteException ex)
{
    Console.WriteLine($"{ex.Sku}: pediste {ex.Solicitado}, hay {ex.Disponible}");
}

static void Cobrar(string sku, int cantidad)
{
    int disponible = 5;

    if (cantidad > disponible)
    {
        throw new StockInsuficienteException(sku, cantidad, disponible);
    }

    Console.WriteLine("Venta registrada");
}

public sealed class StockInsuficienteException : Exception
{
    public StockInsuficienteException(string sku, int solicitado, int disponible)
        : base($"Stock insuficiente para {sku}: pediste {solicitado} y hay {disponible}.")
    {
        Sku = sku;
        Solicitado = solicitado;
        Disponible = disponible;
    }

    public StockInsuficienteException(string message, Exception inner)
        : base(message, inner)
    {
        Sku = "desconocido";
    }

    public string Sku { get; }
    public int Solicitado { get; }
    public int Disponible { get; }
}
```

```text
SKU-9: pediste 12, hay 5
```

El `catch` no necesita analizar el mensaje: lee propiedades tipadas. El segundo constructor sirve para envolver fallas de infraestructura conservando la causa.

## Ejemplo aplicado

Una tienda agrupa sus errores de pedido en una base con código. El llamador filtra por código y deja el resto para un manejo general.

```csharp
try
{
    Confirmar("SKU-7", 10, 0m);
}
catch (PedidoException ex) when (ex.Codigo == "STOCK")
{
    Console.WriteLine($"Revisá stock: {ex.Message}");
}
catch (PedidoException ex)
{
    Console.WriteLine($"Pedido rechazado [{ex.Codigo}]: {ex.Message}");
}

static void Confirmar(string sku, int cantidad, decimal precio)
{
    int disponible = 4;

    if (cantidad > disponible)
    {
        throw new StockInsuficienteException(sku, cantidad, disponible);
    }

    if (precio <= 0)
    {
        throw new PrecioInvalidoException(precio);
    }

    Console.WriteLine("Pedido confirmado");
}

public abstract class PedidoException(string codigo, string message) : Exception(message)
{
    public string Codigo { get; } = codigo;
}

public sealed class StockInsuficienteException(string sku, int solicitado, int disponible)
    : PedidoException("STOCK", $"Stock insuficiente para {sku}: pediste {solicitado} y hay {disponible}.")
{
    public string Sku { get; } = sku;
    public int Solicitado { get; } = solicitado;
    public int Disponible { get; } = disponible;
}

public sealed class PrecioInvalidoException(decimal precio)
    : PedidoException("PRECIO", $"El precio {precio} debe ser mayor que cero.")
{
    public decimal Precio { get; } = precio;
}
```

```text
Revisá stock: Stock insuficiente para SKU-7: pediste 10 y hay 4.
```

La clase base permite un `catch` común; el filtro por `Codigo` distingue el caso que interesa. Si mañana aparece `PrecioInvalidoException`, el mismo `catch` general la va a informar sin tocar el código.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Se hereda de `ApplicationException` | Costumbre de versiones viejas | Heredar de `Exception` |
| El nombre no termina en `Exception` | Se trató como un DTO | Renombrar con la convención |
| Se comparan mensajes de error | La excepción no expone datos | Agregar propiedades de solo lectura |
| Se pierde la causa original | No se pasa `innerException` | Usar el constructor que recibe la excepción interna |
| Se usan excepciones para validar formularios | El flujo esperado se modela como error | Devolver un resultado o un `bool` |
| La jerarquía crece sin control | Cada caso tiene su propio tipo | Agrupar con una base y un código |

## Buenas prácticas

- Crear excepciones solo para errores excepcionales y accionables.
- Terminar el nombre en `Exception` y describir el problema.
- Marcar la clase como `sealed` salvo que exista una razón para heredar.
- Exponer datos tipados en propiedades de solo lectura.
- Envolver excepciones de infraestructura con `innerException` para no perder la causa.

## Ejercicio

Implementá una cuenta con `Saldo` y un método `Retirar(decimal monto)`. Si el monto supera el saldo, lanzá `SaldoInsuficienteException` con las propiedades `Saldo` y `Monto`. El programa debe intentar retirar 5.000 de una cuenta con 3.000 y mostrar un mensaje que incluya cuánto falta.

<details>
<summary>Ver solución</summary>

```csharp
var cuenta = new Cuenta(3_000m);

try
{
    cuenta.Retirar(5_000m);
}
catch (SaldoInsuficienteException ex) when (ex.Monto > ex.Saldo)
{
    decimal falta = ex.Monto - ex.Saldo;
    Console.WriteLine($"Saldo insuficiente: faltan {falta:N0}");
    Console.WriteLine($"Saldo actual: {ex.Saldo:N0}, monto pedido: {ex.Monto:N0}");
}

Console.WriteLine($"Saldo final: {cuenta.Saldo:N0}");

public sealed class Cuenta(decimal saldo)
{
    public decimal Saldo { get; private set; } = saldo;

    public void Retirar(decimal monto)
    {
        if (monto <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser positivo.");
        }

        if (monto > Saldo)
        {
            throw new SaldoInsuficienteException(Saldo, monto);
        }

        Saldo -= monto;
    }
}

public sealed class SaldoInsuficienteException(decimal saldo, decimal monto)
    : Exception($"Saldo insuficiente: hay {saldo:N0} y se pidieron {monto:N0}.")
{
    public decimal Saldo { get; } = saldo;
    public decimal Monto { get; } = monto;
}
```

```text
Saldo insuficiente: faltan 2.000
Saldo actual: 3.000, monto pedido: 5.000
Saldo final: 3.000
```

El filtro comprueba la relación entre los datos, no el texto del mensaje. La cuenta no cambia porque la excepción se lanza antes de modificar el saldo.

</details>

## Resumen

- Una excepción personalizada comunica un error de dominio con datos tipados.
- Hereda de `Exception`, termina en `Exception` y suele ser `sealed`.
- Los constructores deben cubrir el mensaje simple y el envoltorio con causa.
- Una base con código agrupa errores relacionados sin perder precisión.
- Las validaciones esperables se modelan con resultados, no con excepciones.

## Siguiente paso

Cuando una excepción llega al borde de la aplicación, hace falta dejar rastro de lo ocurrido. En la próxima lección vas a usar [logging estructurado con ILogger](/modulos/async-errores/logging) para registrar esos errores con contexto.
