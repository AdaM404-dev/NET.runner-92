# NET.runner-92 docs

Project knowledge base. Plain Markdown, versioned with the code. Open this
`docs/` folder as a vault in Obsidian to browse it with backlinks and the
graph view; any editor works too.

Current status and the work log are **not** here: they live in
[`agent.md`](../agent.md) at the repository root.

## Start here

New to the project or to Unity? Read in this order:

0. [[guide/00-install]]: download and install the project, open it, play
   the hospital, keep it up to date.
1. [[guide/01-unity-in-this-project]] and [[guide/02-guided-tour]]: the
   Unity ideas and a hands-on tour. Both still show the deleted warehouse
   and are rewritten for the hospital after #31; read
   [[systems/level-hospital]] and [[systems/player]] alongside them.
2. [[architecture/project-map]]: where everything is, and "I want to change
   X → open Y".
3. [[architecture/before-new-scripts]]: what has to change before new
   gameplay scripts are added.
4. The [Roadmap issue #5](https://github.com/AdaM404-dev/NET.runner-92/issues/5) on GitHub: every next step and open
   decision, in order, each with why and what will change.

## Map of the docs

| Where | What goes there |
| --- | --- |
| `guide/` | Learning path for people new to the project or to Unity |
| [[design/README\|design/]] | The game: pillars, core loop, RPG systems, world, netrunning |
| [[architecture/overview\|architecture/]] | How the project is organised: [[architecture/project-map]], [[architecture/overview]] (code structure), [[architecture/coding-standards]] (how every script is written), [[architecture/before-new-scripts]], [[architecture/tooling]] (build, test, editor bridge) |
| `systems/` | One note per system, see below |
| [[decisions/README\|decisions/]] | How decisions and next steps work: they are GitHub issues labelled `decision`, starting from the pinned [Roadmap #5](https://github.com/AdaM404-dev/NET.runner-92/issues/5) |
| [[assets/handoff\|assets/]] | Art pipeline, naming, and who owns which folders |
| [[backlog]] | Where the backlog is: the GitHub issues, and what was done before them |
| [[journal]] | Dated session notes: what was built, what was learned |
| `img/` | Pictures used by the notes |

System notes:

| Note | Covers |
| --- | --- |
| [[systems/level-hospital]] | the Northline hospital: the main map, its floors, route and scene structure |
| [[systems/player]] | the first-person player, keys, movement, LATCH-07 |
| [[systems/doors-and-interaction]] | the E key: doors, lift, stairs, reception terminal |
| [[systems/rendering]] | HDRP settings, materials, Vulkan on Linux |
| [[systems/quality-settings]] | Low / Medium / High, F2 and F3, resolution scaling |
| [[systems/performance]] | what the hospital costs, the benchmarks, the optimization plan (#36) |
| [[systems/enemy-k7]] | the K-7 robots patrolling the hospital |
| [[systems/character-and-animation]] | the NEXUS model and Animator, unused since #31 |

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
- A next step or a decision gets a GitHub issue with the ten sections described
  in [[decisions/README]], so it is not re-argued.
