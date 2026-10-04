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

For the playable character, drag `Assets/Prefabs/NEXUS_Player.prefab` into a scene with a collidable floor. The model-only prefab is `Assets/Prefabs/NEXUS_Character.prefab`. The player script uses Unity's legacy `Input` API, so set **Active Input Handling** to **Both** in Player Settings if the destination project uses the new Input System. The committed preview materials were converted to URP Lit on 2026-09-30. Keep URP shaders when importing into another URP project.

## Standalone main-menu prototype

Open `Assets/NETRunner/MainMenu/Scenes/NETRunner_MainMenu_Prototype.unity` and press Play. This surveillance terminal has four camera feeds, rare facility events, UI Toolkit panels and simulated network loading. Continue and New Session return to the terminal after the demonstration; gameplay and saves are not connected. The scene is deliberately absent from Build Settings.

See [the system note](docs/systems/main-menu.md), [asset guide](Assets/NETRunner/MainMenu/README.md) and [validation record](MAIN_MENU_PROGRESS.md).

## Included assets

- `ArtSource/K7_Industrial_Robot/`: the complete K7 enemy robot package, including its Blender source, FBX LODs, 2K/4K textures, Unity setup helpers, previews, authoring scripts and validation reports. See its [README](ArtSource/K7_Industrial_Robot/README.md).
- `ArtSource/Warehouse_NearFuture/`: editable Blender source, original building, scripts, textures, exports and review images. The 2026-10-02 revision closes all 119 doors; see its [review](ArtSource/Warehouse_NearFuture/Project_review.md) and [guide](ArtSource/Warehouse_NearFuture/README.md).
- `Assets/Environment/Warehouse_NearFuture/`: the futuristic warehouse visual and collision FBX models, textures, and Unity metadata.
- `Assets/Characters/NEXUS/`: the refined NEXUS character, first-person arms, LOD models, textures, and Unity metadata.
- `Assets/Materials/NEXUS/`, `Assets/Prefabs/`, and `Assets/Animations/`: materials and prefabs for the imported models.
- `Assets/Scenes/Game/Warehouse.unity`: the gameplay scene and the startup scene of a build; it places the warehouse prefab and `NEXUS_Player.prefab`. `Assets/Scenes/MainTest.unity` and `Assets/Scenes/CharacterPreview.unity`: asset preview scenes.
- `Assets/Documentation/`: warehouse source notes and character asset attribution.

The preview controller uses Unity's legacy Input API, so the project enables both input backends. Unity-generated `Library`, `Temp`, `Logs`, and similar directories are excluded from Git. NEXUS Blender sources and packaged archives remain outside this project. The complete K7 and warehouse source packages are included under `ArtSource`, outside Unity's automatically imported `Assets` folder.

### Import the K7 enemy robot

Copy the `FBX`, `Textures` and `Unity` folders from `ArtSource/K7_Industrial_Robot` into `Assets/Characters/K7_Industrial_Robot`. After Unity imports the files and compiles the helpers, select `FBX/K7_Robot.fbx` and run **Tools > K7 > Build Prefab from Selected FBX**. The supplied helper creates materials for URP and a prefab with the LODs and gameplay sockets. Keep the Blender project and authoring scripts in `ArtSource`.

The upload preserves the original package. The K7 prefab has not been generated or tested in this repository; its AI, movement and combat behavior still need game integration. See the package README for setup instructions and the original validation limits.

The committed preview materials already use URP Lit; future source exports should also target URP.

end of README.md
