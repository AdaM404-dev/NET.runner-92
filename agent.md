# NET.runner-92 agent handoff

Last updated: **2026-09-30**. This is the shared status file for AI agents working on the Unity project. Agents may edit it as the project changes. Keep statements evidence-based so the next agent and the team can see what is done, what is in progress, and what remains unverified.

## What this project is

- **Engine:** Unity 6.6, editor version `6000.6.2f1` (`ProjectSettings/ProjectVersion.txt`). The project uses Universal Render Pipeline (`Packages/manifest.json`).
- **Repository:** `https://github.com/AdaM404-dev/NET.runner-92`, with the Unity project at the repository root on `main`.
- **Assets:** the refined NEXUS character in `Assets/Characters/NEXUS/` and the near-future warehouse in `Assets/Environment/Warehouse_NearFuture/`. FBX models, textures, materials, animations, prefabs, documentation, and Unity `.meta` files are committed.
- **Scenes:** `Assets/Scenes/SampleScene.unity` is first in Build Settings. `MainTest.unity` contains the warehouse and playable NEXUS player; `CharacterPreview.unity` is a stationary character preview. Both preview scenes are also enabled in Build Settings.
- **Player code:** `Assets/Scripts/NexusPlayer.cs` drives a `CharacterController`, camera, and Animator. `Assets/Prefabs/NEXUS_Player.prefab` has its references assigned. `Assets/Scripts/NexusDoor.cs` handles nearby door toggles.
- **Human docs:** `README.md` explains opening/importing the project. `docs/systems/player-movement.md` (formerly `tutorial.md`) explains how to develop NEXUS character movement. `docs/` holds design, architecture, decisions and system notes; `CLAUDE.md` holds build/test commands and code conventions.

## Current status

| Area | Status as of 2026-09-30 | Evidence / limit |
| --- | --- | --- |
| GitHub connection | Done | Local `main` was synced with `origin/main` at `1c49333` before this handoff update. |
| Selected models | Done | The Unity-ready warehouse geometry FBX matched the near-future source by SHA-256; the NEXUS LOD0 FBX matched the refined character source. The imported assets and `.meta` files were committed. |
| Scenes and prefabs | Present; `MainTest` partly verified | `NEXUS_Player.prefab`, `MainTest.unity`, and `CharacterPreview.unity` are in the repository and listed in Build Settings. 2026-09-30 (Linux, `6000.6.2f1`): project imports and compiles with no compiler errors; `MainTest` opens and enters Play mode, `NexusPlayer` reports grounded at its spawn, and the `OnGUI` HUD draws. Movement input, doors and `CharacterPreview` were not exercised. |
| Movement input | Implemented with legacy input | `NexusPlayer.Update()` reads `Input.GetAxisRaw` and keys. `Active Input Handling` is set to `Both`; the new Input System actions asset is not connected to this script. |
| Documentation | Done | `README.md` covers import. `docs/systems/player-movement.md` covers movement implementation and extension. |
| Material rendering | Broken under URP (confirmed) | 2026-09-30: a Game view capture of `MainTest` in Play mode is entirely magenta apart from the HUD text. All 52 `.mat` files reference the Built-in Standard shader. No conversion has been done. |
| Agent tooling | Done | `CLAUDE.md`, `docs/` (Obsidian-readable knowledge base), headless `bin/unity-*` scripts, and the Unity CLI bridge through `com.unity.pipeline` are set up and verified; see `docs/architecture/tooling.md`. |
| Automated tests | None | `bin/unity-test edit` runs the Test Framework successfully and finds 0 tests. |

## Active work

No active task recorded after this handoff update. An agent starting work should add a row here and update it or remove it when finished.

| Started (date) | Agent / task | Files or area | State / next action |
| --- | --- | --- | --- |
| — | — | — | — |

## Open items

These are known verification or improvement items, not authorization to change scope without a related user request.

| ID | Item | Current evidence | Next useful check |
| --- | --- | --- | --- |
| V-01 | Verify import, compilation, and Play mode in this repository's `MainTest` and `CharacterPreview` scenes. | Partly done 2026-09-30: import and compile are clean; `MainTest` enters Play mode with the player grounded. Console showed no errors from project code, two warnings (Input Manager deprecation; `GPUResidentDrawer` unsupported on this GPU) and one editor-internal `SearchDatabase` exception. | Exercise movement, jump, view switch and doors in `MainTest`; open and check `CharacterPreview`. |
| V-02 | Convert materials to URP. | Confirmed broken 2026-09-30: `MainTest` renders fully magenta; all 52 materials use the Built-in Standard shader. | Convert the materials to URP/Lit (owner of the art folders to agree), then re-capture `MainTest` and `CharacterPreview`. |
| V-03 | Consider new Input System integration if requested. | `NexusPlayer` still uses legacy `Input`, while Player Settings allow Both. | Follow `docs/systems/player-movement.md`, replace the input reads, and verify movement is driven once per frame. |
| V-04 | Decide on Git LFS. | No LFS or LFS attributes; binaries up to 24 MB are in normal history (about 155 MB after nine commits). | Team decision; options in `docs/assets/handoff.md`. |
| V-05 | Keep Linux-editor changes to `Packages/` and `ProjectSettings/` out of commits. | Decided 2026-09-30: Samuel works on Linux, AdaM404 on Windows. Opening the project on Linux adds `com.unity.sdk.linux-x86_64` and `com.unity.toolchain.linux-x86_64-linux` (Linux-host toolchains) to the manifest, a `SENTIS_ANALYTICS_ENABLED` define, and a `com.unity.dt.app-ui` entry in `EditorBuildSettings.asset`. | No action; agents on Linux stage files by name. Whether these entries would cause problems in the Windows editor was not tested. See `docs/architecture/tooling.md`. |
| V-06 | No automated tests exist. | `bin/unity-test edit` finds 0 tests. `NexusPlayer` has a command-line auto-test that writes outside the repository. | Add assembly definitions and tests; see `docs/backlog.md` items 4 to 6. |

## How agents should work here

1. Read the user's current request, this file, `README.md`, and the relevant code. Check `git status` before editing so another person's work is not overwritten.
2. Add a concise **Active work** row with your objective and the area you will touch. Keep that row current if the work takes multiple turns.
3. Preserve Unity `.meta` files and GUID relationships. Keep generated `Library`, `Temp`, `Logs`, and build output out of Git. Check scene, prefab, and package references when moving assets.
4. Make the requested change and run only meaningful validation for that change. State clearly when Unity editor or runtime behavior was not tested.
5. Before finishing, update **Current status** and **Open items** as needed, append a **Work log** entry, and close your **Active work** row. Include the result, changed files, verification, and commit or PR link if one exists. Do not erase earlier log entries.

## Work log

| Date | Work completed | Verification / evidence | Commit |
| --- | --- | --- | --- |
| 2026-09-29 | Connected the Unity project to the GitHub repository and added the near-future warehouse and refined NEXUS Unity-ready assets with their metadata, prefabs, and scenes. | Key warehouse and character FBX files matched their sources by SHA-256; asset files had matching `.meta` files and no duplicate GUIDs were found at import time. | `7b71672` |
| 2026-09-29 | Added import instructions to `README.md`. | Reviewed project version, asset paths, and scenes before documenting them. | `06aa371` |
| 2026-09-29 | Reworked `tutorial.md` into a developer guide for coding NEXUS movement. | Checked the guide against `NexusPlayer.cs`, the prefab, and Animator parameters. | `1c49333` |
| 2026-09-30 | Created this handoff file and `AGENTS.md` so future agents can track status and progress. | Rechecked Git status, project version, Build Settings, model paths, and current documentation. | See Git history for this file |
| 2026-09-30 | Agent workflow setup (Claude Code with Samuel): added `CLAUDE.md`, the `docs/` knowledge base, `bin/unity-compile`, `unity-test`, `unity-build`, `unity-shot`, `.claude/` settings and skills (including the Unity CLI skill), `.gitattributes`; moved `tutorial.md` to `docs/systems/player-movement.md`; fixed the two references to the repository's old name in `README.md` and this file. No files under `Assets/`, `Packages/` or `ProjectSettings/` were committed. | Ran a first headless import and compile (exit 0, no compiler errors); `bin/unity-compile` passes; `bin/unity-test edit` runs and reports 0 tests; with the editor open, read the console, entered Play mode in `MainTest`, and captured the Game view through the Unity CLI (result: magenta, see V-02). `bin/unity-build` produced a Linux player (build result: Success); the player itself was not launched. | Branch `setup/agent-workflow`, PR #1 |
