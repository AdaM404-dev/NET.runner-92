# Unity setup

Open the project folder `NETRunner92_Hospital` in Unity Hub and select `6000.6.2f1`. Open [Hospital_Exploration](../NETRunner92_Hospital/Assets/NETRunner92/Scenes/Hospital_Exploration.unity) and press Play. The project is independent of any earlier Unity projects. Unity scale is metres.

| Setting | Value |
| --- | --- |
| Editor | Unity 6000.6.2f1, the locally installed Unity 6.6 supported release |
| Pipeline | HDRP 17.6.0, from the Editor's shipped packages |
| Input | Input System 1.19.0, same package as the installed Editor's project template |
| Player | Windows x64, Direct3D 11, linear lighting, 1920 × 1080 default |
| Scene | `Assets/NETRunner92/Scenes/Hospital_Exploration.unity` |

Unity 6.6 is a Supported release. It is distinct from Unity's LTS releases. No LTS Editor was installed when Phase 5 began. Input System 1.17 failed to compile against Unity 6.6 after removal of older EntityId APIs. Version 1.19 compiled successfully. [Unity release support](https://unity.com/releases/unity-6/support) · [Unity HDRP manual](https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/high-definition-render-pipeline)

The scene is already serialized. The `NET.runner-92 → Build Hospital Exploration Scene` editor command reconstructs the imported scene from its bundled geometry data. Use that command for regeneration, as it replaces the generated scene and mesh assets.

The current Windows x64 development player uses the interactive hospital scene with LATCH-07 and reception CCTV. Keep the executable, data directory and supporting DLLs together. [Current player](../Builds/README.md#current-windows-build) · [Rebuild instructions](state.md)
