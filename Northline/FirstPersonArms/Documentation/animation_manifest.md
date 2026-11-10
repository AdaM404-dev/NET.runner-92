# Viewmodel animation manifest

The NLA editor shows each clip as a named track and timeline marker. The stage camera is fixed. The rig uses an analytic two-link arm solve and independently authored finger rotations. The finished actions contain skeletal transform samples at 60 Hz and require no live Blender constraints.

| Clip | Scene frames | Duration | Loop | Intended transition |
| --- | --- | --- | --- | --- |
| Hack_Equip | 1–31 | 0.50 s | No | Stowed to ready. The forearm rolls toward view and the display rises. |
| Hack_Idle | 61–181 | 2.00 s | Yes | Ready pose with slight breath motion. Both ends use the same pose. |
| Hack_Interact | 211–259 | 0.80 s | No | Index approaches, taps and withdraws to ready. Contact hold is around the middle of the clip. |
| Hack_Success | 281–329 | 0.80 s | No | Restrained right-hand withdrawal and brief stable hold, ending ready. |
| Hack_Failed | 351–387 | 0.60 s | No | Quick hand check and partial carrier recoil, ending ready. |
| Hack_Unequip | 411–435 | 0.40 s | No | Carrier retracts and both arms lower to stowed. |
| Hack_Cancel | 461–476 | 0.25 s | No | Faster ready-to-stowed interruption. |

Each clip begins at local frame 1 in its Action. The inter-clip gaps in the scene are editing space. To preview one clip, use its marked range or select its Action. The continuous video uses both the success and failure branches. The FBX importer may prefix each animation name with `NR92_FP_Rig|`.

Recommended transitions use the common ready pose between actions and the common stowed pose for re-equipping. For a cancellation fired mid-action, use a short runtime blend from the current pose into the fast cancel action, since the exported cancel clip starts at the ready pose. Use game events for the tap and status changes. [Animation preview](../Renders/07_Animation_Preview.mp4) · [Pose audit](../Validation/pose_audit.json)
