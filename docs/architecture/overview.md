# Architecture overview

How the code is organised today, and the structure it is moving to. For
where every file and object is, see [[architecture/project-map]].

## Current state (2026-10-02)

All gameplay code is two MonoBehaviours in `Assets/Scripts/`, in the global
namespace, with no assembly definitions and no tests:

- `NexusPlayer.cs`: input (legacy `Input`), movement on a
  `CharacterController`, first- and third-person camera, door interaction, an
  `OnGUI` HUD, debug keys, preview mode, and a command-line auto-test. See
  [[systems/player]], [[systems/camera]] and [[systems/player-movement]].
- `NexusDoor.cs`: a door that swings or lifts when toggled. See
  [[systems/doors-and-interaction]].

Doors, colliders and lights live in the warehouse prefab, and gameplay
objects go into the gameplay scene `Assets/Scenes/Game/Warehouse.unity`
(since 2026-10-04). What has to change before new
scripts are added is listed, in order, in [[architecture/before-new-scripts]].

The independent main-menu prototype merged in PR #22 lives under
`Assets/NETRunner/MainMenu/`, with runtime, editor and PlayMode test assemblies.
It uses UI Toolkit and has two recorded passing PlayMode tests. It does not
implement the planned Core/Gameplay structure or connect to the player, saves
or scene loading. See [[systems/main-menu]].

## Target structure

Code stays under `Assets/Scripts/`, split into assemblies so that compile
times stay short and dependencies only point one way:

```
NetRunner.Core        plain C#, no scenes: stats, inventory, quests, saves, hacking
      ^               rules, interfaces such as IInteractable, movement maths
NetRunner.Gameplay    MonoBehaviours: PlayerInput, PlayerMotor, PlayerCamera,
      ^               PlayerInteractor, doors, AI, world objects
NetRunner.UI          HUD, menus
NetRunner.Editor      editor-only tools and validation (Editor platform only)
NetRunner.Tests.EditMode / NetRunner.Tests.PlayMode
```

Asset helper scripts delivered with art (for example the K7 robot's) get their
own assembly next to their asset instead of joining ours.

Principles:

- **Rules in plain C#.** Anything that can be decided without a scene lives
  in `Core` and is tested in EditMode, which runs in seconds.
- **Thin MonoBehaviours.** They gather input, call `Core`, and present results.
- **Content as data.** Items, abilities, enemies, quests and tuning values
  are ScriptableObjects; adding content should not require new code.
- **Input System package** for all new input.
- **Named layers and `LayerMask` fields**, never layer numbers in code.

## First refactors

Steps 4 to 7 of [[architecture/before-new-scripts]]: the structure above with
a first test, the `IInteractable` contract, splitting `NexusPlayer` into the
components listed above with identical behaviour, and the switch to the Input
System (closes `agent.md` item V-03). Scenes and prefabs are re-wired through
the editor, never by editing their YAML.
