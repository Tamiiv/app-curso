---
slug: monolito-modular
title: "Monolito modular"
order: 4
estimatedMinutes: 15
summary: "Organizá una aplicación como un solo despliegue con módulos bien delimitados: la simplicidad operativa del monolito con las fronteras que permiten crecer."
quiz:
  - question: "¿Qué caracteriza a un monolito modular?"
    options:
      - "Un solo despliegue con módulos separados por fronteras claras"
      - "Un proyecto único sin carpetas"
      - "Un servicio por tabla"
      - "Una base de datos por módulo obligatoriamente"
    answer: 0
    explanation: "Se despliega junto, pero cada módulo tiene su lógica, su modelo y una interfaz pública acotada."
  - question: "¿Cuál es la principal ventaja frente a microservicios para un equipo chico?"
    options:
      - "Más independencia de despliegue"
      - "Menos costo operativo: un deploy, un log, una base y transacciones locales"
      - "Escala infinitamente sin esfuerzo"
      - "No necesita tests"
    answer: 1
    explanation: "Menos piezas móviles significa menos infraestructura, menos latencia de red y transacciones simples."
  - question: "¿Qué regla evita que un monolito modular se degrade?"
    options:
      - "Cada módulo puede acceder a las tablas de los demás libremente"
      - "Un módulo solo usa la API pública de otro, no sus tablas ni clases internas"
      - "Todos los módulos comparten el mismo DbContext sin restricciones"
      - "No se permiten carpetas"
    answer: 1
    explanation: "La frontera se sostiene si el acceso cruza solo por contratos explícitos. El acceso directo a tablas ajenas crea acoplamiento."
  - question: "¿Qué señal indica que un módulo debería separarse en un servicio?"
    options:
      - "Tiene más de cien líneas"
      - "Necesita escalar, desplegarse o fallar de forma independiente y el costo operativo se justifica"
      - "Tiene su propio archivo de configuración"
      - "Usa una base de datos"
    answer: 1
    explanation: "La separación se justifica por una necesidad operativa real, no por moda ni por tamaño del código."
---

# Monolito modular

## Objetivo

Diseñar una aplicación como un único despliegue con módulos autónomos, definir fronteras verificables y saber cuándo un módulo justifica convertirse en servicio.

## Requisitos previos

Haber leído [Arquitectura hexagonal](/modulos/arquitectura/hexagonal). Necesitás manejar EF Core, dependencias entre proyectos y organización de soluciones.

## Concepto

Un monolito modular es una sola aplicación desplegable, dividida internamente en módulos con responsabilidades claras. Cada módulo tiene:

- Su propio modelo de dominio y casos de uso.
- Su esquema o tablas propias.
- Una API pública mínima para otros módulos.
- Sus tests.

Los módulos no acceden a las tablas ni a las clases internas de otros. Se comunican por contratos.

```text
┌──────────────────────────────────────────────┐
│  Aplicación (un solo deploy)                 │
│  ┌──────────┐ ┌──────────┐ ┌──────────────┐  │
│  │ Ventas   │ │ Stock    │ │ Facturación  │  │
│  │ dominio  │ │ dominio  │ │ dominio      │  │
│  │ tablas   │ │ tablas   │ │ tablas       │  │
│  └────┬─────┘ └────┬─────┘ └──────┬───────┘  │
│       └── contratos internos ─────┘          │
└──────────────────────────────────────────────┘
```

| Aspecto | Monolito clásico | Monolito modular | Microservicios |
|---|---|---|---|
| Despliegue | Uno | Uno | Varios |
| Fronteras | Débiles | Fuertes en código | De red y proceso |
| Transacciones | Locales | Locales | Distribuidas |
| Costo operativo | Bajo | Bajo | Alto |
| Escala | Todo junto | Todo junto | Por servicio |

El enemigo del monolito modular es el atajo: "total, está en el mismo proceso". En cuanto un módulo lee una tabla de otro, la frontera desaparece y el sistema vuelve a ser un monolito enredado.

La forma de sostener la frontera:

- Cada módulo expone una interfaz pública y oculta el resto con `internal`.
- Las consultas entre módulos pasan por esa interfaz o por eventos internos.
- Cada módulo define su `DbContext` o al menos su esquema.
- Los tests de arquitectura verifican que no haya referencias cruzadas indebidas.

No necesitás empezar con diez módulos. Empezá con dos o tres que representen áreas del negocio distintas y crecé cuando aparezca la necesidad.

## Ejemplo mínimo

Contrato público de un módulo y su uso desde otro:

```csharp
namespace AppCurso.Modules.Stock.Contracts;

public interface IStockService
{
    Task<bool> HayDisponibleAsync(Guid productoId, int cantidad, CancellationToken cancellationToken);
    Task ReservarAsync(Guid productoId, int cantidad, CancellationToken cancellationToken);
}

public sealed record ProductoStock(Guid Id, string Nombre, int Disponible);
```

```csharp
namespace AppCurso.Modules.Ventas;

public sealed class CrearVentaHandler(IVentaRepository ventas, IStockService stock)
{
    public async Task<string?> HandleAsync(
        CrearVentaCommand command,
        CancellationToken cancellationToken)
    {
        if (!await stock.HayDisponibleAsync(command.ProductoId, command.Cantidad, cancellationToken))
        {
            return "No hay stock suficiente.";
        }

        var venta = new Venta(Guid.NewGuid(), command.ProductoId, command.Cantidad);
        await ventas.AgregarAsync(venta, cancellationToken);

        await stock.ReservarAsync(command.ProductoId, command.Cantidad, cancellationToken);
        await ventas.SaveChangesAsync(cancellationToken);

        return null;
    }
}
```

`Ventas` conoce `IStockService`, no la tabla `productos` ni la clase `StockDbContext`. La implementación vive en el módulo de stock y se registra en el contenedor.

## Ejemplo aplicado

Cada módulo registra sus servicios y endpoints:

```csharp
namespace AppCurso.Modules.Stock;

public static class StockModule
{
    public static IServiceCollection AddStockModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<StockDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IStockService, StockService>();
        return services;
    }

    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/stock").WithTags("Stock");

        grupo.MapGet("/{productoId:guid}", async (
            Guid productoId,
            StockDbContext db,
            CancellationToken cancellationToken) =>
        {
            var producto = await db.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productoId, cancellationToken);

            return producto is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(new ProductoStock(producto.Id, producto.Nombre, producto.Disponible));
        });

        return app;
    }
}
```

```csharp
// Program.cs: composición explícita.
builder.Services.AddStockModule(builder.Configuration);
builder.Services.AddVentasModule(builder.Configuration);

var app = builder.Build();

app.MapStockEndpoints();
app.MapVentasEndpoints();
```

```text
/stock/{id}        → módulo Stock
/ventas            → módulo Ventas
```

La composición queda visible en un archivo corto. Si un módulo crece demasiado, se puede extraer a un servicio sin reescribir su lógica: el contrato ya existe.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Un módulo consulta la tabla de otro | Atajo por estar en el mismo proceso | Usar el contrato público o un evento |
| Todos comparten un `DbContext` gigante | Se priorizó comodidad | Un contexto o esquema por módulo |
| Los módulos se llaman entre sí en círculo | Fronteras mal definidas | Revisar dependencias y extraer un contrato común |
| Un módulo de 50 archivos sin estructura | Se copió todo en una carpeta | Organizar por dominio dentro del módulo |
| Se separan servicios antes de tiempo | Moda | Mantener el monolito modular hasta que haya una necesidad operativa |
| Tests de arquitectura ausentes | Nada impide la violación | Verificar referencias prohibidas en CI |

## Buenas prácticas

- Definí módulos por área de negocio, no por capa técnica.
- Exponé solo contratos; marcá el resto como `internal`.
- Cada módulo tiene sus tablas y su contexto.
- Un evento interno es preferible a una llamada circular.
- Probá las fronteras con tests de arquitectura.
- Extraé un servicio solo con una razón operativa concreta.

## Ejercicio

Dividí una API de tienda en dos módulos: `Catalogo` y `Pedidos`. `Pedidos` necesita consultar disponibilidad y debe hacerlo por un contrato, no por la tabla de productos. Agregá un test que falle si `Pedidos` referencia el `DbContext` de `Catalogo`.

<details>
<summary>Ver solución</summary>

```csharp
// Catalogo/Contracts/ICatalogoService.cs
namespace AppCurso.Modules.Catalogo.Contracts;

public interface ICatalogoService
{
    Task<bool> HayDisponibleAsync(Guid productoId, int cantidad, CancellationToken cancellationToken);
}
```

```csharp
// Catalogo/CatalogoService.cs
namespace AppCurso.Modules.Catalogo;

internal sealed class CatalogoService(CatalogoDbContext db) : ICatalogoService
{
    public async Task<bool> HayDisponibleAsync(
        Guid productoId,
        int cantidad,
        CancellationToken cancellationToken)
    {
        var disponible = await db.Productos
            .Where(p => p.Id == productoId)
            .Select(p => p.Disponible)
            .FirstOrDefaultAsync(cancellationToken);

        return disponible >= cantidad;
    }
}
```

```csharp
// Pedidos/PedidosService.cs
namespace AppCurso.Modules.Pedidos;

internal sealed class PedidosService(ICatalogoService catalogo)
{
    public async Task<string?> CrearAsync(Guid productoId, int cantidad, CancellationToken cancellationToken) =>
        await catalogo.HayDisponibleAsync(productoId, cantidad, cancellationToken)
            ? null
            : "Sin stock.";
}
```

```csharp
// Tests de arquitectura
[Fact]
public void PedidosNoReferenciaElDbContextDeCatalogo()
{
    var ensamblado = typeof(AppCurso.Modules.Pedidos.PedidosService).Assembly;
    var referencias = ensamblado.GetReferencedAssemblies().Select(a => a.Name);

    Assert.DoesNotContain("AppCurso.Modules.Catalogo.Persistence", referencias);
}
```

La frontera queda protegida por el compilador y por un test que falla si alguien la cruza.

</details>

## Resumen

- El monolito modular despliega junto pero separa responsabilidades por módulo.
- La frontera se sostiene con contratos, no con disciplina informal.
- Menos costo operativo que microservicios, con posibilidad de extraer después.
- Cada módulo tiene su modelo y sus tablas.
- Los tests de arquitectura evitan que la separación se degrade.

## Siguiente paso

Cuando la escala o el equipo lo exigen, el monolito se rompe en servicios. En [Microservicios](/modulos/arquitectura/microservicios) vas a ver qué se gana, qué se pierde y cuándo no conviene.
