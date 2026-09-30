# Rebuilding the K-7 asset

The delivered `.blend` and FBX files are already generated. These scripts are optional authoring sources.

Tested environment: Windows, Blender 5.0.0, NVIDIA RTX 3060 Ti / OptiX, Python with NumPy and Pillow, Windows Arial/Bahnschrift fonts. A different GPU may need the Cycles device configuration changed to CPU in the Blender scripts. Font paths are Windows-specific.

From PowerShell:

```powershell
.\rebuild.ps1 -Workspace 'C:\K7_Rebuild' -Blender 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' -Python 'python'
```

Choose a fresh writable workspace. The pipeline creates `work` and `outputs/K7_Industrial_Robot` beneath it, copies the source scripts, and generates geometry, UVs, PBR bakes, decal textures, rigid skinning, LODs, the diagnostic clip, FBX exports, preview renders, validation reports and ZIP deliverables. It can take several minutes, including GPU shader compilation.

`build_k7.py` contains the dimensional model and rig definitions. `finish_textures.py` draws atlas markings and microdetail. `clean_k7.py` seals curve ends and removes collapsed bevel faces, assigning dedicated UV space to the end caps. `finalize_k7.py` builds image-only export materials, reduces LODs, exports and renders. The validation scripts check geometry, rigid motion, FBX round-trip behavior and UV interior overlap.

The authoring pipeline deliberately keeps the final gameplay FBX separate from the Blender presentation cameras, lights, floor and reference image. `K7AssetSetup.cs` and `K7Robot.cs` are supplied integration helpers; Unity project testing is still required.
