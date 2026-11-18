---
slug: solid-srp-ocp
title: "SOLID: SRP y OCP"
order: 4
estimatedMinutes: 15
summary: "Aplicá el principio de responsabilidad única y el de abierto/cerrado con ejemplos de refactor reales y sin caer en abstracciones innecesarias."
quiz:
  - question: "¿Qué establece el principio de responsabilidad única (SRP)?"
    options:
      - "Que cada clase tenga un solo método"
      - "Que cada módulo tenga una única razón para cambiar"
      - "Que todo el código esté en una sola clase"
      - "Que ninguna clase use interfaces"
    answer: 1
    explanation: "SRP habla de razones de cambio: una clase debe responder a un solo actor o interés del negocio, no tener un único método."
  - question: "¿Qué significa abierto/cerrado (OCP)?"
    options:
      - "Abierto a modificar el código existente y cerrado a los tests"
      - "Abierto a extensión y cerrado a modificación"
      - "Abierto solo en desarrollo y cerrado en producción"
      - "Abierto a cualquier cambio sin revisión"
    answer: 1
    explanation: "Se agrega comportamiento nuevo sin editar el código ya probado, por ejemplo con nuevas implementaciones de una interfaz."
  - question: "¿Cuál es una señal de que una clase viola SRP?"
    options:
      - "Tiene más de diez líneas"
      - "Cambia por motivos de negocio, de persistencia y de presentación a la vez"
      - "Recibe dependencias por constructor"
      - "Tiene tests"
    answer: 1
    explanation: "Si actores distintos piden cambios en la misma clase, tiene más de una razón para cambiar."
  - question: "¿Qué riesgo tiene aplicar OCP con abstracciones prematuras?"
    options:
      - "Ninguno: siempre mejora el diseño"
      - "Agrega complejidad y capas antes de tener casos reales"
      - "Impide compilar"
      - "Rompe la inyección de dependencias"
    answer: 1
    explanation: "OCP se justifica cuando existe una variación real. Abstraer por anticipado suele violar KISS y YAGNI."
---

# SOLID: SRP y OCP

## Objetivo

Detectar clases con demasiadas razones para cambiar y refactorizarlas con responsabilidad única. Luego extender comportamiento sin modificar código estable usando el principio abierto/cerrado.

## Requisitos previos

Haber leído [DRY, KISS y YAGNI](/modulos/codigo-limpio/dry-kiss-yagni) y manejar interfaces e inyección de dependencias.

## Concepto

**SRP** no dice "una clase, un método". Dice que un módulo debe tener **una única razón para cambiar**, entendida como un único actor interesado en él. Una clase `Pedido` que calcula impuestos, guarda en la base y genera un PDF tiene tres actores: finanzas, infraestructura y el área que imprime.

Cuando esos actores piden cambios, todos tocan el mismo archivo. El riesgo no es estético: un cambio de formato de PDF puede romper el cálculo de impuestos.

**OCP** dice que el software debe estar **abierto a extensión y cerrado a modificación**. Agregar un nuevo tipo de descuento no debería implicar editar el `switch` central que ya funciona y está probado.

El mecanismo más común es una interfaz con varias implementaciones. Pero OCP no obliga a prever todo: se aplica cuando la variación es real y repetida.

Un `switch` no es pecado. Si hay dos casos estables, es la solución más simple. OCP empieza a pagar cuando el tercer caso aparece y el `switch` crece en cada release.

## Ejemplo mínimo

Antes, una clase con tres responsabilidades:

```csharp
public sealed class Factura
{
    public decimal Total { get; init; }

    public decimal CalcularImpuesto() => Total * 0.21m;

    public void Guardar() =>
        File.WriteAllText($"factura-{Guid.NewGuid()}.txt", Total.ToString("F2"));

    public string GenerarPdf() => $"PDF de {Total:F2}";
}
```

Después, cada responsabilidad en su lugar:

```csharp
public sealed record Factura(decimal Total);

public static class Impuestos
{
    public const decimal Iva = 0.21m;

    public static decimal Calcular(Factura factura) => factura.Total * Iva;
}

public sealed class FacturaRepository
{
    private readonly string _carpeta;

    public FacturaRepository(string carpeta) => _carpeta = carpeta;

    public void Guardar(Factura factura) =>
        File.WriteAllText(
            Path.Combine(_carpeta, $"factura-{Guid.NewGuid():N}.txt"),
            factura.Total.ToString("F2"));
}

public sealed class FacturaPdf
{
    public string Generar(Factura factura) => $"PDF de {factura.Total:F2}";
}
```

```csharp
var factura = new Factura(1210);
Console.WriteLine(Impuestos.Calcular(factura));
```

```text
254.10
```

Ahora un cambio en el formato del PDF no toca impuestos ni persistencia.

## Ejemplo aplicado

OCP con descuentos. Antes:

```csharp
public decimal CalcularDescuento(Pedido pedido)
{
    return pedido.TipoCliente switch
    {
        "regular" => 0,
        "frecuente" => pedido.Total * 0.05m,
        "vip" => pedido.Total * 0.10m,
        _ => throw new ArgumentOutOfRangeException(nameof(pedido.TipoCliente))
    };
}
```

Cada tipo nuevo obliga a editar el método y volver a probar todos los casos. Después:

```csharp
public interface IDescuento
{
    bool Aplica(Pedido pedido);
    decimal Calcular(Pedido pedido);
}

public sealed class SinDescuento : IDescuento
{
    public bool Aplica(Pedido pedido) => pedido.TipoCliente == "regular";
    public decimal Calcular(Pedido pedido) => 0;
}

public sealed class DescuentoFrecuente : IDescuento
{
    public bool Aplica(Pedido pedido) => pedido.TipoCliente == "frecuente";
    public decimal Calcular(Pedido pedido) => pedido.Total * 0.05m;
}

public sealed class DescuentoVip : IDescuento
{
    public bool Aplica(Pedido pedido) => pedido.TipoCliente == "vip";
    public decimal Calcular(Pedido pedido) => pedido.Total * 0.10m;
}

public sealed class CalculadoraDescuentos(IEnumerable<IDescuento> descuentos)
{
    public decimal Calcular(Pedido pedido) =>
        descuentos.FirstOrDefault(d => d.Aplica(pedido))?.Calcular(pedido) ?? 0;
}
```

```csharp
var calculadora = new CalculadoraDescuentos(
[
    new SinDescuento(),
    new DescuentoFrecuente(),
    new DescuentoVip()
]);

Console.WriteLine(calculadora.Calcular(new Pedido("vip", 1000)));
```

```text
100
```

Agregar `DescuentoEmpleado` es una clase nueva y un registro en el contenedor. No se toca la calculadora ni los descuentos existentes.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Una clase con "Manager" o "Helper" en el nombre | Agrupa responsabilidades no relacionadas | Separar por actor y por motivo de cambio |
| Un archivo de 800 líneas con todo el flujo | SRP ignorado | Extraer cálculo, persistencia y presentación |
| Interfaz por cada clase, aunque tenga un solo uso | OCP aplicado por anticipado | Esperar variación real; usar la clase concreta |
| `switch` que crece en cada release | OCP ausente en un punto de variación real | Polimorfismo con implementaciones registradas |
| Se modifican tests existentes en cada tipo nuevo | El diseño obliga a tocar lo estable | Extender con una clase nueva |
| Se abstrae todo y nadie entiende el flujo | Exceso de indirección | Volver a lo concreto donde no hay variación |

## Buenas prácticas

- Nombrá la responsabilidad: si no podés describir la clase en una frase sin "y", probablemente tenga dos.
- Separá por actor: quien pide el cambio define la frontera.
- Aplicá OCP cuando la variación ya existe, no cuando la imaginás.
- Preferí composición de pequeños colaboradores antes que herencia profunda.
- Mantené los puntos de extensión pequeños y con nombres del dominio.

## Ejercicio

Refactorizá una clase `Notificador` que arma el mensaje, lo envía por email y lo registra en un archivo. Separá al menos tres responsabilidades y agregá un nuevo canal (SMS) sin modificar el código de email.

<details>
<summary>Ver solución</summary>

```csharp
public sealed record Aviso(string Destinatario, string Texto);

public interface ICanalNotificacion
{
    string Nombre { get; }
    void Enviar(Aviso aviso);
}

public sealed class EmailCanal : ICanalNotificacion
{
    public string Nombre => "email";
    public void Enviar(Aviso aviso) => Console.WriteLine($"Email a {aviso.Destinatario}: {aviso.Texto}");
}

public sealed class SmsCanal : ICanalNotificacion
{
    public string Nombre => "sms";
    public void Enviar(Aviso aviso) => Console.WriteLine($"SMS a {aviso.Destinatario}: {aviso.Texto}");
}

public sealed class Notificador(IEnumerable<ICanalNotificacion> canales, ILogger<Notificador> logger)
{
    public void Notificar(Aviso aviso)
    {
        foreach (var canal in canales)
        {
            canal.Enviar(aviso);
            logger.LogInformation("Aviso enviado por {Canal}", canal.Nombre);
        }
    }
}
```

Agregar SMS no toca `EmailCanal` ni `Notificador`. El registro en el contenedor decide qué canales existen.

</details>

## Resumen

- SRP se mide en razones de cambio, no en cantidad de métodos.
- Separar por actor reduce el riesgo de romper algo al cambiar otra cosa.
- OCP permite agregar comportamiento sin editar código estable.
- La extensión se justifica con variación real, no con futuros imaginados.

## Siguiente paso

Faltan los otros tres principios SOLID. En [SOLID: LSP, ISP y DIP](/modulos/codigo-limpio/solid-lsp-isp-dip) vas a completar el conjunto con sustitución, interfaces pequeñas e inversión de dependencias.
