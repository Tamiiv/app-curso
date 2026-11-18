---
slug: unitarios-vs-integracion
title: "Unitarios vs. integración"
order: 3
estimatedMinutes: 15
summary: "Entendé qué prueba cada tipo de test, cuánto cuesta cada uno y cómo combinarlos en una estrategia que detecte fallas sin volverse lenta."
quiz:
  - question: "¿Qué característica define mejor a un test unitario?"
    options:
      - "Usa una base de datos real"
      - "Prueba una unidad de lógica en aislamiento y de forma rápida"
      - "Levanta el servidor web completo"
      - "Solo puede probar controladores"
    answer: 1
    explanation: "El test unitario aísla la unidad bajo prueba, evita infraestructura externa y busca ser rápido y determinista."
  - question: "¿Cuándo conviene escribir un test de integración?"
    options:
      - "Para verificar que EF Core y la base de datos funcionan juntos"
      - "Para probar una suma de dos números"
      - "Para reemplazar todos los tests unitarios"
      - "Solo cuando el proyecto es grande"
    answer: 0
    explanation: "Los tests de integración cubren la colaboración entre componentes reales, como el mapeo de EF Core contra un motor de base de datos."
  - question: "¿Qué problema tiene usar siempre el proveedor InMemory de EF Core en lugar de SQLite?"
    options:
      - "Es más lento que SQLite"
      - "No aplica restricciones relacionales y puede ocultar errores reales"
      - "No permite guardar entidades"
      - "Requiere una licencia adicional"
    answer: 1
    explanation: "El proveedor InMemory no se comporta como una base relacional: no valida claves foráneas ni varias restricciones, así que algunos errores solo aparecen en producción."
  - question: "¿Qué forma tiene una estrategia equilibrada de pruebas?"
    options:
      - "Solo tests de integración de punta a punta"
      - "Solo tests unitarios"
      - "Muchos unitarios rápidos, algunos de integración y pocos de punta a punta"
      - "Un test por cada línea de código"
    answer: 2
    explanation: "La pirámide de pruebas propone una base amplia de unitarios, una capa media de integración y unos pocos recorridos completos."
---

# Unitarios vs. integración

## Objetivo

Distinguir qué prueba un test unitario y qué prueba uno de integración, reconocer el costo y el valor de cada uno, y combinar ambos en una suite que detecte errores reales sin tardar minutos en cada ejecución.

## Requisitos previos

Necesitás escribir tests con xUnit y ordenarlos con Arrange, Act, Assert. Si querés repasar el formato, mirá [Arrange, Act, Assert](/modulos/testing/arrange-act-assert). También ayuda conocer EF Core y `DbContext`, porque va a ser el ejemplo de integración.

## Concepto

Un test unitario prueba una unidad de comportamiento en aislamiento. Esa unidad puede ser una clase, un método o un pequeño grupo de colaboradores. Lo importante es que no toca infraestructura externa: no abre una base de datos, no llama a una API, no lee archivos y no depende del reloj real. Por eso corre en milisegundos y falla de forma determinista.

Un test de integración prueba la colaboración entre piezas reales. Puede verificar que un repositorio guarda y recupera datos con EF Core, que un endpoint responde con el código correcto o que la configuración de serialización produce el JSON esperado. Es más lento y más frágil, pero detecta errores que el unitario no puede ver: mapeos, SQL, rutas, middlewares y contratos.

| Aspecto | Unitario | Integración |
|---|---|---|
| Alcance | Una unidad aislada | Varios componentes juntos |
| Velocidad | Milisegundos | Decenas o cientos de milisegundos |
| Infraestructura | Ninguna o dobles | Base de datos, servidor, archivos |
| Diagnóstico | Muy preciso | Menos preciso, más cercano al usuario |
| Fragilidad | Baja | Media o alta |
| Cantidad recomendada | Muchos | Algunos |

La pirámide de pruebas resume la estrategia: una base ancha de unitarios rápidos, una capa media de integración y muy pocos tests de punta a punta. La forma no es una ley, pero sí una buena señal: si tu suite tarda veinte minutos y casi todo son pruebas de API, vas a dejar de ejecutarla antes de cada cambio.

No todo lo que parece unitario lo es. Un test que crea un `DbContext` con el proveedor InMemory de EF Core sigue siendo una prueba de integración, aunque sea rápida: está verificando el mapeo y las consultas, no solo la lógica. Llamarlo unitario no cambia su costo ni su valor.

Elegir el tipo de test es elegir qué riesgo querés cubrir. La lógica de negocio con muchas ramas se prueba mejor con unitarios. La traducción de esa lógica a SQL, la autenticación o el ruteo se prueban con integración. Duplicar todo en ambos niveles suele ser desperdicio.

## Ejemplo mínimo

Una regla de negocio pura: un pedido supera los 100 000 pesos y recibe envío gratis. No hace falta base de datos ni servidor.

```csharp
public sealed record Pedido(decimal Subtotal);

public static class Envio
{
    public const decimal UmbralEnvioGratis = 100_000m;

    public static decimal Costo(Pedido pedido) =>
        pedido.Subtotal >= UmbralEnvioGratis ? 0m : 8_500m;
}
```

```csharp
using Xunit;

public sealed class EnvioTests
{
    [Fact]
    public void Costo_PedidoSobreElUmbral_EsGratis()
    {
        var pedido = new Pedido(120_000m);

        decimal costo = Envio.Costo(pedido);

        Assert.Equal(0m, costo);
    }

    [Fact]
    public void Costo_PedidoBajoElUmbral_CobraElFijo()
    {
        var pedido = new Pedido(99_999m);

        decimal costo = Envio.Costo(pedido);

        Assert.Equal(8_500m, costo);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 12 ms
```

Este test no necesita nada externo. Si falla, el problema está en la regla de envío o en el test, y no en la base de datos.

## Ejemplo aplicado

Un repositorio de pedidos usa EF Core con SQLite. Para probarlo de verdad conviene una base SQLite en memoria: mantiene el comportamiento relacional y se descarta al cerrar la conexión.

```csharp
using Microsoft.EntityFrameworkCore;

public sealed class Pedido
{
    public int Id { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();
}

public sealed class PedidoRepositorio(AppDbContext db)
{
    public async Task<Pedido> AgregarAsync(Pedido pedido)
    {
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();
        return pedido;
    }

    public async Task<Pedido?> BuscarAsync(int id) =>
        await db.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
}
```

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class PedidoRepositorioTests
{
    [Fact]
    public async Task AgregarAsync_PedidoValido_LoRecuperaConElMismoTotal()
    {
        await using var conexion = new SqliteConnection("DataSource=:memory:");
        await conexion.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conexion)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var repositorio = new PedidoRepositorio(db);

        var creado = await repositorio.AgregarAsync(new Pedido
        {
            Cliente = "Ana Torres",
            Total = 45_000m
        });

        var recuperado = await repositorio.BuscarAsync(creado.Id);

        Assert.NotNull(recuperado);
        Assert.Equal("Ana Torres", recuperado.Cliente);
        Assert.Equal(45_000m, recuperado.Total);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 186 ms
```

La conexión se abre antes de crear el contexto y se mantiene abierta durante todo el test; si se cierra, SQLite en memoria pierde la base. `EnsureCreatedAsync` genera el esquema a partir del modelo. Este test detecta errores de mapeo, de claves y de SQL que un mock del repositorio jamás encontraría.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Un unitario tarda segundos | Toca base de datos, red o archivos | Reemplazar la infraestructura por un doble o mover el test a integración |
| El test de integración es intermitente | Comparte una base o datos entre ejecuciones | Crear una base única por test y limpiar al finalizar |
| Se usa EF InMemory y todo pasa | El proveedor no aplica restricciones relacionales | Usar SQLite en memoria o el mismo motor de producción |
| Se duplica cada test en unitario e integración | No se definió qué riesgo cubre cada nivel | Probar reglas en unitarios y colaboración en integración |
| La suite tarda demasiado en CI | Demasiados tests de punta a punta | Reservar los recorridos completos para flujos críticos |
| Un test de integración verifica detalles privados | Se intenta controlar la implementación | Verificar respuestas y estado observable |

## Buenas prácticas

- Escribí unitarios para reglas con ramas, cálculos y validaciones; son baratos y diagnostican rápido.
- Usá integración para el mapeo, las consultas, la configuración y los contratos HTTP.
- Preferí SQLite en memoria antes que el proveedor InMemory cuando uses EF Core.
- Aislá cada test de integración con su propia base, archivo o servidor.
- Medí el tiempo de la suite: si crece demasiado, revisá cuántos tests de punta a punta tenés.

## Ejercicio

Implementá un servicio `DescuentoService` que aplica 20 % a pedidos de más de 10 unidades y 0 % en caso contrario. Escribí dos tests unitarios para la regla. Después agregá un test de integración con SQLite en memoria que guarde un pedido, lo recupere y verifique que el descuento calculado sobre el total persistido es el esperado.

<details>
<summary>Ver solución</summary>

```csharp
public sealed record LineaPedido(decimal PrecioUnitario, int Cantidad);

public sealed class DescuentoService
{
    public decimal CalcularTotal(IEnumerable<LineaPedido> lineas)
    {
        decimal subtotal = lineas.Sum(l => l.PrecioUnitario * l.Cantidad);
        int unidades = lineas.Sum(l => l.Cantidad);
        return unidades > 10 ? subtotal * 0.80m : subtotal;
    }
}
```

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class DescuentoTests
{
    [Fact]
    public void CalcularTotal_MasDeDiezUnidades_AplicaVeintePorCiento()
    {
        var service = new DescuentoService();
        var lineas = new[] { new LineaPedido(10_000m, 11) };

        Assert.Equal(88_000m, service.CalcularTotal(lineas));
    }

    [Fact]
    public void CalcularTotal_DiezUnidades_NoAplicaDescuento()
    {
        var service = new DescuentoService();
        var lineas = new[] { new LineaPedido(10_000m, 10) };

        Assert.Equal(100_000m, service.CalcularTotal(lineas));
    }

    [Fact]
    public async Task PedidoPersistido_ConOnceUnidades_CalculaDescuento()
    {
        await using var conexion = new SqliteConnection("DataSource=:memory:");
        await conexion.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conexion)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var pedido = new Pedido { Cliente = "Bruno Díaz", Total = 110_000m };
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var guardado = await db.Pedidos.SingleAsync();
        var total = new DescuentoService().CalcularTotal(
            [new LineaPedido(guardado.Total / 11, 11)]);

        Assert.Equal(88_000m, total);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 203 ms
```

El tercer test usa el `AppDbContext` y el `Pedido` del ejemplo aplicado. La división `Total / 11` es intencionalmente simple para el ejercicio; en un caso real guardarías las líneas del pedido como entidades propias.

</details>

## Resumen

- El unitario aísla lógica y corre en milisegundos; la integración prueba piezas reales juntas.
- La integración detecta mapeos, SQL, rutas y configuración que el unitario no ve.
- La pirámide propone muchos unitarios, algunos tests de integración y pocos de punta a punta.
- SQLite en memoria es una mejor opción que EF InMemory para probar comportamiento relacional.
- Elegí el nivel según el riesgo que querés cubrir, no por costumbre.

## Siguiente paso

Muchos unitarios necesitan reemplazar dependencias lentas o impredecibles. En la próxima lección vas a conocer los [mocks, stubs y fakes](/modulos/testing/dobles-de-prueba) y cuándo usar cada uno.
