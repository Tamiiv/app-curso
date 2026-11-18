---
slug: cobertura-calidad
title: "Cobertura y calidad"
order: 8
estimatedMinutes: 12
summary: "Medí cobertura con coverlet, interpretá el número sin convertirlo en una meta y mejorá la calidad con tests que documentan comportamiento."
quiz:
  - question: "¿Qué mide la cobertura de código?"
    options:
      - "Cuántos errores quedan en el código"
      - "Qué porcentaje del código se ejecutó durante los tests"
      - "La velocidad de la suite"
      - "Cuántos tests existen"
    answer: 1
    explanation: "La cobertura indica líneas, ramas o métodos ejecutados. No indica si las aserciones son buenas ni si se cubren los casos límite."
  - question: "¿Cuál es un uso razonable de la cobertura?"
    options:
      - "Exigir 100 % en todo el proyecto"
      - "Detectar zonas críticas sin probar y revisar tendencias"
      - "Reemplazar el code review"
      - "Aumentar el número con tests vacíos"
    answer: 1
    explanation: "La cobertura sirve como señal para encontrar huecos, no como certificado de calidad ni como meta absoluta."
  - question: "¿Qué práctica mejora más la calidad que subir el porcentaje?"
    options:
      - "Agregar tests sin aserciones"
      - "Probar casos límite, errores y reglas de negocio con aserciones claras"
      - "Ejecutar los tests una sola vez"
      - "Ocultar los tests lentos"
    answer: 1
    explanation: "Un test vale por lo que verifica. Cubrir caminos de error y límites encuentra defectos que la cobertura alta no garantiza."
  - question: "¿Qué tipo de cobertura detecta mejor las ramas no probadas de un if?"
    options:
      - "Cobertura de líneas"
      - "Cobertura de ramas"
      - "Cobertura de archivos"
      - "Cobertura de proyectos"
    answer: 1
    explanation: "La cobertura de ramas exige que se ejecuten ambos caminos de cada condición, no solo la línea."
---

# Cobertura y calidad

## Objetivo

Generar un informe de cobertura con coverlet, interpretar sus números con criterio y mejorar la calidad de la suite con casos límite y aserciones significativas.

## Requisitos previos

Haber leído [Pruebas de APIs](/modulos/testing/pruebas-apis). Necesitás una solución con al menos un proyecto de tests xUnit.

## Concepto

La cobertura responde una pregunta concreta: **¿qué código se ejecutó cuando corrieron los tests?** Se mide en líneas, ramas y métodos. No responde si el resultado era el correcto: eso lo hacen las aserciones.

Hay tres tipos habituales:

| Tipo | Qué mide | Ejemplo |
|---|---|---|
| Líneas | Líneas ejecutadas | Un método recorrido completo |
| Ramas | Caminos de cada condición | El `if` verdadero y el falso |
| Métodos | Métodos invocados | Un handler que nunca se llamó |

Un ejemplo clásico de cobertura engañosa:

```csharp
public static decimal Descuento(decimal total, bool clienteFrecuente)
{
    return clienteFrecuente ? total * 0.9m : total;
}
```

Un test que solo llama con `clienteFrecuente: true` cubre una línea y puede mostrar 100 % de líneas, pero deja la rama del cliente normal sin probar. La cobertura de ramas lo detecta.

Los números deben leerse por zona, no por proyecto entero:

- **Alta cobertura en dominio:** valiosa, porque ahí viven las reglas.
- **Baja cobertura en configuración:** normal, no todo se prueba.
- **Alta cobertura con pocas aserciones:** sospechosa.
- **Baja cobertura en un módulo nuevo:** señal para revisar antes de seguir.

Una meta arbitraria como "90 % obligatorio" empuja a escribir tests para el número, no para el comportamiento. Mejor: exigir que las reglas de negocio y los caminos de error estén cubiertos, y usar la cobertura como termómetro.

## Ejemplo mínimo

Con coverlet, la cobertura se genera al correr los tests:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

El resultado queda en `TestResults/<guid>/coverage.cobertura.xml`. Para un informe legible:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html
```

Se abre `coveragereport/index.html` y se navega por proyecto, clase y línea.

Para excluir código que no tiene sentido medir, se usa un atributo o configuración:

```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public static class Program;
```

También se puede configurar un umbral en el archivo de tests:

```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <Threshold>70</Threshold>
  <ThresholdType>line</ThresholdType>
  <ThresholdStat>total</ThresholdStat>
</PropertyGroup>
```

Con `Threshold`, la build falla si la cobertura baja del mínimo. Conviene aplicarlo a un módulo crítico, no a todo el repositorio desde el primer día.

## Ejemplo aplicado

Una suite que de verdad encuentra errores prueba los bordes:

```csharp
public sealed class DescuentoTests
{
    [Theory]
    [InlineData(100, true, 90)]
    [InlineData(100, false, 100)]
    [InlineData(0, true, 0)]
    [InlineData(50.5, true, 45.45)]
    public void AplicaDescuento(decimal total, bool frecuente, decimal esperado)
    {
        var resultado = Descuento(total, frecuente);

        Assert.Equal(esperado, resultado, precision: 2);
    }

    [Fact]
    public void TotalNegativoLanzaExcepcion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Descuento(-1, true));
    }

    private static decimal Descuento(decimal total, bool frecuente)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        return frecuente ? total * 0.9m : total;
    }
}
```

```text
Passed! - Failed: 0, Passed: 5, Skipped: 0
```

Los `InlineData` cubren las dos ramas, el borde en cero y un decimal. El test de excepción cubre el camino de error. Esa es la calidad que la cobertura por sí sola no garantiza.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| 100 % de cobertura y bugs en producción | Solo se probó el camino feliz | Agregar casos límite, errores y ramas |
| Tests escritos para subir el número | La meta es el porcentaje | Escribir tests con aserciones de comportamiento |
| El umbral bloquea todo el CI | Se aplicó a código generado o configuración | Excluir lo que no se mide y aplicar umbrales por módulo |
| La cobertura baja sin motivo | Se agregó código sin tests | Revisar el informe antes de mergear |
| Los tests son lentos y frágiles | Tests de integración para todo | Reservar integración para los flujos clave |
| El informe no incluye el proyecto | Falta el paquete `coverlet.collector` | Agregarlo al proyecto de tests |

## Buenas prácticas

- Medí cobertura, pero decidí con casos de uso.
- Exigí cobertura alta en dominio y en validaciones, no en `Program.cs`.
- Revisá la tendencia entre pull requests, no solo el número absoluto.
- Un test sin aserción no aporta: no lo agregues para inflar el informe.
- Documentá qué zonas quedan fuera y por qué.

## Ejercicio

Tomá una clase de tu proyecto con al menos dos ramas y un camino de error. Generá el informe de cobertura, identificá una rama sin probar y escribí el test que la cubre con al menos una aserción significativa.

<details>
<summary>Ver solución</summary>

```csharp
public sealed class PrioridadTests
{
    [Theory]
    [InlineData(1, "Baja")]
    [InlineData(3, "Media")]
    [InlineData(5, "Alta")]
    public void ClasificaPrioridad(int prioridad, string esperada)
    {
        Assert.Equal(esperada, Clasificar(prioridad));
    }

    [Fact]
    public void PrioridadFueraDeRangoLanzaExcepcion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Clasificar(6));
    }

    private static string Clasificar(int prioridad) => prioridad switch
    {
        < 1 or > 5 => throw new ArgumentOutOfRangeException(nameof(prioridad)),
        <= 2 => "Baja",
        <= 4 => "Media",
        _ => "Alta"
    };
}
```

```text
Passed! - Failed: 0, Passed: 4, Skipped: 0
```

Antes del test faltaba la rama `> 5`. El informe de ramas lo mostraba y el test la cubre ahora con una aserción real.

</details>

## Resumen

- La cobertura mide ejecución, no corrección.
- La cobertura de ramas detecta caminos sin probar que las líneas ocultan.
- Un umbral sirve como alarma, no como meta de calidad.
- Los casos límite y los errores aportan más que el porcentaje.

## Siguiente paso

Es momento de reunir todo en el [ejercicio integrador](/modulos/testing/ejercicio-integrador): una suite completa para una API con unitarios, integración y casos de error.
