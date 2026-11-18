---
slug: hexagonal
title: "Arquitectura hexagonal"
order: 3
estimatedMinutes: 15
summary: "Pensá el sistema como un núcleo con puertos y adaptadores: la misma lógica se expone por HTTP, consola o cola sin cambiar el dominio."
quiz:
  - question: "¿Qué es un puerto en la arquitectura hexagonal?"
    options:
      - "Un contrato que define cómo el núcleo interactúa con el exterior"
      - "Un puerto de red del servidor"
      - "Una tabla de la base de datos"
      - "Un archivo de configuración"
    answer: 0
    explanation: "El puerto es una interfaz del núcleo: de entrada si el exterior lo invoca, de salida si el núcleo necesita algo externo."
  - question: "¿Qué diferencia hay entre un puerto de entrada y uno de salida?"
    options:
      - "No hay diferencia"
      - "El de entrada expone casos de uso; el de salida declara dependencias que el núcleo necesita"
      - "El de entrada va a la base y el de salida a HTTP"
      - "El de salida se implementa en el dominio"
    answer: 1
    explanation: "El puerto de entrada lo implementa el núcleo y lo llama un adaptador. El de salida lo define el núcleo y lo implementa un adaptador externo."
  - question: "¿Qué ventaja práctica aporta poder cambiar un adaptador?"
    options:
      - "Se puede reemplazar la base por un repositorio en memoria en tests"
      - "El dominio deja de necesitar tests"
      - "Desaparecen las interfaces"
      - "El sistema no necesita configuración"
    answer: 0
    explanation: "Al depender de puertos, los tests usan adaptadores falsos y la producción usa los reales."
  - question: "¿Qué señal indica que la arquitectura hexagonal no está aportando valor?"
    options:
      - "Hay más de dos adaptadores"
      - "Solo existe un adaptador trivial y el núcleo no tiene reglas propias"
      - "El núcleo tiene tests"
      - "Los puertos son interfaces"
    answer: 1
    explanation: "Si no hay lógica que aislar ni variantes de infraestructura, las capas agregan ceremonia sin beneficio."
---

# Arquitectura hexagonal

## Objetivo

Diseñar un núcleo aislado con puertos de entrada y salida, implementar adaptadores para HTTP y persistencia, y evaluar cuándo esta separación aporta valor real.

## Requisitos previos

Haber leído [Arquitectura limpia](/modulos/arquitectura/arquitectura-limpia). Necesitás manejar interfaces y casos de uso.

## Concepto

La arquitectura hexagonal (ports and adapters) mira el sistema desde el núcleo: **la lógica de negocio está en el centro y todo lo demás se conecta por puertos**. Un puerto es una interfaz; un adaptador es la implementación concreta que habla con el mundo exterior.

Hay dos familias:

| Puerto | Quién lo define | Quién lo implementa | Ejemplo |
|---|---|---|---|
| Entrada (driving) | El núcleo | Un adaptador externo | `IConfirmarPedido`, endpoint HTTP, CLI |
| Salida (driven) | El núcleo | Un adaptador externo | `IPedidoRepository`, cliente de email |

La diferencia con la arquitectura limpia es de enfoque, no de esencia. Clean Architecture organiza en capas concéntricas; hexagonal enfatiza la simetría entre todos los actores externos. Un endpoint HTTP, una consola y un consumidor de cola son todos adaptadores de entrada al mismo puerto. La base de datos, el email y un servicio de pagos son adaptadores de salida.

```text
   HTTP ─┐                     ┌─ Postgres
   CLI  ─┼── núcleo ───────────┼─ Email
   Cola ─┘   (puertos)         └─ Pasarela de pago
```

La prueba de que la separación funciona: **podés ejecutar el caso de uso sin levantar el servidor**. Si el núcleo depende de `HttpContext` o de un `DbContext`, no hay puerto real, solo nombres nuevos para las mismas dependencias.

El costo es real: una operación simple requiere interfaz, implementación, registro y a veces un DTO de mapeo. La arquitectura se justifica cuando hay reglas que duran, múltiples entradas o infraestructura que probablemente cambie.

## Ejemplo mínimo

Puerto de entrada y su implementación en el núcleo:

```csharp
namespace AppCurso.Application.Cuentas;

public sealed record TransferirCommand(Guid OrigenId, Guid DestinoId, decimal Monto);

public sealed record TransferirResultado(bool Exitoso, string? Error);

// Puerto de entrada: el mundo exterior invoca esto.
public interface ITransferirUseCase
{
    Task<TransferirResultado> EjecutarAsync(TransferirCommand command, CancellationToken cancellationToken);
}

// Puerto de salida: el núcleo necesita esto.
public interface ICuentaRepository
{
    Task<Cuenta?> ObtenerAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class TransferirService(ICuentaRepository repository) : ITransferirUseCase
{
    public async Task<TransferirResultado> EjecutarAsync(
        TransferirCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Monto <= 0)
        {
            return new TransferirResultado(false, "El monto debe ser positivo.");
        }

        var origen = await repository.ObtenerAsync(command.OrigenId, cancellationToken);
        var destino = await repository.ObtenerAsync(command.DestinoId, cancellationToken);

        if (origen is null || destino is null)
        {
            return new TransferirResultado(false, "Cuenta no encontrada.");
        }

        if (origen.Saldo < command.Monto)
        {
            return new TransferirResultado(false, "Saldo insuficiente.");
        }

        origen.Debitar(command.Monto);
        destino.Acreditar(command.Monto);

        await repository.SaveChangesAsync(cancellationToken);
        return new TransferirResultado(true, null);
    }
}
```

```csharp
public sealed class Cuenta
{
    public Cuenta(Guid id, decimal saldo) => (Id, Saldo) = (id, saldo);

    public Guid Id { get; }
    public decimal Saldo { get; private set; }

    public void Debitar(decimal monto) => Saldo -= monto;
    public void Acreditar(decimal monto) => Saldo += monto;
}
```

El servicio no conoce HTTP ni SQL. Depende de un puerto de salida y expone un puerto de entrada.

## Ejemplo aplicado

Dos adaptadores de entrada sobre el mismo puerto: HTTP y consola.

```csharp
// Adaptador de entrada HTTP.
app.MapPost("/transferencias", async (
    TransferirRequest request,
    ITransferirUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var resultado = await useCase.EjecutarAsync(
        new TransferirCommand(request.OrigenId, request.DestinoId, request.Monto),
        cancellationToken);

    return resultado.Exitoso
        ? TypedResults.NoContent()
        : TypedResults.BadRequest(new ProblemDetails { Title = resultado.Error });
});

public sealed record TransferirRequest(Guid OrigenId, Guid DestinoId, decimal Monto);
```

```csharp
// Adaptador de entrada de consola.
var servicio = new ServiceCollection()
    .AddScoped<ICuentaRepository, CuentaRepositorySql>()
    .AddScoped<ITransferirUseCase, TransferirService>()
    .BuildServiceProvider();

var useCase = servicio.GetRequiredService<ITransferirUseCase>();
var resultado = await useCase.EjecutarAsync(
    new TransferirCommand(origen, destino, 500),
    CancellationToken.None);

Console.WriteLine(resultado.Exitoso ? "Transferencia realizada" : resultado.Error);
```

```text
HTTP:     POST /transferencias       → 204 o 400
Consola:  dotnet run --transferir    → mismo caso de uso, mismo resultado
```

El adaptador de salida para EF Core implementa `ICuentaRepository`. En tests se usa un adaptador en memoria y el servicio se prueba sin base ni servidor.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El núcleo recibe `HttpContext` | Se filtró el adaptador hacia adentro | Pasar tipos propios al caso de uso |
| Los puertos son solo renombres de repositorios | No hay reglas que aislar | Evaluar si la estructura aporta valor |
| El adaptador contiene reglas de negocio | Se puso lógica en el borde | Mover las reglas al núcleo |
| Un puerto por método | Exceso de granularidad | Agrupar por caso de uso o capacidad |
| El dominio conoce el ORM | Se usaron atributos de persistencia | Mapear desde infraestructura |
| Muchos adaptadores para una sola salida | Se abstrajo por anticipado | Implementar solo lo que existe |

## Buenas prácticas

- Un puerto de entrada por caso de uso o grupo coherente.
- Puertos de salida nombrados por lo que el núcleo necesita, no por la tecnología.
- Adaptadores delgados: traducen, no deciden.
- Tests del núcleo con adaptadores en memoria.
- Documentá qué adaptadores existen para cada puerto.
- Aplicá la estructura cuando haya más de una entrada o infraestructura cambiante.

## Ejercicio

Implementá un caso de uso `CrearTarea` con un puerto de entrada y dos adaptadores de entrada (HTTP y consola). Verificá que el mismo código responda a ambos sin cambios en el núcleo.

<details>
<summary>Ver solución</summary>

```csharp
public sealed record CrearTareaCommand(string Titulo);
public sealed record CrearTareaResultado(Guid? Id, string? Error);

public interface ICrearTareaUseCase
{
    Task<CrearTareaResultado> EjecutarAsync(CrearTareaCommand command, CancellationToken cancellationToken);
}

public interface ITareaRepository
{
    Task AgregarAsync(Tarea tarea, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class CrearTareaService(ITareaRepository repository) : ICrearTareaUseCase
{
    public async Task<CrearTareaResultado> EjecutarAsync(
        CrearTareaCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Titulo))
        {
            return new CrearTareaResultado(null, "El título es obligatorio.");
        }

        var tarea = new Tarea(Guid.NewGuid(), command.Titulo.Trim());
        await repository.AgregarAsync(tarea, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new CrearTareaResultado(tarea.Id, null);
    }
}
```

```csharp
// Adaptador HTTP
app.MapPost("/tareas", async (
    CrearTareaCommand command,
    ICrearTareaUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var resultado = await useCase.EjecutarAsync(command, cancellationToken);

    return resultado.Id is { } id
        ? TypedResults.Created($"/tareas/{id}", new { id })
        : TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Titulo"] = [resultado.Error!]
        });
});
```

```csharp
// Adaptador consola
var titulo = args.FirstOrDefault() ?? "Tarea de ejemplo";
var resultado = await useCase.EjecutarAsync(new CrearTareaCommand(titulo), CancellationToken.None);
Console.WriteLine(resultado.Id is null ? resultado.Error : $"Creada {resultado.Id}");
```

Ambos adaptadores llaman al mismo servicio. Si mañana se agrega un consumidor de cola, se escribe otro adaptador y el núcleo no se toca.

</details>

## Resumen

- El núcleo contiene reglas y define puertos.
- Los adaptadores traducen entre el mundo exterior y los puertos.
- Entradas y salidas son simétricas: HTTP, consola o cola por un lado; base, email o API por el otro.
- El valor aparece cuando hay reglas que aislar o más de una entrada.
- El núcleo se prueba sin infraestructura.

## Siguiente paso

Ya tenés un núcleo aislado. El siguiente paso es organizar el sistema completo sin caer en microservicios prematuros: el [monolito modular](/modulos/arquitectura/monolito-modular).
