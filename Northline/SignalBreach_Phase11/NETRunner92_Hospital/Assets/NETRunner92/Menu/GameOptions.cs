using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace NETRunner92
{
    internal static class GameOptions
    {
        static readonly int[] ShadowSizes = { 256, 512, 1024, 2048 };
        static readonly float[] ShadowDistances = { 0, 18, 36, 60 };
        static readonly List<DisplayInfo> Displays = new();
        static readonly List<Vector2Int> Modes = new();
        static readonly Vector2Int[] WindowSizes =
        {
            new(960, 540), new(1280, 720), new(1280, 800), new(1600, 900), new(1680, 1050),
            new(1920, 1080), new(1920, 1200), new(2560, 1080), new(2560, 1440), new(3440, 1440), new(3840, 2160)
        };
        static GamePreferences saved = new();
        static string SettingsPath => Path.Combine(LocalGameFiles.ProfileDirectory, "settings.json");
        static Camera eyes;
        static HDAdditionalCameraData cameraData;
        static (HDAdditionalLightData light, bool custom, int size)[] lights;
        static VolumeProfile runtimeProfile;
        static HDShadowSettings shadowVolume;
        static ColorAdjustments color;
        static MotionBlur blur;
        static DepthOfField depth;
        static AudioMixer mixer;
        static int appliedShadowSize;
        static bool displayInitialized;

        internal static GamePreferences Saved => saved;
        internal static GamePreferences Active { get; private set; } = saved;
        internal static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Changed = null;
            displayInitialized = false;
            mixer = null;
            saved = new GamePreferences();
            try
            {
                var file = new FileInfo(SettingsPath);
                if (file.Exists && file.Length <= 16384)
                {
                    var loaded = JsonUtility.FromJson<GamePreferences>(File.ReadAllText(SettingsPath));
                    if (loaded?.version == 1) saved = loaded;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Debug.LogWarning($"Northline settings defaults restored: {ex.Message}");
            }
            saved.Validate();
            Active = saved;
            DynamicResolutionHandler.SetDynamicResScaler(() => Active.renderScale,
                DynamicResScalePolicyType.ReturnsPercentage);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            if (runtimeProfile)
            {
                foreach (var component in runtimeProfile.components)
                    UnityEngine.Object.Destroy(component);
                UnityEngine.Object.Destroy(runtimeProfile);
                runtimeProfile = null;
            }
            eyes = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>()?.Eyes;
            cameraData = eyes ? eyes.GetComponent<HDAdditionalCameraData>() : null;
            appliedShadowSize = 2;
            if (cameraData)
            {
                var shadowLights = new List<(HDAdditionalLightData, bool, int)>();
                foreach (var light in UnityEngine.Object.FindObjectsByType<HDAdditionalLightData>())
                    if (light.GetComponent<Light>().shadows != LightShadows.None)
                        shadowLights.Add((light, light.shadowResolution.useOverride, light.shadowResolution.@override));
                lights = shadowLights.ToArray();
                var settings = new GameObject("Runtime HDRP preferences");
                SceneManager.MoveGameObjectToScene(settings, scene);
                var volume = settings.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 100;
                runtimeProfile = volume.profile;
                shadowVolume = runtimeProfile.Add<HDShadowSettings>();
                color = runtimeProfile.Add<ColorAdjustments>();
                blur = runtimeProfile.Add<MotionBlur>();
                depth = runtimeProfile.Add<DepthOfField>();
                depth.nearFocusStart.Override(0f);
                depth.nearFocusEnd.Override(1f);
                depth.farFocusStart.Override(18f);
                depth.farFocusEnd.Override(35f);
            }
            var start = new GameObject("Apply saved preferences").AddComponent<OptionsStartup>();
            SceneManager.MoveGameObjectToScene(start.gameObject, scene);
        }

        internal static void SetMixer(AudioMixer source) => mixer = source;

        internal static IEnumerator ApplyStartup()
        {
            // AudioMixer.SetFloat must run after the mixer has initialized.
            if (!mixer)
                foreach (var candidate in Resources.FindObjectsOfTypeAll<AudioMixer>())
                    if (candidate.name == "Northline_Ambience")
                    {
                        mixer = candidate;
                        break;
                    }
            Preview(saved);
            if (!displayInitialized)
            {
                displayInitialized = true;
                var args = Environment.GetCommandLineArgs();
                if (!Application.isEditor && !Array.Exists(args, x => x.StartsWith("-screen-")))
                    yield return ApplyDisplay(saved);
            }
        }

        internal static void Preview(GamePreferences value)
        {
            Active = value;
            QualitySettings.vSyncCount = value.vsync ? 1 : 0;
            Application.targetFrameRate = value.frameLimit;
            QualitySettings.globalTextureMipmapLimit = value.texture;
            AudioListener.volume = value.mute ? 0 : value.master;
            if (mixer)
            {
                mixer.SetFloat("NorthlineExterior", ToDecibels(value.ambience));
                mixer.SetFloat("NorthlineInterior", ToDecibels(value.ambience));
                mixer.SetFloat("NorthlineEffects", ToDecibels(value.effects));
            }
            if (cameraData)
            {
                eyes.fieldOfView = value.fov;
                cameraData.antialiasing = value.antialias switch
                {
                    1 => HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing,
                    2 => HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                    3 => HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing,
                    _ => HDAdditionalCameraData.AntialiasingMode.None
                };
                cameraData.allowDynamicResolution = value.renderScale < 100;
                cameraData.customRenderingSettings = true;
                SetFrameSetting(FrameSettingsField.ShadowMaps, value.shadows != 0);
                SetFrameSetting(FrameSettingsField.SSAO, value.ao);
                SetFrameSetting(FrameSettingsField.Volumetrics, value.volumetrics);
                SetFrameSetting(FrameSettingsField.Bloom, value.bloom);
                SetFrameSetting(FrameSettingsField.MotionBlur, value.motionBlur && !value.reducedMotion);
                SetFrameSetting(FrameSettingsField.DepthOfField, value.depthOfField);
                shadowVolume.maxShadowDistance.Override(ShadowDistances[value.shadows]);
                color.postExposure.Override(value.brightness);
                blur.intensity.Override(value.motionBlur && !value.reducedMotion ? .20f : 0);
                depth.focusMode.Override(value.depthOfField ? DepthOfFieldMode.Manual : DepthOfFieldMode.Off);
                if (appliedShadowSize != value.shadowResolution)
                {
                    appliedShadowSize = value.shadowResolution;
                    foreach (var (light, custom, size) in lights)
                    {
                        if (!light) continue;
                        bool useScene = value.shadowResolution == 2;
                        light.SetShadowResolution(useScene ? size : ShadowSizes[value.shadowResolution]);
                        light.SetShadowResolutionOverride(!useScene || custom);
                    }
                }
            }
            Changed?.Invoke();
        }

        static void SetFrameSetting(FrameSettingsField field, bool enabled)
        {
            cameraData.renderingPathCustomFrameSettings.SetEnabled(field, enabled);
            cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
        }

        internal static bool Commit(GamePreferences value, out string error)
        {
            try
            {
                value.Validate();
                LocalGameFiles.Write(SettingsPath, JsonUtility.ToJson(value, true));
                saved = value.Copy();
                Preview(saved);
                error = null;
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning(ex.Message);
                error = "A beállításfájl nem írható.";
                return false;
            }
        }

        internal static float ToDecibels(float linear) => linear <= .0001f ? -80 : 20f * Mathf.Log10(linear);

        internal static IReadOnlyList<DisplayInfo> GetDisplays()
        {
            Displays.Clear();
            if (!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer)
                Screen.GetDisplayLayout(Displays);
            return Displays;
        }

        internal static IReadOnlyList<Vector2Int> ResolutionChoices(GamePreferences value)
        {
            GetDisplays();
            Modes.Clear();
            Modes.Add(Vector2Int.zero);
            bool current = Displays.Count <= 1
                || Displays[Mathf.Clamp(value.monitor, 0, Displays.Count - 1)].Equals(Screen.mainWindowDisplayInfo);
            if (current)
                foreach (var mode in Screen.resolutions)
                    AddMode(new Vector2Int(mode.width, mode.height));
            if (!current || value.displayMode == FullScreenMode.Windowed || Modes.Count == 1)
            {
                Vector2Int desktop = Desktop(value.monitor);
                foreach (var size in WindowSizes)
                    if (size.x <= desktop.x && size.y <= desktop.y) AddMode(size);
                AddMode(desktop);
            }
            return Modes;
        }

        static void AddMode(Vector2Int size)
        {
            if (size.x >= 640 && size.y >= 360 && !Modes.Contains(size)) Modes.Add(size);
        }

        static Vector2Int Desktop(int monitor)
        {
            if (Displays.Count != 0)
            {
                var display = Displays[Mathf.Clamp(monitor, 0, Displays.Count - 1)];
                return new Vector2Int(display.width, display.height);
            }
            var mode = Screen.currentResolution;
            return new Vector2Int(mode.width, mode.height);
        }

        internal static IEnumerator ApplyDisplay(GamePreferences value, bool rollback = false)
        {
            if (Application.isEditor) yield break;
            GetDisplays();
            if (Displays.Count > 1)
            {
                var target = Displays[Mathf.Clamp(value.monitor, 0, Displays.Count - 1)];
                if (!target.Equals(Screen.mainWindowDisplayInfo))
                {
                    Screen.fullScreenMode = FullScreenMode.Windowed;
                    yield return null;
                    yield return Screen.MoveMainWindowTo(in target, Vector2Int.zero);
                }
            }

            var size = new Vector2Int(value.width, value.height);
            var desktop = Desktop(value.monitor);
            if (value.displayMode == FullScreenMode.FullScreenWindow || size == Vector2Int.zero)
                size = desktop;
            else if (value.displayMode == FullScreenMode.ExclusiveFullScreen)
            {
                var resolutions = Screen.resolutions;
                if (!Array.Exists(resolutions, r => r.width == size.x && r.height == size.y)) size = desktop;
            }
            else if (size.x > desktop.x || size.y > desktop.y)
                size = new Vector2Int(Mathf.Min(1280, desktop.x), Mathf.Min(720, desktop.y));

            yield return RequestDisplay(size, value.displayMode);
            if (!rollback || MatchesDisplay(size, value.displayMode)) yield break;

            // Windows can defer a return to fullscreen until the player window regains focus.
            while (!Application.isFocused && !MatchesDisplay(size, value.displayMode))
                yield return null;

            for (int attempt = 0; attempt < 3 && !MatchesDisplay(size, value.displayMode); attempt++)
            {
                yield return RequestDisplay(size, value.displayMode);
                yield return new WaitForSecondsRealtime(.45f);
            }

            if (!MatchesDisplay(size, value.displayMode))
                Debug.LogWarning($"Display rollback requested {size.x}×{size.y} {value.displayMode}, " +
                    $"Windows reports {Screen.width}×{Screen.height} {Screen.fullScreenMode}.");
        }

        static IEnumerator RequestDisplay(Vector2Int size, FullScreenMode mode)
        {
            if (Screen.fullScreenMode != mode)
            {
                Screen.fullScreenMode = mode;
                yield return null;
            }
            Screen.SetResolution(size.x, size.y, mode);
            yield return null;
            yield return null;
        }

        static bool MatchesDisplay(Vector2Int size, FullScreenMode mode) =>
            Screen.fullScreenMode == mode && Screen.width == size.x && Screen.height == size.y;
    }

    public sealed class OptionsStartup : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return GameOptions.ApplyStartup();
            Destroy(gameObject);
        }
    }
}
