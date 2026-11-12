# Doors and interaction

## Purpose

Everything the player uses with **E** in the hospital: doors, the west lift
and its buttons, and the reception security terminal. Since #31
(2026-11-10) this replaces the warehouse's `NexusDoor` and the old E-key
code in `NexusPlayer`, which were deleted.

## Status

Came with the Northline hospital (PR #30) and was moved into the root
project by #31 PR A without code changes; #31 PR B moved it into feature
folders. #10 (2026-11-10) replaced the base classes with interfaces, added
locked doors and the PlayMode tests below, including the #16 check that
every door starts shut. Since #44 (2026-11-12) the doors, the lift, the
stairs and the terminal follow [[architecture/coding-standards]]: their
rules are plain C# in `Core` (`DoorRules`, `LiftCar`, `StairRules`,
`SignalPuzzle`) with EditMode tests, and the components only move the
leaves and the cabin, read the player's position and draw the puzzle.
Behaviour is unchanged: a side-by-side run of the old and the new logic
agreed exactly (see "Brought to the standard" below).

## Where it lives

The interaction is in `Assets/NETRunner/Player/Scripts/` (namespace
`NetRunner.Player`), the things you use in `Assets/NETRunner/World/Scripts/`
(`NetRunner.World`):

| Type | Job |
| --- | --- |
| `IInteractable` (`Player/Scripts/IInteractable.cs`) | the contract: a `Prompt` and `Interact(FirstPersonController)` |
| `ITerminal` (`Player/Scripts/ITerminal.cs`) | an `IInteractable` that takes over the keyboard while open (`ReadInput`, `Close`) |
| `HospitalInteraction` | on the player: ray from the eyes, prompt, E key ([[systems/player]]) |
| `HospitalDoor` | one opening: swing leaves, sliding pairs or the telescopic research barrier; rules `Core/Scripts/DoorRules.cs` |
| `HospitalLift`, `HospitalLiftButton` | the west lift: cabin, cabin door, landing doors, call and floor buttons; rule `Core/Scripts/LiftCar.cs` |
| `StairAccess` | which floors are reached; unlocks them in the lift; rule `Core/Scripts/StairRules.cs` |
| `ReceptionSecurityTerminal` | the reception monitor: a three-cell route puzzle that unlocks CCTV; rule `Core/Scripts/SignalPuzzle.cs` |

The doors, lift and stairs sit under `Hospital systems — circulation and
patrols` in `Hospital_Exploration.unity`. Moving leaves and their
`BoxCollider`s are grouped by opening under `Doors`.

## How it works

- **Aim and use:** the ray is 2.65 m (`HospitalInteraction.Reach`), hits
  the layers in the `aimMask` field (by default all but `Ignore Raycast`)
  and also hits triggers. It takes the first `IInteractable` on
  the hit collider or a parent. `HospitalDoor`, `HospitalLiftButton`,
  `ReceptionSecurityTerminal` (an `ITerminal`) and `LatchRelay` implement
  it; any component can, whatever it derives from. Because an interface
  skips Unity's "destroyed object equals null" check, the interaction asks
  the component whether it still exists before using it.
- **Doors:** each `HospitalDoor` stores closed and open poses per panel
  (`Panel`: position and rotation) and eases between them on an S-curve
  (`Easing.SmoothStep01`). A swing door opens or closes in 1/1.9 ≈ 0.53 s,
  a sliding door or the research barrier in 1/1.55 ≈ 0.65 s. `Closed` and
  `Open` report the state, so the doors "know if they are open".
  `IsPosedShut()` says whether every leaf stands at its closed pose (1 mm,
  0.1°); all doors start shut on scene load, and a test checks it.
- **Locks:** `SetLocked(true)` makes a door prompt "Locked" and ignore E
  (and the diagnostics' path-opening). Locking does not close an open
  door. No door is locked in the scene today; hacking will unlock doors.
- **Doorway:** E does not close a door on a player standing in its
  doorway, and a closing door opens again when the player steps in
  (`Obstructed`). The doorway reaches 0.28 m beyond the opening on either
  side, 0.5 m above and below the door's base, and 1.32 m in front and
  behind for a swing door (as far as a leaf swings) or 0.43 m for a sliding
  one. Lift landing doors are `liftOnly` and can't be opened by hand; the
  east lift's doors are `offline`.
- **Lift:** a state machine (`Closed`, `Opening`, `Open`, `Closing`,
  `Moving`). Landings open only when the cabin is level with them. The
  doors stay open 7 s, counting again while someone stands in a doorway,
  and a closing door opens again for them. The cabin travels at up to
  2.25 m/s, gaining and shedding speed at 2.1 m/s² and never slower than
  0.15 m/s: about 2.8 s from one floor to the next. A cabin button works
  only for a passenger (standing within 1.28 m either side, 1.34 m behind
  and 1.52 m in front of the cabin's centre). Only reached floors can be
  chosen (`StairAccess`); a fresh load starts with L1 only.
- **First ascent:** closures force west stair → L2, east stair → L3, west
  stair → L4. Stepping out of a required stair into the new concourse
  unlocks that floor in the lift: standing on the floor within 0.12 m of
  its height, within 2 m of the stair's exit (1.05 m beyond the core's
  centre, the cores at x = ±32 m) and between z = −1.95 and 2.15 m.
- **Terminal:** E from the staff side of the reception desk opens a modal
  puzzle; movement and look pause until it closes. The local route's three
  cells start at 01 and must match the intercepted signature 03 01 04; left
  and right select a cell and wrap at the ends, up and down count 01 to 04
  and wrap round. A wrong route shows a mismatch until the next change.
  Solving it unlocks the CCTV page on LATCH-07 (key 1).

Full description and controls:
[interactive systems](../../Northline/Documentation/interactive_systems.md),
[reception CCTV](../../Northline/Documentation/cctv_unity.md).

## Known problems

- About 100 regular doors; the exact count has not been re-measured in the
  root project (the old `doors` report read `NexusDoor` and was removed).
- The prompt texts are constants in `Core` (`DoorRules`, `LiftCar`,
  `SignalPuzzle`) until the game has text data for translation.
- `IInteractable` lives in `Player`, not `Core`: `Core` has no engine
  references, and the one who interacts is the player. So `World` still
  depends on `Player` ([[architecture/overview]]).

## Tests

`World/Tests/PlayMode/DoorInteractionTests` (#10), in the real scene:

| Test | Checks |
| --- | --- |
| `EveryDoorStartsShut` | every `HospitalDoor`, lift doors included, is `Closed` and `IsPosedShut()` at load (#16) |
| `AimingAtADoorFromTwoMetresTargetsIt` | eyes 2 m in front of the first reachable ordinary door: the interaction targets it, prompt "E  Open door" |
| `UsingADoorOpensAndClosesIt` | `Interact` opens it and the prompt becomes "E  Close door"; again closes it |
| `ALockedDoorSaysLockedAndStaysShut` | a locked door prompts "Locked" and does not move |
| `AHalfOpenDoorIsNotPosedShut` | `IsPosedShut()` catches a leaf away from its closed pose |

Since #44, on test-built objects or in the scene:

| Test | Checks |
| --- | --- |
| `HospitalDoorTests` (10) | swing and sliding timing, the S-curve, the doorway, locked, lift and offline doors |
| `HospitalLiftTests` (3, in the scene) | refuses floors not reached; opens at its own floor; after the stairs reach L2, travels there and opens |
| `StairAccessTests` (4) | L2 at the west exit, L3 only from the east exit after L2, nothing elsewhere or in the air |
| `ReceptionSecurityTerminalTests` (6) | opens and takes the keyboard, rejects a wrong route, the signature unlocks the CCTV and closes it, values wrap |

The rules have EditMode tests in `Core/Tests/EditMode/` (`DoorRulesTests`,
`LiftCarTests`, `StairRulesTests`, `SignalPuzzleTests`). The 95 hospital
checks (`Diagnostics/Tests/PlayMode/HospitalChecksTests`) also open every
door and ride the lift.

## Brought to the standard (#44)

- **Rules into `Core`:** `DoorRules` (prompt, when E moves a door, its
  speed, the doorway), `LiftCar` (the lift's states, requests, door
  timing, travel and its buttons' prompts), `StairRules` (the first
  ascent), `SignalPuzzle` (the reception puzzle), `PowerFlicker` (the
  residual power, see [[systems/level-hospital]]) and `Scalar` (Unity's
  `MoveTowards` and `Repeat` for `Core`), with 46 EditMode tests.
- **Pinned first:** the 24 new PlayMode tests passed on the old code
  before anything moved.
- **Side by side:** the old logic, run with Unity's own `Mathf`, and the
  new rules gave the same values, bit for bit, over 400,000 door steps,
  383,953 lift frames, 1,000,000 stair positions, 300,000 puzzle key
  presses and 1,000,000 residual-power moments.
- **Changed for other code:** `HospitalDoor.Panel` has a constructor and
  read-only properties (its saved fields keep their names, so the scene's
  doors keep their poses); the scene builder uses it. Everything else
  other code calls kept its name, including `StairAccess.FloorHeight` and
  `StairAccess.CoreForLeg`, which now hand over to `StairRules`.

## Open questions

- None for the contract. Hacking decides who calls `SetLocked(false)`.
