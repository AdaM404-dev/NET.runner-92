# Doors and interaction

## Purpose

Everything the player uses with **E** in the hospital: doors, the west lift
and its buttons, and the reception security terminal. Since #31
(2026-11-10) this replaces the warehouse's `NexusDoor` and the old E-key
code in `NexusPlayer`, which were deleted.

## Status

Came with the Northline hospital (PR #30) and was moved into the root
project by #31 PR A without code changes; #31 PR B moved it into feature
folders. #10 (one interaction contract;
doors that know if they are open; a test that every door starts shut, from
decision #16) will be rewritten against these types.

## Where it lives

The interaction is in `Assets/NETRunner/Player/Scripts/` (namespace
`NetRunner.Player`), the things you use in `Assets/NETRunner/World/Scripts/`
(`NetRunner.World`):

| Type | Job |
| --- | --- |
| `HospitalInteractable` (in `HospitalInteraction.cs`) | base class: a `Prompt` and `Use(FirstPersonController)` |
| `HospitalTerminal` (in `HospitalInteraction.cs`) | base class for an interactable that takes over the keyboard while open (`ReadInput`, `Close`); added in #31 PR B so `Player` does not need `World` |
| `HospitalInteraction` | on the player: ray from the eyes, prompt, E key ([[systems/player]]) |
| `HospitalDoor` | one opening: swing leaves, sliding pairs or the telescopic research barrier |
| `HospitalLift`, `HospitalLiftButton` | the west lift: cabin, cabin door, landing doors, call and floor buttons |
| `StairAccess` | which floors are reached; unlocks them in the lift |
| `ReceptionSecurityTerminal` | the reception monitor: a three-cell route puzzle that unlocks CCTV |

The doors, lift and stairs sit under `Hospital systems — circulation and
patrols` in `Hospital_Exploration.unity`. Moving leaves and their
`BoxCollider`s are grouped by opening under `Doors`.

## How it works

- **Aim and use:** the ray is 2.65 m (`HospitalInteraction.Reach`), ignores
  layer 2 and also hits triggers. It takes the `HospitalInteractable` on
  the hit collider or a parent.
- **Doors:** each `HospitalDoor` stores closed and open poses per panel
  (`Panel`: position and rotation) and blends between them. `Closed` and
  `Open` report the state, so the doors already "know if they are open".
  All doors start closed on scene load. A closing door reopens if the
  player is in its clearance (`Obstructed`). Lift landing doors are
  `liftOnly` and can't be opened by hand; the east lift's doors are
  `offline`.
- **Lift:** a state machine (`Closed`, `Opening`, `Open`, `Closing`,
  `Moving`). Landings open only when the cabin is level with them; closing
  aborts while the player stands in the doorway. Only reached floors can
  be chosen (`StairAccess`); a fresh load starts with L1 only.
- **First ascent:** closures force west stair → L2, east stair → L3, west
  stair → L4. Stepping out of a required stair into the new concourse
  unlocks that floor in the lift.
- **Terminal:** E from the staff side of the reception desk opens a modal
  puzzle; movement and look pause until it closes. Solving it unlocks the
  CCTV page on LATCH-07 (key 1).

Full description and controls:
[interactive systems](../../Northline/Documentation/interactive_systems.md),
[reception CCTV](../../Northline/Documentation/cctv_unity.md).

## Known problems

- About 100 regular doors; the exact count has not been re-measured in the
  root project (the old `doors` report read `NexusDoor` and was removed).
- The 2.65 m reach, the `~(1 << 2)` mask and prompt texts are hard-coded.
- No NUnit tests yet; the hospital's own validator
  (`Editor/PlayModeValidation.cs`) checks the route, doors and lift in
  Play mode.

## Open questions

- The shape of the single interaction contract (#10).
