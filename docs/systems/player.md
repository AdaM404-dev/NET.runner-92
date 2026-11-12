# Player

## Purpose

The first-person player in the hospital: movement, mouse look, the E key,
and the LATCH-07 tablet held in the arms-only viewmodel (decisions #13 and
#14). Since #31 (2026-11-10) it replaces `NexusPlayer`, the warehouse
player with its third-person debug view, which was deleted.

## Status

Came with the Northline hospital (PR #30) and was moved into the root
project by #31 PR A without code changes. Since #43 (2026-11-12) it
follows [[architecture/coding-standards]]: the rules are plain C# in
`Core` with EditMode tests, and the components only read the keys, call
the rules and move the capsule, the camera, the arms' animation and the
tablet's screen. Behaviour is unchanged; how that was checked is under
"Brought to the standard" below. It reads input through the Input System
(`Keyboard.current`, `Mouse.current`); Active Input Handling is Input
System only since #31, which also covers #12.

## Where it lives

Paths under `Assets/NETRunner/`:

| What | Where |
| --- | --- |
| Scene object | `PlayerController` in `Hospital_Exploration.unity` |
| Movement and look | `Player/Scripts/FirstPersonController.cs`; rules `Core/Scripts/PlanarMotion.cs`, `VerticalMotion.cs`, `MotionSteps.cs`, `ViewMotion.cs` |
| Aiming and the E key | `Player/Scripts/HospitalInteraction.cs`, `IInteractable.cs`, `ITerminal.cs` ([[systems/doors-and-interaction]]) |
| Tablet | `Player/Scripts/LatchViewmodel.cs`, `LatchScreen.cs`, `LatchRelay.cs`; rules `Core/Scripts/LatchSession.cs`, `LatchPhase.cs`, `LatchStatus.cs`, `LatchAccent.cs`; art in `Assets/NETRunner92/Player/Latch07/` |
| CCTV on the tablet | `Player/Scripts/SecurityCctv.cs`; rule `Core/Scripts/CctvFeeds.cs` |
| Import check of the arms (editor) | `Player/Editor/LatchViewmodelBuilder.cs` |
| Tests | the rules' EditMode tests in `Core/Tests/EditMode/`; 27 PlayMode tests in `Player/Tests/PlayMode/` |

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

- **Walking** (`PlanarMotion`): the input is shortened to length 1 when it
  is longer, so diagonals are not faster, and the velocity is accelerated
  at 22 m/s² towards the walk or sprint speed. From standing, walking
  speed is reached in the tenth frame at 60 FPS (3.5 / 22 = 0.16 s).
- **Steps** (`MotionSteps`): each frame's motion is split into steps of at
  most 1/90 s for stable stair contact. At most 0.1 s of a frame is moved,
  so after a long hitch the player is not thrown through a wall to catch up.
- **Gravity** (`VerticalMotion`): 18 m/s² in the air, falls capped at
  34 m/s. While grounded the capsule is pushed down at 4.2 m/s ("ground
  adhesion") so it stays on stairs and ramps. A step that bumps the
  ceiling stops a rise. There is no jump.
- **Capsule:** 1.80 m high, 0.30 m radius, 0.35 m step offset, 50° slope
  limit. Eyes at 1.65 m.
- **Looking** (`ViewMotion`): the body turns by the mouse movement times
  the sensitivity (0.13° per unit), and the eyes tilt within 85° up and
  down. Objects on the layer `Near-detail geometry` are culled beyond
  `detailCullDistance` (34 m); the layer is found by its name.
- **Head bob** (`ViewMotion`, off by default): the eyes bob by the walking
  speed times 4 mm, at most 21 mm, once per 2π/3 ≈ 2.1 m walked, and not
  in the air.
- **Interaction:** each frame `HospitalInteraction` casts a ray from the
  eyes, 2.65 m long (`HospitalInteraction.Reach`), against the layers in
  its `aimMask` field (by default everything but `Ignore Raycast`). It
  finds an `IInteractable` (doors, lift buttons, terminal, relay) and shows
  its prompt. E calls `Interact(player)`. While the reception terminal is
  open, movement and look pause. See [[systems/doors-and-interaction]].
- **Lift:** the lift moves the controller with the cabin
  (`FollowPlatform`) and keeps the view direction.

### LATCH-07

Q raises the tablet, and it owns the keys while it is up. `LatchSession`
decides what each key does in each phase; `LatchViewmodel` plays the arms'
clip for each phase and reports how far it has played. A tap commits at
42% of the touch clip, when the finger reaches the screen: the relay links
if the eyes are within 2.65 m and nothing solid is in between (its
`sightMask`). A cancel before that leaves the relay offline. Losing window
focus, or freeing the cursor, stows the tablet at once.

| Phase | Clip | What leads on |
| --- | --- | --- |
| Stowed | — | Q → Equip (acquiring the relay aimed at, if any); 1 once unlocked → Equip with the CCTV page |
| Equip | `Hack_Equip`, 0.5 s | clip ends → Ready; Q or R → Cancel |
| Ready | `Hack_Idle`, 2 s, loops | E → Tap; Q → Unequip; R → Cancel; 1 → CCTV page |
| Tap | `Hack_Interact`, 0.8 s | at 42% the link is tried; clip ends → Success or Failed; Q or R → Cancel |
| Success | `Hack_Success`, 0.8 s | clip ends → Ready |
| Failed | `Hack_Failed`, 0.6 s | clip ends → Ready |
| Unequip | `Hack_Unequip`, 0.4 s | clip ends → Stowed |
| Cancel | `Hack_Cancel`, 0.25 s | clip ends → Stowed |

The tablet's screen (`LatchStatus`, drawn by `LatchScreen` into a
640 × 324 texture) shows in each phase:

| Phase | Heading | Detail | Keys | Colour | Signal bars of 14 |
| --- | --- | --- | --- | --- | ---: |
| Equip | BOOTING | ESTABLISHING LOCAL SESSION | R  CANCEL | cyan | 4 |
| Ready, relay acquired | SIGNAL STANDBY, or RELAY ONLINE once linked | TARGET  /  NORTHLINE SERVICE RELAY | E  LINK     Q  STOW     R  CANCEL | cyan | 12 |
| Ready, no relay | SIGNAL STANDBY | TARGET  /  NO RELAY IN VIEW | as above | amber | 2 |
| Tap | HANDSHAKE | SENDING SERVICE REQUEST | R  CANCEL | cyan | 8 |
| Success | LINK ESTABLISHED | SERVICE RELAY RESPONDING | Q  STOW     R  CANCEL | cyan | 14 |
| Failed | LINK FAILED | EQUIP WHILE AIMING AT A RELAY | Q  STOW     R  CANCEL | fault red | 1 |
| Unequip, Cancel | DISCONNECTING | CLOSING LOCAL SESSION | — | muted | 2 |

Once the CCTV is unlocked, Ready offers `E  LINK     Q  STOW     1  CCTV`
instead. A bar count that ends in a half rounds to the even neighbour, as
Unity's `Mathf.RoundToInt` does (a quarter of 14 bars lights 4).

### CCTV

Pressing 1 before the reception breach shows `CCTV locked   /   Use the
reception terminal` over the reticle for 3 s. After the breach, 1 raises
the tablet with the CCTV page, or shows the page at once when the tablet
is Ready, and 1 again closes it and stows the tablet. The arrow keys step
through the six feeds and wrap round at both ends (`CctvFeeds`). The
watched feed renders into a 576 × 288 texture at most 15 times a second;
closing switches the feed camera off. More:
[LATCH-07 integration](../../Northline/Documentation/latch07_unity.md),
[CCTV](../../Northline/Documentation/cctv_unity.md),
[player controller](../../Northline/Documentation/player_controller.md).

## Brought to the standard (#43)

- **Rules into `Core`:** walking (`PlanarMotion`), steps (`MotionSteps`),
  gravity (`VerticalMotion`, which held the warehouse player's unused jump
  maths until then), look and head bob (`ViewMotion`), the tablet's phases
  (`LatchSession`, `LatchPhase`), its screen (`LatchStatus`, `LatchAccent`)
  and the feed order and frame rate (`CctvFeeds`), with 57 EditMode tests.
  `PlanarMotion` repeats Unity's `Vector3.ClampMagnitude` and
  `Vector3.MoveTowards` step by step.
- **Pinned first:** the 27 PlayMode tests in `Player/Tests/PlayMode/` were
  written against the old code and passed on it before anything moved.
- **Side by side:** the old and the new controller replayed the same 6,000
  frames (walking, sprinting, diagonals, a staircase, a ramp, walls, frames
  up to 0.25 s long, warps, head bob on). Their positions stayed within
  4 µm of each other, and the turn, the view angle and "grounded" were
  identical in every frame. The old tablet logic and the new
  `LatchSession` with the new component's handling took 1,000,000 random
  key presses and clip ends and showed the same state after every one.
- **Pictures:** the hospital checks' pictures match `main`'s apart from
  rain and the walking K-7, by less than two runs of `main` differ from
  each other; the tablet screen's texture is byte for byte the same.
- **Changed for other code:** `LatchViewmodel.Phase` became `Core`'s
  `LatchPhase`; `LatchScreen.Present` takes a `LatchStatus`;
  `SecurityCctv.Feed` has a constructor and read-only properties (its
  saved fields keep their names `label` and `view`); `ITerminal` has a
  file of its own. Everything else other code calls kept its name.
- **New fields:** `HospitalInteraction.aimMask` and `LatchRelay.sightMask`
  replace the `~(1 << 2)` masks. Their default is the same mask, so the
  scene was not edited.

## Known problems

- No jump and no crouch.
- Prompts are drawn with `OnGUI`, not UI Toolkit.
- Numbers above come from the code's defaults and Northline's notes. They
  have not been re-measured in the scene with `bin/unity-inspect`.
- `LatchViewmodelBuilder` was brought to the standard without running it:
  it rewrites assets in AdaM404's `Latch07` folder. Only its form changed.

## Open questions

- Phase 11's player (stamina, key rebinding, the Signal Breach page on the
  tablet) is applied onto this code in #55 step 4.
