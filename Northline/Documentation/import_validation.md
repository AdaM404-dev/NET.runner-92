# Import validation

The source is [Hospital_Quarantine_Grounds.blend](../Environment/Hospital_Quarantine_Grounds.blend), including the viewport correction. Earlier Blender and FBX validation remains recorded in [the exterior report](exterior_validation.md). The earlier temporary FBX smoke test covered a small exterior sample. The Unity integration exports the entire corrected source as two named geometry bundles with their authored transforms, normals, UVs, material slots and hierarchy metadata. Blender X, Y, Z becomes Unity X, Z, Y at unit scale, with triangle orientation converted alongside the coordinate change.

| Region | Authored mesh placements | Unique evaluated mesh entries | Source material slots |
| --- | ---: | ---: | ---: |
| Hospital | 3,626 | 2,835 | 102 |
| Exterior | 3,607 | 845 | 42 |

The twelve Phase 4 collision proxies are present as physics. The Blender volume and review cameras were excluded from mesh import. HDRP fog and the player camera replace those presentation objects. The visible stairs, doors, floor slabs, fit-out, security perimeter, vehicles and exterior prop geometry come from the existing Blender model.

The material pass transfers shader color, roughness, metallic, coat, emissive and transparency properties to HDRP Lit materials. Shared triplanar wear maps supply real-time texture variation for aggregate, concrete and coatings. Blender shader graphs remain the high-quality source. Glass and puddles have transparent materials and separate physics where appropriate.

The saved Unity scene has 7,221 renderers, 2,058 physics shapes and 12 ramps under a renderer-free collision hierarchy. See [testing report](testing_report.md) for in-engine validation status.
