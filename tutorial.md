# NEXUS character controls

This guide covers the playable NEXUS character in this Unity 6.6 project. The controller is [`Assets/Scripts/NexusPlayer.cs`](Assets/Scripts/NexusPlayer.cs), and the ready-to-use prefab is [`Assets/Prefabs/NEXUS_Player.prefab`](Assets/Prefabs/NEXUS_Player.prefab).

## Try the character

1. Open `Assets/Scenes/MainTest.unity` in Unity and press **Play**. This is the warehouse scene with a movable NEXUS player.
2. Click the Game view if it does not have focus. The controller captures the mouse when play starts.
3. Open `Assets/Scenes/CharacterPreview.unity` to inspect the character in a stationary preview. Movement and jumping are disabled there.

`SampleScene.unity` is still the project's startup scene; it does not contain the NEXUS player. Both NEXUS scenes are enabled in Build Settings, so **F1** can switch between them.

## Controls

| Input | Action |
| --- | --- |
| **W / A / S / D** | Move relative to the camera in `MainTest`. |
| **Mouse** | Look around. In third person it orbits the camera; in first person it aims the view. |
| **Left Shift** | Run while moving. |
| **Space** | Jump while grounded in `MainTest`. |
| **E** | Toggle a `NexusDoor` within 3.5 metres of the camera crosshair. |
| **Tab** | Switch between third-person and first-person views. |
| **F1** | Switch between `MainTest` and `CharacterPreview`. |
| **R** | Return to the position recorded when the player spawned. |
| **Escape** | Release or recapture the mouse cursor. Left-click also recaptures it. |

In `CharacterPreview`, the mouse and **Tab** still change the view, but movement and jumping are intentionally disabled. Door interaction only has an effect when the camera ray hits an object with `NexusDoor` on it or one of its parents.

## Put NEXUS in another scene

1. Drag `Assets/Prefabs/NEXUS_Player.prefab` into the scene, placing its root just above a floor with a collider.
2. Keep the prefab's `CharacterController`, `NexusPlayer`, child visual, animator, LOD group, and `Player_Camera` references together. They are already assigned in the prefab.
3. Disable or remove any other active camera and audio listener if this is the player camera.
4. Leave **Preview Mode** unchecked for normal movement. Press **Play** and click the Game view.

`Assets/Prefabs/NEXUS_Character.prefab` is the character visual. Use `NEXUS_Player.prefab` when you need the camera and movement controls.

## Controller settings and integration

The `NexusPlayer` component exposes **Walk Speed** (1.65), **Run Speed** (3.25), and **Sensitivity** (2). **First Person** selects the starting view; **Preview Mode** locks movement for the character study scene. The script drives the Animator's `Speed` float and raises the first-person arms layer when first-person view is active. It also forces the highest LOD in first person and hides head geometry from the camera while retaining its shadows.

The controller reads Unity's legacy `Input` axes and keys. This project enables **Both** input backends in Player Settings; the `InputSystem_Actions.inputactions` asset is not wired to `NexusPlayer`. If your game uses only the new Input System, adapt `NexusPlayer.Update()` to your input actions before switching that setting. The public `Simulate(Vector2 input, bool run, bool jump, float dt)`, `SetView(bool)`, and `Respawn()` methods are available for integration, but `Update()` also reads player input every frame, so coordinate any external calls with that loop.

The controller uses layer 8 to exclude the player from door raycasts and layer 9 for third-person camera collision. If you change those layers, update the masks in `NexusPlayer.cs` as well. Doors need a collider and `NexusDoor` on the hit object or a parent. The character prefab's `CharacterController` is 1.78 m tall with a 0.26 m radius.

The imported materials were authored for Unity's Built-in Standard shader. In this URP project, convert them to URP materials if they render magenta. Asset attribution is in `Assets/Documentation/Character_Licenses/`.
