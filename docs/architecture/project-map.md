# Project map

Where everything is, what uses it, and who owns it. Start here when you are
looking for something. Gameplay measurements are from `df3a31d` (2026-09-30); repository additions
were checked through merged PRs #20–#22 on 2026-10-03.

Ownership follows the proposal in [[assets/handoff]], which AdaM404 has not
confirmed yet.

## The repository

```
NET.runner-92/
├─ Assets/            everything Unity imports: the game itself
├─ Packages/          which Unity packages the project uses (manifest.json)
├─ ProjectSettings/   project-wide settings: layers, quality, input, build list, …
├─ ArtSource/         source art that Unity does not import (K7 robot and editable warehouse packages)
├─ docs/              this knowledge base; open the folder in Obsidian
├─ bin/               terminal helpers: unity-compile, unity-test, unity-build, unity-shot, unity-inspect
├─ tools/inspect/     the read-only reports behind bin/unity-inspect, and the floor-plan generator
├─ .claude/           Claude Code settings and skills
├─ CLAUDE.md          instructions for Claude Code
├─ AGENTS.md, agent.md  instructions and shared status for every AI agent
└─ README.md          how to open the project
Library/, Temp/, Logs/, Builds/, UserSettings/  are created by Unity and never committed.
```

## `Assets/`

| Folder | What is in it | Files | Owner |
| --- | --- | ---: | --- |
| `Animations/` | the Animator Controller `NEXUS_Locomotion` and the first-person arm mask | 2 | AdaM404 |
| `Characters/NEXUS/` | five character models (FBX), 89 textures, import reports | 98 | AdaM404 |
| `Documentation/` | art sources and licences | 3 | AdaM404 |
| `Environment/Warehouse_NearFuture/` | the warehouse models (visual and collision), 29 textures, metadata | 32 | AdaM404 |
| `Materials/` | 27 environment and 24 character materials, the preview floor, one reflection cubemap | 53 | AdaM404 |
| `Prefabs/` | three prefabs, see below | 3 | shared |
| `Scenes/` | the gameplay scene `Game/Warehouse` and two preview scenes, see below | 3 | `Game/` Samuel; the previews AdaM404 |
| `Scripts/` | `NexusPlayer.cs`, `NexusDoor.cs`: legacy player/door gameplay code | 2 | Samuel |
| `NETRunner/MainMenu/` | isolated menu scene, scripts, UI, art, builder and PlayMode tests; see [[systems/main-menu]] | — | prototype; ownership not yet agreed |
| `Settings/` | URP pipeline assets and post-processing profiles | 5 | shared, change by pull request |
| `InputSystem_Actions.inputactions` | Unity's default input actions; not used by any script | 1 | Samuel |

Every file in `Assets/` has a `.meta` file next to it holding its unique id
(GUID). Scenes and prefabs refer to assets by that id, so a file must always
move or be renamed together with its `.meta`.

## Scenes

| Build order | Scene | What it is | Contents |
| ---: | --- | --- | --- |
| 0 | `Game/Warehouse` | **the gameplay scene**: a built game starts here | the warehouse prefab and the player prefab; see [[systems/level-warehouse]] |
| 1 | `MainTest` | AdaM404's preview of the warehouse with a playable NEXUS | the same warehouse prefab and an older copy of the player |
| 2 | `CharacterPreview` | NEXUS on a small lit stage | player set-up in preview mode, stage, three lights |

F1 switches between `MainTest` and `CharacterPreview` in Play mode. The build
order is set in **File > Build Profiles > Scene List**.

The standalone `Assets/NETRunner/MainMenu/Scenes/NETRunner_MainMenu_Prototype.unity`
scene is opened directly, is absent from Build Settings, and returns to itself
after simulated loading. It does not alter the build order above.

## Prefabs (`Assets/Prefabs/`)

| Prefab | What it contains | Used by |
| --- | --- | --- |
| `NEXUS_Character` | LOD group with the four full-body models; Animator on LOD0 | `NEXUS_Player` prefab, `MainTest`, `CharacterPreview` |
| `NEXUS_Player` | CharacterController, `NexusPlayer` script, `NEXUS_Character`, camera; tag `Player` | the gameplay scene (the preview scenes keep their own copies) |
| `NEXUS_FirstPerson_Arms` | arms-only model for a first-person view | nothing |

The warehouse prefab lives with its art: `Assets/Environment/Warehouse_NearFuture/Warehouse_NearFuture.prefab` (geometry, collision, 119 doors, 106 lights), used by the gameplay scene and `MainTest`.

## Code

| File | Lines | What it does | Used by |
| --- | ---: | --- | --- |
| `Assets/Scripts/NexusPlayer.cs` | 144 | input, movement, camera, interaction, HUD, debug keys, auto-test ([[systems/player]]) | `MainTest`, `CharacterPreview`, `NEXUS_Player` prefab |
| `Assets/Scripts/NexusDoor.cs` | 16 | swings or lifts one door ([[systems/doors-and-interaction]]) | 119 doors in `MainTest` |
| `ArtSource/K7_Industrial_Robot/Unity/*.cs` | | K7 robot helpers; not compiled because they are outside `Assets/` ([[systems/enemy-k7]]) | nothing yet |

The legacy player/door code has no namespaces, assembly definitions or
Core/Gameplay tests. The independent menu has runtime, editor and test
assemblies and two recorded passing PlayMode tests.

## Settings assets (`Assets/Settings/`)

| Asset | Role |
| --- | --- |
| `PC_RPAsset`, `PC_Renderer` | URP settings for the PC quality level (the one in use) |
| `SampleSceneProfile` | post-processing profile of the pipeline: bloom, vignette, tonemapping |
| `DefaultVolumeProfile` | URP's global default profile |
| `UniversalRenderPipelineGlobalSettings` | URP's project-wide settings |

Details in [[systems/rendering]].

## Project settings that matter

| Setting | Value | Where |
| --- | --- | --- |
| Layers | 8 `Player`, 9 `World`, 10 `Enemy` and 11 `Interactable` (10 and 11 reserved for later), plus Unity's built-in ones | Project Settings > Tags and Layers |
| Tags | Unity's defaults; only `MainCamera` is used | same |
| Active input handling | **Both** (old Input Manager and new Input System) | Project Settings > Player > Other Settings |
| Company name | `NET.runner` (decides where save files go) | Project Settings > Player |
| Product name, version | `NET.runner-92`, 0.1.0 | same |
| Default window | 1024 × 768, full-screen window | same |
| Physics gravity | −9.81 (the player script uses its own 14) | Project Settings > Physics |
| Fixed timestep | 0.02 s | Project Settings > Time |

## Packages that matter (`Packages/manifest.json`)

| Package | Why it is there | In use |
| --- | --- | --- |
| Universal RP 17.6.0 | the render pipeline | yes |
| Input System 1.20.0 | modern input with action maps | installed, not used by code |
| Test Framework 1.8.0 | automated tests | two standalone-menu PlayMode tests; gameplay/Core tests remain pending |
| AI Navigation 2.0.14 | navigation meshes for enemies | not yet |
| Timeline, uGUI, Visual Scripting | cutscenes, UI, node-based scripting | not used by any asset |
| Pipeline 0.8 (experimental) | lets the `unity` terminal tool drive the editor | yes, by our tooling |
| AI Assistant, AI Inference | Unity's AI assistant and neural-network runtime | not used by the game |
| Rider, Visual Studio integration | code editor support | yes |
| Version Control (collab-proxy) | Unity's own version control | no; we use git |

## What uses what

```
Game/Warehouse.unity
├─ Warehouse_NearFuture.prefab ─┬─ Warehouse_NearFuture_Geometry.fbx ── environment materials ── textures
│                               ├─ Warehouse_NearFuture_Collision.fbx
│                               └─ NexusDoor.cs (119 doors)
├─ NEXUS_Player.prefab ─┬─ NEXUS_Character.prefab ─┬─ NEXUS_FullBody_LOD0…3.fbx ── NEXUS materials ── textures
│                       │                          └─ NEXUS_Locomotion.controller ── clips in LOD0.fbx, arm mask
│                       └─ NexusPlayer.cs
└─ NEXUS_NeutralReflection.cubemap
MainTest.unity ────────── Warehouse_NearFuture.prefab, NEXUS_Character.prefab, NexusPlayer.cs, cubemap
CharacterPreview.unity ── NEXUS_Character.prefab, NexusPlayer.cs, Preview_Stage.mat, cubemap
```

## I want to … → open this

| I want to change | Open |
| --- | --- |
| menu layout, timing, surveillance feeds or simulated loading | `Assets/NETRunner/MainMenu/README.md` and [[systems/main-menu]] |
| editable warehouse source and closed-door exports | `ArtSource/Warehouse_NearFuture/README.md` and [[systems/level-warehouse]] |
| walking or running speed | `NEXUS_Player` → Nexus Player → Walk Speed / Run Speed, **in each scene**; also the blend tree thresholds ([[systems/character-and-animation]]) |
| mouse sensitivity | the same component → Sensitivity |
| jump height or gravity | `NexusPlayer.cs` lines 81–82 |
| which key does what | `NexusPlayer.cs` `Update()`, lines 63–71 |
| third-person camera distance or height | `NexusPlayer.cs` line 94 |
| first-person eye height or field of view | `NexusPlayer.cs` line 91 |
| what pressing E does | `NexusPlayer.cs` `Interact()`, lines 99–103 |
| how fast doors move | `NexusDoor.cs` line 12 |
| how far one door swings or lifts | that door → Nexus Door → Angle / Lift |
| the text on screen | `NexusPlayer.cs` `OnGUI()`, lines 104–111 |
| where the player starts | `NEXUS_Player` → Transform → Position |
| which scene a built game opens | File > Build Profiles > Scene List |
| the character's animations | select `LOD0`, then Window > Animation > Animator |
| a surface colour | the material in `Assets/Materials/` (art) |
| lights and atmosphere | `Facility_Lighting` in `MainTest`, and Window > Rendering > Lighting (art) |
| post-processing | the camera's Post Processing switch plus a Volume ([[systems/rendering]]) |
| layer or tag names | Edit > Project Settings > Tags and Layers |
| graphics quality | Edit > Project Settings > Quality, and `Assets/Settings/PC_RPAsset` |

## Finding things yourself

In the editor:

| To find | Do |
| --- | --- |
| an object by name | type part of the name into the Hierarchy search box, e.g. `DOOR_Hall` |
| all objects with a component | Hierarchy search `t:NexusDoor`, `t:Light`, `t:Camera` |
| an asset | Project window search, e.g. `t:Prefab`, `t:Scene`, `t:Material Door` |
| where an asset is used in the open scene | right-click it in the Project window > Find References In Scene |
| an object in the Scene view | select it in the Hierarchy and press F |

From the terminal (the editor must be open):

```bash
bin/unity-inspect                  # player, scene, environment, animation reports
bin/unity-inspect doors            # every door: exported open or shut
unity command find_gameobjects --name DOOR_Entry_Main
unity command find_gameobjects --type NexusDoor
unity command find_assets --type Prefab
grep -rn "Interact" Assets/Scripts # plain text search in the code
```
