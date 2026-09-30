from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps
import json,shutil,zipfile,hashlib
B=Path(__file__).resolve().parents[2];O=B/'outputs'/'K7_Industrial_Robot';P=O/'Previews'
report=json.loads((O/'Validation'/'asset_report.json').read_text());fbx=json.loads((O/'Validation'/'fbx_roundtrip.json').read_text());uv=json.loads((O/'Validation'/'uv_validation.json').read_text())
meta=json.loads((B/'work'/'build_metadata.json').read_text())
counts=[x['triangles'] for x in report['lods']]
def font(n,bold=False):return ImageFont.truetype('C:/Windows/Fonts/'+('arialbd.ttf' if bold else 'bahnschrift.ttf'),n)
canvas=Image.new('RGB',(2500,2040),(15,21,26));d=ImageDraw.Draw(canvas)
d.text((36,22),'K-7',font=font(72,True),fill=(221,229,230));d.text((235,34),'INDUSTRIAL HUMANOID  /  ENEMY CONFIGURATION',font=font(29),fill=(185,198,202));d.text((237,75),'2.00 M  /  52 BONES  /  4 LODS  /  4K PBR  /  MODULAR ARMOR',font=font(21),fill=(109,133,143))
d.line((35,121,2465,121),fill=(88,110,119),width=2)
def panel(path,box,label,sub=None):
    x,y,w,h=box;d.rectangle((x,y,x+w,y+h),fill=(22,29,34),outline=(65,79,86),width=1)
    im=Image.open(P/path).convert('RGB');im=ImageOps.contain(im,(w-8,h-49));canvas.paste(im,(x+(w-im.width)//2,y+38+(h-42-im.height)//2))
    d.text((x+15,y+10),label,font=font(20),fill=(188,204,208))
    if sub:d.text((x+15,y+h-31),sub,font=font(17),fill=(169,185,190))
panel('K7_Hero.png',(35,150,775,1380),'01  /  FULL ASSET',f'LOD0  {counts[0]:,} TRIANGLES')
for i,(name,label) in enumerate([('Front','FRONT'),('Side','SIDE'),('Rear','REAR')]):panel('K7_'+name+'.png',(838+i*544,150,530,784),f'0{i+2}  /  {label}')
panel('K7_Face_Hostile.png',(838,960,802,570),'05  /  RECESSED THREAT OPTICS')
panel('K7_HackInterface.png',(1668,960,802,570),'06  /  REAR SERVICE INTERFACE')
for i,state in enumerate(['Patrol','Suspicious','Hostile','Hacked']):panel('K7_State_'+state+'.png',(35+i*615,1560,592,413),state.upper())
d.text((36,1993),'RIGGED GAME ASSET  /  HOSTILE FACE REVISION',font=font(19),fill=(129,152,162));d.text((1840,1993),'BLENDER + FBX + UNITY SETUP',font=font(19),fill=(129,152,162))
canvas.save(P/'K7_Overview.jpg',quality=95)
lodsheet=Image.new('RGB',(2400,1080),(15,21,26));ld=ImageDraw.Draw(lodsheet)
ld.text((30,20),'K-7  /  LOD SILHOUETTE COMPARISON',font=font(30),fill=(216,224,228))
for i in range(4):
    path=P/('K7_Hero.png' if i==0 else f'K7_LOD{i}.png');im=Image.open(path).convert('RGB');im=ImageOps.contain(im,(570,910));lodsheet.paste(im,(i*600+(600-im.width)//2,76));ld.text((i*600+30,1000),f'LOD{i}   {counts[i]:,} TRI',font=font(25),fill=(211,222,226))
lodsheet.save(P/'K7_LOD_Comparison.jpg',quality=95)

children={}
for b in meta['bones']:children.setdefault(b['parent'],[]).append(b['name'])
def tree(name,level=0):return '  '*level+name+'\n'+''.join(tree(c,level+1) for c in children.get(name,[]))
skeleton=tree('Root')
table='\n'.join(f"| LOD{x['level']} | {x['triangles']:,} | {x['vertices']:,} | {x['triangles']/counts[0]*100:.1f}% | {x['renderers']} |" for x in report['lods'])
maxrigid=max(fbx['rigid_edge_length_max_error_m'].values())
readme=f'''# K-7 Industrial Humanoid — enemy face revision

An articulated industrial enemy robot based on the supplied K-7 character sheet. The revised head has a heavy angular brow, narrow down-angled sensor slits, an asymmetric ranging camera, and a red hostile default. All four gameplay light states remain controllable.

This is a procedurally authored, functional game-asset iteration. Its surface complexity and visual fidelity are an approximation of the reference, not a hand-authored AA/AAA reproduction. Blender geometry, rig and FBX round-trip checks were completed. The Unity Editor is not available in this environment, so the included Unity integration has not been compiled or play-tested in Unity. Final engine acceptance and art approval remain open.

## Start here

- `Blender/K7_Industrial_Robot.blend`: complete Blender 5.0 project. Image maps and the source reference are packed. Default pose is frame 1; lower LOD collections and authoring helpers are hidden.
- `FBX/K7_Robot.fbx`: one skeleton, all four visual LODs, gameplay sockets and 15 collider proxies. This is the file used by the Unity setup menu.
- `FBX/K7_LOD0.fbx` through `K7_LOD3.fbx`: optional independent exports. Each is a complete alternative with its own copy of the skeleton; do not put all four independent files in one prefab.
- `FBX/K7_ArticulationCheck.fbx`: skeleton/attachment animation diagnostic. No finished walk/run or combat animation library is implied.
- `Textures/4K` and `Textures/2K`: shared image atlas at both resolutions.
- `Unity/Editor/K7AssetSetup.cs`: explicit prefab/material/LOD/collider setup action.
- `Unity/Runtime/K7Robot.cs`: light-state and armor-visibility helpers.
- `Previews/K7_Overview.jpg`: turnaround, revised face, back interface and all light states.
- `Validation`: measured geometry, UV and FBX import results.
- `Source`: reproducible authoring scripts and rebuild instructions.

## Geometry and LODs

| Level | Triangles | Vertices | Relative to LOD0 | Visual objects |
|---|---:|---:|---:|---:|
{table}

Counts exclude collision proxies and presentation objects. All meshes are triangulated. There are 7 visual materials and 12 visual objects at each LOD. LODs reuse the same atlas and 52-bone skeleton. Blender displays LOD0 by default; the other LODs are deliberate distance variants.

Start with screen-relative Unity transition heights `0.55`, `0.28`, `0.12`, `0.035`; below the last threshold the asset is culled. Profile these in the target camera and game. Cross-fading is off by default, avoiding extra shader variant requirements. Near-camera close-ups should use LOD0. LOD2/3 retain small submillimeter height changes from reduction.

The ten detachable/replaceable armor objects are `Armor_Chest`, `Armor_Back`, and `Armor_Shoulder_L/R`, `Armor_Forearm_L/R`, `Armor_Thigh_L/R`, `Armor_Shin_L/R`. Lower variants append `_LOD1`, `_LOD2`, `_LOD3`. The underlying frame remains visible after hiding these plates. `K7_HackInterface` is an independent skinned object on the Chest bone at every LOD.

## Units, axes and sockets

LOD0 measures **2.000 m** from the floor to the head. Bind-pose floor is Blender Z = 0. Root is (0, 0, 0). Visual mesh and rig object scales are (1, 1, 1), with baked mesh transforms. Blender is Z-up, facing -Y. FBX is exported with -Z forward / Y up for the intended Unity +Z-forward convention. The setup enables Unity `bakeAxisConversion` and file units at importer scale 1. Check the prefab facing and a 2 m measuring object in the target Unity project.

Sockets are bone-parented transforms, preserved by keeping Optimize Game Objects disabled. Their local +Z points outward along the intended interaction/look direction; the back HackPoint faces rearward.

| Socket | Parent | Blender bind position, meters |
|---|---|---|
'''
for name,data in report['sockets'].items():readme+=f"| `{name}` | `{data['bone']}` | ({', '.join(f'{v:.3f}' for v in data['blender_world_xyz'])}) |\n"
readme+=f'''
Use `VisionOrigin.forward` for sensor rays after engine validation. `HackPoint` is just outside the upper-back service panel. `AudioOrigin` is near the collar. Hand sockets mark palm interaction points; foot sockets mark contact positions in the bind pose. `Root` is the skeleton's ground-origin bone.

## PBR textures and materials

Every supplied map is 4096×4096 in `4K`, with a 2048×2048 copy in `2K`.

| File | Interpretation |
|---|---|
| `K7_BaseColor.png` | sRGB color; muted paint, industrial markings and restrained wear |
| `K7_Normal.png` | Linear tangent-space +Y normal map; import as Normal Map |
| `K7_Metallic.png` | Linear metallic scalar |
| `K7_Roughness.png` | Linear roughness; 0 smooth, 1 rough |
| `K7_AO.png` | Linear ambient occlusion; occlusion strength 0.7 suggested |
| `K7_MetallicSmoothness.png` | Unity packed texture: **R = metallic, A = 1 − roughness**, G/B unused |

Materials: `MAT_K7_Armor`, `MAT_K7_Internal`, `MAT_K7_Metal`, `MAT_K7_Rubber`, `MAT_K7_SensorGlass`, `MAT_K7_Cables`, `MAT_K7_StatusLight`.

All final visible surface shading uses standard image textures and Principled PBR values. The packed Blender reference is presentation-only. Source procedural shaders are not needed by the exported model. Sensor glass is opaque dark optical glass for predictable real-time rendering; no transparent sorting is required. Emission is a separately controlled material, not a baked state color in the body albedo. Its atlas texels are unused by the final status-light shader.

The sensor defaults to **Hostile** in the Blender scene and Unity helper. `SetState` supports cold white Patrol, amber Suspicious, red Hostile and blue Hacked. Intensity defaults to 5. Runtime changes use a per-material `MaterialPropertyBlock` on all LODs and do not mutate shared materials. Bloom, if desired, is a camera/render-pipeline effect configured by the game.

## Unity setup (URP or Built-in)

1. Extract the separate `K7_Unity_Import.zip` into a Unity project, retaining its `Assets/K7_Industrial_Robot` path. Alternatively copy only this package's `FBX`, `Textures`, and `Unity` folders beneath one directory in `Assets`. Keep the `.blend` and authoring sources outside `Assets` to avoid Unity launching Blender for automatic conversion.
2. Allow scripts to compile, then select `FBX/K7_Robot.fbx` in the Project window.
3. Run **Tools → K7 → Build Prefab from Selected FBX**. This configures Generic import, file units, axis conversion, imported normals/Mikk tangents, material texture channels, LODGroup membership, collision proxies and gameplay socket references. It creates `Materials` and `Prefabs/K7_Industrial_Robot.prefab` beneath the selected package.
4. Place the prefab at identity transform. Verify its 2 m height, +Z facing, normal-map orientation, socket directions, light colors and LOD transitions under the actual game lighting.
5. Preview the diagnostic clip from `K7_ArticulationCheck.fbx`, or assign your Generic AnimatorController. Root motion is not authored in the diagnostic. The setup keeps the default prefab free of an automatically playing test controller.
6. Add the game's navigation, AI, health, hacking logic, movement capsule and Rigidbody/CharacterController as appropriate. These gameplay systems are outside this asset package.

The setup detects URP Lit or Built-in Standard. HDRP material creation is not automated. For HDRP, create corresponding Lit materials and repack channels as required by the project. The Editor menu is explicit and operates on the selected K7 asset only. It intentionally retains bones and sockets instead of optimizing them out.

Each `COL_*` object is a closed box proxy parented to the corresponding skeleton bone. The setup replaces its visible mesh components with a trigger BoxCollider. There are head, torso, pelvis, upper-arm, forearm, hand, thigh, shin and foot proxies. Trigger callbacks still require a suitable project physics configuration. These are hit-detection preparation, not a configured ragdoll or detailed mesh colliders.

Example runtime use:

```csharp
using Hexacorp.K7;
K7Robot robot = enemy.GetComponent<K7Robot>();
robot.SetState(K7Robot.SensorState.Hacked);
Transform target = robot.hackPoint;
robot.SetArmorVisible("Armor_Chest", false); // applies to all LODs
```

## Rig and animation

The 52-bone skeleton uses 100% rigid weights. Each mechanical part follows one bone. Fingers have three articulating segments each, including opposing thumbs. Armor does not stretch. The bind pose is a relaxed A-pose; Generic is the supplied default. Humanoid retargeting requires manual Avatar mapping and a proper T-pose conversion in Unity; it has not been certified in this build.

In Blender, select `K7_Rig` and set the custom properties `FootIK_L/R` or `HandIK_L/R` from 0 to 1 to enable the optional two-bone IK constraints. Unhide `K7_ANIMATION_CONTROLS` for targets and poles. FK is the default. Independent Head and Neck joints support scanning. Head local travel limits are approximately yaw ±75°, pitch −30°/+35°, roll ±20°. The optional head tracking constraint is off by default. Blender authoring controls are excluded from FBX; animate/bake the deform skeleton for export or use Unity Animation Rigging on the supplied bones and sockets.

`K7_ArticulationCheck` is a 300-frame, 30 fps proof sequence: head scans at frames 30/60, a shallow crouch and reach at 90, closed-hand reach at 150, left/right leg and toe lifts at 210/270, rest resets between them. It is intended for inspection, not finished movement, planted-foot locomotion or gameplay timing. FBX round-trip import may offset the first sample from frame 1 to frame 2; duration is unchanged.

```text
{skeleton.rstrip()}
```

## Verification and limits

- Blender file opens; textures are packed and external maps are supplied.
- All four LODs have zero boundary/non-manifold edges, zero degenerate triangles, normalized single-bone weights and clean visual-object transforms.
- LOD0 UV raster audit at 4K found **{uv['overlapping_interior_texels']} overlapping interior texels**. Shared edges are excluded; subpixel overlaps are below this test's resolution. Cross-LOD atlas reuse is intentional.
- Rigid edge-length error across six sampled poses is at most **{maxrigid:.8f} m**.
- Both hand and foot IK solvers reached their test targets within **0.02 mm**.
- FBX round-trip preserved all 48 visual objects, 52 bones, 15 collision proxies, triangle counts and socket positions. Measured imported height is **{fbx['roundtrip']['height_m']:.6f} m**.
- The diagnostic animation round-tripped with finger motion.
- Front, side, rear, face, service interface, grasp, crouch and distance-LOD renders were reviewed.

Assemblies intentionally overlap at bearings, fasteners and armor mounting interfaces. Rigid hoses are attached to their owning segment; they are not physically simulated. Extreme motions, deep crouches, retargeted clips and production hand interactions still need motion-specific clearance review. Full swept-volume collision testing, polished animation, physics simulation, in-engine shader compilation and target-hardware performance testing have not been completed. No claim of AA/AAA visual equivalence or production-engine certification is made.

Unity API references used for the helper:

- [Rig import settings](https://docs.unity3d.com/6000.0/Documentation/Manual/FBXImporter-Rig.html)
- [LODGroup](https://docs.unity3d.com/6000.0/Documentation/Manual/class-LODGroup.html)
- [Axis conversion](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ModelImporter-bakeAxisConversion.html)
- [Per-material property blocks](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Renderer.SetPropertyBlock.html)
'''
(O/'README.md').write_text(readme,encoding='utf8')

for name in ['build_k7.py','bake_k7.py','finish_textures.py','clean_k7.py','patch_caps.py','finalize_k7.py','validate_export.py','validate_uv.py','package_k7.py']:
    shutil.copy2(B/'work'/'scripts'/name,O/'Source'/name)

# Remove only the known backup produced when replacing this deliverable.
backup=O/'Blender'/'K7_Industrial_Robot.blend1'
if backup.exists():backup.unlink()
files=[p for p in O.rglob('*') if p.is_file() and p.name!='SHA256SUMS.txt']
(O/'SHA256SUMS.txt').write_text('\n'.join(hashlib.sha256(p.read_bytes()).hexdigest()+'  '+p.relative_to(O).as_posix() for p in sorted(files))+'\n')
with zipfile.ZipFile(B/'outputs'/'K7_Industrial_Robot_Package.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for p in O.rglob('*'):
        if p.is_file():z.write(p,Path('K7_Industrial_Robot')/p.relative_to(O))
with zipfile.ZipFile(B/'outputs'/'K7_Unity_Import.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for folder in ['FBX','Textures','Unity']:
        for p in (O/folder).rglob('*'):
            if p.is_file():z.write(p,Path('Assets')/'K7_Industrial_Robot'/p.relative_to(O))
    z.write(O/'README.md','Assets/K7_Industrial_Robot/README.md')
print('PACKAGED',counts)
