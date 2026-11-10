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
| Patrol code | `Assets/NETRunner/Enemy/Scripts/K7Patrol.cs` (`NetRunner.Enemy`); rules in `Assets/NETRunner/Core/Scripts/Patrol.cs`, `K7Gait.cs` |
| Placement (editor) | `Assets/NETRunner/World/Editor/HospitalPatrolBuilder.cs` |
| Original source package (Blender, LODs, 2K/4K textures, URP setup helper) | [`ArtSource/K7_Industrial_Robot/`](../../ArtSource/K7_Industrial_Robot/README.md) |

## How it works

Since #41 (2026-11-10) the K-7 follows the coding standard: the rules are
plain C# in `Core` with EditMode tests, and `K7Patrol` only moves the robot
and turns its bones.

| Part | Where | What it decides |
| --- | --- | --- |
| `Patrol` | `Core/Scripts/Patrol.cs` | where the robot is, which way it faces, whether it walks; back and forth along its route |
| `K7Gait` | `Core/Scripts/K7Gait.cs` | the angles of thighs, knees, feet, arms, pelvis and head, and the body bob |
| `K7Patrol` | `Enemy/Scripts/K7Patrol.cs` | reads the route and speed from the scene, calls both every frame, sets the transform and the bones |

The robot turns towards the next point of its route and walks there once
it faces it closely enough. At either end it stops, waits and turns round;
points in between are passed without stopping. The model has no walk clip,
so its bones are turned from their rest poses as it walks. Standing, only
the head moves.

| Value | Number | Where |
| --- | --- | --- |
| walking speed | 1.03, 1.085, 1.14, 1.195 m/s (floors 1–4; default 1.10) | `Speed` on each robot, set by the builder |
| turning | 115° per second | `Patrol.TurnSpeed` |
| walks only when facing the next point within | 25° | `Patrol.WalkingAngle` |
| wait at either end | 1.15 s (the half turn, about 1.35 s, takes longer) | `Patrol.EndPause` |
| a point counts as reached within | 1 cm | `Patrol.ArrivalDistance` |
| one stride (a left and a right step) | every 0.78 m (8.1 radians per metre) | `K7Gait` |
| thigh swing | ±23° around a 4° forward lean | `K7Gait` |
| knee bend, only while its leg swings forward | up to 26° | `K7Gait` |
| foot roll | 10° with the swing, 11° more while the knee bends | `K7Gait` |
| arm swing, against the leg on the same side | ±15° | `K7Gait` |
| pelvis roll | ±2.2° | `K7Gait` |
| body bob, once per step | up to 3.2 cm | `K7Gait` |
| head scan walking | ±8°, one sweep every 8.7 s | `K7Gait` |
| head scan standing | ±23°, one sweep every 3.0 s | `K7Gait` |

- Positions are on the floor (x and z); the robot keeps the height it starts
  at, so a route stays on one floor. It stays upright.
- At an end of the route the way back is exactly behind, so neither way
  round is shorter. `Patrol` then turns without passing south (±180°); on
  the hospital's east–west routes the robots turn through north at both
  ends, as the Northline code did.
- Each robot's head sweep starts at a different point (its first point's
  x), so the four do not scan in step.
- The imported visor faces local −Z, so `K7Patrol` turns the model 180°
  inside its movement root.
- If a robot is switched off and moved, it carries on from where it was
  put (the hospital checks do this to photograph one).
- All four original LODs are set up.

More: [interactive systems, "Residual power and patrols"](../../Northline/Documentation/interactive_systems.md).

## Tests

| Test | Kind | What it checks |
| --- | --- | --- |
| `Core/Tests/EditMode/PatrolTests.cs` | 13 EditMode | speed, turning rate, the 25° rule, the end wait and half turn, back and forth, points in between, `Place` |
| `Core/Tests/EditMode/K7GaitTests.cs` | 9 EditMode | rest when standing, thigh, knee, arm and bob amounts, one stride, head scan |
| `Enemy/Tests/PlayMode/K7PatrolTests.cs` | 5 PlayMode | on a stand-in robot the test builds: walks at its speed, legs swing, stops and turns round at the end, carries on after being moved, model turned round |
| `HospitalChecksTests` | 3 of the 95 checks | the four real robots: routes clear, they move and animate, sensor height |

#41 changed no behaviour. Before the change, a comparison ran the old and
new code side by side on the real model and the four hospital routes for
6,000 frames of uneven length (128 s): positions and turning matched
exactly, and so did every bone for the first 30 s. Later the legs drift
apart by up to 1.8°, because the stride is now worked out from the distance
walked instead of being added up frame by frame; both are rounding.

## Known problems

- No sensing or AI: the robots ignore the player.
- The hospital checks' K-7 picture (`07_K7_Ward_Patrol`) shows the robot
  smeared and see-through. It was like that before #41 too (AdaM404's
  Phase 07 render), because the picture is taken right after the robot is
  moved. In the game the robot looks solid.
- The source package's Unity helper (`ArtSource/K7_Industrial_Robot/Unity`)
  creates URP materials; it is not used and would not suit HDRP.

## Open questions

- What the K-7 does when it sees the player (stealth design; see
  [[design/README]]).
