// Animator controller, blend tree, avatar mask, model import settings, prefab structure.
// Read-only. Run with: bin/unity-inspect animation   (the Unity editor must be open on this project)
// This is a snippet for `unity command eval_file`, not a compiled script: it is wrapped in a method,
// so there are no `using` lines; UnityEngine and System are already in scope.

var sb = new System.Text.StringBuilder();
var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animations/NEXUS_Locomotion.controller");
foreach (var p in ctrl.parameters) sb.AppendLine("PARAM " + p.name + " " + p.type + " default=" + p.defaultFloat);
for (int i = 0; i < ctrl.layers.Length; i++)
{
    var l = ctrl.layers[i];
    sb.AppendLine("LAYER " + i + " '" + l.name + "' defaultWeight=" + l.defaultWeight + " blending=" + l.blendingMode + " mask=" + (l.avatarMask ? UnityEditor.AssetDatabase.GetAssetPath(l.avatarMask) : "none") + " ik=" + l.iKPass + " states=" + l.stateMachine.states.Length + " anyStateTransitions=" + l.stateMachine.anyStateTransitions.Length);
    foreach (var cs in l.stateMachine.states)
    {
        var st = cs.state;
        sb.AppendLine("  STATE '" + st.name + "' speed=" + st.speed + " transitions=" + st.transitions.Length + " motion=" + (st.motion ? st.motion.GetType().Name + ":" + st.motion.name : "none"));
        var bt = st.motion as UnityEditor.Animations.BlendTree;
        if (bt != null)
        {
            sb.AppendLine("    BLENDTREE type=" + bt.blendType + " parameter=" + bt.blendParameter + " autoThresholds=" + bt.useAutomaticThresholds);
            foreach (var c in bt.children)
            {
                var clip = c.motion as UnityEngine.AnimationClip;
                sb.AppendLine("      threshold=" + c.threshold + " timeScale=" + c.timeScale + " clip=" + (clip ? clip.name + " length=" + clip.length.ToString("0.###") + "s loop=" + clip.isLooping + " fps=" + clip.frameRate + " avgSpeed=" + clip.averageSpeed + " humanMotion=" + clip.humanMotion : "null"));
            }
        }
        var ac = st.motion as UnityEngine.AnimationClip;
        if (ac != null) sb.AppendLine("    CLIP " + ac.name + " length=" + ac.length.ToString("0.###") + "s loop=" + ac.isLooping + " humanMotion=" + ac.humanMotion);
    }
}
var mask = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AvatarMask>("Assets/Animations/NEXUS_FirstPerson_UpperBody.mask");
if (mask != null)
{
    var parts = new System.Collections.Generic.List<string>();
    for (int i = 0; i < (int)UnityEngine.AvatarMaskBodyPart.LastBodyPart; i++) parts.Add(((UnityEngine.AvatarMaskBodyPart)i) + "=" + (mask.GetHumanoidBodyPartActive((UnityEngine.AvatarMaskBodyPart)i) ? "ON" : "off"));
    sb.AppendLine("MASK humanoid: " + string.Join(", ", parts) + " transformCount=" + mask.transformCount);
}
foreach (var fbx in new[] { "Assets/Characters/NEXUS/FBX/NEXUS_FullBody_LOD0.fbx", "Assets/Characters/NEXUS/FBX/NEXUS_FullBody_LOD1.fbx", "Assets/Characters/NEXUS/FBX/NEXUS_FirstPerson_Arms.fbx" })
{
    var imp = UnityEditor.AssetImporter.GetAtPath(fbx) as UnityEditor.ModelImporter;
    sb.AppendLine("FBX " + fbx + " animationType=" + imp.animationType + " avatarSetup=" + imp.avatarSetup + " importAnimation=" + imp.importAnimation + " sourceAvatar=" + (imp.sourceAvatar ? imp.sourceAvatar.name : "none") + " globalScale=" + imp.globalScale + " optimizeGameObjects=" + imp.optimizeGameObjects + " materialImportMode=" + imp.materialImportMode + " materialLocation=" + imp.materialLocation + " isReadable=" + imp.isReadable + " meshCompression=" + imp.meshCompression);
    foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbx))
    {
        var clip = o as UnityEngine.AnimationClip;
        if (clip != null && !clip.name.StartsWith("__preview__")) sb.AppendLine("    clip '" + clip.name + "' length=" + clip.length.ToString("0.###") + "s loop=" + clip.isLooping + " humanMotion=" + clip.humanMotion + " events=" + clip.events.Length + " hasRootCurves=" + clip.hasRootCurves);
    }
}
// where are the prefabs used
foreach (var pf in new[] { "Assets/Prefabs/NEXUS_Player.prefab", "Assets/Prefabs/NEXUS_Character.prefab", "Assets/Prefabs/NEXUS_FirstPerson_Arms.prefab", "Assets/Prefabs/NEXUS_FullBody_LOD0.prefab" })
{
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(pf);
    sb.AppendLine("PREFAB " + pf + " type=" + UnityEditor.PrefabUtility.GetPrefabAssetType(go) + " rootComponents=[" + string.Join(",", System.Linq.Enumerable.Select(go.GetComponents<UnityEngine.Component>(), c => c.GetType().Name)) + "] children=[" + string.Join(",", System.Linq.Enumerable.Select(System.Linq.Enumerable.Cast<UnityEngine.Transform>(go.transform), t => t.name + "{" + string.Join("+", System.Linq.Enumerable.Select(t.GetComponents<UnityEngine.Component>(), c => c.GetType().Name)) + "}")) + "] layer=" + go.layer + " variantOf=" + UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(go)));
}
return sb.ToString();
