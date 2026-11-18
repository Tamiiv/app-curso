---
slug: asnotracking-rendimiento
title: "AsNoTracking y rendimiento"
order: 7
estimatedMinutes: 15
summary: "Reducí el costo del ChangeTracker en lecturas y actualizá miles de filas con ExecuteUpdate y ExecuteDelete sin cargarlas en memoria."
quiz:
  - question: "¿Cuándo conviene usar AsNoTracking?"
    options:
      - "Cuando vas a modificar las entidades y guardar cambios"
      - "En lecturas que no necesitan seguir los cambios"
      - "Solo en migraciones"
      - "Cuando necesitás cargar la misma entidad dos veces"
    answer: 1
    explanation: "Sin tracking, EF Core no guarda instantáneas ni resuelve identidades, lo que reduce memoria y tiempo en consultas de solo lectura."
  - question: "¿Qué hace ExecuteUpdateAsync?"
    options:
      - "Carga las entidades, las modifica y llama a SaveChanges"
      - "Ejecuta un UPDATE directamente en la base y devuelve las filas afectadas"
      - "Genera una migración"
      - "Solo funciona con AsNoTracking"
    answer: 1
    explanation: "ExecuteUpdate arma un UPDATE masivo en SQL; no pasa por el ChangeTracker ni ejecuta SaveChanges."
  - question: "¿Qué desventaja tiene AsSplitQuery?"
    options:
      - "Duplica las filas del resultado"
      - "Genera varias consultas y puede ser inconsistente sin transacción"
      - "Impide usar Include"
      - "Desactiva los índices"
    answer: 1
    explanation: "Divide la consulta en varias para evitar la explosión cartesiana, a costa de más viajes a la base y de necesitar una transacción si exigís consistencia."
  - question: "¿Cuándo hace falta AsNoTrackingWithIdentityResolution?"
    options:
      - "Cuando querés modificar entidades sin tracking"
      - "Cuando una lectura sin tracking puede devolver la misma entidad repetida y necesitás una sola instancia"
      - "Cuando usás ExecuteDelete"
      - "Cuando no hay relaciones"
    answer: 1
    explanation: "Resuelve identidades en memoria sin registrar las entidades para cambios, útil en grafos con referencias repetidas."
---

# AsNoTracking y rendimiento

## Objetivo

Medir el impacto del tracking, desactivarlo en lecturas y usar operaciones masivas para modificar o borrar datos sin cargarlos en memoria.

## Requisitos previos

Necesitás consultas con filtros y proyecciones, y un modelo con relaciones. También ayuda entender qué hace `SaveChangesAsync` con el `ChangeTracker`.

## Concepto

Por defecto, EF Core sigue cada entidad que materializa. Guarda una instantánea de los valores originales para detectar cambios, resuelve identidades y mantiene referencias. Ese trabajo es necesario si vas a modificar y guardar, pero es costo puro en una lectura.

`AsNoTracking()` indica que la consulta no registre entidades. El resultado es más liviano y rápido. La contrapartida es que los cambios que hagas sobre esos objetos no se guardan con `SaveChangesAsync`.

| Necesidad | Configuración |
|---|---|
| Leer y mostrar | `AsNoTracking()` |
| Leer, modificar y guardar | Tracking normal |
| Leer un grafo con identidades repetidas | `AsNoTrackingWithIdentityResolution()` |
| Varios `Include` de colecciones | `AsSplitQuery()` |

Las operaciones masivas van un paso más allá. `ExecuteUpdateAsync` y `ExecuteDeleteAsync` generan un `UPDATE` o `DELETE` directo y devuelven la cantidad de filas afectadas. No cargan entidades, no usan el `ChangeTracker` y no llaman a `SaveChanges`.

Eso las hace ideales para mantenimiento: aplicar un descuento a toda una categoría, borrar registros viejos, marcar como inactivos. La contrapartida es que omiten validaciones de la aplicación, eventos y tokens de concurrencia.

Para que una consulta rinda también importan los índices, la paginación y proyectar solo las columnas necesarias. `AsNoTracking` no compensa una consulta que trae toda la tabla.

## Ejemplo mínimo

Comparar cuántas entidades quedan trackeadas:

```csharp
using Microsoft.EntityFrameworkCore;

var conTracking = await db.Productos.ToListAsync();
Console.WriteLine($"Con tracking: {db.ChangeTracker.Entries().Count()} entradas");

db.ChangeTracker.Clear();

var sinTracking = await db.Productos
    .AsNoTracking()
    .ToListAsync();

Console.WriteLine($"Sin tracking: {db.ChangeTracker.Entries().Count()} entradas");
Console.WriteLine($"Productos leídos: {sinTracking.Count}");
```

```text
Con tracking: 3 entradas
Sin tracking: 0 entradas
Productos leídos: 3
```

`ChangeTracker.Clear()` descarta el estado anterior para que la medición sea limpia. En un contexto nuevo, el segundo conteo también sería cero.

## Ejemplo aplicado

Actualización masiva y borrado con operaciones directas:

```csharp
decimal precioMinimo = 100000m;

int actualizados = await db.Productos
    .Where(p => p.Precio >= precioMinimo)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(p => p.Precio, p => p.Precio * 0.9m));

Console.WriteLine($"Productos actualizados: {actualizados}");

DateOnly limite = new(2025, 1, 1);

int eliminados = await db.Productos
    .Where(p => p.Estado == EstadoProducto.Descontinuado && p.CreadoEn < limite.ToDateTime(TimeOnly.MinValue))
    .ExecuteDeleteAsync();

Console.WriteLine($"Productos eliminados: {eliminados}");
```

```text
Productos actualizados: 2
Productos eliminados: 1
```

Las dos operaciones se ejecutan en SQL. No hay entidades en memoria ni `SaveChanges`. Si necesitás que ambas sean atómicas, envolvelas en una transacción explícita, como vas a ver en la lección siguiente.

Para grafos con varios `Include`, `AsSplitQuery` evita la multiplicación de filas:

```csharp
var clientes = await db.Clientes
    .AsNoTracking()
    .Include(c => c.Pedidos)
    .ThenInclude(p => p.Detalles)
    .AsSplitQuery()
    .ToListAsync();
```

En lugar de un `JOIN` gigante, EF Core ejecuta una consulta por cada colección. El resultado es menos datos duplicados, pero más viajes a la base.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Los cambios no se guardan | Se usó `AsNoTracking` y luego `SaveChanges` | Quitar `AsNoTracking` o adjuntar y marcar la entidad |
| `ExecuteUpdate` no actualiza objetos en memoria | La operación va directo a la base | Volver a consultar o limpiar el contexto |
| `ExecuteUpdate` no compila con `Include` | No se permite componer una actualización con navegaciones | Filtrar por claves o usar una subconsulta |
| Se duplican entidades en el resultado | El `JOIN` de colecciones repite el principal | Proyectar o usar `AsSplitQuery` |
| Dos operaciones masivas quedan a medias | Cada `Execute*` es independiente | Envolverlas en una transacción |
| La consulta sigue lenta sin tracking | Falta un índice o se traen demasiadas columnas | Revisar plan, índices y proyección |

## Buenas prácticas

- Aplicá `AsNoTracking` en endpoints y reportes de solo lectura.
- Proyectá a DTOs antes de desactivar el tracking; muchas veces la proyección ya evita el problema.
- Usá `ExecuteUpdate` y `ExecuteDelete` para mantenimiento masivo, no para cambios unitarios con reglas de negocio.
- Medí antes de optimizar: `ToQueryString`, `EXPLAIN ANALYZE` y tiempos reales.
- Creá índices para las columnas de filtro y orden, y revisá su costo de escritura.

## Ejercicio

Aplicá un aumento del 15 % a los cursos virtuales con precio menor a 50000 y luego eliminá los cursos sin módulos creados antes de 2024. Mostrá cuántas filas afectó cada operación y verificá con una consulta sin tracking que los cambios están en la base.

<details>
<summary>Ver solución</summary>

```csharp
int actualizados = await db.Cursos
    .Where(c => c.Modalidad == Modalidad.Virtual && c.Precio < 50000m)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(c => c.Precio, c => c.Precio * 1.15m));

Console.WriteLine($"Cursos actualizados: {actualizados}");

int eliminados = await db.Cursos
    .Where(c => !c.Modulos.Any() && c.FechaInicio < new DateOnly(2024, 1, 1))
    .ExecuteDeleteAsync();

Console.WriteLine($"Cursos eliminados: {eliminados}");

var restantes = await db.Cursos
    .AsNoTracking()
    .OrderBy(c => c.Titulo)
    .Select(c => new { c.Titulo, c.Precio })
    .ToListAsync();

foreach (var curso in restantes)
{
    Console.WriteLine($"{curso.Titulo}: {curso.Precio}");
}
```

```text
Cursos actualizados: 1
Cursos eliminados: 1
EF Core desde cero: 57500.00
```

La consulta final usa `AsNoTracking` porque solo muestra datos. Los cambios ya fueron confirmados por las operaciones masivas.

</details>

## Resumen

- El tracking cuesta memoria y tiempo; no lo pagues en lecturas.
- `AsNoTracking` mejora consultas de solo lectura y no permite guardar cambios.
- `ExecuteUpdate` y `ExecuteDelete` operan en la base sin cargar entidades.
- `AsSplitQuery` reduce la duplicación de filas a cambio de más consultas.
- Índices, proyección y paginación siguen siendo la base del rendimiento.

## Siguiente paso

Las operaciones masivas pueden quedar a medias si falla la segunda. En la próxima lección vas a coordinarlas con [transacciones y concurrencia](/modulos/ef-core/transacciones-concurrencia).
