using System;
using System.IO;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace NetRunner.Intro.Editor
{
    /// <summary>
    /// Builds the startup scene <c>StudioIntro.unity</c> (#66): a camera that
    /// only clears to black and listens, the intro video with its sound, the
    /// full-screen panel it is shown on, and <see cref="StudioIntro"/>, which
    /// then opens the main menu. Switches Unity's splash screen off and puts
    /// the intro first in Build Settings.
    /// </summary>
    public static class IntroSceneBuilder
    {
        private const string VideoPath = HospitalAssets.Intro + "/Sanhilzon_Studio_Intro_1080p60.webm";
        private const string PanelPath = HospitalAssets.Intro + "/StudioIntro_Panel.asset";
        private const string HudPanelPath = HospitalAssets.Ui + "/HospitalHud_Panel.asset";

        // The intro's panel draws above everything and fills the screen.
        private const int OnTop = 100;
        private const float MatchHeight = 1;

        private static readonly FrameSettingsField[] SkippedEffects =
        {
            FrameSettingsField.Postprocess, FrameSettingsField.SSAO, FrameSettingsField.SSR, FrameSettingsField.Volumetrics,
            FrameSettingsField.ShadowMaps,
        };

        /// <summary>Builds and saves the intro scene, switches the splash screen off and sets the start order.</summary>
        [MenuItem("NET.runner-92/Intro/Create studio intro scene")]
        public static void Create()
        {
            PanelSettings hudPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(HudPanelPath);
            if (!AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath) || !hudPanel)
            {
                throw new InvalidOperationException("The intro video or the HUD panel is missing.");
            }

            // NewScene unloads assets nothing in a scene uses, so the panel is
            // made first and the clip is loaded after it.
            PanelSettings panel = Panel(hudPanel);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
            panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            AddCamera();
            var intro = new GameObject("Sanhilzon Studio intro");
            AudioSource sound = intro.AddComponent<AudioSource>();
            sound.playOnAwake = false;
            VideoPlayer video = intro.AddComponent<VideoPlayer>();
            video.source = VideoSource.VideoClip;
            video.clip = clip;
            video.playOnAwake = false;
            video.isLooping = false;
            video.waitForFirstFrame = true;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.controlledAudioTrackCount = 1;
            video.EnableAudioTrack(0, true);
            video.SetTargetAudioSource(0, sound);
            UIDocument document = intro.AddComponent<UIDocument>();
            document.panelSettings = panel;
            intro.AddComponent<StudioIntro>().Configure(video, document, Path.GetFileNameWithoutExtension(HospitalAssets.MainMenuPath));
            if (!EditorSceneManager.SaveScene(scene, HospitalAssets.IntroScenePath))
            {
                throw new IOException("Could not save the studio intro scene.");
            }

            PlayerSettings.SplashScreen.show = false;
            SceneParts.SetBuildOrder();
            AssetDatabase.SaveAssets();
            Debug.Log("NET92 INTRO | Saved the studio intro scene first in Build Settings; Unity's splash screen is off.");
        }

        // A copy of the HUD's panel settings that fills the screen above every other panel.
        private static PanelSettings Panel(PanelSettings hudPanel)
        {
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (!panel)
            {
                panel = UnityEngine.Object.Instantiate(hudPanel);
                panel.name = "Studio intro panel";
                AssetDatabase.CreateAsset(panel, PanelPath);
            }

            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = MatchHeight;
            panel.sortingOrder = OnTop;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        private static void AddCamera()
        {
            var screen = new GameObject("Intro camera");
            Camera camera = screen.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            HDAdditionalCameraData hdrp = screen.AddComponent<HDAdditionalCameraData>();
            hdrp.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hdrp.backgroundColorHDR = Color.black;
            hdrp.customRenderingSettings = true;
            foreach (FrameSettingsField setting in SkippedEffects)
            {
                hdrp.renderingPathCustomFrameSettings.SetEnabled(setting, false);
                hdrp.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)setting] = true;
            }

            screen.AddComponent<AudioListener>();
        }
    }
}
