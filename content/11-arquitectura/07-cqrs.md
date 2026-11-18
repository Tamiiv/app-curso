---
slug: cqrs
title: "CQRS"
order: 7
estimatedMinutes: 15
summary: "Separá comandos de consultas cuando los modelos de escritura y lectura tienen necesidades distintas, sin duplicar el sistema por moda."
quiz:
  - question: "¿Qué propone CQRS?"
    options:
      - "Separar las operaciones que cambian estado de las que solo leen"
      - "Eliminar las consultas a la base de datos"
      - "Usar una sola clase para todo"
      - "Compartir el modelo de escritura con el cliente"
    answer: 0
    explanation: "CQRS separa comandos (escritura) y queries (lectura), con modelos y a veces almacenes distintos."
  - question: "¿Qué es CQRS ligero?"
    options:
      - "Separar los handlers y los modelos en código, sobre la misma base"
      - "Duplicar la base de datos desde el primer día"
      - "Usar solo procedimientos almacenados"
      - "Evitar las interfaces"
    answer: 0
    explanation: "La forma más común separa objetos y handlers en la aplicación sin dos bases de datos ni proyecciones asíncronas."
  - question: "¿Qué costo introduce CQRS con proyecciones separadas?"
    options:
      - "Consistencia eventual y mayor complejidad de sincronización"
      - "Menos código siempre"
      - "Consultas más simples sin riesgo"
      - "Desaparecen los tests"
    answer: 0
    explanation: "Mantener dos modelos requiere proyectar cambios y manejar el retraso entre escritura y lectura."
  - question: "¿Cuándo NO conviene CQRS completo?"
    options:
      - "Cuando lecturas y escrituras tienen requisitos muy distintos"
      - "Cuando el sistema es un CRUD simple con un solo modelo"
      - "Cuando hay alto volumen de lectura y escritura"
      - "Cuando se necesita escalar lecturas por separado"
    answer: 1
    explanation: "Si la misma forma de dato sirve para leer y escribir, la duplicación agrega costo sin beneficio."
---

# CQRS

## Objetivo

Separar comandos y consultas con criterio, diseñar handlers simples y reconocer cuándo el CQRS completo con proyecciones justifica su costo.

## Requisitos previos

Haber leído [Comunicación entre servicios](/modulos/arquitectura/comunicacion-servicios). Necesitás manejar casos de uso, EF Core y el concepto de evento.

## Concepto

CQRS (Command Query Responsibility Segregation) separa dos responsabilidades que suelen mezclarse:

- **Comando:** cambia estado. No devuelve datos de negocio (a lo sumo un id o el resultado de la operación).
- **Consulta:** lee estado. No modifica nada.

En su forma **ligera**, la separación es de código: clases distintas, modelos distintos, handlers distintos, sobre la misma base de datos. Es la variante más común y la que menos cuesta.

En su forma **completa**, los lados de escritura y lectura usan almacenes distintos. El lado de escritura mantiene el modelo transaccional; el de lectura, proyecciones desnormalizadas optimizadas para consultas. Esa separación permite escalar lecturas y escribir consultas rápidas, pero introduce consistencia eventual y sincronización.

| Nivel | Qué separa | Costo |
|---|---|---|
| Sin CQRS | Un modelo para todo | Bajo, pero mezcla intenciones |
| CQRS ligero | Handlers y modelos en código | Bajo |
| CQRS con proyecciones | Almacenes de lectura y escritura | Alto |

No todo necesita CQRS. Un CRUD donde la entidad se lee igual que se escribe gana poco con la separación. El patrón empieza a pagar cuando:

- Las consultas necesitan formas muy distintas de las tablas normalizadas.
- Hay mucho más volumen de lectura que de escritura, o viceversa.
- El modelo de escritura protege invariantes complejas y el de lectura prioriza velocidad.
- Distintos equipos trabajan en lecturas y escrituras.

Un error frecuente es creer que CQRS exige eventos, colas o dos bases de datos. No es así: se puede empezar con handlers separados y evolucionar si hace falta.

## Ejemplo mínimo

CQRS ligero con comandos y queries separados:

```csharp
namespace AppCurso.Application.Pedidos;

public sealed record CrearPedidoCommand(Guid ProductoId, int Cantidad);

public sealed record PedidoResumen(Guid Id, string Estado, decimal Total);

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult?> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

public sealed class CrearPedidoHandler(IPedidoRepository pedidos) 
    : ICommandHandler<CrearPedidoCommand, Guid>
{
    public async Task<Guid> HandleAsync(CrearPedidoCommand command, CancellationToken cancellationToken)
    {
        var pedido = Pedido.Crear(command.ProductoId, command.Cantidad);
        await pedidos.AgregarAsync(pedido, cancellationToken);
        await pedidos.SaveChangesAsync(cancellationToken);
        return pedido.Id;
    }
}
```

```csharp
public sealed record ObtenerPedidoQuery(Guid Id);

public sealed class ObtenerPedidoHandler(IPedidoReadDb db)
    : IQueryHandler<ObtenerPedidoQuery, PedidoResumen>
{
    public async Task<PedidoResumen?> HandleAsync(
        ObtenerPedidoQuery query,
        CancellationToken cancellationToken) =>
        await db.Pedidos
            .Where(p => p.Id == query.Id)
            .Select(p => new PedidoResumen(p.Id, p.Estado, p.Total))
            .FirstOrDefaultAsync(cancellationToken);
}
```

El comando devuelve el id generado y nada más. La consulta devuelve un modelo de lectura ajustado a lo que el cliente necesita.

## Ejemplo aplicado

Proyección de lectura actualizada por eventos, con dos contextos:

```csharp
// Escritura: modelo rico y transaccional.
public sealed class Pedido
{
    public Guid Id { get; private set; }
    public Guid ProductoId { get; private set; }
    public int Cantidad { get; private set; }
    public EstadoPedido Estado { get; private set; }

    public static Pedido Crear(Guid productoId, int cantidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);
        return new Pedido { Id = Guid.NewGuid(), ProductoId = productoId, Cantidad = cantidad };
    }
}
```

```csharp
// Lectura: tabla desnormalizada y rápida.
public sealed class PedidoLectura
{
    public Guid Id { get; init; }
    public string Cliente { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public decimal Total { get; init; }
}

public sealed class PedidoProyeccion(PedidosLecturaDbContext lectura)
{
    public async Task HandleAsync(PedidoCreado evento, CancellationToken cancellationToken)
    {
        var existente = await lectura.Pedidos.FindAsync([evento.PedidoId], cancellationToken);
        if (existente is not null)
        {
            return; // Idempotente.
        }

        lectura.Pedidos.Add(new PedidoLectura
        {
            Id = evento.PedidoId,
            Cliente = evento.Cliente,
            Estado = "Pendiente",
            Total = evento.Total
        });

        await lectura.SaveChangesAsync(cancellationToken);
    }
}
```

```text
Escritura: POST /pedidos → guarda el agregado y publica PedidoCreado
Lectura:   GET /pedidos   → consulta la tabla desnormalizada
Retraso típico: milisegundos; el usuario puede ver el pedido un instante después
```

La consulta no hace `JOIN` con cinco tablas ni carga el agregado. La escritura no se preocupa por el formato de salida.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| CQRS completo en un CRUD | Moda | Empezar sin CQRS o con la variante ligera |
| Dos bases sin estrategia de proyección | Se copió una arquitectura | Definir eventos, reintentos e idempotencia |
| El comando devuelve la entidad completa | Se mezcló con consulta | Devolver id o resultado mínimo |
| La consulta usa el agregado de escritura | Se duplicó el modelo sin necesidad | Modelo de lectura específico |
| Proyecciones desincronizadas sin alertas | No hay monitoreo | Medir el lag y alertar |
| Consistencia eventual no comunicada al usuario | Se ocultó el retraso | Mostrar estado "procesando" o refrescar |

## Buenas prácticas

- Empezá con handlers separados y una sola base.
- Un comando, una intención de negocio; una query, una necesidad de pantalla.
- Modelos de lectura ajustados al consumidor, no a las tablas.
- Proyecciones idempotentes y monitoreadas.
- No prometas lectura inmediata si hay consistencia eventual.
- Medí antes de partir la base: el costo debe justificarse.

## Ejercicio

Implementá CQRS ligero para una lista de tareas: `CrearTareaCommand`, `CompletarTareaCommand` y `ListarTareasQuery` que devuelva solo id, título y estado. Usá la misma base de datos y modelos separados de lectura.

<details>
<summary>Ver solución</summary>

```csharp
public sealed record CrearTareaCommand(string Titulo);
public sealed record CompletarTareaCommand(Guid Id);
public sealed record ListarTareasQuery(bool? Completadas);

public sealed record TareaResumen(Guid Id, string Titulo, bool Completada);

public sealed class CrearTareaHandler(AppDbContext db)
{
    public async Task<Guid> HandleAsync(CrearTareaCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Titulo))
        {
            throw new ArgumentException("El título es obligatorio.");
        }

        var tarea = new Tarea(Guid.NewGuid(), command.Titulo.Trim());
        db.Tareas.Add(tarea);
        await db.SaveChangesAsync(cancellationToken);
        return tarea.Id;
    }
}

public sealed class CompletarTareaHandler(AppDbContext db)
{
    public async Task<bool> HandleAsync(CompletarTareaCommand command, CancellationToken cancellationToken)
    {
        var tarea = await db.Tareas.FindAsync([command.Id], cancellationToken);
        if (tarea is null)
        {
            return false;
        }

        tarea.Completar();
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class ListarTareasHandler(AppDbContext db)
{
    public async Task<IReadOnlyList<TareaResumen>> HandleAsync(
        ListarTareasQuery query,
        CancellationToken cancellationToken)
    {
        var consulta = db.Tareas.AsNoTracking();

        if (query.Completadas is { } completadas)
        {
            consulta = consulta.Where(t => t.Completada == completadas);
        }

        return await consulta
            .Select(t => new TareaResumen(t.Id, t.Titulo, t.Completada))
            .ToListAsync(cancellationToken);
    }
}
```

```text
POST /tareas            → comando, devuelve 201 con el id
POST /tareas/{id}/completar → comando, 204 o 404
GET  /tareas?completadas=false → query, devuelve solo los campos necesarios
```

La consulta proyecta directamente en SQL y no carga la entidad completa.

</details>

## Resumen

- CQRS separa intenciones de escritura y lectura.
- La variante ligera resuelve la mayoría de los casos a bajo costo.
- Las proyecciones separadas escalan lecturas pero introducen consistencia eventual.
- Los comandos no devuelven modelos de lectura.
- El patrón se justifica por necesidades distintas reales.

## Siguiente paso

Toda decisión de arquitectura necesita registro y contexto. En [Decisiones y ADRs](/modulos/arquitectura/decisiones-adr) vas a documentar por qué el sistema es como es.
