---
slug: dobles-de-prueba
title: "Mocks, stubs y fakes"
order: 4
estimatedMinutes: 15
summary: "Reemplazá dependencias lentas o impredecibles con dobles de prueba, elegí el adecuado y evitá tests acoplados a la implementación."
quiz:
  - question: "¿Qué es un stub?"
    options:
      - "Un doble que devuelve respuestas preparadas para que el test pueda avanzar"
      - "Un doble que verifica que se llamó a un método"
      - "Una base de datos real en memoria"
      - "Un test que se ejecuta dos veces"
    answer: 0
    explanation: "El stub aporta datos controlados. No verifica interacciones; solo responde lo que el test necesita."
  - question: "¿Qué diferencia principal tiene un mock frente a un stub?"
    options:
      - "El mock siempre es más rápido"
      - "El mock verifica interacciones, como que un método se llamó una vez"
      - "El stub no se puede usar con interfaces"
      - "El mock reemplaza a la base de datos"
    answer: 1
    explanation: "El mock se centra en las expectativas de llamadas: si se invocó, cuántas veces y con qué argumentos."
  - question: "¿Qué es un fake?"
    options:
      - "Una implementación simplificada pero funcional de una dependencia"
      - "Un test que siempre pasa"
      - "Un objeto que lanza excepciones"
      - "Un atributo de xUnit"
    answer: 0
    explanation: "Un fake implementa la interfaz con lógica real pero liviana, por ejemplo un repositorio en memoria con un diccionario."
  - question: "¿Qué señal indica que estás usando demasiados mocks?"
    options:
      - "El test corre en menos de un segundo"
      - "El test verifica cada llamada interna y se rompe ante cualquier refactor"
      - "El test usa `[Fact]`"
      - "El test tiene una assertion"
    answer: 1
    explanation: "Si el test conoce y verifica cada detalle interno, queda acoplado a la implementación y deja de proteger el comportamiento."
---

# Mocks, stubs y fakes

## Objetivo

Sustituir dependencias externas por dobles de prueba, distinguir stubs, fakes, spies y mocks, y escribir tests que verifiquen comportamiento sin quedar atados a cada llamada interna.

## Requisitos previos

Necesitás manejar interfaces, inyección de dependencias y tests unitarios. Si venís de la lección anterior, ya sabés por qué conviene aislar la lógica de la base de datos y del reloj: en [Unitarios vs. integración](/modulos/testing/unitarios-vs-integracion) quedó claro el costo de tocar infraestructura real.

## Concepto

Un doble de prueba es un objeto que reemplaza a una dependencia real durante un test. La palabra "mock" se usa coloquialmente para todos, pero conviene distinguir cinco roles:

| Tipo | Qué hace | Ejemplo |
|---|---|---|
| Dummy | Solo ocupa un parámetro; no se usa | Un `ILogger` que nunca se invoca |
| Stub | Devuelve respuestas preparadas | Un reloj fijo que siempre da la misma fecha |
| Spy | Registra las llamadas para inspeccionarlas | Un notificador que guarda los mensajes enviados |
| Mock | Verifica expectativas de llamadas | Comprobar que se envió un correo una vez |
| Fake | Implementación liviana y funcional | Un repositorio respaldado por un diccionario |

La diferencia entre spy y mock es de intención. El spy registra y después consultás el registro. El mock declara la expectativa y la verifica al final. En la práctica, un fake con una lista interna puede actuar como spy sin necesidad de una biblioteca.

Elegir el doble correcto depende de qué querés comprobar. Si tu test necesita que el reloj devuelva una fecha, usá un stub. Si necesita comprobar que se guardó un pedido, un fake de repositorio suele ser más claro que un mock. Si la interacción es el contrato, por ejemplo "se debe enviar una notificación", un mock o un spy es apropiado.

Preferí dobles escritos a mano cuando son chicos. Un `RepositorioEnMemoria` de veinte líneas se lee, se depura y no agrega dependencias. Herramientas como Moq o NSubstitute son útiles cuando la interfaz es grande o cuando necesitás configurar muchos escenarios, pero no reemplazan el criterio de diseño.

Un doble no debe replicar la complejidad de la dependencia real. Si tu fake necesita transacciones, consultas LINQ y manejo de concurrencia, probablemente estés probando integración y convenga usar la dependencia real.

## Ejemplo mínimo

Un servicio decide si una factura está vencida comparando su fecha con el relojactual. Para que el test no dependa del día en que se ejecuta, se reemplaza el reloj por un stub.

```csharp
public interface IReloj
{
    DateTimeOffset Ahora { get; }
}

public sealed class RelojSistema : IReloj
{
    public DateTimeOffset Ahora => DateTimeOffset.UtcNow;
}

public sealed class FacturaService(IReloj reloj)
{
    public bool EstaVencida(DateTimeOffset fechaEmision) =>
        fechaEmision < reloj.Ahora.AddDays(-30);
}
```

```csharp
using Xunit;

public sealed class RelojFijo(DateTimeOffset ahora) : IReloj
{
    public DateTimeOffset Ahora { get; } = ahora;
}

public sealed class FacturaServiceTests
{
    [Fact]
    public void EstaVencida_FacturaDeCuarentaDias_DevuelveTrue()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new FacturaService(reloj);

        bool vencida = service.EstaVencida(new DateTimeOffset(2026, 4, 20, 0, 0, 0, TimeSpan.Zero));

        Assert.True(vencida);
    }

    [Fact]
    public void EstaVencida_FacturaDeDiezDias_DevuelveFalse()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new FacturaService(reloj);

        bool vencida = service.EstaVencida(new DateTimeOffset(2026, 5, 22, 0, 0, 0, TimeSpan.Zero));

        Assert.False(vencida);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 14 ms
```

El stub `RelojFijo` tiene una sola responsabilidad: devolver la fecha que el test eligió. No verifica nada ni simula el paso del tiempo.

## Ejemplo aplicado

Un servicio confirma pedidos y envía un aviso por correo. El repositorio se reemplaza por un fake en memoria y el notificador por un spy que guarda los mensajes.

```csharp
public sealed record Pedido(int Id, decimal Total);

public interface IPedidoRepositorio
{
    Pedido? Buscar(int id);
    void Guardar(Pedido pedido);
}

public interface INotificador
{
    void Enviar(string destinatario, string mensaje);
}

public sealed class ConfirmacionService(IPedidoRepositorio repositorio, INotificador notificador)
{
    public bool Confirmar(int id, string email)
    {
        Pedido? pedido = repositorio.Buscar(id);
        if (pedido is null)
        {
            return false;
        }

        notificador.Enviar(email, $"Pedido {pedido.Id} confirmado por {pedido.Total:C}");
        return true;
    }
}
```

```csharp
using Xunit;

public sealed class RepositorioEnMemoria : IPedidoRepositorio
{
    private readonly Dictionary<int, Pedido> _pedidos = [];

    public Pedido? Buscar(int id) => _pedidos.GetValueOrDefault(id);

    public void Guardar(Pedido pedido) => _pedidos[pedido.Id] = pedido;
}

public sealed class NotificadorEspia : INotificador
{
    public List<(string Destinatario, string Mensaje)> Enviados { get; } = [];

    public void Enviar(string destinatario, string mensaje) =>
        Enviados.Add((destinatario, mensaje));
}

public sealed class ConfirmacionServiceTests
{
    [Fact]
    public void Confirmar_PedidoExistente_LoNotificaUnaVez()
    {
        var repositorio = new RepositorioEnMemoria();
        repositorio.Guardar(new Pedido(7, 45_000m));
        var notificador = new NotificadorEspia();
        var service = new ConfirmacionService(repositorio, notificador);

        bool confirmado = service.Confirmar(7, "ana@ejemplo.com");

        Assert.True(confirmado);
        var envio = Assert.Single(notificador.Enviados);
        Assert.Equal("ana@ejemplo.com", envio.Destinatario);
        Assert.Contains("Pedido 7", envio.Mensaje);
    }

    [Fact]
    public void Confirmar_PedidoInexistente_NoNotifica()
    {
        var repositorio = new RepositorioEnMemoria();
        var notificador = new NotificadorEspia();
        var service = new ConfirmacionService(repositorio, notificador);

        bool confirmado = service.Confirmar(99, "bruno@ejemplo.com");

        Assert.False(confirmado);
        Assert.Empty(notificador.Enviados);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 21 ms
```

El fake conserva el comportamiento esencial del repositorio: guardar y buscar por id. El spy registra los envíos para que el test pueda inspeccionarlos. No hay base de datos ni servidor de correo.

Con Moq, el mismo escenario se escribe así:

```csharp
using Moq;
using Xunit;

var repositorio = new Mock<IPedidoRepositorio>();
repositorio.Setup(r => r.Buscar(7)).Returns(new Pedido(7, 45_000m));
var notificador = new Mock<INotificador>();
var service = new ConfirmacionService(repositorio.Object, notificador.Object);

service.Confirmar(7, "ana@ejemplo.com");

notificador.Verify(n => n.Enviar("ana@ejemplo.com", It.Is<string>(m => m.Contains("Pedido 7"))), Times.Once);
```

Moq es cómodo, pero fijate que el test ahora conoce la firma exacta de `Enviar` y el texto del mensaje. Un fake escrito a mano suele ser más fácil de leer cuando el escenario es simple.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `NotSupportedException` al crear el mock | Se intenta simular una clase sin interfaz ni miembros virtuales | Depender de una interfaz o marcar el miembro como `virtual` |
| El mock devuelve `null` inesperadamente | `Mock<T>` en modo loose devuelve el valor por defecto | Configurar `Setup` o usar `MockBehavior.Strict` |
| El test se rompe ante cualquier refactor | Se verifican llamadas internas que no son el contrato | Verificar el resultado observable y solo las interacciones relevantes |
| El fake es más complejo que la dependencia real | Se intenta replicar toda la infraestructura | Simplificar el fake o escribir un test de integración |
| El mock asincrónico devuelve `null` | Falta `ReturnsAsync` o el `Setup` no coincide | Usar `Setup(...).ReturnsAsync(...)` y revisar argumentos |
| Se usa el mismo doble en toda la clase | Estado compartido entre tests | Crear el doble dentro de cada test o en el constructor de instancia |

## Buenas prácticas

- Dependé de interfaces pequeñas; son más fáciles de doblar y revelan mejor las responsabilidades.
- Usá stubs para datos y spies o mocks solo cuando la interacción sea parte del contrato.
- Preferí un fake escrito a mano si el comportamiento es simple y estable.
- No verifiques llamadas que no le importan al negocio: el test se vuelve frágil.
- Mantené los dobles dentro del proyecto de pruebas; nunca en el código de producción.

## Ejercicio

Implementá `AlertaStockService` con una dependencia `IInventario` que expone `int Disponible(string codigo)` y otra `IAlerta` con `void Enviar(string mensaje)`. El servicio debe enviar una alerta cuando el stock sea menor a 5 y no enviar nada cuando sea suficiente. Escribí un fake de inventario y un spy de alertas, y cubrí ambos casos.

<details>
<summary>Ver solución</summary>

```csharp
public interface IInventario
{
    int Disponible(string codigo);
}

public interface IAlerta
{
    void Enviar(string mensaje);
}

public sealed class AlertaStockService(IInventario inventario, IAlerta alerta)
{
    public bool Verificar(string codigo)
    {
        int disponible = inventario.Disponible(codigo);
        if (disponible >= 5)
        {
            return false;
        }

        alerta.Enviar($"Stock bajo para {codigo}: quedan {disponible} unidades.");
        return true;
    }
}
```

```csharp
using Xunit;

public sealed class InventarioFake(int cantidad) : IInventario
{
    public int Disponible(string codigo) => cantidad;
}

public sealed class AlertaSpy : IAlerta
{
    public List<string> Mensajes { get; } = [];

    public void Enviar(string mensaje) => Mensajes.Add(mensaje);
}

public sealed class AlertaStockServiceTests
{
    [Fact]
    public void Verificar_StockBajo_EnviaUnaAlerta()
    {
        var alerta = new AlertaSpy();
        var service = new AlertaStockService(new InventarioFake(2), alerta);

        bool alertado = service.Verificar("TEC-01");

        Assert.True(alertado);
        string mensaje = Assert.Single(alerta.Mensajes);
        Assert.Contains("TEC-01", mensaje);
        Assert.Contains("2", mensaje);
    }

    [Fact]
    public void Verificar_StockSuficiente_NoEnviaNada()
    {
        var alerta = new AlertaSpy();
        var service = new AlertaStockService(new InventarioFake(5), alerta);

        bool alertado = service.Verificar("TEC-01");

        Assert.False(alertado);
        Assert.Empty(alerta.Mensajes);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 16 ms
```

El valor 5 es el borde exacto: con cinco unidades ya no se alerta. Ese caso suele olvidarse y conviene tenerlo explícito.

</details>

## Resumen

- Dummy, stub, spy, mock y fake cumplen roles distintos; no todos verifican llamadas.
- El stub aporta datos, el spy registra, el mock verifica y el fake implementa comportamiento liviano.
- Los dobles escritos a mano son claros para escenarios simples.
- Moq sirve cuando la interfaz es grande, pero puede acoplar el test a detalles internos.
- Un buen test verifica comportamiento observable, no la coreografía de llamadas.

## Siguiente paso

Ya sabés aislar dependencias y probar comportamiento. En la próxima lección vas a usar esas herramientas para escribir primero el test y después el código con [TDD](/modulos/testing/tdd).
