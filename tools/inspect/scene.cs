// Open scene: object count, roots, layers, tags, static flags, doors.
// Read-only. Run with: bin/unity-inspect scene   (the Unity editor must be open on this project)
// This is a snippet for `unity command eval_file`, not a compiled script: it is wrapped in a method,
// so there are no `using` lines; UnityEngine and System are already in scope.

var sb = new System.Text.StringBuilder();
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var all = new System.Collections.Generic.List<UnityEngine.Transform>();
foreach (var root in scene.GetRootGameObjects()) all.AddRange(root.GetComponentsInChildren<UnityEngine.Transform>(true));
sb.AppendLine("SCENE " + scene.name + " path=" + scene.path + " objects=" + all.Count + " unsavedChanges=" + scene.isDirty);
foreach (var root in scene.GetRootGameObjects())
{
    sb.AppendLine("root '" + root.name + "' prefabStatus=" + UnityEditor.PrefabUtility.GetPrefabInstanceStatus(root) + " objects=" + root.GetComponentsInChildren<UnityEngine.Transform>(true).Length);
    foreach (UnityEngine.Transform c in root.transform)
        sb.AppendLine("  child '" + c.name + "' prefabSource=" + UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject) + " objects=" + c.GetComponentsInChildren<UnityEngine.Transform>(true).Length);
}
// layers and tags and static and inactive
var layers = new System.Collections.Generic.SortedDictionary<int, int>();
var tags = new System.Collections.Generic.SortedDictionary<string, int>();
int stat = 0, inactive = 0;
foreach (var t in all)
{
    int l = t.gameObject.layer; layers[l] = layers.ContainsKey(l) ? layers[l] + 1 : 1;
    string tg = t.gameObject.tag; tags[tg] = tags.ContainsKey(tg) ? tags[tg] + 1 : 1;
    if (t.gameObject.isStatic) stat++;
    if (!t.gameObject.activeSelf) inactive++;
}
foreach (var kv in layers) sb.AppendLine("layer " + kv.Key + " '" + UnityEngine.LayerMask.LayerToName(kv.Key) + "': " + kv.Value);
foreach (var kv in tags) sb.AppendLine("tag " + kv.Key + ": " + kv.Value);
sb.AppendLine("static=" + stat + " inactiveSelf=" + inactive);
// which top-level groups use each custom layer
var groups = new System.Collections.Generic.SortedDictionary<string, int>();
foreach (var t in all)
{
    if (t.gameObject.layer < 8) continue;
    var top = t; while (top.parent != null && top.parent.parent != null) top = top.parent;
    string k = "layer " + t.gameObject.layer + " under '" + (top.parent != null ? top.parent.name + "/" : "") + top.name + "'";
    groups[k] = groups.ContainsKey(k) ? groups[k] + 1 : 1;
}
foreach (var kv in groups) sb.AppendLine(kv.Key + ": " + kv.Value);
// static flags census
var sf = new System.Collections.Generic.SortedDictionary<string, int>();
foreach (var t in all) { string k = UnityEditor.GameObjectUtility.GetStaticEditorFlags(t.gameObject).ToString(); sf[k] = sf.ContainsKey(k) ? sf[k] + 1 : 1; }
foreach (var kv in sf) sb.AppendLine("staticFlags [" + kv.Key + "]: " + kv.Value);
// doors
var doors = UnityEngine.Object.FindObjectsByType<NetRunner.World.NexusDoor>(UnityEngine.FindObjectsInactive.Include);
int loading = 0; var angles = new System.Collections.Generic.SortedDictionary<string, int>();
var doorLayers = new System.Collections.Generic.SortedDictionary<int, int>();
int trig = 0, withMeshCol = 0; var childNames = new System.Collections.Generic.SortedDictionary<string, int>();
foreach (var d in doors)
{
    if (d.loadingDoor) loading++;
    string k = "loading=" + d.loadingDoor + " angle=" + d.angle + " lift=" + d.lift; angles[k] = angles.ContainsKey(k) ? angles[k] + 1 : 1;
    int l = d.gameObject.layer; doorLayers[l] = doorLayers.ContainsKey(l) ? doorLayers[l] + 1 : 1;
    var bc = d.GetComponent<UnityEngine.BoxCollider>(); if (bc != null && bc.isTrigger) trig++;
    if (d.GetComponent<UnityEngine.MeshCollider>() != null) withMeshCol++;
    foreach (UnityEngine.Transform c in d.transform) { string cn = System.Text.RegularExpressions.Regex.Replace(c.name, @"[-\d\.]+", "#"); childNames[cn] = childNames.ContainsKey(cn) ? childNames[cn] + 1 : 1; }
}
sb.AppendLine("DOORS total=" + doors.Length + " loadingDoor=" + loading + " boxColliderIsTrigger=" + trig + " withMeshCollider=" + withMeshCol);
foreach (var kv in angles) sb.AppendLine("  door config " + kv.Key + ": " + kv.Value);
foreach (var kv in doorLayers) sb.AppendLine("  door layer " + kv.Key + ": " + kv.Value);
// names of door objects grouped
var dn = new System.Collections.Generic.SortedDictionary<string, int>();
foreach (var d in doors) { string k = System.Text.RegularExpressions.Regex.Replace(d.name, @"[-\d\.]+", "#"); dn[k] = dn.ContainsKey(k) ? dn[k] + 1 : 1; }
foreach (var kv in dn) sb.AppendLine("  door name pattern '" + kv.Key + "': " + kv.Value);
return sb.ToString();
