# Quality settings

## Purpose

Let the game run on weaker graphics cards: three quality levels the player
switches with **F2**, an FPS counter on **F3**, and a first start that
does not draw at a 4K monitor's full size. Added 2026-11-10 after the first
Windows alpha test build lagged; Samuel chose the design in chat ([#34](https://github.com/AdaM404-dev/NET.runner-92/issues/34)).

## Status

Built and tested on Linux; released in alpha test 2. Not yet measured on
the Windows test machine; see [[systems/performance]].

## Where it lives

| What | Where |
| --- | --- |
| The component | `Assets/NETRunner/Settings/Scripts/QualitySwitch.cs` (assembly and namespace `NetRunner.Settings`) |
| Rules, plain C# | `Assets/NETRunner/Core/Scripts/QualityChoice.cs`, `StartResolution.cs`, `FrameRateMeter.cs`, `LaunchArguments.cs` |
| Tests | `Core/Tests/EditMode/QualityChoiceTests`, `StartResolutionTests`, `FrameRateMeterTests`, `LaunchArgumentsTests` (24); `Settings/Tests/PlayMode/QualitySwitchTests` (5) |
| Pipeline assets | `Assets/NETRunner92/Rendering/Northline_HDRP_Low.asset`, `Northline_HDRP_Medium.asset`, `Northline_HDRP.asset` (High) |
| Levels | Edit → Project Settings → Quality |

## The three levels

| | Low | Medium | High |
| --- | --- | --- | --- |
| Pipeline asset | `Northline_HDRP_Low` | `Northline_HDRP_Medium` | `Northline_HDRP` (the Northline setup, unchanged) |
| Render resolution, upscaled with STP | 50% | 67% | 100% |
| Screen-space reflections | off | tier 1 at 16 ray steps (High: 32) | on |
| Contact shadows | off | tier 1 at 6 samples (High: 10) | on |
| Ambient occlusion, tier 1 | 4 steps, 1 direction | 4 steps | 6 steps, 2 directions |
| Shadow atlases (punctual, area) | 2048 | 2048 | 4096 |
| Largest single shadow map (directional, punctual, area) | 1024 | 2048 | 2048 |
| VSync | off | off | off |

The hospital's volumes pick HDRP quality *tiers* (fog tier 0, reflections,
ambient occlusion and contact shadows tier 1), and each pipeline asset
decides what a tier costs, so the values above change the look without
touching the scene. Fog is already on the cheapest tier everywhere and
stays as it is. The shadow *distance* (36 m) is set in a scene volume, not
in the pipeline asset, so it is the same on all levels.

The non-pipeline settings of each level are Unity's defaults for Low,
Medium and Ultra (renamed High): for example, LOD bias 0.4, 0.7 and 1.

**STP** (Spatial-Temporal Post-processing) is Unity's own upscaler: it
draws fewer pixels and rebuilds a sharp full-size image from several
frames. It works on any graphics card.

## How it works

- `QualitySwitch` creates itself before the first scene loads
  (`RuntimeInitializeOnLoadMethod`), so no scene contains it.
- **Built game, first start** (nothing saved yet): the level is the
  platform default, **Medium**. The screen is capped at 1080p tall,
  keeping the monitor's shape (`StartResolution.Cap`). A 4K monitor
  starts at 1920×1080. The level is saved, so later starts keep the choice
  and Unity keeps the window size itself.
- **F2**: next level (`QualityChoice.Next`; after High comes Low), shows
  "Quality: …" for two seconds, saves it in `PlayerPrefs` (on Windows the
  registry, key `NetRunner.Settings.QualityLevel`).
- **Launch option** `-net92-quality Low` (or `Medium`, `High`, any case):
  that level for this run only; nothing is saved and F2 does not save
  either. The benchmark script uses it ([[systems/performance]]).
- **F3**: FPS counter on or off, averaged over half a second
  (`FrameRateMeter`).
- **Resolution scaling needs the camera's permission**: HDRP only scales
  cameras with *Allow Dynamic Resolution*. The component switches it on
  for every camera that draws to the screen, when that camera starts
  rendering (`RenderPipelineManager.beginCameraRendering`), so neither the
  scene nor the player prefab changes. Cameras that draw into a texture
  (the CCTV feeds) keep their own size.
- **In the editor** nothing is loaded or saved and the resolution is not
  touched: editor Play mode, tests and the hospital validator use the
  editor's current level (High) and leave no saved setting.

## Known problems

- How much faster Low and Medium are on the test machine is not known yet.
  In the Linux editor at 1920×1080 the frame time hardly changed
  (spawn: High 31.8 ms, Medium 29.7, Low 27.7). Those frames were limited
  by the editor's own work and a forced read-back, not by the graphics
  card, so they say little about a built game. If Low still lags in the
  built game, the limit is the CPU (draw calls and culling of about 8
  million triangles), and resolution will not fix it.
- The note and the counter are drawn with `OnGUI`, a debug-style UI.
- Changing level rebuilds the HDRP pipeline, which takes a moment.

## Open questions

- A real options menu (resolution, quality, key bindings) once the game
  has a main menu.
