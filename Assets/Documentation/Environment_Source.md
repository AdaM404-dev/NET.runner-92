# Warehouse complex — near-future retrofit

Open **Warehouse_NearFuture.blend** in Blender 5.0 or later. This is an additive upgrade of the existing warehouse, with renewed materials and lighting. The original deliverable remains unchanged. An untouched source copy is included in **Source/Original_Warehouse.blend**.

## What changed

- Reinforced skins on the original hinged doors, magnetic-lock headers, status indicators and wall-mounted access readers. Added door skins are parented to the original leaves and follow their hinge motion.
- Reinforced loading-bay housings, shutter detailing, digital bay status, dock controllers, alignment scanners and illuminated guidance lines. Shutter detailing follows each separate original loading door.
- Dark graphite façade overlays, renewed metal coatings, tinted smart-glass materials and a restrained industrial identity.
- Neutral/cool-white overhead lighting rails attached to existing roof purlins, column-mounted monitoring pods, selected cyan status accents, and short illuminated railing/tread markers.
- Built-in hall logistics panels, office room-control panels, technical power/relay housings, service diagnostic screens, tidy conduit runs and shaft lock indicators.
- Floor guidance markings, safety boundaries, security sensors and compact rooftop communications units.

All screen graphics and security devices are modeled visual systems. Runtime door automation, scanning, hacking logic, UI interaction and switchable glass behavior are not implemented.

## What was preserved

The 118 × 80 m footprint, 72 × 44 m hall, structural grid, roof geometry, room partitions, dock positions, catwalks, stair placement and original door transforms remain unchanged. The audit compares the vertex coordinates, polygon indices and world transforms of **all 1,716 original mesh objects**, including existing collision helpers and blockout objects. It found no differences.

The upgrade is organized under **17_NEAR_FUTURE_RETROFIT**, with separate collections for doors, façades, security, circulation lighting, service infrastructure, ceiling systems, roof communications and signage. Hide that collection to inspect the original geometry with the renewed materials still applied; open the source copy for the complete original appearance.

The existing **01_COMPLETE**, **02_CUTAWAY** and **03_GROUND_PLAN** view layers remain available. The master opens with the full roof enabled. Existing cameras are preserved, so the comparison gallery uses identical viewpoints.

## Files

- **Warehouse_NearFuture.blend** — editable scene; all texture maps are packed.
- **Warehouse_NearFuture_Geometry.fbx** — visual geometry and door parent relationships.
- **Warehouse_NearFuture_Collision.fbx** — original static collision and stair ramps, plus collision helpers for substantial added panels, dock assemblies and column casings.
- **Textures/** — portable Base Color, Roughness and Normal maps for the renewed original material families, plus two flat interface textures. Added modular hardware uses authored PBR materials; subtle emissive materials use color/strength values.
- **Previews/Release/** and **Review.html** — eight rendered views, plus three original views for direct comparison.
- **Preservation_validation.json**, **Route_validation.json**, **Export_validation.json**, **Asset_manifest.json** — measurements and test results.

## Validation and Unity integration

The room-door grid connectivity and stair landing/headroom checks are rerun with the added static geometry included. Operable door leaves and their parented skins are excluded from these route checks, representing open/unlocked doors. These are sampled geometric checks, not a complete controller simulation.

The FBX is re-imported into a fresh Blender scene to check object/triangle counts, UVs, materials and texture references. Unity runtime testing is not performed.

The final visual export contains **1,827 mesh objects and 1,086,728 triangles**, including the original 664,944-triangle building. The collision export contains **710 helper objects**. Labels use reduced curve resolution; screen graphics use flat texture maps. Original linked geometry is preserved in the Blender source.

Import the visual FBX and textures using metre scale. Recreate/remap materials for the chosen Unity render pipeline, including emission and glass. Convert roughness to smoothness where required. Import the collision FBX at the same transform, disable its renderers and add static non-convex MeshColliders. Moving doors need separate colliders and interaction components; their new skins must remain children of their door leaves. Generate lightmap UVs, bake lighting/occlusion/navigation, and test with the target player controller. Emissive strips and the Blender preview lights are not a finished Unity lighting setup.

## Reproducibility

The source scene is loaded directly; the original building generator is not executed. `geometry_helpers.py` contains only the reusable primitive construction functions.

```text
blender --background --python upgrade_warehouse.py
blender --background --python export_unity.py
blender --background --python validate_routes.py
blender --background --python check_export.py
blender --background --python render_previews.py -- Overview Exterior MainHall UpperLevel Maintenance Office FloorPlan Enclosed
```

These scripts overwrite the generated near-future files. Save any manually edited version under a new name before rebuilding. The supplied maps are used directly; no network or external asset download is required.
