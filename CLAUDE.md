# NET.runner-92

First-person cyberpunk RPG in Unity `6000.6.2f1` (URP). Two people: Samuel
(gameplay code, on Linux, works with Claude Code) and AdaM404 (art and
graphical content, on Windows, works with his own AI agent). Anything
committed has to work on both platforms.

@AGENTS.md

`agent.md` is the single shared status file for every agent. Read it at the
start of a session and update it at the end, following its own protocol. Pull
before editing it and only append to its tables; two agents write to it.

## Where knowledge lives

- `agent.md` — what is done, in progress, unverified; the work log.
- `docs/` — an Obsidian vault of plain Markdown. Start at `docs/README.md`.
  - `architecture/project-map.md` says where every file and object is;
    check it before searching.
  - `guide/` the learning path for Samuel, `design/` the game itself,
    `architecture/` project and code structure, `systems/` one note per
    system, `decisions/README.md` how decision issues work, `assets/` art
    pipeline and handoff rules, `journal.md` session notes.
- **GitHub issues labelled `decision`**: every next step and open decision,
  each with why, what will change, risks and checks. The pinned Roadmap
  (issue #5) gives the order; `bin/decisions` lists them in the terminal.
- Do not restate in this file what `agent.md` or `docs/` already record.

Read the matching `docs/systems/` note before changing a system, and update
it in the same commit when behaviour or public API changes. A next step or a
decision that needs agreement gets a `decision` issue with the ten sections
in `docs/decisions/README.md` (issue #6 is the model).

**Before working on a planned change**, read its issue. Start only when it
has `status: accepted` or Samuel says to go ahead; then set
`status: in progress`. The pull request says `Closes #<number>` and fills in
the Outcome section of the template. Record answers given in chat as an
issue comment.

**Before adding any new gameplay script**, check
`docs/architecture/before-new-scripts.md`: its seven "must do first" steps
come first unless Samuel explicitly skips one.

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
unity command run_tests --mode editor                        # EditMode tests
unity command run_tests --mode playmode --async_tests true   # PlayMode tests, then
unity command test_status                                    # poll until "completed"
bin/unity-shot <name> [game|scene]    # screenshot into Logs/shots/
bin/unity-inspect [player|scene|environment|animation|doors|map]  # read-only facts
```

Decisions on GitHub (needs `gh`):

```bash
bin/decisions                         # open decision issues, foundation steps in order
gh issue view <n>                     # read one
gh issue comment <n> --body "…"       # record an answer or an outcome
gh issue edit <n> --add-label "status: accepted" --remove-label "status: proposed" --remove-label "needs Samuel"
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

- Code lives in feature folders, `Assets/NETRunner/<Feature>/` (today
  `Core`, `World`, `Player` and the `MainMenu` prototype). Each has
  `Scripts/` with one assembly definition and namespace `NetRunner.<Feature>`,
  and its tests next to it in `Tests/EditMode/` or `Tests/PlayMode/`, each
  with its own test assembly. A new feature gets a new folder. References
  point one way, towards `Core`; `Core` references no other feature. Layout,
  rules and "where does my new script go": `docs/architecture/overview.md`.
- Game rules (stats, inventory, quests, dialogue, hacking, saves) are plain C#
  with no `UnityEngine` scene dependency, live in `Core` and are covered by
  EditMode tests. MonoBehaviours stay thin: read input, call the rules, show
  the result.
- Content is data (ScriptableObjects), not code.
- Use the Input System package for new input code, not legacy `Input`.
- One statement per line, braces on their own lines, spaces around operators.
  `.editorconfig` holds these rules for Rider and Visual Studio.

## Unity rules

- Never hand-edit `.unity`, `.prefab`, `.asset`, `.mat` or `.controller`
  YAML. Change them with `unity command` asset commands or an editor script.
- Every asset keeps its `.meta`; move or rename assets together with it
  (`git mv` both) so GUIDs survive. Never regenerate a `.meta`.
- `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/` stay out of git.
- On Linux the editor rewrites `Packages/manifest.json`,
  `packages-lock.json` and several `ProjectSettings` files on its own. Never
  commit those: the added packages are Linux-host toolchains and the
  teammate works on Windows. Stage files by name, not `git add -A`.
  See "Known platform quirks" in `docs/architecture/tooling.md`.
- Art folders (`Assets/Characters`, `Environment`, `Materials`, `Animations`)
  and the preview scenes belong to the teammate. Do not modify them without
  asking; see `docs/assets/handoff.md`.
- Never save `MainTest` or `CharacterPreview` as a side effect: URP marks
  them unsaved on its own. Entering and leaving Play mode is safe.

## Git

- Work on a branch and open a PR with `gh`; no direct pushes to `main`.
- Small commits, one vertical slice each. Record the commit in `agent.md`'s
  work log.
- The repository is public. No secrets, keys or personal data in it.
