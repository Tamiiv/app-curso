---
slug: arquitectura-limpia
title: "Arquitectura limpia"
order: 2
estimatedMinutes: 18
summary: "Aplicá la regla de dependencia: el dominio y los casos de uso no conocen frameworks, base de datos ni HTTP, y todo lo externo se conecta por contratos."
quiz:
  - question: "¿Qué establece la regla de dependencia de la arquitectura limpia?"
    options:
      - "Las dependencias del código fuente apuntan siempre hacia las capas internas"
      - "El dominio depende de la base de datos para persistir"
      - "Cada capa puede importar cualquier otra si está en el mismo repositorio"
      - "La infraestructura define las reglas de negocio"
    answer: 0
    explanation: "El código de las capas externas puede conocer a las internas, pero nunca al revés. El dominio no importa EF Core ni ASP.NET Core."
  - question: "¿Dónde vive un caso de uso como `RegistrarPedido`?"
    options:
      - "En el controlador o endpoint"
      - "En la capa de aplicación, coordinando dominio e interfaces"
      - "En el DbContext"
      - "En el archivo de configuración"
    answer: 1
    explanation: "Los casos de uso orquestan: cargan entidades, aplican reglas y llaman a contratos. El endpoint solo traduce HTTP."
  - question: "¿Qué gana el dominio al no depender de EF Core?"
    options:
      - "Se puede probar sin base de datos y sobrevive a un cambio de persistencia"
      - "Compila más rápido siempre"
      - "No necesita entidades"
      - "Evita usar interfaces"
    answer: 0
    explanation: "Al aislar el dominio, los tests son unitarios y rápidos, y cambiar de ORM o base no toca las reglas."
  - question: "¿Qué es un DTO en esta arquitectura?"
    options:
      - "Una entidad de dominio con métodos"
      - "Un objeto para transportar datos entre capas o hacia el cliente"
      - "Un repositorio en memoria"
      - "Un tipo de migración"
    answer: 1
    explanation: "Los DTO evitan exponer entidades y acoplan la forma del contrato a lo que la capa necesita, no al modelo interno."
---

# Arquitectura limpia

## Objetivo

Organizar un proyecto con la regla de dependencia de la arquitectura limpia, ubicar casos de uso, entidades y adaptadores en su lugar, y reconocer cuándo esta estructura paga su costo.

## Requisitos previos

Haber leído [Arquitectura en capas](/modulos/arquitectura/capas). Necesitás manejar interfaces, inyección de dependencias y EF Core a nivel básico.

## Concepto

La arquitectura limpia (Clean Architecture) lleva la idea de capas a una regla simple y estricta: **las dependencias del código fuente apuntan hacia adentro**. El centro es el dominio; hacia afuera están los casos de uso, los adaptadores y los frameworks.

```text
        ┌─────────────────────────────────┐
        │  Frameworks y drivers           │  ASP.NET Core, EF Core, Postgres
        │  ┌───────────────────────────┐  │
        │  │ Adaptadores               │  │  Controladores, repositorios
        │  │ ┌───────────────────────┐ │  │
        │  │ │ Casos de uso          │ │  │  RegistrarPedido, Login
        │  │ │ ┌───────────────────┐ │ │  │
        │  │ │ │ Dominio           │ │ │  │  Entidades, reglas, contratos
        │  │ │ └───────────────────┘ │ │  │
        │  │ └───────────────────────┘ │  │
        │  └───────────────────────────┘  │
        └─────────────────────────────────┘
```

La consecuencia práctica: el proyecto `Domain` no referencia a nadie. El proyecto `Application` referencia a `Domain`. `Infrastructure` referencia a `Application` y `Domain`. `Web` referencia a `Application` y, para el registro de dependencias, a `Infrastructure`.

| Proyecto | Contiene | Referencia a |
|---|---|---|
| `Domain` | Entidades, value objects, reglas, interfaces de repositorio | Nada |
| `Application` | Casos de uso, DTOs, validaciones de aplicación | `Domain` |
| `Infrastructure` | EF Core, repositorios, clientes HTTP, email | `Application`, `Domain` |
| `Web` | Endpoints, middleware, composición | `Application`, `Infrastructure` |

El dominio no sabe que existe una base de datos. Define `IPedidoRepository` y la infraestructura lo implementa. Eso es la inversión de dependencias aplicada a la arquitectura.

Esta estructura tiene costos: más proyectos, más mapeos y más archivos para un cambio simple. Se justifica cuando hay reglas de negocio que duran y varios canales (API, jobs, consola) sobre el mismo núcleo. Para un CRUD de una tabla, probablemente sea demasiado.

## Ejemplo mínimo

Dominio sin dependencias externas:

```csharp
namespace AppCurso.Domain.Pedidos;

public sealed class Pedido
{
    private readonly List<LineaPedido> _lineas = [];

    private Pedido() { }

    public Pedido(Guid id, string clienteId)
    {
        if (string.IsNullOrWhiteSpace(clienteId))
        {
            throw new ArgumentException("El cliente es obligatorio.", nameof(clienteId));
        }

        Id = id;
        ClienteId = clienteId;
        Estado = EstadoPedido.Borrador;
    }

    public Guid Id { get; private set; }
    public string ClienteId { get; private set; } = string.Empty;
    public EstadoPedido Estado { get; private set; }
    public IReadOnlyList<LineaPedido> Lineas => _lineas;

    public decimal Total => _lineas.Sum(l => l.Subtotal);

    public void AgregarLinea(string producto, decimal precio, int cantidad)
    {
        if (Estado != EstadoPedido.Borrador)
        {
            throw new InvalidOperationException("Solo se modifican pedidos en borrador.");
        }

        _lineas.Add(new LineaPedido(producto, precio, cantidad));
    }

    public void Confirmar()
    {
        if (_lineas.Count == 0)
        {
            throw new InvalidOperationException("Un pedido vacío no se confirma.");
        }

        Estado = EstadoPedido.Confirmado;
    }
}

public enum EstadoPedido
{
    Borrador,
    Confirmado,
    Cancelado
}

public sealed record LineaPedido(string Producto, decimal Precio, int Cantidad)
{
    public decimal Subtotal => Precio * Cantidad;
}

public interface IPedidoRepository
{
    Task<Pedido?> ObtenerAsync(Guid id, CancellationToken cancellationToken);
    Task AgregarAsync(Pedido pedido, CancellationToken cancellationToken);
}
```

La entidad protege sus invariantes: no se confirma un pedido vacío, no se modifica uno confirmado. No hay atributos de EF, ni `DbContext`, ni JSON.

## Ejemplo aplicado

Caso de uso en aplicación + adaptador en infraestructura:

```csharp
namespace AppCurso.Application.Pedidos;

public sealed record ConfirmarPedidoCommand(Guid PedidoId);

public sealed record ConfirmarPedidoResult(bool Exitoso, string? Error);

public sealed class ConfirmarPedidoHandler(IPedidoRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<ConfirmarPedidoResult> HandleAsync(
        ConfirmarPedidoCommand command,
        CancellationToken cancellationToken)
    {
        var pedido = await repository.ObtenerAsync(command.PedidoId, cancellationToken);

        if (pedido is null)
        {
            return new ConfirmarPedidoResult(false, "Pedido no encontrado.");
        }

        try
        {
            pedido.Confirmar();
        }
        catch (InvalidOperationException ex)
        {
            return new ConfirmarPedidoResult(false, ex.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ConfirmarPedidoResult(true, null);
    }
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
```

```csharp
namespace AppCurso.Infrastructure.Persistence;

public sealed class PedidoRepository(AppDbContext db) : IPedidoRepository
{
    public Task<Pedido?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.Pedidos
            .Include(p => p.Lineas)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AgregarAsync(Pedido pedido, CancellationToken cancellationToken) =>
        await db.Pedidos.AddAsync(pedido, cancellationToken);
}

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
```

```csharp
app.MapPost("/pedidos/{id:guid}/confirmar", async (
    Guid id,
    ConfirmarPedidoHandler handler,
    CancellationToken cancellationToken) =>
{
    var resultado = await handler.HandleAsync(new ConfirmarPedidoCommand(id), cancellationToken);

    return resultado.Exitoso
        ? TypedResults.NoContent()
        : TypedResults.BadRequest(new ProblemDetails { Title = resultado.Error });
});
```

El endpoint no conoce el dominio ni EF Core: llama a un caso de uso y traduce el resultado a HTTP. Cambiar el endpoint por un job o una consola reutiliza el mismo handler.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El dominio referencia EF Core | Se pusieron atributos o `DbContext` en entidades | Configurar el mapeo desde infraestructura con `IEntityTypeConfiguration` |
| La entidad tiene setters públicos | Se priorizó comodidad | Propiedades privadas y métodos que protegen invariantes |
| El caso de uso devuelve `IResult` | Se mezcló HTTP con aplicación | Devolver un resultado propio y traducirlo en el borde |
| Hay cinco proyectos para un CRUD | Se aplicó la estructura sin necesidad | Usar capas simples o un solo proyecto |
| El repositorio expone `IQueryable` | Se filtró la persistencia hacia arriba | Devolver entidades o DTOs con métodos concretos |
| Todo es una interfaz con una implementación | Exceso de abstracción | Abstraer solo lo que se cruza de capa o se reemplaza en tests |

## Buenas prácticas

- El dominio no tiene dependencias de paquetes externos.
- Los casos de uso reciben y devuelven tipos propios, no HTTP.
- La infraestructura implementa contratos; nunca al revés.
- Mapeá entidades a DTOs en el borde para no exponer el modelo interno.
- Medí el costo: la estructura debe resolver un problema real del proyecto.
- Probá el dominio sin base de datos ni servidor.

## Ejercicio

Tomá una API con la lógica dentro de los endpoints y separá: entidad de dominio con una regla, caso de uso que la ejecuta e interfaz de repositorio implementada en infraestructura. Verificá que el dominio compile sin referencias a ASP.NET Core ni a EF Core.

<details>
<summary>Ver solución</summary>

```csharp
// Domain: sin dependencias externas.
public sealed class Cuenta
{
    public Cuenta(Guid id, decimal saldoInicial)
    {
        if (saldoInicial < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(saldoInicial));
        }

        Id = id;
        Saldo = saldoInicial;
    }

    public Guid Id { get; }
    public decimal Saldo { get; private set; }

    public void Debitar(decimal monto)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(monto);

        if (monto > Saldo)
        {
            throw new InvalidOperationException("Saldo insuficiente.");
        }

        Saldo -= monto;
    }
}

public interface ICuentaRepository
{
    Task<Cuenta?> ObtenerAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
```

```csharp
// Application: orquesta el caso de uso.
public sealed record DebitarCommand(Guid CuentaId, decimal Monto);

public sealed class DebitarHandler(ICuentaRepository repository)
{
    public async Task<string?> HandleAsync(DebitarCommand command, CancellationToken cancellationToken)
    {
        var cuenta = await repository.ObtenerAsync(command.CuentaId, cancellationToken);
        if (cuenta is null)
        {
            return "Cuenta no encontrada.";
        }

        try
        {
            cuenta.Debitar(command.Monto);
            await repository.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
    }
}
```

```csharp
// Infrastructure: implementa el contrato.
public sealed class CuentaRepository(AppDbContext db) : ICuentaRepository
{
    public Task<Cuenta?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        db.Cuentas.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
```

El proyecto `Domain` no tiene `PackageReference` a ASP.NET Core ni a EF Core. Los tests del débito se escriben sin base de datos.

</details>

## Resumen

- La regla de dependencia hace que el código fuente apunte hacia el dominio.
- El dominio protege invariantes y no conoce frameworks.
- Los casos de uso coordinan dominio y contratos.
- La infraestructura implementa interfaces definidas hacia adentro.
- La estructura se justifica por la complejidad del negocio y la vida del sistema.

## Siguiente paso

La arquitectura limpia organiza por capas concéntricas. La [arquitectura hexagonal](/modulos/arquitectura/hexagonal) propone otra forma de mirar lo mismo: el núcleo en el centro y puertos y adaptadores alrededor.
