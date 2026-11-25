using System;
using NetRunner.Player;
using NetRunner.SceneBuilding.Editor;
using NetRunner.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NetRunner.Hud.Editor
{
    /// <summary>
    /// Gives the player the hospital HUD: a <see cref="HospitalHud"/> under the
    /// player with the HUD layout and its panel, which scales with the screen
    /// from a 1920 × 1080 reference and draws above the tablet's panel, and
    /// links it to the interaction, LATCH-07, the CCTV and the lift. Run again,
    /// it updates the existing HUD.
    /// </summary>
    public static class HudBuilder
    {
        private const string LayoutPath = HospitalAssets.Ui + "/HospitalHud.uxml";
        private const string PanelPath = HospitalAssets.Ui + "/HospitalHud_Panel.asset";
        private const string TabletPanelPath = HospitalAssets.Player + "/Latch07/Latch07_Panel.asset";
        private const float WidthOrHeight = .5f;
        private const int AboveTablet = 10;

        private static readonly Vector2Int ReferenceResolution = new Vector2Int(1920, 1080);

        /// <summary>Installs or updates the HUD on <paramref name="player"/>; the caller saves the scene.</summary>
        public static void Install(FirstPersonController player, SecurityCctv cameras, HospitalLift lift)
        {
            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            if (!player || !cameras || !layout)
            {
                throw new InvalidOperationException("Hospital HUD dependencies are missing.");
            }

            HospitalHud hud = player.GetComponentInChildren<HospitalHud>(true);
            if (!hud)
            {
                hud = SceneParts.Node("Northline hospital HUD", player.transform).gameObject.AddComponent<HospitalHud>();
            }

            UIDocument document = hud.GetComponent<UIDocument>();
            document.panelSettings = Panel();
            document.visualTreeAsset = layout;
            hud.Configure(player.GetComponent<HospitalInteraction>(), player.GetComponent<LatchViewmodel>(), cameras, lift);
        }

        // A copy of the tablet's panel settings that scales with the screen.
        private static PanelSettings Panel()
        {
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel)
            {
                return panel;
            }

            panel = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(TabletPanelPath));
            panel.name = "Hospital HUD panel";
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = ReferenceResolution;
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = WidthOrHeight;
            panel.sortingOrder = AboveTablet;
            AssetDatabase.CreateAsset(panel, PanelPath);
            return panel;
        }
    }
}
