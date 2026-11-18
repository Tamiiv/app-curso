---
slug: transacciones-concurrencia
title: "Transacciones y concurrencia"
order: 8
estimatedMinutes: 15
summary: "Agrupá operaciones en transacciones atómicas y resolvé ediciones simultáneas con tokens de concurrencia y reintentos controlados."
quiz:
  - question: "¿Qué garantiza SaveChangesAsync por sí solo?"
    options:
      - "Que todos los cambios de esa llamada se apliquen en una transacción"
      - "Que dos usuarios no puedan editar la misma fila"
      - "Que las consultas posteriores usen la misma conexión"
      - "Que se ejecuten las migraciones pendientes"
    answer: 0
    explanation: "SaveChanges envuelve sus INSERT, UPDATE y DELETE en una transacción; si uno falla, no se confirma ninguno."
  - question: "¿Cuándo hace falta una transacción explícita?"
    options:
      - "Cuando se guarda una sola entidad"
      - "Cuando varias operaciones o llamadas a SaveChanges deben ser atómicas"
      - "Siempre que se usa AsNoTracking"
      - "Solo al crear migraciones"
    answer: 1
    explanation: "Si necesitás que dos SaveChanges o una operación masiva y otra escritura se confirmen juntas, la transacción explícita es la herramienta."
  - question: "¿Qué genera un token de concurrencia en el UPDATE?"
    options:
      - "Un bloqueo de tabla permanente"
      - "Una condición adicional en el WHERE con el valor original"
      - "Un índice único automático"
      - "Una consulta previa por cada fila"
    answer: 1
    explanation: "EF Core compara el valor original del token; si otra operación lo cambió, el UPDATE afecta cero filas y se lanza DbUpdateConcurrencyException."
  - question: "¿Para qué sirve Database.CreateExecutionStrategy()?"
    options:
      - "Para crear la base si no existe"
      - "Para ejecutar un bloque con la política de reintentos del proveedor"
      - "Para cambiar el nivel de aislamiento"
      - "Para desactivar el tracking"
    answer: 1
    explanation: "La estrategia permite reintentar operaciones transitorias; es obligatorio usarla cuando combinás reintentos con transacciones explícitas."
---

# Transacciones y concurrencia

## Objetivo

Coordinar varias escrituras en una sola transacción, entender el aislamiento y resolver conflictos cuando dos usuarios modifican el mismo registro.

## Requisitos previos

Necesitás consultas y operaciones de escritura funcionando, además de un modelo con claves y relaciones. Conviene haber visto `ExecuteUpdate` y `ExecuteDelete` en la lección anterior.

## Concepto

Una transacción agrupa operaciones que deben confirmarse o revertirse juntas. `SaveChangesAsync` ya usa una transacción interna: si insertás un pedido y sus detalles en la misma llamada, o se guardan todos o ninguno.

Cuando necesitás atomicidad entre varias llamadas, abrís una transacción explícita con `BeginTransactionAsync` y terminás con `CommitAsync` o `RollbackAsync`. Mientras está abierta, todas las operaciones del contexto participan de ella.

| Nivel de aislamiento | Efecto |
|---|---|
| `ReadCommitted` | Valor por defecto en PostgreSQL |
| `RepeatableRead` | Evita que cambien filas ya leídas |
| `Serializable` | Equivale a ejecutar en serie; puede fallar por conflicto |

La concurrencia optimista no bloquea filas. Cada entidad lleva un token, por ejemplo una columna `Version`, que se incluye en el `WHERE` del `UPDATE`. Si otro proceso cambió la fila, el token ya no coincide, se actualizan cero filas y EF Core lanza `DbUpdateConcurrencyException`. La aplicación decide entonces si recarga, reintenta o avisa al usuario.

La concurrencia pesimista, en cambio, bloquea la fila con `SELECT ... FOR UPDATE`. En EF Core no hay una API de alto nivel para eso; se usa SQL crudo o una transacción con bloqueo explícito. Para la mayoría de los casos, el token de concurrencia es suficiente.

Si el proveedor configura reintentos automáticos, una transacción explícita debe ejecutarse dentro de `CreateExecutionStrategy`; de lo contrario EF Core lanza una excepción porque no puede reintentar un bloque que ya empezó.

## Ejemplo mínimo

Transferir stock entre dos productos de forma atómica:

```csharp
using Microsoft.EntityFrameworkCore;

var strategy = db.Database.CreateExecutionStrategy();

await strategy.ExecuteAsync(async () =>
{
    await using var transaccion = await db.Database.BeginTransactionAsync();

    var origen = await db.Productos.SingleAsync(p => p.Id == 1);
    var destino = await db.Productos.SingleAsync(p => p.Id == 2);

    if (origen.Stock < 3)
    {
        throw new InvalidOperationException("Stock insuficiente.");
    }

    origen.Stock -= 3;
    destino.Stock += 3;

    await db.SaveChangesAsync();
    await transaccion.CommitAsync();

    Console.WriteLine("Transferencia confirmada.");
});
```

```text
Transferencia confirmada.
```

Si `SaveChangesAsync` falla, la transacción se revierte al salir del `await using`. El `CommitAsync` solo se ejecuta cuando ambas entidades quedaron actualizadas.

## Ejemplo aplicado

Un token de concurrencia se configura en el modelo:

```csharp
public sealed class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int Version { get; set; }
}

public sealed class ProductoConfiguracion : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("productos");
        builder.Property(p => p.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Version).IsConcurrencyToken();
    }
}
```

Dos sesiones intentan vender la última unidad:

```csharp
await using var db1 = new CatalogoDbContext(options);
await using var db2 = new CatalogoDbContext(options);

var p1 = await db1.Productos.SingleAsync(p => p.Id == 1);
var p2 = await db2.Productos.SingleAsync(p => p.Id == 1);

p1.Stock -= 1;
p1.Version++;
await db1.SaveChangesAsync();
Console.WriteLine($"Sesión 1: stock {p1.Stock}");

p2.Stock -= 1;
p2.Version++;

try
{
    await db2.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    var entrada = ex.Entries.Single();
    await entrada.ReloadAsync();
    Console.WriteLine($"Sesión 2: conflicto, stock actual {entrada.Entity.Stock}");
}
```

```text
Sesión 1: stock 0
Sesión 2: conflicto, stock actual 0
```

La segunda actualización envió `WHERE "Id" = 1 AND "Version" = 1`, pero la fila ya tenía `Version = 2`. `ReloadAsync` trae los valores actuales y permite mostrar un mensaje coherente o reintentar la operación.

En PostgreSQL también podés usar la columna de sistema `xmin` como token, con `UseXminAsConcurrencyToken()` del proveedor Npgsql. Evita mantener una columna propia, aunque ata el modelo al proveedor.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `DbUpdateConcurrencyException` | Otro proceso cambió la fila desde que la leíste | Recargar, reintentar o informar el conflicto |
| `The execution strategy does not support user-initiated transactions` | Se abrió una transacción sin `CreateExecutionStrategy` | Envolver el bloque en la estrategia |
| La transacción no impacta | Faltó `CommitAsync` | Confirmar antes de salir del bloque |
| `deadlock detected` | Dos transacciones tomaron bloqueos en distinto orden | Ordenar las operaciones y acortar las transacciones |
| `connection is already in a transaction` | Se abrió otra transacción sobre la misma conexión | Reutilizar la transacción existente |
| Una lectura ve datos a medias | Se usó un aislamiento demasiado bajo para el caso | Evaluar `RepeatableRead` o `Serializable` |

## Buenas prácticas

- Dejá que `SaveChangesAsync` maneje la transacción cuando todo ocurre en una sola llamada.
- Abrí transacciones explícitas solo cuando varias operaciones deben ser atómicas.
- Mantené las transacciones cortas y sin llamadas HTTP ni esperas largas.
- Usá tokens de concurrencia en entidades que editan varias personas.
- Definí una política clara ante conflictos: recargar, reintentar o pedir confirmación.

## Ejercicio

Implementá una reserva de stock: verificá que haya al menos 2 unidades, descontalas, registrá un `Movimiento` y guardá todo en una transacción. Si el stock es insuficiente, revertí y mostrá un mensaje. Después simulá un conflicto de concurrencia sobre el mismo producto y resolvelo recargando la entidad.

<details>
<summary>Ver solución</summary>

```csharp
var strategy = db.Database.CreateExecutionStrategy();

await strategy.ExecuteAsync(async () =>
{
    await using var transaccion = await db.Database.BeginTransactionAsync();

    var producto = await db.Productos.SingleAsync(p => p.Id == 1);

    if (producto.Stock < 2)
    {
        await transaccion.RollbackAsync();
        Console.WriteLine("Stock insuficiente.");
        return;
    }

    producto.Stock -= 2;
    producto.Version++;

    db.Movimientos.Add(new Movimiento
    {
        ProductoId = producto.Id,
        Cantidad = -2,
        Fecha = DateTime.UtcNow
    });

    try
    {
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        Console.WriteLine($"Reserva confirmada. Stock: {producto.Stock}");
    }
    catch (DbUpdateConcurrencyException)
    {
        await transaccion.RollbackAsync();
        Console.WriteLine("La reserva no se pudo completar por un cambio simultáneo.");
    }
});
```

```text
Reserva confirmada. Stock: 8
```

Si el `UPDATE` no afecta filas por el token, el `catch` revierte todo, incluido el movimiento. No queda un registro de movimiento sin descuento de stock.

</details>

## Resumen

- `SaveChangesAsync` ya es atómico para los cambios de una llamada.
- Una transacción explícita coordina varias operaciones o varios `SaveChanges`.
- El aislamiento define qué anomalías pueden ocurrir entre transacciones.
- Un token de concurrencia detecta ediciones simultáneas sin bloquear filas.
- `DbUpdateConcurrencyException` se resuelve recargando, reintentando o informando.

## Siguiente paso

Ya tenés las piezas: contexto, modelo, migraciones, consultas, rendimiento y transacciones. En el [ejercicio integrador](/modulos/ef-core/ejercicio-integrador) vas a combinarlas en un caso completo.
