---
slug: solid-lsp-isp-dip
title: "SOLID: LSP, ISP y DIP"
order: 5
estimatedMinutes: 18
summary: "Completá SOLID: sustitución de Liskov sin sorpresas, interfaces pequeñas y dependencias que apuntan a abstracciones en lugar de detalles."
quiz:
  - question: "¿Qué exige el principio de sustitución de Liskov (LSP)?"
    options:
      - "Que una subclase pueda reemplazar a la base sin romper el comportamiento esperado"
      - "Que una clase no pueda heredar"
      - "Que todas las subclases tengan el mismo código"
      - "Que se use composición siempre"
    answer: 0
    explanation: "Si el código cliente funciona con la base, debe funcionar con cualquier subtipo sin casos especiales ni excepciones inesperadas."
  - question: "¿Qué propone el principio de segregación de interfaces (ISP)?"
    options:
      - "Una interfaz gigante para todo el dominio"
      - "Varias interfaces pequeñas y específicas en lugar de una que obligue a implementar de más"
      - "Que no se usen interfaces"
      - "Que todas las interfaces tengan un solo método"
    answer: 1
    explanation: "ISP evita que una implementación dependa de métodos que no usa, separando contratos según el cliente."
  - question: "¿Qué significa inversión de dependencias (DIP)?"
    options:
      - "Que las clases de alto nivel dependan de abstracciones y no de detalles de infraestructura"
      - "Que la base de datos dependa del dominio"
      - "Que se inyecten todas las dependencias con new"
      - "Que las interfaces dependan de clases concretas"
    answer: 0
    explanation: "DIP invierte la dirección: el dominio define la abstracción y la infraestructura la implementa."
  - question: "¿Qué síntoma indica una violación de LSP?"
    options:
      - "La subclase hereda todos los métodos"
      - "El cliente debe preguntar por el tipo concreto o capturar excepciones que la base no lanza"
      - "La base tiene una interfaz"
      - "La subclase agrega métodos nuevos"
    answer: 1
    explanation: "Si el cliente necesita `if (x is SubTipo)` o recibe excepciones inesperadas, el subtipo no es sustituible."
---

# SOLID: LSP, ISP y DIP

## Objetivo

Detectar jerarquías que rompen la sustitución, dividir interfaces demasiado grandes y organizar dependencias para que el dominio no dependa de la infraestructura.

## Requisitos previos

Haber leído [SOLID: SRP y OCP](/modulos/codigo-limpio/solid-srp-ocp). Necesitás manejar herencia, interfaces e inyección de dependencias.

## Concepto

**LSP (Liskov Substitution Principle):** si una función usa una clase base, debe funcionar igual con cualquier subclase. El subtipo puede agregar comportamiento, pero no puede romper las promesas del contrato.

El ejemplo clásico es `Ave` con `Volar()`. Un `Pingüino` hereda y lanza una excepción. El cliente que recorre aves y llama a `Volar()` se rompe. El problema no es el pingüino: es que `Volar` no pertenece a la base. La solución es un contrato `IVolador` separado.

**ISP (Interface Segregation Principle):** los clientes no deben depender de métodos que no usan. Una interfaz `IRepositorio` con veinte operaciones obliga a un repositorio de solo lectura a implementar `Agregar`, `Eliminar` y `Actualizar` con `NotSupportedException`. Eso es una bomba de tiempo.

**DIP (Dependency Inversion Principle):** los módulos de alto nivel no dependen de los de bajo nivel; ambos dependen de abstracciones. En la práctica, el dominio define `IPedidoRepository` y la infraestructura provee `PedidoRepositorySql`. El dominio no sabe si hay SQL, archivos o una API.

DIP no es "usar interfaces en todos lados". Es una dirección de dependencia. Si la interfaz vive junto al código de SQL, el dominio sigue dependiendo de la infraestructura.

## Ejemplo mínimo

LSP roto y corregido:

```csharp
// Mal: la base promete Volar y el pingüino no puede.
public abstract class Ave
{
    public abstract void Volar();
}

public sealed class Pinguino : Ave
{
    public override void Volar() =>
        throw new NotSupportedException("Un pingüino no vuela.");
}
```

```csharp
// Bien: cada capacidad en su contrato.
public abstract class Ave
{
    public required string Nombre { get; init; }
}

public interface IVolador
{
    void Volar();
}

public sealed class Pinguino : Ave
{
    public void Nadar() => Console.WriteLine($"{Nombre} nada.");
}

public sealed class Paloma : Ave, IVolador
{
    public void Volar() => Console.WriteLine($"{Nombre} vuela.");
}
```

```csharp
foreach (IVolador volador in new IVolador[] { new Paloma { Nombre = "Paloma" } })
{
    volador.Volar();
}
```

```text
Paloma vuela.
```

El cliente que necesita volar pide `IVolador`. El zoológico que solo muestra aves trabaja con `Ave` sin excepciones.

## Ejemplo aplicado

ISP y DIP juntos en un repositorio:

```csharp
public sealed record Pedido(Guid Id, decimal Total);

public interface ILectorDePedidos
{
    Pedido? Obtener(Guid id);
    IReadOnlyList<Pedido> Listar();
}

public interface IEscritorDePedidos
{
    void Agregar(Pedido pedido);
}

public interface IRepositorioDePedidos : ILectorDePedidos, IEscritorDePedidos
{
}

public sealed class PedidoSql(IConfiguration configuracion) : IRepositorioDePedidos
{
    private readonly List<Pedido> _cache = [];

    public Pedido? Obtener(Guid id) => _cache.FirstOrDefault(p => p.Id == id);

    public IReadOnlyList<Pedido> Listar() => _cache;

    public void Agregar(Pedido pedido) => _cache.Add(pedido);
}

public sealed class ReporteDePedidos(ILectorDePedidos lector)
{
    public decimal Total() => lector.Listar().Sum(p => p.Total);
}
```

`ReporteDePedidos` solo depende de lectura. Si mañana se crea un lector de solo lectura para reportes, no arrastra la capacidad de escritura. Y como `ReporteDePedidos` depende de `ILectorDePedidos`, el dominio no conoce `PedidoSql`.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `NotSupportedException` en una subclase | La base promete algo que el subtipo no puede cumplir | Separar la capacidad en otra interfaz |
| Interfaz con veinte métodos | Se agrupó por conveniencia | Dividir por cliente y por capacidad |
| El dominio referencia `Microsoft.EntityFrameworkCore` | La dependencia apunta hacia la infraestructura | Mover la abstracción al dominio |
| Se inyecta `DbContext` en un servicio de dominio | La capa alta conoce el detalle | Inyectar un repositorio o unidad de trabajo |
| Demasiadas interfaces de un método | Exceso de ISP | Agrupar por contrato coherente, no por método |
| Se usa `new` para dependencias externas | No hay inversión | Registrar e inyectar la abstracción |

## Buenas prácticas

- Modelá capacidades, no taxonomías rígidas.
- Si un cliente no usa un método, probablemente la interfaz sea demasiado grande para ese cliente.
- Hacé que las abstracciones vivan cerca del consumidor, no del proveedor.
- Preferí composición antes que jerarquías profundas.
- Verificá LSP con tests que usen la base y pasen cualquier implementación.

## Ejercicio

Refactorizá una interfaz `IAlmacen` con `Leer`, `Escribir` y `Eliminar` para que un servicio de reportes dependa solo de lectura. Luego creá una implementación en memoria y otra que simule SQL, sin que el servicio de reportes cambie.

<details>
<summary>Ver solución</summary>

```csharp
public interface ILecturaAlmacen
{
    IReadOnlyList<string> Leer();
}

public interface IEscrituraAlmacen
{
    void Escribir(string valor);
    void Eliminar(string valor);
}

public sealed class AlmacenMemoria : ILecturaAlmacen, IEscrituraAlmacen
{
    private readonly List<string> _datos = [];

    public IReadOnlyList<string> Leer() => _datos;

    public void Escribir(string valor) => _datos.Add(valor);

    public void Eliminar(string valor) => _datos.Remove(valor);
}

public sealed class ReporteService(ILecturaAlmacen almacen)
{
    public int Cantidad() => almacen.Leer().Count;
}

var memoria = new AlmacenMemoria();
memoria.Escribir("uno");
memoria.Escribir("dos");

var reporte = new ReporteService(memoria);
Console.WriteLine(reporte.Cantidad());
```

```text
2
```

`ReporteService` nunca ve `Escribir` ni `Eliminar`. Una implementación SQL solo necesita cumplir `ILecturaAlmacen` para los reportes.

</details>

## Resumen

- LSP exige subtipos sustituibles sin sorpresas para el cliente.
- ISP divide contratos para que nadie implemente de más.
- DIP hace que el dominio dependa de abstracciones y la infraestructura las implemente.
- Las interfaces se diseñan desde el cliente, no desde la implementación.

## Siguiente paso

Con SOLID completo, el próximo tema son los [patrones creacionales](/modulos/codigo-limpio/patrones-creacionales): formas probadas de crear objetos complejos sin acoplar al cliente.
