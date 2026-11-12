# Northline Phase 11 · Signal Breach

AdaM404's Phase 11 version of the hospital (PR #52): main menu, settings,
three save slots, the HUD, the K-7 forearm cannon and the Signal Breach
hack. It is a separate Unity project (`NETRunner92_Hospital/`, Unity
`6000.6.2f1`, HDRP), not part of the root project.

`CODEX-TEST` is based on `feature/phase11-standard`. It includes the
Phase 11 HUD, menu and settings, the merged Player and World runtime
refactors, the F4 performance panel, frame pacing and the hospital
benchmark fix. The [repository coding standard](../../docs/architecture/coding-standards.md)
and [#55](https://github.com/AdaM404-dev/NET.runner-92/issues/55) describe
the integration.

Windows validation recorded 295/295 EditMode tests, 100/100 Vulkan
PlayMode tests (including all 95 hospital checks) and 55/55 release
acceptance checks. The release benchmark exited 0 and recorded all five
viewpoints. This version still needs the full Linux run described below.

## Open and run

1. Clone `CODEX-TEST` with Git LFS, then add `NETRunner92_Hospital/` in
   Unity Hub with Unity 6000.6.2f1.
2. Open `Assets/NETRunner92/Scenes/MainMenu.unity` and press Play.
   The hospital alone (`Hospital_Exploration.unity`) runs without the
   pause menu and saves, as the tests and benchmarks use it.
3. On Linux the player builds with Vulkan; Windows uses Direct3D 11.

## Windows player

Use **NET.runner-92 › Main Menu › Build Windows player** in the editor to
build `../Builds/Phase_11_Frame_Pacing/NETRunner92_Hospital.exe`. Run the
executable with the rest of its build folder beside it.

Press **F4** in the menu or hospital to toggle the performance panel. It
shows the CPU, GPU, RAM, graphics memory capacity, rendering API, quality,
resolution, render scale and live frame statistics. CPU and GPU timings are
shown when Unity's frame timing counters are available. F4 is reserved for
the panel. Existing profiles that assigned F4 get a free default key for
that action.

The main menu uses a 30 FPS software limit and its UI-only camera skips HDRP
dynamic resolution. New profiles and Restore Defaults use the
Medium preset and a 60 FPS gameplay limit. Existing profiles keep their
saved quality and limit. When VSync is requested, the game uses hardware
sync if the display can match the limit or runs slower, and uses software
pacing otherwise. The F4 panel shows the active limit and VSync interval.

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

## Hospital rendering

See [the detailed Phase 11 optimization and GPU measurements](PERFORMANCE_HU.md)
for the implementation, test setup and results in Hungarian.

The hospital scene uses baked occlusion culling, following [#36](https://github.com/AdaM404-dev/NET.runner-92/issues/36).
Walls and floors can hide geometry behind them. Glass can be hidden but
does not hide other objects. Doors, the lift and the K-7s remain dynamic.
The built-in Umbra module lets player builds use the baked data.
The hospital player camera also skips HDRP's decal pass because the scene
has no decal projectors.
After changing the imported building geometry, run **NET.runner-92 › Bake
Occlusion Culling** in the editor to regenerate the scene's culling data.

In the local Windows comparison (RTX 3060 Ti, Direct3D 11, 1920×1080
offscreen, Medium), both modes used the same development build and camera
positions. P95 is total frame time. The offscreen run returned no GPU
timestamps. The CSVs and screenshots are under `Logs/OcclusionUmbra/`.

| View | Triangles per frame, off → on | P95 frame time, off → on |
| --- | ---: | ---: |
| South checkpoint | 7.86M → 3.07M | 14.60 → 10.01 ms |
| Reception | 4.26M → 0.73M | 10.03 → 6.26 ms |
| Ward | 6.26M → 0.86M | 12.94 → 8.86 ms |

## Checks

- EditMode and PlayMode tests: Unity's Test Runner, or headless with
  `-runTests -testPlatform EditMode|PlayMode`.
- Run the hospital benchmark from the Windows player with
  `-net92-benchmark -net92-quality Medium -net92-profile <empty folder> -net92-output <csv>`.
  It loads the hospital from the menu, applies the selected quality, then
  measures five viewpoints with VSync and the FPS limit off. The first CSV
  line identifies the GPU, graphics API, output resolution, quality and
  render mode. Offscreen mode renders into a texture when the window does not
  render. An unavailable GPU timing or rendering counter is reported as `-1`.
- In a built player: `-net92-menu-check -net92-profile <empty folder>`
  (menu, settings, saves) and, in a build that starts in the hospital,
  `-net92-input-check -net92-output <file>` (movement, doors, LATCH-07,
  terminal, CCTV). The input check needs the window to keep focus.

## Linux validation

Run the archive itself with Unity `6000.6.2f1` from a Linux desktop session
with Vulkan support. Set `UNITY_EDITOR` to the Linux Unity executable,
close the editor, and run the following from this directory.

```bash
project="$(realpath NETRunner92_Hospital)"
logs="$project/Logs/Step55-Linux"
mkdir -p "$logs"
"$UNITY_EDITOR" -batchmode -quit -nographics -projectPath "$project" -logFile "$logs/Compile.log"
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$project" \
  -runTests -testPlatform EditMode -testResults "$logs/EditMode.xml" -logFile "$logs/EditMode.log"
"$UNITY_EDITOR" -batchmode -force-vulkan -projectPath "$project" \
  -runTests -testPlatform PlayMode -testResults "$logs/PlayMode.xml" -logFile "$logs/PlayMode.log"
```

The PlayMode suite includes `HospitalChecksTests`. Its detailed report is
`NETRunner92_Hospital/Logs/hospital-checks/playmode_checks.txt`. Use
the full suite's exit status and XML to record the result for this version.

With Linux Build Support installed, also build the two enabled scenes and
run the benchmark with a fresh profile.

```bash
build="$logs/Build/NETRunner92_Hospital.x86_64"
mkdir -p "$(dirname "$build")"
"$UNITY_EDITOR" -batchmode -quit -force-vulkan -projectPath "$project" \
  -buildTarget Linux64 -buildLinux64Player "$build" -logFile "$logs/BuildLinux.log"
"$build" -net92-benchmark -net92-quality Medium -net92-profile "$logs/benchmark-profile" \
  -net92-output "$logs/benchmark.csv" -logFile "$logs/Benchmark.log"
```

## Open points

- The menu music `Menu/Deep Atmospheric Pads.mp3` has no source or licence note yet.
- #55 step 4 has the merged Player refactor (#43, PR #57) and World
  runtime refactor (#44, PR #60). The remaining World scene-builder split
  (#44) and Diagnostics refactor (#45) are pending on the main branch.
