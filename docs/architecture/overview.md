# Architecture overview

How the code is organised, why, and where a new script goes. For where every
file and object is, see [[architecture/project-map]].

## Feature folders (since 2026-10-04)

All code lives under `Assets/NETRunner/`, one folder per feature. Each
feature folder holds its own scripts, its own tests and, later, its own
prefabs, UI and art, so everything about one feature is in one place. The
main menu prototype (PR #22) started this layout; the gameplay code moved
into it in Step 4 ([#9](https://github.com/AdaM404-dev/NET.runner-92/issues/9)).

```
Assets/NETRunner/
├─ Core/                          NetRunner.Core: rules in plain C#, no scene objects
│  ├─ Scripts/VerticalMotion.cs     gravity, jump and ground stick for one frame
│  └─ Tests/EditMode/               6 tests, run in a fraction of a second
├─ World/                         NetRunner.World: things that live in the level
│  └─ Scripts/NexusDoor.cs          a door that swings or lifts when toggled
├─ Player/                        NetRunner.Player: the player you control
│  ├─ Scripts/NexusPlayer.cs        input, movement, camera, interaction, HUD
│  └─ Tests/PlayMode/               3 tests: walk, run, jump, open a door
└─ MainMenu/                      NetRunner.MenuPrototype: the standalone menu
```

Each `Scripts/` folder has an **assembly definition** (`.asmdef` file). Unity
compiles every assembly definition into its own DLL, and a script can only
use code from assemblies its own assembly lists as references. Two things
follow from that:

- **Dependencies point one way.** `Player` may use `World` and `Core`;
  `World` may use `Core`; `Core` uses nothing of ours. If `Core` tried to use
  `NexusPlayer`, it would not compile. That keeps the rules in `Core`
  testable without a scene.
- **Faster compiles.** Changing a file recompiles its own assembly and the
  ones that reference it, not the whole project.

The namespace matches the assembly: `NetRunner.Core`, `NetRunner.World`,
`NetRunner.Player`. Namespaces are what packages are in Java: the full name
of the door script is `NetRunner.World.NexusDoor`.

```
NetRunner.Player ──→ NetRunner.World ──→ NetRunner.Core
        │                                       ↑
        └───────────────────────────────────────┘
```

`Player` references `World` only because `NexusPlayer.Interact()` looks for a
`NexusDoor`. Once Step 5 ([#10](https://github.com/AdaM404-dev/NET.runner-92/issues/10))
puts an `IInteractable` interface into `Core`, that reference can go: the
player then talks to "anything interactable", not to doors.

**Tests sit next to the code they test**, in `Tests/EditMode/` (no scene,
runs in milliseconds; for `Core`) and `Tests/PlayMode/` (loads a scene and
runs frames; for MonoBehaviours). Test assemblies carry the define
constraint `UNITY_INCLUDE_TESTS`, so a normal build of the game leaves them
out. How to run them: [[architecture/tooling]].

## Where does my new script go?

| The script… | Goes into | Example |
| --- | --- | --- |
| decides something without needing a scene: a rule, a formula, a state machine | `Core/Scripts/`, with an EditMode test | `VerticalMotion`, later stats, inventory, hacking rules, `IInteractable` |
| is a thing placed in the level | `World/Scripts/` | `NexusDoor`, later terminals, access readers, pickups |
| belongs to the player | `Player/Scripts/` | `NexusPlayer`, later `PlayerMotor`, `PlayerCamera` (Step 6) |
| starts a new feature (enemies, hacking UI, saves) | a new folder `Assets/NETRunner/<Feature>/` with `Scripts/` and an assembly definition `NetRunner.<Feature>` | the K7 enemy's gameplay code would be `Enemy/` |
| is an editor tool | `<Feature>/Editor/`, with an assembly definition limited to the Editor platform | `MainMenu/Editor/` |

Asset helper scripts delivered with art (for example the K7 robot's) keep
their own assembly next to their asset; see [[systems/enemy-k7]].

A script that ends up outside any assembly definition compiles into Unity's
default assembly (`Assembly-CSharp`), and **no assembly definition can
reference that one**. So a stray script in a random folder cannot be used by
our code. Today no script is outside an assembly definition.

## Principles

- **Rules in plain C#.** Anything that can be decided without a scene lives
  in `Core` and is tested in EditMode, which runs in seconds.
- **Thin MonoBehaviours.** They gather input, call `Core`, and present results.
  `NexusPlayer.Simulate()` does this for gravity and jumping: it hands the
  numbers to `VerticalMotion.Step()` and only moves the CharacterController.
- **Content as data.** Items, abilities, enemies, quests and tuning values
  are ScriptableObjects; adding content should not require new code.
- **Input System package** for all new input.
- **Named layers and `LayerMask` fields**, never layer numbers in code.
- **One statement per line**, braces on their own lines; `.editorconfig` at
  the repository root holds the rules for Rider and Visual Studio.

## Why feature folders, not one folder per kind of code

Step 4 first proposed `Core/`, `Gameplay/`, `UI/` folders under
`Assets/Scripts/`. Samuel chose feature folders on 2026-10-04 (recorded on
[#9](https://github.com/AdaM404-dev/NET.runner-92/issues/9)): the menu
already used them, everything about one feature stays together, and a
feature can be removed by deleting one folder. `Core` is the one shared
folder, for rules that several features use.

## Next refactors

Steps 5 to 7 of [[architecture/before-new-scripts]]: the `IInteractable`
contract, splitting `NexusPlayer` into components with identical behaviour
(the PlayMode tests have to pass before and after), and the switch to the
Input System (closes `agent.md` item V-03). Scenes and prefabs are re-wired
through the editor, never by editing their YAML.
