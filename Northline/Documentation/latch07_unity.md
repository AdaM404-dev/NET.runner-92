# LATCH-07 Unity integration

## Play the hospital scene

Run the Windows executable from its extracted folder or open `Hospital_Exploration.unity` in the Unity project. Walk left from the spawn toward the edge of the marked crossing inside the south gate. The new service relay sits beside the crossing. Its amber indicator turns cyan when the link succeeds. [Phase 06 Windows archive](../Builds/README.md#earlier-checkpoints)

| Control | In the hospital |
| --- | --- |
| Q | Raise LATCH-07. Aim at the service relay before pressing Q to acquire it. Press Q again from standby to stow it. |
| E | Tap Link while the tablet is raised. While stowed, E operates doors and lift controls or opens the tablet when aimed at the relay. |
| R | Cancel the current tablet action and lower it. |
| Esc | Release the cursor and hide the viewmodel. Left click recaptures the cursor. |

The optional relay is an integration test for the animation and UI. A tap commits at 42% of the touch animation and rechecks reach and line of sight. Without an acquired relay, the same tap plays the failure animation and shows the corresponding message. Canceling before contact leaves the relay offline. A successful link changes the relay indicator for the current scene session.

## Assets and setup

The copied FBX is byte-for-byte the Blender export. The isolated import scene checks one Generic avatar, nine skinned meshes, seven named 60 Hz clips, the 150 × 76 mm display, the full 0–1 UV rectangle and ready framing. It uses the Blender reference 50.53° vertical field of view. The hospital uses its own existing player camera and 68° vertical field of view. The prefab follows the player eyes and stays inactive outside tablet actions.

The Unity assets are in `Assets/NETRunner92/Player/Latch07`. HDRP materials use the source base color and normal PNGs. The five generated mask maps pack metallic and inverted roughness. The `NR92_Display_Surface` mesh receives a live 640 × 324 UI Toolkit panel on its own unlit material. The rendered text, signal bars and status come from the current interaction state. [PanelSettings render-target reference](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UIElements.PanelSettings-targetTexture.html)

`NET.runner-92 → LATCH-07 → Prepare Import Check` rebuilds the import prefab and review scene from the copied inputs. `NET.runner-92 → LATCH-07 → Install in Hospital` installs the prefab and optional relay. Hospital scene regeneration also restores the integration if the prefab is present. The installer can be run repeatedly on a scene with the feature installed.

## Verification and continuation

Play Mode exercised the stowed, equip, idle, tap, failure, success, cancel and unequip states on the real hospital player. The relay held its offline state through a pre-contact cancellation. The screen produced visible UI pixels in its RenderTexture and was legible from the gameplay camera. The hospital checks also covered existing doors, lift, stairs, patrols and reachability across all 108 zones. [Import checks](../Renders/Phase_06_Latch07/import_checks.txt) · [Play Mode checks](../Renders/Phase_06_Latch07/playmode_checks.txt) · [In-game view](../Renders/Phase_06_Latch07/07_LATCH07_Linked.png) · [Live UI capture](../Renders/Phase_06_Latch07/08_LATCH07_Screen.png)

The first focused Windows replay exposed lateral contact with the relay pedestal. The relay was moved beside the crossing. The rebuilt player passed all 21 focused checks, including unobstructed A movement, tablet Q/E/R, the link and the aimed-at door staying closed when E belonged to the tablet. The final import audit had 11 passes and Play Mode had 75 passes. [Final Windows input replay](../Renders/Phase_06_Latch07/focused_input_checks.txt)

The service relay demonstrates the original tablet link flow. The reception terminal now runs a separate puzzle that unlocks a live CCTV page on the tablet. The viewmodel shares the world depth buffer, so nearby world geometry can occlude it. [Reception CCTV controls](cctv_unity.md)
