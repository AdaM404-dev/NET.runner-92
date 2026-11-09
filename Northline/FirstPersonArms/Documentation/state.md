# First-person viewmodel state

## Resume here

The Blender asset, verified FBX, textures, six stills and animation reel are complete. The hospital scene now has a Unity viewmodel, live screen and an optional service relay. [Unity integration checkpoint](../../Documentation/latch07_unity.md) · [Editable Blender project](../NETRunner_FP_Arms.blend) · [Verified FBX](../NETRunner_FP_Arms.fbx) · [Animation reel](../Renders/07_Animation_Preview.mp4)

## Modeling and rig

The standalone Blend contains the existing operative's two cropped arms, fitted textile and glove details, the new LATCH-07 assembly, an 80° stationary first-person camera, a temporary corridor and robot silhouette. The authored cloth, housing, guards, fasteners, webbing and optics remain editable.

## Animation and mechanical validation

All seven clips were baked at 60 Hz. The 328 frame samples found no camera-visible shoulder cut and zero visible asset vertices at each stowed endpoint. The closest arm vertex stayed 0.170 m from the camera. The display stayed inside the full 16:9 idle frame and occupied 7.93–8.27% of the image area. The tap approached within 0.6 mm. Three lower-screen probe targets reached 1.0 mm with matching target UVs.

The same frame sweep found zero right-hand/display, arm-to-arm, rigid mount/arm or carrier/guard triangle crossings in the tested sets. Flexible webbing, gasket and liner contact with the sleeve is part of the mounting fit. Those parts use close layered surfaces and are excluded from the rigid clearance result. The clip joins and loop endpoints match to within 0.000001 in the sampled bone matrices. [Full frame audit](../Validation/pose_audit.json) · [Three touch targets](../Validation/reach_targets.json)

## Export

The FBX was reopened in a clean Blender scene. It returned one rig, 41 named bones, nine mesh assemblies, 31,085 triangles, 12 materials and 20 embedded images. All seven animation stacks returned with the intended lengths. The three sampled poses per clip differed by at most 0.002 mm at the bone heads. The four screen UV corners survived. The exported scene contained only the viewmodel assets. [Round-trip report](../Validation/fbx_roundtrip.json)

## Review media

Blender reopened the H.264 review reel at 640 × 360, 134 frames and 20 fps. The reel shows all seven actions across the success and failure sequences. The six PNGs also reopened successfully. [Ready pose](../Renders/01_FirstPerson_Ready.png) · [Touch pose](../Renders/02_FirstPerson_Touch.png) · [Stowed pose](../Renders/03_FirstPerson_Stowed.png) · [Tablet close-up](../Renders/04_LATCH07_Closeup.png) · [Front](../Renders/05_Model_Front.png) · [Side](../Renders/06_Model_Side.png)

## Continuation

Unity import, camera framing, the seven Generic clips, the screen binding and the relay state flow have been verified in the hospital project. The authored touch still uses one fixed UV and the shoulder geometry is trimmed for first-person framing. Continue from the integration checkpoint when designing the full minigame. [Unity handoff](unity_integration.md)

The authoring scripts under `source` run inside Blender. Open the existing Phase 14 operative source read-only and run `build.py` to rebuild this package. Run `validate.py -- blend` and `validate.py -- fbx` on the saved viewmodel Blend. Run `render.py -- stills` or `render.py -- reel` on the same file for review output. The saved Blend is editable without the source character file. [Model overview](fp_arms_overview.md) · [Animation manifest](animation_manifest.md)
