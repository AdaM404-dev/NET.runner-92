# Project map

Where everything is, what uses it, and who owns it. Start here when you are
looking for something. Rewritten on 2026-11-10 for #31 PR A: the Northline
hospital is the main map, the project uses HDRP, and the warehouse, the
main-menu prototype and `NexusPlayer` are gone. Updated for #31 PR B: the
hospital code moved from `Assets/NETRunner92/` into feature folders under
`Assets/NETRunner/`; `Assets/NETRunner92/` now holds only art and scenes.

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
├─ tools/benchmark/   run-benchmarks.bat (shipped in the Windows alpha zips) and run-benchmarks.sh (the Linux build)
├─ .claude/           Claude Code settings and skills
├─ AGENTS.md          the rules for every AI agent: sync with main, workflow, structure, coding standard
├─ CLAUDE.md          Claude Code's additions; loads AGENTS.md
├─ agent.md           shared status and work log for every AI agent
└─ README.md          how to open the project
Library/, Temp/, Logs/, Builds/, UserSettings/  are created by Unity and never committed.
```

## `Assets/`

| Folder | What is in it | Files | Owner |
| --- | --- | ---: | --- |
| `NETRunner92/` | **the hospital's art and scenes**: scenes, environment, LATCH-07 arms and player prefab, K-7, atmosphere assets, rendering (below); no code since #31 PR B | 252 | art: AdaM404; gameplay scenes: Claude/Samuel |
| `NETRunner/Core/` | rules in plain C# (`PlanarMotion`, `VerticalMotion`, `MotionSteps`, `ViewMotion`, `LatchSession`, `LatchPhase`, `LatchStatus`, `LatchAccent`, `CctvFeeds`, `Easing`, `QualityChoice`, `StartResolution`, `FrameRateMeter`, `LaunchArguments`, `RenderFeature`, `RenderFeatureList`, `OcclusionRole`, `OcclusionRules`, `Patrol`, `K7Gait`, `ZoneBlend`, `Ambience`, `RainField`, `AmbientEvents`, `LightPulse`, `RandomRange`) and their EditMode tests, two assembly definitions | 49 | Claude/Samuel |
| `NETRunner/Player/`, `World/`, `Enemy/`, `Atmosphere/`, `Diagnostics/` | the hospital's code, one feature each, with their tests (see "Code" below) | | Claude/Samuel |
| `NETRunner/Settings/` | `QualitySwitch` (F2 quality, F3 FPS) and its PlayMode tests, two assembly definitions; see [[systems/quality-settings]] | 4 | Claude/Samuel |
| `HDRPDefaultResources/` | HDRP global settings and default volume profile | 2 | shared, change by pull request |
| `UI Toolkit/` | Unity's default runtime theme, used by the LATCH-07 screen | 1 | shared |
| `InputSystem_Actions.inputactions` | Unity's default input actions, the project-wide actions asset; no script reads it | 1 | Claude/Samuel |

Inside `NETRunner92/`:

| Folder | What is in it |
| --- | --- |
| `Scenes/` | `Hospital_Exploration` (gameplay; its baked occlusion data in `Hospital_Exploration/OcclusionCullingData.asset`) and `LATCH07_ViewmodelCheck` |
| `Environment/Hospital`, `Exterior` | hospital and grounds geometry (`MeshData.bytes`, Git LFS) |
| `Environment/Materials`, `Textures` | 151 HDRP Lit materials and their textures |
| `Environment/K7/` | the K-7 robot: FBX, seven HDRP materials, `K7_Patrol.prefab` |
| `Player/Latch07/`, `Player/Prefabs/` | the LATCH-07 arms and tablet; `PlayerController.prefab` |
| `Atmosphere/` | fog volumes per floor, ambience audio and mixer, rain and dust VFX |
| `Rendering/` | the pipeline assets `Northline_HDRP` (High), `Northline_HDRP_Medium`, `Northline_HDRP_Low`, and the exposure profile |

Every file in `Assets/` has a `.meta` file next to it holding its unique id
(GUID). Scenes and prefabs refer to assets by that id, so a file must always
move or be renamed together with its `.meta`.

## Scenes

| Build order | Scene | What it is |
| ---: | --- | --- |
| 0 | `NETRunner92/Scenes/Hospital_Exploration` | **the gameplay scene**: a built game starts here; see [[systems/level-hospital]] |
| — | `NETRunner92/Scenes/LATCH07_ViewmodelCheck` | isolated check of the LATCH-07 arms; not in the build |

## Code

Since #31 PR B the hospital code is in feature folders, each with its own
assembly and namespace `NetRunner.<Feature>` ([[architecture/overview]]).
The code is unchanged from Northline apart from `Enemy` (#41),
`Atmosphere` (#42) and `Player` (#43), which follow
[[architecture/coding-standards]]; #44 and #45 do the rest, one feature each. Paths below are under `Assets/NETRunner/`.

| File | What it does | Note |
| --- | --- | --- |
| `Player/Scripts/FirstPersonController.cs` | movement, mouse look, cursor, with the rules from `PlanarMotion`, `VerticalMotion`, `MotionSteps` and `ViewMotion` | [[systems/player]] |
| `Player/Scripts/HospitalInteraction.cs`, `IInteractable.cs`, `ITerminal.cs` | aiming ray, prompt, E key; the interfaces `IInteractable` and `ITerminal` | [[systems/doors-and-interaction]] |
| `Player/Scripts/Latch*.cs`, `SecurityCctv.cs` | LATCH-07 tablet and live CCTV, with the rules from `LatchSession`, `LatchStatus` and `CctvFeeds` | [[systems/player]] |
| `Player/Tests/PlayMode/*.cs` | walking, sprint, gravity, look; aiming; the relay link; the CCTV feeds (on test-built objects); the tablet's phases and screen (in the hospital) | [[systems/player]] |
| `Player/Editor/LatchViewmodelBuilder.cs` | LATCH-07 import check scene | [[systems/player]] |
| `World/Scripts/HospitalDoor.cs`, `HospitalLift.cs`, `HospitalLiftButton.cs`, `StairAccess.cs` | doors, lift, first-ascent rules | [[systems/doors-and-interaction]] |
| `World/Scripts/ReceptionSecurityTerminal.cs` | reception puzzle that unlocks CCTV | [[systems/doors-and-interaction]] |
| `World/Scripts/ResidualPower.cs`, `RoomReachability.cs`, `ExplorationRoute.cs` | flickering emergency power; room plan and stair route used by the checks | [[systems/level-hospital]] |
| `Enemy/Scripts/K7Patrol.cs` | K-7 patrol: moves the robot and turns its bones, with the rules from `Patrol` and `K7Gait` | [[systems/enemy-k7]] |
| `Enemy/Tests/PlayMode/K7PatrolTests.cs` | walks, legs swing, turns round at the end, carries on after being moved | [[systems/enemy-k7]] |
| `Atmosphere/Scripts/*.cs` | fog zones, weather and ambience, pulsing lights, with the rules from `ZoneBlend`, `Ambience`, `RainField`, `AmbientEvents` and `LightPulse` | [[systems/atmosphere]] |
| `Atmosphere/Tests/PlayMode/*.cs` | zone weights, rain, crossfade, dust, steam, monitor, pulsing lights on a stand-in site | [[systems/atmosphere]] |
| `Diagnostics/Scripts/*Diagnostics.cs` | the hospital's 95 Play-mode checks (editor only) | [[systems/level-hospital]] |
| `Diagnostics/Scripts/PerformanceCapture.cs`, `RoutePerformanceCapture.cs`, `FocusedInputCapture.cs` | the built game's benchmark and input-check modes | [[systems/performance]] |
| `Diagnostics/Scripts/RenderFeatureSwitch.cs` | `-net92-without`: switches rendering features off for one benchmark run | [[systems/performance]] |
| `Diagnostics/Tests/PlayMode/HospitalChecksTests.cs` | runs the 95 checks as one NUnit test | [[architecture/tooling]] |
| `Diagnostics/Tests/PlayMode/RenderFeatureSwitchTests.cs` | each switched-off feature changes only its own camera or light setting | [[systems/performance]] |
| `World/Tests/PlayMode/DoorInteractionTests.cs` | doors start shut, aim, open/close, lock | [[systems/doors-and-interaction]] |
| `World/Editor/*.cs` | scene builders (one partial class `HospitalSceneBuilder`, including `HospitalOcclusion.cs`, which bakes occlusion culling) and `PlayModeValidation` | [[systems/level-hospital]] |
| `NETRunner/Core/Scripts/PlanarMotion.cs`, `VerticalMotion.cs`, `MotionSteps.cs`, `ViewMotion.cs` | the player's walking, gravity, movement steps, mouse look and head bob | [[systems/player]] |
| `NETRunner/Core/Scripts/LatchSession.cs`, `LatchPhase.cs`, `LatchStatus.cs`, `LatchAccent.cs`, `CctvFeeds.cs` | the LATCH-07 tablet's phases and screen, and the CCTV feeds | [[systems/player]] |
| `NETRunner/Core/Scripts/QualityChoice.cs`, `StartResolution.cs`, `FrameRateMeter.cs`, `LaunchArguments.cs` | rules behind the quality switch | [[systems/quality-settings]] |
| `NETRunner/Core/Scripts/RenderFeature.cs`, `RenderFeatureList.cs` | the features a benchmark run can switch off, and their names | [[systems/performance]] |
| `NETRunner/Core/Scripts/OcclusionRole.cs`, `OcclusionRules.cs` | which part an object plays in the baked occlusion culling | [[systems/level-hospital]] |
| `NETRunner/Core/Scripts/Patrol.cs`, `K7Gait.cs` | the K-7's patrol walk and its leg, arm and head angles | [[systems/enemy-k7]] |
| `NETRunner/Core/Scripts/ZoneBlend.cs`, `Ambience.cs`, `RainField.cs`, `AmbientEvents.cs`, `LightPulse.cs`, `RandomRange.cs`, `Easing.cs` | the atmosphere's rules: zone weights, volumes, rain streaks, lightning and sounds, pulsing lights | [[systems/atmosphere]] |
| `NETRunner/Settings/Scripts/QualitySwitch.cs` | F2 quality level, F3 FPS counter, first-start resolution, resolution scaling on screen cameras | [[systems/quality-settings]] |
| `NETRunner/Core/Tests/EditMode/*.cs`, `NETRunner/Settings/Tests/PlayMode/*.cs` | 181 EditMode and 5 PlayMode tests | the test runner |

Tests: 181 EditMode (`Core`) and 63 PlayMode (5 in `Settings`, 27 in
`Player`, 5 in `World`, 5 in `Enemy`, 15 in `Atmosphere`, 6 in `Diagnostics`: 5 for the
benchmark's feature switch and 1 that runs the hospital's 95 checks in
about 45 seconds).

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
| Test Framework 1.8.0 | automated tests | yes: 181 EditMode, 63 PlayMode tests |
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
| which key does what | `FirstPersonController.Update()`, `HospitalInteraction.Update()`, `LatchViewmodel.ReadInput()`; what the tablet's keys do in each phase: `Core`'s `LatchSession` |
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
bin/unity-inspect                  # scene and environment reports
unity command find_gameobjects --type HospitalDoor
unity command find_assets --type Prefab
grep -rn "Reach" Assets/NETRunner --include=*.cs   # plain text search in the code
```
