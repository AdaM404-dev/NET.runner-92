# NET.runner-92 · Northline Medical

Northline is a four-level hospital and quarantine campus for first-person exploration, stealth and hacking. This snapshot corresponds to the Phase 07 CCTV build.

## Play or edit

Since #31 (2026-11-10) the hospital lives in the root Unity project; the separate project `NETRunner92_Hospital` was retired.

1. Add the repository root in Unity Hub with **Unity 6000.6.2f1**.
2. Let Unity import the project with HDRP and the Input System.
3. Open [Hospital_Exploration.unity](../Assets/NETRunner92/Scenes/Hospital_Exploration.unity) and press Play, or use the [Phase 07 Windows archive](Builds/README.md#current-windows-build).
4. Use **E** to operate doors and the reception terminal. Aim at the amber-marked monitor from the staff side of the desk. Solve the three-cell signature puzzle, then press **1** and use the arrow keys to switch CCTV feeds on LATCH-07.

The first ascent alternates west, east and west across the four floors. The west lift can revisit floors the player has reached. A K-7 patrol follows its authored route on each level. [Controls and interaction](Documentation/interactive_systems.md) · [LATCH-07](Documentation/latch07_unity.md) · [CCTV](Documentation/cctv_unity.md)

## Project materials

| Path | Contents |
| --- | --- |
| [Assets/NETRunner92](../Assets/NETRunner92/) | The hospital's Unity assets and code, moved into the root project by #31 |
| [Hospital](Hospital/overview.md) and [Environment](Environment/) | Hospital shell, successive fit-outs, finished interior and quarantine grounds |
| [Cinematic](Cinematic/) | Editable Blender intro, 3D title and audio stems |
| [FirstPersonArms](FirstPersonArms/Documentation/fp_arms_overview.md) | LATCH-07 and arm model, animation, exports and textures |
| [Interface](Interface/README.md) | Earlier standalone interface prototype with its own demo data |
| [Renders](Renders/) and [Documentation](Documentation/state.md) | Visual references, test results and continuation notes |
| [Release asset index](Builds/README.md) | Windows builds, final intro and archived frame sequences |

[Repository integration and LFS notes](Documentation/github_integration.md)

Since #31 the root Unity project is the hospital project; the warehouse was removed. [Northline setup](Documentation/unity_setup.md) · [Intro music credit and license](Documentation/music_license.md)
