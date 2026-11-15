---
slug: colecciones
title: "Colecciones: List, Dictionary, HashSet"
order: 1
estimatedMinutes: 15
summary: "Cuándo usar List, Dictionary o HashSet, cómo recorrerlas y evitar los errores más comunes al guardar datos."
quiz:
  - question: "¿Qué colección conviene para buscar un cliente por su id?"
    options:
      - "List<Cliente>"
      - "Dictionary<int, Cliente>"
      - "HashSet<Cliente>"
      - "Cliente[]"
    answer: 1
    explanation: "Dictionary<TKey, TValue> guarda pares clave-valor y permite buscar por clave sin recorrer toda la colección."
  - question: "¿Qué devuelve HashSet<T>.Add cuando el elemento ya existe?"
    options:
      - "true"
      - "false"
      - "Una excepción"
      - "null"
    answer: 1
    explanation: "Add devuelve false si el elemento ya estaba en el conjunto; no lanza una excepción."
  - question: "¿Qué ocurre al leer con el indexador una clave que no existe en un Dictionary?"
    options:
      - "Devuelve null"
      - "Devuelve el valor por defecto del tipo"
      - "Lanza KeyNotFoundException"
      - "Agrega la clave automáticamente"
    answer: 2
    explanation: "El indexador del diccionario lanza KeyNotFoundException. Para evitarlo, usá TryGetValue o ContainsKey."
---

# Colecciones: List, Dictionary, HashSet

## Objetivo

Elegir la colección adecuada según el problema, recorrerla con comodidad y evitar los errores clásicos de búsqueda, duplicados y modificación mientras se itera.

## Requisitos previos

Conviene haber visto arrays y bucles en el módulo 2, y clases, records e interfaces en el módulo 3. No hace falta conocer LINQ: en esta lección usamos solo colecciones.

## Concepto

Un array tiene un tamaño fijo. Si necesitás agregar un producto al carrito, no podés redimensionarlo: tenés que crear otro array y copiar todo. Las colecciones resuelven eso con estructuras pensadas para crecer, buscar y evitar duplicados.

`List<T>` es una secuencia ordenada. Podés acceder por índice, agregar al final, insertar en el medio y repetir elementos. Es la opción natural cuando importa el orden y necesitás recorrer todo. Buscar con `Contains` recorre la lista de principio a fin, así que en listas grandes puede ser lento.

`Dictionary<TKey, TValue>` guarda pares clave-valor. Cada clave es única y la búsqueda es muy rápida, sin importar cuántos elementos haya. Pensalo como una guía telefónica: buscás por nombre y obtenés el teléfono. Es ideal para acceder a un cliente por id, a un producto por código o a una configuración por nombre.

`HashSet<T>` representa un conjunto: no tiene orden garantizado y no admite duplicados. Agregar un elemento que ya existe no falla; simplemente devuelve `false`. Es la estructura correcta para saber si algo pertenece a un grupo, eliminar repetidos o comparar conjuntos.

| Colección | Orden | Duplicados | Búsqueda | Uso típico |
|---|---|---|---|---|
| `List<T>` | Sí | Sí | Recorre la lista | Carrito, historial, resultados |
| `Dictionary<TKey, TValue>` | No garantizado | Claves únicas | Muy rápida por clave | Índice por id o email |
| `HashSet<T>` | No garantizado | No | Muy rápida por elemento | Etiquetas, ciudades únicas |

## Ejemplo mínimo

```csharp
var ciudades = new List<string> { "Rosario", "Córdoba" };
ciudades.Add("Mendoza");

var stock = new Dictionary<string, int>
{
    ["teclado"] = 12,
    ["mouse"] = 30
};
stock["monitor"] = 8;

var categorias = new HashSet<string> { "Periféricos", "Pantallas" };
bool seAgrego = categorias.Add("Periféricos");

Console.WriteLine($"{ciudades.Count} ciudades, monitor: {stock["monitor"]}, se agregó: {seAgrego}");
```

```text
3 ciudades, monitor: 8, se agregó: False
```

## Ejemplo aplicado

Un pedido tiene varias líneas y cada línea apunta a un producto. Los clientes se buscan por id y las ciudades se guardan sin repetir para armar una campaña.

```csharp
var clientes = new Dictionary<int, Cliente>
{
    [1] = new(1, "Ana Torres", "Rosario"),
    [2] = new(2, "Bruno Díaz", "Córdoba")
};

var carrito = new List<LineaPedido>
{
    new(new Producto(10, "Teclado", 45_000m), 2),
    new(new Producto(11, "Mouse", 18_000m), 1)
};
carrito.Add(new LineaPedido(new Producto(12, "Monitor", 320_000m), 1));

if (clientes.TryGetValue(1, out Cliente? cliente))
{
    decimal total = 0m;
    foreach (LineaPedido linea in carrito)
    {
        total += linea.Subtotal;
    }

    Console.WriteLine($"{cliente.Nombre}: {carrito.Count} ítems, total {total}");
}

var ciudades = new HashSet<string>();
foreach (Cliente c in clientes.Values)
{
    ciudades.Add(c.Ciudad);
}

var ciudadesOrdenadas = new List<string>(ciudades);
ciudadesOrdenadas.Sort();
Console.WriteLine(string.Join(", ", ciudadesOrdenadas));

public sealed record Cliente(int Id, string Nombre, string Ciudad);
public sealed record Producto(int Id, string Nombre, decimal Precio);
public sealed record LineaPedido(Producto Producto, int Cantidad)
{
    public decimal Subtotal => Producto.Precio * Cantidad;
}
```

```text
Ana Torres: 3 ítems, total 401000
Córdoba, Rosario
```

`TryGetValue` devuelve `true` y entrega el cliente por el parámetro de salida. Si la clave no existe, devuelve `false` y no lanza una excepción.

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| `KeyNotFoundException` | Se leyó una clave inexistente con `stock["x"]` | Usar `TryGetValue`, `ContainsKey` o `GetValueOrDefault` |
| `ArgumentException` al usar `Add` | La clave ya existía en el diccionario | Usar el indexador `stock["x"] = valor` para reemplazar |
| `InvalidOperationException` al modificar en un `foreach` | Se agregó o quitó un elemento mientras se recorría | Recorrer una copia o usar un `for` inverso |
| Búsquedas lentas con `List.Contains` dentro de un bucle | Cada búsqueda recorre toda la lista | Usar un `HashSet<T>` o un `Dictionary<TKey, TValue>` |
| Duplicados inesperados en una lista | `List<T>` permite repetidos | Usar `HashSet<T>` o filtrar con `Distinct` |

## Buenas prácticas

- Declará la variable con la interfaz más chica que necesites: `IReadOnlyList<T>` si solo vas a leer, `IEnumerable<T>` si solo vas a recorrer.
- Usá `Dictionary<TKey, TValue>` cuando el acceso principal sea por clave y `HashSet<T>` cuando el acceso principal sea por pertenencia.
- Si conocés la cantidad aproximada de elementos, pasá la capacidad al constructor: `new List<Producto>(1000)`.
- No expongas un `List<T>` como propiedad pública si no querés que otros lo modifiquen. En la lección 8 vas a ver cómo devolver una vista de solo lectura.
- Elegí nombres que revelen el contenido: `clientesPorId`, `ciudadesAtendidas`, `carrito`.

## Ejercicio

Dada una lista de pedidos, contá cuántas veces aparece cada ciudad usando un `Dictionary<string, int>` y obtené las ciudades únicas con un `HashSet<string>`. Mostrá el conteo ordenado alfabéticamente.

```csharp
var pedidos = new List<Pedido>
{
    new(1, "Rosario"),
    new(2, "Córdoba"),
    new(3, "Rosario"),
    new(4, "Mendoza"),
    new(5, "Rosario")
};

public sealed record Pedido(int Id, string Ciudad);
```

<details>
<summary>Ver solución</summary>

```csharp
var pedidos = new List<Pedido>
{
    new(1, "Rosario"),
    new(2, "Córdoba"),
    new(3, "Rosario"),
    new(4, "Mendoza"),
    new(5, "Rosario")
};

var conteo = new Dictionary<string, int>();
var ciudadesUnicas = new HashSet<string>();

foreach (Pedido pedido in pedidos)
{
    conteo[pedido.Ciudad] = conteo.GetValueOrDefault(pedido.Ciudad) + 1;
    ciudadesUnicas.Add(pedido.Ciudad);
}

var ciudades = new List<string>(conteo.Keys);
ciudades.Sort();

foreach (string ciudad in ciudades)
{
    Console.WriteLine($"{ciudad}: {conteo[ciudad]}");
}

Console.WriteLine($"Ciudades únicas: {ciudadesUnicas.Count}");

public sealed record Pedido(int Id, string Ciudad);
```

```text
Córdoba: 1
Mendoza: 1
Rosario: 3
Ciudades únicas: 3
```

`GetValueOrDefault` devuelve `0` cuando la clave todavía no existe, así que la suma funciona sin un `if` previo.

</details>

## Resumen

- `List<T>` mantiene orden y permite duplicados; es la colección base para secuencias.
- `Dictionary<TKey, TValue>` busca por clave y exige claves únicas.
- `HashSet<T>` evita duplicados y responde rápido a la pregunta "¿pertenece?".
- `TryGetValue` es la forma segura de consultar un diccionario.

## Siguiente paso

Ahora que sabés guardar datos, en la próxima lección vas a ver cómo recorrerlos sin cargar todo en memoria: [IEnumerable e iteradores](/modulos/colecciones-linq/ienumerable-iteradores).
