# Northline Medical visual upgrade

The cinematic Phase 3 environment uses the furnished Northline Medical hospital from modeling Phase 5. All four levels are furnished in Blender. The previous hospital remains available both in its original location and as a byte-identical backup. [Furnished source](../Hospital/Phase_05/NET-runner-92-hospital-phase-05.blend) · [Pre-upgrade backup](../Environment/Hospital_Pre_Visual_Upgrade.blend) · [Upgraded environment](../Environment/Hospital_Final.blend)

## Architectural pass

| Area | Added work |
| --- | --- |
| Exterior | Zinc cap flashings, anodized spandrel and pier assemblies aligned to the existing grid, four rooftop air-handling assemblies, distinct public and ambulance fascias, and mixed working and failed canopy lamps. The public fascia has modeled **NORTHLINE MEDICAL** and **MEDICAL RESEARCH FACILITY / EST. 2087** lettering. |
| Level 1, reception and emergency | Neutral clinical laminate and darker crash-zone cladding, overhead acoustic and lighting cassette above reception, and a cold-white standby circuit at selected points. The room is still readable from the public approach. |
| Level 2, wards | Warmer patient-side finish variants and standby lights along the existing circulation. The original ward pods, beds, ward access, and glazing layout are retained. |
| Level 3, surgery and ICU | Cooler service-side wall panels, visible overhead service lanes, and retained bright clinical task pools around surgical equipment. |
| Level 4, restricted research | Desaturated green-gray laminates, warmer controlled-access hardware, separate sensor faces at checkpoint A, and privacy glazing on selected room windows. The sensor faces sit beside the original door frame and preserve the existing checkpoint reader and moving leaves. |
| All levels | Repeated inspection cassettes, slotted return grilles, limited battery-light segments, service intercoms, lift status plates, and a floor index above each lift lobby opening. |

The new cladding follows polygons from the existing wall faces. It terminates at openings and omits panels where baseline props, trim, or signs occupy the same area. New architectural overlays live in separate collections under each floor and the exterior. The stacked fit-out view hides the overlays so the furniture remains visible.

## Materials and lighting

The material pass uses Principled shaders with physically scaled broad aging, fine surface relief, roughness variation, and anisotropic contact scuff patterns. Runoff is most apparent on the exterior. Clinical partitions and interior panels retain quieter variation, and a low clear coat gives floor highlights more definition where fixtures reflect. Lift hardware uses a more metallic response than painted clinical doors. Thick existing glazing uses a 1.48 IOR and a roughness range. Selected panes receive an object-level frosted material override. The shared glass mesh and its geometry are unchanged.

Existing review lights were rebalanced by floor and use. Additional power is concentrated at modeled practicals, reception, the entrance, and the research checkpoint. The cinematic scene has its own shot lighting and transient effects. The environment materials are native Blender node graphs and can be baked or translated to the intended Unity render pipeline.

## Validation

- Reopened the upgraded file in Blender 5.0.0. All 3,225 pre-existing objects retain their names, world transforms, data names, collection memberships, and custom properties. All 3,020 original meshes retain their vertex positions, edge topology, and polygon topology. The six original view layers preserve their baseline object visibility.
- Checked additions against 2,160 rays through 108 original framed openings, 36 concourse path samples, and 132 stair headroom samples. No sampled ray was blocked by an addition. See the [furnished baseline validation](../Hospital/Phase_05/modeling_progress.md) for the prior occupied-zone and stair checks.
- The environment contains 606 additional meshes, 18 practical light objects, and one exterior review camera. Lettering was converted to mesh. No live external fonts or textures are needed.
- The navigation remains south entrance, west stair to wards, east stair to acute care, then west stair to restricted research. The original route still depends on gameplay access control. The Blender review is geometric and does not exercise a Unity controller or scripted state.

## Environment stills

The five 1920 × 1080 images use the upgraded Blender environment. Cycles rendered the stills with up to 16 samples, adaptive sampling, denoising, and AgX. The exterior still uses temporary neutral ground and a lighting rig for the product view. The furnished floor stills use the in-file review cameras and upgraded lights.

- [Exterior](../Renders/Stills/Hospital_Exterior.png)
- [Reception](../Renders/Stills/Hospital_Reception.png)
- [Wards](../Renders/Stills/Hospital_Wards.png)
- [Surgery](../Renders/Stills/Hospital_Surgery.png)
- [Research checkpoint](../Renders/Stills/Hospital_Research.png)

## Engine handoff

Original interaction identifiers, moving door leaf objects, and structural floor surfaces remain available under the same names. Export the new envelope and clinical overlay collections separately for material baking, visibility, and LOD work. Validate the resulting collider set, material response, and access scripting inside the game.

