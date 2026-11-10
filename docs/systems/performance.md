# Performance

## Purpose

What makes the hospital expensive to draw, how to measure it, and the plan
to reach the target of [#36](https://github.com/AdaM404-dev/NET.runner-92/issues/36):
**60 FPS at 1920×1080 on Medium on the Windows test machine**.

## Status

2026-11-10: measured. Alpha test 1 lagged on the test machine; alpha test
2 added quality levels ([[systems/quality-settings]]); alpha test 3 is the
measurement build, and its results are below ([#38](https://github.com/AdaM404-dev/NET.runner-92/issues/38)).
The test machine has integrated Intel graphics (8–14 FPS); Samuel's laptop
with an RTX 4050 already runs Medium at 119–179 FPS. The target is being
reconsidered on #36. The
optimizations come after #31 PR B (decided on #36).

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

### Alpha test 3, Windows test machine (2026-11-10, [#38](https://github.com/AdaM404-dev/NET.runner-92/issues/38))

Intel Core i5-10500, **Intel UHD Graphics 630** (integrated, driver
31.0.101.2135), 16 GB, Direct3D 11, 1920×1080.

Fixed views, P95 GPU time in ms (FPS in brackets):

| View | Triangles | Low | Medium | High |
| --- | --- | --- | --- | --- |
| South checkpoint | 8.1 M | 101 (9.8) | 131 (7.7) | 190 (5.3) |
| Reception | 4.6 M | 84 (12.5) | 107 (9.3) | 163 (6.2) |
| Ward | 6.5 M | 94 (10.6) | 123 (8.2) | 177 (5.7) |
| Surgery | 5.3 M | 84 (11.8) | 111 (9.0) | 161 (6.2) |
| Research | 2.6 M | 69 (14.4) | 132 (1.3)* | 154 (6.5) |

\* One frame of about 30 s fell inside the Medium research sample, which
pulls its average down; its P95 is also out of line with Low and High.
Treat that cell as a glitch.

Route on Medium (forecourt to the L2→L3 stair, 716 frames): 10.6 FPS on
average, median frame 92 ms, of which the GPU took 90 ms and the main
thread 11 ms. Every frame was over 16.7 ms.

What the numbers say:

- **The graphics chip is the limit, everywhere.** GPU time equals frame
  time; the CPU's main thread needs 11–17 ms. Draw calls are not the
  problem on this machine.
- **Geometry costs about 6 ms per million triangles** (Low: 53 ms + 5.9 ms
  per million fits all five views within 4 ms; Medium: 76 ms + 6.7 ms per
  million). At the entrance that is about 48 of 101 ms on Low. Occlusion
  culling and LOD target this part.
- **The rest, about 50–75 ms, does not depend on the view**: lighting of
  268 lights, volumetric fog, shadows, ambient occlusion, post effects.
  Fewer pixels help (High → Medium saves about 60 ms) but do not remove it.
- **60 FPS needs a frame six times faster than Low today.** That is not
  reachable on this chip with HDRP and this scene; 30 FPS on Low might
  be, once geometry is culled and the fixed costs are cut.
- The route stopped at the L2→L3 stair: the walker stood about 1 m short
  of the stair's exit waypoint until its time limit ran out. The laptop
  run below stopped the same way at the L1→L2 stair at 180 FPS, so it is
  not the frame rate; something holds the walker at the stair core
  (x = ±32). Not yet investigated; it comes up again when #31 PR B turns
  the route checks into tests.

### Alpha test 3 build, Samuel's laptop (2026-11-10)

AMD Ryzen 7 7435HS, **NVIDIA GeForce RTX 4050 Laptop GPU** (driver
615.78.08), 32 GB, Linux, Vulkan, 1920×1080 full screen. Same build and
options as the Windows run.

Fixed views, average FPS (P95 GPU time in ms in brackets):

| View | Low | Medium | High |
| --- | --- | --- | --- |
| South checkpoint | 130 (6.4) | 119 (8.3) | 96 (10.7) |
| Reception | 177 (5.1) | 153 (6.6) | 114 (9.2) |
| Ward | 139 (7.0) | 119 (9.0) | 100 (10.1) |
| Surgery | 165 (5.8) | 142 (7.2) | 113 (9.0) |
| Research | 207 (4.3) | 179 (5.7) | 131 (7.7) |

Route on Medium (forecourt to the L1→L2 stair, 5,974 frames): 178 FPS on
average, P95 frame 8.1 ms, P95 main thread 4.8 ms; no frame over 16.7 ms.

- **On a separate graphics card the 60 FPS target is already met**, on
  every level, with room to spare: the slowest view on High is 96 FPS.
- The same scene is about 13–15 times slower on the UHD 630 (Medium,
  South checkpoint: 8.3 ms against 131 ms of GPU time). The gap between
  the two machines, not the scene alone, is the problem.
