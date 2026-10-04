# Coding NEXUS character movement

This guide explains how the NEXUS movement code works and where to change it. The implementation lives in [`Assets/Scripts/NexusPlayer.cs`](../../Assets/Scripts/NexusPlayer.cs). [`Assets/Prefabs/NEXUS_Player.prefab`](../../Assets/Prefabs/NEXUS_Player.prefab) already connects that script to a `CharacterController`, camera, animator, LOD group, and character visual. Start from that prefab when developing movement; `NEXUS_Character.prefab` contains the visual without the player controller.

> Note (updated 2026-10-04): the gameplay scene `Assets/Scenes/Game/Warehouse.unity` uses `NEXUS_Player.prefab`. `MainTest` and `CharacterPreview` still hold their own copies of the player, so a change to the prefab does not show in those two scenes. The whole player object, its keys and its frame order are described in [[systems/player]]; the plan to fix the copies is step 3 of [[architecture/before-new-scripts]].

## Movement pipeline

Each frame, `NexusPlayer.Update()` reads input and calls `Simulate(input, run, jump, Time.deltaTime)` once. `Simulate` turns the two-dimensional input into a world-space direction, rotates the character, applies gravity and jumping through `CharacterController.Move`, and updates the Animator. `LateUpdate()` positions the camera after the character moves.

```text
Input in Update → camera-relative direction → CharacterController.Move
                → Animator Speed → camera follow in LateUpdate
```

The prefab disables Animator root motion, so the `CharacterController` owns translation. Keep one owner for movement: enabling root motion or calling `Move` from another controller as well will produce conflicting motion.

## 1. Read input in `Update`

The current code reads Unity's legacy `Input` axes and keys. It builds a `Vector2` from `Horizontal` and `Vertical`, reads the run and jump buttons, then passes those values to `Simulate` with the frame duration:

```csharp
Vector2 input = cursorCaptured
    ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
    : Vector2.zero;

Simulate(input, Input.GetKey(KeyCode.LeftShift),
    Input.GetKeyDown(KeyCode.Space), Time.deltaTime);
```

`cursorCaptured` prevents movement when the mouse has been released for UI use. `previewMode` also zeros movement inside `Simulate` and blocks jumping; leave it false for a playable character. The project sets **Active Input Handling** to **Both** because this controller still uses the legacy API.

Mouse input updates the private `yaw` and `pitch` fields in `Update`. `yaw` controls camera-relative movement and first-person facing; `pitch` is clamped to −78° through 78° for camera aim. If you replace the input source, continue updating these values before calling `Simulate`.

## 2. Calculate camera-relative direction

`Simulate` clamps the input vector to length 1 so diagonal movement is not faster, then rotates it by camera yaw:

```csharp
input = Vector2.ClampMagnitude(input, 1f);
Vector3 direction = Quaternion.Euler(0f, yaw, 0f)
    * new Vector3(input.x, 0f, input.y);
```

In first person, the root faces `yaw` directly. In third person, it turns toward nonzero movement with `Quaternion.Slerp`. If you add strafing or aim-while-moving, change this facing rule separately from the movement vector.

The prefab exposes `walkSpeed` (1.65 m/s) and `runSpeed` (3.25 m/s). `Simulate` chooses one and multiplies it by `direction`. Change these fields on the prefab for basic speed tuning. To add acceleration, keep a horizontal velocity field and move that velocity toward `direction * targetSpeed` each frame before calling `CharacterController.Move`; also drive the Animator from the resulting velocity rather than raw input.

## 3. Apply gravity and jumping

The controller stores vertical velocity in `verticalSpeed`. When grounded and falling, it holds the character against the floor at −2 m/s. A grounded jump sets vertical velocity to 4.4 m/s, then gravity subtracts `14 * dt` every frame:

```csharp
if (motor.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
if (jump && motor.isGrounded && !previewMode) verticalSpeed = 4.4f;
verticalSpeed -= 14f * dt;

motor.Move((direction * (run ? runSpeed : walkSpeed)
    + Vector3.up * verticalSpeed) * dt);
```

Keep the horizontal and vertical movement in the same `Move` call so collision handling sees the combined displacement. `CharacterController` does not apply gravity by itself. Tune jump impulse and gravity together; both are currently constants in `Simulate`. `Grounded` exposes the controller's grounded state, and `Respawn()` resets both position and vertical velocity.

## 4. Match animation and camera to motion

The Animator controller has a `Speed` float. `Simulate` sets it to the selected speed multiplied by input magnitude, with 0.15 seconds of damping. When you introduce acceleration, use actual horizontal velocity for this value so the animation follows the character's movement.

`SetView(bool)` switches the first-person animation layer, forces LOD 0 in first person, and makes head renderers cast shadows without drawing in the camera view. `LateUpdate()` then places the camera at the first-person eye position or behind the character in third person. The third-person camera spherecasts against layer 9 to shorten its offset near walls. Preserve this order if you change the camera: move the character in `Update`, then place the camera in `LateUpdate`.

## 5. Replace or extend the input source

The project includes `Assets/InputSystem_Actions.inputactions` with `Player/Move`, `Player/Look`, `Player/Jump`, and `Player/Sprint` actions, but `NexusPlayer` does not yet use them. To migrate:

1. Enable the `Player` action map and connect its action values to the existing move, look, run, and jump variables.
2. Replace the legacy reads in `Update`; keep the cursor, view-switching, and camera logic you still need.
3. Update `yaw` and `pitch` from the look action, then call `Simulate(move, run, jumpPressedThisFrame, Time.deltaTime)` exactly once per frame.
4. Remove or disable the old input path before another component starts calling `Simulate`. Otherwise both paths will move the same `CharacterController`.

`Simulate` is public for tests or external control, but it uses the controller's private `yaw`. An AI or network controller should supply a look/heading value through a new method or refactor heading into an explicit argument. Keep the animation and camera updates connected to the same movement state.

## Verify a movement change

Use `Assets/Scenes/MainTest.unity` for a quick integration check. Confirm that idle input leaves the player still, diagonal input does not increase speed, the character turns correctly in both views, a jump starts only while grounded and lands, the Animator follows actual motion, and the third-person camera moves closer near a layer-9 wall. Check `CharacterPreview.unity` separately if you touched `previewMode` or `SetView`.
