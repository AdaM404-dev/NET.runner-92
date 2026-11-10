# NET.runner-92 · Unity continuation

## Start here

- [Phase 07 Windows x64 development build](../Builds/README.md#current-windows-build) is the current player for this snapshot. Keep the DLLs and `NETRunner92_Hospital_Data` beside the executable. Use W/A/S/D to move, the mouse to look, Shift to sprint and **E** to operate a world control. After breaching the reception terminal, press **1** for CCTV and use the arrow keys to switch feeds.
- [Unity project](../../Assets/NETRunner92) (moved into the root project by #31) uses installed Unity **6000.6.2f1**, HDRP **17.6.0** and Input System **1.19.0**. Open [Hospital_Exploration.unity](../../Assets/NETRunner92/Scenes/Hospital_Exploration.unity) and press Play. The scene opens with the invisible first-person capsule inside the south quarantine gate.
- The hospital now includes the LATCH-07 viewmodel and optional service relay. The new implementation, controls and QA evidence are in the [LATCH-07 Unity checkpoint](latch07_unity.md).
- The existing reception monitor runs a three-cell signature puzzle. It unlocks six live virtual viewpoints on LATCH-07 for the current scene session. [CCTV implementation and verification](cctv_unity.md)
- [Corrected Blender source](../Environment/Hospital_Quarantine_Grounds.blend) remains the source of truth. Its 12 collision proxies are hidden in Blender review layers and were deliberately exported for Unity physics.
- [Earlier Phase 4 state](phase4_state.md) and [Phase 3 state](phase3_state.md) preserve the previous deliverables.

## Saved and verified

- The saved scene includes the complete four-level hospital and quarantine grounds. The source export supplied 7,233 mesh placements. Unity displays 7,221 of these and uses the 12 exterior proxies only for physics. There are 2,058 total physics shapes and 12 smooth stair ramps.
- Direct3D 11 Play Mode verified the west/east/west stair route in both directions with the actual CharacterController. All six attempts to enter the isolated legs were blocked. The PhysX room graph reached 108/108 planned zones with the normal doors opened. Entrance, ambulance, slab elevations, walls, reception counter, ground contact, closed gate and perimeter probes passed.
- HDRP camera captures were checked outdoors and on all floors. Rain emission, roof rejection, transparent materials and the interior ambience crossfade were exercised. The source scene contains the real-time lighting, volume profiles, weather particles and an AudioMixer with five original ambience/effect clips.
- The current scene uses a 36 m shadow range, two cascades, reduced volumetric sampling and a 34 m camera cull distance for fine detail. It skips rain probes deep inside the building on all four floors. The full Play Mode suite passed again with these settings.
- The interactive scene adds 108 closed architectural door assemblies, a working west lift with stair-gated stops, four failing concourse practicals, five powered readouts and four K-7 patrols sourced from the original project package. Play Mode tested all four lift stops with the player aboard and checked the four patrol routes and original-rig animation. [Interaction systems](interactive_systems.md) · [Interactive Play Mode results](../Renders/Phase_05_Unity/Interactive/playmode_checks.txt)
- The interactive Windows build passed all 15 focused Input System checks, including E to open and close the public entrance and to call the lift at the west lobby. [Focused input results](../Renders/Phase_05_Unity/Interactive/focused_input_checks.txt)
- The previous traversal player covered 640.8 m from the south forecourt to Level 4 and back. At 1920 × 1080 on the RTX 3060 Ti, all five stationary views and all 18 moving segments recorded P95 frame times below 16.67 ms. That run predates the interactive systems. [Performance report](performance_report.md)

## Remaining at this checkpoint

1. In the Windows player, walk from the south gate to Level 4 and return by lift. Check mouse comfort, stair and lift control readability, patrol appearance in motion, and the outdoor rain to indoor ventilation crossfade by ear.
2. For another performance pass, compare the interactive build against the existing moving baseline, especially the Level 4 approach and L3→L4 stairs. Use repeated runs before assigning spikes to a specific cause. [Performance report](performance_report.md)

## Reproduction

The saved scene is ready to run. The Unity editor menu item `NET.runner-92 → Build Hospital Exploration Scene` regenerates the scene from the bundled `SceneSource.json` and `MeshData.bytes` pairs. It replaces generated assets. Preserve local scene edits before regeneration.

From the `Northline` directory in PowerShell, run the following with an active Unity Hub Editor license and the project closed in other Unity windows. Play Mode writes new captures into `Renders/Phase_07_CCTV`.

```powershell
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe'
$project = (Resolve-Path 'NETRunner92_Hospital').Path
& $unity -batchmode -force-d3d11 -projectPath $project `
  -executeMethod NETRunner92.Editor.PlayModeValidation.Run `
  -logFile (Join-Path $project 'Logs/Phase5PlayMode.log')
```

For a Windows rebuild after a verified scene edit, use `-batchmode -force-d3d11 -quit -executeMethod NETRunner92.Editor.HospitalSceneBuilder.BuildCctvWindows` with the same project path. The method installs the systems if needed and writes to `Builds/Phase_07_CCTV`. Close the player before replacing a build in place.

```powershell
$game = (Resolve-Path 'Builds/Phase_07_CCTV/NETRunner92_Hospital.exe').Path
$reports = Join-Path (Resolve-Path 'Renders/Phase_07_CCTV').Path 'Recheck'
New-Item -ItemType Directory -Path $reports -Force | Out-Null
& $game -screen-width 1920 -screen-height 1080 -screen-fullscreen 1 `
  -window-mode borderless -net92-benchmark -net92-output "$reports/stationary_performance.csv" | Out-Default
& $game -screen-width 1920 -screen-height 1080 -screen-fullscreen 1 `
  -window-mode borderless -net92-input-check -net92-output "$reports/focused_input_checks.txt" | Out-Default
& $game -screen-width 1920 -screen-height 1080 -screen-fullscreen 1 `
  -window-mode borderless -net92-route-benchmark -net92-output $reports | Out-Default
```

## Documentation

[Unity setup](unity_setup.md) · [Import validation](import_validation.md) · [Player controller](player_controller.md) · [Atmosphere system](atmosphere_system.md) · [Interaction systems](interactive_systems.md) · [Testing report](testing_report.md) · [Performance report](performance_report.md)
