# Phase 5 performance report

## Environment and method

The development player uses Windows x64, HDRP 17.6, Direct3D 11 and the Ultra quality slot with scene-specific settings. Hardware was an Intel Core i5-9600K and NVIDIA RTX 3060 Ti. The game reported 8,024 MB of graphics memory. Runs request 1920 × 1080 with VSync disabled. The stationary benchmark records five repeatable views after warm-up. The traversal benchmark records the same game camera while the CharacterController follows a timed route. Both use Unity ProfilerRecorder counters.

## First Windows build

| View | Average FPS | P95 frame ms | P95 GPU ms |
| --- | ---: | ---: | ---: |
| South checkpoint | 19.5 | 33.29 | 22.48 |
| Reception | 75.4 | 16.85 | 12.82 |
| Ward | 63.4 | 21.09 | 17.80 |
| Surgery | 78.3 | 16.70 | 13.12 |
| Research | 109.8 | 12.49 | 11.19 |

The south view also reported about 12.74 million triangles per frame across passes. The first run's draw-call counter returned zero and was unusable. The outdoor average was affected by startup frame time, so the revised benchmark waits longer before the first sample. [Baseline CSV](../Renders/Phase_05_Unity/performance_baseline.csv)

## Optimized build

The optimized scene caps the HDRP shadow distance at 42 m and uses two cascades. It switches fine or indoor furnishing renderers out of shadow casting, culls 1,293 fine renderers past 34 m in the player camera, and lowers the volumetric fog sampling tier. The scene contains the full source geometry and collision. The optimized Windows build completed successfully. The optimized run used a longer warm-up and then sampled 4.5 seconds at each position.


| View | Samples | Average FPS | P95 frame ms | P95 GPU ms | Mean triangles |
| --- | ---: | ---: | ---: | ---: | ---: |
| South checkpoint | 318 | 70.6 | 18.09 | 12.94 | 8,185,862 |
| Reception | 472 | 104.7 | 12.64 | 9.39 | 4,497,910 |
| Ward | 396 | 87.9 | 14.99 | 12.10 | 6,258,309 |
| Surgery | 464 | 103.0 | 11.75 | 9.74 | 4,864,458 |
| Research | 614 | 136.5 | 10.15 | 8.11 | 2,532,486 |

All five sampled views averaged more than 60 FPS. The south checkpoint exceeded the 16.67 ms target at the 95th percentile in this build. The GPU time and triangle counter decreased there after optimization. The warm-up change also makes the two average FPS values unsuitable as a direct speedup ratio. Draw calls were unavailable from ProfilerRecorder in the optimized run. [Optimized CSV](../Renders/Phase_05_Unity/performance.csv)

## Continuous traversal baseline

The same 1920 × 1080 Direct3D 11 player walked from the south forecourt through reception, up to Level 4 and back outside without teleporting after the initial spawn. The test measured 640.8 m in 184.91 seconds and 28,809 rendered frames. It used the same stair landing waypoints as the Play Mode suite. [Baseline route results](../Renders/Phase_05_Unity/Traversal/Baseline/route_performance.csv) · [Baseline frame trace](../Renders/Phase_05_Unity/Traversal/Baseline/route_frames.csv)

| Segment | Average FPS | P95 frame ms | P99 frame ms |
| --- | ---: | ---: | ---: |
| Full route | 155.8 | 12.07 | 15.96 |
| South forecourt towards hospital | 71.6 | 19.30 | 24.90 |
| Level 2 return concourse | 119.4 | 16.83 | 28.51 |

The entry forecourt and the Level 2 return were the two segments with P95 above 16.67 ms. On the Level 2 return, the worst frames clustered near the eastern end. CPU and GPU timing counters are recorded separately, and single-frame correlation may be offset by the graphics pipeline. The run does not establish the source of every spike.

The follow-up build limits HDRP shadows to 36 m and avoids precipitation probes while the listener is deep inside the building on any floor. The 34 m detail layer distance remains. Removed the spherical camera-culling flag that Unity warned was unavailable under HDRP. [Unity API reference](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/camera/layercullspherical)

## Tuned traversal build

| Segment | Average FPS | P95 frame ms | P99 frame ms | Frames over 16.67 ms |
| --- | ---: | ---: | ---: | ---: |
| Full route | 150.8 | 12.18 | 15.97 | 224 |
| South forecourt towards hospital | 76.2 | 16.59 | 22.77 | 13 |
| Level 2 return concourse | 134.5 | 12.39 | 14.36 | 8 |

The repeat covered 640.8 m in 184.91 s and recorded 27,883 frames. All 18 timed segments met the 16.67 ms P95 target in this run. Full-route average FPS fell from 155.8 to 150.8 while the two previously over-target segments improved. The Level 4 approach had 73 frames over 16.67 ms and the L3→L4 stair segment had a 24.51 ms P99. These are individual runs per build, so the comparison leaves room for run-to-run variation and slower individual frames. [Tuned route results](../Renders/Phase_05_Unity/Traversal/route_performance.csv) · [Tuned frame trace](../Renders/Phase_05_Unity/Traversal/route_frames.csv)

| Stationary view | Average FPS | Previous P95 frame ms | Tuned P95 frame ms | Tuned P95 GPU ms |
| --- | ---: | ---: | ---: | ---: |
| South checkpoint | 72.5 | 18.09 | 16.36 | 12.99 |
| Reception | 107.3 | 12.64 | 11.12 | 9.24 |
| Ward | 88.7 | 14.99 | 13.78 | 11.86 |
| Surgery | 104.7 | 11.75 | 11.14 | 9.79 |
| Research | 144.1 | 10.15 | 8.17 | 7.60 |

All five stationary views met the 16.67 ms P95 target. The south view averaged 7.95 million triangles across passes, down from 8.19 million in the previous stationary run. Its P95 GPU time was effectively flat, so the frame-time difference alone cannot identify the bottleneck. [Tuned stationary CSV](../Renders/Phase_05_Unity/Traversal/stationary_performance.csv)

These measurements are from the earlier traversal player, before interactive doors, the lift and K-7 patrols. The interaction tests report functional results separately. [Interactive validation](testing_report.md#interactive-build-validation)
