# Northline Phase 11 · Signal Breach

AdaM404's Phase 11 version of the hospital (PR #52): main menu, settings,
three save slots, the HUD, the K-7 forearm cannon and the Signal Breach
hack. It is a separate Unity project (`NETRunner92_Hospital/`, Unity
`6000.6.2f1`, HDRP), not part of the root project.

On branch `feature/phase11-standard` its code follows the repository's
coding standard (#55): the feature folders and rules of `main`'s
`Assets/NETRunner/`, with the Phase 11 features added as `Settings`,
`Saves`, `Menu` and `Hud`. Merging it into `main` is a later decision.

## Open and run

1. Add `NETRunner92_Hospital/` in Unity Hub with Unity 6000.6.2f1.
2. Open `Assets/NETRunner92/Scenes/MainMenu.unity` and press Play.
   The hospital alone (`Hospital_Exploration.unity`) runs without the
   pause menu and saves, as the tests and benchmarks use it.
3. On Linux the player builds with Vulkan; Windows uses Direct3D 11.

## How the pieces connect

| Piece | Where | Note |
| --- | --- | --- |
| Settings | `Assets/NETRunner/Settings/` | One `Preferences` asset (`Settings/Resources/`), applied by `PreferencesRuntime`, created at start. The player's controller references it; F2 picks the next quality level's preset. |
| Saves | `Assets/NETRunner/Saves/` | `slot-1.json` to `slot-3.json` in the profile folder (`-net92-profile <folder>` to choose one). |
| Menu and run | `Assets/NETRunner/Menu/` | The menu hands the slot to the `RunSession` asset (`Menu/Data/`); `HospitalRun` in the hospital scene takes it. |
| HUD | `Assets/NETRunner/Hud/` | Layout in `Assets/NETRunner92/UI/HospitalHud.uxml`. |
| Rules | `Assets/NETRunner/Core/` | `SignalBreach`, `CannonCycle`, `CheckpointRules`, `PreferenceRules`, `QualityPreset`, `RoomPlan`, with EditMode tests. |

After rebuilding a scene with the hospital's builders, run
**NET.runner-92 › Phase 11 › Install runtime references**: it sets the
references the features need (no component searches the scene at runtime).

## Checks

- EditMode and PlayMode tests: Unity's Test Runner, or headless with
  `-runTests -testPlatform EditMode|PlayMode`.
- In a built player: `-net92-menu-check -net92-profile <empty folder>`
  (menu, settings, saves) and, in a build that starts in the hospital,
  `-net92-input-check -net92-output <file>` (movement, doors, LATCH-07,
  terminal, CCTV). The input check needs the window to keep focus.

## Open points

- The menu music `Menu/Deep Atmospheric Pads.mp3` has no source or licence note yet.
- Player, World and Diagnostics take `main`'s #43–#45 once those merge (#55 step 4).
