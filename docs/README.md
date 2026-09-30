# NET.runner-92 docs

Project knowledge base. Plain Markdown, versioned with the code. Open this
`docs/` folder as a vault in Obsidian to browse it with backlinks and the
graph view; any editor works too.

Current status and the work log are **not** here: they live in
[`agent.md`](../agent.md) at the repository root.

| Where | What goes there |
| --- | --- |
| [[design/README\|design/]] | The game: pillars, core loop, RPG systems, world, netrunning |
| [[architecture/overview\|architecture/]] | How the code is organised; [[architecture/tooling]] for the build, test and editor-bridge setup |
| `systems/` | One note per system. First one: [[systems/player-movement]] |
| [[decisions/README\|decisions/]] | Architecture decision records (ADRs) |
| [[assets/handoff\|assets/]] | Art pipeline, naming, and who owns which folders |
| [[backlog]] | Prioritised tasks, each tagged YOU WRITE / PAIR / CLAUDE |
| [[journal]] | Dated session notes: what was built, what was learned |

## Conventions

- One topic per file. Link with `[[wikilinks]]` between notes; use normal
  relative Markdown links for files outside `docs/`.
- A system note has these headings: Purpose, Status, Public API, Data,
  Tests, Open questions.
- When behaviour or a public API changes, the system note changes in the
  same commit.
- A decision that took real discussion gets an ADR, so it is not re-argued.
