---
slug: consultas-linq
title: "Consultas con LINQ"
order: 5
estimatedMinutes: 15
summary: "Escribí consultas con IQueryable que EF Core traduce a SQL, con filtros, orden, paginación y operadores asíncronos."
quiz:
  - question: "¿Qué método ejecuta realmente una consulta en la base?"
    options:
      - "Where"
      - "OrderBy"
      - "ToListAsync"
      - "AsQueryable"
    answer: 2
    explanation: "Where y OrderBy solo construyen el árbol de expresión; ToListAsync materializa los resultados y dispara el SQL."
  - question: "¿Qué ventaja ofrece IQueryable frente a IEnumerable en una consulta?"
    options:
      - "Permite que EF Core traduzca la expresión completa a SQL"
      - "Evita abrir conexiones a la base"
      - "Garantiza que no haya errores de traducción"
      - "Desactiva el ChangeTracker automáticamente"
    answer: 0
    explanation: "IQueryable acumula expresiones que el proveedor traduce; IEnumerable obliga a traer los datos antes de seguir filtrando."
  - question: "¿Qué par de métodos implementa paginación correctamente?"
    options:
      - "Take y Distinct"
      - "Skip y Take"
      - "First y Last"
      - "GroupBy y Count"
    answer: 1
    explanation: "Skip salta una cantidad de filas y Take limita cuántas devuelve; conviene combinarlos con un orden estable."
  - question: "¿Qué ocurre si llamás a AsEnumerable antes de un Where?"
    options:
      - "El filtro se traduce igual a SQL"
      - "Los datos se materializan y el filtro se ejecuta en memoria"
      - "La consulta se cancela"
      - "EF Core lanza una excepción de traducción"
    answer: 1
    explanation: "AsEnumerable corta la traducción: todo lo que sigue se ejecuta en el cliente, con más datos transferidos y menos eficiencia."
---

# Consultas con LINQ

## Objetivo

Escribir consultas expresivas con LINQ, entender la diferencia entre `IQueryable` e `IEnumerable` y usar filtros, orden, paginación y operadores asíncronos sin traer datos de más.

## Requisitos previos

Necesitás un contexto con entidades y migraciones aplicadas. También conviene manejar LINQ básico: `Where`, `Select`, `OrderBy` y `GroupBy`, temas del módulo de colecciones.

## Concepto

Un `DbSet<T>` implementa `IQueryable<T>`. Cada operador de LINQ agrega un nodo a un árbol de expresión; EF Core lo traduce a SQL cuando la consulta se enumera. Por eso podés encadenar filtros sin que se ejecute nada hasta el final.

La ejecución es diferida. `Where` y `OrderBy` no van a la base; `ToListAsync`, `FirstOrDefaultAsync`, `CountAsync` o un `foreach` sí. Si nunca enumerás la consulta, nunca se ejecuta.

| Operador | Traducción |
|---|---|
| `Where` | `WHERE` |
| `OrderBy` / `ThenBy` | `ORDER BY` |
| `Select` | Lista de columnas del `SELECT` |
| `Skip` / `Take` | `OFFSET` / `LIMIT` |
| `GroupBy` | `GROUP BY` |
| `Any` / `Count` | `EXISTS` / `COUNT` |

No todo se traduce. Un método propio o una operación no soportada puede lanzar `InvalidOperationException` o, en versiones recientes, obligarte a materializar antes. Cuando veas que EF Core no puede traducir, reformulá la consulta o proyectá primero.

`IQueryable` se compone: podés armar la consulta en varios pasos y agregar filtros condicionales. `IEnumerable` corta esa composición y ejecuta en memoria.

## Ejemplo mínimo

Productos activos, ordenados por precio y proyectados a un DTO:

```csharp
using Microsoft.EntityFrameworkCore;

var consulta = db.Productos
    .Where(p => p.Estado == EstadoProducto.Activo)
    .OrderByDescending(p => p.Precio)
    .Select(p => new ProductoResumen(p.Id, p.Nombre, p.Precio));

List<ProductoResumen> productos = await consulta.ToListAsync();

foreach (ProductoResumen p in productos)
{
    Console.WriteLine($"{p.Id}: {p.Nombre} - {p.Precio}");
}

public sealed record ProductoResumen(int Id, string Nombre, decimal Precio);
```

```text
3: Monitor 27 pulgadas - 350000.00
1: Teclado mecánico - 85000.00
2: Mouse - 12000.00
```

El `Select` a un record hace que SQL solo traiga las columnas necesarias. Es una de las mejoras de rendimiento más simples y efectivas.

## Ejemplo aplicado

Búsqueda con filtros opcionales, texto parcial y paginación:

```csharp
string? texto = "mon";
int pagina = 1;
int tamano = 10;

IQueryable<Producto> consulta = db.Productos;

if (!string.IsNullOrWhiteSpace(texto))
{
    string patron = $"%{texto}%";
    consulta = consulta.Where(p => EF.Functions.Like(p.Nombre, patron));
}

consulta = consulta
    .Where(p => p.Estado == EstadoProducto.Activo)
    .OrderBy(p => p.Nombre)
    .ThenBy(p => p.Id)
    .Skip((pagina - 1) * tamano)
    .Take(tamano);

Console.WriteLine(consulta.ToQueryString());

List<Producto> resultados = await consulta.ToListAsync();
Console.WriteLine($"Encontrados: {resultados.Count}");
```

```text
SELECT p."Id", p."CreadoEn", p."Estado", p."Nombre", p."Precio", p."Sku"
FROM productos AS p
WHERE p."Nombre" ILIKE @__patron_1 AND p."Estado" = 'Activo'
ORDER BY p."Nombre", p."Id"
LIMIT @__p_2 OFFSET @__p_3

Encontrados: 1
```

`EF.Functions.Like` se traduce a `LIKE` y, en PostgreSQL, el proveedor usa `ILIKE` para búsquedas sin distinguir mayúsculas según la configuración. El patrón viaja como parámetro, no concatenado en el SQL.

El `OrderBy` con un segundo criterio (`ThenBy`) da un orden estable. Sin él, dos filas con el mismo nombre podrían cambiar de página entre consultas.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| La consulta trae toda la tabla | Se llamó a `AsEnumerable` o `ToList` antes de filtrar | Mantener `IQueryable` hasta el operador final |
| `could not be translated` | Se usó un método que EF Core no conoce | Reformular con operadores traducibles o proyectar antes |
| Se ejecutan consultas dentro de un bucle | Se accede a una navegación no cargada por cada fila | Usar `Include` o una proyección, tema de la lección 6 |
| Los resultados se repiten entre páginas | Falta un orden estable | Agregar `ThenBy` con la clave |
| `NpgsqlException: operator does not exist` | Se comparó un tipo incompatible | Revisar el tipo de la propiedad y del parámetro |
| La consulta tarda aunque devuelve pocas filas | Falta un índice en la columna filtrada | Crear un índice con migración |

## Buenas prácticas

- Proyectá a DTOs con `Select` para traer solo las columnas que usás.
- Mantené la consulta como `IQueryable` y agregá filtros antes de materializar.
- Usá siempre operadores asíncronos en aplicaciones web y de consola con I/O.
- Ordená por una columna única o agregá la clave como desempate para paginar.
- Revisá `ToQueryString()` cuando una consulta no rinde como esperás.

## Ejercicio

Escribí una consulta que reciba un precio mínimo opcional y una página, filtre productos activos, ordene por precio ascendente y luego por nombre, y devuelva solo `Id`, `Nombre` y `Precio`. Mostrá el SQL generado y la cantidad de resultados para la página 2 con tamaño 5.

<details>
<summary>Ver solución</summary>

```csharp
decimal? precioMinimo = 50000m;
int pagina = 2;
int tamano = 5;

IQueryable<Producto> consulta = db.Productos
    .Where(p => p.Estado == EstadoProducto.Activo);

if (precioMinimo is decimal minimo)
{
    consulta = consulta.Where(p => p.Precio >= minimo);
}

var paginada = consulta
    .OrderBy(p => p.Precio)
    .ThenBy(p => p.Nombre)
    .Skip((pagina - 1) * tamano)
    .Take(tamano)
    .Select(p => new { p.Id, p.Nombre, p.Precio });

Console.WriteLine(paginada.ToQueryString());

var resultados = await paginada.ToListAsync();
Console.WriteLine($"Resultados: {resultados.Count}");

foreach (var p in resultados)
{
    Console.WriteLine($"{p.Id}: {p.Nombre} - {p.Precio}");
}
```

```text
SELECT p."Id", p."Nombre", p."Precio"
FROM productos AS p
WHERE p."Estado" = 'Activo' AND p."Precio" >= @__minimo_1
ORDER BY p."Precio", p."Nombre"
LIMIT @__p_2 OFFSET @__p_3

Resultados: 1
```

</details>

## Resumen

- `IQueryable` compone expresiones que EF Core traduce a SQL al materializar.
- La ejecución es diferida hasta `ToListAsync`, `FirstOrDefaultAsync` o un `foreach`.
- `Select` reduce columnas y mejora el rendimiento.
- `Skip` y `Take` paginan, siempre con un orden estable.
- `EF.Functions.Like` permite búsquedas parciales traducidas y parametrizadas.

## Siguiente paso

Las consultas ya devuelven los datos correctos, pero las navegaciones todavía pueden quedar sin cargar. En la próxima lección vas a resolver eso con [Include y proyecciones](/modulos/ef-core/carga-navegacion).
