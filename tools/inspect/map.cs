// Positions of doors, floor zones, stairs, docks and columns, one per line, for the floor plan.
// Read-only. Run with: bin/unity-inspect map > map.txt   (the Unity editor must be open on this project)
// This is a snippet for `unity command eval_file`, not a compiled script: it is wrapped in a method,
// so there are no `using` lines; UnityEngine and System are already in scope.

var sb = new System.Text.StringBuilder();
foreach (var d in UnityEngine.Object.FindObjectsByType<NetRunner.World.NexusDoor>(FindObjectsInactive.Include))
{
    var b = d.GetComponent<BoxCollider>().bounds;
    sb.AppendLine("DOOR|" + d.name + "|" + b.center.x.ToString("0.00") + "|" + b.center.y.ToString("0.00") + "|" + b.center.z.ToString("0.00") + "|" + b.size.x.ToString("0.00") + "|" + b.size.z.ToString("0.00") + "|" + (d.loadingDoor ? 1 : 0) + "|" + d.transform.position.x.ToString("0.00") + "|" + d.transform.position.z.ToString("0.00"));
}
var geo = GameObject.Find("Warehouse_NearFuture_Geometry");
foreach (Transform t in geo.transform)
{
    var r = t.GetComponent<Renderer>(); if (r == null) continue;
    string kind = null;
    if (t.name.StartsWith("STR_Column")) kind = "COLUMN";
    else if (t.name.StartsWith("FLOOR_") && !t.name.Contains("ControlJoints")) kind = "FLOOR";
    else if (t.name.StartsWith("STAIR_")) kind = "STAIR";
    else if (t.name.StartsWith("DOCK_")) kind = "DOCK";
    else if (t.name.StartsWith("MEZZ_")) kind = "MEZZ";
    else if (t.name.StartsWith("SHAFT_") && t.name.EndsWith("Cap")) kind = "SHAFT";
    else if (t.name.StartsWith("CATWALK_") && !t.name.Contains("Bracket")) kind = "CATWALK";
    else if (t.name.StartsWith("LANDING_")) kind = "LANDING";
    if (kind == null) continue;
    var b = r.bounds;
    sb.AppendLine(kind + "|" + t.name + "|" + b.center.x.ToString("0.00") + "|" + b.center.y.ToString("0.00") + "|" + b.center.z.ToString("0.00") + "|" + b.size.x.ToString("0.00") + "|" + b.size.z.ToString("0.00") + "|" + b.size.y.ToString("0.00"));
}
return sb.ToString();
