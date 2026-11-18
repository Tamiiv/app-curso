---
slug: configuracion-mapeo
title: "Configuración y mapeo"
order: 2
estimatedMinutes: 15
summary: "Controlá nombres, claves, índices, precisión y conversiones con Fluent API para que el modelo y PostgreSQL hablen el mismo idioma."
quiz:
  - question: "¿Dónde se aplica la configuración del modelo con Fluent API?"
    options:
      - "En el constructor del DbContext"
      - "En el método OnModelCreating del DbContext"
      - "En cada propiedad de la entidad"
      - "En el archivo appsettings.json"
    answer: 1
    explanation: "OnModelCreating recibe el ModelBuilder y es el lugar donde se define el mapeo antes de que EF Core construya el modelo."
  - question: "¿Cómo se registran automáticamente las clases que implementan IEntityTypeConfiguration<T>?"
    options:
      - "Con modelBuilder.ApplyConfigurationsFromAssembly(...)"
      - "Con db.Database.Migrate()"
      - "Agregando un atributo a cada clase"
      - "Llamando a SaveChanges dos veces"
    answer: 0
    explanation: "ApplyConfigurationsFromAssembly busca en el ensamblado las configuraciones y las aplica sin enumerarlas una por una."
  - question: "¿Qué configuración impide dos productos con el mismo SKU?"
    options:
      - "builder.Property(p => p.Sku).IsRequired()"
      - "builder.HasIndex(p => p.Sku).IsUnique()"
      - "builder.HasKey(p => p.Sku)"
      - "builder.ToTable(\"productos\")"
    answer: 1
    explanation: "Un índice único delega la restricción a PostgreSQL y además acelera las búsquedas por esa columna."
  - question: "¿Qué efecto tiene HasConversion<string>() sobre una propiedad enum?"
    options:
      - "Convierte el enum en una tabla relacionada"
      - "Guarda el nombre del valor como texto en la columna"
      - "Elimina el enum del modelo"
      - "Obliga a usar un entero autoincremental"
    answer: 1
    explanation: "La conversión guarda el nombre legible del enum como texto; es útil para revisar datos, aunque ocupa más espacio que un entero."
---

# Configuración y mapeo

## Objetivo

Configurar el modelo de EF Core con Fluent API: nombres de tablas y columnas, claves, índices, longitudes, precisión decimal y conversiones de tipos.

## Requisitos previos

Necesitás una entidad y un `DbContext` funcionando, como los de la lección anterior. También conviene tener PostgreSQL disponible para probar el SQL generado con `ToQueryString()`.

## Concepto

EF Core infiere mucho por convención: `Id` es la clave, el nombre de la propiedad es el de la columna y las relaciones se deducen de las propiedades de navegación. Cuando la base existente usa otros nombres o necesitás reglas explícitas, configurás el modelo.

Hay dos mecanismos: data annotations y Fluent API. Las anotaciones son cómodas para reglas simples, pero ensucian la entidad y no cubren todo. Fluent API gana en expresividad y mantiene las entidades limpias. En proyectos con varias entidades conviene una clase `IEntityTypeConfiguration<T>` por entidad y registrarlas todas con `ApplyConfigurationsFromAssembly`.

La configuración no cambia la base por sí sola. Modificar el modelo y ejecutar la aplicación no altera las tablas: hace falta una migración, tema de la lección 4. Si la base ya existe, compará el modelo con el esquema real antes de generar cambios.

Puntos que conviene configurar siempre:

| Regla | Método |
|---|---|
| Nombre de tabla | `ToTable("productos")` |
| Clave primaria | `HasKey(p => p.Id)` |
| Longitud máxima | `Property(p => p.Nombre).HasMaxLength(120)` |
| Obligatoriedad | `Property(p => p.Nombre).IsRequired()` |
| Precisión decimal | `Property(p => p.Precio).HasPrecision(12, 2)` |
| Índice único | `HasIndex(p => p.Sku).IsUnique()` |
| Conversión de enum | `Property(p => p.Estado).HasConversion<string>()` |

## Ejemplo mínimo

Una configuración aislada para `Producto`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public enum EstadoProducto
{
    Borrador,
    Activo,
    Descontinuado
}

public sealed class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public EstadoProducto Estado { get; set; }
    public DateTime CreadoEn { get; set; }
}

public sealed class ProductoConfiguracion : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("productos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(p => p.Sku)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Precio).HasPrecision(12, 2);

        builder.Property(p => p.Estado)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.CreadoEn).HasDefaultValueSql("now()");
    }
}
```

El contexto aplica todas las configuraciones del ensamblado:

```csharp
public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options)
    : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogoDbContext).Assembly);
    }
}
```

`HasDefaultValueSql("now()")` hace que PostgreSQL complete la fecha si no la enviás. La conversión del enum a texto guarda `"Activo"` en lugar de `1`, lo que facilita leer la tabla directamente.

## Ejemplo aplicado

Con dos configuraciones y una consulta que muestra el SQL real:

```csharp
var producto = new Producto
{
    Nombre = "Monitor 27 pulgadas",
    Sku = "MON-27",
    Precio = 350000m,
    Estado = EstadoProducto.Activo
};

db.Productos.Add(producto);
await db.SaveChangesAsync();

var consulta = db.Productos
    .Where(p => p.Estado == EstadoProducto.Activo && p.Precio > 100000m)
    .OrderBy(p => p.Nombre);

Console.WriteLine(consulta.ToQueryString());

foreach (Producto p in await consulta.ToListAsync())
{
    Console.WriteLine($"{p.Sku} - {p.Nombre} - {p.Precio}");
}
```

```text
SELECT p."Id", p."CreadoEn", p."Estado", p."Nombre", p."Precio", p."Sku"
FROM productos AS p
WHERE p."Estado" = 'Activo' AND p."Precio" > 100000.0
ORDER BY p."Nombre"

MON-27 - Monitor 27 pulgadas - 350000.00
```

El SQL confirma dos decisiones del mapeo: la tabla se llama `productos` y el enum viaja como texto. `ToQueryString()` no ejecuta la consulta; es una herramienta de diagnóstico ideal para revisar traducciones dudosas.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `duplicate key value violates unique constraint` | El índice único está funcionando y se repitió el valor | Validar antes de insertar o capturar la excepción |
| La columna se creó como `text` sin límite | Se configuró `HasMaxLength` después de crear la tabla | Generar y aplicar una migración |
| `numeric field overflow` | Precisión insuficiente para el valor | Ajustar `HasPrecision(12, 2)` al rango real |
| El enum se guarda como número | No se configuró la conversión | Usar `HasConversion<string>()` y migrar |
| La configuración no se aplica | La clase no implementa `IEntityTypeConfiguration<T>` o no se registró | Revisar la interfaz y `ApplyConfigurationsFromAssembly` |
| `null value in column "Nombre"` | La propiedad es obligatoria y se envió null | Inicializar la propiedad o validar antes de guardar |

## Buenas prácticas

- Usá una clase de configuración por entidad y mantené `OnModelCreating` corto.
- Configurá longitudes máximas: `text` sin límite impide ciertos índices y oculta datos inválidos.
- Poné precisión explícita en todos los `decimal`; el valor por defecto rara vez coincide con el negocio.
- Nombrá tablas y columnas en el idioma y estilo de la base, no en función de las clases.
- Revisá el SQL con `ToQueryString()` antes de dar por cerrada una configuración.

## Ejercicio

Configurá una entidad `Curso` con `Id`, `Codigo`, `Titulo`, `Precio`, `Modalidad` (enum `Presencial`, `Virtual`, `Mixta`) y `FechaInicio`. El código debe ser único y de hasta 10 caracteres, el título obligatorio de hasta 150, el precio con precisión 10,2, la modalidad guardada como texto y la fecha con un índice. Mostrá el SQL de una consulta por modalidad.

<details>
<summary>Ver solución</summary>

```csharp
public enum Modalidad
{
    Presencial,
    Virtual,
    Mixta
}

public sealed class Curso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public Modalidad Modalidad { get; set; }
    public DateOnly FechaInicio { get; set; }
}

public sealed class CursoConfiguracion : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> builder)
    {
        builder.ToTable("cursos");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Codigo).HasMaxLength(10).IsRequired();
        builder.HasIndex(c => c.Codigo).IsUnique();

        builder.Property(c => c.Titulo).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Precio).HasPrecision(10, 2);
        builder.Property(c => c.Modalidad).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(c => c.FechaInicio);
    }
}

var consulta = db.Cursos
    .Where(c => c.Modalidad == Modalidad.Virtual)
    .OrderBy(c => c.FechaInicio);

Console.WriteLine(consulta.ToQueryString());
```

```text
SELECT c."Id", c."Codigo", c."FechaInicio", c."Modalidad", c."Precio", c."Titulo"
FROM cursos AS c
WHERE c."Modalidad" = 'Virtual'
ORDER BY c."FechaInicio"
```

</details>

## Resumen

- Fluent API define el mapeo sin ensuciar las entidades.
- `IEntityTypeConfiguration<T>` agrupa reglas por entidad y se registra por ensamblado.
- Índices, longitudes y precisión protegen la calidad de los datos.
- Las conversiones permiten guardar enums como texto de forma legible.
- Todo cambio de modelo necesita una migración para llegar a la base.

## Siguiente paso

Con una entidad bien mapeada ya podés modelar un dominio completo. En la próxima lección vas a conectar entidades con [relaciones](/modulos/ef-core/relaciones), incluidas las de uno a muchos, uno a uno y muchos a muchos.
