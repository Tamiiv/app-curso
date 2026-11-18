---
slug: xunit-primeros-pasos
title: "xUnit: primeros pasos"
order: 1
estimatedMinutes: 15
summary: "Creá tu primer proyecto de pruebas con xUnit, escribí tests con [Fact] y [Theory], y ejecutalos con dotnet test desde la terminal."
quiz:
  - question: "¿Qué atributo marca un método de prueba que no recibe parámetros en xUnit?"
    options:
      - "[Fact]"
      - "[Test]"
      - "[TestMethod]"
      - "[Theory]"
    answer: 0
    explanation: "[Fact] identifica un test que siempre se ejecuta igual. [Theory] se usa cuando el test recibe datos desde [InlineData] u otra fuente."
  - question: "¿Qué assertion usás para comprobar que una acción lanza una excepción?"
    options:
      - "Assert.Equal"
      - "Assert.Throws<T>"
      - "Assert.True"
      - "Assert.Contains"
    answer: 1
    explanation: "Assert.Throws<T> ejecuta el delegate y falla si no se lanza exactamente la excepción indicada. Para código asincrónico existe Assert.ThrowsAsync<T>."
  - question: "¿Qué comando descubre y ejecuta las pruebas de una solución?"
    options:
      - "dotnet run"
      - "dotnet build"
      - "dotnet test"
      - "dotnet publish"
    answer: 2
    explanation: "dotnet test restaura, compila y ejecuta los proyectos de prueba usando el runner configurado, sin necesidad de arrancar la aplicación."
  - question: "¿Qué permite combinar [Theory] con varios [InlineData]?"
    options:
      - "Ejecutar el mismo test con distintos conjuntos de datos"
      - "Ejecutar los tests en paralelo"
      - "Omitir los tests que fallan"
      - "Generar el código de producción automáticamente"
    answer: 0
    explanation: "Cada [InlineData] produce una ejecución independiente del mismo método con los argumentos indicados, y todas se reportan por separado."
---

# xUnit: primeros pasos

## Objetivo

Crear un proyecto de pruebas con xUnit, escribir tests con `[Fact]` y `[Theory]`, usar las assertions principales y ejecutar todo con `dotnet test` para obtener una señal rápida y confiable sobre tu código.

## Requisitos previos

Necesitás el SDK de .NET 10, saber crear proyectos con la CLI y manejar clases y métodos en C#. También conviene haber terminado el [ejercicio integrador de EF Core](/modulos/ef-core/ejercicio-integrador), porque ahí aparece el tipo de lógica de negocio que vas a querer cubrir con pruebas.

## Concepto

Un test automatizado es un método que prepara un escenario, ejecuta una parte del sistema y verifica un resultado. Si el resultado no coincide con lo esperado, el test falla y te avisa. xUnit es el framework de pruebas más usado en .NET: descubre los métodos marcados con atributos, los ejecuta y resume cuántos pasaron.

El comando `dotnet new xunit` genera un proyecto con las piezas necesarias:

| Pieza | Rol |
|---|---|
| `xunit` | Biblioteca con atributos y assertions |
| `xunit.runner.visualstudio` | Integración con `dotnet test` y Visual Studio |
| `Microsoft.NET.Test.Sdk` | Infraestructura común de pruebas de .NET |
| `coverlet.collector` | Recolección de cobertura, que vas a usar más adelante |

Un proyecto de prueba referencia al proyecto que quiere probar. No al revés: el código de producción nunca debe depender de los tests.

xUnit crea una instancia nueva de la clase de prueba para cada método. Eso significa que los campos de la clase no se comparten entre tests y que no podés guardar estado de un test para usarlo en otro. Esa decisión evita gran parte de los problemas de aislamiento.

Los dos atributos básicos son:

- `[Fact]`: el test no recibe parámetros y se ejecuta una sola vez.
- `[Theory]`: el test recibe parámetros y se ejecuta una vez por cada `[InlineData]`, `[MemberData]` o `[ClassData]`.

Las assertions viven en la clase estática `Assert`. Las más frecuentes son `Equal`, `NotEqual`, `True`, `False`, `Null`, `NotNull`, `Contains`, `Empty`, `Single`, `Throws<T>` y `ThrowsAsync<T>`. Cada assertion falla con un mensaje que muestra el valor esperado y el obtenido, lo que acelera el diagnóstico.

## Ejemplo mínimo

Supongamos una calculadora con dos operaciones y una regla: dividir por cero lanza `DivideByZeroException`.

```csharp
public sealed class Calculadora
{
    public int Sumar(int a, int b) => a + b;

    public decimal Dividir(decimal dividendo, decimal divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException("No se puede dividir por cero.");
        }

        return dividendo / divisor;
    }
}
```

El proyecto de pruebas se crea y se vincula al código de producción:

```bash
dotnet new xunit -n Tienda.Tests
dotnet add Tienda.Tests reference Tienda/Tienda.csproj
dotnet sln add Tienda.Tests
```

Los tests quedan así:

```csharp
using Xunit;

public sealed class CalculadoraTests
{
    [Fact]
    public void Sumar_DosEnteros_DevuelveLaSuma()
    {
        var calculadora = new Calculadora();

        int resultado = calculadora.Sumar(2, 3);

        Assert.Equal(5, resultado);
    }

    [Fact]
    public void Dividir_PorCero_LanzaExcepcion()
    {
        var calculadora = new Calculadora();

        Assert.Throws<DivideByZeroException>(() => calculadora.Dividir(10, 0));
    }

    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(-2, 3, 1)]
    [InlineData(0, 0, 0)]
    public void Sumar_VariosCasos_DevuelveLaSuma(int a, int b, int esperado)
    {
        var calculadora = new Calculadora();

        Assert.Equal(esperado, calculadora.Sumar(a, b));
    }
}
```

Al ejecutar `dotnet test`:

```text
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 42 ms
```

Hay cinco ejecuciones porque los dos `[Fact]` cuentan uno cada uno y el `[Theory]` cuenta tres, una por cada `[InlineData]`.

## Ejemplo aplicado

Una tienda calcula el total de un carrito y aplica un descuento del 10 % cuando el subtotal supera los 50 000 pesos. La lógica es chica, pero tiene un borde que conviene cubrir: el límite exacto.

```csharp
public sealed record ItemCarrito(string Nombre, decimal Precio, int Cantidad);

public sealed class Carrito
{
    private readonly List<ItemCarrito> _items = [];

    public void Agregar(ItemCarrito item) => _items.Add(item);

    public decimal Subtotal => _items.Sum(item => item.Precio * item.Cantidad);

    public decimal Total => Subtotal > 50_000m ? Subtotal * 0.90m : Subtotal;
}
```

```csharp
using Xunit;

public sealed class CarritoTests
{
    [Fact]
    public void Total_SubtotalBajoElLimite_NoAplicaDescuento()
    {
        var carrito = new Carrito();
        carrito.Agregar(new ItemCarrito("Teclado", 20_000m, 1));

        Assert.Equal(20_000m, carrito.Total);
    }

    [Fact]
    public void Total_SubtotalSobreElLimite_AplicaDiezPorCiento()
    {
        var carrito = new Carrito();
        carrito.Agregar(new ItemCarrito("Monitor", 60_000m, 1));

        Assert.Equal(54_000m, carrito.Total);
    }

    [Theory]
    [InlineData(50_000, 50_000)]
    [InlineData(50_001, 45_000.9)]
    public void Total_EnElLimite_RespetaLaRegla(decimal subtotal, decimal esperado)
    {
        var carrito = new Carrito();
        carrito.Agregar(new ItemCarrito("Producto", subtotal, 1));

        Assert.Equal(esperado, carrito.Total);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 38 ms
```

El caso de 50 000 exactos no recibe descuento porque la condición usa `>`. Ese borde suele olvidarse y es justamente donde aparecen los errores de negocio. El segundo caso usa 50 001 para demostrar que un peso más ya activa la regla.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `dotnet test` no encuentra ningún test | La clase o el método no son `public`, o falta el atributo | Marcar la clase y el método como `public` y usar `[Fact]` o `[Theory]` |
| El test pasa aunque el código esté mal | Falta una assertion o se compara el valor consigo mismo | Verificar el resultado con `Assert.Equal` u otra assertion concreta |
| `async void` en un test | xUnit no puede esperar una tarea que no devuelve nada | Declarar `public async Task Metodo()` y usar `await` |
| Un test afecta al siguiente | Se comparte estado estático o una base de datos real | Evitar estáticos mutables y crear el estado dentro de cada test |
| `Assert.Equal` falla por decimales | Se comparan `double` con igualdad exacta | Usar `Assert.Equal(esperado, actual, precision)` o `decimal` |
| El proyecto de pruebas no compila | Falta la referencia al proyecto de producción | Ejecutar `dotnet add reference` y revisar el `.csproj` |

## Buenas prácticas

- Nombrá los tests con el patrón `Metodo_Escenario_ResultadoEsperado`; el nombre debe explicar qué se rompe si falla.
- Escribí tests chicos y con una sola razón para fallar. Un test con diez assertions mezcladas es difícil de diagnosticar.
- Cubrí primero los bordes: cero, negativos, límites, listas vacías y valores nulos.
- Usá `[Theory]` cuando el mismo comportamiento se repite con datos distintos; evita copiar y pegar tests casi idénticos.
- Ejecutá `dotnet test` antes de cada commit. Un test que no se ejecuta nunca no protege nada.

## Ejercicio

Creá un proyecto xUnit para una clase `Termometro` con dos métodos: `ConvertirACelsius(decimal fahrenheit)` y `EsCongelante(decimal celsius)`, que devuelve `true` cuando la temperatura es menor o igual a 0. Escribí al menos un `[Fact]` por método y un `[Theory]` con tres temperaturas de prueba.

<details>
<summary>Ver solución</summary>

```csharp
public sealed class Termometro
{
    public decimal ConvertirACelsius(decimal fahrenheit) => (fahrenheit - 32m) * 5m / 9m;

    public bool EsCongelante(decimal celsius) => celsius <= 0m;
}
```

```csharp
using Xunit;

public sealed class TermometroTests
{
    [Fact]
    public void ConvertirACelsius_TreintaYDosFahrenheit_DevuelveCero()
    {
        var termometro = new Termometro();

        Assert.Equal(0m, termometro.ConvertirACelsius(32m));
    }

    [Fact]
    public void EsCongelante_CeroGrados_DevuelveTrue()
    {
        var termometro = new Termometro();

        Assert.True(termometro.EsCongelante(0m));
    }

    [Theory]
    [InlineData(-10, true)]
    [InlineData(0, true)]
    [InlineData(25, false)]
    public void EsCongelante_VariasTemperaturas_DevuelveLoEsperado(decimal celsius, bool esperado)
    {
        var termometro = new Termometro();

        Assert.Equal(esperado, termometro.EsCongelante(celsius));
    }
}
```

```text
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 31 ms
```

</details>

## Resumen

- xUnit descubre tests por atributos: `[Fact]` para casos únicos y `[Theory]` con `[InlineData]` para datos.
- Cada test recibe una instancia nueva de la clase, lo que ayuda al aislamiento.
- `Assert` concentra las verificaciones y muestra esperado contra obtenido cuando falla.
- `dotnet test` restaura, compila y ejecuta toda la suite desde la terminal.
- Probar los bordes desde el principio evita sorpresas en producción.

## Siguiente paso

Ya sabés ejecutar tests, pero un test desordenado cuesta mantenerlo. En la próxima lección vas a ordenar cada prueba con el patrón [Arrange, Act, Assert](/modulos/testing/arrange-act-assert).
