# NET.runner-92 agent handoff

Last updated: **2026-09-30**. This is the shared status file for AI agents working on the Unity project. Agents may edit it as the project changes. Keep statements evidence-based so the next agent and the team can see what is done, what is in progress, and what remains unverified.

## What this project is

- **Engine:** Unity 6.6, editor version `6000.6.2f1` (`ProjectSettings/ProjectVersion.txt`). The project uses Universal Render Pipeline (`Packages/manifest.json`).
- **Repository:** `https://github.com/AdaM404-dev/NET.runner-92-2026-09-29_22-29-57`, with the Unity project at the repository root on `main`.
- **Assets:** the refined NEXUS character in `Assets/Characters/NEXUS/` and the near-future warehouse in `Assets/Environment/Warehouse_NearFuture/`. FBX models, textures, materials, animations, prefabs, documentation, and Unity `.meta` files are committed.
- **Scenes:** `Assets/Scenes/SampleScene.unity` is first in Build Settings. `MainTest.unity` contains the warehouse and playable NEXUS player; `CharacterPreview.unity` is a stationary character preview. Both preview scenes are also enabled in Build Settings.
- **Player code:** `Assets/Scripts/NexusPlayer.cs` drives a `CharacterController`, camera, and Animator. `Assets/Prefabs/NEXUS_Player.prefab` has its references assigned. `Assets/Scripts/NexusDoor.cs` handles nearby door toggles.
- **Human docs:** `README.md` explains opening/importing the project. `tutorial.md` explains how to develop NEXUS character movement.

## Current status

| Area | Status as of 2026-09-30 | Evidence / limit |
| --- | --- | --- |
| GitHub connection | Done | Local `main` was synced with `origin/main` at `1c49333` before this handoff update. |
| Selected models | Done | The Unity-ready warehouse geometry FBX matched the near-future source by SHA-256; the NEXUS LOD0 FBX matched the refined character source. The imported assets and `.meta` files were committed. |
| Scenes and prefabs | Present | `NEXUS_Player.prefab`, `MainTest.unity`, and `CharacterPreview.unity` are in the repository and listed in Build Settings. Their runtime behavior in this URP project has not been independently verified. |
| Movement input | Implemented with legacy input | `NexusPlayer.Update()` reads `Input.GetAxisRaw` and keys. `Active Input Handling` is set to `Both`; the new Input System actions asset is not connected to this script. |
| Documentation | Done | `README.md` covers import. `tutorial.md` covers movement implementation and extension. |
| Material rendering | Needs verification | Imported preview materials use the Built-in Standard shader. They may need conversion to URP if they appear magenta. No conversion is recorded here. |

## Active work

No active task recorded after this handoff update. An agent starting work should add a row here and update it or remove it when finished.

| Started (date) | Agent / task | Files or area | State / next action |
| --- | --- | --- | --- |
| — | — | — | — |

## Open items

These are known verification or improvement items, not authorization to change scope without a related user request.

| ID | Item | Current evidence | Next useful check |
| --- | --- | --- | --- |
| V-01 | Verify import, compilation, and Play mode in this repository's `MainTest` and `CharacterPreview` scenes. | The assets were copied with metadata; no completed runtime check in this target project is recorded. | Open with Unity `6000.6.2f1`, inspect Console, and test both scenes. Record actual results. |
| V-02 | Check material rendering under URP. | Source materials use Built-in Standard shader. | Inspect scenes; convert affected materials if a requested task requires a visual fix. |
| V-03 | Consider new Input System integration if requested. | `NexusPlayer` still uses legacy `Input`, while Player Settings allow Both. | Follow `tutorial.md`, replace the input reads, and verify movement is driven once per frame. |

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
