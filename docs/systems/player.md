# Player

## Purpose

The first-person player in the hospital: movement, mouse look, the E key,
and the LATCH-07 tablet held in the arms-only viewmodel (decisions #13 and
#14). Since #31 (2026-11-10) it replaces `NexusPlayer`, the warehouse
player with its third-person debug view, which was deleted.

## Status

Came with the Northline hospital (PR #30) and was moved into the root
project by #31 PR A without code changes. It reads input through the
Input System (`Keyboard.current`, `Mouse.current`); Active Input Handling
is Input System only since #31, which also covers #12. It does not follow
[[architecture/coding-standards]] yet (one namespace, `Assembly-CSharp`,
no NUnit tests); #31 PR B and C fix that.

## Where it lives

| What | Where |
| --- | --- |
| Scene object | `PlayerController` in `Hospital_Exploration.unity` |
| Movement and look | `Assets/NETRunner92/Player/Scripts/FirstPersonController.cs` |
| Aiming and the E key | `Assets/NETRunner92/Core/HospitalInteraction.cs` |
| Tablet | `Player/Scripts/LatchViewmodel.cs`, `LatchScreen.cs`, `LatchRelay.cs`, assets in `Player/Latch07/` |
| CCTV on the tablet | `Player/Scripts/SecurityCctv.cs` |

`PlayerController` has a `CharacterController`, `FirstPersonController`,
`HospitalInteraction`, one camera and an `AudioListener`. It has no visible
body.

## Keys

| Input | Action |
| --- | --- |
| W, A, S, D | Walk (3.5 m/s) |
| Left Shift | Sprint (5.5 m/s) |
| Mouse | Look, pitch limited to 85° |
| E | Use what the reticle points at (door, lift button, terminal); tap Link when the tablet is raised |
| Q | Raise or stow LATCH-07 |
| R | Cancel the tablet action |
| 1 | Open or close the CCTV page after the reception breach |
| Arrow keys | Reception puzzle, or switch CCTV feeds |
| Esc / left click | Release / capture the cursor |
| F2 / F3 | Next graphics quality level / FPS counter ([[systems/quality-settings]]) |

## How it works

- **Movement:** the input is normalised so diagonals are not faster, then
  accelerated at 22 m/s² towards the walk or sprint speed. Gravity is
  18 m/s² (falls are capped at 34 m/s); while grounded the capsule is
  pushed down at 4.2 m/s ("ground adhesion") so it stays on stairs and
  ramps. Each frame's motion is split into steps of at most 1/90 s for
  stable stair contact.
- **Capsule:** 1.80 m high, 0.30 m radius, 0.35 m step offset, 50° slope
  limit. Eyes at 1.65 m. Head bob exists but is off by default.
- **Camera:** objects on layer 8 (`Near-detail geometry`) are culled beyond
  `detailCullDistance` (34 m by default).
- **Interaction:** each frame `HospitalInteraction` casts a ray from the
  eyes, 2.65 m long, ignoring layer 2 (`Ignore Raycast`). It finds a
  `HospitalInteractable` (doors, lift buttons, terminal) and shows its
  prompt. E calls `Use(player)`. While the reception terminal is open,
  movement and look pause. See [[systems/doors-and-interaction]].
- **Lift:** the lift moves the controller with the cabin
  (`FollowPlatform`) and keeps the view direction.
- **LATCH-07:** raised with Q, it owns the E key while up. Details:
  [LATCH-07 integration](../../Northline/Documentation/latch07_unity.md),
  [CCTV](../../Northline/Documentation/cctv_unity.md).

More detail from the original project:
[player controller](../../Northline/Documentation/player_controller.md).

## Known problems

- No jump and no crouch.
- Prompts are drawn with `OnGUI`, not UI Toolkit.
- Numbers above come from the code's defaults and Northline's notes. They
  have not been re-measured in the root project with `bin/unity-inspect`
  (the old `player` report read `NexusPlayer` and was removed).

## Open questions

- Whether `Core/VerticalMotion` (the warehouse player's gravity and jump
  maths, with tests) is reused when the controller is brought in line with
  the coding standard (#31 PR C).
