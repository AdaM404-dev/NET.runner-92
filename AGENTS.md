# Rules for every AI agent

This file is the one set of rules for every AI agent that works on this
repository: Claude Code (Samuel), Codex (AdaM404) and any other. Claude
Code loads it through `CLAUDE.md`; Codex reads it directly. Tool-specific
files add to these rules and never replace them.

NET.runner-92 is a first-person cyberpunk RPG in Unity `6000.6.2f1` with
HDRP (on Linux the editor runs on Vulkan). The main map is the Northline
hospital. Two people work on it:

- **Samuel** (Linux, with Claude Code): gameplay code. He decides what gets
  built and reviews every pull request that changes code.
- **AdaM404** (Windows, with Codex): art and graphical content.

Everything you commit must work on both Linux and Windows.

## 1. Before you do anything: sync with `main`

`main` moves while you work: the other person and the other agent merge
into it too. Work on an old base causes conflicts, re-done work and broken
scenes. **Check before the first change of every task, and again right
before every commit, push and pull request.**

```bash
git fetch origin --prune
git status -sb                          # your branch and its upstream
git rev-list --count HEAD..origin/main  # commits on main that you do not have
```

Then:

| Situation | What to do |
| --- | --- |
| You are on `main` | Bring it up to date: `git merge --ff-only origin/main`. Then create a branch for your task (section 4). Never work on `main` itself. |
| Your branch is behind its own upstream (`behind N` in `git status -sb`) | Someone pushed to it: `git merge --ff-only @{u}`. |
| `origin/main` has commits your branch does not (the count above is not 0) | Merge them in: `git merge origin/main`. If you have never pushed the branch, `git rebase origin/main` is fine too. |
| Your branch's pull request is already merged, or the branch is gone from GitHub | Do not keep working on it. Update `main` and start a new branch. |
| A merge or rebase has conflicts, or local changes block it | Stop and tell the person you work with. Never discard their changes (`reset --hard`, `checkout --`, `stash drop`, `clean`) to get past it. |

After an update, run `git lfs pull` if art changed, and re-read `agent.md`:
another agent may have started on the same files.

On Samuel's Linux clone `git pull` fails while the Linux editor's changes
to `Packages/` and `ProjectSettings/` are unstaged (it rebases). Use the
`fetch` + `merge` commands above instead.

## 2. Where knowledge lives

Read these before you work. Do not copy their content into other files;
link to them.

- **`agent.md`**: the shared status file. It says what is done, in progress
  and unverified, and holds the work log. Read it at the start. Update it at
  the end: record your task under **Active work**, then update **Current
  status**, **Open items** and **Work log** (files changed, the checks you
  actually ran, the commit, any blocker). Pull before editing it and only
  append to its tables; several agents write to it.
- **`docs/`**: an Obsidian vault of plain Markdown. Start at
  `docs/README.md`.
  - `docs/architecture/project-map.md` says where every file and object
    is; check it before searching.
  - `docs/architecture/overview.md` covers the code structure, and
    `docs/architecture/coding-standards.md` the coding standard (section 6
    sums both up).
  - `docs/systems/` has one note per system. Read the matching note before
    changing a system. Update it in the same commit when behaviour or
    public API changes.
  - `docs/assets/handoff.md` covers the art pipeline and who owns which
    folders.
  - `docs/architecture/tooling.md` covers commands and platform quirks.
  - `docs/journal.md` holds dated session notes.
- **GitHub issues labelled `decision`**: every next step and open decision,
  each with why, what will change, risks and checks. The pinned Roadmap
  (issue #5) gives the order, and `docs/decisions/README.md` explains the
  labels. If you cannot reach GitHub, ask the person you work with to paste
  the issue.

## 3. Before a planned change

- Read its issue. Start only when the issue has `status: accepted`, or when
  Samuel says to go ahead. Then set `status: in progress`.
- A new next step or a decision that needs agreement gets its own `decision`
  issue, with the ten sections in `docs/decisions/README.md`. Issue #6 is
  the model.
- Answers given in chat are recorded as a comment on their issue.
- Before adding any new gameplay script, check
  `docs/architecture/before-new-scripts.md`. Its "must do first" steps come
  first unless Samuel explicitly skips one.

## 4. Git and pull requests

- One branch per task, created from an up-to-date `main` (section 1). Name
  it `<kind>/<topic>`, for example `feature/coding-standard-player`,
  `art/k7-materials` or `docs/install-guide`.
- Never push directly to `main`. Open a pull request (`gh pr create`) that
  fills in `.github/pull_request_template.md`. If it carries out an issue,
  it says `Closes #<number>`.
- Keep commits small, one vertical slice each. Record each commit in
  `agent.md`'s work log.
- Stage files by name, never with `git add -A` or `git add .` (section 7
  explains why).
- The repository is public. No secrets, keys, tokens or personal data in
  it, including in commits, issues and pull requests.
- A task is done when it compiles, its tests pass and, for a visual
  change, a screenshot has been looked at. Never report a result you did
  not see.

## 5. Who owns what

| Area | Owner | Rule for other agents |
| --- | --- | --- |
| Gameplay code in `Assets/NETRunner/`, `bin/`, `tools/`, `docs/` | Written by Claude Code for Samuel. Samuel reviews every change. | Any agent may change or add code, but only by following sections 6 and 7 and by opening a pull request that Samuel reviews. |
| Art: `Assets/Characters`, `Environment`, `Materials`, `Animations`, the art in `Assets/NETRunner92/`, `Northline/`, `ArtSource/` | AdaM404 | Do not modify without asking AdaM404. Gameplay nests art prefabs or makes variants; it never edits them in place. |
| `Packages/`, `ProjectSettings/` | Shared | Change only by pull request, and only with an intended change (section 7). |

Details: `docs/assets/handoff.md`.

## 6. Code: structure and standards

The full standard is `docs/architecture/coding-standards.md`. Read it
before you write or change any C#. Code that breaks it is fixed before
merging. Formatting that an editor can check is in `.editorconfig`. If this
summary and the standard ever disagree, the standard wins, and this file
gets fixed.

**Structure** (`docs/architecture/overview.md`):

- All code lives in feature folders: `Assets/NETRunner/<Feature>/`. Today
  they are `Core`, `Settings`, `Player`, `World`, `Enemy`, `Atmosphere` and
  `Diagnostics`. `Assets/NETRunner92/` holds only the hospital's art and
  scenes.
- Each feature has `Scripts/` with one assembly definition
  `NetRunner.<Feature>` and namespace `NetRunner.<Feature>`. Folder,
  assembly, `.asmdef` file name and namespace match.
- Each feature keeps editor tools in `Editor/` (`NetRunner.<Feature>.Editor`,
  Editor platform only).
- Tests sit next to the code in `Tests/EditMode/` and `Tests/PlayMode/`,
  each with its own test assembly.
- References point one way, towards `Core`. `Core` references no feature
  and has no engine references. The compiler enforces this, so `Core`
  cannot use `UnityEngine`.
- A new feature gets a new folder with its own assembly. No script goes
  outside an assembly definition.
- Where a new script goes: a rule or formula goes in `Core/Scripts/` with an
  EditMode test; a thing placed in the level goes in `World/`; anything of
  the player goes in `Player/`; a new area of the game gets a new feature
  folder.

**Standard** (`docs/architecture/coding-standards.md`):

- **Rules go in plain C# in `Core`.** Same input, same output. No scene
  access and no hidden state, and nothing reads `Time.deltaTime` (`dt` is
  passed in). Randomness comes in as a `System.Random`. Every rule has
  EditMode tests.
- **MonoBehaviours are thin.** They read input or events, call the rules
  and show the result. A component that starts a second job is split.
- **Content and tuning are data**: ScriptableObjects, or named constants
  until the data exists. No magic numbers in new code.
- **Composition, not inheritance.** Every class is `sealed` unless it is
  the base of a closed family.
- **Components find each other** through Inspector references,
  `GetComponent` on the same or a parent object, and C# `event`s. Never use
  `GameObject.Find`, `FindObjectOfType`, tag lookups, singletons or static
  mutable state at runtime.
- **Input comes only from the Input System.** Layers are used through
  `LayerMask` fields, never numbers.
- **Fields:**
  - no `public` mutable fields;
  - `[SerializeField] private` for Inspector values, and read-only
    properties for values other code reads (`public float Lift => lift;`);
  - access modifiers are always written, and each declaration has one
    field;
  - renaming a serialized field needs `[FormerlySerializedAs("old")]`, or
    the values saved in scenes are lost.
- **Naming:**
  - PascalCase for types, methods, properties, events and constants;
  - camelCase for fields, parameters and locals, with no `m_` or `_`;
  - interfaces start with `I`, booleans read as a question, and handlers
    are `On…` or `Handle…`;
  - one top-level type per file, and the file is named after it;
  - a test's name is a sentence stating the expected behaviour.
- **Member order:**
  1. nested types
  2. constants and static fields
  3. serialized fields
  4. private state
  5. events
  6. properties
  7. Unity messages, in the order Unity calls them
  8. public methods
  9. private methods
- **Formatting:**
  - braces on their own lines, always, also around a one-line `if`;
  - four spaces;
  - block-scoped `namespace X { … }` (Unity compiles C# 9);
  - `=>` only for one-line members;
  - `var` only when the type is visible on the same line;
  - `using System` first, then the other usings in alphabetical order.
- **Comments:** every public type gets a `/// <summary>`. Comments explain
  *why*, not *what*.
- **Tests:**
  - every `Core` rule has EditMode tests, and every component at least one
    PlayMode test of its main behaviour;
  - expected values come from the specification, never from the code's own
    output;
  - tests do not save scenes and write files only under `Logs/`.
- **Bringing old code to the standard** (#41–#45) follows the steps in the
  standard's section 8: pin today's behaviour with a test, move the rules,
  keep serialized names, and compare old and new side by side.

## 7. Unity and platform rules

- **Never hand-edit Unity YAML** (`.unity`, `.prefab`, `.asset`, `.mat`,
  `.controller`). Change it in the editor, through an editor script, or
  through the hospital's scene builders in `Assets/NETRunner/World/Editor/`.
- **Every asset keeps its `.meta`.** Move or rename an asset together with
  its `.meta` (`git mv` both) so its GUID survives. Never delete or
  regenerate a `.meta`.
- `Library/`, `Temp/`, `Logs/`, `Builds/` and `UserSettings/` stay out of
  git.
- **Each OS's editor rewrites some project files on its own.** The Linux
  editor adds Linux-host toolchain packages to `Packages/manifest.json` and
  `packages-lock.json`, and touches several `ProjectSettings` files. Never
  commit changes you did not intend, which is why files are staged by name.
  When such a file also has an intended change, stage only that change. See
  "Known platform quirks" in `docs/architecture/tooling.md`.
- **Case matters on Linux.** File and folder names must not differ only
  by case, and references must match a file's exact case.
- **Git LFS holds the art** (`.gitattributes` lists the types). A new art
  file type gets its `git lfs track` line before its first commit.

## 8. Building and testing

On Linux, with the editor closed, the `bin/` scripts run Unity in batch
mode. They are bash scripts for Samuel's machine.

```bash
bin/unity-compile             # import + compile; prints compiler errors only
bin/unity-test edit           # EditMode tests
bin/unity-test play           # PlayMode tests
bin/unity-build [windows]     # player into Builds/Linux/ (or Builds/Windows/)
bin/decisions                 # open decision issues, in order
```

On Windows, or with the editor open, use the editor's Console and Test
Runner (EditMode and PlayMode), and say in the pull request which tests you
ran. The hospital's 95 checks run as the PlayMode test `HospitalChecksTests`.
More: `docs/architecture/tooling.md`.
