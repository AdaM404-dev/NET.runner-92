# Phase 1 modeling progress

## Completed

1. **Massing and programme.** Laid out four clinical and research levels, separate public and ambulance entrances, the longitudinal concourse and two continuous end stairs. Refined the south silhouette with a 2.2 m setback over the central research-floor frontage.
2. **Architecture.** Built the envelope, roof and parapets, entry canopies and aprons, slabs, ceiling beams, partitions, grid columns, door leaves and frames, windows, guarded stairs and two empty bed-lift shafts with pits and closed landing doors. The Blender file includes the complete scene and four isolated floor views.
3. **Verification.** Reopened the saved file in Blender 5.0.0. Checked the two elevations, each floor from overhead and first-person views at the public entry, ward hall and stair. All 1,335 mesh objects passed Blender mesh validation, manifold edge checks and signed-volume checks. A ray-based walkability sample reached all 108 occupiable zones from their floor concourse. Geometry checks sampled all 109 passable doorways or open portals, 189 glazed openings, 12 flights and their landings. The stair tread samples cleared a 2.30 m upward probe. A temporary Blender FBX export and reimport preserved all 1,335 mesh names and the building bounds.

## Open items

- The layout uses an assumed hospital programme and grid. Structural loads, accessibility and fire compliance need separate review if they become project requirements.
- The walkability checks used Blender geometry. The Unity player controller, collision, stair traversal, navmesh, room culling and runtime performance still need an engine playtest.
- Door leaves are posed for layout review. Mission locks, electronic access, stair routing and lift operation remain gameplay work.
- Furnishings, medical equipment, lighting, ceilings below the structural beams, surface ageing, signage, equipment services and other dressing belong to later phases.

## Next phase

Start with **Level 1 reception and emergency**. Block out the reception desk and waiting area, then establish the ambulance-to-resuscitation route and the first controlled threshold near triage. Walk that route in first person with temporary bed and equipment footprints to check turning clearances, sightlines and cover. Use the validated room and doorway dimensions to build the reusable ward and clinical furnishing sets for the upper levels.
