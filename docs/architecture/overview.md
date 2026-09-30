# Architecture overview

## Current state (2026-09-30)

All gameplay code is two MonoBehaviours in `Assets/Scripts/`, in the global
namespace, with no assembly definitions and no tests:

- `NexusPlayer.cs` — input (legacy `Input`), movement on a
  `CharacterController`, first/third-person camera, an `OnGUI` HUD, door
  interaction, and a command-line auto-test (`-nexus-autotest`) that walks,
  jumps, captures screenshots and writes to `../../QA/Runtime`, outside the
  repository. See [[systems/player-movement]].
- `NexusDoor.cs` — toggled swing or lift door.

`Assets/TutorialInfo/` and `Assets/Readme.asset` are Unity template leftovers.

## Target structure

Code stays under `Assets/Scripts/`, split into assemblies so that compile
times stay short and dependencies only point one way:

```
NetRunner.Core        plain C#, no scenes: stats, inventory, quests, saves, hacking rules
      ^
NetRunner.Gameplay    MonoBehaviours: player, interaction, AI, world objects
      ^
NetRunner.UI          HUD, menus
NetRunner.Editor      editor-only tools and validation (Editor platform only)
NetRunner.Tests.EditMode / NetRunner.Tests.PlayMode
```

Principles:

- **Rules in plain C#.** Anything that can be decided without a scene lives
  in `Core` and is tested in EditMode, which runs in seconds.
- **Thin MonoBehaviours.** They gather input, call `Core`, and present results.
- **Content as data.** Items, abilities, enemies and quests are
  ScriptableObjects; adding content should not require new code.
- **Input System package** for all new input.

## Planned first refactor

Split `NexusPlayer` into an input reader, a motor, a camera rig and a HUD,
keeping behaviour identical, and move the auto-test into a PlayMode test that
writes inside the project. This closes `agent.md` item V-03. The prefab
`NEXUS_Player.prefab` must be re-wired through the editor, not by editing YAML.
