---
slug: refactoring-ejercicio-integrador
title: "Refactoring y ejercicio integrador"
order: 9
estimatedMinutes: 25
summary: "Tomá una clase con múltiples responsabilidades, refactorizala en pasos seguros con tests y aplicá los principios y patrones del módulo."
quiz:
  - question: "¿Cuál es el primer paso seguro antes de refactorizar?"
    options:
      - "Reescribir todo desde cero"
      - "Asegurar tests que capturen el comportamiento actual"
      - "Cambiar los nombres de todas las clases"
      - "Aplicar todos los patrones conocidos"
    answer: 1
    explanation: "Sin una red de seguridad, no hay forma de saber si el refactor cambió el comportamiento. Los tests permiten avanzar en pasos chicos."
  - question: "¿Qué es un refactor?"
    options:
      - "Un cambio de comportamiento visible para el usuario"
      - "Un cambio en la estructura interna que preserva el comportamiento externo"
      - "Una migración de base de datos"
      - "Una nueva funcionalidad"
    answer: 1
    explanation: "Refactorizar es mejorar el diseño sin alterar lo que observa el usuario del sistema."
  - question: "¿Qué señal indica que conviene extraer una clase?"
    options:
      - "La clase tiene más de cien líneas"
      - "Un grupo de métodos comparte datos y cambia por el mismo motivo"
      - "La clase tiene un constructor"
      - "La clase tiene un nombre largo"
    answer: 1
    explanation: "La cohesión guía la extracción: métodos y datos que cambian juntos forman una responsabilidad."
  - question: "¿Cómo se refactoriza con seguridad en pasos chicos?"
    options:
      - "Cambiando todo y ejecutando tests al final"
      - "Haciendo un cambio pequeño, ejecutando tests y repitiendo"
      - "Sin tests, confiando en el code review"
      - "Solo en la rama principal y sin backups"
    answer: 1
    explanation: "Pasos pequeños con la suite en verde permiten detectar el cambio que rompió algo y revertirlo sin perder trabajo."
---

# Refactoring y ejercicio integrador

## Objetivo

Refactorizar una clase con varias responsabilidades en pasos seguros, usando tests como red de protección, y aplicar los principios y patrones del módulo a un caso realista.

## Requisitos previos

Haber recorrido el módulo: [Nombres y funciones](/modulos/codigo-limpio/nombres-y-funciones), [Comentarios y formato](/modulos/codigo-limpio/comentarios-y-formato), [DRY, KISS y YAGNI](/modulos/codigo-limpio/dry-kiss-yagni), [SOLID](/modulos/codigo-limpio/solid-srp-ocp) y [patrones](/modulos/codigo-limpio/patrones-creacionales). También conviene tener presente el módulo de [testing](/modulos/testing).

## Concepto

Refactorizar es cambiar la estructura sin cambiar el comportamiento observable. No es "reescribir porque el código es feo". La diferencia es crucial: un refactor seguro se apoya en tests y avanza en pasos chicos.

El proceso que funciona:

1. **Caracterizar:** escribir tests que capturen el comportamiento actual, aunque el código sea feo.
2. **Aislar:** identificar una responsabilidad y extraerla.
3. **Verificar:** ejecutar la suite después de cada paso.
4. **Repetir:** continuar con la siguiente responsabilidad.
5. **Limpiar:** renombrar, borrar duplicación y actualizar documentación.

Qué buscar al leer una clase:

| Señal | Posible problema |
|---|---|
| Muchos `if` sobre el mismo campo | Condicional que pide polimorfismo |
| Métodos que tocan archivos, base y presentación | SRP violado |
| Parámetros que solo algunos métodos usan | Clase poco cohesiva |
| Nombres como `procesar`, `manejar`, `hacer` | Intención oculta |
| Comentarios que explican bloques | Código que pide extracción de método |

El refactor no necesita patrones. Muchas veces alcanza con extraer funciones, mover código a otra clase y poner nombres claros. Los patrones aparecen cuando hay variación real.

## Ejemplo mínimo

Antes: una clase que hace todo.

```csharp
public sealed class ProcesadorPedido
{
    public void Procesar(string cliente, decimal total, string tipo)
    {
        if (string.IsNullOrWhiteSpace(cliente))
        {
            throw new ArgumentException("Cliente inválido");
        }

        decimal descuento = tipo switch
        {
            "vip" => total * 0.1m,
            "frecuente" => total * 0.05m,
            _ => 0
        };

        decimal final = total - descuento;
        File.AppendAllText("pedidos.log", $"{cliente}:{final}\n");
        Console.WriteLine($"Pedido de {cliente} por {final:F2}");
    }
}
```

Después: cada responsabilidad en su lugar.

```csharp
public sealed record Pedido(string Cliente, decimal Total, string TipoCliente);

public interface ICalculadoraDescuento
{
    decimal Calcular(Pedido pedido);
}

public sealed class DescuentoVip : ICalculadoraDescuento
{
    public decimal Calcular(Pedido pedido) => pedido.Total * 0.10m;
}

public sealed class DescuentoSinBeneficio : ICalculadoraDescuento
{
    public decimal Calcular(Pedido pedido) => 0;
}

public sealed class ProcesadorPedido(ICalculadoraDescuento descuento, IRegistroPedidos registro)
{
    public void Procesar(Pedido pedido)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pedido.Cliente);

        var final = pedido.Total - descuento.Calcular(pedido);
        registro.Registrar(pedido.Cliente, final);
        Console.WriteLine($"Pedido de {pedido.Cliente} por {final:F2}");
    }
}

public interface IRegistroPedidos
{
    void Registrar(string cliente, decimal total);
}

public sealed class ArchivoRegistroPedidos(string ruta) : IRegistroPedidos
{
    public void Registrar(string cliente, decimal total) =>
        File.AppendAllText(ruta, $"{cliente}:{total}\n");
}
```

```csharp
var procesador = new ProcesadorPedido(
    new DescuentoVip(),
    new ArchivoRegistroPedidos("pedidos.log"));

procesador.Procesar(new Pedido("Ana", 1000, "vip"));
```

```text
Pedido de Ana por 900.00
```

La validación sigue, el cálculo se aisló y el registro se puede reemplazar por un doble en tests.

## Ejemplo aplicado

Refactor guiado por tests. Primero se caracteriza el comportamiento:

```csharp
public sealed class ProcesadorPedidoTests
{
    [Fact]
    public void ClienteVipRecibeDiezPorCientoDeDescuento()
    {
        var registro = new RegistroEnMemoria();
        var procesador = new ProcesadorPedido(new DescuentoVip(), registro);

        procesador.Procesar(new Pedido("Ana", 1000, "vip"));

        Assert.Equal(900m, registro.Total);
    }

    [Fact]
    public void ClienteSinBeneficioPagaElTotal()
    {
        var registro = new RegistroEnMemoria();
        var procesador = new ProcesadorPedido(new DescuentoSinBeneficio(), registro);

        procesador.Procesar(new Pedido("Bruno", 1000, "regular"));

        Assert.Equal(1000m, registro.Total);
    }

    [Fact]
    public void ClienteVacioLanzaExcepcion()
    {
        var procesador = new ProcesadorPedido(new DescuentoSinBeneficio(), new RegistroEnMemoria());

        Assert.Throws<ArgumentException>(() => procesador.Procesar(new Pedido("", 100, "regular")));
    }

    private sealed class RegistroEnMemoria : IRegistroPedidos
    {
        public decimal Total { get; private set; }

        public void Registrar(string cliente, decimal total) => Total = total;
    }
}
```

```text
Passed! - Failed: 0, Passed: 3, Skipped: 0
```

Con la red de seguridad, el siguiente paso es elegir la estrategia según el tipo de cliente:

```csharp
public sealed class CalculadoraDescuentos(IReadOnlyDictionary<string, ICalculadoraDescuento> calculadoras)
{
    public decimal Calcular(Pedido pedido) =>
        calculadoras.TryGetValue(pedido.TipoCliente, out var calculadora)
            ? calculadora.Calcular(pedido)
            : 0;
}
```

Si el `switch` vuelve a crecer, esta es la señal para pasar a estrategias registradas. Si solo hay dos casos estables, el `switch` sigue siendo válido.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| El refactor cambia el comportamiento | No había tests o se cambiaron reglas sin querer | Caracterizar antes y ejecutar la suite en cada paso |
| Un commit gigante con todo el refactor | Se mezclaron pasos | Commits chicos y verificables |
| Se extraen clases sin cohesión | Se dividió por cantidad de líneas | Agrupar por responsabilidad y motivo de cambio |
| Se agregan patrones sin necesidad | Sobreingeniería | Ir de lo simple a lo complejo solo con evidencia |
| Se mezcla refactor con nueva funcionalidad | Dos objetivos en la misma rama | Separar: primero refactor, después feature |
| Se rompen los tests existentes | Los tests dependían de detalles internos | Probar comportamiento observable, no implementación |

## Buenas prácticas

- Refactorizá con la suite en verde y en pasos que puedas revertir.
- Un commit, un cambio de estructura.
- No cambies comportamiento y estructura en el mismo paso.
- Nombres claros antes de mover código: el nombre correcto revela dónde va.
- Borrá el código muerto en lugar de comentarlo.

## Ejercicio

Refactorizá un método `RegistrarUsuario(string email, string password, bool admin)` que valida, hashea la contraseña, guarda en un archivo y envía un email de bienvenida. Separá validación, persistencia y notificación, y cubrí el flujo con al menos tres tests.

<details>
<summary>Ver solución</summary>

```csharp
public sealed record Usuario(string Email, string PasswordHash, bool Admin);

public static class ValidadorUsuario
{
    public static void Validar(string email, string password)
    {
        if (!email.Contains('@'))
        {
            throw new ArgumentException("Email inválido.");
        }

        if (password.Length < 8)
        {
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        }
    }
}

public interface IUsuarioRepository
{
    void Guardar(Usuario usuario);
}

public interface INotificadorBienvenida
{
    void Enviar(string email);
}

public interface IHashService
{
    string Hashear(string password);
}

public sealed class RegistroUsuario(
    IUsuarioRepository repositorio,
    IHashService hash,
    INotificadorBienvenida notificador)
{
    public Usuario Registrar(string email, string password, bool admin)
    {
        ValidadorUsuario.Validar(email, password);

        var usuario = new Usuario(email, hash.Hashear(password), admin);
        repositorio.Guardar(usuario);
        notificador.Enviar(email);
        return usuario;
    }
}
```

```csharp
var repository = new RepositorioEnMemoria();
var notificador = new NotificadorEspia();
var registro = new RegistroUsuario(repository, new HashFake(), notificador);

registro.Registrar("ana@ejemplo.com", "Password123", admin: false);

Assert.Single(repository.Usuarios);
Assert.Equal("ana@ejemplo.com", notificador.UltimoEmail);
```

```text
Passed! - Failed: 0, Passed: 3, Skipped: 0
```

La lógica de hash, persistencia y notificación se puede cambiar sin tocar la validación ni el flujo de registro.

</details>

## Resumen

- Refactorizar preserva comportamiento y mejora estructura.
- Los tests son la red de seguridad; sin ellos, es reescritura a ciegas.
- Se avanza en pasos chicos, con commits verificables.
- Los patrones aparecen cuando hay variación real, no antes.

## Siguiente paso

El código está más limpio y probado. El próximo módulo es [Seguridad y JWT](/modulos/seguridad-jwt): autenticación, autorización y los errores que cuestan caro.
