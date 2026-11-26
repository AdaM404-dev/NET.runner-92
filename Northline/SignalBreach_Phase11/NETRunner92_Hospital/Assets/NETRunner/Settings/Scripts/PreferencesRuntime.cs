using System;
using System.Collections;
using System.Collections.Generic;
using NetRunner.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace NetRunner.Settings
{
    /// <summary>
    /// Applies the <see cref="Preferences"/> in effect to the engine (Phase
    /// 11's settings): quality level, VSync and frame cap, texture limit,
    /// volumes, on the player's camera the field of view, antialiasing,
    /// render scale, effects and shadows, and whether the scene's point and
    /// spot lights light the fog (<see cref="FogLights"/>). Created when the game starts, with
    /// the F2 <see cref="QualitySwitch"/> beside it, so no scene needs it; it
    /// is the one place that loads the preferences asset. Each scene that
    /// loads gets a global volume for brightness, motion blur, depth of field
    /// and shadow distance.
    /// </summary>
    public sealed class PreferencesRuntime : MonoBehaviour
    {
        /// <summary>The preferences asset's path under a Resources folder.</summary>
        public const string ResourcePath = "Preferences";

        private const string ScreenOptionPrefix = "-screen-";
        private const string MainMenuScene = "MainMenu";
        private const float RefreshCheckSeconds = 1f;
        private const int VolumePriority = 100;

        // The shadow resolution step that keeps each light's own resolution from the scene.
        private const int SceneShadowResolution = 2;

        // No quality level's fog setting is applied to the scene's lights yet.
        private const int NoLevel = -1;

        private const float MotionBlurIntensity = .20f;
        private const string EffectsGroup = "NorthlineEffects";

        // Depth of field keeps 1 to 18 m sharp and blurs fully beyond 35 m.
        private const float NearFocusEnd = 1f;
        private const float FarFocusStart = 18f;
        private const float FarFocusEnd = 35f;

        // Devices without hardware dynamic resolution, where HDRP cannot run STP, its
        // temporal upscaler. Mirrors HDRP's HDUtils.IsHardwareDynamicResolutionSupportedByDevice, which is internal.
        private static readonly GraphicsDeviceType[] SoftwareScalingDevices =
        {
            GraphicsDeviceType.Direct3D11, GraphicsDeviceType.OpenGLCore, GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.WebGPU,
        };

        private static readonly int[] ShadowSizes = { 256, 512, 1024, 2048 };
        private static readonly float[] ShadowDistances = { 0f, 18f, 36f, 60f };
        private static readonly string[] AmbienceGroups = { "NorthlineExterior", "NorthlineInterior" };

        private readonly DisplayModes display = new DisplayModes();
        private readonly List<(HDAdditionalLightData Light, bool Custom, int Size, float Fade)> shadowLights =
            new List<(HDAdditionalLightData, bool, int, float)>();

        // The point and spot lights, with the fog setting the scene gave them.
        private readonly List<(HDAdditionalLightData Light, bool InFog)> localLights = new List<(HDAdditionalLightData, bool)>();

        private Preferences preferences;
        private HDAdditionalCameraData cameraData;
        private VolumeProfile runtimeProfile;
        private HDShadowSettings shadowVolume;
        private Fog fog;
        private ColorAdjustments color;
        private MotionBlur blur;
        private DepthOfField depth;
        private int appliedShadowResolution;
        private int appliedShadowDistance = NoLevel;
        private int appliedLightLevel = NoLevel;
        private bool displayApplied;
        private bool isMainMenu;
        private double displayRefreshRate;
        private float nextRefreshCheck;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Update()
        {
            if (isMainMenu || !preferences || preferences.MenuShown || preferences.Active == null || !preferences.Active.VsyncEnabled
                || Time.unscaledTime < nextRefreshCheck)
            {
                return;
            }

            nextRefreshCheck = Time.unscaledTime + RefreshCheckSeconds;
            double refreshRate = Screen.mainWindowDisplayInfo.refreshRate.value;
            if (refreshRate != displayRefreshRate)
            {
                ApplyFramePacing(preferences.Active, refreshRate);
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (preferences)
            {
                preferences.Changed -= Apply;
                preferences.MenuShownChanged -= HandleMenuShown;
            }
        }

        /// <summary>Applies <paramref name="source"/> from now on; the render scale follows its setting.</summary>
        public void Configure(Preferences source)
        {
            preferences = source;
            preferences.Changed += Apply;
            preferences.MenuShownChanged += HandleMenuShown;
            DynamicResolutionHandler.SetDynamicResScaler(() => preferences.Active.RenderScale, DynamicResScalePolicyType.ReturnsPercentage);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnStart()
        {
            var preferences = Resources.Load<Preferences>(ResourcePath);
            if (!preferences)
            {
                Debug.LogError($"The preferences asset is missing from Resources/{ResourcePath}.");
                return;
            }

            preferences.Load();
            var host = new GameObject(nameof(PreferencesRuntime));
            DontDestroyOnLoad(host);
            host.AddComponent<PreferencesRuntime>().Configure(preferences);
            host.AddComponent<QualitySwitch>().Configure(preferences);
            host.AddComponent<PerformanceOverlay>().Configure(preferences);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive || !preferences)
            {
                return;
            }

            RemoveRuntimeVolume();
            isMainMenu = scene.name == MainMenuScene;
            ApplyFramePacing(preferences.StartingPreferences, Screen.mainWindowDisplayInfo.refreshRate.value);
            var eyes = preferences.PlayerCamera;
            cameraData = eyes ? eyes.GetComponent<HDAdditionalCameraData>() : null;
            appliedShadowResolution = SceneShadowResolution;
            appliedShadowDistance = NoLevel;
            appliedLightLevel = NoLevel;
            if (cameraData)
            {
                CollectShadowLights(scene);
                CollectLocalLights(scene);
                CreateRuntimeVolume(scene);
            }

            StartCoroutine(ApplyStartup());
        }

        // The mixer only accepts volumes once it has started, so wait a frame.
        private IEnumerator ApplyStartup()
        {
            yield return null;
            preferences.Preview(preferences.StartingPreferences);
            if (displayApplied)
            {
                yield break;
            }

            displayApplied = true;
            if (!Application.isEditor && !Array.Exists(Environment.GetCommandLineArgs(), x => x.StartsWith(ScreenOptionPrefix)))
            {
                yield return display.Apply(preferences.Saved);
            }
        }

        private void Apply()
        {
            var value = preferences.Active;

            // The level first: switching level resets VSync, texture limit and the pipeline asset, which the lines below then set.
            if (QualitySettings.GetQualityLevel() != value.Quality && value.Quality < QualitySettings.count)
            {
                QualitySettings.SetQualityLevel(value.Quality, true);
            }

            ApplyFramePacing(value, Screen.mainWindowDisplayInfo.refreshRate.value);
            QualitySettings.globalTextureMipmapLimit = value.Texture;
            AudioListener.volume = value.Muted ? 0f : value.Master;
            var mixer = preferences.Mixer;
            if (mixer)
            {
                foreach (string group in AmbienceGroups)
                {
                    mixer.SetFloat(group, PreferenceRules.ToDecibels(value.Ambience));
                }

                mixer.SetFloat(EffectsGroup, PreferenceRules.ToDecibels(value.Effects));
            }

            if (cameraData)
            {
                ApplyToCamera(value);
            }
        }

        private void ApplyToCamera(GamePreferences value)
        {
            bool motionBlur = value.MotionBlurEnabled && !value.ReduceMotion;
            preferences.PlayerCamera.fieldOfView = value.FieldOfView;
            cameraData.antialiasing = value.Antialias switch
            {
                1 => HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing,
                2 => HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                3 => HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing,
                _ => HDAdditionalCameraData.AntialiasingMode.None,
            };
            cameraData.allowDynamicResolution = QualityPreset.UsesDynamicResolution(value.RenderScale);
            cameraData.customRenderingSettings = true;
            bool motionVectors = MotionVectorRule.AreNeeded(value.Antialias, motionBlur, value.AmbientOcclusionEnabled,
                HasTemporalUpscaler(value.RenderScale));
            // The hospital has no decal projectors. Skip its decal depth prepass.
            SetFrameSetting(FrameSettingsField.Decals, false);
            // No material refracts or distorts, so skip the colour pyramid HDRP builds for them every frame (#36 round 3).
            SetFrameSetting(FrameSettingsField.Refraction, false);
            SetFrameSetting(FrameSettingsField.Distortion, false);
            SetFrameSetting(FrameSettingsField.ShadowMaps, value.Shadows != 0);
            SetFrameSetting(FrameSettingsField.SSAO, value.AmbientOcclusionEnabled);
            SetFrameSetting(FrameSettingsField.Volumetrics, value.VolumetricsEnabled);
            SetFrameSetting(FrameSettingsField.Bloom, value.BloomEnabled);
            SetFrameSetting(FrameSettingsField.MotionBlur, motionBlur);
            SetFrameSetting(FrameSettingsField.DepthOfField, value.DepthOfFieldEnabled);
            SetFrameSetting(FrameSettingsField.MotionVectors, motionVectors);
            SetFrameSetting(FrameSettingsField.ObjectMotionVectors, motionVectors);
            shadowVolume.maxShadowDistance.Override(ShadowDistances[value.Shadows]);
            ApplyShadowFade(value.Shadows);
            color.postExposure.Override(value.Brightness);
            blur.intensity.Override(motionBlur ? MotionBlurIntensity : 0f);
            depth.focusMode.Override(value.DepthOfFieldEnabled ? DepthOfFieldMode.Manual : DepthOfFieldMode.Off);
            ApplyFogLights(value.Quality);
            if (appliedShadowResolution == value.ShadowResolution)
            {
                return;
            }

            appliedShadowResolution = value.ShadowResolution;
            bool useScene = value.ShadowResolution == SceneShadowResolution;
            foreach (var (light, custom, size, _) in shadowLights)
            {
                if (!light)
                {
                    continue;
                }

                light.SetShadowResolution(useScene ? size : ShadowSizes[value.ShadowResolution]);
                light.SetShadowResolutionOverride(!useScene || custom);
            }
        }

        private void ApplyFramePacing(GamePreferences value, double refreshRate)
        {
            displayRefreshRate = refreshRate;
            bool menuShown = isMainMenu || preferences.MenuShown;
            int frameLimit = FramePacing.LimitFor(menuShown, value.FrameLimit);
            QualitySettings.vSyncCount = FramePacing.SyncInterval(!menuShown && value.VsyncEnabled, frameLimit, refreshRate);
            Application.targetFrameRate = frameLimit;
        }

        // HDRP runs STP below 100 % wherever the device has hardware dynamic resolution.
        private static bool HasTemporalUpscaler(int renderScale)
        {
            return QualityPreset.UsesDynamicResolution(renderScale)
                && Array.IndexOf(SoftwareScalingDevices, SystemInfo.graphicsDeviceType) < 0;
        }

        private void SetFrameSetting(FrameSettingsField field, bool enabled)
        {
            cameraData.renderingPathCustomFrameSettings.SetEnabled(field, enabled);
            cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
        }

        // Point and spot lights stop drawing their shadow maps beyond the level's
        // shadow distance, where the volume has faded those shadows out anyway
        // (#36 round 3); with shadows off the scene's fade distance stays.
        private void ApplyShadowFade(int step)
        {
            if (appliedShadowDistance == step)
            {
                return;
            }

            appliedShadowDistance = step;
            foreach (var (light, _, _, fade) in shadowLights)
            {
                if (light && light.GetComponent<Light>().type != LightType.Directional)
                {
                    light.shadowFadeDistance = step > 0 ? Mathf.Min(fade, ShadowDistances[step]) : fade;
                }
            }
        }

        private void HandleMenuShown()
        {
            ApplyFramePacing(preferences.Active, Screen.mainWindowDisplayInfo.refreshRate.value);
        }

        // Takes the point and spot lights out of the fog below High (FogLights);
        // High gives every light back the scene's setting.
        private void ApplyFogLights(int level)
        {
            if (appliedLightLevel == level)
            {
                return;
            }

            appliedLightLevel = level;
            bool lightsInFog = FogLights.AreOnFor(level);
            foreach (var (light, inFog) in localLights)
            {
                if (light)
                {
                    light.affectsVolumetric = lightsInFog && inFog;
                }
            }
        }

        // The point and spot lights of the scene that just loaded, with their own fog setting.
        private void CollectLocalLights(Scene scene)
        {
            localLights.Clear();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var light in root.GetComponentsInChildren<HDAdditionalLightData>(true))
                {
                    if (light.GetComponent<Light>().type != LightType.Directional)
                    {
                        localLights.Add((light, light.affectsVolumetric));
                    }
                }
            }
        }

        // The shadow-casting lights of the scene that just loaded, with the resolution the scene gave them.
        private void CollectShadowLights(Scene scene)
        {
            shadowLights.Clear();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var light in root.GetComponentsInChildren<HDAdditionalLightData>(true))
                {
                    if (light.GetComponent<Light>().shadows != LightShadows.None)
                    {
                        shadowLights.Add((light, light.shadowResolution.useOverride, light.shadowResolution.@override, light.shadowFadeDistance));
                    }
                }
            }
        }

        private void CreateRuntimeVolume(Scene scene)
        {
            var settings = new GameObject("Runtime HDRP preferences");
            SceneManager.MoveGameObjectToScene(settings, scene);
            var volume = settings.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = VolumePriority;
            runtimeProfile = volume.profile;
            shadowVolume = runtimeProfile.Add<HDShadowSettings>();
            fog = runtimeProfile.Add<Fog>();
            fog.globalLightProbeDimmer.Override(FogLights.BakedGiDimmer);
            color = runtimeProfile.Add<ColorAdjustments>();
            blur = runtimeProfile.Add<MotionBlur>();
            depth = runtimeProfile.Add<DepthOfField>();
            depth.nearFocusStart.Override(0f);
            depth.nearFocusEnd.Override(NearFocusEnd);
            depth.farFocusStart.Override(FarFocusStart);
            depth.farFocusEnd.Override(FarFocusEnd);
        }

        private void RemoveRuntimeVolume()
        {
            if (!runtimeProfile)
            {
                return;
            }

            foreach (var component in runtimeProfile.components)
            {
                Destroy(component);
            }

            Destroy(runtimeProfile);
            runtimeProfile = null;
        }
    }
}
