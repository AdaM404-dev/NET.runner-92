using System.Linq;
using NetRunner.Player;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using UnityEngine;

namespace NetRunner.World.Editor
{
    public static partial class HospitalSceneBuilder
    {
        const string LatchFolder = Root + "/Player/Latch07";
        const string LatchPrefab = LatchFolder + "/Latch07_Viewmodel.prefab";

        [MenuItem("NET.runner-92/LATCH-07/Install in Hospital")]
        public static void InstallLatch07()
        {
            InstallHospitalSystems();
            var scene = EditorSceneManager.GetActiveScene();
            var player = Object.FindAnyObjectByType<FirstPersonController>();
            if (player.GetComponent<LatchViewmodel>()) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LatchPrefab);
            if (!prefab) throw new System.InvalidOperationException("Run LATCH-07 / Prepare Import Check first.");

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.Eyes.transform);
            var display = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == "NR92_Display_Surface");
            string materialPath = LatchFolder + "/Latch07_LiveDisplay.mat";
            var displayMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!displayMaterial)
            {
                displayMaterial = new Material(Shader.Find("HDRP/Unlit")) { name = "LATCH-07 live display" };
                displayMaterial.SetColor("_UnlitColor", Color.white);
                HDMaterial.ValidateMaterial(displayMaterial);
                AssetDatabase.CreateAsset(displayMaterial, materialPath);
            }
            display.sharedMaterial = displayMaterial;
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(LatchFolder + "/Latch07_Panel.asset");
            if (!panel)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.name = "LATCH-07 display panel";
                panel.scaleMode = PanelScaleMode.ConstantPixelSize;
                AssetDatabase.CreateAsset(panel, LatchFolder + "/Latch07_Panel.asset");
            }
            var screen = player.gameObject.AddComponent<LatchScreen>();
            screen.Configure(panel, display);
            var latch = player.gameObject.AddComponent<LatchViewmodel>();
            latch.Configure(model, model.GetComponentInChildren<Animator>(true), screen);
            model.SetActive(false);

            var systems = GameObject.Find(SystemsRoot).transform;
            var relay = Node("LATCH-07 Northline service relay", systems);
            relay.position = new Vector3(-3.55f, 0f, -34.85f);
            var shell = SystemMaterial("Relay frame", new Color(.09f,.13f,.15f), .58f);
            var plate = SystemMaterial("Relay fascia", new Color(.24f,.30f,.31f), .33f);
            var lens = SystemMaterial("Relay standby", new Color(.20f,.071f,.025f), .25f, new Color(1f,.32f,.04f) * 1.1f);
            Block(relay, "Relay pedestal", new Vector3(0, .64f, .06f), new Vector3(.18f, 1.28f, .21f), shell);
            Block(relay, "Relay base", new Vector3(0, .045f, .06f), new Vector3(.39f, .09f, .35f), shell);
            Block(relay, "Relay enclosure", new Vector3(0, 1.40f, .035f), new Vector3(.37f, .36f, .17f), shell);
            var face = Block(relay, "Relay face", new Vector3(0, 1.42f, -.065f), new Vector3(.32f, .26f, .04f), plate, false);
            var aim = face.gameObject.AddComponent<BoxCollider>();
            aim.isTrigger = true;
            var lamp = Block(relay, "Relay link indicator", new Vector3(0, 1.44f, -.09f), new Vector3(.23f, .043f, .016f), lens, false);
            face.gameObject.AddComponent<LatchRelay>().Configure(lamp.GetComponent<Renderer>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("LATCH07 | Installed viewmodel and optional south forecourt relay.");
        }

        public static void BuildLatchWindows()
        {
            InstallLatch07();
            BuildWindows("../Builds/Phase_06_Latch07/NETRunner92_Hospital.exe");
        }
    }
}
