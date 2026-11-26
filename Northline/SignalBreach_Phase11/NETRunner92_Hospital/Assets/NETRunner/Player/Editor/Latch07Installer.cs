using System;
using System.Linq;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace NetRunner.Player.Editor
{
    /// <summary>
    /// Gives the player the LATCH-07 tablet: the viewmodel prefab (from
    /// <see cref="LatchViewmodelBuilder"/>) under the eyes, its live display
    /// material and UI panel, <see cref="LatchScreen"/> and
    /// <see cref="LatchViewmodel"/>. Also places the service relay by the
    /// south gate that the tablet links to.
    /// </summary>
    public static class Latch07Installer
    {
        private const string Folder = HospitalAssets.Player + "/Latch07";
        private const string PrefabPath = Folder + "/Latch07_Viewmodel.prefab";
        private const string DisplayMaterialPath = Folder + "/Latch07_LiveDisplay.mat";
        private const string PanelPath = Folder + "/Latch07_Panel.asset";
        private const string DisplayMesh = "NR92_Display_Surface";
        private const string UnlitShader = "HDRP/Unlit";

        private static readonly Vector3 RelayPosition = new Vector3(-3.55f, 0f, -34.85f);
        private static readonly Color FrameColor = new Color(.09f, .13f, .15f);
        private static readonly Color FasciaColor = new Color(.24f, .30f, .31f);
        private static readonly Color StandbyColor = new Color(.20f, .071f, .025f);
        private static readonly Color StandbyGlow = new Color(1f, .32f, .04f) * 1.1f;
        private const float FrameMetallic = .58f;
        private const float FasciaMetallic = .33f;
        private const float StandbyMetallic = .25f;

        /// <summary>Whether the viewmodel prefab exists, so the tablet can be installed.</summary>
        public static bool IsViewmodelReady()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        /// <summary>
        /// Puts the tablet on <paramref name="player"/> and the relay under
        /// <paramref name="systems"/>. The caller saves the scene.
        /// </summary>
        public static void Install(FirstPersonController player, Transform systems)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab)
            {
                throw new InvalidOperationException("Run LATCH-07 / Prepare Import Check first.");
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.Eyes.transform);
            SkinnedMeshRenderer display = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == DisplayMesh);
            display.sharedMaterial = DisplayMaterial();
            LatchScreen screen = player.gameObject.AddComponent<LatchScreen>();
            screen.Configure(Panel(), display);
            LatchViewmodel latch = player.gameObject.AddComponent<LatchViewmodel>();
            latch.Configure(model, model.GetComponentInChildren<Animator>(true), screen);
            model.SetActive(false);
            AddRelay(systems);
        }

        private static Material DisplayMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DisplayMaterialPath);
            if (!material)
            {
                material = new Material(Shader.Find(UnlitShader)) { name = "LATCH-07 live display" };
                material.SetColor("_UnlitColor", Color.white);
                HDMaterial.ValidateMaterial(material);
                AssetDatabase.CreateAsset(material, DisplayMaterialPath);
            }

            return material;
        }

        private static PanelSettings Panel()
        {
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (!panel)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.name = "LATCH-07 display panel";
                panel.scaleMode = PanelScaleMode.ConstantPixelSize;
                AssetDatabase.CreateAsset(panel, PanelPath);
            }

            return panel;
        }

        // A pedestal with an enclosure whose face the tablet aims at, and a
        // small lamp that turns from amber to cyan once linked.
        private static void AddRelay(Transform systems)
        {
            Transform relay = SceneParts.Node("LATCH-07 Northline service relay", systems);
            relay.position = RelayPosition;
            Material frame = SceneParts.SystemMaterial("Relay frame", FrameColor, FrameMetallic);
            Material fascia = SceneParts.SystemMaterial("Relay fascia", FasciaColor, FasciaMetallic);
            Material standby = SceneParts.SystemMaterial("Relay standby", StandbyColor, StandbyMetallic, StandbyGlow);
            SceneParts.Block(relay, "Relay pedestal", new Vector3(0, .64f, .06f), new Vector3(.18f, 1.28f, .21f), frame);
            SceneParts.Block(relay, "Relay base", new Vector3(0, .045f, .06f), new Vector3(.39f, .09f, .35f), frame);
            SceneParts.Block(relay, "Relay enclosure", new Vector3(0, 1.40f, .035f), new Vector3(.37f, .36f, .17f), frame);
            Transform face = SceneParts.Block(relay, "Relay face", new Vector3(0, 1.42f, -.065f), new Vector3(.32f, .26f, .04f), fascia, false);
            BoxCollider aim = face.gameObject.AddComponent<BoxCollider>();
            aim.isTrigger = true;
            Transform lamp = SceneParts.Block(relay, "Relay link indicator", new Vector3(0, 1.44f, -.09f), new Vector3(.23f, .043f, .016f),
                standby, false);
            face.gameObject.AddComponent<LatchRelay>().Configure(lamp.GetComponent<Renderer>());
        }
    }
}
