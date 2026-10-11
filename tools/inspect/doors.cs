// Doors: for every swing door, is the leaf shut or standing open as exported, and what does a toggle do?
// Read-only. Run with: bin/unity-inspect doors   (the Unity editor must be open on this project)
// This is a snippet for `unity command eval_file`, not a compiled script: it is wrapped in a method,
// so there are no `using` lines; UnityEngine and System are already in scope.

var sb = new System.Text.StringBuilder();
var groups = new System.Collections.Generic.SortedDictionary<string, int>();
var examples = new System.Collections.Generic.Dictionary<string, string>();
int noFrame = 0;
foreach (var d in UnityEngine.Object.FindObjectsByType<NetRunner.World.NexusDoor>(FindObjectsInactive.Include))
{
    if (d.loadingDoor) continue;
    var frame = GameObject.Find(d.name + "_Frame");
    var box = d.GetComponent<BoxCollider>();
    // leaf direction: the long horizontal axis of the leaf's local box, in world space
    Vector3 leafDir = box.size.x >= box.size.y ? d.transform.right : d.transform.up;
    leafDir.y = 0; leafDir.Normalize();
    string pose;
    if (frame == null) { noFrame++; pose = "no frame object"; }
    else
    {
        var fb = frame.GetComponent<Renderer>().bounds;
        Vector3 wallDir = fb.size.x >= fb.size.z ? Vector3.right : Vector3.forward;
        float a0 = Vector3.Angle(leafDir, wallDir); if (a0 > 90) a0 = 180 - a0;
        var opened = Quaternion.AngleAxis(d.angle, Vector3.up) * leafDir;
        float a1 = Vector3.Angle(opened, wallDir); if (a1 > 90) a1 = 180 - a1;
        pose = "asExported=" + (a0 < 20 ? "SHUT" : a0 > 60 ? "STANDING OPEN" : "AJAR") + "(" + a0.ToString("0") + " deg off the wall) afterToggle=" + (a1 < 20 ? "SHUT" : a1 > 60 ? "OPEN" : "AJAR") + "(" + a1.ToString("0") + " deg)";
    }
    string g = System.Text.RegularExpressions.Regex.Replace(d.name, @"[-\d\.]+", "#") + " | " + pose;
    groups[g] = groups.ContainsKey(g) ? groups[g] + 1 : 1;
}
foreach (var kv in groups) sb.AppendLine(kv.Value + " x " + kv.Key);
sb.AppendLine("doors without a matching _Frame object: " + noFrame);
return sb.ToString();
