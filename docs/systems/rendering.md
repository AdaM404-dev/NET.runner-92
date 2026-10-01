# Rendering (URP)

**Purpose.** How the picture is made: the render pipeline, its settings, the
materials, and why the scenes look the way they do.

**Status.** All materials render correctly since the URP conversion
(2026-09-30). The image is dark and flat: no shadows, no post-processing, no
anti-aliasing, no baked light.

Verified 2026-09-30 on commit `df3a31d` from the settings files and
`bin/unity-inspect player environment`; frame statistics taken in Play mode.

## How the settings fit together

Unity's **Universal Render Pipeline (URP)**, version 17.6, draws the game.
Its settings are spread over a chain of assets; each one points to the next:

```
Project Settings > Quality         which quality level is active: "PC"
  └─ URP asset for that level      Assets/Settings/PC_RPAsset.asset
       ├─ renderer                 Assets/Settings/PC_Renderer.asset
       └─ default volume profile   Assets/Settings/SampleSceneProfile.asset
Project Settings > Graphics > URP  global volume profile: DefaultVolumeProfile.asset
Camera (per camera)                "Post Processing" and "Anti-aliasing" switches
Material (per surface)             shader "Universal Render Pipeline/Lit"
```

## Quality levels

| Level | URP asset | LOD bias | Used |
| --- | --- | ---: | --- |
| Mobile | `Mobile_RPAsset` + `Mobile_Renderer` | 1 | no platform uses it |
| **PC** | `PC_RPAsset` + `PC_Renderer` | 2 | default for Windows, Linux and macOS, and active in the editor |

`Project Settings > Graphics` has no pipeline of its own; the quality level
decides.

## `PC_RPAsset` (the settings that matter today)

| Setting | Value | Meaning |
| --- | --- | --- |
| HDR | on | colours brighter than white are kept (needed for glowing LEDs) |
| Anti-aliasing (MSAA) | off | |
| Render scale | 1 | full resolution |
| Additional lights | per pixel | every point light is calculated per pixel |
| Shadows | supported; 50 m distance, 4 cascades, soft | no light in `MainTest` actually casts shadows |
| SRP Batcher | on | cheaper drawing of many objects with the same shader |
| GPU Resident Drawer | requested | **not supported** by the editor on this Linux machine (it runs on OpenGL); the console warns about it on every Play |
| Volume profile | `SampleSceneProfile` | bloom (intensity 0.25), vignette (0.2), tonemapping (Neutral) |

`PC_Renderer` uses the **Forward+** path, which handles many lights without a
per-object limit, and has **Screen Space Ambient Occlusion** switched on
(intensity 0.4, radius 0.3).

## Post-processing is off

The player camera's **Post Processing** switch is off and `MainTest` has no
Volume object. So bloom, vignette and tonemapping from the profiles above are
never applied: bright LEDs do not glow and there is no exposure control.
Anti-aliasing on the camera is also off. See [[systems/camera]].

## Materials

All 52 materials use `Universal Render Pipeline/Lit`. They were converted
from the Built-in `Standard` shader on 2026-09-30 (before that, everything
rendered magenta).

| Folder | Count | Notes |
| --- | ---: | --- |
| `Assets/Materials/Environment/` | 27 | 8 glow (LEDs, screens, lamps); `DirtyGlass` is transparent |
| `Assets/Materials/NEXUS/` | 24 | textured; `Hair_DarkBrown` uses alpha cut-out |
| `Assets/Materials/Preview_Stage.mat` | 1 | the floor of the preview scene |

A new material must use a URP shader, or it shows magenta. Imported models
from Blender usually arrive with Built-in materials; see [[assets/handoff]].

## Why the warehouse looks dark and flat

| Cause | Where it is set | Owner |
| --- | --- | --- |
| Lights are modest (0.6 to 5) and none casts shadows | `Facility_Lighting` in `MainTest` | AdaM404 |
| Low ambient light and no sky | Lighting window, `MainTest` | AdaM404 |
| No post-processing: no exposure, tonemapping or bloom | camera and a scene Volume | Samuel (camera), AdaM404 (look) |
| No reflection probes: metal reflects one fixed neutral cubemap | `MainTest` | AdaM404 |
| No baked or bounced light | Lighting window, `MainTest` | AdaM404 |
| Fog darkens the distance | Lighting window, `MainTest` | AdaM404 |

## Frame statistics (rough)

Taken in Play mode in the editor on the development laptop (RTX 4050 Laptop
GPU, Ryzen 7 7435HS, Linux, OpenGL) with a small Game view (640 × 480). The
editor adds overhead; treat these as a first reference, not a budget.

| View at the start position | Triangles | Draw calls | SetPass calls | CPU time per frame |
| --- | ---: | ---: | ---: | ---: |
| Third person, looking into the hall | 0.94 M | 1,537 | 32 | 6 ms |
| First person, same spot | 0.87 M | 2,381 | 28 | 13 ms |

For comparison: the warehouse model has 1.08 million triangles and the
player's full-detail model 188,001.

## Known problems

- Camera post-processing and anti-aliasing are off.
- No shadows, probes or baked light (an art task).
- The Mobile quality level and its two assets are unused unless mobile is a target.
- GPU Resident Drawer is enabled but unsupported here, producing a console
  warning on every Play.

## Open questions

- Target platforms: PC only (Windows and Linux)?
- What look is wanted? A lighting pass needs reference images from the team.
