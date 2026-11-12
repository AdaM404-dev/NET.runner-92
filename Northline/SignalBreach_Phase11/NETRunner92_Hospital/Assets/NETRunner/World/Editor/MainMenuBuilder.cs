using System;
using System.IO;
using System.Linq;
using NetRunner.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NetRunner.World.Editor
{
    public static partial class HospitalSceneBuilder
    {
        const string MenuAssets = Root + "/Menu";
        public const string MainMenuPath = Root + "/Scenes/MainMenu.unity";
        private const string MenuBuildPath = "../Builds/Phase_11_Frame_Pacing/NETRunner92_Hospital.exe";

        [MenuItem("NET.runner-92/Main Menu/Create startup scene")]
        public static void CreateMainMenu()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(MenuAssets + "/Northline_Research.png");
            var styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(MenuAssets + "/MainMenu.uss");
            var score = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuAssets + "/Deep Atmospheric Pads.mp3");
            var click = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuAssets + "/Northline_MenuClick.wav");
            var schedule = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Environment/Hospital/RoomSchedule.json");
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Atmos + "/Audio/Northline_Ambience.mixer");
            var hudPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(Root + "/UI/HospitalHud_Panel.asset");
            if (!backdrop || !styles || !score || !click || !schedule || !mixer || !hudPanel)
                throw new InvalidOperationException("Missing MainMenu assets.");

            var path = MenuAssets + "/MainMenu_Panel.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (!panel)
            {
                panel = UnityEngine.Object.Instantiate(hudPanel);
                panel.name = "MainMenu panel";
                AssetDatabase.CreateAsset(panel, path);
            }
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 1;
            panel.sortingOrder = 0;
            EditorUtility.SetDirty(panel);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var screen = new GameObject("Menu presentation camera");
            screen.tag = "MainCamera";
            var camera = screen.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.01f, .02f, .03f);
            camera.cullingMask = 0;
            var hdrp = screen.AddComponent<HDAdditionalCameraData>();
            hdrp.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hdrp.backgroundColorHDR = camera.backgroundColor;
            hdrp.customRenderingSettings = true;
            foreach (var setting in new[] { FrameSettingsField.Postprocess, FrameSettingsField.SSAO,
                FrameSettingsField.SSR, FrameSettingsField.Volumetrics, FrameSettingsField.ShadowMaps })
            {
                hdrp.renderingPathCustomFrameSettings.SetEnabled(setting, false);
                hdrp.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)setting] = true;
            }
            screen.AddComponent<AudioListener>();

            var view = new GameObject("Northline main menu");
            view.AddComponent<UIDocument>().panelSettings = panel;
            var backgroundAudio = new GameObject("Menu score").AddComponent<AudioSource>();
            backgroundAudio.transform.SetParent(view.transform);
            backgroundAudio.clip = score;
            backgroundAudio.volume = .1625f;
            backgroundAudio.loop = true;
            backgroundAudio.playOnAwake = true;
            var buttonAudio = new GameObject("Menu interface feedback").AddComponent<AudioSource>();
            buttonAudio.transform.SetParent(view.transform);
            buttonAudio.clip = click;
            buttonAudio.playOnAwake = false;
            view.AddComponent<MainMenuView>().Configure(styles, backdrop, backgroundAudio, buttonAudio,
                CreateOrLoad<NetRunner.Settings.Preferences>(PreferencesPath), CreateOrLoad<RunSession>(RunSessionPath));

            if (!EditorSceneManager.SaveScene(scene, MainMenuPath))
                throw new IOException("Could not save MainMenu scene.");
            var scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true), new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = scenes.Concat(EditorBuildSettings.scenes.Where(s =>
                s.path != MainMenuPath && s.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("NET92 MAIN MENU | Saved startup scene and wired the hospital scene.");
        }

        /// <summary>Builds the saved menu and hospital as a standalone Windows game.</summary>
        [MenuItem("NET.runner-92/Main Menu/Build Windows player")]
        public static void BuildMainMenuWindows()
        {
            if (!File.Exists(MainMenuPath))
            {
                CreateMainMenu();
            }

            BuildWindows(MenuBuildPath, new[] { MainMenuPath, ScenePath }, BuildOptions.None);
            var outputFolder = Path.GetDirectoryName(Path.GetFullPath(MenuBuildPath));
            File.Copy(Root + "/UI/RobotoMono-LICENSE.txt", Path.Combine(outputFolder, "RobotoMono-LICENSE.txt"), true);
        }
    }
}
