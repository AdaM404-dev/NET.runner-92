# NET.runner-92 docs

Project knowledge base. Plain Markdown, versioned with the code. Open this
`docs/` folder as a vault in Obsidian to browse it with backlinks and the
graph view; any editor works too.

Current status and the work log are **not** here: they live in
[`agent.md`](../agent.md) at the repository root.

## Start here

New to the project or to Unity? Read in this order:

1. [[guide/01-unity-in-this-project]]: the Unity ideas the game uses, each
   shown on a real object.
2. [[guide/02-guided-tour]]: a 40-minute hands-on walk through the editor
   and the code.
3. [[architecture/project-map]]: where everything is, and "I want to change
   X → open Y".
4. [[architecture/before-new-scripts]]: what has to change before new
   gameplay scripts are added.

## Map of the docs

| Where | What goes there |
| --- | --- |
| `guide/` | Learning path for people new to the project or to Unity |
| [[design/README\|design/]] | The game: pillars, core loop, RPG systems, world, netrunning |
| [[architecture/overview\|architecture/]] | How the project is organised: [[architecture/project-map]], [[architecture/overview]] (code structure), [[architecture/before-new-scripts]], [[architecture/tooling]] (build, test, editor bridge) |
| `systems/` | One note per system, see below |
| [[decisions/README\|decisions/]] | Architecture decision records (ADRs) |
| [[assets/handoff\|assets/]] | Art pipeline, naming, and who owns which folders |
| [[backlog]] | Prioritised tasks, each tagged YOU WRITE / PAIR / CLAUDE |
| [[journal]] | Dated session notes: what was built, what was learned |
| `img/` | Pictures used by the notes; the floor plan is generated, see [[systems/level-warehouse]] |

System notes:

| Note | Covers |
| --- | --- |
| [[systems/player]] | the player object, its script, keys, what happens each frame |
| [[systems/player-movement]] | the movement maths inside `Simulate`, step by step |
| [[systems/camera]] | first- and third-person camera |
| [[systems/character-and-animation]] | the NEXUS model, LODs, Animator, clips |
| [[systems/doors-and-interaction]] | doors and the E key |
| [[systems/level-warehouse]] | the warehouse scene, its areas, collision, lights |
| [[systems/rendering]] | URP settings, materials, why the scene looks dark |
| [[systems/enemy-k7]] | the K7 robot package, not imported yet |

## Conventions

- One topic per file. Link with `[[wikilinks]]` between notes; use normal
  relative Markdown links for files outside `docs/`.
- A system note has these headings where they apply: Purpose, Status, Where
  it lives, How it works (or the note's own sections), Known problems, Open
  questions. It says when and how its facts were verified.
- When behaviour or a public API changes, the system note changes in the
  same commit.
- Numbers in the notes come from `bin/unity-inspect` (read-only reports in
  `tools/inspect/`); re-run it after the art changes.
- A decision that took real discussion gets an ADR, so it is not re-argued.
