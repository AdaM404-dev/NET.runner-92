using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using NetRunner.Hud;
using NetRunner.Player;

namespace NetRunner.World.Editor
{
    public static partial class HospitalSceneBuilder
    {
        const string HudFolder = Root + "/UI";

        [MenuItem("NET.runner-92/HUD/Install in Hospital")]
        public static void InstallHud()
        {
            InstallCctv();
            var scene = EditorSceneManager.GetActiveScene();
            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();
            var cameras = UnityEngine.Object.FindAnyObjectByType<SecurityCctv>();
            var lift = UnityEngine.Object.FindAnyObjectByType<HospitalLift>();
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudFolder + "/HospitalHud.uxml");
            if (!player || !cameras || !tree)
                throw new InvalidOperationException("Hospital HUD dependencies are missing.");

            string panelPath = HudFolder + "/HospitalHud_Panel.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (!panel)
            {
                var tabletPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(LatchFolder + "/Latch07_Panel.asset");
                panel = UnityEngine.Object.Instantiate(tabletPanel);
                panel.name = "Hospital HUD panel";
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1920, 1080);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = .5f;
                panel.sortingOrder = 10;
                AssetDatabase.CreateAsset(panel, panelPath);
            }

            var hud = player.GetComponentInChildren<HospitalHud>(true);
            if (!hud) hud = Node("Northline hospital HUD", player.transform).gameObject.AddComponent<HospitalHud>();
            var document = hud.GetComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = tree;
            hud.Configure(player.GetComponent<HospitalInteraction>(), player.GetComponent<LatchViewmodel>(),
                cameras, lift);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("NET92 HUD | Installed the hospital overlay and linked the existing interaction systems.");
        }

        public static void BuildHudWindows()
        {
            BuildWindows("../Builds/Phase_08_HUD_Reference/NETRunner92_Hospital.exe");
        }
    }
}
