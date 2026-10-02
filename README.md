# NET.runner-92

Unity 6.6 project (`6000.6.2f1`). Open this directory in Unity Hub.

Development status and agent handoff notes are tracked in [agent.md](agent.md).

## Import into Unity

### Open the complete project

1. Clone the repository, or download its ZIP from GitHub and extract it:

   ```bash
   git clone https://github.com/AdaM404-dev/NET.runner-92.git
   ```

2. In Unity Hub, add the cloned or extracted project folder. Select the folder that directly contains `Assets`, `Packages`, and `ProjectSettings`.
3. Open it with Unity **6000.6.2f1**. Let Unity download the packages and finish importing the assets on first launch. The generated `Library` folder does not need to be downloaded from GitHub.
4. In the Project window, open `Assets/Scenes/MainTest.unity` and press **Play** to try the warehouse and NEXUS player. Open `Assets/Scenes/CharacterPreview.unity` for the stationary character preview. See [docs/systems/player-movement.md](docs/systems/player-movement.md) for the movement implementation guide.

### Bring the assets into another Unity project

Copy `Assets/Characters`, `Assets/Environment`, `Assets/Materials`, `Assets/Animations`, `Assets/Prefabs`, and `Assets/Scripts` into the other project's `Assets` folder. Include their `.meta` files and keep the same relative paths so prefab and material references retain their Unity GUIDs. Copy `Assets/Documentation` for attribution and the two NEXUS scenes from `Assets/Scenes` if you also want the previews. Check for folder or filename conflicts before copying into an existing project.

For the playable character, drag `Assets/Prefabs/NEXUS_Player.prefab` into a scene with a collidable floor. The model-only prefab is `Assets/Prefabs/NEXUS_Character.prefab`. The player script uses Unity's legacy `Input` API, so set **Active Input Handling** to **Both** in Player Settings if the destination project uses the new Input System. The materials were authored with the Built-in Standard shader; convert them for URP if they appear magenta.

## Included assets

- `ArtSource/K7_Industrial_Robot/`: the complete K7 enemy robot package, including its Blender source, FBX LODs, 2K/4K textures, Unity setup helpers, previews, authoring scripts and validation reports. See its [README](ArtSource/K7_Industrial_Robot/README.md).
- `ArtSource/Warehouse_NearFuture/`: editable Blender source, original building, scripts, textures, exports and review images. The 2026-10-02 revision closes all 119 doors; see its [review](ArtSource/Warehouse_NearFuture/Project_review.md) and [guide](ArtSource/Warehouse_NearFuture/README.md).
- `Assets/Environment/Warehouse_NearFuture/`: the futuristic warehouse visual and collision FBX models, textures, and Unity metadata.
- `Assets/Characters/NEXUS/`: the refined NEXUS character, first-person arms, LOD models, textures, and Unity metadata.
- `Assets/Materials/NEXUS/`, `Assets/Prefabs/`, and `Assets/Animations/`: materials and prefabs for the imported models.
- `Assets/Scenes/MainTest.unity` and `Assets/Scenes/CharacterPreview.unity`: asset preview scenes. `SampleScene.unity` remains the startup scene.
- `Assets/Documentation/`: warehouse source notes and character asset attribution.

The preview controller uses Unity's legacy Input API, so the project enables both input backends. Unity-generated `Library`, `Temp`, `Logs`, and similar directories are excluded from Git. NEXUS Blender sources and packaged archives remain outside this project. The complete K7 and warehouse source packages are included under `ArtSource`, outside Unity's automatically imported `Assets` folder.

### Warehouse replacement status

The model with all 119 doors closed is uploaded in [PR #21](https://github.com/AdaM404-dev/NET.runner-92/pull/21) on `codex/warehouse-closed-doors`. It replaces the previous visual FBX at `Assets/Environment/Warehouse_NearFuture/Models/Warehouse_NearFuture_Geometry.fbx`. Its existing `.meta` GUID is preserved, so `MainTest.unity` uses the replacement with its door scripts, colliders and material assignments retained. Unity 6000.6.2f1 import, compilation and the focused Play-mode door checks passed; see [the validation report](ArtSource/Warehouse_NearFuture/Unity_validation.json).

PR #21 is awaiting merge into `main`. Until it is merged, check out `codex/warehouse-closed-doors` before opening the Unity project to use this revision. The earlier active model has been overwritten in that branch; `ArtSource/Warehouse_NearFuture/Source/Original_Warehouse.blend` is an authoring reference and is outside Unity's imported `Assets` folder.

### Import the K7 enemy robot

Copy the `FBX`, `Textures` and `Unity` folders from `ArtSource/K7_Industrial_Robot` into `Assets/Characters/K7_Industrial_Robot`. After Unity imports the files and compiles the helpers, select `FBX/K7_Robot.fbx` and run **Tools > K7 > Build Prefab from Selected FBX**. The supplied helper creates materials for URP and a prefab with the LODs and gameplay sockets. Keep the Blender project and authoring scripts in `ArtSource`.

The upload preserves the original package. The K7 prefab has not been generated or tested in this repository; its AI, movement and combat behavior still need game integration. See the package README for setup instructions and the original validation limits.

The imported preview materials were authored with the Built-in Standard shader. Convert them to URP materials in the Unity editor if they appear magenta in this URP project.

end of README.md
