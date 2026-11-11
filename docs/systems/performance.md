# Performance

## Purpose

What makes the hospital expensive to draw, how to measure it, and the plan
to reach the targets of [#36](https://github.com/AdaM404-dev/NET.runner-92/issues/36), set by Samuel on 2026-11-10
after the first measurements:

- **30 FPS at 1920×1080 on Low on the Windows test machine** (Intel UHD
  Graphics 630, integrated): the minimum hardware we support, and the
  goal of the optimizations.
- **60 FPS at 1920×1080 on Medium on a separate graphics card**: already
  met (RTX 4050 laptop: 119–179 FPS); it must not get worse.

## Status

2026-11-10: measured. Alpha test 1 lagged on the test machine; alpha test
2 added quality levels ([[systems/quality-settings]]); alpha test 3 is the
measurement build, and its results are below ([#38](https://github.com/AdaM404-dev/NET.runner-92/issues/38)).
The test machine has integrated Intel graphics (8–14 FPS); Samuel's laptop
with an RTX 4050 already runs Medium at 119–179 FPS. The first target (60
FPS on Medium on the test machine) was replaced by the two above. The
optimizations come after #31 PR B (decided on #36), which is done.

2026-11-11: round 1 of the optimizations started (Plan below). The
benchmark can now switch single features off (`-net92-without`), the
route walks the whole way again, and the laptop's breakdown is under
Results; the test machine follows with alpha test 4.

## What the scene contains

Counted in `Assets/NETRunner92/Scenes/Hospital_Exploration.unity` on
2026-11-10 (scene YAML and settings files):

| What | Value | Why it matters |
| --- | --- | --- |
| Mesh renderers | 7,270 | each is at least one draw call; draw calls cost CPU time every frame |
| Objects marked static | 0 of 9,706; **since 2026-11-11, 7,040** for occlusion culling only (6,837 occluders, 203 see-through occludees) | baked occlusion culling since round 1 ([[systems/level-hospital]]); still no static batching or baked lighting |
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
(`Assets/NETRunner/Diagnostics/Scripts/PerformanceCapture.cs`,
`RoutePerformanceCapture.cs`). They run only when the game is started with
their launch option, then quit.

| Option | Does | Writes |
| --- | --- | --- |
| `-net92-benchmark` | 5 fixed views (south checkpoint, reception, ward, surgery, research); 4.5 s each after a warm-up | one CSV: graphics card, resolution and quality level in the first line, then per view: samples, average FPS, P95 frame ms, P95 GPU ms, draw calls, triangles |
| `-net92-route-benchmark` | walks the whole route from the forecourt to level 4 and back (about 3 minutes) | a folder with `route_performance.csv` (per segment), `route_frames.csv` (every frame), `route_audio.csv`, `route_checks.txt` |
| `-net92-output <path>` | where to write (a file for the views, a folder for the route) | default: Unity's `persistentDataPath` |
| `-net92-quality Low\|Medium\|High` | quality level for this run only, not saved ([[systems/quality-settings]]) | |
| `-net92-without <names>` | switches rendering features off for this run only, so the run's frame times show what they cost; names separated by commas (below). Since 2026-11-11 | the CSV's first line says which, after `without:` |

The features `-net92-without` can switch off (`RenderFeatureSwitch`, names
in `Core/Scripts/RenderFeatureList.cs`):

| Name | Switches off | So the run shows |
| --- | --- | --- |
| `occlusion-culling` | the camera's use of the baked occlusion data | what occlusion culling saves |
| `volumetric-fog` | the volumetric fog (HDRP frame setting *Volumetrics*), and with it every light shining into it | the fog's whole cost |
| `fog-lights` | the point and spot lights' part of the fog; the fog and the moonlight in it stay | what 266 lights in the fog cost |
| `shadows` | every shadow map | the shadows' cost |
| `ambient-occlusion` | screen-space ambient occlusion | its cost |
| `distant-lights` | point and spot lights more than 25 m from the camera (their fade distance) | what a light fade distance would save |
| `transparent-objects` | everything see-through: glass, rain, dust, steam | their cost |
| `decals` | decals, and the depth pass HDRP draws first for every surface that can receive one; the hospital has no decal projectors | what that depth pass costs |

**On Windows, the easy way:** `run-benchmarks.bat` (source:
`tools/benchmark/run-benchmarks.bat`, shipped in the alpha zips from
alpha test 3). Put it next to `NET.runner-92.exe` and double-click it. It
runs the views on Low, Medium and High, then the views on Low once for
each feature above switched off, then the route on Low, and writes
everything into a `benchmark` folder (`views-low.csv`,
`views-low-without-shadows.csv`, …, `route-low/`), plus `system.txt` with
the processor, graphics card, driver and memory, and nothing that names
the computer or its user. About 15 minutes; do not touch the mouse or
keyboard. (Alpha test 3's version ran only the views and the route on
Medium, about 6 minutes.)

**On Linux:** `tools/benchmark/run-benchmarks.sh [game folder] [output
folder]` runs the same sequence on the Linux build (`bin/unity-build
linux`; defaults `Builds/Linux` and `Builds/Linux/benchmark`), with the
same file names, plus each run's log in `logs/`. Switch the desktop's
screensaver and screen lock off for the run (on Omarchy: stay awake):
the route takes about 4 minutes without a key press, and a screensaver
that takes the screen stalls the game. A stall shows as one long frame in
the results; the route counts at most 0.1 s of each frame towards a leg's
time limit, so a stall is not taken for the walker being stuck (until
2026-11-11 it was: three laptop runs failed that way).

Notes:

- "P95" is the 95th percentile: 95% of frames were faster. It shows
  stutter that an average hides. 16.67 ms is 60 FPS.
- The draw-call column has stayed empty in release builds (Northline saw
  the same); triangles and GPU time are recorded.
- The benchmark turns VSync off and does not cap the frame rate.

## Plan (#36)

1. Measurement build, alpha test 3, and the CSVs from the test machine
   and Samuel's Linux laptop. Done.
2. #31 PR B. Done.
3. One PR per lever, each with before and after CSVs:
   - **Round 1 (Samuel, 2026-11-11):** baked occlusion culling (static
     flags on the building geometry), plus the `-net92-without` runs, so
     one run on the test machine shows what each fixed cost costs there.
     Measured on Samuel's laptop now, on the test machine later (alpha
     test 4).
   - Round 2: the fixed costs, chosen by that breakdown: light fade
     distances, fewer lights in the volumetric fog, cheaper shadows.
   - GPU Resident Drawer and GPU occlusion culling (Unity 6), only if baked
     occlusion falls short; it ignores the 34 m detail-layer distance.
   - simpler distant geometry (mesh LOD)
4. Retune the quality levels; alpha test 4.

## Results

### Round 1, lever 1: baked occlusion culling, Samuel's laptop (2026-11-11)

Linux build of `perf/occlusion-culling` (the baseline above plus the baked
occlusion data), same laptop and script.

Fixed views, average FPS before → after, and million triangles per frame
before → after:

| View | Low | Medium | High | Triangles |
| --- | --- | --- | --- | --- |
| South checkpoint | 123 → 181 | 117 → 150 | 98 → 113 | 8.1 → 3.3 M |
| Reception | 166 → 268 | 155 → 204 | 117 → 143 | 4.6 → 1.1 M |
| Ward | 138 → 209 | 122 → 167 | 102 → 126 | 6.5 → 1.2 M |
| Surgery | 161 → 245 | 142 → 186 | 114 → 124 | 5.2 → 1.2 M |
| Research | 200 → 296 | 179 → 214 | 133 → 149 | 2.7 → 0.6 M |

The same build with occlusion culling switched off
(`-net92-without occlusion-culling`) draws the old triangle counts again.
Against it, occlusion culling cuts the P95 GPU time on Low by 19–29% per
view (south checkpoint 6.92 → 5.56 ms, ward 7.36 → 5.23 ms) and the P95
frame time by 29–39%: it also saves processor time, as fewer objects are
sent to the graphics card.

The route on Low, with and without occlusion culling back to back: on the
seven segments both runs walked (forecourt to the L2→L3 stair) 225–340
FPS against 126–250, P95 main thread 2.5–4.1 ms against 3.8–6.6 ms, no
frame over 16.7 ms against 13. A whole route with occlusion culling
averaged 294 FPS (P95 frame 4.5 ms, P95 main thread 2.9 ms), counting an
8 s freeze made on purpose to test the stall fix. An earlier full run
showed a main thread of 8–11 ms for 90 s whatever the place, then 3 ms:
other load on the laptop, not the culling.

**What it should mean for the UHD 630** (an estimate, not a measurement):
alpha test 3 fitted Low there as 53 ms plus 5.9 ms per million triangles.
With the new counts that is 72 ms at the south checkpoint and 56–60 ms in
the other views, about 14–18 FPS instead of 10–14. Geometry is now the
smaller part of the frame; the fixed 53 ms is what stands between Low and
30 FPS (33 ms), which is round 2.

The breakdown in this build (`-net92-without`, Low, P95 GPU time):

- The volumetric fog costs 33%, of which the lights in it 29%, as before.
- Shadows 7%; their maps hold 0.3–0.6 M triangles in every view, because
  shadow maps are not occlusion-culled.
- `decals` (new): without it the depth pass that HDRP draws for every
  surface able to receive a decal goes away: 8–33% fewer triangles per
  view (south checkpoint 3.3 → 2.4 M). The GPU time changed within the
  noise here; on the UHD 630 that is roughly 1–5 ms.
- Ambient occlusion, distant lights and see-through objects were within
  the noise, and the runs late in the sequence were slower overall (other
  load on the laptop), so `distant-lights` even came out slower. The test
  machine's run will be the one to rank them.

### Round 1 baseline, Samuel's laptop (2026-11-11)

Linux build of `perf/benchmark-variants` (`main` at `7b788f8` plus the
benchmark changes; nothing optimised yet), RTX 4050 Laptop GPU, Vulkan,
1920×1080 full screen, `tools/benchmark/run-benchmarks.sh`.

Fixed views, average FPS (P95 GPU ms; million triangles per frame):

| View | Low | Medium | High |
| --- | --- | --- | --- |
| South checkpoint | 123 (7.2; 8.1 M) | 117 (9.2; 8.1 M) | 98 (10.6; 8.1 M) |
| Reception | 166 (5.4; 4.6 M) | 155 (6.5; 4.6 M) | 117 (8.9; 4.6 M) |
| Ward | 138 (8.1; 6.5 M) | 122 (8.5; 6.5 M) | 102 (10.2; 6.5 M) |
| Surgery | 161 (6.1; 5.2 M) | 142 (7.3; 5.2 M) | 114 (9.2; 5.2 M) |
| Research | 200 (4.3; 2.7 M) | 179 (5.9; 2.7 M) | 133 (7.7; 2.7 M) |

On Low, P95 GPU ms per view with one feature switched off
(`-net92-without`), and the change against all on:

| Switched off | South checkpoint | Reception | Ward | Surgery | Research | Mean |
| --- | --- | --- | --- | --- | --- | --- |
| nothing (all on) | 7.23 | 5.43 | 8.06 | 6.10 | 4.33 | |
| `occlusion-culling` (no data yet: a repeat) | 6.49 (−0.74) | 5.06 (−0.37) | 7.24 (−0.82) | 5.78 (−0.32) | 4.21 (−0.12) | −7% |
| `volumetric-fog` | 4.90 (−2.33) | 3.94 (−1.49) | 4.30 (−3.76) | 4.23 (−1.87) | 3.32 (−1.01) | −32% |
| `fog-lights` | 5.01 (−2.22) | 4.12 (−1.31) | 4.47 (−3.59) | 4.38 (−1.72) | 3.47 (−0.86) | −29% |
| `distant-lights` | 5.17 (−2.06) | 4.53 (−0.90) | 5.13 (−2.93) | 4.93 (−1.17) | 3.95 (−0.38) | −22% |
| `shadows` | 6.17 (−1.06) | 4.77 (−0.66) | 6.68 (−1.38) | 5.46 (−0.64) | 4.08 (−0.25) | −12% |
| `ambient-occlusion` | 6.49 (−0.74) | 4.99 (−0.44) | 8.18 (+0.12) | 5.79 (−0.31) | 4.13 (−0.20) | −5% |
| `transparent-objects` | 6.30 (−0.93) | 4.96 (−0.47) | 9.69 (+1.63) | 5.72 (−0.38) | 4.12 (−0.21) | −2% |

What the numbers say:

- **This build has no occlusion data yet**, so the `occlusion-culling`
  run repeats the all-on run. Its spread, up to 0.8 ms or about 10%, is
  this laptop's run-to-run noise; smaller differences say nothing.
- **The lights in the fog cost the most.** Without the volumetric fog the
  GPU needs 32% less time, and without only the 266 point and spot lights'
  part of it 29% less: the fog itself is cheap, the lights in it are not.
  Fading lights out beyond 25 m saves 22%, most of it in the fog too.
- **Shadows: 12%.** They account for only 0.5 M of the 8.1 M triangles at
  the south checkpoint, so the triangles are in the camera's own passes.
- Ambient occlusion and see-through objects are within the noise here.
- How these shares carry over to the UHD 630 is not known; its run of the
  same breakdown (alpha test 4) will tell.

Route on Low: the walker climbed to L4 and came back down to the L3→L2
stair (477 m of 641 m, 29,323 frames: 213 FPS on average, P95 frame
7.5 ms, P95 GPU 6.5 ms), where the desktop's screensaver took the screen
and stalled the game long enough to fail the route; see "On Linux" above.
The same build walked the whole route (641.1 m, 12 of 12 route checks)
when started without a window (`-batchmode`), which confirms the stair
fix; frame times from such a run are not measurements.

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
  (x = ±32). **Found and fixed on 2026-11-11:** the walker asks a door to
  open 1.4 m ahead and keeps walking. It reaches a hinged leaf about 0.4 s
  later, while the leaf needs 0.53 s to swing, so the swinging leaf shoved
  it about 1.1 m aside, into the corner between the open leaf and the wall
  (frame trace of the L1→L2 stair on the laptop). The route now opens
  doors at once, as the hospital checks always did. A player who walks
  into a door while it swings could be shoved the same way; noted for #44.

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

- **On a separate graphics card 60 FPS is already met**, on
  every level, with room to spare: the slowest view on High is 96 FPS.
- The same scene is about 13–15 times slower on the UHD 630 (Medium,
  South checkpoint: 8.3 ms against 131 ms of GPU time). The gap between
  the two machines, not the scene alone, is the problem.
