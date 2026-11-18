---
slug: migraciones
title: "Migraciones"
order: 4
estimatedMinutes: 15
summary: "Versioná el esquema de PostgreSQL con migraciones, aplicalas en orden y revertí cambios sin perder el control del modelo."
quiz:
  - question: "¿Qué comando crea una migración nueva?"
    options:
      - "dotnet ef database update"
      - "dotnet ef migrations add Inicial"
      - "dotnet ef migrations remove"
      - "dotnet ef dbcontext script"
    answer: 1
    explanation: "migrations add genera los archivos con las operaciones Up y Down a partir de la diferencia entre el modelo y la última migración."
  - question: "¿Cuál es la forma recomendada de aplicar migraciones en producción?"
    options:
      - "Ejecutar Database.EnsureCreated() al iniciar"
      - "Generar un script SQL idempotente y revisarlo antes de ejecutarlo"
      - "Borrar la base y volver a crearla"
      - "Copiar los archivos .cs a la base de datos"
    answer: 1
    explanation: "Un script permite revisar, versionar y ejecutar los cambios con los permisos controlados de producción, sin que la aplicación modifique el esquema."
  - question: "¿Qué hace Database.Migrate() al iniciar la aplicación?"
    options:
      - "Crea un script para revisar"
      - "Aplica las migraciones pendientes automáticamente"
      - "Elimina las migraciones aplicadas"
      - "Solo valida que el modelo compile"
    answer: 1
    explanation: "Migrate aplica las migraciones pendientes, pero hacerlo al arrancar puede ser riesgoso si hay varias instancias o cambios destructivos."
  - question: "¿Cuándo se puede usar dotnet ef migrations remove?"
    options:
      - "Siempre, incluso después de aplicar la migración"
      - "Solo si la migración todavía no se aplicó a la base"
      - "Solo en producción"
      - "Únicamente con EnsureCreated"
    answer: 1
    explanation: "Remove borra la última migración del proyecto; si ya se aplicó, el esquema y el historial quedan desincronizados."
---

# Migraciones

## Objetivo

Crear, aplicar y revertir migraciones de EF Core, entender qué archivos genera y cómo llevar cambios de esquema a producción de forma segura.

## Requisitos previos

Necesitás el modelo de las lecciones anteriores y PostgreSQL accesible. También necesitás el SDK de .NET 10 y permisos para instalar herramientas globales.

## Concepto

Una migración es un archivo de código que describe un cambio de esquema: crear una tabla, agregar una columna, cambiar un índice. EF Core compara el modelo actual con la última migración y genera dos métodos: `Up`, que aplica el cambio, y `Down`, que lo revierte.

Las migraciones se guardan en el proyecto, generalmente en una carpeta `Migrations`, y se aplican en orden. La tabla `__EFMigrationsHistory` registra cuáles ya se ejecutaron. Sin esa tabla, EF Core no sabe qué falta aplicar.

El flujo básico es:

1. Cambiar entidades o configuraciones.
2. Ejecutar `dotnet ef migrations add NombreDescriptivo`.
3. Revisar el código generado, sobre todo si hay pérdida de datos.
4. Aplicar con `dotnet ef database update` en desarrollo.
5. Generar un script para producción.

`EnsureCreated()` crea la base y el esquema sin migraciones. Sirve para prototipos descartables, pero no para proyectos reales: no permite evolucionar el esquema ni convive con migraciones.

## Ejemplo mínimo

Instalá la herramienta y el paquete de diseño:

```text
dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Si el proyecto es una consola sin host de dependencias, EF Core necesita una fábrica en tiempo de diseño. La cadena de conexión se lee de configuración, igual que en la aplicación:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public sealed class CatalogoDbContextFactory : IDesignTimeDbContextFactory<CatalogoDbContext>
{
    public CatalogoDbContext CreateDbContext(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        string connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Default.");

        var options = new DbContextOptionsBuilder<CatalogoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CatalogoDbContext(options);
    }
}
```

Los comandos principales:

```text
dotnet ef migrations add Inicial
dotnet ef database update
dotnet ef migrations list
```

```text
Build started...
Build succeeded.
Applying migration '20260115103000_Inicial'.
Done.
```

La migración `Inicial` contiene un `CreateTable` por entidad, sus claves e índices. El nombre del archivo incluye un timestamp para mantener el orden.

## Ejemplo aplicado

Agregás una propiedad `Stock` a `Producto` y generás la segunda migración:

```csharp
public sealed class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}
```

```text
dotnet ef migrations add AgregarStock
dotnet ef database update
```

El método `Up` generado se parece a esto:

```csharp
migrationBuilder.AddColumn<int>(
    name: "Stock",
    table: "productos",
    type: "integer",
    nullable: false,
    defaultValue: 0);
```

`defaultValue: 0` permite agregar la columna obligatoria a filas existentes. Si el dato correcto no es cero, conviene editar la migración para completar los valores antes de hacerla obligatoria.

Para volver atrás, aplicás la migración anterior por nombre:

```text
dotnet ef database update Inicial
```

Eso ejecuta los `Down` de las migraciones posteriores. En producción, en cambio, generá un script y revisalo:

```text
dotnet ef migrations script Inicial AgregarStock --idempotent --output migracion.sql
```

```text
Build started...
Build succeeded.
```

El script incluye comprobaciones para no repetir migraciones ya aplicadas. Así podés ejecutarlo con `psql` o desde el pipeline de despliegue.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `dotnet ef` no se reconoce | La herramienta global no está en el PATH | Reinstalar con `dotnet tool install --global dotnet-ef` |
| `Your startup project doesn't reference Microsoft.EntityFrameworkCore.Design` | Falta el paquete de diseño | Agregarlo al proyecto que contiene el `DbContext` |
| `Unable to create a DbContext` | No hay fábrica ni host que construya el contexto | Implementar `IDesignTimeDbContextFactory<T>` |
| `Pending model changes` | Se modificó el modelo sin crear migración | Ejecutar `dotnet ef migrations add` |
| `relation already exists` | La base tiene tablas creadas fuera de las migraciones | Revisar el esquema y alinear el historial |
| Cambios aplicados a medias | Se editó una migración ya ejecutada | Crear una migración nueva en lugar de modificar la anterior |

## Buenas prácticas

- Usá nombres descriptivos: `AgregarStock`, `CrearTablaPrestamos`, `IndiceSkuUnico`.
- Revisá siempre el código generado antes de aplicarlo; una migración puede borrar columnas.
- Versioná la carpeta `Migrations` junto con el código, sin excluirla.
- En producción, aplicá scripts revisados y no `Database.Migrate()` en cada arranque.
- Hacé copias de seguridad antes de migraciones destructivas y probalas en un entorno similar.

## Ejercicio

Agregá una entidad `Editorial` con `Id`, `Nombre` y `Pais`, relacionada con `Libro` mediante `EditorialId`. Creá la migración `AgregarEditoriales`, aplicala, listá las migraciones y generá un script idempotente. Después volvé a la migración anterior y explicá qué pasa con la tabla.

<details>
<summary>Ver solución</summary>

```csharp
public sealed class Editorial
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public List<Libro> Libros { get; set; } = [];
}

public sealed class EditorialConfiguracion : IEntityTypeConfiguration<Editorial>
{
    public void Configure(EntityTypeBuilder<Editorial> builder)
    {
        builder.ToTable("editoriales");
        builder.Property(e => e.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Pais).HasMaxLength(60).IsRequired();
    }
}
```

```text
dotnet ef migrations add AgregarEditoriales
dotnet ef database update
dotnet ef migrations list
dotnet ef migrations script --idempotent --output editoriales.sql
```

```text
20260115103000_Inicial
20260116120000_AgregarStock
20260117101500_AgregarEditoriales
```

Al ejecutar `dotnet ef database update AgregarStock`, el `Down` de `AgregarEditoriales` elimina la tabla `editoriales` y la columna `EditorialId`. Es una operación destructiva: los datos de editoriales se pierden. Por eso conviene probar el rollback en un entorno de desarrollo antes de necesitarlo en producción.

</details>

## Resumen

- Una migración versiona un cambio de esquema con métodos `Up` y `Down`.
- `migrations add` genera el archivo; `database update` lo aplica.
- La tabla `__EFMigrationsHistory` controla qué migraciones ya se ejecutaron.
- En producción se usan scripts revisados, no ejecución automática al iniciar.
- `EnsureCreated` no sirve para proyectos que van a evolucionar.

## Siguiente paso

Con el esquema bajo control ya podés consultar datos reales. En la próxima lección vas a escribir [consultas con LINQ](/modulos/ef-core/consultas-linq) que EF Core traduce a SQL.
