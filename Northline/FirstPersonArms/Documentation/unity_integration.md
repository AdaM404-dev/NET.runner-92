# Unity handoff

The hospital project now contains the imported viewmodel, live screen and service relay. These are the source-asset import and binding notes for rebuilding it. [Implemented Unity setup and checks](../../Documentation/latch07_unity.md)

1. Import `NETRunner_FP_Arms.fbx` at metre scale. Its export axes are −Z forward and +Y up. Use the Generic rig type and keep `FP_Root` as the motion hierarchy root. Keep baked scale at 1 and confirm orientation and size with a metre reference.
2. Parent the viewmodel root to the player camera, or use a dedicated arms camera with an aligned origin. The Blender reference camera is at the origin with an 80° horizontal field of view, 16:9 framing and a 0.016 m near plane. If matching at 16:9 with a vertical FOV parameter, the equivalent is approximately 50.5°.
3. Import the seven clips. Set Loop Time for `Hack_Idle`. Build transitions around the ready and stowed states in the animation manifest. Use baked skeletal transforms, including the translations on the shoulders and on the device carrier.
4. Remap the named materials to the project's render pipeline. Use the exported PNGs if embedded slots do not map automatically. The standard HDRP mask channels are Metallic in R, AO in G, Detail mask in B and Smoothness in A. The package provides standalone Roughness and Metallic. Invert Roughness for Smoothness and repack when creating a pipeline mask. Import the tangent normals as normal maps.
5. Bind a world-space UI or RenderTexture to `NR92_Display_Surface`. Its unique UV0 plane travels with `Device_Display`. Use the 150:76 aspect ratio. Keep the preview text out of the runtime UI and route the minigame events through the controller separately from the surface material.
6. Enable the viewmodel when equip begins. Disable its renderers after unequip or cancel reaches the stowed endpoint. Handle screen power and per-instance indicator state in the runtime controller. If the arms render on a dedicated layer, preserve the world camera's coverage.

The hierarchy is `NR92_FP_Rig / FP_Root / LeftShoulder or RightShoulder / UpperArm / LowerArm / Hand / finger chains`. `Device_Mount` hangs from `LeftLowerArm` with `Device_Display` beneath it. All nine exported mesh assemblies are skinned to the single rig. Preserve the dedicated screen mesh when doing any future mesh consolidation.

The authored touch clip uses one fixed UV target. Three contact placements were tested in Blender. The current Unity integration uses the fixed clip and shared world depth. Import framing was checked at the 16:9 reference and at 4:3 with the hospital camera's field of view. [Blender FBX round-trip](../Validation/fbx_roundtrip.json) · [Unity import checks](../../Renders/Phase_06_Latch07/import_checks.txt)
