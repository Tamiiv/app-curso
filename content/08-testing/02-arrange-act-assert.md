---
slug: arrange-act-assert
title: "Arrange, Act, Assert"
order: 2
estimatedMinutes: 12
summary: "Ordená cada test en tres bloques claros, elegí nombres descriptivos y evitá dependencias ocultas entre pruebas."
quiz:
  - question: "¿Qué representa la fase Act en el patrón Arrange, Act, Assert?"
    options:
      - "Preparar los datos y las dependencias"
      - "Ejecutar el comportamiento que se quiere probar"
      - "Verificar el resultado obtenido"
      - "Limpiar los recursos al finalizar"
    answer: 1
    explanation: "Arrange prepara el escenario, Act ejecuta una única acción sobre el sistema y Assert comprueba el resultado."
  - question: "¿Cuál es la principal ventaja de separar un test en tres bloques?"
    options:
      - "Hace que el test se ejecute más rápido"
      - "Permite entender qué se prepara, qué se ejecuta y qué se verifica"
      - "Elimina la necesidad de usar assertions"
      - "Obliga a que cada test tenga varias acciones"
    answer: 1
    explanation: "La separación es ante todo comunicativa: quien lee el test identifica el escenario, la acción y la expectativa sin reconstruirlos mentalmente."
  - question: "¿Qué test está mejor nombrado?"
    options:
      - "Test1"
      - "ProbarCarrito"
      - "Total_CarritoVacio_DevuelveCero"
      - "AssertTotal"
    answer: 2
    explanation: "El nombre indica el método, el escenario y el resultado esperado, de modo que un fallo se entiende sin abrir el código."
  - question: "¿Por qué conviene evitar más de un Act en el mismo test?"
    options:
      - "Porque xUnit no permite dos llamadas"
      - "Porque si falla la segunda acción no se sabe cuál de las dos causó el problema"
      - "Porque el test deja de compilar"
      - "Porque el runner ejecuta solo la primera línea"
    answer: 1
    explanation: "Con varias acciones en un mismo test, el diagnóstico se vuelve ambiguo. Es preferible dividirlo en tests independientes."
---

# Arrange, Act, Assert

## Objetivo

Estructurar cada prueba en tres bloques explícitos, escribir nombres que documenten el comportamiento y mantener los tests independientes para que un fallo señale con precisión dónde está el problema.

## Requisitos previos

Necesitás haber creado un proyecto xUnit y saber usar `[Fact]`, `[Theory]` y `Assert`. Si todavía no lo hiciste, repasá [xUnit: primeros pasos](/modulos/testing/xunit-primeros-pasos).

## Concepto

Arrange, Act, Assert es una convención para ordenar el cuerpo de un test:

| Fase | Pregunta que responde | Ejemplo |
|---|---|---|
| Arrange | ¿Qué necesito antes de actuar? | Crear el objeto, cargar datos, configurar un fake |
| Act | ¿Qué comportamiento estoy probando? | Llamar a un método, enviar una petición |
| Assert | ¿Qué debería haber pasado? | Comparar el resultado o verificar una excepción |

No es una regla del compilador ni una API. Es una forma de escritura que hace visible la intención. Cuando las tres fases están mezcladas, el lector tiene que reconstruir el escenario mentalmente y el fallo cuesta más de interpretar.

La fase Act debería tener una sola línea en la enorme mayoría de los casos. Si necesitás dos llamadas para llegar al resultado, probablemente estés probando un flujo completo; eso puede ser válido, pero conviene que el nombre lo diga. Si las dos llamadas son independientes, mejor separarlas en dos tests.

La fase Assert puede tener más de una verificación cuando todas describen el mismo resultado. Por ejemplo, después de crear un pedido podés comprobar el identificador, el estado y el total. Lo que no conviene es verificar cosas que no tienen relación entre sí: si falla una, no sabés si el problema es del cálculo o de la persistencia.

El nombre del test es parte del diseño. Un buen patrón es `Metodo_Escenario_ResultadoEsperado`. En `Total_CarritoVacio_DevuelveCero` se entiende el método bajo prueba, el dato relevante y la expectativa. En cambio, `Test1` o `ProbarCarrito` no aportan información cuando aparecen en el reporte de fallos.

Los comentarios `// Arrange`, `// Act` y `// Assert` son útiles mientras el test es largo, pero no deberían ser obligatorios en tests de cinco líneas. Si necesitás explicar demasiado, el test está haciendo demasiado.

## Ejemplo mínimo

Un servicio aplica un descuento fijo del 15 % a los productos en oferta. El test prepara el precio, ejecuta el cálculo y verifica el resultado.

```csharp
public sealed class Precios
{
    public decimal AplicarOferta(decimal precio) => precio * 0.85m;
}
```

```csharp
using Xunit;

public sealed class PreciosTests
{
    [Fact]
    public void AplicarOferta_PrecioDeMil_DevuelveOchocientosCincuenta()
    {
        // Arrange
        var precios = new Precios();
        decimal precio = 1_000m;

        // Act
        decimal resultado = precios.AplicarOferta(precio);

        // Assert
        Assert.Equal(850m, resultado);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 18 ms
```

El test no necesita saber cómo se calcula el descuento. Solo describe el contrato observable: mil pesos en oferta cuestan ochocientos cincuenta.

## Ejemplo aplicado

Un carrito permite agregar items y calcula el total. También rechaza cantidades menores o iguales a cero. Cada regla tiene su propio test, con un único Act.

```csharp
public sealed record Item(string Nombre, decimal Precio, int Cantidad);

public sealed class Carrito
{
    private readonly List<Item> _items = [];

    public void Agregar(Item item)
    {
        if (item.Cantidad <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(item), "La cantidad debe ser mayor que cero.");
        }

        _items.Add(item);
    }

    public decimal Total => _items.Sum(item => item.Precio * item.Cantidad);
}
```

```csharp
using Xunit;

public sealed class CarritoTests
{
    [Fact]
    public void Total_UnItemConCantidadTres_MultiplicaPrecioPorCantidad()
    {
        var carrito = new Carrito();
        carrito.Agregar(new Item("Teclado", 25_000m, 3));

        decimal total = carrito.Total;

        Assert.Equal(75_000m, total);
    }

    [Fact]
    public void Total_CarritoVacio_DevuelveCero()
    {
        var carrito = new Carrito();

        decimal total = carrito.Total;

        Assert.Equal(0m, total);
    }

    [Fact]
    public void Agregar_CantidadCero_LanzaArgumentOutOfRangeException()
    {
        var carrito = new Carrito();

        var excepcion = Assert.Throws<ArgumentOutOfRangeException>(
            () => carrito.Agregar(new Item("Mouse", 10_000m, 0)));

        Assert.Contains("cantidad", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1, 25_000)]
    [InlineData(2, 50_000)]
    [InlineData(4, 100_000)]
    public void Total_VariasCantidades_DevuelvePrecioPorCantidad(int cantidad, decimal esperado)
    {
        var carrito = new Carrito();
        carrito.Agregar(new Item("Monitor", 25_000m, cantidad));

        decimal total = carrito.Total;

        Assert.Equal(esperado, total);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 34 ms
```

El test de excepción tiene un Act implícito dentro de `Assert.Throws`, y el Assert principal verifica el tipo. La comprobación del mensaje es secundaria: si el texto cambia por una mejora de redacción, el test no debería romperse. Una alternativa más robusta es verificar solo el tipo de excepción o el nombre del parámetro.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Test difícil de leer | Se mezclan preparación, acción y verificación | Separar en bloques y dejar un Act claro |
| Un fallo no dice qué se rompió | El nombre es genérico como `TestTotal` | Usar `Metodo_Escenario_ResultadoEsperado` |
| El test falla de forma intermitente | Comparte estado con otro test | Crear los datos dentro del test y evitar estáticos mutables |
| Se prueban varias acciones no relacionadas | El test hace de más | Dividir en tests independientes |
| El test cambia ante cualquier refactor | Verifica detalles internos en vez de comportamiento | Assert sobre resultados observables y contratos públicos |
| Se repite el mismo Arrange en diez tests | Falta un helper o un fixture | Extraer un método de creación, sin esconder datos relevantes |

## Buenas prácticas

- Dejá una línea en blanco entre Arrange, Act y Assert: el ritmo visual ayuda más que los comentarios.
- Un test, un comportamiento. Si el nombre necesita una "y", probablemente sean dos tests.
- Usá datos con significado para el negocio, no valores arbitrarios como `123` o `999`.
- Verificá el resultado observable, no la secuencia exacta de llamadas internas.
- Si un test es difícil de preparar, suele ser una señal de acoplamiento en el diseño; escuchala.

## Ejercicio

Escribí tests para una clase `Cuenta` con `Depositar(decimal monto)` y `Retirar(decimal monto)`. La cuenta empieza en cero, no permite montos negativos y rechaza retiros mayores al saldo con `InvalidOperationException`. Aplicá Arrange, Act, Assert y nombres descriptivos. Cubrí el saldo inicial, un depósito, un retiro válido y un retiro sin fondos.

<details>
<summary>Ver solución</summary>

```csharp
public sealed class Cuenta
{
    public decimal Saldo { get; private set; }

    public void Depositar(decimal monto)
    {
        if (monto <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser positivo.");
        }

        Saldo += monto;
    }

    public void Retirar(decimal monto)
    {
        if (monto <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser positivo.");
        }

        if (monto > Saldo)
        {
            throw new InvalidOperationException("Saldo insuficiente.");
        }

        Saldo -= monto;
    }
}
```

```csharp
using Xunit;

public sealed class CuentaTests
{
    [Fact]
    public void Saldo_CuentaNueva_EsCero()
    {
        var cuenta = new Cuenta();

        Assert.Equal(0m, cuenta.Saldo);
    }

    [Fact]
    public void Depositar_MilPesos_IncrementaElSaldo()
    {
        var cuenta = new Cuenta();

        cuenta.Depositar(1_000m);

        Assert.Equal(1_000m, cuenta.Saldo);
    }

    [Fact]
    public void Retirar_ConSaldoSuficiente_ReduceElSaldo()
    {
        var cuenta = new Cuenta();
        cuenta.Depositar(1_000m);

        cuenta.Retirar(400m);

        Assert.Equal(600m, cuenta.Saldo);
    }

    [Fact]
    public void Retirar_MasQueElSaldo_LanzaInvalidOperationException()
    {
        var cuenta = new Cuenta();
        cuenta.Depositar(100m);

        Assert.Throws<InvalidOperationException>(() => cuenta.Retirar(101m));
        Assert.Equal(100m, cuenta.Saldo);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 27 ms
```

En el último test hay dos assertions, pero ambas describen el mismo resultado: la operación se rechaza y el saldo no cambia. Eso es aceptable porque forman una sola expectativa coherente.

</details>

## Resumen

- Arrange prepara, Act ejecuta una acción y Assert verifica un resultado.
- Un test debería expresar un único comportamiento y un único motivo de fallo.
- El nombre `Metodo_Escenario_ResultadoEsperado` documenta el contrato.
- Los tests deben ser independientes y crear su propio estado.
- Si preparar el test es complicado, el problema suele estar en el diseño del código.

## Siguiente paso

Ya tenés tests ordenados, pero no todos prueban lo mismo ni cuestan lo mismo. En la próxima lección vas a distinguir [pruebas unitarias y de integración](/modulos/testing/unitarios-vs-integracion) y elegir cuál usar en cada caso.
