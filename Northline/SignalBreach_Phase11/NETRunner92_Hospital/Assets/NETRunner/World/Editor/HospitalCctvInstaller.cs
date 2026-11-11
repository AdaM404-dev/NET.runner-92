using System.Linq;
using NetRunner.Player;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

namespace NetRunner.World.Editor
{
    public static partial class HospitalSceneBuilder
    {
        const int ViewmodelLayer = 9;
        const string ViewmodelLayerName = "First-person viewmodel";

        [MenuItem("NET.runner-92/CCTV/Install in Hospital")]
        public static void InstallCctv()
        {
            InstallLatch07();
            if (Object.FindAnyObjectByType<SecurityCctv>()) return;
            var scene = EditorSceneManager.GetActiveScene();
            var latch = Object.FindAnyObjectByType<LatchViewmodel>();
            var desk = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Single(r => r.name == "F01_FF_040_Reception_Counter");
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var slot = tags.FindProperty("layers").GetArrayElementAtIndex(ViewmodelLayer);
            if (slot.stringValue != "" && slot.stringValue != ViewmodelLayerName)
                throw new System.InvalidOperationException("CCTV viewmodel layer slot is already occupied.");
            slot.stringValue = ViewmodelLayerName;
            tags.ApplyModifiedPropertiesWithoutUndo();
            foreach (var part in latch.Model.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = ViewmodelLayer;
                PrefabUtility.RecordPrefabInstancePropertyModifications(part.gameObject);
            }

            var root = Node("Northline CCTV network", GameObject.Find(SystemsRoot).transform);
            var camera = Node("CCTV selected feed camera", root).gameObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.depth = -8f;
            camera.nearClipPlane = .075f;
            camera.farClipPlane = 64f;
            camera.fieldOfView = 54f;
            camera.cullingMask = ~(1 << ViewmodelLayer);
            var distances = new float[32];
            distances[8] = 24f;
            camera.layerCullDistances = distances;
            var hd = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            hd.customRenderingSettings = true;
            foreach (var setting in new[] { FrameSettingsField.SSR, FrameSettingsField.SSAO,
                FrameSettingsField.Volumetrics, FrameSettingsField.MotionVectors, FrameSettingsField.ContactShadows,
                FrameSettingsField.PlanarProbe })
            {
                hd.renderingPathCustomFrameSettings.SetEnabled(setting, false);
                hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)setting] = true;
            }
            var views = Node("Virtual security viewpoints", root);
            var positions = new (string name, Vector3 at, Vector3 aim)[]
            {
                ("PUBLIC ENTRY", new Vector3(-4.9f,3.18f,-23.2f), new Vector3(0,1.45f,-20.2f)),
                ("RECEPTION", new Vector3(3.8f,3.15f,-12.8f), new Vector3(-5.6f,1.24f,-6.8f)),
                ("EMERGENCY CONCOURSE", new Vector3(-29f,3.14f,1.6f), new Vector3(-10f,1.47f,0)),
                ("PATIENT WARDS", new Vector3(-22f,7.34f,1.56f), new Vector3(-4f,5.62f,0)),
                ("SURGERY CONCOURSE", new Vector3(-11f,11.52f,-1.56f), new Vector3(9f,9.78f,0)),
                ("RESEARCH ACCESS", new Vector3(7.3f,15.70f,1.58f), new Vector3(13f,13.95f,0))
            };
            var feeds = positions.Select((p, index) =>
            {
                var view = Node($"CAM {index + 1:00} / {p.name}", views);
                view.SetPositionAndRotation(p.at, Quaternion.LookRotation(p.aim - p.at));
                return new SecurityCctv.Feed { label = p.name, view = view };
            }).ToArray();
            var network = root.gameObject.AddComponent<SecurityCctv>();
            network.Configure(camera, feeds);
            latch.SetCctv(network);

            var terminal = Node("Reception right monitor / CCTV terminal", root);
            terminal.SetPositionAndRotation(desk.transform.TransformPoint(new Vector3(.62f, 1.18f, .365f)),
                desk.transform.rotation);
            var aim = terminal.gameObject.AddComponent<BoxCollider>();
            aim.center = new Vector3(0, 0, .072f);
            aim.size = new Vector3(.46f, .28f, .145f);
            aim.isTrigger = true;
            var lens = SystemMaterial("Reception CCTV standby", new Color(.19f,.075f,.022f), .15f,
                new Color(1f,.38f,.055f) * 1.3f);
            var marker = Block(terminal, "CCTV link status", new Vector3(0, -.011f, -.031f),
                new Vector3(.32f,.018f,.008f), lens, false);
            terminal.gameObject.AddComponent<ReceptionSecurityTerminal>().Configure(network, marker.GetComponent<Renderer>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"NET92 CCTV | Reused the reception's right monitor and installed {feeds.Length} virtual views.");
        }

        public static void BuildCctvWindows()
        {
            InstallCctv();
            BuildWindows("../Builds/Phase_07_CCTV/NETRunner92_Hospital.exe");
        }
    }
}
