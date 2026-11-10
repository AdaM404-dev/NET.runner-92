# NET.runner-92

Unity 6.6 project (`6000.6.2f1`). Open this directory in Unity Hub.

Development status and agent handoff notes are tracked in [agent.md](agent.md).

The game's main map is the **Northline hospital**, rendered with **HDRP**
(since [#31](https://github.com/AdaM404-dev/NET.runner-92/issues/31),
2026-11-10). Its production material (Blender sources, cinematic, renders,
design notes) is in [Northline/](Northline/README.md).

## Import into Unity

### Open the complete project

New machine? The step-by-step tutorial for Windows and Linux (Git LFS, Unity Hub, the exact editor version, first open, troubleshooting) is [docs/guide/00-install.md](docs/guide/00-install.md). The short version:

1. Clone the repository. The art (textures, models, Blender files, audio) is
   stored in [Git LFS](https://git-lfs.com/), so install LFS once per machine
   first; Git for Windows includes it:

   ```bash
   git lfs install
   git clone https://github.com/AdaM404-dev/NET.runner-92.git
   ```

   A clone without LFS, or a ZIP download from GitHub, contains small pointer
   files instead of the art, and Unity shows missing or magenta assets.

2. In Unity Hub, add the cloned or extracted project folder. Select the folder that directly contains `Assets`, `Packages`, and `ProjectSettings`.
3. Open it with Unity **6000.6.2f1**. Let Unity download the packages and finish importing the assets on first launch. The generated `Library` folder does not need to be downloaded from GitHub.
4. In the Project window, open `Assets/NETRunner92/Scenes/Hospital_Exploration.unity` and press **Play**. W/A/S/D walk, Shift sprints, the mouse looks, **E** uses doors, lift buttons and the reception terminal, **Q** raises the LATCH-07 tablet. All keys: [docs/systems/player.md](docs/systems/player.md).

On Linux the editor must run on **Vulkan** (HDRP does not support OpenGL); the project's Player Settings select it. See [docs/architecture/tooling.md](docs/architecture/tooling.md).

## Included assets

- `Assets/NETRunner92/`: the hospital: scenes, environment, materials, the K-7 patrol robot, the LATCH-07 arms and tablet, atmosphere, HDRP settings, code and scene builders. See [docs/systems/level-hospital.md](docs/systems/level-hospital.md).
- `Assets/NETRunner/Core/`: plain C# rules and their tests.
- `Northline/`: the hospital's Blender sources, cinematic, renders, interface prototype and design notes.
- `ArtSource/K7_Industrial_Robot/`: the complete K7 robot source package (Blender source, FBX LODs, 2K/4K textures, previews, validation reports). The hospital already uses its FBX with HDRP materials. See its [README](ArtSource/K7_Industrial_Robot/README.md).
- `Assets/Characters/NEXUS/`, `Assets/Materials/`, `Assets/Animations/`, `Assets/Prefabs/`: the NEXUS character. It is not used by the hospital and its materials are still URP (they render magenta); whether it stays is open on #31.
- `Assets/Documentation/`: character asset attribution.

Unity-generated `Library`, `Temp`, `Logs`, and similar directories are excluded from Git.

### The K7 source package

The hospital's K-7 patrols use the package's FBX with HDRP materials (`Assets/NETRunner92/Environment/K7/`). The package's own Unity helper (`ArtSource/K7_Industrial_Robot/Unity`) creates URP materials and is not used. See [docs/systems/enemy-k7.md](docs/systems/enemy-k7.md).
