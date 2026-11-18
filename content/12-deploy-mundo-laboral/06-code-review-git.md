---
slug: code-review-git
title: "Code review y flujo con Git"
order: 6
estimatedMinutes: 15
summary: "Armá un flujo de ramas corto y predecible, escribí pull requests chicos y hacé reviews que mejoran el código sin frenar al equipo."
quiz:
  - question: "¿Cuál es el tamaño recomendado para un pull request revisable?"
    options:
      - "Menos de 400 líneas de cambio"
      - "Entre 1500 y 2000 líneas"
      - "Todo el módulo en un solo PR"
      - "No importa el tamaño, importa el autor"
    answer: 0
    explanation: "Los PR chicos se revisan más rápido y con más profundidad. Pasadas unas 400 líneas, la atención cae y se escapan defectos."
  - question: "¿Qué mensaje de commit sigue Conventional Commits?"
    options:
      - "cambios varios"
      - "fix: corregir cálculo de IVA en pedidos"
      - "Se arregló el bug que reportó Juan"
      - "WIP"
    answer: 1
    explanation: "El formato es tipo(alcance opcional): descripción. fix, feat, refactor, test y docs son tipos habituales."
  - question: "¿Qué ventaja tiene una rama corta que vive pocos días?"
    options:
      - "Evita conflictos grandes y facilita el review"
      - "Permite más commits sin tests"
      - "Obliga a usar merge commits"
      - "Elimina la necesidad de CI"
    answer: 0
    explanation: "Cuanto menos tiempo vive una rama, menos divergencia acumula respecto de main y más simple es integrar y revisar."
  - question: "¿Qué debería hacer un revisor ante un cambio que no entiende?"
    options:
      - "Aprobarlo para no bloquear"
      - "Rechazarlo sin explicación"
      - "Preguntar con una sugerencia concreta y pedir contexto"
      - "Reescribir todo el PR él mismo"
    answer: 2
    explanation: "Preguntar y proponer una alternativa concreta mantiene el foco en el código y no en la persona, y desbloquea la conversación."
---

# Code review y flujo con Git

## Objetivo

Trabajar con ramas y pull requests chicos, escribir mensajes de commit que cuenten la historia del cambio y dar reviews útiles, técnicos y rápidos.

## Requisitos previos

Necesitás manejar `git add`, `commit`, `push` y `pull`, y haber visto cómo un pipeline verifica los cambios en [CI/CD con GitHub Actions](/modulos/deploy-mundo-laboral/ci-cd).

## Concepto

El flujo de trabajo importa más de lo que parece. Un equipo con ramas que viven semanas acumula conflictos, reviews imposibles y despliegues riesgosos. Un equipo con ramas cortas y cambios pequeños integra varias veces por día y detecta problemas cuando todavía son baratos.

El modelo más simple y extendido es trunk-based con ramas de corta vida. `main` siempre está desplegable y protegida. Cada cambio nace en una rama como `feat/pedidos-descuento`, se integra con un pull request y se borra al mergear. No hace falta una rama `develop` intermedia si el pipeline es confiable.

Los commits también son documentación. Conventional Commits propone `tipo(alcance): descripción`, por ejemplo `feat(pedidos): agregar descuento por volumen`. Los tipos más usados son `feat`, `fix`, `refactor`, `test`, `docs` y `chore`. Ese formato permite generar changelogs y entender el historial sin abrir cada diff.

Un pull request debe poder revisarse en una sentada. Si el cambio es grande porque la tarea era grande, partilo: primero el refactor sin cambio de comportamiento, después la funcionalidad. El revisor agradece la separación y el `git bisect` también.

La review no es una aprobación burocrática. Busca defectos, riesgos, problemas de diseño y oportunidades de simplificación. No busca imponer gustos personales. Un comentario útil dice qué pasa, por qué importa y qué alternativa propone.

Las ramas protegidas y las reglas de merge completan el círculo: exigir CI verde, al menos una aprobación y conversaciones resueltas. El objetivo no es bloquear, es evitar que un error obvio llegue a producción.

## Ejemplo mínimo

```bash
# 1. Actualizar main
git switch main
git pull --ff-only

# 2. Rama corta y descriptiva
git switch -c feat/pedidos-descuento

# 3. Trabajar en commits pequeños
git add src/Pedidos/DescuentoService.cs
git commit -m "feat(pedidos): calcular descuento por volumen"

git add tests/Pedidos/DescuentoServiceTests.cs
git commit -m "test(pedidos): cubrir descuento con borde de 10 unidades"

# 4. Mantener la rama al día sin ruido
git fetch origin
git rebase origin/main

# 5. Publicar y abrir PR
git push -u origin feat/pedidos-descuento
```

```text
Switched to a new branch 'feat/pedidos-descuento'
[feat/pedidos-descuento 1a2b3c4] feat(pedidos): calcular descuento por volumen
 1 file changed, 28 insertions(+)
Successfully rebased and updated refs/heads/feat/pedidos-descuento.
```

El rebase sobre `main` mantiene el historial lineal y evita el commit de merge innecesario dentro de la rama. No reescribas ramas compartidas: si otra persona trabaja sobre tu rama, coordiná antes.

## Ejemplo aplicado

Un cambio real con un problema típico: el servicio consulta la base dentro de un bucle y el review lo detecta.

```csharp
// Antes
public async Task<decimal> CalcularTotalAsync(int pedidoId, CancellationToken ct)
{
    var pedido = await _db.Pedidos.FindAsync([pedidoId], ct);
    decimal total = 0;

    foreach (var item in pedido!.Items)
    {
        var producto = await _db.Productos.FindAsync([item.ProductoId], ct);
        total += producto!.Precio * item.Cantidad;
    }

    return total;
}
```

```csharp
// Después
public async Task<decimal> CalcularTotalAsync(int pedidoId, CancellationToken ct)
{
    var pedido = await _db.Pedidos
        .Include(p => p.Items)
        .ThenInclude(i => i.Producto)
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == pedidoId, ct)
        ?? throw new NotFoundException($"Pedido {pedidoId} no encontrado");

    return pedido.Items.Sum(i => i.Producto.Precio * i.Cantidad);
}
```

```text
Comentario de review:
src/Pedidos/PedidoService.cs:7
El FindAsync dentro del foreach genera una consulta por ítem (N+1).
Con 50 ítems son 51 viajes a la base.
Sugiero Include + ThenInclude y sumar en memoria con AsNoTracking.
¿Hay algún caso donde el pedido pueda tener cientos de ítems?
```

Ese comentario tiene ubicación, problema, impacto, propuesta y una pregunta. No dice está mal. Explica por qué y deja espacio para que el autor responda con contexto que el revisor no tiene.

Una plantilla de PR ayuda a que el autor llegue con la información necesaria:

```markdown
## Qué cambia
Calcula descuentos por volumen en pedidos de más de 10 unidades.

## Por qué
Ticket #142: los clientes mayoristas pedían la bonificación automática.

## Cómo probar
1. Crear pedido con 11 unidades.
2. Verificar 10 % de descuento en el total.
3. Confirmar que 10 unidades no aplican descuento.

## Riesgos
Cambia el cálculo del total. Requiere revisar reportes históricos.

## Checklist
- [x] Tests agregados
- [x] Sin secretos en el diff
- [x] Migración compatible hacia atrás
```

## Errores comunes

| Error | Causa | Solución |
|---|---|---|
| PR de 2000 líneas | Se mezcló refactor, feature y formato | Dividir en PRs temáticos y encadenarlos |
| Conflictos constantes | Ramas largas sin integrar | Rebase frecuente sobre `main` y ramas de pocos días |
| El historial no se entiende | Mensajes como `cambios` o `fix` | Usar Conventional Commits con descripción concreta |
| La review se vuelve personal | Comentarios sobre estilo sin criterio acordado | Acordar un `.editorconfig` y hablar del código |
| Se mergea con CI rojo | Rama sin protección | Exigir checks obligatorios y conversaciones resueltas |
| Se pierde el contexto del cambio | PR sin descripción ni pasos de prueba | Completar plantilla y enlazar el ticket |

## Buenas prácticas

- Mantené ramas de menos de tres días y PRs de menos de 400 líneas.
- Escribí el título del PR en imperativo y con el mismo formato que los commits.
- Revisá primero el diseño y después el detalle; no empieces por el formato.
- Separá comentarios bloqueantes de sugerencias opcionales con un prefijo claro.
- Aprobá con entusiasmo y explicá qué te convenció, no solo qué falta.

## Ejercicio

Tomá un cambio pendiente de tu proyecto, dividilo en dos commits (`refactor` y `feat`), abrí un PR con la plantilla y hacé una review de tu propio diff. Marcá al menos un problema real, una pregunta y una sugerencia no bloqueante. Después verificá que el pipeline corra sobre la rama.

<details>
<summary>Ver solución</summary>

```bash
git switch -c refactor/pedido-total
# mover código sin cambiar comportamiento
git commit -m "refactor(pedidos): extraer cálculo de total a un servicio"

git switch -c feat/descuento-volumen
# agregar la funcionalidad sobre la base anterior
git commit -m "feat(pedidos): aplicar descuento desde 11 unidades"
git push -u origin refactor/pedido-total
git push -u origin feat/descuento-volumen
```

Autoreview con checklist:

- El commit de refactor no altera resultados: los tests existentes pasan sin cambios.
- El commit de feature agrega tests de borde: 10 unidades sin descuento, 11 con descuento.
- No hay `Console.WriteLine`, `TODO` sin ticket ni secretos en el diff.
- El PR enlaza el ticket y describe cómo probar.
- El pipeline ejecutó build y tests en la rama.

```text
Comentarios de ejemplo en el autoreview:

[bloqueante] PedidoService.cs:22
El descuento se aplica también a pedidos cancelados.
Sugiero filtrar por Estado == EstadoPedido.Confirmado antes de calcular.

[pregunta] DescuentoService.cs:9
¿El 10 % es fijo por negocio o debería venir de configuración?
Si puede cambiar sin deploy, conviene una opción validada.

[sugerencia] PedidoServiceTests.cs:31
Se puede usar [Theory] con InlineData(10, 0) e InlineData(11, 10)
para evitar duplicar el cuerpo del test.
```

```text
Resultado esperado del PR:
- 2 commits, 1 PR encadenado o 2 PRs con base en el anterior.
- CI verde en ambas ramas.
- Comentarios clasificados y respondidos.
```

</details>

## Resumen

- `main` protegida y siempre desplegable; ramas cortas y temáticas.
- Conventional Commits hace el historial legible y automatizable.
- Los PR chicos se revisan mejor y generan menos conflictos.
- Una buena review explica problema, impacto y alternativa.
- Las reglas de merge convierten las buenas intenciones en garantías.

## Siguiente paso

El flujo técnico ya está claro. Falta entender cómo se organiza el trabajo alrededor de él. Seguí con [Metodologías ágiles](/modulos/deploy-mundo-laboral/metodologias-agiles).
