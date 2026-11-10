# Architecture overview

How the code is organised, why, and where a new script goes. For where every
file and object is, see [[architecture/project-map]].

## Feature folders (since 2026-10-04)

All code lives under `Assets/NETRunner/`, one folder per feature. Each
feature folder holds its own scripts, its own tests and, later, its own
prefabs, UI and art, so everything about one feature is in one place. The
gameplay code moved into it in Step 4 ([#9](https://github.com/AdaM404-dev/NET.runner-92/issues/9)).

**State since #31 PR B (2026-11-10):** the warehouse code and the
`MainMenu/` prototype were deleted in PR A; playtest 2 added `Settings/`;
PR B moved the hospital code in, behaviour unchanged; #41 brought `Enemy`
to the coding standard:

```
Assets/NETRunner/
├─ Core/                          NetRunner.Core: rules in plain C#, no scene objects
│  ├─ Scripts/VerticalMotion.cs     gravity, jump and ground stick for one frame
│  ├─ Scripts/Easing.cs             a slow-start, slow-end curve
│  ├─ Scripts/QualityChoice.cs      next quality level, start level
│  ├─ Scripts/StartResolution.cs    first-start screen size, capped at 1080p
│  ├─ Scripts/FrameRateMeter.cs     averaged frames per second
│  ├─ Scripts/LaunchArguments.cs    reads the game's command-line options
│  ├─ Scripts/Patrol.cs             walking a route back and forth: turn, walk, wait at the ends
│  ├─ Scripts/K7Gait.cs             the K-7's leg, arm and head angles as it walks
│  └─ Tests/EditMode/               56 tests, run in a fraction of a second
├─ Settings/                      NetRunner.Settings: graphics quality for players
│  ├─ Scripts/QualitySwitch.cs      F2 quality, F3 FPS, resolution scaling on screen cameras
│  └─ Tests/PlayMode/               5 tests
├─ Player/                        NetRunner.Player: FirstPersonController, interaction, LATCH-07, CCTV
│  └─ Editor/                       NetRunner.Player.Editor: LATCH-07 import check
├─ World/                         NetRunner.World: doors, lift, stairs, terminal, emergency power
│  └─ Editor/                       NetRunner.World.Editor: the hospital scene builders
├─ Enemy/                         NetRunner.Enemy: K7Patrol (moves the robot, turns its bones)
│  └─ Tests/PlayMode/               5 tests on a stand-in robot
├─ Atmosphere/                    NetRunner.Atmosphere: fog zones, ambience, light pulses
└─ Diagnostics/                   NetRunner.Diagnostics: the 95 hospital checks, benchmark modes
   └─ Tests/PlayMode/               1 test that runs the 95 checks
```

Two choices differ from the plan on #31, both to keep behaviour unchanged:

- **One editor assembly for the scene builders**, in `World/Editor/`, not
  one per feature. The builders are a single `partial class
  HospitalSceneBuilder` spread over eight files, and C# cannot split one
  class across assemblies. Splitting it is part of #44.
- **A `Diagnostics` feature** for the hospital's checks and the benchmark
  modes. They use every other feature, and the benchmark modes have to
  ship in the built game, so they cannot live in a test assembly. The
  checks stay usable from `PlayModeValidation` (AdaM404's workflow) and from
  the NUnit test.

Each `Scripts/` folder has an **assembly definition** (`.asmdef` file). Unity
compiles every assembly definition into its own DLL, and a script can only
use code from assemblies its own assembly lists as references. Two things
follow from that:

- **Dependencies point one way.** `Core` uses nothing of ours. If `Core`
  tried to use the player script, it would not compile. That keeps the
  rules in `Core` testable without a scene.
- **Faster compiles.** Changing a file recompiles its own assembly and the
  ones that reference it, not the whole project.

The namespace matches the assembly: `NetRunner.Core`, `NetRunner.World`,
`NetRunner.Player`. Namespaces are what packages are in Java: the full name
of the door script is `NetRunner.World.HospitalDoor`.

```
NetRunner.Diagnostics ──→ World, Enemy, Atmosphere, Player
NetRunner.World ────────→ NetRunner.Player
NetRunner.Settings ─────→ NetRunner.Core
NetRunner.Enemy ────────→ NetRunner.Core
NetRunner.Player, Atmosphere: none of ours
```

**`World` uses `Player`**: doors, lift and stairs take a
`FirstPersonController` (`IInteractable.Interact(FirstPersonController)`).
The player never names a `World` type: it talks to "anything
interactable" through the interfaces `IInteractable` and `ITerminal` in
`Player` ([#10](https://github.com/AdaM404-dev/NET.runner-92/issues/10)).
They are in `Player` rather than `Core` because `Core` has no engine
references and the one who interacts is the player.

**Tests sit next to the code they test**, in `Tests/EditMode/` (no scene,
runs in milliseconds; for `Core`) and `Tests/PlayMode/` (loads a scene and
runs frames; for MonoBehaviours). Test assemblies carry the define
constraint `UNITY_INCLUDE_TESTS`, so a normal build of the game leaves them
out. How to run them: [[architecture/tooling]].

## Where does my new script go?

| The script… | Goes into | Example |
| --- | --- | --- |
| decides something without needing a scene: a rule, a formula, a state machine | `Core/Scripts/`, with an EditMode test | `Patrol`, `QualityChoice`, later stats, inventory, hacking rules |
| is a thing placed in the level | `World/Scripts/` | `HospitalDoor`, `HospitalLift`, terminals, pickups |
| belongs to the player | `Player/Scripts/` | `FirstPersonController`, `LatchViewmodel` |
| starts a new feature (enemies, hacking UI, saves) | a new folder `Assets/NETRunner/<Feature>/` with `Scripts/` and an assembly definition `NetRunner.<Feature>` | `K7Patrol` goes into `Enemy/` |
| is an editor tool | `<Feature>/Editor/`, with an assembly definition limited to the Editor platform | the hospital's scene builders in `World/Editor/` |

Asset helper scripts delivered with art (for example the K7 robot's) keep
their own assembly next to their asset; see [[systems/enemy-k7]].

A script that ends up outside any assembly definition compiles into Unity's
default assembly (`Assembly-CSharp`), and **no assembly definition can
reference that one**. So a stray script in a random folder cannot be used by
our code. Since #31 PR B no script of ours is outside an assembly
definition.

## Principles

The rules for writing code (paradigm, fields and access, naming, member
order, layout, formatting, tests) are in [[architecture/coding-standards]].
In one line: rules in plain C# in `Core` with EditMode tests, thin
MonoBehaviours, content as ScriptableObjects. `Enemy` follows it since #41;
the rest of the hospital code does not yet, and #42–#45 bring it in line,
one feature each (Atmosphere, Player, World, Diagnostics).

## Why feature folders, not one folder per kind of code

Step 4 first proposed `Core/`, `Gameplay/`, `UI/` folders under
`Assets/Scripts/`. Samuel chose feature folders on 2026-10-04 (recorded on
[#9](https://github.com/AdaM404-dev/NET.runner-92/issues/9)): the menu
already used them, everything about one feature stays together, and a
feature can be removed by deleting one folder. `Core` is the one shared
folder, for rules that several features use.

## Next refactors

From [#31](https://github.com/AdaM404-dev/NET.runner-92/issues/31): PR B
moved the hospital code into feature folders with assemblies and runs its
95 Play-mode checks as an NUnit test (behaviour unchanged, the checks pass
before and after); #41 (done) to #45 bring each feature in line with
[[architecture/coding-standards]], including the interaction contract of
[#10](https://github.com/AdaM404-dev/NET.runner-92/issues/10). The switch
to the Input System (#12) is done: the hospital code uses it and Active
Input Handling is Input System only. Scenes and prefabs are re-wired
through the editor, never by editing their YAML.
