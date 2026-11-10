# Exterior asset manifest

The exterior is under `ENV_Hospital_Exterior` in [Hospital_Quarantine_Grounds.blend](../Environment/Hospital_Quarantine_Grounds.blend). New objects use the `Q4_` prefix. Names identify assemblies and parts. Matching posts, rails, fence panels, ground hardware, wheels and plants share mesh datablocks. The ambulance roots hold their editable parts and provide vehicle-local origins.

| Collection | Major assets and reuse |
| --- | --- |
| `01_Terrain` | Site formation and four gravel shoulders. Reuse the ground surfaces as terrain and grade references. |
| `02_Roads_Pavement` | Rectangular road slabs, plaza, north staging pad, visitor lot, both egress links, drainage grates, parking stops, crosswalk, stencils and embedded guidance cassettes. The pavement strips and grates are reusable. |
| `03_Security_Perimeter` | Footings, posts, welded anti-climb modules, outrigger strands, precast panel runs and two sealed gate pairs. Shared bay meshes repeat within each run. |
| `04_Main_Entrance` | Retractable bollards, planted queue split, benches, damaged information terminal, Northline monument, and failed security mast. Relocate the plaza furniture and terminals as assemblies. |
| `05_Ambulance_Area` | Direction board, medical stock cabinet, stretcher trolley, EMS charge terminals and rear checkpoint devices. The ambulance drive and original canopy remain separate from the vehicles. |
| `06_Checkpoints` | Gate armor, drive motors, contamination portal, staff booth, maintenance cage and reader, biometric terminal variants and working PTZ units. Gate leaves and motor housings remain separate. |
| `07_Quarantine_Props` | Concrete crash blocks, modular triage and decon shelters, privacy screens, evacuation assembly sign, carts and transit trunks. The shelters repeat the same bays, ballast and framing. |
| `08_Vehicles` | EMS E-12 and E-09, with molded wheel arches, glazing, road wheels, stretcher lift steps, sensors, light housings and equipment panels. Both are built as separate parented assemblies. |
| `09_Futuristic_Technology` | Perimeter emitter posts, vehicle charger, autonomous inspection drone and dock, cleaning crawler and lockable waste units. Terminals have physical bases and connection points. |
| `10_Vegetation` | Instanced sedge blades, shrub crowns, tree parts and north-wall ivy. Thin vegetation can be combined by cell on game export. |
| `11_Decay_Debris` | Water films and margins, fractures, moss, fallen screen, broken glazing detail, paper and glass fragments, fallen leaves and visitor belongings. Decorative layers can be culled independently. |
| `12_Lighting` | Road lamp and floodlight modules, powered and failed variants, targeted practical lights and large overcast bounce lights. |
| `13_Atmosphere` | Low wet-weather volume. Use as a reference when translating fog to the target renderer. |
| `14_Background` | Low terrain plate, north embankment and distant tree belt. Keep separate from playable geometry and colliders. |
| `15_Collision_Proxies` | Ten closed perimeter proxy segments and two vehicle proxies. This layer is excluded in review views. Include it deliberately in a collision export. |
| `16_Review_Cameras` | Eight exterior, first-person and overhead views. These are presentation cameras. |

Use `06 | Quarantine Grounds` for site review. The original floor and stacked fit-out views still isolate the interior. Procedural materials are self-contained in Blender. Before Unity integration, group static detail by zone, bake the required material maps, and test the lightweight proxies with the target controller. Keep the vehicle roots, sliding leaves and access-control meshes separate where the game will animate them.
