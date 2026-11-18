---
slug: comentarios-y-formato
title: "Comentarios, formato y legibilidad"
order: 2
estimatedMinutes: 12
summary: "Usá comentarios para explicar decisiones y no para repetir el código, y aplicá un formato consistente que reduzca el esfuerzo de lectura."
quiz:
  - question: "¿Qué comentario aporta valor real?"
    options:
      - "// Incrementa el contador en uno"
      - "// Se usa una transacción porque el proveedor no admite reintentos idempotentes"
      - "// Declara la variable total"
      - "// Recorre la lista de pedidos"
    answer: 1
    explanation: "El comentario útil explica una decisión o una restricción externa que el código no puede expresar por sí solo."
  - question: "¿Qué debería hacerse con código comentado que lleva meses sin usarse?"
    options:
      - "Dejarlo por si acaso"
      - "Moverlo al final del archivo"
      - "Eliminarlo: el historial de git conserva la versión anterior"
      - "Envolverlo en #region"
    answer: 2
    explanation: "El control de versiones guarda el historial. El código comentado solo agrega ruido y se desactualiza."
  - question: "¿Cuál es la ventaja principal de las guard clauses?"
    options:
      - "Reducen el uso de memoria"
      - "Aplanan el anidamiento y dejan visible el camino feliz"
      - "Permiten omitir validaciones"
      - "Hacen que el método sea asíncrono"
    answer: 1
    explanation: "Al salir temprano ante casos inválidos, el resto del método queda en un solo nivel de indentación."
  - question: "¿Para qué sirve un archivo .editorconfig en un equipo?"
    options:
      - "Para compilar más rápido"
      - "Para definir reglas de estilo y análisis que las herramientas aplican de forma consistente"
      - "Para reemplazar las pruebas unitarias"
      - "Para configurar la base de datos"
    answer: 1
    explanation: ".editorconfig centraliza convenciones de formato y severidad de analizadores, y `dotnet format` las aplica."
---

# Comentarios, formato y legibilidad

## Objetivo

Distinguir los comentarios que documentan decisiones de los que repiten el código, y adoptar un formato consistente que haga visible la estructura del programa.

## Requisitos previos

Haber trabajado la lección de [nombres y funciones](/modulos/codigo-limpio/nombres-y-funciones). El formato empieza donde termina el buen nombre: si el código se explica solo, sobran los comentarios.

## Concepto

Un comentario no compensa un mal nombre. `// suma el total` arriba de `t += x` es ruido; lo que falta es que la variable se llame `total`. La regla práctica: los comentarios explican **por qué**, el código explica **qué**.

Hay comentarios legítimos:

- **Decisiones de diseño**: por qué se eligió una cola en memoria y no una tabla.
- **Restricciones externas**: un proveedor que devuelve fechas en UTC sin offset.
- **Workarounds**: un bug conocido de una librería, con enlace al issue.
- **Advertencias**: un método que no es thread-safe.
- **Documentación XML** en APIs públicas: `/// <summary>` describe el contrato y aparece en IntelliSense.

Y comentarios que conviene borrar: código comentado "por si acaso" (git conserva el historial), comentarios desactualizados que mienten, cabeceras con autor y fecha, `TODO` sin dueño ni ticket, y ruido obvio como `// fin del if`.

El formato no es cosmético. Un archivo con bloques separados por líneas en blanco, métodos cortos y una indentación estable se escanea en segundos. La consistencia importa más que la preferencia individual: si el equipo usa llaves en línea nueva, todos las usan igual. Un `.editorconfig` versionado y `dotnet format` convierten esa convención en algo automático.

Las guard clauses son la herramienta de formato con mayor impacto: validan y salen temprano, así el camino feliz queda alineado a la izquierda. En lugar de cuatro `if` anidados para comprobar que el pedido, el cliente y las líneas existen, se escribe una salida por cada caso inválido y la lógica queda en un solo nivel.

## Ejemplo mínimo

Antes, comentarios que repiten lo evidente y una cabecera inútil:

```csharp
// Clase que calcula el IVA
public class Iva
{
    // Método que calcula
    public decimal Calcular(decimal baseImponible)
    {
        // Multiplica por 1.21
        return baseImponible * 1.21m; // Devuelve el resultado
    }
}
```

Después, el código dice qué hace y el comentario conserva solo el dato que no se deduce:

```csharp
/// <summary>
/// Calcula el precio con IVA general del 21 %.
/// </summary>
public static class CalculadoraIva
{
    // La alícuota general surge del artículo 28 de la ley 23.349.
    private const decimal AlicuotaGeneral = 0.21m;

    public static decimal CalcularConIva(decimal baseImponible)
        => baseImponible * (1 + AlicuotaGeneral);
}
```

El nombre y la firma reemplazaron tres comentarios. El que quedó explica de dónde sale el 21 %, algo que el código no puede contar.

## Ejemplo aplicado

Un método de importación mezcla validación, parseo y persistencia, con comentarios que marcan bloques en lugar de extraerlos. La versión limpia revela la estructura y deja un comentario solo donde aporta contexto:

```csharp
public (int Importados, int Omitidos) Importar(string ruta)
{
    string[] lineas = File.ReadAllLines(ruta);
    if (lineas.Length == 0)
    {
        return (0, 0);
    }

    int omitidas = 0;
    foreach (string linea in lineas)
    {
        if (!TryParsearCliente(linea, out Cliente? cliente))
        {
            omitidas++;
            continue;
        }

        _repositorio.Agregar(cliente);
    }

    return (lineas.Length - omitidas, omitidas);
}

private static bool TryParsearCliente(string linea, out Cliente? cliente)
{
    // Formato heredado del sistema anterior: nombre;email;ciudad
    string[] partes = linea.Split(';');
    cliente = partes.Length == 3 ? new Cliente(partes[0], partes[1], partes[2]) : null;
    return cliente is not null;
}
```

```text
Importados: 2
Omitidos: 1
```

Los comentarios de bloque desaparecieron porque los nombres de los métodos los hacían innecesarios. El único que sobrevive documenta un formato heredado, que no se deduce de la firma.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Comentarios que repiten el código | Se documenta la mecánica en vez de la intención | Renombrar y borrar el comentario |
| Código comentado durante meses | Miedo a perderlo | Eliminarlo; el historial de git lo conserva |
| `// TODO` sin contexto | No hay responsable ni seguimiento | Crear un issue y referenciarlo |
| Anidamiento profundo con `else` | Se escribe el flujo completo de una vez | Usar guard clauses y salir temprano |
| Formato distinto en cada archivo | No hay convención compartida | Versionar `.editorconfig` y ejecutar `dotnet format` |
| Documentación XML desactualizada | Se cambió la firma y no el comentario | Revisar docs en el mismo pull request |
## Buenas prácticas

- Escribí el comentario solo cuando el código no pueda expresar el por qué.
- Documentá con XML únicamente las APIs públicas que consumen otros equipos.
- Dejá una línea en blanco entre bloques lógicos y evitá métodos de más de una pantalla.
- Automatizá el estilo con `.editorconfig`, analizadores y `dotnet format` en CI.

## Ejercicio

Refactorizá este método: eliminá los comentarios redundantes, aplicá guard clauses, extraé el cálculo y dejá un comentario solo donde aporte información que el código no tiene.

```csharp
public decimal CalcularDescuento(Pedido pedido)
{
    // Verificar pedido
    if (pedido != null && pedido.Lineas.Count > 0)
    {
        decimal total = 0;
        foreach (var linea in pedido.Lineas)
        {
            total += linea.Precio * linea.Cantidad;
        }

        // Si supera 100000 aplica 10%
        if (total > 100_000m) return total * 0.10m;
    }

    return 0;
}
```

<details>
<summary>Ver solución</summary>

```csharp
public decimal CalcularDescuento(Pedido pedido)
{
    ArgumentNullException.ThrowIfNull(pedido);

    decimal total = pedido.Lineas.Sum(linea => linea.Precio * linea.Cantidad);
    return total > UmbralDescuento ? total * PorcentajeDescuento : 0m;
}

// Regla comercial acordada con Finanzas en julio de 2026.
private const decimal UmbralDescuento = 100_000m;
private const decimal PorcentajeDescuento = 0.10m;
```
```csharp
var pedido = new Pedido(
    Lineas: [new LineaPedido(Precio: 60_000m, Cantidad: 2)]);

Console.WriteLine(CalcularDescuento(pedido));
```

```text
12000.0
```

El comentario que queda explica el origen de la regla, no la multiplicación. Las guard clauses y la consulta LINQ hicieron el resto.

</details>

## Resumen

- Los comentarios explican por qué; el código explica qué.
- El código comentado y los `TODO` sin seguimiento son deuda que conviene eliminar o registrar.
- El formato consistente reduce el tiempo de lectura y se automatiza con herramientas.
- Las guard clauses aplanan el flujo y hacen visible el camino feliz.
- La documentación XML se reserva para contratos públicos y se actualiza con el código.

## Siguiente paso

Con nombres claros, funciones chicas y un formato estable ya se lee mejor. En la próxima lección vas a ver cómo evitar duplicación sin caer en abstracciones prematuras con [DRY, KISS y YAGNI](/modulos/codigo-limpio/dry-kiss-yagni).
