# Warehouse review — 2026-10-02

The two-storey industrial complex retains a consistent near-future style, an identifiable structural grid, independent door leaves, and detailed service equipment. The requested edit corrects the initial door state in both the editable source and visual FBX.

## Changes and verification

- All 119 operable doors are closed: 113 hinged leaves and six fully lowered loading shutters. The 71 formerly open leaves use their authored `closed_rotation_z` values.
- Closed leaf axes align with their corresponding frames, and leaf widths fit within those frames. Loading-shutter lower edges remain at floor level.
- All mesh vertex coordinates and polygon topology are unchanged. Non-door world transforms are unchanged; all 119 door skins retain their local transforms and parents.
- The visual FBX re-import contains the same 1,827 meshes and 1,086,728 triangles, with UV0, materials and all texture files present. Every imported door world pose matches the closed Blender source within 0.0001.
- All eight release views were rendered again. Office, MainHall and FloorPlan were inspected: leaves sit in their frames, the corridors are clear of open leaves, and the building layout remains intact.

## Integration considerations

- Approximately 1.09 million visual triangles and 1,827 render meshes warrant a performance check in the target game; this revision adds no geometry. LODs and batching remain future work.
- The supplied route check excludes operable leaves and represents unlocked/open doors. A closed door should block navigation until an interaction permits passage; no NavMesh or controller traversal was validated by that geometric report.
- Blender preview lighting is separate from Unity's lighting. The repository already has URP materials and scene components; their `.meta` GUIDs and remaps must be preserved when replacing the FBX.
- Existing Unity interaction reach, physics behavior, locks, sounds and code structure are outside this art edit. Issue #16 remains a broader code/art decision; the current user explicitly requested re-exporting this warehouse closed.
- `Preservation_validation.json` records the initial retrofit stage. `Door_closure_validation.json` and `Export_validation.json` describe this revision.

The GitHub package includes the editable Blender file, untouched original building, authoring/export scripts, textures, FBX files, regenerated review images and validation reports under `ArtSource/Warehouse_NearFuture`. Unity's imported models remain under `Assets/Environment/Warehouse_NearFuture/Models`.

## Unity verification

Unity 6000.6.2f1 on Windows imported and compiled the project without compiler errors. A read-only MainTest check passed 594 assertions: 119 NexusDoor components, 113 hinged leaves aligned with frames, six unraised shutters, all door colliders and attached skins, 1,827 visual meshes and assigned materials. In Play mode all doors began logically and physically closed; DOOR_Hall_X-18_4 opened 95 degrees on its first scripted Toggle call and returned closed on the next. No scene was saved. No manual key input or Unity rendering was tested. `Unity_validation.json` records the result; `Source/WarehouseDoorValidation.cs` contains the temporary editor check.
