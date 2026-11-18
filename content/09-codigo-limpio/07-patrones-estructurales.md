---
slug: patrones-estructurales
title: "Patrones estructurales"
order: 7
estimatedMinutes: 15
summary: "Componé objetos con Adapter, Decorator y Facade para integrar código ajeno, agregar comportamiento sin heredar y simplificar interfaces complejas."
quiz:
  - question: "¿Qué problema resuelve el patrón Adapter?"
    options:
      - "Permitir que dos interfaces incompatibles trabajen juntas"
      - "Crear familias de objetos"
      - "Reducir el acoplamiento entre capas"
      - "Serializar objetos"
    answer: 0
    explanation: "Adapter envuelve una interfaz existente y expone la interfaz que el cliente espera."
  - question: "¿Qué diferencia hay entre Decorator y herencia?"
    options:
      - "Decorator agrega comportamiento en tiempo de ejecución y se puede combinar"
      - "La herencia es siempre mejor"
      - "Decorator no puede implementar interfaces"
      - "No hay diferencia"
    answer: 0
    explanation: "Decorator envuelve un objeto y agrega responsabilidades sin crear una subclase por cada combinación."
  - question: "¿Para qué sirve una Facade?"
    options:
      - "Para ocultar un subsistema complejo detrás de una interfaz simple"
      - "Para aumentar la cantidad de clases públicas"
      - "Para reemplazar los tests"
      - "Para crear un singleton"
    answer: 0
    explanation: "Facade ofrece una puerta de entrada simple y deja los detalles del subsistema encapsulados."
  - question: "¿Cuándo un Decorator se vuelve un problema?"
    options:
      - "Cuando se combinan muchas capas y el orden importa pero no está documentado"
      - "Cuando implementa una interfaz"
      - "Cuando agrega logging"
      - "Cuando se usa con inyección de dependencias"
    answer: 0
    explanation: "El orden de los decoradores cambia el resultado. Sin documentación, el comportamiento se vuelve difícil de predecir."
---

# Patrones estructurales

## Objetivo

Componer objetos con Adapter, Decorator y Facade para integrar código de terceros, agregar comportamiento alrededor de un objeto y simplificar subsistemas complejos.

## Requisitos previos

Haber leído [Patrones creacionales](/modulos/codigo-limpio/patrones-creacionales). Necesitás manejar interfaces y composición.

## Concepto

Los patrones estructurales responden a una pregunta: **¿cómo combino objetos para que encajen sin acoplarlos?**

| Patrón | Qué hace | Ejemplo típico |
|---|---|---|
| Adapter | Traduce una interfaz a otra | Integrar un SDK de pagos |
| Decorator | Envuelve y agrega comportamiento | Cache, logs, reintentos |
| Facade | Simplifica un subsistema | Un servicio que orquesta varios pasos |
| Proxy | Controla el acceso a un objeto | Acceso remoto o perezoso |
| Composite | Trata un árbol como un nodo | Estructuras jerárquicas |

**Adapter** es el más frecuente en el trabajo real. Llega un SDK con métodos raros y no querés que todo el dominio dependa de él. Se escribe un adaptador que implementa tu interfaz y traduce las llamadas.

**Decorator** permite agregar comportamiento sin tocar la clase original ni crear una subclase por combinación. Un `NotificadorConLog` envuelve a `EmailNotificacion`, y un `NotificadorConReintento` envuelve al anterior. Cada uno cumple el mismo contrato.

**Facade** no agrega funcionalidad: organiza el acceso. Un `CheckoutService` que valida stock, cobra y crea el pedido oculta tres subsistemas detrás de un método. El cliente no necesita conocer el orden correcto.

La diferencia entre Adapter y Facade es la intención: el adaptador **traduce** una interfaz incompatible; la fachada **simplifica** un conjunto de interfaces.

## Ejemplo mínimo

Adapter alrededor de un SDK de pagos:

```csharp
// SDK externo: no lo controlamos.
public sealed class PagoSdk
{
    public string Charge(string customer, decimal amount, string currency) =>
        $"OK-{customer}-{amount:F2}-{currency}";
}

public interface IPasarelaPago
{
    ResultadoPago Cobrar(string clienteId, decimal monto);
}

public sealed record ResultadoPago(bool Exitoso, string? Referencia);

public sealed class PagoSdkAdapter(PagoSdk sdk) : IPasarelaPago
{
    public ResultadoPago Cobrar(string clienteId, decimal monto)
    {
        var referencia = sdk.Charge(clienteId, monto, "ARS");
        return new ResultadoPago(referencia.StartsWith("OK-", StringComparison.Ordinal), referencia);
    }
}
```

```csharp
var pasarela = new PagoSdkAdapter(new PagoSdk());
var resultado = pasarela.Cobrar("cliente-42", 1500.50m);
Console.WriteLine($"{resultado.Exitoso} {resultado.Referencia}");
```

```text
True OK-cliente-42-1500.50-ARS
```

El dominio solo conoce `IPasarelaPago` y `ResultadoPago`. Cambiar de proveedor es escribir otro adaptador.

## Ejemplo aplicado

Decorator para cache y logging sobre un repositorio:

```csharp
public interface IProductoRepository
{
    Producto? Obtener(int id);
}

public sealed record Producto(int Id, string Nombre);

public sealed class ProductoRepository : IProductoRepository
{
    private readonly Dictionary<int, Producto> _datos = new()
    {
        [1] = new Producto(1, "Teclado"),
        [2] = new Producto(2, "Mouse")
    };

    public Producto? Obtener(int id) => _datos.GetValueOrDefault(id);
}

public sealed class ProductoCache(IProductoRepository interno) : IProductoRepository
{
    private readonly Dictionary<int, Producto> _cache = [];

    public Producto? Obtener(int id)
    {
        if (_cache.TryGetValue(id, out var producto))
        {
            return producto;
        }

        producto = interno.Obtener(id);
        if (producto is not null)
        {
            _cache[id] = producto;
        }

        return producto;
    }
}

public sealed class ProductoLog(IProductoRepository interno, ILogger<ProductoLog> logger) : IProductoRepository
{
    public Producto? Obtener(int id)
    {
        logger.LogInformation("Buscando producto {Id}", id);
        return interno.Obtener(id);
    }
}
```

```csharp
IProductoRepository repositorio = new ProductoRepository();
repositorio = new ProductoCache(repositorio);
repositorio = new ProductoLog(repositorio, logger);

repositorio.Obtener(1);
repositorio.Obtener(1);
```

```text
[INF] Buscando producto 1
[INF] Buscando producto 1
```

Ambas llamadas pasan por el log; la segunda no consulta la fuente porque el cache responde. El orden importa: si se invierte, el log no registra las lecturas cacheadas. Esa decisión debe estar documentada y probada.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El dominio usa tipos del SDK externo | Falta el adaptador | Encapsular el SDK detrás de una interfaz propia |
| Decorador que no delega | Se olvidó llamar al interno | Llamar al envuelto y agregar solo lo necesario |
| El orden de decoradores no está claro | Se combinaron sin documentar | Definir y documentar el orden; cubrirlo con tests |
| Facade con toda la lógica del sistema | Se convirtió en una clase gigante | Mantener la fachada como orquestadora delgada |
| Cache que devuelve datos viejos | No hay política de expiración | Agregar TTL o invalidación explícita |
| Adaptador con lógica de negocio | Se mezcló traducción con reglas | El adaptador traduce; las reglas viven en el dominio |

## Buenas prácticas

- Nombrá el adaptador por el proveedor que envuelve.
- Documentá el orden de los decoradores y probá sus combinaciones.
- Mantené las fachadas delgadas: orquestan, no concentran reglas.
- Agregá TTL o invalidación a cualquier cache desde el primer día.
- Preferí composición antes que herencia para agregar comportamiento.

## Ejercicio

Implementá un adaptador para un servicio de email con una interfaz incómoda y un decorador de reintentos que lo envuelva. El reintento debe intentar hasta tres veces y registrar cada fallo.

<details>
<summary>Ver solución</summary>

```csharp
public sealed class EmailSdk
{
    private int _intentos;

    public bool Send(string to, string body)
    {
        _intentos++;
        return _intentos >= 3;
    }
}

public interface IEmailService
{
    bool Enviar(string destinatario, string cuerpo);
}

public sealed class EmailSdkAdapter(EmailSdk sdk) : IEmailService
{
    public bool Enviar(string destinatario, string cuerpo) => sdk.Send(destinatario, cuerpo);
}

public sealed class EmailConReintento(IEmailService interno, ILogger<EmailConReintento> logger) : IEmailService
{
    public bool Enviar(string destinatario, string cuerpo)
    {
        for (var intento = 1; intento <= 3; intento++)
        {
            if (interno.Enviar(destinatario, cuerpo))
            {
                return true;
            }

            logger.LogWarning("Fallo el envío a {Destinatario} en el intento {Intento}", destinatario, intento);
        }

        return false;
    }
}

IEmailService email = new EmailConReintento(new EmailSdkAdapter(new EmailSdk()), logger);
Console.WriteLine(email.Enviar("ana@ejemplo.com", "Hola"));
```

```text
[WRN] Fallo el envío a ana@ejemplo.com en el intento 1
[WRN] Fallo el envío a ana@ejemplo.com en el intento 2
True
```

En producción, el reintento incluye espera con backoff y no repite errores permanentes como un email inválido.

</details>

## Resumen

- Adapter traduce interfaces incompatibles sin contaminar el dominio.
- Decorator agrega comportamiento por composición y en tiempo de ejecución.
- Facade simplifica el acceso a un subsistema complejo.
- El orden de los decoradores es parte del contrato y debe probarse.

## Siguiente paso

Los estructurales organizan objetos. Los [patrones de comportamiento](/modulos/codigo-limpio/patrones-comportamiento) organizan la comunicación y las decisiones entre ellos.
