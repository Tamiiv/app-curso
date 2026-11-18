---
slug: relaciones
title: "Relaciones entre entidades"
order: 3
estimatedMinutes: 18
summary: "Modelá uno a muchos, uno a uno y muchos a muchos con navegaciones, claves foráneas y la política de borrado adecuada."
quiz:
  - question: "¿Qué par de métodos configura una relación de uno a muchos?"
    options:
      - "HasOne y WithMany"
      - "HasMany y WithOne"
      - "HasMany y WithMany"
      - "HasOne y WithOne"
    answer: 1
    explanation: "HasMany declara la colección del lado principal y WithOne el punto de retorno desde la entidad dependiente."
  - question: "En una relación uno a uno, ¿dónde se indica la clave foránea?"
    options:
      - "En el lado que no tiene la navegación"
      - "En HasForeignKey<TDependiente>(d => d.Clave)"
      - "Con HasIndex en ambas entidades"
      - "No se indica, EF Core siempre la inventa"
    answer: 1
    explanation: "HasForeignKey con el tipo de la entidad dependiente le dice a EF Core qué propiedad guarda la referencia única."
  - question: "¿Qué hace DeleteBehavior.Restrict?"
    options:
      - "Borra en cascada los dependientes"
      - "Impide borrar el principal si todavía tiene dependientes"
      - "Ignora los dependientes al consultar"
      - "Convierte la relación en opcional"
    answer: 1
    explanation: "Restrict protege la integridad: PostgreSQL rechaza el DELETE del principal mientras existan filas relacionadas."
  - question: "¿Cómo se puede modelar una relación muchos a muchos en EF Core?"
    options:
      - "Solo con una tabla intermedia escrita a mano en SQL"
      - "Con colecciones en ambos lados y una entidad de unión explícita o implícita"
      - "Con dos claves primarias en la misma entidad"
      - "No se puede modelar sin herencia"
    answer: 1
    explanation: "EF Core crea una tabla de unión cuando hay colecciones en ambos lados, y también permite controlarla con una entidad explícita."
---

# Relaciones entre entidades

## Objetivo

Modelar relaciones de uno a muchos, uno a uno y muchos a muchos, elegir correctamente la clave foránea y definir qué pasa al eliminar un registro principal.

## Requisitos previos

Necesitás entidades configuradas con Fluent API, como las de la lección anterior. También ayuda haber usado `Include` alguna vez, aunque acá vas a cargar grafos completos y en la lección 6 vas a profundizar las consultas.

## Concepto

Una relación une dos entidades mediante una clave foránea. La entidad que apunta se llama dependiente y la que recibe la referencia, principal. EF Core necesita dos cosas: la propiedad escalar con el valor de la clave y las propiedades de navegación que permiten ir de un objeto al otro.

En uno a muchos, el principal tiene una colección y el dependiente tiene una referencia. En uno a uno, ambos lados tienen una sola referencia y el dependiente lleva la clave foránea con un índice único. En muchos a muchos hay una tabla intermedia con dos claves foráneas.

| Relación | Principal | Dependiente | Configuración |
|---|---|---|---|
| Uno a muchos | `Cliente` | `Pedido` | `HasMany(...).WithOne(...)` |
| Uno a uno | `Cliente` | `Direccion` | `HasOne(...).WithOne(...)` |
| Muchos a muchos | `Producto` | `Etiqueta` | Colecciones en ambos lados |

Las claves foráneas pueden ser obligatorias o opcionales. Un `int ClienteId` no anulable implica que el pedido siempre tiene cliente. Un `int? ClienteId` permite pedidos sin cliente y cambia el comportamiento de borrado.

`DeleteBehavior` define qué hace la base al eliminar el principal:

| Valor | Comportamiento |
|---|---|
| `Cascade` | Borra los dependientes |
| `Restrict` | Rechaza el borrado si hay dependientes |
| `SetNull` | Pone la clave foránea en null |
| `NoAction` | Deja decidir a la base |

En PostgreSQL, `Restrict` es la opción más segura para datos comerciales: evita perder pedidos al borrar un cliente por error.

## Ejemplo mínimo

Un cliente con muchos pedidos y un pedido con un cliente:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<Pedido> Pedidos { get; set; } = [];
}

public sealed class Pedido
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
}

public sealed class ClienteConfiguracion : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.Property(c => c.Nombre).HasMaxLength(120).IsRequired();

        builder.HasMany(c => c.Pedidos)
            .WithOne(p => p.Cliente)
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

`Cliente` se inicializa con `null!` porque EF Core la completa al materializar la entidad. La colección se inicializa vacía para que puedas agregar pedidos antes de guardar.

## Ejemplo aplicado

Un grafo completo con uno a uno y muchos a muchos:

```csharp
public sealed class Direccion
{
    public int Id { get; set; }
    public string Calle { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
}

public sealed class Etiqueta
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<Producto> Productos { get; set; } = [];
}

public sealed class ProductoEtiqueta
{
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public int EtiquetaId { get; set; }
    public Etiqueta Etiqueta { get; set; } = null!;
}

public sealed class DireccionConfiguracion : IEntityTypeConfiguration<Direccion>
{
    public void Configure(EntityTypeBuilder<Direccion> builder)
    {
        builder.ToTable("direcciones");
        builder.Property(d => d.Calle).HasMaxLength(150).IsRequired();

        builder.HasOne(d => d.Cliente)
            .WithOne(c => c.Direccion)
            .HasForeignKey<Direccion>(d => d.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductoEtiquetaConfiguracion : IEntityTypeConfiguration<ProductoEtiqueta>
{
    public void Configure(EntityTypeBuilder<ProductoEtiqueta> builder)
    {
        builder.ToTable("producto_etiquetas");
        builder.HasKey(pe => new { pe.ProductoId, pe.EtiquetaId });

        builder.HasOne(pe => pe.Producto)
            .WithMany(p => p.Etiquetas)
            .HasForeignKey(pe => pe.ProductoId);

        builder.HasOne(pe => pe.Etiqueta)
            .WithMany(e => e.Productos)
            .HasForeignKey(pe => pe.EtiquetaId);
    }
}
```

Para guardar el grafo alcanza con agregar el principal y llamar a `SaveChangesAsync` una vez:

```csharp
var cliente = new Cliente
{
    Nombre = "Ana Torres",
    Direccion = new Direccion { Calle = "San Martín 123", Ciudad = "Rosario" },
    Pedidos =
    [
        new Pedido { Fecha = DateTime.UtcNow, Total = 45000m }
    ]
};

db.Clientes.Add(cliente);
await db.SaveChangesAsync();

Console.WriteLine($"Cliente {cliente.Id} con {cliente.Pedidos.Count} pedido(s)");
```

```text
Cliente 1 con 1 pedido(s)
```

EF Core inserta en orden: cliente, dirección, pedido. La clave generada del cliente se propaga a las demás filas. Si una inserción falla, `SaveChangesAsync` revierte todo dentro de su transacción.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `violates foreign key constraint` | Se intentó guardar un dependiente con un principal inexistente | Guardar el principal primero o agregar el grafo completo |
| `update or delete violates foreign key constraint` | Borraste un principal con dependientes y `Restrict` activo | Borrar primero los dependientes o cambiar la política |
| Se creó una columna `ClienteId1` | La convención no coincidió y se generó una clave sombra | Declarar `HasForeignKey` de forma explícita |
| La colección llega vacía | No se cargó la navegación en la consulta | Usar `Include`, tema de la lección 6 |
| `null value in column "ClienteId"` | La relación es obligatoria y falta la clave | Asignar el principal o hacer la relación opcional |
| Se borraron pedidos sin querer | Política `Cascade` en una relación comercial | Revisar `OnDelete` y preferir `Restrict` |

## Buenas prácticas

- Inicializá las colecciones de navegación con `[]` y las referencias obligatorias con `null!`.
- Configurá `HasForeignKey` explícitamente cuando el nombre no siga la convención.
- Usá `Restrict` en relaciones con datos que no deberían desaparecer.
- Preferí una entidad de unión explícita si la tabla intermedia va a tener columnas propias.
- Modelá ambos lados de la navegación; simplifica consultas y hace visible la relación.

## Ejercicio

Modelá un sistema de cursos con `Curso`, `Instructor` y `Modulo`. Un instructor dicta varios cursos; un curso tiene un solo instructor y varios módulos. Configurá las claves foráneas, usá `Restrict` para el instructor y `Cascade` para los módulos. Insertá un curso con dos módulos y mostrá cuántos quedaron.

<details>
<summary>Ver solución</summary>

```csharp
public sealed class Instructor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<Curso> Cursos { get; set; } = [];
}

public sealed class Curso
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;
    public List<Modulo> Modulos { get; set; } = [];
}

public sealed class Modulo
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;
}

public sealed class CursoConfiguracion : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> builder)
    {
        builder.ToTable("cursos");

        builder.HasOne(c => c.Instructor)
            .WithMany(i => i.Cursos)
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Modulos)
            .WithOne(m => m.Curso)
            .HasForeignKey(m => m.CursoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

var curso = new Curso
{
    Titulo = "EF Core desde cero",
    Instructor = new Instructor { Nombre = "Bruno Díaz" },
    Modulos =
    [
        new Modulo { Titulo = "DbContext" },
        new Modulo { Titulo = "Consultas" }
    ]
};

db.Cursos.Add(curso);
await db.SaveChangesAsync();

Console.WriteLine($"Curso {curso.Id}: {curso.Modulos.Count} módulos");
```

```text
Curso 1: 2 módulos
```

</details>

## Resumen

- Las relaciones combinan una clave foránea con navegaciones en ambos lados.
- `HasMany` y `WithOne` configuran uno a muchos; `HasOne` y `WithOne`, uno a uno.
- La clave foránea del uno a uno se declara con `HasForeignKey<TDependiente>`.
- Muchos a muchos se resuelve con una entidad de unión o con colecciones en ambos lados.
- `DeleteBehavior` define si el borrado se propaga, se bloquea o deja la clave en null.

## Siguiente paso

El modelo ya representa el dominio, pero todavía vive solo en memoria. En la próxima lección vas a llevar esos cambios a PostgreSQL con [migraciones](/modulos/ef-core/migraciones).
