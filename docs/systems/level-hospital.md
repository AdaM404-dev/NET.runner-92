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
- **Atmosphere:** storm audio, fog zones, flickering practicals ("residual
  power"); see [atmosphere system](../../Northline/Documentation/atmosphere_system.md).
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

## Open questions

- Which builders stay once the scene is final (#31 PR B/C).
