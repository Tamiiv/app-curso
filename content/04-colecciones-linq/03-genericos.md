---
slug: genericos
title: "Genéricos"
order: 3
estimatedMinutes: 15
summary: "Escribí clases y métodos que funcionan con cualquier tipo sin perder seguridad de tipos ni repetir código."
quiz:
  - question: "¿Qué restricción permite crear una instancia de T con new T()?"
    options:
      - "where T : class"
      - "where T : struct"
      - "where T : new()"
      - "where T : notnull"
    answer: 2
    explanation: "La restricción new() exige que T tenga un constructor público sin parámetros, que es lo que usa new T()."
  - question: "¿Qué ventaja principal aportan los genéricos frente a usar object?"
    options:
      - "Ocupan menos memoria en todos los casos"
      - "Evitan conversiones y errores detectados en tiempo de compilación"
      - "Permiten modificar el tipo en tiempo de ejecución"
      - "Funcionan solo con tipos por referencia"
    answer: 1
    explanation: "Con genéricos el compilador conoce el tipo concreto: no hace falta convertir desde object y los errores aparecen al compilar."
  - question: "¿Qué restricción conviene para comparar dos valores de tipo T con CompareTo?"
    options:
      - "where T : class"
      - "where T : IComparable<T>"
      - "where T : new()"
      - "where T : struct"
    answer: 1
    explanation: "IComparable<T> garantiza que T tiene el método CompareTo(T), necesario para ordenar o encontrar un máximo."
---

# Genéricos

## Objetivo

Escribir clases y métodos reutilizables con parámetros de tipo, aplicar restricciones cuando haga falta y reconocer cuándo un genérico simplifica el diseño.

## Requisitos previos

Necesitás manejar clases, records, interfaces y colecciones. Si venís de la lección anterior, ya viste que `List<T>` y `IEnumerable<T>` usan un tipo entre ángulos: ese `T` es un parámetro genérico.

## Concepto

Un genérico es un tipo o método que recibe un tipo como parámetro. En lugar de escribir `ListaDeClientes`, `ListaDeProductos` y `ListaDePedidos`, escribís `Lista<T>` una sola vez. El compilador genera una versión segura para cada tipo que uses.

La alternativa histórica era guardar todo como `object`. Eso obliga a convertir en cada lectura, permite meter un `Producto` en una lista de clientes y traslada los errores a tiempo de ejecución. Con genéricos, `List<Cliente>` solo acepta clientes y el compilador lo verifica.

Los parámetros de tipo se declaran entre ángulos: `Repositorio<T>`, `Maximo<T>`, `Resultado<T>`. El nombre no tiene que ser `T`; en estructuras con varias partes se usan nombres descriptivos como `TKey` y `TValue`, igual que en `Dictionary<TKey, TValue>`.

Las restricciones limitan qué tipos puede recibir un genérico. Sin restricciones solo podés usar las capacidades de `object`. Si tu algoritmo necesita comparar, pedí `IComparable<T>`; si necesita crear instancias, pedí `new()`; si necesita una referencia, pedí `class`. Cuanto más precisa sea la restricción, más operaciones podés usar dentro del método.

| Restricción | Permite |
|---|---|
| `where T : class` | Tipos por referencia, incluidos records de clase |
| `where T : struct` | Tipos por valor, como `int`, `decimal` o `DateTime` |
| `where T : new()` | `new T()` con constructor sin parámetros |
| `where T : IComparable<T>` | Comparar con `CompareTo` |
| `where T : notnull` | Excluir tipos anulables |

El orden importa: primero `class`, `struct` o la clase base; después las interfaces; y `new()` al final.

## Ejemplo mínimo

```csharp
static T Maximo<T>(IEnumerable<T> valores) where T : IComparable<T>
{
    bool hayValor = false;
    T maximo = default!;

    foreach (T valor in valores)
    {
        if (!hayValor || valor.CompareTo(maximo) > 0)
        {
            maximo = valor;
            hayValor = true;
        }
    }

    if (!hayValor)
    {
        throw new InvalidOperationException("La secuencia está vacía.");
    }

    return maximo;
}

Console.WriteLine(Maximo(new[] { 3, 7, 2 }));
Console.WriteLine(Maximo(new[] { "Ana", "Carla", "Bruno" }));
```

```text
7
Carla
```

El mismo método sirve para números y para textos porque ambos implementan `IComparable<T>`. `default!` indica que `maximo` se asigna antes de usarse; el `!` calma al compilador de nulos.

## Ejemplo aplicado

Un repositorio en memoria guarda entidades por id. La lógica es idéntica para clientes, productos o pedidos, así que no tiene sentido repetirla.

```csharp
var clientes = new Repositorio<Cliente>();
clientes.Guardar(1, new Cliente(1, "Ana Torres", "Rosario"));
clientes.Guardar(2, new Cliente(2, "Bruno Díaz", "Córdoba"));

Cliente? cliente = clientes.Buscar(1);
Console.WriteLine(cliente?.Nombre ?? "No encontrado");
Console.WriteLine($"Clientes guardados: {clientes.Todos.Count}");

public sealed class Repositorio<T> where T : class
{
    private readonly Dictionary<int, T> _items = [];

    public void Guardar(int id, T item) => _items[id] = item;

    public T? Buscar(int id) => _items.GetValueOrDefault(id);

    public IReadOnlyCollection<T> Todos => _items.Values;
}

public sealed record Cliente(int Id, string Nombre, string Ciudad);
```

```text
Ana Torres
Clientes guardados: 2
```

`Repositorio<T>` no sabe qué es un cliente. Solo sabe guardar un `T` y devolverlo. La restricción `class` permite que `Buscar` devuelva `T?` y comunique que puede no haber resultado.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `Operator '==' cannot be applied to operands of type 'T'` | No todos los tipos admiten `==` | Usar `EqualityComparer<T>.Default.Equals(a, b)` |
| `Cannot create an instance of T` | Falta la restricción de constructor | Agregar `where T : new()` |
| `T` no tiene el método que querés usar | Falta una restricción de interfaz o clase base | Agregar `where T : IComparable<T>` o la interfaz necesaria |
| El código no compila con `T?` | El contexto de nulabilidad no está claro | Restringir a `class` o `struct`, o usar `default!` con cuidado |
| Los contadores estáticos se comportan raro | Cada tipo cerrado tiene su propia copia de los miembros estáticos | Evitar estado estático compartido entre tipos genéricos |
| Todo termina siendo `object` disfrazado | Se usan genéricos sin aprovechar el tipo concreto | Aplicar restricciones y firmas específicas |

## Buenas prácticas

- Usá genéricos cuando el algoritmo sea realmente independiente del tipo; si solo tenés dos casos, una clase concreta puede ser más simple.
- Restringí lo justo y necesario. `where T : class, IEntidad, new()` comunica mucho mejor que un `T` sin restricciones.
- Preferí los genéricos de la biblioteca (`List<T>`, `Dictionary<TKey, TValue>`, `Func<T, TResult>`) antes de inventar estructuras propias.
- Evitá parámetros de tipo que no aparecen en la firma; suelen indicar un diseño confuso.
- Documentá las restricciones en el nombre o en la firma, no en un comentario lejano.

## Ejercicio

Implementá una `Pila<T>` genérica con `Apilar`, `Desapilar`, `TryDesapilar`, `Count` y `EstaVacia`. `Desapilar` debe lanzar `InvalidOperationException` si la pila está vacía; `TryDesapilar` debe devolver `false` sin lanzar.

<details>
<summary>Ver solución</summary>

```csharp
public sealed class Pila<T>
{
    private readonly List<T> _items = [];

    public int Count => _items.Count;

    public bool EstaVacia => _items.Count == 0;

    public void Apilar(T item) => _items.Add(item);

    public T Desapilar()
    {
        if (EstaVacia)
        {
            throw new InvalidOperationException("La pila está vacía.");
        }

        int ultimo = _items.Count - 1;
        T item = _items[ultimo];
        _items.RemoveAt(ultimo);
        return item;
    }

    public bool TryDesapilar(out T item)
    {
        if (EstaVacia)
        {
            item = default!;
            return false;
        }

        item = Desapilar();
        return true;
    }
}

var pila = new Pila<string>();
pila.Apilar("Teclado");
pila.Apilar("Mouse");
pila.Apilar("Monitor");

Console.WriteLine(pila.Desapilar());

if (pila.TryDesapilar(out string siguiente))
{
    Console.WriteLine(siguiente);
}

Console.WriteLine($"Quedan: {pila.Count}");
```

```text
Monitor
Mouse
Quedan: 1
```

La pila funciona igual con `int`, `Pedido` o cualquier otro tipo. El estado interno es una `List<T>`, pero los detalles de almacenamiento quedan encapsulados.

</details>

## Resumen

- Un genérico recibe tipos como parámetros y evita duplicar código.
- El compilador conserva la seguridad de tipos y elimina conversiones desde `object`.
- Las restricciones habilitan operaciones y documentan qué tipos acepta.
- `new()`, `class`, `struct` e `IComparable<T>` son las restricciones más frecuentes.

## Siguiente paso

Los genéricos aparecen en todas las APIs modernas de .NET, y también en las funciones que pasás como argumento. En la próxima lección vas a conocer las [lambdas y los delegates](/modulos/colecciones-linq/lambdas-delegates) que hacen posible LINQ.
