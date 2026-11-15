---
slug: git-github
title: "Git y GitHub para el día a día"
order: 7
estimatedMinutes: 15
summary: "Inicializar un repositorio, hacer commits claros, trabajar con ramas y subir tu proyecto a GitHub sin subir basura."
quiz:
  - question: "¿Qué comando prepara los cambios para el próximo commit?"
    options:
      - "git commit"
      - "git add"
      - "git push"
      - "git init"
    answer: 1
    explanation: "git add pasa los cambios al staging area; git commit recién después crea la fotografía con lo que está preparado."
  - question: "¿Qué archivo indica qué archivos no debe versionar Git?"
    options:
      - "README.md"
      - ".gitignore"
      - ".gitattributes"
      - "global.json"
    answer: 1
    explanation: "Las reglas de .gitignore excluyen archivos generados, secretos y configuración local del control de versiones."
  - question: "¿Qué hace git switch -c feature/login?"
    options:
      - "Crea una rama nueva y se mueve a ella"
      - "Borra la rama feature/login"
      - "Sube la rama al remoto"
      - "Fusiona feature/login con main"
    answer: 0
    explanation: "git switch -c crea la rama indicada y cambia el HEAD a esa rama en un solo paso."
---

# Git y GitHub para el día a día

## Objetivo

Inicializar un repositorio para un proyecto .NET, hacer commits claros, trabajar con ramas y publicar el código en GitHub sin subir archivos generados ni secretos.

## Requisitos previos

Haber completado las lecciones anteriores del módulo. Necesitás Git instalado (`git --version`) y una cuenta de GitHub. Si no tenés Git, instalalo desde [git-scm.com](https://git-scm.com/downloads).

## Concepto

Git guarda **commits**: fotografías del proyecto con autor, fecha, mensaje y un puntero al commit anterior. El flujo básico tiene tres áreas: el directorio de trabajo (tus archivos), el staging area (lo que vas a incluir) y el repositorio (los commits). `git add` pasa cambios al staging; `git commit` crea la foto.

Una **rama** es un puntero móvil a un commit. `main` es la rama principal. Crear una rama para cada cambio mantiene `main` estable y permite revisar antes de integrar.

Un **remoto** es una copia del repositorio en otro lugar, normalmente GitHub. `origin` es el nombre convencional del remoto principal. `git push` sube commits; `git pull` trae y fusiona los del remoto.

`.gitignore` le dice a Git qué no versionar. En .NET, `bin/` y `obj/` son generados; también conviene ignorar `*.user`, `.vs/`, `.env` y archivos de configuración local. `dotnet new gitignore` crea una lista completa y actualizada.

GitHub agrega encima: repositorios remotos, pull requests, issues, Actions y releases. Un **pull request** (PR) es una propuesta de fusión con revisión y verificaciones automáticas.

## Ejemplo mínimo

```bash
dotnet new console -n GitDemo
cd GitDemo
dotnet new gitignore
git init
git add .
git commit -m "feat: crear proyecto de consola"
git log --oneline
```

Salida:

```text
a1b2c3d feat: crear proyecto de consola
```

`git init` crea el repositorio local en `.git/`. `git add .` prepara todos los archivos no ignorados. `git commit -m` guarda la foto. `git log --oneline` muestra el historial resumido.

Antes de commitear, revisá qué va a entrar:

```bash
git status
git diff --staged
```

`git status` muestra archivos nuevos, modificados y sin seguimiento. `git diff --staged` muestra el contenido exacto que se va a commitear.

## Ejemplo aplicado

Supongamos que querés agregar un saludo personalizado sin tocar `main`:

```bash
git switch -c feature/saludo
```

Editá `Program.cs`:

```csharp
Console.Write("Tu nombre: ");
var nombre = Console.ReadLine();
Console.WriteLine($"Hola, {nombre}!");
```

```bash
git add Program.cs
git commit -m "feat: saludar por nombre"
git switch main
git merge feature/saludo
```

`git switch -c` crea la rama y se mueve a ella. Al terminar, `git switch main` vuelve y `git merge` integra los cambios. Si no hubo conflictos, `main` avanza.

Para subirlo a GitHub:

```bash
gh repo create GitDemo --public --source=. --remote=origin --push
```

Si no tenés `gh`, creá el repositorio vacío en GitHub y conectalo:

```bash
git remote add origin https://github.com/tu-usuario/GitDemo.git
git push -u origin main
```

`-u` deja `origin/main` como upstream, así los próximos `git push` y `git pull` no necesitan argumentos.

Cuando el remoto ya tiene commits (por ejemplo, un README creado desde la web), el primer push puede rechazarse. Traé los cambios y poné tus commits locales encima:

```bash
git pull --rebase origin main
git push
```

Un **conflicto** aparece cuando dos ramas cambian la misma línea. Git marca el archivo con `<<<<<<<`, `=======` y `>>>>>>>`. Editá para quedarte con la versión correcta, borrá los marcadores y luego:

```bash
git add archivo.cs
git commit
```

## Errores comunes

| Error | Causa probable | Solución |
|---|---|---|
| `bin/` y `obj/` aparecen en el commit | No hay `.gitignore` | `dotnet new gitignore` y después `git rm -r --cached bin obj` |
| `git push` rechazado (non-fast-forward) | El remoto tiene commits nuevos | `git pull --rebase origin main` y volver a pushear |
| Se commiteó una contraseña | Se agregó un archivo con secretos | Rotar la credencial y limpiar el historial; no alcanza con borrar el archivo |
| `detached HEAD` | Se hizo checkout de un commit, no de una rama | `git switch main` o crear una rama con `git switch -c` |
| Conflicto de merge | Dos ramas tocaron la misma línea | Editar, `git add` y `git commit` |
| Cambios locales que no querés conservar | Edición equivocada | `git restore archivo` (descarta; no se puede deshacer) |
| Commit con mensaje equivocado | Typo en el mensaje | `git commit --amend -m "mensaje correcto"` si no se pusheó |
| Archivos grandes | Binarios en el repositorio | Git LFS o no versionarlos |

## Buenas prácticas

- Inicializá el repo y el `.gitignore` antes del primer commit.
- Hacé commits chicos, con un solo propósito y mensaje en imperativo: `feat: agregar validación`, `fix: corregir cálculo`.
- Cada commit debería compilar y pasar las pruebas.
- Nunca commitees secretos, tokens ni connection strings. Usá variables de entorno o `dotnet user-secrets`.
- Hacé `git pull --rebase` antes de pushear para mantener el historial lineal.
- Protegé `main` en GitHub y trabajá con ramas y PRs, incluso en proyectos personales.
- Etiquetá versiones: `git tag -a v1.0.0 -m "Primera versión"` y `git push --tags`.
- Escribí un README con requisitos, cómo ejecutar y cómo publicar.

## Ejercicio

Creá un repositorio para una app `HolaGit`, agregá `.gitignore`, hacé dos commits, creá una rama `feature/saludo`, cambiá el saludo, fusioná en `main`, subí el proyecto a GitHub y etiquetá `v1.0.0`. Mostrá el historial con `git log --oneline --graph --all`.

<details>
<summary>Ver solución</summary>

```bash
dotnet new console -n HolaGit
cd HolaGit
dotnet new gitignore
git init
git add .
git commit -m "chore: crear proyecto de consola"

git switch -c feature/saludo
```

`Program.cs`:

```csharp
Console.Write("Tu nombre: ");
var nombre = Console.ReadLine();
Console.WriteLine($"Hola, {nombre}!");
```

```bash
git add Program.cs
git commit -m "feat: saludar por nombre"
git switch main
git merge feature/saludo
git tag -a v1.0.0 -m "Primera versión"

gh repo create HolaGit --public --source=. --remote=origin --push
git push --tags
```

Si usás la web en lugar de `gh`:

```bash
git remote add origin https://github.com/tu-usuario/HolaGit.git
git push -u origin main
git push --tags
```

Historial esperado:

```text
* 7f3a1c2 (HEAD -> main, tag: v1.0.0, feature/saludo) feat: saludar por nombre
* 2b9d4e1 chore: crear proyecto de consola
```

</details>

## Resumen

- Git guarda commits; `add` prepara y `commit` confirma.
- Las ramas aíslan cambios y los PRs los revisan.
- `.gitignore` evita versionar `bin/`, `obj/` y secretos.
- `origin` es el remoto; `push` sube y `pull --rebase` integra.
- Etiquetá las versiones que publicás.

## Siguiente paso

En [Publicar una aplicación de consola](/modulos/fundamentos/publicar-consola) vas a convertir el proyecto versionado en un ejecutable que se puede entregar.
