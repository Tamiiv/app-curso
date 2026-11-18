---
slug: carga-navegacion
title: "Include y proyecciones"
order: 6
estimatedMinutes: 15
summary: "Cargá relaciones con Include y ThenInclude, evitá el problema N+1 y elegí proyecciones cuando necesitás solo una parte del grafo."
quiz:
  - question: "¿Para qué sirve ThenInclude?"
    options:
      - "Para filtrar la entidad principal"
      - "Para cargar una navegación de la navegación anterior"
      - "Para ordenar los resultados"
      - "Para desactivar el tracking"
    answer: 1
    explanation: "Include carga el primer nivel y ThenInclude permite seguir bajando por relaciones anidadas."
  - question: "¿Qué ocurre si usás Include después de un Select que devuelve un DTO?"
    options:
      - "EF Core incluye igual todas las columnas"
      - "El Include se ignora porque la proyección ya define qué datos se consultan"
      - "Se lanza una excepción siempre"
      - "Se duplican los resultados"
    answer: 1
    explanation: "Al proyectar a un tipo que no es la entidad, Include no tiene efecto: la proyección decide qué columnas y navegaciones se traen."
  - question: "¿Qué es el problema N+1?"
    options:
      - "Una consulta que devuelve N filas de más"
      - "Una consulta principal más una consulta extra por cada fila al acceder a una navegación"
      - "Un error de migración con N tablas"
      - "Un índice que falta en N columnas"
    answer: 1
    explanation: "Se carga una lista y luego se consulta la relación de cada elemento, generando N consultas adicionales y mucho más tiempo de red."
  - question: "¿Qué permite un Include filtrado como Include(c => c.Pedidos.Where(p => p.Total > 1000))?"
    options:
      - "Limitar la colección cargada a los pedidos que cumplen la condición"
      - "Filtrar los clientes que tienen pedidos"
      - "Evitar que se cargue la colección"
      - "Crear un índice automáticamente"
    answer: 0
    explanation: "El filtro se aplica a la navegación de colección; los clientes se siguen devolviendo aunque no tengan pedidos que cumplan la condición."
---

# Include y proyecciones

## Objetivo

Cargar relaciones de forma explícita con `Include` y `ThenInclude`, detectar el problema N+1 y decidir cuándo conviene proyectar en lugar de traer el grafo completo.

## Requisitos previos

Necesitás entidades relacionadas y consultas LINQ funcionando. Si todavía no modelaste relaciones, repasá [relaciones entre entidades](/modulos/ef-core/relaciones).

## Concepto

EF Core no carga las navegaciones automáticamente. Si consultás clientes y luego accedés a `cliente.Pedidos`, la colección puede estar vacía aunque existan filas en la base. Eso es intencional: cargar todo siempre sería costoso e impredecible.

`Include` indica qué navegación querés traer en la misma consulta. Para relaciones anidadas se encadena `ThenInclude`. El resultado es una consulta con `JOIN` que materializa el grafo.

```csharp
var clientes = await db.Clientes
    .Include(c => c.Pedidos)
    .ThenInclude(p => p.Detalles)
    .ThenInclude(d => d.Producto)
    .ToListAsync();
```

Hay tres estrategias de carga:

| Estrategia | Cuándo usarla |
|---|---|
| Carga anticipada (`Include`) | Sabés desde el inicio qué navegaciones necesitás |
| Carga explícita (`Entry(...).Collection(...).LoadAsync()`) | La necesitás después de una condición |
| Carga diferida (lazy loading) | Poco recomendada en servicios: oculta consultas |

El problema N+1 aparece cuando cargás una lista y después consultás una navegación por cada elemento. Una consulta principal y N adicionales pueden ser mucho más lentas que un solo `JOIN`, sobre todo con latencia de red.

Sin embargo, `Include` no siempre es la respuesta. Si solo necesitás el nombre del cliente y la cantidad de pedidos, una proyección trae menos columnas y evita duplicar filas. Las proyecciones también son la forma natural de devolver DTOs desde una API.

## Ejemplo mínimo

Clientes con sus pedidos:

```csharp
using Microsoft.EntityFrameworkCore;

List<Cliente> clientes = await db.Clientes
    .Include(c => c.Pedidos)
    .OrderBy(c => c.Nombre)
    .ToListAsync();

foreach (Cliente cliente in clientes)
{
    Console.WriteLine($"{cliente.Nombre}: {cliente.Pedidos.Count} pedido(s)");

    foreach (Pedido pedido in cliente.Pedidos.OrderBy(p => p.Fecha))
    {
        Console.WriteLine($"  {pedido.Fecha:yyyy-MM-dd} - {pedido.Total}");
    }
}
```

```text
Ana Torres: 2 pedido(s)
  2026-03-01 - 45000.00
  2026-03-15 - 12000.00
Bruno Díaz: 1 pedido(s)
  2026-03-10 - 85000.00
```

Sin `Include`, `cliente.Pedidos` estaría vacío. Con `Include`, EF Core llena la colección y el `ChangeTracker` registra también los pedidos.

## Ejemplo aplicado

Dos consultas: una con grafo y otra con proyección. La segunda es la que conviene para un listado:

```csharp
var conInclude = await db.Clientes
    .Include(c => c.Pedidos)
    .ThenInclude(p => p.Detalles)
    .ThenInclude(d => d.Producto)
    .SingleAsync(c => c.Id == 1);

Console.WriteLine($"Pedidos cargados: {conInclude.Pedidos.Count}");

var resumen = await db.Clientes
    .OrderBy(c => c.Nombre)
    .Select(c => new
    {
        c.Nombre,
        Cantidad = c.Pedidos.Count,
        Total = c.Pedidos.Sum(p => p.Total)
    })
    .ToListAsync();

foreach (var fila in resumen)
{
    Console.WriteLine($"{fila.Nombre}: {fila.Cantidad} pedidos, total {fila.Total}");
}
```

```text
Pedidos cargados: 2
Ana Torres: 2 pedidos, total 57000.00
Bruno Díaz: 1 pedidos, total 85000.00
```

La proyección se traduce a `COUNT` y `SUM` en SQL. No trae pedidos ni detalles, solo el resultado agregado. Para un endpoint de listado suele ser la mejor opción.

Si necesitás filtrar una colección incluida y limitar su tamaño, usá un Include filtrado:

```csharp
var clientesConPedidosGrandes = await db.Clientes
    .Include(c => c.Pedidos.Where(p => p.Total > 40000m))
    .ToListAsync();
```

Los clientes sin pedidos que cumplan la condición igual aparecen, pero con la colección vacía.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La colección de navegación está vacía | No se usó `Include` | Cargar la relación o proyectar |
| `ThenInclude` no compila | Se encadenó sobre una propiedad escalar | Verificar que el paso anterior sea una navegación |
| Una consulta por cada fila | Se accede a una navegación no cargada dentro de un bucle | Usar `Include` o proyectar |
| Filas duplicadas | El `JOIN` de una colección repite el principal | Proyectar con `Distinct` o usar `AsSplitQuery` |
| `Include` no tiene efecto | Se aplicó después de un `Select` a DTO | Incluir la navegación dentro de la proyección |
| Se traen miles de filas para mostrar un resumen | Se cargó el grafo completo cuando bastaba un agregado | Proyectar con `Count` y `Sum` |

## Buenas prácticas

- Incluí solo las navegaciones que vas a usar en esa operación.
- Preferí proyecciones para listados y respuestas de API.
- Evitá `Include` de colecciones grandes sin filtro ni paginación.
- No actives lazy loading por defecto: genera consultas invisibles y N+1.
- Revisá el SQL cuando un `Include` produce muchas filas o duplicados.

## Ejercicio

Listá los cursos con su instructor y la cantidad de módulos, sin traer los módulos completos. Después cargá un curso puntual con instructor y módulos usando `Include` y `ThenInclude`, y mostrá el título de cada módulo.

<details>
<summary>Ver solución</summary>

```csharp
var listado = await db.Cursos
    .OrderBy(c => c.Titulo)
    .Select(c => new
    {
        c.Titulo,
        Instructor = c.Instructor.Nombre,
        Modulos = c.Modulos.Count
    })
    .ToListAsync();

foreach (var curso in listado)
{
    Console.WriteLine($"{curso.Titulo} - {curso.Instructor} - {curso.Modulos} módulos");
}

var detalle = await db.Cursos
    .Include(c => c.Instructor)
    .Include(c => c.Modulos)
    .SingleAsync(c => c.Id == 1);

Console.WriteLine($"Curso: {detalle.Titulo}");
Console.WriteLine($"Instructor: {detalle.Instructor.Nombre}");

foreach (Modulo modulo in detalle.Modulos.OrderBy(m => m.Id))
{
    Console.WriteLine($"  {modulo.Titulo}");
}
```

```text
EF Core desde cero - Bruno Díaz - 2 módulos
Cursos: EF Core desde cero
Instructor: Bruno Díaz
  DbContext
  Consultas
```

</details>

## Resumen

- `Include` carga una navegación y `ThenInclude` sigue a un nivel más profundo.
- Sin carga explícita, las navegaciones no se completan.
- El N+1 se evita con `Include` o con proyecciones.
- Proyectar reduce columnas y es ideal para listados y APIs.
- El Include filtrado limita la colección, no los principales.

## Siguiente paso

Cargar menos datos es solo una parte del rendimiento. En la próxima lección vas a desactivar el tracking y usar operaciones masivas con [AsNoTracking y rendimiento](/modulos/ef-core/asnotracking-rendimiento).
