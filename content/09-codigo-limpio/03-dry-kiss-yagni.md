---
slug: dry-kiss-yagni
title: "DRY, KISS y YAGNI"
order: 3
estimatedMinutes: 12
summary: "Evitá duplicar conocimiento, elegí la solución más simple y no construyas lo que todavía no necesitás: tres reglas que se equilibran entre sí."
quiz:
  - question: "¿Qué significa DRY bien entendido?"
    options:
      - "Que no exista una línea de código repetida en ningún archivo"
      - "Que cada pieza de conocimiento tenga una única representación autorizada"
      - "Que todas las clases usen herencia"
      - "Que se compartan variables globales"
    answer: 1
    explanation: "DRY habla de conocimiento duplicado, no de texto idéntico. Dos bloques parecidos pueden representar reglas distintas."
  - question: "¿Cuándo aplicar YAGNI?"
    options:
      - "Cuando ya hay tres casos de uso que justifican una abstracción"
      - "Cuando se quiere preparar el sistema para un futuro incierto"
      - "Cuando una funcionalidad no es necesaria hoy"
      - "Cuando el código es difícil de leer"
    answer: 2
    explanation: "YAGNI evita construir por anticipado. Si la necesidad no existe hoy, la abstracción agrega costo sin beneficio demostrado."
  - question: "¿Qué tensión existe entre DRY y KISS?"
    options:
      - "Ninguna: siempre coinciden"
      - "Una abstracción para evitar duplicación puede volver el código más complejo de lo necesario"
      - "KISS obliga a duplicar código"
      - "DRY prohíbe las funciones"
    answer: 1
    explanation: "Extraer una abstracción prematura por dos usos parecidos puede violar KISS. El equilibrio está en esperar evidencia y medir el costo."
  - question: "¿Qué señal indica que una abstracción se adelantó?"
    options:
      - "Se usa en un solo lugar y tiene parámetros para casos hipotéticos"
      - "Se usa en cinco módulos distintos"
      - "Tiene tests"
      - "Tiene un nombre claro"
    answer: 0
    explanation: "Si la abstracción existe para un solo uso o acumula parámetros 'por si acaso', probablemente sea prematura."
---

# DRY, KISS y YAGNI

## Objetivo

Aplicar DRY, KISS y YAGNI con criterio, reconocer cuándo una abstracción ayuda y cuándo solo agrega complejidad.

## Requisitos previos

Haber leído [Nombres y funciones chicas](/modulos/codigo-limpio/nombres-y-funciones) y [Comentarios, formato y legibilidad](/modulos/codigo-limpio/comentarios-y-formato).

## Concepto

Tres principios cortos, fáciles de memorizar y difíciles de aplicar juntos:

- **DRY** (*Don't Repeat Yourself*): cada conocimiento del sistema tiene una única representación autorizada.
- **KISS** (*Keep It Simple, Stupid*): la solución más simple que resuelve el problema es la mejor.
- **YAGNI** (*You Aren't Gonna Need It*): no construyas funcionalidad para un futuro que todavía no llegó.

DRY se malinterpreta como "no repetir texto". Si dos lugares tienen el mismo código pero cambian por razones distintas, no están duplicados: son dos reglas que coinciden hoy. La prueba es preguntar **¿si cambia una, cambia la otra?** Si la respuesta es no, unificarlas crea acoplamiento.

Ejemplo de duplicación real:

```csharp
public decimal CalcularTotal(decimal subtotal) => subtotal * 1.21m;
public decimal CalcularTotalConEnvio(decimal subtotal) => subtotal * 1.21m + 500;
```

El IVA es una regla. Si cambia la alícuota, hay que tocar dos lugares y es fácil olvidar uno. Conviene extraerlo:

```csharp
private const decimal Iva = 1.21m;

public decimal CalcularTotal(decimal subtotal) => subtotal * Iva;
public decimal CalcularTotalConEnvio(decimal subtotal) => CalcularTotal(subtotal) + 500;
```

Ejemplo de duplicación aparente:

```csharp
public string FormatearCliente(Cliente cliente) => $"{cliente.Nombre} - {cliente.Email}";
public string FormatearProveedor(Proveedor proveedor) => $"{proveedor.Nombre} - {proveedor.Email}";
```

Hoy el formato coincide, pero un proveedor podría necesitar el CUIT y un cliente no. Unificar por parecido crea una abstracción que en el próximo cambio hay que desarmar. La regla del tres ayuda: esperá al tercer uso antes de generalizar.

KISS y YAGNI frenan la sobreingeniería. Un sistema con interfaces, fábricas y capas para un CRUD de una tabla no es más profesional: es más caro de entender y de mantener. La complejidad se justifica con un problema presente.

## Ejemplo mínimo

YAGNI en acción. Antes:

```csharp
public interface ICalculadoraDeDescuentosConEstrategia
{
    decimal Calcular(Pedido pedido, IEstrategiaDeDescuento estrategia);
}

public interface IEstrategiaDeDescuento
{
    decimal Aplicar(decimal total);
}
```

Para un único descuento fijo, eso es demasiado. Después:

```csharp
public static class Descuentos
{
    private const decimal PorcentajeClienteFrecuente = 0.10m;

    public static decimal Aplicar(decimal total, bool clienteFrecuente) =>
        clienteFrecuente ? total * (1 - PorcentajeClienteFrecuente) : total;
}
```

```csharp
Console.WriteLine(Descuentos.Aplicar(1000, clienteFrecuente: true));
```

```text
900
```

Cuando aparezcan descuentos por temporada, cupón y categoría, la estrategia se justifica. Antes, no.

## Ejemplo aplicado

Un caso donde DRY y KISS chocan. Dos validaciones parecidas:

```csharp
public static bool EsEmailValido(string email) =>
    !string.IsNullOrWhiteSpace(email) && email.Contains('@');

public static bool EsUsuarioValido(string usuario) =>
    !string.IsNullOrWhiteSpace(usuario) && usuario.Length >= 3;
```

Son similares, pero no iguales. La tentación es crear `EsTextoValido(string, int, bool)` con banderas. Eso viola KISS y hace ilegible cada llamada.

La decisión equilibrada:

```csharp
public static bool EsEmailValido(string email) =>
    !string.IsNullOrWhiteSpace(email) && email.Contains('@');

public static bool EsUsuarioValido(string usuario) =>
    !string.IsNullOrWhiteSpace(usuario) && usuario.Length >= 3;
```

Se mantiene la duplicación mínima porque las reglas son independientes y cada una es obvia. Si mañana la validación de email se vuelve compleja, se extrae sola.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| Abstracción con diez parámetros opcionales | Se intentó cubrir todos los casos futuros | Volver a código concreto y esperar usos reales |
| Código duplicado que evoluciona distinto | Se unificó por parecido superficial | Separar las reglas y dejar cada una en su lugar |
| Una clase `Utilidades` con todo | DRY mal aplicado | Agrupar por dominio, no por tipo de operación |
| Interfaz con una sola implementación y un solo uso | YAGNI ignorado | Usar la clase concreta hasta que exista una razón |
| Copy paste con un `if` de diferencia | Miedo a abstraer | Si la regla es la misma, extraer; si no, dejar explícito |
| Código "simple" que repite la regla del IVA en cinco archivos | KISS mal aplicado | Simple no es sinónimo de duplicado |

## Buenas prácticas

- Antes de unificar, preguntá si los dos fragmentos cambian por la misma razón.
- Esperá tres usos antes de generalizar; el tercero revela la forma real.
- Preferí una función concreta y bien nombrada antes que una interfaz genérica.
- Si una abstracción necesita explicar su uso en cada llamada, probablemente no ayuda.
- Revisá si el código que "por si acaso" agregaste tiene un test o un caso de uso; si no, borralo.

## Ejercicio

Encontrá en tu proyecto un bloque duplicado. Decidí con la regla del tres si conviene extraerlo o dejarlo. Justificá en un comentario de commit por qué cambia junto o por separado.

<details>
<summary>Ver solución</summary>

```text
Caso A: dos endpoints calculan el IVA con la misma constante.
→ Cambian juntos: si cambia la alícuota, cambian los dos. Extraer.

Caso B: dos reportes formatean fechas con el mismo patrón.
→ Pueden cambiar por separado: uno es para exportar y otro para mostrar.
   Dejar duplicado hasta que aparezca una tercera necesidad concreta.
```

La decisión no es "hay texto repetido, extraigo". Es "hay conocimiento repetido que evoluciona junto, extraigo". El commit documenta el criterio para que el próximo desarrollador entienda la decisión.

</details>

## Resumen

- DRY aplica a conocimiento que cambia junto, no a texto parecido.
- KISS elige la solución más simple que resuelve el problema actual.
- YAGNI evita construir para un futuro incierto.
- Los tres se equilibran: abstraer de más rompe KISS; duplicar reglas rompe DRY.

## Siguiente paso

Con el criterio de simplicidad claro, el próximo paso son los principios [SOLID: SRP y OCP](/modulos/codigo-limpio/solid-srp-ocp), que ayudan a decidir dónde va cada responsabilidad.
