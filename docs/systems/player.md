# Player

**Purpose.** The object you control: a capsule that moves through the level,
a body that animates, and the camera you see through. All of it is driven by
one script, `NexusPlayer`.

**Status.** Works as an asset-preview controller. It is not yet structured
for gameplay; see [[architecture/before-new-scripts]].

Verified 2026-09-30 on commit `df3a31d` (Unity 6000.6.2f1, Linux) with
`bin/unity-inspect player` and scripted Play-mode checks. The checks called
the same methods the keys call; nobody pressed the keys by hand. On
2026-10-04 (Step 4, [#9](https://github.com/AdaM404-dev/NET.runner-92/issues/9))
the script moved to its feature folder, got a namespace and one statement per
line, and handed gravity and jumping to `VerticalMotion` in `Core`, without a
change in behaviour: the PlayMode tests below measure the same walk, run and
jump values.

New to Unity words like *component* or *prefab*? Read
[[guide/01-unity-in-this-project]] first.

## Where it lives

| What | Where |
| --- | --- |
| Prefab | `Assets/Prefabs/NEXUS_Player.prefab`, tagged `Player`: **the** player |
| Gameplay scene | `Assets/Scenes/Game/Warehouse.unity` → `NEXUS_Player`, an instance of the prefab |
| Script | [`Assets/NETRunner/Player/Scripts/NexusPlayer.cs`](../../Assets/NETRunner/Player/Scripts/NexusPlayer.cs), one class, `NetRunner.Player.NexusPlayer` |
| Gravity and jump maths | [`Assets/NETRunner/Core/Scripts/VerticalMotion.cs`](../../Assets/NETRunner/Core/Scripts/VerticalMotion.cs), plain C#, called by `Simulate()` |
| Tests | `Assets/NETRunner/Player/Tests/PlayMode/` and `Assets/NETRunner/Core/Tests/EditMode/`, see "Tests" below |
| Preview copies | `MainTest` → `NEXUS_Player` and `CharacterPreview` → `Character_Preview` (with `previewMode` on): older copies of the same setup in AdaM404's preview scenes, **not** linked to the prefab |

Change the prefab, not the preview copies: only the gameplay scene follows
it. The copies hold the same values today; they are replaced when the player
is split up in Step 6 ([#11](https://github.com/AdaM404-dev/NET.runner-92/issues/11)).

## The object tree

```
NEXUS_Player              layer 8 Player, position (0, 0.03, 12), tag Untagged
│  Transform
│  CharacterController      the capsule that collides with the world
│  NexusPlayer              the script
├─ NEXUS_Character          instance of NEXUS_Character.prefab
│  │  LODGroup              picks which of the four models is drawn
│  ├─ LOD0                  full-detail model; the only one with an Animator
│  ├─ LOD1
│  ├─ LOD2
│  └─ LOD3
└─ Player_Camera            Camera + AudioListener, tag MainCamera, layer 0
```

The model and its animation are described in
[[systems/character-and-animation]], the camera in [[systems/camera]].

## Components and values

**CharacterController** (the collision capsule; Unity's built-in component)

| Setting | Value | Meaning |
| --- | --- | --- |
| Height | 1.78 m | capsule height; the model is 1.79 m tall |
| Radius | 0.26 m | capsule half-width; openings narrower than 0.52 m block the player |
| Center | (0, 0.89, 0) | capsule middle, so its bottom sits at the feet |
| Slope Limit | 45° | steeper ground cannot be walked up |
| Step Offset | 0.32 m | ledges up to this height are climbed without jumping (stairs) |
| Skin Width | 0.035 m | small buffer that keeps the capsule from getting stuck in walls |

**NexusPlayer** (fields visible in the Inspector)

| Field | Value | Meaning |
| --- | --- | --- |
| View Camera | `Player_Camera` | the camera the script moves every frame |
| Animator | `NEXUS_Character/LOD0` | receives the `Speed` number and the arm-layer weight |
| Lod Group | `NEXUS_Character` | forced to the full-detail model in first person |
| Visual | `NEXUS_Character` | searched for head parts to hide in first person |
| First Person | off | which view the game starts in |
| Preview Mode | off | on = character viewer: no walking, no jumping |
| Walk Speed | 1.65 m/s | measured: 3.30 m in 2 s |
| Run Speed | 3.25 m/s | measured: 6.50 m in 2 s |
| Sensitivity | 2 | degrees of turn per unit of mouse movement |
| Camera Collision Layers | World | what the third-person camera treats as walls |
| Interaction Layers | everything except Player | what the E ray can hit; leaving out Player makes it ignore your own body |

**Numbers written in the code**, not reachable from the Inspector:

| Number | Value | Where |
| --- | --- | --- |
| Gravity | 14 m/s² (Unity's own physics gravity of 9.81 is not used) | `VerticalMotion.Gravity` |
| Jump speed | 4.4 m/s upward; measured jump 0.66 m high, 0.62 s in the air | `VerticalMotion.JumpSpeed` |
| Ground stick | −2 m/s while standing, keeps the capsule pressed to the floor | `VerticalMotion.GroundStick` |
| Look up/down limit | ±78°, starting at 8° down | `NexusPlayer.Update()`; the start value is the `pitch` field |
| Third-person turn rate | 12 (higher = the body turns faster toward movement) | `NexusPlayer.Simulate()` |
| Animation smoothing | 0.15 s | `NexusPlayer.Simulate()` |
| Fall limit | below y = −12 the player is put back at the start | `NexusPlayer.Simulate()` |
| Interaction distance | 3.5 m from the camera | `NexusPlayer.Interact()` |

The docs name methods and constants instead of line numbers, because line
numbers change with every edit.

## Keys

All keys are read in `NexusPlayer.Update()`.

| Key | What it does |
| --- | --- |
| W A S D, arrow keys, gamepad stick | move (legacy axes `Horizontal`, `Vertical`) |
| Mouse | look |
| Left Shift | run |
| Space | jump |
| E | interact; today that means doors only, see [[systems/doors-and-interaction]] |
| Tab | switch first / third person |
| F1 | load the other scene (`MainTest` ↔ `CharacterPreview`) |
| R | back to the start position |
| Esc | release or recapture the mouse cursor; no movement while released |
| Left click | recapture the cursor |

All of these are read with Unity's old `Input` class. The project also
contains an Input System actions asset (`Assets/InputSystem_Actions.inputactions`
with Move, Look, Jump, Sprint, Interact, Crouch, Attack) that no script uses.

## What happens in one frame

Unity calls these methods by itself; nothing in the project calls them.

```
once, when the scene starts
  Awake()        find the CharacterController, remember the start position,
                 collect the model's renderers, apply the starting view,
                 lock the mouse cursor

every frame, in this order
  Update()       read keys and mouse
    ├─ Esc, click, Tab, F1, R, E
    ├─ mouse → yaw and pitch (the two look angles)
    └─ Simulate(input, run, jump, dt)
         1. input + yaw → a direction in the world
         2. turn the body: first person faces where you look,
            third person turns toward the movement direction
         3. VerticalMotion.Step(...) works out the new verticalSpeed:
            ground stick, jump, gravity (plain maths in Core)
         4. CharacterController.Move(...)  ← the only place the player moves
         5. tell the Animator the speed (from the input, not from real movement)
         6. fell too far → Respawn()

  (Unity's Animator then poses the skeleton)

  LateUpdate()   put the camera in place, after the body has moved
  OnGUI()        draw the text in the top-left corner and the notices
```

The movement maths inside `Simulate` is explained line by line in
[[systems/player-movement]].

## Measured behaviour

| Behaviour | Result |
| --- | --- |
| Speed | exactly the Inspector values; diagonal movement is not faster |
| Acceleration | none: full speed at once, stops at once |
| In the air | full steering at walk or run speed; no double jump |
| Third person, pressing S | the body turns around and walks toward the camera; there is no walking backwards |
| First person | the body always faces where you look |
| Respawn (R) | returns to where the object stood when the scene started |
| Changing `First Person` in the Inspector during Play | the camera jumps to the eyes, but the head is not hidden and the arms do not rise: only `SetView()` (the Tab key) does all three |

From the code, not measured: in first person, walking backwards or sideways
plays the forward walk animation, because only forward clips exist; and
walking into a wall keeps the walk animation playing, because the animation
speed comes from the keys rather than from real movement.

## An extra job hidden in the script

**Preview mode.** With `previewMode` on, movement input is ignored, jumping is
off, the top-left text changes and F1 leads back to the warehouse. The
`CharacterPreview` scene relies on this to show the model on a small stage.

Until 2026-10-04 the script also had a command-line auto-test
(`-nexus-autotest`) that saved screenshots into a folder next to the
repository. The PlayMode tests below replaced it.

## What other scripts can call

`Simulate(Vector2 input, bool run, bool jump, float dt)`, `SetView(bool firstPerson)`,
`Respawn()`, the read-only `Grounded` and `Position`, and the public fields
listed above. Everything else is private. Other assemblies need a reference
to `NetRunner.Player` and `using NetRunner.Player;` to see the class.

## Tests

Added 2026-10-04 (Step 4). Run them with `bin/unity-test edit` and
`bin/unity-test play` (editor closed) or through the editor bridge; see
[[architecture/tooling]].

**EditMode**, `Assets/NETRunner/Core/Tests/EditMode/VerticalMotionTests.cs`:
six tests of `VerticalMotion.Step()` without a scene: gravity takes 14 m/s
off every second, standing replaces a falling speed with the ground stick, a
jump from the floor starts at 4.4 m/s, a jump in the air or with jumping
switched off does nothing, and a jump rises about 0.66 m.

**PlayMode**, `Assets/NETRunner/Player/Tests/PlayMode/PlayerPlayModeTests.cs`:
each test loads the gameplay scene `Warehouse`, switches the script off so
it does not read the keyboard, lets the player settle for 90 frames, then
calls `Simulate()` itself, one frame at a time:

| Test | Checks |
| --- | --- |
| `PlayerStandsWalksAndRunsAtTheMeasuredSpeeds` | standing on the floor; 2 s of walking = 3.3 m (±0.1); 0.5 s of running = 1.625 m (±0.1); saves `Logs/PlayModeTests/walked-third-person.png` |
| `PlayerJumpsAboutTwoThirdsOfAMetreAndLands` | jump height 0.66 m (±0.06), standing again afterwards |
| `ADoorNearTheStartOpensWhenToggled` | `DOOR_Hall_X-18_4` reports open and has turned 95° (±1) after one second |

Step 6 ([#11](https://github.com/AdaM404-dev/NET.runner-92/issues/11))
splits this script up; these tests have to pass before and after.

## Known problems

Each of these is explained, with the proposed fix, in
[[architecture/before-new-scripts]].

- One class has nine jobs: input, movement, two camera modes, interaction,
  the text HUD, cursor handling, debug keys, respawn, preview mode.
- The two preview scenes still hold their own copies of the player.
- Most tunable numbers are written into the code; gravity and jump are
  named constants in `VerticalMotion`, but still not in the Inspector.
- `Awake` stops with an error if `Visual` is not assigned; `Interact` does the
  same if `View Camera` is missing.
- The script sets `Application.runInBackground = true` for the whole game.

## Open questions

- Is third person part of the game, or only a debug view?
- Should jumping exist at all in this game? (Design decision.)
