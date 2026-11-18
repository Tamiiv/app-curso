---
slug: capas
title: "Arquitectura en capas"
order: 1
estimatedMinutes: 15
summary: "Separá presentación, aplicación, dominio e infraestructura para que los cambios de una parte no obliguen a reescribir las demás."
quiz:
  - question: "¿Cuál es la dirección correcta de dependencias en una arquitectura en capas?"
    options:
      - "Presentación depende de aplicación, aplicación del dominio e infraestructura implementa contratos"
      - "El dominio depende de la infraestructura para acceder a la base de datos"
      - "La infraestructura depende de la presentación para conocer los endpoints"
      - "Todas las capas dependen entre sí sin una dirección definida"
    answer: 0
    explanation: "Las capas internas no conocen a las externas: presentación llama a aplicación, aplicación usa el dominio y la infraestructura implementa las interfaces definidas hacia adentro."
  - question: "¿Dónde conviene declarar la interfaz IPedidoRepository?"
    options:
      - "En el proyecto de infraestructura, junto a EF Core"
      - "En el proyecto de aplicación o dominio, porque es un contrato que ellos necesitan"
      - "En el proyecto de la API, porque ahí se registra en el contenedor"
      - "En un archivo de configuración JSON"
    answer: 1
    explanation: "El contrato pertenece a quien lo consume. Si la interfaz vive en infraestructura, la aplicación termina dependiendo de la base de datos."
  - question: "¿En qué caso NO conviene aplicar capas estrictas desde el primer día?"
    options:
      - "Un sistema con reglas de negocio complejas y varios equipos"
      - "Un prototipo CRUD de una sola pantalla que se va a validar en una semana"
      - "Una aplicación que debe durar años y cambiar de base de datos"
      - "Un producto con auditoría y permisos detallados"
    answer: 1
    explanation: "Para un prototipo descartable, cuatro proyectos y mapeos adicionales agregan costo sin resolver un problema real. La arquitectura se justifica por el contexto."
---

# Arquitectura en capas

## Objetivo

Organizar una aplicación en capas con responsabilidades claras, hacer que las dependencias apunten hacia adentro y reconocer cuándo esta separación ayuda y cuándo solo agrega ceremonia.

## Requisitos previos

Necesitás haber completado el [ejercicio integrador de seguridad JWT](/modulos/seguridad-jwt/ejercicio-integrador) y manejar APIs mínimas, inyección de dependencias y acceso a datos con EF Core. También conviene tener claro qué es una interfaz y cómo se registra un servicio en `Program.cs`.

## Concepto

La arquitectura en capas divide el sistema según responsabilidades. Cada capa tiene un motivo distinto para cambiar y se comunica con las vecinas a través de contratos.

| Capa | Responsabilidad | Cambia cuando |
|---|---|---|
| Presentación | Recibir HTTP, validar forma del request, devolver códigos | Cambia el contrato de la API o el formato de salida |
| Aplicación | Orquestar casos de uso, transacciones, autorización | Cambia el flujo de una operación |
| Dominio | Reglas de negocio, invariantes, entidades | Cambian las reglas del negocio |
| Infraestructura | Base de datos, correo, HTTP, colas, archivos | Cambia un proveedor técnico |

La regla central es la dirección de las dependencias: presentación conoce aplicación, aplicación conoce dominio, e infraestructura implementa interfaces que viven hacia adentro. El dominio no sabe que existe SQL Server, ASP.NET Core ni un sistema de correo.

```text
Tienda.sln
src/Tienda.Dominio/          Pedido.cs
src/Tienda.Aplicacion/       IPedidoRepository.cs, ConfirmarPedido.cs
src/Tienda.Infraestructura/  RepositorioPedidos.cs
src/Tienda.Api/              Program.cs
tests/Tienda.Aplicacion.Tests/
```

La capa de aplicación cuenta la historia del caso de uso: buscar el pedido, verificar que se pueda confirmar, guardar y avisar. El dominio se ocupa de que el pedido no quede en un estado inválido. La infraestructura resuelve el cómo técnico.

Un error frecuente es crear una capa de servicios que solo reenvía llamadas al repositorio: eso es un pasamanos, no arquitectura. Si un método no agrega coordinación, autorización ni reglas, el endpoint puede usar el repositorio directamente.

## Ejemplo mínimo

La interfaz del repositorio vive en aplicación; la implementación vive en infraestructura.

```csharp
// Tienda.Aplicacion
public interface IContadorPedidos { int Cantidad { get; } }

public sealed class ConsultarCantidadPedidos(IContadorPedidos contador)
{
    public string Ejecutar() => contador.Cantidad == 0
        ? "Todavía no hay pedidos"
        : $"Hay {contador.Cantidad} pedidos";
}

// Tienda.Infraestructura
public sealed class ContadorEnMemoria : IContadorPedidos
{
    public int Cantidad { get; private set; }
    public void Registrar() => Cantidad++;
}

// Tienda.Api/Program.cs
builder.Services.AddSingleton<ContadorEnMemoria>();
builder.Services.AddSingleton<IContadorPedidos>(sp => sp.GetRequiredService<ContadorEnMemoria>());
builder.Services.AddScoped<ConsultarCantidadPedidos>();
app.MapGet("/pedidos/cantidad", (ConsultarCantidadPedidos caso) =>
    Results.Ok(new { mensaje = caso.Ejecutar() }));
```

Responde `200 OK` con `{ "mensaje": "Todavía no hay pedidos" }`.

La aplicación no conoce `ContadorEnMemoria`: cambiar el contador por una consulta a la base no toca el caso de uso ni el endpoint.

## Ejemplo aplicado

Un pedido se confirma solo si tiene líneas y no fue cancelado. La regla vive en el dominio; la coordinación, en aplicación; la persistencia, en infraestructura.

```csharp
// Tienda.Dominio/Pedido.cs
namespace Tienda.Dominio;

public sealed class Pedido(int id)
{
    public int Id { get; } = id;
    public int Lineas { get; private set; }
    public bool Confirmado { get; private set; }
    public bool Cancelado { get; private set; }

    public void AgregarLinea()
    {
        if (Confirmado || Cancelado) throw new InvalidOperationException("Pedido cerrado.");
        Lineas++;
    }

    public void Confirmar()
    {
        if (Cancelado) throw new InvalidOperationException("Pedido cancelado.");
        if (Lineas == 0) throw new InvalidOperationException("Sin líneas.");
        Confirmado = true;
    }
}

// Tienda.Aplicacion/IPedidoRepository.cs y ConfirmarPedido.cs
namespace Tienda.Aplicacion;

public interface IPedidoRepository { Pedido? Buscar(int id); void Guardar(Pedido p); }

public sealed class ConfirmarPedido(IPedidoRepository repositorio)
{
    public void Ejecutar(int id)
    {
        Pedido pedido = repositorio.Buscar(id)
            ?? throw new KeyNotFoundException($"No existe el pedido {id}.");
        pedido.Confirmar();
        repositorio.Guardar(pedido);
    }
}

// Tienda.Api/Program.cs
builder.Services.AddSingleton<IPedidoRepository, RepositorioPedidos>();
builder.Services.AddScoped<ConfirmarPedido>();
app.MapPost("/pedidos/{id:int}/confirmar", (int id, ConfirmarPedido caso) => { caso.Ejecutar(id); return Results.NoContent(); });
```

`RepositorioPedidos` es un diccionario en memoria de infraestructura. El endpoint responde `204 No Content` o propaga la excepción del dominio.

El endpoint no decide si el pedido se puede confirmar. Solo traduce HTTP a una llamada de aplicación. Si cambia la regla de negocio, se modifica `Pedido` y las pruebas de dominio detectan el impacto.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El dominio referencia EF Core | Se anotaron las entidades con atributos de persistencia | Separar modelo de dominio y modelo de datos |
| La aplicación instancia el repositorio con `new` | Se evita la inyección de dependencias | Registrar la interfaz en el contenedor |
| Capas que solo reenvían llamadas | No hay lógica de coordinación real | Unir capas o mover la lógica al lugar correcto |
| El endpoint tiene reglas de negocio | Se programó directo en el handler HTTP | Mover la regla al dominio o al caso de uso |
| Cambiar la base obliga a tocar la aplicación | La interfaz vive en infraestructura | Declarar el contrato en aplicación |

## Buenas prácticas

- Hacé que las dependencias apunten hacia el dominio; si una interfaz existe solo para que el dominio llame a la base, está mal ubicada.
- Mantené las entidades de dominio sin atributos de serialización ni de persistencia.
- Usá la capa de aplicación para transacciones y autorización, no para repetir reglas del dominio.
- Probá el dominio y los casos de uso sin levantar la API ni la base de datos.

## Ejercicio

Implementá `CancelarPedido` en la capa de aplicación. Debe buscar el pedido, lanzar `KeyNotFoundException` si no existe y llamar a `Cancelar()` en el dominio. Un pedido confirmado no se puede cancelar: esa regla debe vivir en `Pedido`. Agregá el endpoint `POST /pedidos/{id}/cancelar` y una prueba con un repositorio en memoria.

<details>
<summary>Ver solución</summary>

```csharp
public void Cancelar()
{
    if (Cancelado) throw new InvalidOperationException("Ya está cancelado.");
    if (Confirmado) throw new InvalidOperationException("No se cancela un confirmado.");
    Cancelado = true;
}

public sealed class CancelarPedido(IPedidoRepository repositorio)
{
    public void Ejecutar(int id)
    {
        Pedido pedido = repositorio.Buscar(id)
            ?? throw new KeyNotFoundException($"No existe el pedido {id}.");
        pedido.Cancelar();
        repositorio.Guardar(pedido);
    }
}

// Tienda.Api/Program.cs
app.MapPost("/pedidos/{id:int}/cancelar", (int id, CancelarPedido caso) =>
{
    try { caso.Ejecutar(id); return Results.NoContent(); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
    catch (InvalidOperationException) { return Results.Conflict(); }
});
```

Prueba de aplicación: `new CancelarPedido(repo).Ejecutar(1); Assert.True(repo.Buscar(1)!.Cancelado);`

```text
POST /pedidos/7/cancelar   -> 204 No Content
POST /pedidos/99/cancelar  -> 404 Not Found
POST /pedidos/8/cancelar   -> 409 Conflict
```

La regla quedó en el dominio y el caso de uso solo coordina. El endpoint se limita a traducir excepciones a códigos HTTP.

</details>

## Resumen

- Las capas separan responsabilidades y limitan el impacto de los cambios.
- Las dependencias apuntan hacia el dominio; la infraestructura implementa contratos.
- La aplicación coordina casos de uso; el dominio protege las invariantes.
- Las capas agregan valor cuando el sistema y el equipo lo justifican, no por defecto.

## Siguiente paso

Las capas resuelven la separación, pero no dicen nada sobre cómo aislar el núcleo del negocio de los frameworks. En la próxima lección vas a ver la [arquitectura limpia](/modulos/arquitectura/arquitectura-limpia) y su regla de dependencia.
