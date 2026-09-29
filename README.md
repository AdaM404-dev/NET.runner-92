# NET.runner-92

Unity 6.6 project (`6000.6.2f1`). Open this directory in Unity Hub.

## Included assets

- `Assets/Environment/Warehouse_NearFuture/`: the futuristic warehouse visual and collision FBX models, textures, and Unity metadata.
- `Assets/Characters/NEXUS/`: the refined NEXUS character, first-person arms, LOD models, textures, and Unity metadata.
- `Assets/Materials/NEXUS/`, `Assets/Prefabs/`, and `Assets/Animations/`: materials and prefabs for the imported models.
- `Assets/Scenes/MainTest.unity` and `Assets/Scenes/CharacterPreview.unity`: asset preview scenes. `SampleScene.unity` remains the startup scene.
- `Assets/Documentation/`: warehouse source notes and character asset attribution.

The preview controller uses Unity's legacy Input API, so the project enables both input backends. Unity-generated `Library`, `Temp`, `Logs`, and similar directories are excluded from Git. Source Blender files and packaged archives remain in the original asset folders outside this project; the repository contains their Unity-ready exports.

The imported preview materials were authored with the Built-in Standard shader. Convert them to URP materials in the Unity editor if they appear magenta in this URP project.
