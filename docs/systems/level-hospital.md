# Level: Northline hospital

## Purpose

The game's main map since #31 (2026-11-10): Northline Medical, a four-storey
hospital and its quarantine grounds, explored in first person. It replaced
the warehouse, which was deleted.

## Status

Built by AdaM404 with Codex as the separate "Northline Phase 07" project
(PR #30), then moved into the root project by #31 PR A. Its 95-check
PlayMode validator passed on Windows (Direct3D 11) before the move; for the
Linux run after the move, see the work log in [`agent.md`](../../agent.md).
#31 PR B moved its code into feature folders under `Assets/NETRunner/`
([[architecture/project-map]]); the art and scenes stay in
`Assets/NETRunner92/`. The 95 checks now also run as an NUnit PlayMode test
(`bin/unity-test play`).

## Where it lives

| What | Where |
| --- | --- |
| Gameplay scene, first in Build Settings | `Assets/NETRunner92/Scenes/Hospital_Exploration.unity` |
| LATCH-07 check scene (not in the build) | `Assets/NETRunner92/Scenes/LATCH07_ViewmodelCheck.unity` |
| Hospital and exterior geometry | `Assets/NETRunner92/Environment/Hospital`, `Exterior` (meshes in `MeshData.bytes`, Git LFS) |
| Materials (HDRP Lit) | `Assets/NETRunner92/Environment/Materials` |
| Fog, lighting, audio, VFX | `Assets/NETRunner92/Atmosphere/` |
| Scene builders (editor menu **NET.runner-92**) | `Assets/NETRunner/World/Editor/` |
| Occlusion culling data (baked by `World/Editor/HospitalOcclusion.cs`) | `Assets/NETRunner92/Scenes/Hospital_Exploration/OcclusionCullingData.asset` |
| Blender sources, renders, cinematic, design notes | [`Northline/`](../../Northline/README.md) |

## How it works

- **Layout:** a main block of about 72 × 30 m with floors at 0.00, 4.20,
  8.40 and 12.60 m. A central east–west concourse, a lift lobby with two
  shafts, and stairs at both ends. Details:
  [hospital overview](../../Northline/Hospital/overview.md).
- **First ascent:** physical closures force west stair → L2, east stair →
  L3, west stair → L4. Reaching a floor by its stair unlocks it in the west
  lift; the east lift is out of service. See [[systems/doors-and-interaction]].
- **Scene structure:** `Hospital systems — circulation and patrols` holds the
  doors, stairs, lift, patrols and residual power;
  `Northline Medical — imported environment` holds the model. The player is
  `PlayerController` ([[systems/player]]).
- **Stairs:** the twelve visible flights have invisible ramp colliders
  (2.10 m rise over 3.19 m).
- **Atmosphere:** rain, lightning, ambience sound, fog zones and pulsing
  lights: [[systems/atmosphere]].
- **Residual power:** four concourse lamps and a few screens flicker on
  `World`'s `ResidualPower`, with the timing from `Core`'s `PowerFlicker`.
  Each repeats an outage every 11.5 s plus 9% of its own phase, so no two
  run in step. A lamp drops to 4% from 0 to 0.13 s and from 0.29 to
  0.43 s, browns out to 27% from 0.64 to 1.20 s and burns fully otherwise;
  its glowing material follows it. A screen sits at 18% for the first
  0.28 s and then at 78% ± 20%, waving at 3.1 radians a second.
  `ResidualPowerTests` follows a test-built lamp through its cycle.
- **Room reachability:** `World`'s `RoomReachability` (editor only) floods
  each floor from its centre in 0.32 m steps wherever a player's capsule
  fits and reports which rooms of `RoomSchedule.json` it reached; the
  hospital checks use it.
- **Occlusion culling** (since 2026-11-11, #36 round 1): the cameras skip
  what walls and floors hide. Everything under
  `Northline Medical — imported environment` is static for occlusion:
  6,837 solid renderers hide what is behind them, and 203 see-through ones
  (glass) can be hidden but hide nothing (`OcclusionRules` in `Core`).
  Doors, the lift, the patrols, the player and the other systems move, so
  they stay dynamic; a closed door therefore hides nothing. The data is
  baked with Umbra (smallest occluder 2 m, smallest hole 0.25 m; about a
  minute): menu **NET.runner-92 → Bake Occlusion Culling**, or headless
  `source bin/_unity.sh && run_unity occlusion -quit -executeMethod
  NetRunner.World.Editor.HospitalSceneBuilder.BakeOcclusion`.
  **Build Hospital Exploration Scene** bakes it last.
- **Layers:** 8 `Near-detail geometry` (culled by the player camera past
  `detailCullDistance`, 34 m by default), 9 `First-person viewmodel` (LATCH-07; hidden from CCTV cameras).
  The names were set by #31; the warehouse's `Player`/`World` names are
  gone. 10 `Enemy` and 11 `Interactable` stay reserved.

## Known problems

- The scene is generated and maintained by editor builders
  (`HospitalSceneBuilder`, `HospitalSystemsBuilder`, …). Re-running them
  regenerates parts of the scene; treat them as tools, not gameplay code.
- Builders and validation write to `Northline/Renders/` and
  `Northline/Builds/`.
- The occlusion data describes the static geometry where it was when it
  was baked. After moving, adding or removing any of it, bake again, or
  objects can vanish behind walls that are no longer there.

## Open questions

- Which builders stay once the scene is final (#31 PR B/C).
