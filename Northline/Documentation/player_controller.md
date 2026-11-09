# Invisible first-person controller

`PlayerController` contains a CharacterController, the movement script, one camera, and an AudioListener. The capsule and environment collision hierarchy have no renderers. The eyes sit 1.65 m above the player's origin. Head bob is disabled by default.

| Input | Action |
| --- | --- |
| W, A, S, D | Walk |
| Mouse | Look, with configurable sensitivity and an 85° vertical limit |
| Left Shift | Sprint |
| E | Operate the targeted world control or tap Link when the tablet is raised |
| Q | Raise or stow LATCH-07 |
| R | Cancel the current tablet action |
| 1 | Open or close the live camera page after the reception breach |
| Arrow keys | Adjust the reception puzzle or select a camera on the CCTV page |
| Esc | Release cursor and hide the tablet |
| Left click | Capture cursor |

Movement normalizes the input vector and caps diagonal speed. Walk and sprint target 3.5 and 5.5 m/s. The capsule is 1.80 m high with a 0.30 m radius, 0.35 m step offset, 0.034 m skin and 50° slope limit. Ground adhesion and gravity run through the same CharacterController movement call. Motion is subdivided for stable stair contact.

The twelve visible Blender flights remain in the model. Their colliders are invisible ramps between original landing elevations, with the original rail, wall and landing collision retained. A flight rises 2.10 m over 3.19 m at the same 1.97 m width. The physical isolation closures enforce the west/east/west first ascent. The moving lift cabin carries the controller while preserving camera aim. [Interaction systems](interactive_systems.md) · [Testing report](testing_report.md)

LATCH-07 follows the eyes and is hidden when stowed. Tablet input owns E while raised. Player movement and camera controls remain available during tablet use. [LATCH-07 controls and relay](latch07_unity.md)

The reception terminal uses a separate modal input state. Movement and mouse look pause while the terminal puzzle is open and resume when the player exits. [Reception CCTV controls](cctv_unity.md)
