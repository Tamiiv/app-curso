---
slug: patrones-creacionales
title: "Patrones creacionales"
order: 6
estimatedMinutes: 18
summary: "Usá Factory Method, Builder y Singleton con criterio: qué problema resuelve cada uno, cómo se implementan en C# y cuándo conviene no usarlos."
quiz:
  - question: "¿Qué problema resuelve el patrón Builder?"
    options:
      - "Crear objetos con muchos parámetros opcionales de forma legible y segura"
      - "Mejorar el rendimiento de una consulta"
      - "Reemplazar la inyección de dependencias"
      - "Serializar objetos a JSON"
    answer: 0
    explanation: "Builder separa la construcción paso a paso y evita constructores con diez parámetros o combinaciones de flags."
  - question: "¿Cuándo conviene una fábrica (Factory Method)?"
    options:
      - "Cuando el cliente no debe conocer la clase concreta que crea"
      - "Cuando hay que crear un objeto con new en un solo lugar"
      - "Cuando se quiere evitar el polimorfismo"
      - "Cuando el objeto es inmutable"
    answer: 0
    explanation: "La fábrica encapsula la decisión de qué implementación crear y deja al cliente dependiendo de una abstracción."
  - question: "¿Por qué el Singleton se considera riesgoso?"
    options:
      - "Porque no compila en .NET"
      - "Porque introduce estado global, dificulta los tests y esconde dependencias"
      - "Porque consume demasiada memoria"
      - "Porque no se puede usar con interfaces"
    answer: 1
    explanation: "El acceso global crea acoplamiento oculto y complica el aislamiento. En .NET normalmente se reemplaza por un servicio registrado en el contenedor."
  - question: "¿Cuál es la diferencia entre Factory Method y Abstract Factory?"
    options:
      - "No hay diferencia"
      - "Factory Method crea un producto; Abstract Factory crea familias de productos relacionados"
      - "Abstract Factory solo sirve para bases de datos"
      - "Factory Method requiere herencia múltiple"
    answer: 1
    explanation: "Abstract Factory agrupa varias fábricas para construir una familia coherente de objetos."
---

# Patrones creacionales

## Objetivo

Aplicar Factory Method, Builder y Singleton cuando el problema lo justifica, reconocer sus costos y escribir implementaciones simples en C#.

## Requisitos previos

Haber leído [SOLID: LSP, ISP y DIP](/modulos/codigo-limpio/solid-lsp-isp-dip). Necesitás manejar interfaces, records y el contenedor de dependencias.

## Concepto

Los patrones creacionales resuelven un problema puntual: **cómo crear objetos sin acoplar al cliente con los detalles de construcción**.

| Patrón | Problema | Costo |
|---|---|---|
| Factory Method | El cliente no debe decidir la clase concreta | Una indirección |
| Abstract Factory | Hay familias de objetos relacionados | Más clases y configuración |
| Builder | Muchos parámetros o pasos de construcción | Más código que un constructor |
| Singleton | Se necesita una única instancia global | Estado global oculto |

En C# moderno, el contenedor de dependencias ya cubre gran parte de lo que antes hacía Singleton. Un `builder.Services.AddSingleton<ICache, Cache>()` da una única instancia sin acceso global estático y con tests posibles.

Factory Method aparece cuando la elección depende de datos en tiempo de ejecución: el tipo de archivo, el proveedor de pago o el canal de notificación. En lugar de un `switch` en el cliente, una implementación de fábrica decide.

Builder aparece cuando un objeto tiene muchas partes opcionales y combinaciones. En C#, los argumentos nombrados y los records con `with` cubren muchos casos. Builder sigue siendo útil cuando la construcción tiene pasos con validaciones o cuando el orden importa.

La regla es la de siempre: el patrón se justifica por el problema, no por el catálogo.

## Ejemplo mínimo

Factory Method para notificaciones:

```csharp
public interface INotificacion
{
    void Enviar(string destinatario, string mensaje);
}

public sealed class EmailNotificacion : INotificacion
{
    public void Enviar(string destinatario, string mensaje) =>
        Console.WriteLine($"Email a {destinatario}: {mensaje}");
}

public sealed class SmsNotificacion : INotificacion
{
    public void Enviar(string destinatario, string mensaje) =>
        Console.WriteLine($"SMS a {destinatario}: {mensaje}");
}

public static class NotificacionFactory
{
    public static INotificacion Crear(string canal) => canal switch
    {
        "email" => new EmailNotificacion(),
        "sms" => new SmsNotificacion(),
        _ => throw new ArgumentOutOfRangeException(nameof(canal), canal, "Canal desconocido.")
    };
}
```

```csharp
var notificacion = NotificacionFactory.Crear("sms");
notificacion.Enviar("+54 341 555 5555", "Tu pedido salió.");
```

```text
SMS a +54 341 555 5555: Tu pedido salió.
```

El cliente no conoce `SmsNotificacion`. Si se agrega WhatsApp, cambia la fábrica y no el consumidor.

## Ejemplo aplicado

Builder para una consulta con filtros opcionales:

```csharp
public sealed class ConsultaPedidos
{
    private readonly List<string> _filtros = [];
    private string? _orden;
    private int _limite = 50;

    public ConsultaPedidos Desde(DateTime desde)
    {
        _filtros.Add($"fecha >= '{desde:yyyy-MM-dd}'");
        return this;
    }

    public ConsultaPedidos Hasta(DateTime hasta)
    {
        _filtros.Add($"fecha <= '{hasta:yyyy-MM-dd}'");
        return this;
    }

    public ConsultaPedidos ConEstado(string estado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(estado);
        _filtros.Add($"estado = '{estado}'");
        return this;
    }

    public ConsultaPedidos OrdenarPor(string columna)
    {
        _orden = columna;
        return this;
    }

    public ConsultaPedidos ConLimite(int limite)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limite);
        _limite = limite;
        return this;
    }

    public string Construir()
    {
        var where = _filtros.Count > 0 ? $" WHERE {string.Join(" AND ", _filtros)}" : string.Empty;
        var order = _orden is null ? string.Empty : $" ORDER BY {_orden}";
        return $"SELECT * FROM pedidos{where}{order} LIMIT {_limite};";
    }
}
```

```csharp
var consulta = new ConsultaPedidos()
    .Desde(new DateTime(2026, 1, 1))
    .ConEstado("pendiente")
    .OrdenarPor("fecha")
    .ConLimite(10)
    .Construir();

Console.WriteLine(consulta);
```

```text
SELECT * FROM pedidos WHERE fecha >= '2026-01-01' AND estado = 'pendiente' ORDER BY fecha LIMIT 10;
```

El builder valida en cada paso y evita un constructor con seis parámetros donde dos son `null`. Ojo: el ejemplo arma SQL con interpolación solo para mostrar la forma. En producción se usan parámetros, nunca concatenación de valores.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Builder para un objeto de tres campos | El patrón no aporta | Usar un record o argumentos nombrados |
| Fábrica que devuelve siempre la misma clase | No hay variación real | Instanciar directamente |
| `switch` de fábrica que crece sin control | Falta registro de implementaciones | Registrar en el contenedor o usar un diccionario |
| Singleton estático con estado mutable | Estado global oculto | Registrar el servicio en el contenedor |
| Builder que permite construir objetos inválidos | No valida al construir | Validar en `Construir()` o en el constructor del producto |
| Abstract Factory para un solo producto | Complejidad innecesaria | Factory Method simple |

## Buenas prácticas

- Preferí el contenedor de dependencias antes que Singleton manual.
- Usá fábricas cuando la elección depende de datos de ejecución.
- Usá Builder cuando la construcción tiene pasos, validaciones u opcionales reales.
- Validá en el punto de construcción para que no existan objetos a medio armar.
- Documentá qué variación cubre el patrón y cuál queda fuera.

## Ejercicio

Implementá una fábrica de exportadores (`csv`, `json`, `xml`) que devuelva `IExportador` y un Builder para armar un `Reporte` con título, filtros opcionales y formato. El reporte debe fallar si no tiene título al construirse.

<details>
<summary>Ver solución</summary>

```csharp
public interface IExportador
{
    string Exportar(IEnumerable<string> filas);
}

public sealed class CsvExportador : IExportador
{
    public string Exportar(IEnumerable<string> filas) => string.Join("\n", filas);
}

public sealed class JsonExportador : IExportador
{
    public string Exportar(IEnumerable<string> filas) =>
        "[" + string.Join(",", filas.Select(f => $"\"{f}\"")) + "]";
}

public sealed class XmlExportador : IExportador
{
    public string Exportar(IEnumerable<string> filas) =>
        "<filas>" + string.Concat(filas.Select(f => $"<fila>{f}</fila>")) + "</filas>";
}

public static class ExportadorFactory
{
    public static IExportador Crear(string formato) => formato switch
    {
        "csv" => new CsvExportador(),
        "json" => new JsonExportador(),
        "xml" => new XmlExportador(),
        _ => throw new ArgumentOutOfRangeException(nameof(formato), formato, "Formato no soportado.")
    };
}

public sealed class Reporte
{
    public required string Titulo { get; init; }
    public IReadOnlyList<string> Filtros { get; init; } = [];
}

public sealed class ReporteBuilder
{
    private string? _titulo;
    private readonly List<string> _filtros = [];

    public ReporteBuilder ConTitulo(string titulo)
    {
        _titulo = titulo;
        return this;
    }

    public ReporteBuilder ConFiltro(string filtro)
    {
        _filtros.Add(filtro);
        return this;
    }

    public Reporte Construir()
    {
        if (string.IsNullOrWhiteSpace(_titulo))
        {
            throw new InvalidOperationException("El reporte necesita un título.");
        }

        return new Reporte { Titulo = _titulo, Filtros = _filtros.ToArray() };
    }
}

var reporte = new ReporteBuilder()
    .ConTitulo("Pedidos pendientes")
    .ConFiltro("estado = pendiente")
    .Construir();

var exportador = ExportadorFactory.Crear("csv");
Console.WriteLine(exportador.Exportar(new[] { reporte.Titulo, string.Join(";", reporte.Filtros) }));
```

```text
Pedidos pendientes
estado = pendiente
```

</details>

## Resumen

- Factory Method encapsula qué clase concreta se crea.
- Builder arma objetos complejos paso a paso y valida al final.
- Singleton en .NET se reemplaza casi siempre por un servicio del contenedor.
- El patrón se elige por el problema, no por la moda.

## Siguiente paso

Los creacionales resuelven la construcción. Los [patrones estructurales](/modulos/codigo-limpio/patrones-estructurales) resuelven cómo componer objetos para adaptar, envolver y simplificar interfaces.
