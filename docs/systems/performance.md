# Performance

## Purpose

What makes the hospital expensive to draw, how to measure it, and the plan
to reach the target of [#36](https://github.com/AdaM404-dev/NET.runner-92/issues/36):
**60 FPS at 1920×1080 on Medium on the Windows test machine**.

## Status

2026-11-10: measuring. Alpha test 1 lagged on the test machine; alpha test
2 added quality levels ([[systems/quality-settings]]); alpha test 3 is the
measurement build. The optimizations come after #31 PR B (decided on #36).

## What the scene contains

Counted in `Assets/NETRunner92/Scenes/Hospital_Exploration.unity` on
2026-11-10 (scene YAML and settings files):

| What | Value | Why it matters |
| --- | --- | --- |
| Mesh renderers | 7,270 | each is at least one draw call; draw calls cost CPU time every frame |
| Objects marked static | 0 of 9,706 | no static batching, no baked occlusion culling, no baked lighting |
| LOD groups on the hospital | 0 | distant rooms are drawn in full detail; Northline measured about 8 million triangles per frame at the entrance |
| Lights | 268, all real-time: 249 point, 17 spot, 2 directional | only 5 cast shadows; nothing is baked; lights also feed the volumetric fog |
| Reflection probes | 0 | |
| Unity 6 GPU Resident Drawer | off | made for scenes with many renderers |
| Live CCTV cameras | rendered only when due (`SecurityCctv`) | already throttled |
| Frame timing stats | **on since alpha test 3** | builds can report CPU and GPU time per frame |

Northline's own numbers (RTX 3060 Ti, 1080p, before the doors, lift,
K-7s and CCTV were added) are in
[performance report](../../Northline/Documentation/performance_report.md).

## How to measure

The game has two benchmark modes built in, from the Northline project
(`Assets/NETRunner92/Core/PerformanceCapture.cs`,
`RoutePerformanceCapture.cs`). They run only when the game is started with
their launch option, then quit.

| Option | Does | Writes |
| --- | --- | --- |
| `-net92-benchmark` | 5 fixed views (south checkpoint, reception, ward, surgery, research); 4.5 s each after a warm-up | one CSV: graphics card, resolution and quality level in the first line, then per view: samples, average FPS, P95 frame ms, P95 GPU ms, draw calls, triangles |
| `-net92-route-benchmark` | walks the whole route from the forecourt to level 4 and back (about 3 minutes) | a folder with `route_performance.csv` (per segment), `route_frames.csv` (every frame), `route_audio.csv`, `route_checks.txt` |
| `-net92-output <path>` | where to write (a file for the views, a folder for the route) | default: Unity's `persistentDataPath` |
| `-net92-quality Low\|Medium\|High` | quality level for this run only, not saved ([[systems/quality-settings]]) | |

**On Windows, the easy way:** `run-benchmarks.bat` (source:
`tools/benchmark/run-benchmarks.bat`, shipped in the alpha zips from
alpha test 3). Put it next to `NET.runner-92.exe` and double-click it. It
runs the views on Low, Medium and High, then the route on Medium, and
writes everything into a `benchmark` folder, plus `system.txt` with the
processor, graphics card, driver and memory, and nothing that names the
computer or its user. About 6 minutes; do not touch the mouse or keyboard.

Notes:

- "P95" is the 95th percentile: 95% of frames were faster. It shows
  stutter that an average hides. 16.67 ms is 60 FPS.
- The draw-call column has stayed empty in release builds (Northline saw
  the same); triangles and GPU time are recorded.
- The benchmark turns VSync off and does not cap the frame rate.

## Plan (#36)

1. Measurement build, alpha test 3, and the CSVs from the test machine
   and Samuel's Linux laptop.
2. #31 PR B.
3. One PR per lever, each with before and after CSVs:
   - GPU Resident Drawer and GPU occlusion culling (Unity 6)
   - static flags on the building geometry
   - light fade distances; fewer lights in the volumetric fog
   - simpler distant geometry (mesh LOD)
4. Retune the quality levels; alpha test 4.

## Results

None yet from the test machine.
