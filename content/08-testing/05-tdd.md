---
slug: tdd
title: "TDD: test first"
order: 5
estimatedMinutes: 15
summary: "Aplicá el ciclo rojo-verde-refactor para diseñar código desde los tests, avanzar en pasos chicos y mantener la suite siempre en verde."
quiz:
  - question: "¿Cuál es el orden correcto del ciclo TDD?"
    options:
      - "Verde, rojo, refactor"
      - "Rojo, verde, refactor"
      - "Refactor, rojo, verde"
      - "Rojo, refactor, verde"
    answer: 1
    explanation: "Primero escribís un test que falla (rojo), después el mínimo código para que pase (verde) y por último mejorás el diseño sin cambiar el comportamiento."
  - question: "¿Qué debería hacer el primer test de una funcionalidad nueva?"
    options:
      - "Fallar por una razón significativa, porque el comportamiento todavía no existe"
      - "Pasar de inmediato para confirmar que el entorno funciona"
      - "Probar todos los casos borde a la vez"
      - "Verificar la implementación interna"
    answer: 0
    explanation: "El rojo demuestra que el test puede detectar la ausencia del comportamiento. Si pasa de entrada, no estás probando nada nuevo."
  - question: "¿Qué significa escribir el mínimo código para pasar el test?"
    options:
      - "Implementar todas las validaciones futuras"
      - "Escribir solo lo necesario para que el test actual pase, sin anticipar requisitos"
      - "Copiar la solución de otro proyecto"
      - "Omitir el refactor"
    answer: 1
    explanation: "Avanzar en pasos pequeños mantiene el foco y evita código especulativo. Los siguientes tests guían las siguientes reglas."
  - question: "¿Para qué sirve la fase de refactor?"
    options:
      - "Para agregar nuevas funcionalidades"
      - "Para mejorar el diseño manteniendo los tests en verde"
      - "Para borrar los tests que ya pasan"
      - "Para cambiar el comportamiento esperado"
    answer: 1
    explanation: "El refactor elimina duplicación y mejora nombres o estructura sin alterar el comportamiento; los tests siguen siendo la red de seguridad."
---

# TDD: test first

## Objetivo

Aplicar el ciclo rojo-verde-refactor para diseñar una clase desde sus tests, avanzar en incrementos pequeños y usar la suite como red de seguridad durante las mejoras.

## Requisitos previos

Necesitás escribir tests con xUnit, ordenarlos con Arrange, Act, Assert y saber cuándo usar un doble. Si querés repasar el aislamiento, mirá [Mocks, stubs y fakes](/modulos/testing/dobles-de-prueba).

## Concepto

TDD significa Test-Driven Development, o desarrollo guiado por pruebas. No es "escribir tests después" ni "escribir todos los tests antes". Es un ciclo corto que se repite muchas veces:

1. Rojo: escribís un test para el siguiente comportamiento y lo ves fallar.
2. Verde: escribís el código mínimo para que pase.
3. Refactor: mejorás el diseño sin cambiar el comportamiento y verificás que todo siga verde.

El rojo importa porque demuestra que el test es capaz de detectar la falla. Un test que nace en verde no prueba que el comportamiento exista: puede estar mal escrito o no estar ejecutándose. Verlo fallar por la razón correcta te da confianza.

El verde importa porque te obliga a avanzar en pasos pequeños. Si escribís tres reglas nuevas y el test falla, no sabés cuál de las tres está mal. Si escribís una y falla, el espacio de búsqueda es mínimo.

El refactor importa porque el código que hace pasar un test no siempre es el mejor diseño. TDD no reemplaza el criterio de diseño: lo alimenta. La duplicación que aparece entre el segundo y el tercer test es la señal para extraer una abstracción.

TDD también es una herramienta de descubrimiento. Antes de escribir código te obliga a responder preguntas incómodas: ¿qué recibe el método?, ¿qué devuelve?, ¿qué pasa con un valor inválido?, ¿quién es el responsable de validar? Muchas veces el diseño cambia antes de la primera línea de producción.

El ciclo debería durar minutos, no horas. Si un test es difícil de escribir, probablemente la unidad tenga demasiadas responsabilidades. Ese dolor es información de diseño.

## Ejemplo mínimo

Una cuenta bancaria empieza con saldo cero. El primer test describe exactamente eso.

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
}
```

Al ejecutar `dotnet test`, no compila porque `Cuenta` no existe. Ese es el rojo. La implementación mínima es una clase con una propiedad en cero:

```csharp
public sealed class Cuenta
{
    public decimal Saldo { get; private set; }
}
```

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 15 ms
```

Verde. No agregues `Depositar` todavía: no hay un test que lo pida. El siguiente ciclo empieza con un test que falla porque el método no existe.

## Ejemplo aplicado

El siguiente requisito es depositar. El test se escribe antes de la implementación.

```csharp
[Fact]
public void Depositar_MilPesos_IncrementaElSaldo()
{
    var cuenta = new Cuenta();

    cuenta.Depositar(1_000m);

    Assert.Equal(1_000m, cuenta.Saldo);
}
```

Rojo: `Cuenta` no tiene `Depositar`. La implementación mínima:

```csharp
public void Depositar(decimal monto) => Saldo += monto;
```

Verde. Ahora el requisito de retirar:

```csharp
[Fact]
public void Retirar_ConSaldoSuficiente_ReduceElSaldo()
{
    var cuenta = new Cuenta();
    cuenta.Depositar(1_000m);

    cuenta.Retirar(400m);

    Assert.Equal(600m, cuenta.Saldo);
}
```

```csharp
public void Retirar(decimal monto) => Saldo -= monto;
```

Verde. Después aparece la regla de negocio: no se puede retirar más que el saldo.

```csharp
[Fact]
public void Retirar_MasQueElSaldo_LanzaInvalidOperationException()
{
    var cuenta = new Cuenta();
    cuenta.Depositar(100m);

    Assert.Throws<InvalidOperationException>(() => cuenta.Retirar(101m));
    Assert.Equal(100m, cuenta.Saldo);
}
```

Rojo. Implementación mínima para pasar:

```csharp
public void Retirar(decimal monto)
{
    if (monto > Saldo)
    {
        throw new InvalidOperationException("Saldo insuficiente.");
    }

    Saldo -= monto;
}
```

Verde. Ahora el refactor: las validaciones de montos negativos y la duplicación de asignaciones se pueden ordenar. Este es el código final después de varios ciclos:

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

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Depositar_MontoNoPositivo_LanzaArgumentOutOfRangeException(decimal monto)
    {
        var cuenta = new Cuenta();

        Assert.Throws<ArgumentOutOfRangeException>(() => cuenta.Depositar(monto));
    }
}
```

```text
Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 28 ms
```

El último `[Theory]` también se escribió antes de la validación. Agregar dos casos borde de una vez es aceptable porque describen la misma regla: el monto debe ser positivo.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El test inicial pasa sin escribir código | El comportamiento ya existía o el test no lo verifica | Comprobar que el test falla por la razón esperada antes de implementar |
| Se escriben diez tests antes de implementar | Se pierde el ciclo corto y el diagnóstico | Avanzar de a un comportamiento por vez |
| El test verifica detalles internos | Se acopla a la implementación | Assert sobre resultados y contratos públicos |
| Se salta el refactor | La presión por avanzar deja duplicación | Reservar unos minutos para mejorar el diseño en verde |
| El test es enorme y prueba un flujo completo | La unidad tiene demasiadas responsabilidades | Dividir en tests más chicos o rediseñar la clase |
| TDD se convierte en escribir tests triviales | Se prueban getters o setters sin valor | Empezar por el comportamiento que puede fallar |

## Buenas prácticas

- Mantené el ciclo en minutos: un test, una implementación mínima, un refactor pequeño.
- Antes de empezar, escribí una lista de comportamientos esperados y elegí el más simple.
- Hacé fallar el test por la razón correcta; leé el mensaje de error.
- No agregues código que ningún test pida; el código especulativo se vuelve deuda.
- Refactorizá con la suite en verde y volvé a ejecutarla después de cada cambio.

## Ejercicio

Aplicá TDD para construir una clase `Contador` con tres comportamientos: empieza en cero, `Incrementar()` suma uno y `Reiniciar()` vuelve a cero. Escribí un test por comportamiento, implementá el mínimo necesario en cada paso y terminá con un refactor que elimine duplicación. Mostrá el orden de los ciclos.

<details>
<summary>Ver solución</summary>

Ciclo 1, rojo:

```csharp
[Fact]
public void Valor_ContadorNuevo_EsCero()
{
    var contador = new Contador();

    Assert.Equal(0, contador.Valor);
}
```

Verde:

```csharp
public sealed class Contador
{
    public int Valor { get; private set; }
}
```

Ciclo 2, rojo:

```csharp
[Fact]
public void Incrementar_UnaVez_SumaUno()
{
    var contador = new Contador();

    contador.Incrementar();

    Assert.Equal(1, contador.Valor);
}
```

Verde:

```csharp
public void Incrementar() => Valor++;
```

Ciclo 3, rojo:

```csharp
[Fact]
public void Reiniciar_ContadorConValor_VuelveACero()
{
    var contador = new Contador();
    contador.Incrementar();
    contador.Incrementar();

    contador.Reiniciar();

    Assert.Equal(0, contador.Valor);
}
```

Verde:

```csharp
public void Reiniciar() => Valor = 0;
```

Código final:

```csharp
public sealed class Contador
{
    public int Valor { get; private set; }

    public void Incrementar() => Valor++;

    public void Reiniciar() => Valor = 0;
}
```

```csharp
using Xunit;

public sealed class ContadorTests
{
    [Fact]
    public void Valor_ContadorNuevo_EsCero()
    {
        Assert.Equal(0, new Contador().Valor);
    }

    [Fact]
    public void Incrementar_UnaVez_SumaUno()
    {
        var contador = new Contador();

        contador.Incrementar();

        Assert.Equal(1, contador.Valor);
    }

    [Fact]
    public void Reiniciar_ContadorConValor_VuelveACero()
    {
        var contador = new Contador();
        contador.Incrementar();
        contador.Incrementar();

        contador.Reiniciar();

        Assert.Equal(0, contador.Valor);
    }
}
```

```text
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 19 ms
```

En este caso el refactor es mínimo porque la clase ya es simple. Lo importante es el orden: cada test apareció antes de su implementación y se vio fallar.

</details>

## Resumen

- TDD alterna rojo, verde y refactor en ciclos cortos.
- El test debe fallar primero para demostrar que detecta la ausencia del comportamiento.
- La implementación mínima evita anticipar requisitos que todavía no existen.
- El refactor mejora el diseño con la red de seguridad de los tests.
- TDD es una herramienta de diseño, no solo una técnica de cobertura.

## Siguiente paso

La lógica ya está cubierta, pero una API puede fallar en el ruteo, la serialización o la inyección de dependencias. En la próxima lección vas a probar la aplicación completa con [WebApplicationFactory](/modulos/testing/webapplicationfactory).
