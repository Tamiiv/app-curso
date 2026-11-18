---
slug: patrones-comportamiento
title: "Patrones de comportamiento"
order: 8
estimatedMinutes: 18
summary: "Coordiná objetos con Strategy, Observer y Command: reemplazá condicionales por polimorfismo y desacoplá quien dispara una acción de quien la ejecuta."
quiz:
  - question: "¿Qué problema resuelve Strategy?"
    options:
      - "Elegir un algoritmo en tiempo de ejecución sin un switch gigante"
      - "Crear objetos complejos"
      - "Cachear respuestas"
      - "Serializar datos"
    answer: 0
    explanation: "Strategy encapsula cada algoritmo detrás de una interfaz y el cliente elige la implementación."
  - question: "¿Cómo funciona Observer?"
    options:
      - "Un sujeto notifica a suscriptores cuando cambia su estado"
      - "Un objeto envuelve a otro para agregar comportamiento"
      - "Un cliente consulta la base cada segundo"
      - "Se crean familias de objetos"
    answer: 0
    explanation: "Observer desacopla el emisor del conjunto de interesados, que se registran y reciben notificaciones."
  - question: "¿Qué ventaja aporta Command?"
    options:
      - "Encapsula una acción como objeto, permitiendo encolarla, deshacerla o registrarla"
      - "Reduce el tamaño del JSON"
      - "Reemplaza la base de datos"
      - "Evita el uso de interfaces"
    answer: 0
    explanation: "Al representar la acción como objeto se puede almacenar, ejecutar después o revertir."
  - question: "¿Qué riesgo tiene usar Observer sin control?"
    options:
      - "Notificaciones en cascada, fugas de memoria por suscripciones y orden impredecible"
      - "Que el código no compile"
      - "Que el sujeto no pueda tener estado"
      - "Que no se puedan usar interfaces"
    answer: 0
    explanation: "Sin desuscripción y sin límites, los observadores generan ciclos, efectos inesperados y retención de objetos."
---

# Patrones de comportamiento

## Objetivo

Aplicar Strategy, Observer y Command para reemplazar condicionales por polimorfismo, desacoplar notificaciones y representar acciones como objetos.

## Requisitos previos

Haber leído [Patrones estructurales](/modulos/codigo-limpio/patrones-estructurales). Necesitás manejar interfaces, delegados y eventos.

## Concepto

Los patrones de comportamiento tratan sobre **cómo se comunican y deciden los objetos**.

| Patrón | Problema | Señal de uso |
|---|---|---|
| Strategy | Varios algoritmos intercambiables | `switch` por tipo de cálculo |
| Observer | Avisar a varios interesados | Un cambio debe notificar a otros módulos |
| Command | Ejecutar, encolar o deshacer acciones | Colas de trabajo, historial, operaciones repetibles |
| Template Method | Esqueleto fijo con pasos variables | Algoritmo con pasos que cambian |

**Strategy** encapsula un algoritmo detrás de una interfaz. En C# muchas veces se resuelve con delegados: un `Func<Pedido, decimal>` es una estrategia sin crear clases. Las clases tienen sentido cuando el algoritmo tiene estado o depende de servicios.

**Observer** desacopla el emisor del receptor. El sujeto mantiene una lista de suscriptores y los notifica. En .NET, `event` y `IObserver<T>` ya implementan la idea. El riesgo es olvidar desuscribirse: un suscriptor vivo mantiene viva la referencia y puede generar fugas.

**Command** convierte una operación en un objeto con `Ejecutar` y, opcionalmente, `Deshacer`. Es la base de colas, historial y operaciones transaccionales. En una API, un comando puede representar la intención del usuario y procesarse más tarde.

No todos los `switch` son malos. Si hay dos casos estables, un `switch` es más directo. Strategy empieza a pagar cuando el algoritmo crece, se testea por separado o se elige por configuración.

## Ejemplo mínimo

Strategy con delegados:

```csharp
public sealed record Pedido(decimal Subtotal, bool ClienteVip);

public static class Descuentos
{
    public static decimal SinDescuento(Pedido pedido) => 0;

    public static decimal Vip(Pedido pedido) =>
        pedido.ClienteVip ? pedido.Subtotal * 0.10m : 0;

    public static decimal Cupon(decimal porcentaje) =>
        pedido => pedido.Subtotal * (porcentaje / 100m);
}

var estrategia = Descuentos.Vip;
var pedido = new Pedido(1000, ClienteVip: true);

Console.WriteLine(estrategia(pedido));
```

```text
100.0
```

La estrategia es una función. Cambiar el cálculo no obliga a tocar al cliente que la invoca.

## Ejemplo aplicado

Command y Observer juntos en un flujo de pedidos:

```csharp
public sealed record Pedido(Guid Id, decimal Total);

public interface IComandoPedido
{
    void Ejecutar();
}

public sealed class ConfirmarPedido(Pedido pedido, IObservadorPedido observador) : IComandoPedido
{
    public void Ejecutar()
    {
        Console.WriteLine($"Pedido {pedido.Id} confirmado por {pedido.Total:F2}");
        observador.Notificar(pedido);
    }
}

public interface IObservadorPedido
{
    void Notificar(Pedido pedido);
}

public sealed class EmailAlCliente : IObservadorPedido
{
    public void Notificar(Pedido pedido) =>
        Console.WriteLine($"Email: tu pedido {pedido.Id} fue confirmado.");
}

public sealed class StockService : IObservadorPedido
{
    public void Notificar(Pedido pedido) =>
        Console.WriteLine($"Stock reservado para el pedido {pedido.Id}.");
}

var pedido = new Pedido(Guid.NewGuid(), 2500);
var observadores = new IObservadorPedido[] { new EmailAlCliente(), new StockService() };

foreach (var observador in observadores)
{
    new ConfirmarPedido(pedido, observador).Ejecutar();
}
```

```text
Pedido 6f... confirmado por 2500.00
Email: tu pedido 6f... fue confirmado.
Pedido 6f... confirmado por 2500.00
Stock reservado para el pedido 6f...
```

El ejemplo ejecuta un comando por observador para mantenerlo corto. En un caso real, el comando notifica a todos los observadores y estos se registran una sola vez. Lo importante es que `ConfirmarPedido` no conoce ni el email ni el stock.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Strategy para dos casos que nunca cambian | Complejidad sin variación real | Mantener el `if` simple |
| Observadores que nunca se desuscriben | Referencias retenidas | Desuscribir al finalizar el ciclo de vida |
| Notificaciones en cascada | Un observador dispara otro cambio que notifica de nuevo | Definir límites y evitar ciclos |
| Comando que hace todo | Se convirtió en un servicio gigante | Un comando, una intención |
| El orden de notificación importa y no está definido | Se asumió un orden arbitrario | Documentar el orden o desacoplar con cola |
| Eventos que ocultan el flujo | Suscripciones dispersas | Centralizar el registro y documentar quién escucha |

## Buenas prácticas

- Empezá con delegados para Strategy; pasá a clases cuando haya estado.
- Desuscribí observadores y usá `IDisposable` cuando corresponda.
- Un comando representa una intención del negocio, no una llamada técnica.
- Documentá el orden y los efectos de las notificaciones.
- Evitá que un observador dependa de otro observador.

## Ejercicio

Implementá tres estrategias de cálculo de envío (retiro en tienda, envío estándar y express) y un observador que registre en consola cada envío calculado. El cliente elige la estrategia según el código del método de envío.

<details>
<summary>Ver solución</summary>

```csharp
public sealed record Envio(string Metodo, decimal PesoKg, decimal DistanciaKm);

public interface ICalculoEnvio
{
    string Metodo { get; }
    decimal Calcular(Envio envio);
}

public sealed class RetiroEnTienda : ICalculoEnvio
{
    public string Metodo => "retiro";
    public decimal Calcular(Envio envio) => 0;
}

public sealed class EnvioEstandar : ICalculoEnvio
{
    public string Metodo => "estandar";
    public decimal Calcular(Envio envio) => 500 + envio.PesoKg * 100;
}

public sealed class EnvioExpress : ICalculoEnvio
{
    public string Metodo => "express";
    public decimal Calcular(Envio envio) => 1500 + envio.PesoKg * 250 + envio.DistanciaKm * 10;
}

public sealed class RegistroEnvios : IRegistroEnvio
{
    public void Registrar(decimal costo) => Console.WriteLine($"Costo calculado: {costo:F2}");
}

public interface IRegistroEnvio
{
    void Registrar(decimal costo);
}

var envio = new Envio("express", 2, 15);
var calculos = new ICalculoEnvio[] { new RetiroEnTienda(), new EnvioEstandar(), new EnvioExpress() };
var calculo = calculos.Single(c => c.Metodo == envio.Metodo);

var costo = calculo.Calcular(envio);
Console.WriteLine($"Método: {calculo.Metodo}");
Console.WriteLine($"Total: {costo:F2}");

new RegistroEnvios().Registrar(costo);
```

```text
Método: express
Total: 2150.00
Costo calculado: 2150.00
```

La estrategia se elige por el método del envío. El registro recibe el costo para auditarlo sin que el cálculo lo conozca.

</details>

## Resumen

- Strategy encapsula algoritmos intercambiables y elimina condicionales repetidos.
- Observer desacopla emisor y receptores, con cuidado en la desuscripción.
- Command convierte una acción en objeto para encolar, registrar o deshacer.
- El patrón se justifica por variación real y por separación de responsabilidades.

## Siguiente paso

Cerramos el módulo con un [refactoring integrador](/modulos/codigo-limpio/refactoring-ejercicio-integrador): tomar código con problemas reales y mejorarlo con los principios y patrones vistos.
