---
slug: ejercicio-integrador
title: "Ejercicio integrador"
order: 9
estimatedMinutes: 25
summary: "Combiná modelo, migraciones, consultas, proyecciones, operaciones masivas y transacciones en un sistema de préstamos de biblioteca."
quiz:
  - question: "Para un endpoint que solo lista libros con su autor, ¿qué combinación conviene?"
    options:
      - "Tracking normal e Include de todos los préstamos"
      - "AsNoTracking y una proyección con los campos necesarios"
      - "ExecuteUpdate y luego ToList"
      - "EnsureCreated en cada request"
    answer: 1
    explanation: "Una lectura de solo lectura no necesita tracking, y la proyección evita traer columnas y relaciones que no se muestran."
  - question: "¿Qué ocurre si una operación masiva y una inserción deben ser atómicas?"
    options:
      - "SaveChanges las agrupa automáticamente"
      - "Hay que envolverlas en una transacción explícita"
      - "ExecuteUpdate crea la transacción"
      - "No se pueden combinar"
    answer: 1
    explanation: "Cada operación masiva es independiente; solo una transacción explícita garantiza que ambas se confirmen o se reviertan juntas."
  - question: "Si al guardar un préstamo se lanza DbUpdateConcurrencyException, ¿qué conviene hacer?"
    options:
      - "Ignorar la excepción y continuar"
      - "Reintentar sin recargar la entidad"
      - "Recargar, evaluar el stock actual y reintentar o informar"
      - "Borrar la base y volver a crearla"
    answer: 2
    explanation: "Recargar trae el estado real de la fila; después decidís si el préstamo todavía es posible o si hay que avisar al usuario."
  - question: "¿Cuál es el orden correcto para llevar el modelo a PostgreSQL?"
    options:
      - "database update, migrations add, cambiar entidades"
      - "cambiar entidades, migrations add, revisar, database update"
      - "migrations remove, EnsureCreated, migrations add"
      - "solo EnsureCreated al iniciar"
    answer: 1
    explanation: "Primero se modifica el modelo, luego se genera la migración, se revisa el código y finalmente se aplica al esquema."
---

# Ejercicio integrador

## Objetivo

Construir un sistema de préstamos de biblioteca con EF Core y PostgreSQL, aplicando modelo, mapeo, migraciones, consultas eficientes, operaciones masivas y transacciones.

## Requisitos previos

Necesitás haber completado las lecciones del módulo y tener PostgreSQL disponible. El proyecto puede ser de consola; no hace falta una API para practicar el acceso a datos.

## Concepto

El caso tiene tres entidades. Un `Autor` escribe muchos `Libro`. Un `Libro` recibe muchos `Prestamo`. Cada libro controla sus copias disponibles y usa un token de concurrencia para evitar que dos préstamos simultáneos dejen el stock en negativo.

Vas a resolver cinco operaciones:

1. Definir entidades y configuraciones con claves, longitudes, índices y relaciones.
2. Generar y aplicar la migración inicial.
3. Cargar datos de prueba.
4. Consultar el catálogo con proyección y sin tracking.
5. Registrar un préstamo dentro de una transacción, descontando una copia.

La regla de negocio es simple: no se presta un libro sin copias disponibles. Si el stock cambia entre la lectura y la escritura, el token de concurrencia lo detecta y el préstamo se revierte.

## Ejemplo mínimo

El proyecto necesita los paquetes de EF Core, el proveedor de PostgreSQL y configuración:

```text
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
```

La conexión se lee de `appsettings.json` o de `ConnectionStrings__Default`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=biblioteca_ef;Username=postgres;Password=postgres"
  }
}
```

```csharp
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

string connectionString = configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:Default.");

var options = new DbContextOptionsBuilder<BibliotecaDbContext>()
    .UseNpgsql(connectionString)
    .Options;
```

## Ejemplo aplicado

Las entidades y sus configuraciones:

```csharp
public sealed class Autor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<Libro> Libros { get; set; } = [];
}

public sealed class Libro
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int Copias { get; set; }
    public int Version { get; set; }
    public int AutorId { get; set; }
    public Autor Autor { get; set; } = null!;
    public List<Prestamo> Prestamos { get; set; } = [];
}

public sealed class Prestamo
{
    public int Id { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public bool Devuelto { get; set; }
    public int LibroId { get; set; }
    public Libro Libro { get; set; } = null!;
}

public sealed class LibroConfiguracion : IEntityTypeConfiguration<Libro>
{
    public void Configure(EntityTypeBuilder<Libro> builder)
    {
        builder.ToTable("libros");
        builder.Property(l => l.Titulo).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Version).IsConcurrencyToken();
        builder.HasIndex(l => l.Titulo);

        builder.HasOne(l => l.Autor)
            .WithMany(a => a.Libros)
            .HasForeignKey(l => l.AutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.Prestamos)
            .WithOne(p => p.Libro)
            .HasForeignKey(p => p.LibroId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BibliotecaDbContext(DbContextOptions<BibliotecaDbContext> options)
    : DbContext(options)
{
    public DbSet<Autor> Autores => Set<Autor>();
    public DbSet<Libro> Libros => Set<Libro>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BibliotecaDbContext).Assembly);
    }
}
```

Migración y datos iniciales:

```text
dotnet ef migrations add Inicial
dotnet ef database update
```

```csharp
var autor = new Autor
{
    Nombre = "Jorge Luis Borges",
    Libros =
    [
        new Libro { Titulo = "El Aleph", Copias = 2 },
        new Libro { Titulo = "Ficciones", Copias = 1 }
    ]
};

db.Autores.Add(autor);
await db.SaveChangesAsync();
```

La consulta de catálogo usa proyección y `AsNoTracking` porque solo muestra datos:

```csharp
var catalogo = await db.Libros
    .AsNoTracking()
    .OrderBy(l => l.Titulo)
    .Select(l => new
    {
        l.Titulo,
        Autor = l.Autor.Nombre,
        l.Copias,
        Activos = l.Prestamos.Count(p => !p.Devuelto)
    })
    .ToListAsync();

foreach (var libro in catalogo)
{
    Console.WriteLine($"{libro.Titulo} - {libro.Autor} - copias: {libro.Copias} - activos: {libro.Activos}");
}
```

```text
El Aleph - Jorge Luis Borges - copias: 2 - activos: 0
Ficciones - Jorge Luis Borges - copias: 1 - activos: 0
```

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El stock queda negativo | Se validó y descontó sin transacción ni token | Usar transacción y control de concurrencia |
| `DbUpdateConcurrencyException` sin manejar | Dos préstamos simultáneos sobre la misma fila | Capturar, recargar y reintentar o informar |
| La consulta del catálogo trae todo el grafo | Se usó `Include` en un listado | Proyectar solo los campos necesarios |
| `relation "libros" does not exist` | No se aplicó la migración | Ejecutar `dotnet ef database update` |
| `duplicate key` al cargar datos | Se ejecutó el seed dos veces | Verificar si ya existen datos antes de insertar |
| El préstamo se guarda pero el stock no baja | Se usaron operaciones separadas sin transacción | Unificar en una transacción explícita |

## Buenas prácticas

- Definí la regla de negocio en un solo lugar y protegela con transacción y concurrencia.
- Proyectá las lecturas de listado y usá `AsNoTracking`.
- Revisá las migraciones antes de aplicarlas y mantenelas en el repositorio.
- Medí el SQL de las consultas importantes con `ToQueryString()`.
- Escribí pruebas de integración contra una base real o con contenedores, tema del próximo módulo.

## Ejercicio

Implementá el alta de un préstamo para un libro con copias disponibles. La operación debe verificar el stock, descontar una copia, incrementar `Version`, insertar el `Prestamo` y confirmar todo en una transacción. Si hay conflicto de concurrencia, revertir y mostrar el stock real. Al final, listá los préstamos activos con título y autor.

<details>
<summary>Ver solución</summary>

```csharp
int libroId = 1;

var strategy = db.Database.CreateExecutionStrategy();

await strategy.ExecuteAsync(async () =>
{
    await using var transaccion = await db.Database.BeginTransactionAsync();

    var libro = await db.Libros.SingleAsync(l => l.Id == libroId);

    if (libro.Copias <= 0)
    {
        await transaccion.RollbackAsync();
        Console.WriteLine($"Sin copias de {libro.Titulo}.");
        return;
    }

    libro.Copias--;
    libro.Version++;

    db.Prestamos.Add(new Prestamo
    {
        LibroId = libro.Id,
        FechaPrestamo = DateTime.UtcNow,
        Devuelto = false
    });

    try
    {
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        Console.WriteLine($"Préstamo registrado. Copias restantes: {libro.Copias}");
    }
    catch (DbUpdateConcurrencyException ex)
    {
        await transaccion.RollbackAsync();
        var entrada = ex.Entries.Single();
        await entrada.ReloadAsync();
        Console.WriteLine($"Conflicto: el stock real es {((Libro)entrada.Entity).Copias}.");
    }
});

var activos = await db.Prestamos
    .AsNoTracking()
    .Where(p => !p.Devuelto)
    .OrderBy(p => p.FechaPrestamo)
    .Select(p => new
    {
        p.FechaPrestamo,
        Libro = p.Libro.Titulo,
        Autor = p.Libro.Autor.Nombre
    })
    .ToListAsync();

foreach (var prestamo in activos)
{
    Console.WriteLine($"{prestamo.FechaPrestamo:yyyy-MM-dd}: {prestamo.Libro} - {prestamo.Autor}");
}
```

```text
Préstamo registrado. Copias restantes: 1
2026-11-15: El Aleph - Jorge Luis Borges
```

Si el `UPDATE` afecta cero filas por el token, se revierte la transacción completa: no queda ni el préstamo ni el descuento. La consulta final no usa tracking porque solo muestra información.

</details>

## Resumen

- Un módulo completo combina modelo, mapeo, migraciones y consultas.
- Las lecturas se optimizan con proyecciones y `AsNoTracking`.
- Las reglas de negocio que tocan varias filas necesitan transacciones.
- Los tokens de concurrencia evitan pisar cambios simultáneos.
- Medir y revisar el SQL cierra el ciclo de un acceso a datos confiable.

## Siguiente paso

Con EF Core bajo control, el siguiente desafío es demostrar que todo funciona y que va a seguir funcionando. En el módulo de [testing](/modulos/testing) vas a aprender a escribir pruebas unitarias y de integración para tu capa de datos.
