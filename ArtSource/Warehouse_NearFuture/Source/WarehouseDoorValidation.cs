using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Temporary, read-only integration check. It never saves the scene or assets.
[InitializeOnLoad]
public static class WarehouseDoorValidation
{
    static NexusDoor[] doors;
    static NexusDoor sample;
    static Quaternion closedRotation;
    static double started;
    static int phase;
    static int checks;
    static string ReportPath => Path.GetFullPath("Logs/warehouse-door-validation.json");

    static WarehouseDoorValidation()
    {
        if (SessionState.GetBool("WarehouseDoorValidationRunning", false))
        {
            EditorApplication.playModeStateChanged += ModeChanged;
            EditorApplication.update += Tick;
            checks = SessionState.GetInt("WarehouseDoorValidationChecks", 0);
            started = EditorApplication.timeSinceStartup;
        }
    }

    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainTest.unity");
            doors = UnityEngine.Object.FindObjectsByType<NexusDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(doors.Length == 119, "119 NexusDoor components retained");
            Require(doors.Count(d => d.loadingDoor) == 6, "six loading shutters retained");
            foreach (var d in doors)
            {
                Require(d.GetComponent<BoxCollider>() != null, "door collider: " + d.name);
                Require(d.transform.Cast<Transform>().Count(t => t.name.StartsWith("NF_DOOR_Skin_") || t.name.StartsWith("NF_LOADING_LeafDetail_")) == 1,
                    "parented skin: " + d.name);
                if (!d.loadingDoor)
                {
                    Require(AngleFromFrame(d) < 0.1f, "closed leaf: " + d.name);
                }
                else
                {
                    Require(Mathf.Abs(d.transform.position.y) < 0.02f, "unraised shutter: " + d.name);
                }
            }
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/Warehouse_NearFuture/Models/Warehouse_NearFuture_Geometry.fbx");
            Require(imported.GetComponentsInChildren<MeshFilter>(true).Length == 1827, "1827 visual meshes retained");
            Require(!imported.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.sharedMaterials.Any(m => m == null)), "material references retained");
            SessionState.SetBool("WarehouseDoorValidationRunning", true);
            SessionState.SetInt("WarehouseDoorValidationChecks", checks);
            EditorApplication.playModeStateChanged += ModeChanged;
            EditorApplication.update += Tick;
            started = EditorApplication.timeSinceStartup;
            phase = 0;
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Fail(e); }
    }

    static float AngleFromFrame(NexusDoor d)
    {
        var frame = GameObject.Find(d.name + "_Frame");
        Require(frame != null, "frame: " + d.name);
        var bounds = frame.GetComponent<Renderer>().bounds;
        var box = d.GetComponent<BoxCollider>();
        var leaf = box.size.x >= box.size.y ? d.transform.right : d.transform.up;
        leaf.y = 0;
        var axis = bounds.size.x >= bounds.size.z ? Vector3.right : Vector3.forward;
        var angle = Vector3.Angle(leaf.normalized, axis);
        return Mathf.Min(angle, 180 - angle);
    }

    static void ModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            phase = 1;
            started = EditorApplication.timeSinceStartup;
        }
    }

    static void Tick()
    {
        try
        {
            double elapsed = EditorApplication.timeSinceStartup - started;
            if (elapsed > 120) throw new Exception("Play-mode validation timeout");
            if (phase == 1 && elapsed > 1)
            {
                doors = UnityEngine.Object.FindObjectsByType<NexusDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Require(doors.Length == 119 && doors.All(d => !d.IsOpen), "all 119 doors start logically closed in Play mode");
                Require(doors.Where(d => !d.loadingDoor).All(d => AngleFromFrame(d) < 0.1f), "all 113 leaves remain closed after Awake/Update");
                sample = doors.Single(d => d.name == "DOOR_Hall_X-18_4");
                closedRotation = sample.transform.rotation;
                sample.Toggle();
                phase = 2;
                started = EditorApplication.timeSinceStartup;
            }
            else if (phase == 2 && elapsed > 1.5)
            {
                Require(sample.IsOpen && Mathf.Abs(Quaternion.Angle(closedRotation, sample.transform.rotation) - 95) < 0.2f,
                    "first toggle opens formerly open-exported hall door by 95 degrees");
                Require(AngleFromFrame(sample) > 80, "toggled leaf is visibly open");
                sample.Toggle();
                phase = 3;
                started = EditorApplication.timeSinceStartup;
            }
            else if (phase == 3 && elapsed > 1.5)
            {
                Require(!sample.IsOpen && AngleFromFrame(sample) < 0.1f, "second toggle closes hall door");
                File.WriteAllText(ReportPath, "{\"passed\":true,\"doors\":119,\"hinged_doors\":113,\"loading_shutters\":6,\"checks\":" + checks +
                    ",\"all_scene_doors_closed\":true,\"play_mode_initial_state_verified\":true,\"formerly_open_door_toggle_cycle_verified\":true,\"scene_saved\":false}");
                Debug.Log("WAREHOUSE_DOORS_VALIDATED " + checks);
                SessionState.SetBool("WarehouseDoorValidationRunning", false);
                EditorApplication.update -= Tick;
                EditorApplication.playModeStateChanged -= ModeChanged;
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(0);
            }
        }
        catch (Exception e) { Fail(e); }
    }

    static void Require(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception(description);
    }

    static void Fail(Exception e)
    {
        Debug.LogException(e);
        SessionState.SetBool("WarehouseDoorValidationRunning", false);
        File.WriteAllText(ReportPath, "{\"passed\":false,\"error\":\"" + e.Message.Replace("\"", "'").Replace("\\", "/") + "\"}");
        EditorApplication.update -= Tick;
        EditorApplication.Exit(1);
    }
}
