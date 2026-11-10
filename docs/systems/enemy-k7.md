# Enemy: K-7 industrial robot

## Purpose

The game's first enemy: a K-7 industrial robot.

## Status

Since #31 (2026-11-10) the K-7 is in the project as part of the hospital:
four units patrol the main concourse, one per floor. They walk, turn and
scan with the head. They have **no perception, pursuit, combat or
interaction** yet.

## Where it lives

| What | Where |
| --- | --- |
| Model (byte-for-byte copy of the source FBX) | `Assets/NETRunner92/Environment/K7/K7_Robot.fbx` |
| Materials (HDRP Lit, seven) and mask map | `Assets/NETRunner92/Environment/K7/MAT_K7_*.mat`, `K7_HDRP_Mask.asset`, `Textures/` |
| Patrol prefab | `Assets/NETRunner92/Environment/K7/K7_Patrol.prefab` |
| Patrol code | `Assets/NETRunner92/Core/K7Patrol.cs` |
| Placement (editor) | `Assets/NETRunner92/Editor/HospitalPatrolBuilder.cs` |
| Original source package (Blender, LODs, 2K/4K textures, URP setup helper) | [`ArtSource/K7_Industrial_Robot/`](../../ArtSource/K7_Industrial_Robot/README.md) |

## How it works

- `K7Patrol` walks between authored points (`route`) at 1.10 m/s, pauses
  and turns at the ends, and scans with its head.
- The model has no walk clip, so the code animates the bones directly:
  thighs, knees, feet, arms, pelvis and head swing around their rest
  poses.
- The imported visor faces local −Z, so the visual model is turned 180°
  inside its movement root.
- All four original LODs are set up.

More: [interactive systems, "Residual power and patrols"](../../Northline/Documentation/interactive_systems.md).

## Known problems

- No sensing or AI: the robots ignore the player.
- The source package's Unity helper (`ArtSource/K7_Industrial_Robot/Unity`)
  creates URP materials; it is not used and would not suit HDRP.

## Open questions

- What the K-7 does when it sees the player (stealth design; see
  [[design/README]]).
