# Architecture overview

How the code is organised, why, and where a new script goes. For where every
file and object is, see [[architecture/project-map]].

## Feature folders (since 2026-10-04)

All code lives under `Assets/NETRunner/`, one folder per feature. Each
feature folder holds its own scripts, its own tests and, later, its own
prefabs, UI and art, so everything about one feature is in one place. The
gameplay code moved into it in Step 4 ([#9](https://github.com/AdaM404-dev/NET.runner-92/issues/9)).

**State since #31 PR A (2026-11-10):** the warehouse code (`World/`,
`Player/`) and the `MainMenu/` prototype were deleted. Playtest 2 added
`Settings/`:

```
Assets/NETRunner/
├─ Core/                          NetRunner.Core: rules in plain C#, no scene objects
│  ├─ Scripts/VerticalMotion.cs     gravity, jump and ground stick for one frame
│  ├─ Scripts/Easing.cs             a slow-start, slow-end curve
│  ├─ Scripts/QualityChoice.cs      next quality level, start level
│  ├─ Scripts/StartResolution.cs    first-start screen size, capped at 1080p
│  ├─ Scripts/FrameRateMeter.cs     averaged frames per second
│  └─ Tests/EditMode/               28 tests, run in a fraction of a second
└─ Settings/                      NetRunner.Settings: graphics quality for players
   ├─ Scripts/QualitySwitch.cs      F2 quality, F3 FPS, resolution scaling on screen cameras
   └─ Tests/PlayMode/               5 tests
```

The hospital's code is still in `Assets/NETRunner92/` in one namespace,
`NETRunner92`, outside any assembly definition (see
[[architecture/project-map]]). #31 PR B moves it into this layout, behaviour
unchanged: `Player/` (`FirstPersonController`, LATCH-07, CCTV), `World/`
(doors, lift, stairs, interaction, terminal), a new `Enemy/` (`K7Patrol`),
a new `Atmosphere/`, and an `Editor/` assembly per feature for the scene
builders.

Each `Scripts/` folder has an **assembly definition** (`.asmdef` file). Unity
compiles every assembly definition into its own DLL, and a script can only
use code from assemblies its own assembly lists as references. Two things
follow from that:

- **Dependencies point one way.** `Player` may use `World` and `Core`;
  `World` may use `Core`; `Core` uses nothing of ours. If `Core` tried to use
  the player script, it would not compile. That keeps the rules in `Core`
  testable without a scene.
- **Faster compiles.** Changing a file recompiles its own assembly and the
  ones that reference it, not the whole project.

The namespace matches the assembly: `NetRunner.Core`, `NetRunner.World`,
`NetRunner.Player`. Namespaces are what packages are in Java: after #31 PR B
the full name of the door script will be `NetRunner.World.HospitalDoor`.

```
NetRunner.Player ──→ NetRunner.World ──→ NetRunner.Core
        │                                       ↑
        └───────────────────────────────────────┘
```

In the hospital code the interaction base class (`HospitalInteractable`)
takes the player type as a parameter, so `World` and `Player` would need
each other. #31 PR B has to break that cycle; the planned way is
[#10](https://github.com/AdaM404-dev/NET.runner-92/issues/10): an
interaction interface in `Core`, so the player talks to "anything
interactable".

**Tests sit next to the code they test**, in `Tests/EditMode/` (no scene,
runs in milliseconds; for `Core`) and `Tests/PlayMode/` (loads a scene and
runs frames; for MonoBehaviours). Test assemblies carry the define
constraint `UNITY_INCLUDE_TESTS`, so a normal build of the game leaves them
out. How to run them: [[architecture/tooling]].

## Where does my new script go?

| The script… | Goes into | Example |
| --- | --- | --- |
| decides something without needing a scene: a rule, a formula, a state machine | `Core/Scripts/`, with an EditMode test | `VerticalMotion`, `Easing`, later stats, inventory, hacking rules, the interaction interface |
| is a thing placed in the level | `World/Scripts/` | `HospitalDoor`, `HospitalLift`, terminals, pickups |
| belongs to the player | `Player/Scripts/` | `FirstPersonController`, `LatchViewmodel` |
| starts a new feature (enemies, hacking UI, saves) | a new folder `Assets/NETRunner/<Feature>/` with `Scripts/` and an assembly definition `NetRunner.<Feature>` | `K7Patrol` goes into `Enemy/` |
| is an editor tool | `<Feature>/Editor/`, with an assembly definition limited to the Editor platform | the hospital's scene builders, after #31 PR B |

Asset helper scripts delivered with art (for example the K7 robot's) keep
their own assembly next to their asset; see [[systems/enemy-k7]].

A script that ends up outside any assembly definition compiles into Unity's
default assembly (`Assembly-CSharp`), and **no assembly definition can
reference that one**. So a stray script in a random folder cannot be used by
our code. Today the hospital code in `Assets/NETRunner92/` is outside any
assembly definition; #31 PR B fixes that.

## Principles

The rules for writing code (paradigm, fields and access, naming, member
order, layout, formatting, tests) are in [[architecture/coding-standards]].
In one line: rules in plain C# in `Core` with EditMode tests, thin
MonoBehaviours, content as ScriptableObjects. The hospital code does not
follow this yet; #31 PR C brings it in line feature by feature.

## Why feature folders, not one folder per kind of code

Step 4 first proposed `Core/`, `Gameplay/`, `UI/` folders under
`Assets/Scripts/`. Samuel chose feature folders on 2026-10-04 (recorded on
[#9](https://github.com/AdaM404-dev/NET.runner-92/issues/9)): the menu
already used them, everything about one feature stays together, and a
feature can be removed by deleting one folder. `Core` is the one shared
folder, for rules that several features use.

## Next refactors

From [#31](https://github.com/AdaM404-dev/NET.runner-92/issues/31): PR B
moves the hospital code into feature folders with assemblies and turns its
95 Play-mode checks into NUnit tests (behaviour unchanged, the checks pass
before and after); PR C and later bring each feature in line with
[[architecture/coding-standards]], including the interaction contract of
[#10](https://github.com/AdaM404-dev/NET.runner-92/issues/10). The switch
to the Input System (#12) is done: the hospital code uses it and Active
Input Handling is Input System only. Scenes and prefabs are re-wired
through the editor, never by editing their YAML.
