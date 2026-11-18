---
slug: nombres-y-funciones
title: "Nombres y funciones chicas"
order: 1
estimatedMinutes: 15
summary: "Elegí nombres que revelen intención y dividí funciones grandes en pasos pequeños con una sola responsabilidad y menos parámetros."
quiz:
  - question: "¿Qué nombre comunica mejor la intención de un método que devuelve true si un pedido puede enviarse?"
    options:
      - "Check"
      - "PedidoOk"
      - "PuedeEnviarse"
      - "ValidarDatos"
    answer: 2
    explanation: "PuedeEnviarse describe una pregunta booleana y se lee como una condición del dominio. Check no dice qué se comprueba."
  - question: "¿Cuál es la señal más clara de que una función hace más de una cosa?"
    options:
      - "Tiene más de tres parámetros"
      - "Su nombre necesita la palabra Y para describirla"
      - "Devuelve un record en lugar de una tupla"
      - "Usa expresiones lambda"
    answer: 1
    explanation: "Si para nombrarla necesitás unir dos acciones con \"y\", probablemente sean dos funciones escondidas en una."
  - question: "¿Qué implica el principio de separación comando-consulta (CQS)?"
    options:
      - "Toda función debe devolver un valor"
      - "Una función modifica estado o devuelve información, pero no ambas cosas a la vez"
      - "Los comandos deben ser clases abstractas"
      - "Las consultas siempre deben ser asíncronas"
    answer: 1
    explanation: "CQS separa las operaciones que cambian estado de las que solo leen. Así el efecto de cada llamada es predecible."
  - question: "¿Por qué conviene evitar un parámetro booleano como ultimo en una función?"
    options:
      - "Porque los booleanos ocupan más memoria"
      - "Porque el sitio de llamada no explica qué significa true o false"
      - "Porque C# no permite parámetros booleanos"
      - "Porque impide usar métodos async"
    answer: 1
    explanation: "En la llamada se lee Procesar(pedido, true), que no comunica nada. Dos funciones con nombres claros expresan mejor la intención."
---

# Nombres y funciones chicas

## Objetivo

Elegir nombres que revelen la intención del código y descomponer funciones largas en pasos pequeños, legibles y con una única responsabilidad.

## Requisitos previos

Haber completado el módulo de testing, en especial el [ejercicio integrador](/modulos/testing/ejercicio-integrador). También conviene manejar clases, records, interfaces y expresiones lambda, porque los ejemplos usan esos recursos todo el tiempo.

## Concepto

Un nombre es la primera documentación del código. `CalcularTotalConImpuestos` se entiende sin abrir el cuerpo; `Calc` obliga a leer la implementación. Un buen nombre responde tres preguntas: qué hace, sobre qué lo hace y qué devuelve.

Nombrar bien exige descartar vicios frecuentes: **desinformación** (`listaClientes` para un `Dictionary`), **codificaciones** (`strNombre`, `iContador`; el tipo ya está en la firma), **ruido** (`DatosPedido`, `InfoPedido`), **abreviaturas crípticas** (`usr`, `px`) y nombres imposibles de buscar (`x1`, `tmp2`).

Para booleanos conviene una pregunta: `EstaVencido`, `TieneStock`, `PuedeEnviarse`. Para métodos, un verbo: `ReservarStock`, `CalcularDescuento`.

Una función chica hace una sola cosa, en un solo nivel de abstracción y sin efectos colaterales sorpresivos. "Chica" no es una cantidad exacta de líneas, pero si necesita más de dos pantallas o un comentario para explicar sus bloques, casi siempre se puede dividir. La prueba práctica: extraé el cuerpo a un método con un nombre honesto. Si el nombre sale claro y corto, era una función. Si necesitás un `Y`, eran dos.

La cantidad de parámetros también importa: cero a tres se leen bien y cuatro o más suelen indicar que falta un objeto que agrupe esos datos. Un booleano en la llamada es invisible (`Enviar(pedido, true)` no dice nada); mejor `EnviarConPrioridad(pedido)`. Y por la separación comando-consulta (CQS), una función que modifica estado no debería devolver información, ni una consulta debería modificar nada: `GuardarYDevolverId` mezcla ambas cosas y complica las pruebas.

## Ejemplo mínimo

Antes, un método con nombre opaco, parámetros sueltos y dos tareas mezcladas:

```csharp
public decimal Calc(List<LineaPedido> l, bool d, decimal u)
{
    decimal t = 0;
    foreach (var i in l)
    {
        t += i.Precio * i.Cantidad;
    }

    if (d)
    {
        t = t - (t * u);
    }

    return t;
}
```

Después, la intención aparece en la firma y el cálculo se apoya en pasos nombrados:

```csharp
public decimal CalcularTotal(
    IReadOnlyCollection<LineaPedido> lineas,
    decimal porcentajeDescuento)
{
    decimal subtotal = CalcularSubtotal(lineas);
    return AplicarDescuento(subtotal, porcentajeDescuento);
}

private static decimal CalcularSubtotal(IReadOnlyCollection<LineaPedido> lineas)
    => lineas.Sum(linea => linea.Precio * linea.Cantidad);

private static decimal AplicarDescuento(decimal subtotal, decimal porcentaje)
    => subtotal * (1 - porcentaje);
```

El booleano desapareció: un descuento del 0 % es la ausencia de descuento. La firma se lee sola y cada método tiene una razón para cambiar.

## Ejemplo aplicado

Un servicio confirma pedidos. La versión original hace validación, cálculo, stock y notificación en un solo bloque:

```csharp
public async Task<bool> Confirmar(Pedido pedido)
{
    if (pedido.Lineas.Count == 0) return false;
    foreach (var linea in pedido.Lineas)
    {
        if (linea.Cantidad <= 0) return false;
        if (await _stock.Obtener(linea.ProductoId) < linea.Cantidad) return false;
    }

    pedido.Total = pedido.Lineas.Sum(l => l.Precio * l.Cantidad);
    foreach (var linea in pedido.Lineas)
        await _stock.Descontar(linea.ProductoId, linea.Cantidad);

    await _correo.Enviar(pedido.ClienteEmail, "Pedido confirmado");
    pedido.Estado = EstadoPedido.Confirmado;
    return true;
}
```

La versión limpia conserva el mismo comportamiento, pero cada paso tiene nombre y se puede probar por separado:

```csharp
public async Task<ResultadoConfirmacion> ConfirmarAsync(Pedido pedido)
{
    if (!await _stock.HayStockAsync(pedido.Lineas))
    {
        return ResultadoConfirmacion.SinStock;
    }

    pedido.Total = pedido.Lineas.Sum(linea => linea.Precio * linea.Cantidad);
    await _stock.ReservarAsync(pedido.Lineas);
    pedido.Estado = EstadoPedido.Confirmado;
    await _notificador.NotificarConfirmacionAsync(pedido);
    return ResultadoConfirmacion.Confirmado;
}
```

`ConfirmarAsync` ahora se lee como una receta, los detalles viven en métodos privados y el resultado es explícito en lugar de un `bool` ambiguo.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Nombres como `data`, `info` o `temp` | Se nombra por comodidad, no por significado | Describir el contenido: `pedidosPendientes`, `saldoDisponible` |
| Una función de 80 líneas "porque es más rápido" | Se mezclan niveles de detalle | Extraer bloques a métodos privados con nombres de dominio |
| `ProcesarPedidoYEnviarCorreo` | Dos responsabilidades en una función | Separar en `ConfirmarPedido` y `NotificarCliente` |
| Booleanos posicionales en llamadas | El sitio de llamada pierde legibilidad | Dividir en dos métodos o usar un enum expresivo |

## Buenas prácticas

- Nombrá por intención y por dominio, no por tipo ni por estructura técnica.
- Mantené las funciones en un solo nivel de abstracción: no mezcles una regla de negocio con apertura de conexiones.
- Preferí 0 a 3 parámetros; si necesitás más, agrupá en un record con nombre propio.
- Evitá flags booleanos en APIs públicas: crean dos comportamientos escondidos en una firma.

## Ejercicio

Refactorizá este método aplicando nombres expresivos, guard clauses y extracción de funciones. El método debe devolver `true` solo si el cliente es mayor de edad, tiene crédito suficiente y el pedido no está vacío.

```csharp
public bool P(Cliente c, Pedido o)
{
    if (c != null)
    {
        if (c.Edad >= 18)
        {
            if (c.LimiteCredito - c.Deuda >= o.Total)
            {
                if (o.Lineas.Count > 0)
                {
                    return true;
                }
            }
        }
    }

    return false;
}
```

<details>
<summary>Ver solución</summary>

```csharp
public bool PuedeConfirmar(Cliente cliente, Pedido pedido)
    => EsMayorDeEdad(cliente)
        && TieneCreditoSuficiente(cliente, pedido.Total)
        && pedido.Lineas.Count > 0;

private static bool EsMayorDeEdad(Cliente cliente) => cliente.Edad >= 18;

private static bool TieneCreditoSuficiente(Cliente cliente, decimal total)
    => cliente.CreditoDisponible >= total;
```

```csharp
var cliente = new Cliente(Edad: 30, LimiteCredito: 100_000m, Deuda: 20_000m);
var pedido = new Pedido(Total: 45_000m, Lineas: [new LineaPedido(7, 2, 22_500m)]);

Console.WriteLine(PuedeConfirmar(cliente, pedido));
```

```text
True
```

La firma dice exactamente qué decide el método. Cada condición se puede probar y explicar por separado, y las guard clauses eliminaron el anidamiento innecesario.

</details>

## Resumen

- Un nombre revela intención, se pronuncia y se puede buscar.
- Las funciones chicas hacen una sola cosa en un solo nivel de abstracción.
- Pocos parámetros y sin booleanos posicionales producen llamadas legibles.
- Separar comandos de consultas vuelve previsible el efecto del código.

## Siguiente paso

Ya tenés nombres claros y funciones enfocadas. En la próxima lección vas a ver cuándo un [comentario o un formato](/modulos/codigo-limpio/comentarios-y-formato) ayudan de verdad y cuándo solo esconden un problema de diseño.
