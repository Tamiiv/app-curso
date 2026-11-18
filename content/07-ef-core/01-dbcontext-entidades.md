---
slug: dbcontext-entidades
title: "DbContext y entidades"
order: 1
estimatedMinutes: 15
summary: "Conocé el DbContext, las entidades POCO y el ciclo de vida que conecta tus objetos de C# con las tablas de PostgreSQL."
quiz:
  - question: "¿De dónde conviene leer la cadena de conexión?"
    options:
      - "De una constante en Program.cs"
      - "De appsettings.json o de variables de entorno"
      - "De OnModelCreating"
      - "Del archivo de migración"
    answer: 1
    explanation: "La cadena de conexión es configuración, no código: se lee de appsettings.json y se puede sobrescribir con variables de entorno o user secrets."
  - question: "¿Qué hace SaveChangesAsync en un DbContext?"
    options:
      - "Crea la base de datos si no existe"
      - "Ejecuta en una transacción los INSERT, UPDATE y DELETE pendientes"
      - "Abre una conexión permanente hasta que se cierre el programa"
      - "Genera una migración con los cambios del modelo"
    answer: 1
    explanation: "SaveChangesAsync analiza el ChangeTracker y envía los comandos pendientes dentro de una transacción; si algo falla, no se aplica ninguno."
  - question: "¿Cómo se debe usar un DbContext en una aplicación?"
    options:
      - "Como singleton compartido por toda la aplicación"
      - "Una instancia por entidad"
      - "Una instancia por unidad de trabajo, sin usarla en paralelo"
      - "Una instancia por cada consulta SQL generada"
    answer: 2
    explanation: "El DbContext no es thread-safe. Se crea una instancia por unidad de trabajo, por ejemplo un request o una operación de consola, y se descarta al terminar."
---

# DbContext y entidades

## Objetivo

Entender qué es un `DbContext`, cómo se declaran las entidades y cómo se guardan los cambios en PostgreSQL sin mezclar la configuración con el código.

## Requisitos previos

Venís del [ejercicio integrador de APIs](/modulos/aspnet-core-apis/ejercicio-integrador), así que ya sabés inyectar dependencias y leer configuración. También necesitás PostgreSQL corriendo en tu máquina y un proyecto de consola con .NET 10.

## Concepto

`DbContext` es la pieza central de EF Core. Representa una sesión con la base de datos y cumple dos roles: es una unidad de trabajo que acumula cambios y es la puerta de entrada a las consultas. Cada `DbSet<T>` declara un conjunto de entidades que se mapea a una tabla.

Las entidades son clases comunes, llamadas POCO, sin herencia obligatoria ni atributos especiales. Una propiedad `Id` o `ProductoId` se reconoce como clave por convención. El contexto descubre las entidades cuando las exponés en un `DbSet<T>` o cuando aparecen en una relación.

El `ChangeTracker` observa las instancias que cargás o agregás y registra su estado:

| Estado | Significado |
|---|---|
| `Added` | Se va a insertar en el próximo `SaveChanges` |
| `Modified` | Se va a actualizar |
| `Deleted` | Se va a borrar |
| `Unchanged` | Se cargó desde la base y no cambió |
| `Detached` | No está siendo seguida por el contexto |

El contexto abre y cierra conexiones según la operación; no mantiene una conexión abierta durante toda la vida de la aplicación. Tampoco es thread-safe: dos consultas simultáneas sobre la misma instancia producen `InvalidOperationException`.

La cadena de conexión va en configuración. En desarrollo podés usar `appsettings.json` o user secrets; en producción, variables de entorno como `ConnectionStrings__Default`. Nunca la escribas como constante en el código.

## Ejemplo mínimo

Instalá los paquetes y creá un `appsettings.json` con la conexión local:

```text
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
```

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=curso_ef;Username=postgres;Password=postgres"
  }
}
```

El siguiente programa registra un producto y lo vuelve a leer:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

string connectionString = configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:Default.");

var options = new DbContextOptionsBuilder<CatalogoDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new CatalogoDbContext(options);

db.Productos.Add(new Producto { Nombre = "Mouse", Precio = 12000m });
await db.SaveChangesAsync();

foreach (Producto producto in await db.Productos.OrderBy(p => p.Nombre).ToListAsync())
{
    Console.WriteLine($"{producto.Id}: {producto.Nombre}");
}

public sealed class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
}

public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options)
    : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();
}
```

```text
1: Mouse
```

La propiedad `Productos` usa `Set<Producto>()` para evitar un campo nulo. `Id` se detecta como clave y, como es `int`, PostgreSQL la genera con `GENERATED BY DEFAULT AS IDENTITY`.

## Ejemplo aplicado

En una operación real conviene mirar el estado de las entidades para entender qué está haciendo EF Core. Este ejemplo agrega, modifica y elimina un producto:

```csharp
await using var db = new CatalogoDbContext(options);

var teclado = new Producto { Nombre = "Teclado mecánico", Precio = 85000m };
db.Productos.Add(teclado);
Console.WriteLine($"Antes: Id={teclado.Id}, estado={db.Entry(teclado).State}");

await db.SaveChangesAsync();
Console.WriteLine($"Después: Id={teclado.Id}, estado={db.Entry(teclado).State}");

Producto? encontrado = await db.Productos.FindAsync(teclado.Id);
if (encontrado is null)
{
    Console.WriteLine("No encontrado");
    return;
}

encontrado.Precio = 79000m;
Console.WriteLine($"Modificado: estado={db.Entry(encontrado).State}");
await db.SaveChangesAsync();

db.Productos.Remove(encontrado);
await db.SaveChangesAsync();
Console.WriteLine($"Productos restantes: {await db.Productos.CountAsync()}");
```

```text
Antes: Id=0, estado=Added
Después: Id=2, estado=Unchanged
Modificado: estado=Modified
Productos restantes: 1
```

`FindAsync` primero busca en el `ChangeTracker` y solo consulta la base si no la tiene. Después de `SaveChangesAsync`, la clave generada se escribe en la entidad y el estado vuelve a `Unchanged`.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `Npgsql.NpgsqlException: Connection refused` | PostgreSQL no está iniciado o el puerto no coincide | Verificar el servicio y el `Port` de la cadena |
| `InvalidOperationException: Connection string is not set` | La clave de configuración no existe o tiene otro nombre | Revisar `ConnectionStrings:Default` y las variables de entorno |
| Los cambios no aparecen en la base | Se modificó la entidad pero no se llamó a `SaveChanges` | Guardar antes de cerrar el contexto |
| `A second operation was started on this context` | Dos consultas concurrentes sobre el mismo contexto | Crear un contexto por operación o esperar cada `await` |
| `relation "Productos" does not exist` | El modelo existe pero la tabla no se creó | Aplicar migraciones, tema de la lección 4 |
| `password authentication failed` | Usuario o contraseña incorrectos | Corregir la configuración, no el código |

## Buenas prácticas

- Registrá el contexto como `Scoped` en aplicaciones web y creá uno nuevo por operación en consola.
- Mantené las entidades simples: sin lógica de acceso a datos ni dependencias del contexto.
- Leé la cadena de conexión desde configuración y usá user secrets o variables de entorno para credenciales.
- Descartá el contexto con `await using` para liberar conexiones y recursos.
- No compartas una instancia entre tareas paralelas; EF Core no es thread-safe.

## Ejercicio

Creá un `BibliotecaDbContext` con una entidad `Libro` que tenga `Id`, `Titulo`, `Autor` y `Copias`. Insertá dos libros, listalos ordenados por título, actualizá las copias de uno y eliminá el otro. Mostrá el estado de cada entidad antes y después de guardar.

<details>
<summary>Ver solución</summary>

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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

await using var db = new BibliotecaDbContext(options);

var libros = new[]
{
    new Libro { Titulo = "Cien años de soledad", Autor = "García Márquez", Copias = 3 },
    new Libro { Titulo = "El Aleph", Autor = "Borges", Copias = 2 }
};

db.Libros.AddRange(libros);
Console.WriteLine($"Estados: {string.Join(", ", libros.Select(l => db.Entry(l).State))}");
await db.SaveChangesAsync();
Console.WriteLine($"Estados: {string.Join(", ", libros.Select(l => db.Entry(l).State))}");

foreach (Libro libro in await db.Libros.OrderBy(l => l.Titulo).ToListAsync())
{
    Console.WriteLine($"{libro.Titulo} ({libro.Autor}) - copias: {libro.Copias}");
}

Libro primero = libros[0];
primero.Copias = 5;
await db.SaveChangesAsync();

db.Libros.Remove(libros[1]);
await db.SaveChangesAsync();

Console.WriteLine($"Quedan: {await db.Libros.CountAsync()}");
```

```text
Estados: Added, Added
Estados: Unchanged, Unchanged
Cien años de soledad (García Márquez) - copias: 3
El Aleph (Borges) - copias: 2
Quedan: 1
```

</details>

## Resumen

- `DbContext` es la unidad de trabajo y el punto de acceso a la base.
- Cada `DbSet<T>` representa un conjunto de entidades mapeadas a una tabla.
- El `ChangeTracker` decide qué INSERT, UPDATE o DELETE enviar al guardar.
- La cadena de conexión se lee de configuración y nunca se escribe en el código.
- El contexto se crea por unidad de trabajo y no se comparte entre tareas.

## Siguiente paso

Ya sabés abrir el contexto y guardar cambios con las convenciones por defecto. En la próxima lección vas a tomar control del modelo con [configuración y mapeo](/modulos/ef-core/configuracion-mapeo): nombres de tablas, índices, precisión y conversiones.
