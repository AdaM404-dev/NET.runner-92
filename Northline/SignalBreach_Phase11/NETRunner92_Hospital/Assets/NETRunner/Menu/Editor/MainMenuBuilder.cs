using System;
using System.IO;
using System.Linq;
using NetRunner.SceneBuilding.Editor;
using NetRunner.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NetRunner.Menu.Editor
{
    /// <summary>
    /// Builds the startup scene <c>MainMenu.unity</c>: no camera, since the
    /// menu is UI and the engine draws it (#36, round 3), an audio listener,
    /// the menu view with its backdrop, styles, the score and the click sound, linked to
    /// the shared preferences and run session. Puts the menu after the studio
    /// intro (when it exists) and before the hospital in Build Settings.
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string BackdropPath = HospitalAssets.Menu + "/Northline_Research.png";
        private const string StylesPath = HospitalAssets.Menu + "/MainMenu.uss";
        private const string ScorePath = HospitalAssets.Menu + "/Deep Atmospheric Pads.mp3";
        private const string ClickPath = HospitalAssets.Menu + "/Northline_MenuClick.wav";
        private const string PanelPath = HospitalAssets.Menu + "/MainMenu_Panel.asset";
        private const string SchedulePath = HospitalAssets.Environment + "/Hospital/RoomSchedule.json";
        private const string MixerPath = HospitalAssets.Atmosphere + "/Audio/Northline_Ambience.mixer";
        private const string HudPanelPath = HospitalAssets.Ui + "/HospitalHud_Panel.asset";
        private const float ScoreVolume = .1625f;

        // The menu panel matches the screen's height, below every other panel.
        private const float MatchHeight = 1;
        private const int Bottom = 0;

        /// <summary>Builds and saves the menu scene; in the editor it first offers to save open changes.</summary>
        [MenuItem("NET.runner-92/Main Menu/Create startup scene")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Texture2D backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(BackdropPath);
            StyleSheet styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesPath);
            AudioClip score = AssetDatabase.LoadAssetAtPath<AudioClip>(ScorePath);
            AudioClip click = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickPath);
            TextAsset schedule = AssetDatabase.LoadAssetAtPath<TextAsset>(SchedulePath);
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            PanelSettings hudPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(HudPanelPath);
            if (!backdrop || !styles || !score || !click || !schedule || !mixer || !hudPanel)
            {
                throw new InvalidOperationException("Missing MainMenu assets.");
            }

            PanelSettings panel = Panel(hudPanel);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddListener();
            var view = new GameObject("Northline main menu");
            view.AddComponent<UIDocument>().panelSettings = panel;
            AudioSource backgroundAudio = new GameObject("Menu score").AddComponent<AudioSource>();
            backgroundAudio.transform.SetParent(view.transform);
            backgroundAudio.clip = score;
            backgroundAudio.volume = ScoreVolume;
            backgroundAudio.loop = true;
            backgroundAudio.playOnAwake = true;
            AudioSource buttonAudio = new GameObject("Menu interface feedback").AddComponent<AudioSource>();
            buttonAudio.transform.SetParent(view.transform);
            buttonAudio.clip = click;
            buttonAudio.playOnAwake = false;
            view.AddComponent<MainMenuView>().Configure(styles, backdrop, backgroundAudio, buttonAudio,
                SceneParts.CreateOrLoad<Preferences>(HospitalAssets.PreferencesPath), SceneParts.CreateOrLoad<RunSession>(HospitalAssets.RunSessionPath));
            if (!EditorSceneManager.SaveScene(scene, HospitalAssets.MainMenuPath))
            {
                throw new IOException("Could not save MainMenu scene.");
            }

            SceneParts.SetBuildOrder();
            AssetDatabase.SaveAssets();
            Debug.Log("NET92 MAIN MENU | Saved startup scene and wired the hospital scene.");
        }

        // The menu's panel, first made as a copy of the HUD's.
        private static PanelSettings Panel(PanelSettings hudPanel)
        {
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (!panel)
            {
                panel = UnityEngine.Object.Instantiate(hudPanel);
                panel.name = "MainMenu panel";
                AssetDatabase.CreateAsset(panel, PanelPath);
            }

            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = MatchHeight;
            panel.sortingOrder = Bottom;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        // No camera: an HDRP camera that draws no objects still runs HDRP's whole
        // frame at full resolution, about 70% of a hospital view's GPU time on Low
        // (#36, round 3). With no camera on, HDRP leaves the UI to the engine.
        // MainMenuView paints the dark backdrop the camera used to clear to.
        private static void AddListener()
        {
            new GameObject("Menu audio listener").AddComponent<AudioListener>();
        }
    }
}
