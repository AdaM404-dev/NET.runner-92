# NET.runner-92

Unity 6.6 project (`6000.6.2f1`). Open this directory in Unity Hub.

Development status and agent handoff notes are tracked in [agent.md](agent.md).

## Import into Unity

### Open the complete project

1. Clone the repository, or download its ZIP from GitHub and extract it:

   ```bash
   git clone https://github.com/AdaM404-dev/NET.runner-92-2026-09-29_22-29-57.git
   ```

2. In Unity Hub, add the cloned or extracted project folder. Select the folder that directly contains `Assets`, `Packages`, and `ProjectSettings`.
3. Open it with Unity **6000.6.2f1**. Let Unity download the packages and finish importing the assets on first launch. The generated `Library` folder does not need to be downloaded from GitHub.
4. In the Project window, open `Assets/Scenes/MainTest.unity` and press **Play** to try the warehouse and NEXUS player. Open `Assets/Scenes/CharacterPreview.unity` for the stationary character preview. See [tutorial.md](tutorial.md) for the movement implementation guide.

### Bring the assets into another Unity project

Copy `Assets/Characters`, `Assets/Environment`, `Assets/Materials`, `Assets/Animations`, `Assets/Prefabs`, and `Assets/Scripts` into the other project's `Assets` folder. Include their `.meta` files and keep the same relative paths so prefab and material references retain their Unity GUIDs. Copy `Assets/Documentation` for attribution and the two NEXUS scenes from `Assets/Scenes` if you also want the previews. Check for folder or filename conflicts before copying into an existing project.

For the playable character, drag `Assets/Prefabs/NEXUS_Player.prefab` into a scene with a collidable floor. The model-only prefab is `Assets/Prefabs/NEXUS_Character.prefab`. The player script uses Unity's legacy `Input` API, so set **Active Input Handling** to **Both** in Player Settings if the destination project uses the new Input System. The materials were authored with the Built-in Standard shader; convert them for URP if they appear magenta.

## Included assets

- `Assets/Environment/Warehouse_NearFuture/`: the futuristic warehouse visual and collision FBX models, textures, and Unity metadata.
- `Assets/Characters/NEXUS/`: the refined NEXUS character, first-person arms, LOD models, textures, and Unity metadata.
- `Assets/Materials/NEXUS/`, `Assets/Prefabs/`, and `Assets/Animations/`: materials and prefabs for the imported models.
- `Assets/Scenes/MainTest.unity` and `Assets/Scenes/CharacterPreview.unity`: asset preview scenes. `SampleScene.unity` remains the startup scene.
- `Assets/Documentation/`: warehouse source notes and character asset attribution.

The preview controller uses Unity's legacy Input API, so the project enables both input backends. Unity-generated `Library`, `Temp`, `Logs`, and similar directories are excluded from Git. Source Blender files and packaged archives remain in the original asset folders outside this project; the repository contains their Unity-ready exports.

The imported preview materials were authored with the Built-in Standard shader. Convert them to URP materials in the Unity editor if they appear magenta in this URP project.
