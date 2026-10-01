# Project map

Where everything is, what uses it, and who owns it. Start here when you are
looking for something. As of commit `df3a31d` (2026-09-30).

Ownership follows the proposal in [[assets/handoff]], which AdaM404 has not
confirmed yet.

## The repository

```
NET.runner-92/
├─ Assets/            everything Unity imports: the game itself
├─ Packages/          which Unity packages the project uses (manifest.json)
├─ ProjectSettings/   project-wide settings: layers, quality, input, build list, …
├─ ArtSource/         source art that Unity does not import (the K7 robot package)
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
| `Prefabs/` | seven prefabs, see below | 7 | shared |
| `Scenes/` | three scenes, see below | 3 | AdaM404 (previews); ours once a gameplay scene exists |
| `Scripts/` | `NexusPlayer.cs`, `NexusDoor.cs`: **all the game's code** | 2 | Samuel |
| `Settings/` | URP pipeline assets and post-processing profiles | 7 | shared, change by pull request |
| `InputSystem_Actions.inputactions` | Unity's default input actions; not used by any script | 1 | Samuel |
| `TutorialInfo/`, `Readme.asset` | leftovers from Unity's project template | 8 | nobody: to delete |

Every file in `Assets/` has a `.meta` file next to it holding its unique id
(GUID). Scenes and prefabs refer to assets by that id, so a file must always
move or be renamed together with its `.meta`.

## Scenes

| Build order | Scene | What it is | Contents |
| ---: | --- | --- | --- |
| 0 | `SampleScene` | Unity's empty template | camera, sun, post-processing volume. **A built game starts here and shows only sky.** |
| 1 | `MainTest` | the warehouse with a playable NEXUS | 3,080 objects; see [[systems/level-warehouse]] |
| 2 | `CharacterPreview` | NEXUS on a small lit stage | player set-up in preview mode, stage, three lights |

F1 switches between `MainTest` and `CharacterPreview` in Play mode. The build
order is set in **File > Build Profiles > Scene List**.

## Prefabs (`Assets/Prefabs/`)

| Prefab | What it contains | Used by |
| --- | --- | --- |
| `NEXUS_Character` | LOD group with the four full-body models; Animator on LOD0 | `NEXUS_Player` prefab, `MainTest`, `CharacterPreview` |
| `NEXUS_Player` | CharacterController, `NexusPlayer` script, `NEXUS_Character`, camera | **nothing** (both scenes have their own copy) |
| `NEXUS_FullBody_LOD0` … `LOD3` | one model each, as a variant of its FBX | nothing |
| `NEXUS_FirstPerson_Arms` | arms-only model for a first-person view | nothing |

## Code

| File | Lines | What it does | Used by |
| --- | ---: | --- | --- |
| `Assets/Scripts/NexusPlayer.cs` | 135 | input, movement, camera, interaction, HUD, debug keys, auto-test ([[systems/player]]) | `MainTest`, `CharacterPreview`, `NEXUS_Player` prefab |
| `Assets/Scripts/NexusDoor.cs` | 16 | swings or lifts one door ([[systems/doors-and-interaction]]) | 119 doors in `MainTest` |
| `Assets/TutorialInfo/Readme.cs`, `Editor/ReadmeEditor.cs` | | Unity template leftovers | `Readme.asset` |
| `ArtSource/K7_Industrial_Robot/Unity/*.cs` | | K7 robot helpers; not compiled because they are outside `Assets/` ([[systems/enemy-k7]]) | nothing yet |

The code has no namespaces, no assembly definitions and no tests.

## Settings assets (`Assets/Settings/`)

| Asset | Role |
| --- | --- |
| `PC_RPAsset`, `PC_Renderer` | URP settings for the PC quality level (the one in use) |
| `Mobile_RPAsset`, `Mobile_Renderer` | URP settings for the Mobile quality level (unused) |
| `SampleSceneProfile` | post-processing profile of the pipeline: bloom, vignette, tonemapping |
| `DefaultVolumeProfile` | URP's global default profile |
| `UniversalRenderPipelineGlobalSettings` | URP's project-wide settings |

Details in [[systems/rendering]].

## Project settings that matter

| Setting | Value | Where |
| --- | --- | --- |
| Layers | only Unity's built-in names; **8 and 9 are used but unnamed** | Project Settings > Tags and Layers |
| Tags | Unity's defaults; only `MainCamera` is used | same |
| Active input handling | **Both** (old Input Manager and new Input System) | Project Settings > Player > Other Settings |
| Company name | `DefaultCompany` (decides where save files go) | Project Settings > Player |
| Product name, version | `NET.runner-92`, 0.1.0 | same |
| Default window | 1024 × 768, full-screen window | same |
| Physics gravity | −9.81 (the player script uses its own 14) | Project Settings > Physics |
| Fixed timestep | 0.02 s | Project Settings > Time |

## Packages that matter (`Packages/manifest.json`)

| Package | Why it is there | In use |
| --- | --- | --- |
| Universal RP 17.6.0 | the render pipeline | yes |
| Input System 1.20.0 | modern input with action maps | installed, not used by code |
| Test Framework 1.8.0 | automated tests | no tests yet |
| AI Navigation 2.0.14 | navigation meshes for enemies | not yet |
| Timeline, uGUI, Visual Scripting | cutscenes, UI, node-based scripting | not used by any asset |
| Pipeline 0.8 (experimental) | lets the `unity` terminal tool drive the editor | yes, by our tooling |
| AI Assistant, AI Inference | Unity's AI assistant and neural-network runtime | not used by the game |
| Rider, Visual Studio integration | code editor support | yes |
| Version Control (collab-proxy) | Unity's own version control | no; we use git |

## What uses what

```
MainTest.unity
├─ NEXUS_Character.prefab ─┬─ NEXUS_FullBody_LOD0…3.fbx ── NEXUS materials ── textures
│                          └─ NEXUS_Locomotion.controller ── clips in LOD0.fbx, arm mask
├─ Warehouse_NearFuture_Geometry.fbx ── environment materials ── textures
├─ Warehouse_NearFuture_Collision.fbx
├─ NexusPlayer.cs, NexusDoor.cs
└─ NEXUS_NeutralReflection.cubemap
CharacterPreview.unity ── NEXUS_Character.prefab, NexusPlayer.cs, Preview_Stage.mat, cubemap
NEXUS_Player.prefab ───── NEXUS_Character.prefab, NexusPlayer.cs          (used by nothing)
```

## I want to … → open this

| I want to change | Open |
| --- | --- |
| walking or running speed | `NEXUS_Player` → Nexus Player → Walk Speed / Run Speed, **in each scene**; also the blend tree thresholds ([[systems/character-and-animation]]) |
| mouse sensitivity | the same component → Sensitivity |
| jump height or gravity | `NexusPlayer.cs` lines 72–73 |
| which key does what | `NexusPlayer.cs` `Update()`, lines 54–62 |
| third-person camera distance or height | `NexusPlayer.cs` line 85 |
| first-person eye height or field of view | `NexusPlayer.cs` line 82 |
| what pressing E does | `NexusPlayer.cs` `Interact()`, lines 90–94 |
| how fast doors move | `NexusDoor.cs` line 12 |
| how far one door swings or lifts | that door → Nexus Door → Angle / Lift |
| the text on screen | `NexusPlayer.cs` `OnGUI()`, lines 95–102 |
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
