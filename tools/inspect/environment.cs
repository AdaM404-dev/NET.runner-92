// Open scene: lights, ambient and fog, component census, collision, geometry statistics.
// Read-only. Run with: bin/unity-inspect environment   (the Unity editor must be open on this project)
// This is a snippet for `unity command eval_file`, not a compiled script: it is wrapped in a method,
// so there are no `using` lines; UnityEngine and System are already in scope.

var sb = new System.Text.StringBuilder();
System.Action<System.Collections.Generic.SortedDictionary<string,int>, string> inc = (d, k) => { d[k] = d.ContainsKey(k) ? d[k] + 1 : 1; };
// lights
var lights = UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsInactive.Include);
var lc = new System.Collections.Generic.SortedDictionary<string,int>();
foreach (var l in lights)
    inc(lc, "type=" + l.type + " mode=" + l.lightmapBakeType + " shadows=" + l.shadows + " range=" + l.range.ToString("0.#") + " intensity=" + l.intensity.ToString("0.##") + " spot=" + (l.type == UnityEngine.LightType.Spot ? l.spotAngle.ToString("0") : "-") + " color=#" + UnityEngine.ColorUtility.ToHtmlStringRGB(l.color) + " name~" + System.Text.RegularExpressions.Regex.Replace(l.name, @"[-\d\.]+", "#"));
sb.AppendLine("LIGHTS total=" + lights.Length);
foreach (var kv in lc) sb.AppendLine("  " + kv.Value + " x " + kv.Key);
// render settings
sb.AppendLine("RENDERSETTINGS skybox=" + (UnityEngine.RenderSettings.skybox ? UnityEngine.RenderSettings.skybox.name : "none") + " ambientMode=" + UnityEngine.RenderSettings.ambientMode + " ambientLight=" + UnityEngine.RenderSettings.ambientLight + " ambientSky=" + UnityEngine.RenderSettings.ambientSkyColor + " ambientEquator=" + UnityEngine.RenderSettings.ambientEquatorColor + " ambientGround=" + UnityEngine.RenderSettings.ambientGroundColor + " ambientIntensity=" + UnityEngine.RenderSettings.ambientIntensity);
sb.AppendLine("  fog=" + UnityEngine.RenderSettings.fog + " fogMode=" + UnityEngine.RenderSettings.fogMode + " fogColor=" + UnityEngine.RenderSettings.fogColor + " fogDensity=" + UnityEngine.RenderSettings.fogDensity + " fogStart=" + UnityEngine.RenderSettings.fogStartDistance + " fogEnd=" + UnityEngine.RenderSettings.fogEndDistance + " reflectionMode=" + UnityEngine.RenderSettings.defaultReflectionMode + " reflectionIntensity=" + UnityEngine.RenderSettings.reflectionIntensity + " sun=" + (UnityEngine.RenderSettings.sun ? UnityEngine.RenderSettings.sun.name : "none"));
sb.AppendLine("  lightmaps=" + UnityEngine.LightmapSettings.lightmaps.Length + " lightProbes=" + (UnityEngine.LightmapSettings.lightProbes != null ? UnityEngine.LightmapSettings.lightProbes.count.ToString() : "none"));

// other component types present in scene
var types = new System.Collections.Generic.SortedDictionary<string,int>();
foreach (var c in UnityEngine.Object.FindObjectsByType<UnityEngine.Component>(UnityEngine.FindObjectsInactive.Include)) inc(types, c.GetType().Name);
sb.AppendLine("COMPONENT TYPES: " + string.Join(", ", System.Linq.Enumerable.Select(types, kv => kv.Key + "=" + kv.Value)));
// collision objects
var cols = UnityEngine.Object.FindObjectsByType<UnityEngine.MeshCollider>(UnityEngine.FindObjectsInactive.Include);
int convex = 0, rendOn = 0, rendOff = 0, noRend = 0, colStatic = 0, colTrigger = 0; long colTris = 0;
foreach (var c in cols)
{
    if (c.convex) convex++; if (c.isTrigger) colTrigger++; if (c.gameObject.isStatic) colStatic++;
    var r = c.GetComponent<UnityEngine.MeshRenderer>(); if (r == null) noRend++; else if (r.enabled) rendOn++; else rendOff++;
    if (c.sharedMesh != null) colTris += c.sharedMesh.triangles.Length / 3;
}
sb.AppendLine("MESHCOLLIDERS total=" + cols.Length + " convex=" + convex + " trigger=" + colTrigger + " static=" + colStatic + " rendererEnabled=" + rendOn + " rendererDisabled=" + rendOff + " noRenderer=" + noRend + " totalTris=" + colTris);
var sc = UnityEngine.GameObject.Find("Static_Collision");
if (sc != null)
{
    int nocol = 0; var names = new System.Collections.Generic.List<string>();
    foreach (UnityEngine.Transform t in sc.transform) if (t.GetComponent<UnityEngine.Collider>() == null) { nocol++; var r = t.GetComponent<UnityEngine.MeshRenderer>(); names.Add(t.name + "(renderer " + (r == null ? "none" : r.enabled ? "ON" : "off") + ")"); }
    sb.AppendLine("Static_Collision children without collider=" + nocol + ": " + string.Join(", ", names));
    sb.AppendLine("Static_Collision prefab source=" + UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sc) + " pos=" + sc.transform.position + " rot=" + sc.transform.eulerAngles + " scale=" + sc.transform.localScale);
}
var geo = UnityEngine.GameObject.Find("Warehouse_NearFuture_Geometry");
if (geo != null)
{
    var rs = geo.GetComponentsInChildren<UnityEngine.MeshRenderer>(true); long tris = 0; var mats = new System.Collections.Generic.SortedDictionary<string,int>(); int shadowOn = 0;
    var b = new UnityEngine.Bounds(); bool first = true;
    foreach (var r in rs)
    {
        var mf = r.GetComponent<UnityEngine.MeshFilter>(); if (mf != null && mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
        foreach (var m in r.sharedMaterials) inc(mats, m ? m.name : "NULL");
        if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) shadowOn++;
        if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
    }
    sb.AppendLine("GEOMETRY renderers=" + rs.Length + " tris=" + tris + " shadowCasters=" + shadowOn + " bounds center=" + b.center + " size=" + b.size + " prefab=" + UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(geo) + " pos=" + geo.transform.position + " rot=" + geo.transform.eulerAngles + " scale=" + geo.transform.localScale);
    sb.AppendLine("GEOMETRY materials: " + string.Join(", ", System.Linq.Enumerable.Select(mats, kv => kv.Key + "=" + kv.Value)));
}
var wr = UnityEngine.GameObject.Find("Warehouse_NearFuture");
if (wr != null) sb.AppendLine("Warehouse root prefabStatus=" + UnityEditor.PrefabUtility.GetPrefabInstanceStatus(wr) + " children=" + string.Join(",", System.Linq.Enumerable.Select(System.Linq.Enumerable.Cast<UnityEngine.Transform>(wr.transform), t => t.name + "[" + UnityEditor.PrefabUtility.GetPrefabInstanceStatus(t.gameObject) + "]")));
return sb.ToString();
