// Player object: components, references, camera, LODs, animator, layers.
// Read-only. Run with: bin/unity-inspect player   (the Unity editor must be open on this project)
// This is a snippet for `unity command eval_file`, not a compiled script: it is wrapped in a method,
// so there are no `using` lines; UnityEngine and System are already in scope.

var sb = new System.Text.StringBuilder();
System.Func<UnityEngine.Transform, string> path = null;
path = t => t.parent == null ? t.name : path(t.parent) + "/" + t.name;
var player = UnityEngine.Object.FindFirstObjectByType<NetRunner.Player.NexusPlayer>(UnityEngine.FindObjectsInactive.Include);
if (player == null) return "no NexusPlayer in open scene";
var go = player.gameObject;
sb.AppendLine("PLAYER " + go.name + " tag=" + go.tag + " layer=" + go.layer + " static=" + go.isStatic + " pos=" + go.transform.position + " rot=" + go.transform.eulerAngles + " scale=" + go.transform.localScale);
sb.AppendLine("prefabStatus=" + UnityEditor.PrefabUtility.GetPrefabInstanceStatus(go) + " source=" + UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(go)));
bool isInstance = UnityEditor.PrefabUtility.GetPrefabInstanceStatus(go) != UnityEditor.PrefabInstanceStatus.NotAPrefab;
sb.AppendLine("isPrefabInstance=" + isInstance + " isPartOfAnyPrefab=" + UnityEditor.PrefabUtility.IsPartOfAnyPrefab(go));
if (isInstance)
{
    var mods = UnityEditor.PrefabUtility.GetPropertyModifications(go);
    sb.AppendLine("overrides=" + (mods == null ? 0 : mods.Length));
    sb.AppendLine("addedComponents=" + UnityEditor.PrefabUtility.GetAddedComponents(go).Count + " addedGameObjects=" + UnityEditor.PrefabUtility.GetAddedGameObjects(go).Count + " removedComponents=" + UnityEditor.PrefabUtility.GetRemovedComponents(go).Count);
}
var cc = go.GetComponent<UnityEngine.CharacterController>();
sb.AppendLine("CharacterController height=" + cc.height + " radius=" + cc.radius + " center=" + cc.center + " slopeLimit=" + cc.slopeLimit + " stepOffset=" + cc.stepOffset + " skinWidth=" + cc.skinWidth + " minMoveDistance=" + cc.minMoveDistance);
sb.AppendLine("NexusPlayer viewCamera=" + (player.viewCamera ? path(player.viewCamera.transform) : "null") + " animator=" + (player.animator ? path(player.animator.transform) : "null") + " lodGroup=" + (player.lodGroup ? path(player.lodGroup.transform) : "null") + " visual=" + (player.visual ? path(player.visual) : "null"));
sb.AppendLine("NexusPlayer firstPerson=" + player.firstPerson + " previewMode=" + player.previewMode + " walkSpeed=" + player.walkSpeed + " runSpeed=" + player.runSpeed + " sensitivity=" + player.sensitivity + " enabled=" + player.enabled);
var cam = player.viewCamera;
if (cam != null)
{
    sb.AppendLine("CAMERA " + cam.name + " tag=" + cam.tag + " layer=" + cam.gameObject.layer + " localPos=" + cam.transform.localPosition + " localRot=" + cam.transform.localEulerAngles + " fov=" + cam.fieldOfView + " near=" + cam.nearClipPlane + " far=" + cam.farClipPlane + " cullingMask=" + cam.cullingMask + " clearFlags=" + cam.clearFlags + " bg=" + cam.backgroundColor + " depth=" + cam.depth + " hdr=" + cam.allowHDR + " msaa=" + cam.allowMSAA + " ortho=" + cam.orthographic);
    var urp = cam.GetComponent("UniversalAdditionalCameraData");
    if (urp == null) sb.AppendLine("  URP camera data: not present (URP adds it when the camera is first used)");
    else
    {
        var so = new UnityEditor.SerializedObject(urp);
        sb.AppendLine("  URP camera data: postProcessing=" + so.FindProperty("m_RenderPostProcessing").boolValue + " antialiasing=" + so.FindProperty("m_Antialiasing").enumDisplayNames[so.FindProperty("m_Antialiasing").enumValueIndex] + " renderShadows=" + so.FindProperty("m_RenderShadows").boolValue);
    }
    var al = cam.GetComponent<UnityEngine.AudioListener>(); sb.AppendLine("  AudioListener enabled=" + (al && al.enabled));
}
var lg = player.lodGroup;
if (lg != null)
{
    sb.AppendLine("LODGROUP on " + path(lg.transform) + " size=" + lg.size + " localRefPoint=" + lg.localReferencePoint + " fadeMode=" + lg.fadeMode + " animateCrossFading=" + lg.animateCrossFading + " enabled=" + lg.enabled + " lodCount=" + lg.lodCount);
    var lods = lg.GetLODs();
    for (int i = 0; i < lods.Length; i++)
    {
        var parents = new System.Collections.Generic.HashSet<string>();
        foreach (var r in lods[i].renderers) if (r != null) parents.Add(r.transform.parent != null ? r.transform.parent.name : "?");
        sb.AppendLine("  LOD" + i + " screenRelativeTransitionHeight=" + lods[i].screenRelativeTransitionHeight + " fadeTransitionWidth=" + lods[i].fadeTransitionWidth + " renderers=" + lods[i].renderers.Length + " under=[" + string.Join(",", parents) + "]");
    }
}
foreach (var an in go.GetComponentsInChildren<UnityEngine.Animator>(true))
    sb.AppendLine("ANIMATOR on " + path(an.transform) + " controller=" + (an.runtimeAnimatorController ? UnityEditor.AssetDatabase.GetAssetPath(an.runtimeAnimatorController) : "null") + " avatar=" + (an.avatar ? an.avatar.name + " (" + UnityEditor.AssetDatabase.GetAssetPath(an.avatar) + ") human=" + an.avatar.isHuman + " valid=" + an.avatar.isValid : "null") + " applyRootMotion=" + an.applyRootMotion + " updateMode=" + an.updateMode + " cullingMode=" + an.cullingMode + " enabled=" + an.enabled);
var visual = player.visual;
for (int i = 0; visual != null && i < visual.childCount; i++)
{
    var lod = visual.GetChild(i);
    var smr = lod.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true);
    var mr = lod.GetComponentsInChildren<UnityEngine.MeshRenderer>(true);
    int tris = 0; foreach (var r in smr) if (r.sharedMesh != null) tris += r.sharedMesh.triangles.Length / 3;
    var src = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(lod.gameObject);
    sb.AppendLine("VISUAL child " + lod.name + " active=" + lod.gameObject.activeSelf + " layer=" + lod.gameObject.layer + " localPos=" + lod.localPosition + " localScale=" + lod.localScale + " skinned=" + smr.Length + " meshRenderers=" + mr.Length + " tris=" + tris + " children=" + lod.childCount + " origin=" + UnityEditor.AssetDatabase.GetAssetPath(src) + " nearestPrefab=" + UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(lod.gameObject));
}
var layers = new System.Collections.Generic.SortedDictionary<int, int>();
foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>(true)) { int l = t.gameObject.layer; layers[l] = layers.ContainsKey(l) ? layers[l] + 1 : 1; }
foreach (var kv in layers) sb.AppendLine("player-hierarchy layer " + kv.Key + " (" + UnityEngine.LayerMask.LayerToName(kv.Key) + "): " + kv.Value + " objects");
return sb.ToString();
