# Unity, explained with this game's own objects

Every Unity idea the game uses today, each shown on a real object you can
click. Read it with the editor open on the `MainTest` scene. It takes about
25 minutes. Then do the hands-on [[guide/02-guided-tour]].

## 1. The editor windows

| Window | Shows |
| --- | --- |
| **Hierarchy** | the objects in the open scene, as a tree |
| **Scene** | the 3D world you can fly through while editing |
| **Game** | what the player's camera sees; this is where Play mode runs |
| **Inspector** | everything about the selected object or asset |
| **Project** | the files in `Assets/` |
| **Console** | messages, warnings and errors |

## 2. GameObject and Transform

A **GameObject** is an empty container with a name. On its own it does
nothing. Every GameObject has a **Transform**: position, rotation and scale.

Objects can be **children** of other objects. A child moves, turns and
scales with its parent. In the Hierarchy, `NEXUS_Player` has two children,
`NEXUS_Character` (the body) and `Player_Camera`. When the player moves, the
body goes along because it is a child.

> Watch out: a child's Transform shows its position **relative to its
> parent**, not its position in the world.

## 3. Components

Everything an object can do comes from the **components** attached to it,
listed top to bottom in the Inspector. Select `NEXUS_Player`: it has a
Transform, a CharacterController (the collision capsule) and Nexus Player
(our script).

The components used in this game:

| Component | Count in `MainTest` | What it does here |
| --- | ---: | --- |
| MeshFilter + MeshRenderer | 2,537 | holds a 3D shape and draws it with a material |
| SkinnedMeshRenderer | 162 | draws a shape that bends with a skeleton (the character) |
| MeshCollider | 704 | an invisible copy of a shape that physics can hit (the walls) |
| BoxCollider | 119 | a box that physics can hit (one per door) |
| CharacterController | 1 | the player's capsule, moved by code |
| Light | 106 | a lamp |
| Camera | 1 | renders the picture |
| AudioListener | 1 | the ears; sounds are heard from here |
| Animator | 1 | plays the character's animations |
| LODGroup | 1 | swaps the character model for simpler ones at a distance |
| NexusPlayer, NexusDoor | 1, 119 | our two scripts |

## 4. Scripts are components

A script is a C# class that derives from `MonoBehaviour`. Once it is on a
GameObject in a loaded scene, Unity creates the object and calls methods with
special names at the right moments. There is no `main` method, and you never
write `new NexusPlayer()`.

| Method | When Unity calls it | What `NexusPlayer` does in it |
| --- | --- | --- |
| `Awake()` | once, when the object is created | finds its CharacterController, remembers the start position |
| `Start()` | once, before the first frame | starts the auto-test if asked |
| `Update()` | every frame | reads keys and mouse, moves the player |
| `LateUpdate()` | every frame, after all `Update()`s and animations | places the camera |
| `OnGUI()` | several times per frame | draws the text in the corner |

A **frame** is one picture. At 60 frames per second, `Update()` runs 60
times a second. `Time.deltaTime` is the time since the last frame; multiplying
a speed by it makes movement the same on fast and slow computers.

## 5. The Inspector and public fields

A script's `public` fields appear in the Inspector, where you can type
values or drag in other objects. `NexusPlayer` has
`public Camera viewCamera;` and in the Inspector its View Camera slot points
to `Player_Camera`. That link is stored **in the scene file**, not in the
code.

> Watch out: `walkSpeed = 1.65f` in the code is only the starting value for
> a new component. After that, the Inspector value wins. And if a reference
> slot shows *None*, the script stops with a `NullReferenceException` when it
> uses it.

## 6. Scenes

A **scene** is a file (`.unity`) holding a set of GameObjects: a level, a
menu or a test room. This project has three: `SampleScene` (empty template),
`MainTest` (the warehouse) and `CharacterPreview`. A built game opens the
first scene of the list in **File > Build Profiles > Scene List**, which is
currently the empty `SampleScene`.

## 7. Assets and `.meta` files

Everything in `Assets/` is an **asset**. Next to each file Unity keeps a
`.meta` file with the asset's unique id (GUID). Scenes and prefabs point to
assets by that id, not by file name. Move or rename assets inside Unity, or
move the `.meta` along with the file, or the links break.

## 8. Models, meshes, materials and shaders

A **model** (`.fbx`, exported from Blender) contains **meshes** (shapes) and
often a skeleton and animations. Unity shows an imported model like a prefab
you can drop into a scene. `Warehouse_NearFuture_Geometry.fbx` is the whole
building.

A **material** says how a surface looks (colour, textures, shininess). It
does that with a **shader**, a small program that draws the surface. Our
materials use `Universal Render Pipeline/Lit`; a material with a shader this
pipeline cannot run shows up **magenta**.

## 9. Prefabs, instances and overrides

A **prefab** is a saved GameObject (with its children and components) that
can be reused. Placing it in a scene makes an **instance**; the Hierarchy
shows instances with a blue cube icon. Changing the prefab file changes every
instance.

- A change made on one instance only is an **override**. The 119
  `NexusDoor` components in `MainTest` are overrides on the imported
  warehouse model.
- A prefab placed inside another is a **nested prefab**: `NEXUS_Character`
  sits inside the `NEXUS_Player` prefab.
- A **variant** is a prefab that inherits from another and changes a few
  things: `NEXUS_FullBody_LOD0.prefab` is a variant of its FBX model.

> Watch out: the player in `MainTest` is **not** an instance of
> `NEXUS_Player.prefab`; it is a separate copy. Editing the prefab does not
> change the scene.

## 10. Colliders, the CharacterController and rays

A **collider** is an invisible shape the physics engine uses; the visible
mesh plays no part in collisions. The warehouse walls are 704 MeshColliders
on a separate, simpler model (`Static_Collision`); each door has a
BoxCollider.

The **CharacterController** is a capsule with a `Move()` method. It slides
along walls, climbs small steps and reports whether it stands on the ground,
but it does not fall on its own: our script adds gravity itself.

A **raycast** shoots an invisible line and reports the first collider it
hits. Pressing E raycasts 3.5 m from the camera; the third-person camera uses
a thick ray (a sphere cast) to avoid ending up inside walls.

## 11. Layers and tags

A **layer** is a number from 0 to 31 on every GameObject. Physics queries
and cameras can include or ignore layers. Here layer **8** is the player and
layer **9** is the whole warehouse, but neither has a name in Project Settings
> Tags and Layers. The code picks layers with bit masks:

```csharp
1 << 9       // only layer 9: the camera's wall check
~(1 << 8)    // every layer except 8: the E ray ignores the player's own body
```

A **tag** is a text label. Only `MainCamera` is used, on `Player_Camera`.

## 12. The camera

A **Camera** renders what it sees into the Game view. Its **field of view**
is how wide it sees; its **near and far clipping planes** are the closest and
furthest distances it draws. This game has one camera, and `NexusPlayer`
moves it every frame: behind the player or at the eyes. See [[systems/camera]].

## 13. Lights and the render pipeline

A **Light** is a lamp: point (all directions), spot (a cone) or directional
(the sun). Lights are either calculated every frame (**realtime**) or
pre-calculated into textures (**baked**). The warehouse has 106 realtime
point lights, none casting shadows.

The **render pipeline** is the engine part that draws the picture. This
project uses **URP** (Universal Render Pipeline), configured by the assets in
`Assets/Settings/`. See [[systems/rendering]].

## 14. Animation

| Piece | Here |
| --- | --- |
| **Animation clip**: recorded movement of a skeleton | `Walk_Forward`, inside the LOD0 model file |
| **Animator Controller**: a graph of states and when to switch between them | `Assets/Animations/NEXUS_Locomotion.controller` |
| **Parameter**: a value code sets to steer the graph | `Speed` |
| **Blend tree**: mixes clips by a parameter | idle → walk → jog by `Speed` |
| **Layer** with an **avatar mask**: a second animation limited to some bones | "First Person Arms", arms only |
| **Avatar**: maps the model's bones to a standard human skeleton | `NEXUS_FullBody_LOD0Avatar` |
| **Animator** component: plays a controller on a model | on `NEXUS_Character/LOD0` |

Open **Window > Animation > Animator** with `LOD0` selected to see the graph.
See [[systems/character-and-animation]].

## 15. Level of detail (LOD)

A **LODGroup** holds several versions of a model, from detailed to simple,
and draws the one that suits how large the object appears on screen.
NEXUS has four, from 188,001 down to 19,499 triangles.

## 16. Input

Unity has two input systems. The old **Input Manager** (`Input.GetKey`,
`Input.GetAxisRaw("Horizontal")`) is what `NexusPlayer` uses. The newer
**Input System** package works with an **actions asset** that maps named
actions (Move, Jump, Interact) to keys and gamepad buttons; the project has
one, `Assets/InputSystem_Actions.inputactions`, but no code uses it yet.

## 17. Play mode and Edit mode

Pressing Play runs the game inside the editor. **Anything you change in the
Inspector during Play mode is undone when you press Play again to stop.**
That makes Play mode a safe place to experiment.

A `*` after the scene name in the Hierarchy means unsaved changes. `MainTest`
can show one even though you changed nothing: URP adds its own helper
components to lights and cameras the first time it draws them. Until the team
decides to keep those, answer **Don't Save** when Unity asks.

## 18. Coroutines

A **coroutine** is a method that can pause (`yield return`) and continue in a
later frame. The auto-test in `NexusPlayer` is one: it walks for 120 frames,
waits, takes a screenshot, and so on.

## Where a change ends up

| You change | It is saved in |
| --- | --- |
| an object or component value in a scene | the `.unity` scene file |
| a prefab (in its own editing view) | the `.prefab` file, and so in every scene using it |
| code | the `.cs` file |
| an asset's import settings | its `.meta` file |
| layers, tags, quality, input, build list | files in `ProjectSettings/` |
| anything during Play mode | nowhere: it is undone |
