# Player

**Purpose.** The object you control: a capsule that moves through the level,
a body that animates, and the camera you see through. All of it is driven by
one script, `NexusPlayer`.

**Status.** Works as an asset-preview controller. It is not yet structured
for gameplay; see [[architecture/before-new-scripts]].

Verified 2026-09-30 on commit `df3a31d` (Unity 6000.6.2f1, Linux) with
`bin/unity-inspect player` and scripted Play-mode checks. The checks called
the same methods the keys call; nobody pressed the keys by hand.

New to Unity words like *component* or *prefab*? Read
[[guide/01-unity-in-this-project]] first.

## Where it lives

| What | Where |
| --- | --- |
| Scene object | scene `MainTest` → `NEXUS_Player`, at the top level of the Hierarchy |
| Script | [`Assets/Scripts/NexusPlayer.cs`](../../Assets/Scripts/NexusPlayer.cs), 135 lines, one class |
| Prefab | `Assets/Prefabs/NEXUS_Player.prefab`: the same setup, but **no scene uses it** |
| Second copy | scene `CharacterPreview` → `Character_Preview`: the same script with `previewMode` switched on |

The three copies currently hold identical values. Changing one does not
change the others.

## The object tree

```
NEXUS_Player              layer 8 (unnamed), position (0, 0.03, 12), tag Untagged
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

**Numbers written in the code**, not reachable from the Inspector:

| Number | Value | Line |
| --- | --- | --- |
| Gravity | 14 m/s² (Unity's own physics gravity of 9.81 is not used) | 73 |
| Jump speed | 4.4 m/s upward; measured jump 0.66 m high, 0.62 s in the air | 72 |
| Ground stick | −2 m/s while standing, keeps the capsule pressed to the floor | 71 |
| Look up/down limit | ±78°, starting at 8° down | 60, 19 |
| Third-person turn rate | 12 (higher = the body turns faster toward movement) | 69 |
| Animation smoothing | 0.15 s | 74 |
| Fall limit | below y = −12 the player is put back at the start | 75 |
| Interaction distance | 3.5 m from the camera | 92 |
| Layers | `1<<9` = world, `~(1<<8)` = everything except the player | 86, 92 |

## Keys

| Key | What it does | Line |
| --- | --- | --- |
| W A S D, arrow keys, gamepad stick | move (legacy axes `Horizontal`, `Vertical`) | 61 |
| Mouse | look | 60 |
| Left Shift | run | 62 |
| Space | jump | 62 |
| E | interact; today that means doors only, see [[systems/doors-and-interaction]] | 59 |
| Tab | switch first / third person | 56 |
| F1 | load the other scene (`MainTest` ↔ `CharacterPreview`) | 57 |
| R | back to the start position | 58 |
| Esc | release or recapture the mouse cursor; no movement while released | 54 |
| Left click | recapture the cursor | 55 |

All of these are read with Unity's old `Input` class. The project also
contains an Input System actions asset (`Assets/InputSystem_Actions.inputactions`
with Move, Look, Jump, Sprint, Interact, Crouch, Attack) that no script uses.

## What happens in one frame

Unity calls these methods by itself; nothing in the project calls them.

```
once, when the scene starts
  Awake()        line 27   find the CharacterController, remember the start
                           position, collect the model's renderers, apply the
                           starting view, lock the mouse cursor
  Start()        line 37   start the auto-test if it was requested

every frame, in this order
  Update()       line 51   read keys and mouse
    ├─ Esc, click, Tab, F1, R, E
    ├─ mouse → yaw and pitch (the two look angles)
    └─ Simulate(input, run, jump, dt)        line 64
         1. input + yaw → a direction in the world
         2. turn the body: first person faces where you look,
            third person turns toward the movement direction
         3. gravity and jump change verticalSpeed
         4. CharacterController.Move(...)  ← the only place the player moves
         5. tell the Animator the speed (from the input, not from real movement)
         6. fell too far → Respawn()

  (Unity's Animator then poses the skeleton)

  LateUpdate()   line 78   put the camera in place, after the body has moved
  OnGUI()        line 95   draw the text in the top-left corner and the notices
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

## Two extra jobs hidden in the script

**Preview mode.** With `previewMode` on, movement input is ignored, jumping is
off, the top-left text changes and F1 leads back to the warehouse. The
`CharacterPreview` scene relies on this to show the model on a small stage.

**Auto-test** (lines 103–134). When the game is started with the command-line
argument `-nexus-autotest`, the script ignores the keyboard and runs a
scripted sequence: walk forward, jump, switch to first person, look down,
toggle one door, then load `CharacterPreview`. It saves four screenshots and
`RuntimeValidation.json` into `../../QA/Runtime` relative to `Assets/`, which
is a folder **next to** the repository, not inside it.

## What other scripts can call

`Simulate(Vector2 input, bool run, bool jump, float dt)`, `SetView(bool firstPerson)`,
`Respawn()`, the read-only `Grounded` and `Position`, and the public fields
listed above. Everything else is private.

## Tests

None. The auto-test above is the only automated check and it is not part of
Unity's test runner.

## Known problems

Each of these is explained, with the proposed fix, in
[[architecture/before-new-scripts]].

- One class has ten jobs: input, movement, two camera modes, interaction, the
  text HUD, cursor handling, debug keys, respawn, preview mode, auto-test.
- The player exists three times (two scenes and an unused prefab).
- Layers are used by number only; layers 8 and 9 have no names.
- Tunable numbers are buried in the code.
- `Awake` stops with an error if `Visual` is not assigned; `Interact` does the
  same if `View Camera` is missing.
- The script sets `Application.runInBackground = true` for the whole game.

## Open questions

- Is third person part of the game, or only a debug view?
- Should jumping exist at all in this game? (Design decision.)
