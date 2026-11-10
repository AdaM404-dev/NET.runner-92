# Project map

Where everything is, what uses it, and who owns it. Start here when you are
looking for something. Rewritten on 2026-11-10 for #31 PR A: the Northline
hospital is the main map, the project uses HDRP, and the warehouse, the
main-menu prototype and `NexusPlayer` are gone. #31 PR B moves the hospital
code from `Assets/NETRunner92/` into feature folders; this map changes
again then.

Ownership follows the proposal in [[assets/handoff]], which AdaM404 has not
confirmed yet.

## The repository

```
NET.runner-92/
├─ Assets/            everything Unity imports: the game itself
├─ Packages/          which Unity packages the project uses (manifest.json)
├─ ProjectSettings/   project-wide settings: layers, quality, input, build list, …
├─ ArtSource/         source art Unity does not import (the K7 robot package)
├─ Northline/         the hospital's production material: Blender sources, cinematic, renders, design notes
├─ docs/              this knowledge base; open the folder in Obsidian
├─ bin/               terminal helpers: unity-compile, unity-test, unity-build, unity-shot, unity-inspect
├─ tools/inspect/     the read-only reports behind bin/unity-inspect
├─ .claude/           Claude Code settings and skills
├─ CLAUDE.md          instructions for Claude Code
├─ AGENTS.md, agent.md  instructions and shared status for every AI agent
└─ README.md          how to open the project
Library/, Temp/, Logs/, Builds/, UserSettings/  are created by Unity and never committed.
```

## `Assets/`

| Folder | What is in it | Files | Owner |
| --- | --- | ---: | --- |
| `NETRunner92/` | **the hospital**: scenes, environment, player, K-7, atmosphere, rendering, code, editor builders (below) | 285 | code: Claude/Samuel; art: AdaM404 |
| `NETRunner/Core/` | rules in plain C# (`VerticalMotion`, `Easing`, `QualityChoice`, `StartResolution`, `FrameRateMeter`) and their EditMode tests, two assembly definitions | 12 | Claude/Samuel |
| `NETRunner/Settings/` | `QualitySwitch` (F2 quality, F3 FPS) and its PlayMode tests, two assembly definitions; see [[systems/quality-settings]] | 4 | Claude/Samuel |
| `HDRPDefaultResources/` | HDRP global settings and default volume profile | 2 | shared, change by pull request |
| `UI Toolkit/` | Unity's default runtime theme, used by the LATCH-07 screen | 1 | shared |
| `Characters/NEXUS/`, `Materials/`, `Animations/`, `Prefabs/` | the NEXUS character: models, 24 URP materials, Animator, two prefabs. **Unused since #31**; AdaM404 decides on #31 whether it stays | 128 | AdaM404 |
| `Documentation/` | character licences | 2 | AdaM404 |
| `InputSystem_Actions.inputactions` | Unity's default input actions, the project-wide actions asset; no script reads it | 1 | Claude/Samuel |

Inside `NETRunner92/`:

| Folder | What is in it |
| --- | --- |
| `Scenes/` | `Hospital_Exploration` (gameplay) and `LATCH07_ViewmodelCheck` |
| `Environment/Hospital`, `Exterior` | hospital and grounds geometry (`MeshData.bytes`, Git LFS) |
| `Environment/Materials`, `Textures` | 151 HDRP Lit materials and their textures |
| `Environment/K7/` | the K-7 robot: FBX, seven HDRP materials, `K7_Patrol.prefab` |
| `Player/Scripts/` | `FirstPersonController`, `LatchViewmodel`, `LatchScreen`, `LatchRelay`, `SecurityCctv` |
| `Player/Latch07/`, `Player/Prefabs/` | the LATCH-07 arms and tablet; `PlayerController.prefab` |
| `Core/` | hospital MonoBehaviours: doors, lift, stairs, interaction, terminal, patrol, residual power, plus diagnostics used by the validator |
| `Atmosphere/` | fog volumes per floor, ambience audio and mixer, rain and dust VFX, `PracticalPulse` |
| `Rendering/` | the pipeline assets `Northline_HDRP` (High), `Northline_HDRP_Medium`, `Northline_HDRP_Low`, and the exposure profile |
| `Editor/` | scene builders and `PlayModeValidation` (menu **NET.runner-92**) |

Every file in `Assets/` has a `.meta` file next to it holding its unique id
(GUID). Scenes and prefabs refer to assets by that id, so a file must always
move or be renamed together with its `.meta`.

## Scenes

| Build order | Scene | What it is |
| ---: | --- | --- |
| 0 | `NETRunner92/Scenes/Hospital_Exploration` | **the gameplay scene**: a built game starts here; see [[systems/level-hospital]] |
| — | `NETRunner92/Scenes/LATCH07_ViewmodelCheck` | isolated check of the LATCH-07 arms; not in the build |

## Code

The hospital code is all in namespace `NETRunner92`, compiled into Unity's
default `Assembly-CSharp` (no assembly definitions yet), so it does not
follow [[architecture/overview]] and [[architecture/coding-standards]] yet;
#31 PR B and C fix that.

| File | What it does | Note |
| --- | --- | --- |
| `NETRunner92/Player/Scripts/FirstPersonController.cs` | movement, mouse look, cursor | [[systems/player]] |
| `NETRunner92/Core/HospitalInteraction.cs` | aiming ray, prompt, E key; base class `HospitalInteractable` | [[systems/doors-and-interaction]] |
| `NETRunner92/Core/HospitalDoor.cs`, `HospitalLift.cs`, `HospitalLiftButton.cs`, `StairAccess.cs` | doors, lift, first-ascent rules | [[systems/doors-and-interaction]] |
| `NETRunner92/Core/ReceptionSecurityTerminal.cs`, `Player/Scripts/SecurityCctv.cs` | reception puzzle and live CCTV | [[systems/doors-and-interaction]] |
| `NETRunner92/Player/Scripts/Latch*.cs` | LATCH-07 tablet | [[systems/player]] |
| `NETRunner92/Core/K7Patrol.cs` | K-7 patrol walk | [[systems/enemy-k7]] |
| `NETRunner92/Atmosphere/*.cs`, `Core/ResidualPower.cs` | fog zones, ambience, flickering lights | [[systems/level-hospital]] |
| `NETRunner92/Core/*Diagnostics.cs`, `*Capture.cs`, `RoomReachability.cs`, `ExplorationRoute.cs` | Play-mode checks and captures run by the validator | — |
| `NETRunner92/Editor/*.cs` | scene builders and `PlayModeValidation` | [[systems/level-hospital]] |
| `NETRunner/Core/Scripts/VerticalMotion.cs`, `Easing.cs` | plain C# rules; currently unused by the hospital | — |
| `NETRunner/Core/Scripts/QualityChoice.cs`, `StartResolution.cs`, `FrameRateMeter.cs` | rules behind the quality switch | [[systems/quality-settings]] |
| `NETRunner/Settings/Scripts/QualitySwitch.cs` | F2 quality level, F3 FPS counter, first-start resolution, resolution scaling on screen cameras | [[systems/quality-settings]] |
| `NETRunner/Core/Tests/EditMode/*.cs`, `NETRunner/Settings/Tests/PlayMode/*.cs` | 28 EditMode and 5 PlayMode tests | the test runner |

Tests: 28 EditMode (`Core`) and 5 PlayMode (`Settings`). The hospital's 95 Play-mode checks run through
`PlayModeValidation`, not the test runner, until #31 PR B turns them into
NUnit tests.

## Project settings that matter

| Setting | Value | Where |
| --- | --- | --- |
| Render pipeline | HDRP; quality levels Low / Medium (default) / High, each with its own pipeline asset ([[systems/quality-settings]]) | Project Settings > Graphics, Quality |
| Start resolution | 1920 × 1080, full-screen window (the first start also caps the screen at 1080p) | Project Settings > Player > Resolution and Presentation |
| Layers | 8 `Near-detail geometry`, 9 `First-person viewmodel`, 10 `Enemy` and 11 `Interactable` (reserved), plus Unity's built-in ones | Project Settings > Tags and Layers |
| Active input handling | **Input System Package (New)** only | Project Settings > Player > Other Settings |
| Company name | `NET.runner` (decides where save files go) | Project Settings > Player |
| Product name, version | `NET.runner-92`, 0.1.0 | same |
| Fixed timestep | 0.02 s | Project Settings > Time |

## Packages that matter (`Packages/manifest.json`)

| Package | Why it is there | In use |
| --- | --- | --- |
| High Definition RP 17.6.0 | the render pipeline (also brings Shader Graph and VFX Graph) | yes |
| Input System 1.20.0 | keyboard and mouse input | yes, by the hospital code |
| Test Framework 1.8.0 | automated tests | yes: 10 EditMode tests |
| AI Navigation 2.0.14 | navigation meshes for enemies | not yet |
| Timeline, uGUI, Visual Scripting | cutscenes, UI, node-based scripting | not used by any asset |
| Pipeline 0.8 (experimental) | lets the `unity` terminal tool drive the editor | yes, by our tooling |
| AI Assistant, AI Inference | Unity's AI assistant and neural-network runtime | not used by the game |
| Rider, Visual Studio integration | code editor support | yes |
| Version Control (collab-proxy) | Unity's own version control | no; we use git |

## I want to … → open this

| I want to change | Open |
| --- | --- |
| walking or sprint speed, mouse sensitivity, eye height | `PlayerController` → First Person Controller ([[systems/player]]) |
| which key does what | `FirstPersonController.Update()`, `HospitalInteraction.Update()`, `LatchViewmodel` |
| how far E reaches | `HospitalInteraction.Reach` (2.65 m) |
| a door, the lift or the first-ascent rules | `HospitalDoor`, `HospitalLift`, `StairAccess` ([[systems/doors-and-interaction]]) |
| the reception puzzle or CCTV feeds | `ReceptionSecurityTerminal`, `SecurityCctv` |
| K-7 patrol speed or route | the K-7 object → K7 Patrol → Speed / Route ([[systems/enemy-k7]]) |
| fog, lights, storm | `Assets/NETRunner92/Atmosphere/` ([[systems/rendering]]) |
| graphics quality | Edit > Project Settings > Quality, and the three pipeline assets in `Assets/NETRunner92/Rendering/` ([[systems/quality-settings]]) |
| which scene a built game opens | File > Build Profiles > Scene List |
| layer or tag names | Edit > Project Settings > Tags and Layers |
| the hospital's Blender source or design notes | [`Northline/`](../../Northline/README.md) |

## Finding things yourself

In the editor:

| To find | Do |
| --- | --- |
| an object by name | type part of the name into the Hierarchy search box |
| all objects with a component | Hierarchy search `t:HospitalDoor`, `t:Light`, `t:Camera` |
| an asset | Project window search, e.g. `t:Prefab`, `t:Scene`, `t:Material` |
| where an asset is used in the open scene | right-click it in the Project window > Find References In Scene |
| an object in the Scene view | select it in the Hierarchy and press F |

From the terminal (the editor must be open):

```bash
bin/unity-inspect                  # scene, environment, animation reports
unity command find_gameobjects --type HospitalDoor
unity command find_assets --type Prefab
grep -rn "Reach" Assets/NETRunner92 --include=*.cs   # plain text search in the code
```
