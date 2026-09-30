# NET.runner-92

First-person cyberpunk RPG in Unity `6000.6.2f1` (URP). Two people: Samuel
(gameplay code, works with Claude Code) and AdaM404 (art and graphical
content, works with his own AI agent).

@AGENTS.md

`agent.md` is the single shared status file for every agent. Read it at the
start of a session and update it at the end, following its own protocol. Pull
before editing it and only append to its tables; two agents write to it.

## Where knowledge lives

- `agent.md` — what is done, in progress, unverified; the work log.
- `docs/` — an Obsidian vault of plain Markdown. Start at `docs/README.md`.
  - `design/` the game itself, `architecture/` how the code is organised,
    `systems/` one note per system, `decisions/` ADRs, `assets/` art pipeline
    and handoff rules, `backlog.md` tagged tasks, `journal.md` session notes.
- Do not restate in this file what `agent.md` or `docs/` already record.

Read the matching `docs/systems/` note before changing a system, and update
it in the same commit when behaviour or public API changes. A decision that
took real discussion gets an ADR in `docs/decisions/`.

## Commands

Editor closed (batch mode; exits 3 if the editor has the project open):

```bash
bin/unity-compile            # import + compile; prints compiler errors only
bin/unity-test edit          # EditMode tests; summary + failures
bin/unity-test play          # PlayMode tests
bin/unity-test edit <filter> # only tests whose name matches
bin/unity-build              # Linux player into Builds/Linux/
```

Editor open (Unity CLI bridge through `com.unity.pipeline`):

```bash
unity status                          # connected and "ready"?
unity command console --level error   # read the console
unity command recompile               # then: unity command recompile_status
unity command run_tests
bin/unity-shot <name> [game|scene]    # screenshot into Logs/shots/
```

`unity command` with no arguments lists everything the editor exposes; the
`unity-cli` skill is the reference. Setup notes and quirks:
`docs/architecture/tooling.md`. Full logs land in `Logs/` (git-ignored).

A task is not done until it compiles and its tests pass. For visual changes,
look at a screenshot; do not report a visual result that was not seen.

## How we split work

Every backlog task carries one tag, agreed before starting:

- **YOU WRITE** — Samuel implements it to learn. Give the spec, signatures and
  test names; hints in stages (concept, then API, then a small snippet). Full
  solution only on request. Review what he writes.
- **PAIR** — Claude writes it live and explains the non-obvious lines, then
  leaves Samuel a small follow-up change.
- **CLAUDE** — Claude builds it; Samuel reviews the result.

Samuel's background is JavaScript and Java. Explain C#- or Unity-specific
ideas briefly the first time they come up.

## Code conventions

- Gameplay code lives in `Assets/Scripts/`. Keep that path; prefabs and the
  teammate's docs reference it.
- Game rules (stats, inventory, quests, dialogue, hacking, saves) are plain C#
  with no `UnityEngine` scene dependency, covered by EditMode tests.
  MonoBehaviours stay thin: read input, call the rules, show the result.
- Content is data (ScriptableObjects), not code.
- Target layout: namespaces and assembly definitions `NetRunner.Core`,
  `NetRunner.Gameplay`, `NetRunner.UI`, `NetRunner.Editor`,
  `NetRunner.Tests.EditMode`, `NetRunner.Tests.PlayMode`. Not created yet;
  see `docs/architecture/overview.md` for current status.
- Use the Input System package for new input code, not legacy `Input`.
- One statement per line, braces on their own lines, spaces around operators.
  The two existing scripts predate this and are to be refactored.

## Unity rules

- Never hand-edit `.unity`, `.prefab`, `.asset`, `.mat` or `.controller`
  YAML. Change them with `unity command` asset commands or an editor script.
- Every asset keeps its `.meta`; move or rename assets together with it
  (`git mv` both) so GUIDs survive. Never regenerate a `.meta`.
- `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/` stay out of git.
- On Linux the editor rewrites `Packages/manifest.json`,
  `packages-lock.json` and two `ProjectSettings` files on its own. Do not
  commit those changes unless the team has agreed to; stage files by name.
  See "Known platform quirks" in `docs/architecture/tooling.md`.
- Art folders (`Assets/Characters`, `Environment`, `Materials`, `Animations`)
  and the preview scenes belong to the teammate. Do not modify them without
  asking; see `docs/assets/handoff.md`.

## Git

- Work on a branch and open a PR with `gh`; no direct pushes to `main`.
- Small commits, one vertical slice each. Record the commit in `agent.md`'s
  work log.
- The repository is public. No secrets, keys or personal data in it.
