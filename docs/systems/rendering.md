# Rendering (HDRP)

**Purpose.** How the picture is made: the render pipeline, its settings, the
materials, and what each platform needs.

**Status.** Since #31 (2026-11-10) the project uses the **High Definition
Render Pipeline (HDRP) 17.6**, taken over from the Northline hospital
project (PR #30). The Universal Render Pipeline (URP), its assets and the
warehouse's URP materials were removed. Settings were copied from the
Northline project unchanged (`GraphicsSettings`, `QualitySettings`,
`HDRPProjectSettings`, `ShaderGraphSettings`, `VFXManager`).

## How the settings fit together

```
Project Settings > Graphics        default pipeline: Assets/NETRunner92/Rendering/Northline_HDRP.asset
  └─ HDRP global settings          Assets/HDRPDefaultResources/HDRenderPipelineGlobalSettings.asset
       └─ default volume profile   Assets/HDRPDefaultResources/DefaultSettingsVolumeProfile.asset
Project Settings > Quality         three levels, Low / Medium / High, each with its own pipeline asset:
                                   Northline_HDRP_Low, _Medium, and Northline_HDRP (High); Medium is the default
Scene volumes                      fog per floor in Assets/NETRunner92/Atmosphere/Fog/*.asset,
                                   exposure in Assets/NETRunner92/Rendering/Overcast_Luminance.asset
Material (per surface)             HDRP Lit, 151 in Assets/NETRunner92/Environment/Materials
```

Since playtest 2 (2026-11-10) the player switches levels with F2; Low and
Medium render at 50% and 67% and upscale with STP, and use cheaper
reflections, ambient occlusion, contact shadows and shadow maps. Details
and the exact values: [[systems/quality-settings]]. The game still targets
**PC only** (#15).

## Platforms and graphics APIs

HDRP needs Direct3D 11/12 on Windows and **Vulkan on Linux**; it does not
run on OpenGL. On Linux the editor started on OpenGL by default during #31
and HDRP drew nothing ("Platform StandaloneLinux64 with graphics API
OpenGLCore is not supported with HDRP"). See the work log in
[`agent.md`](../../agent.md) for how the Linux editor is set to Vulkan.

## Materials and art

- New art targets **HDRP Lit**. HDRP mask maps pack metallic (R), ambient
  occlusion (G), detail mask (B) and smoothness (A).
- The NEXUS character's materials (`Assets/Materials/NEXUS`) are still
  URP Lit and render magenta. NEXUS is not used by the hospital; whether
  to convert or delete it is AdaM404's call, asked on #31.
- The K-7 robot in the hospital already has HDRP materials
  ([[systems/enemy-k7]]).

## Atmosphere

Storm, rain, fog and flickering lights come from the hospital's
atmosphere system: [atmosphere system](../../Northline/Documentation/atmosphere_system.md),
[visual upgrade](../../Northline/Documentation/visual_upgrade.md),
[performance report](../../Northline/Documentation/performance_report.md).

## Known problems

- HDRP is heavier than URP. The first Windows playtest build lagged on the
  test machine; playtest 2 adds the quality levels. Frame time in a built
  game has not been measured on Linux or Windows yet.
- `HospitalOptimization` (editor) puts small detail objects on layer 8 so
  the player camera can cull them past 34 m.
