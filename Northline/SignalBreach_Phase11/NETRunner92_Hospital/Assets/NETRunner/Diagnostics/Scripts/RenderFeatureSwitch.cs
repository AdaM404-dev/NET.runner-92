using System;
using System.Collections.Generic;
using NetRunner.Atmosphere;
using NetRunner.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Diagnostics
{
    /// <summary>
    /// Switches rendering features off for one benchmark run when the game is
    /// started with <c>-net92-without fill-lights,glass</c> (the names are in
    /// <see cref="RenderFeatureList"/>), and picks the upscaler with
    /// <c>-net92-upscaler fsr1</c> (<see cref="UpscalerList"/>), so that the
    /// run's frame times show what each costs on the machine that runs it.
    /// Ported from main's alpha test 4 for #36 round 3. Nothing is saved.
    /// Phase 11's benchmark starts at the menu and loads the hospital itself,
    /// so <see cref="PerformanceCapture"/> calls <see cref="ApplyFromLaunchOptions"/>
    /// once the hospital and the player's preferences are in place.
    /// </summary>
    public static class RenderFeatureSwitch
    {
        /// <summary>The launch option; the word after it lists the features.</summary>
        public const string Option = "-net92-without";

        /// <summary>The launch option; the word after it names the upscaler.</summary>
        public const string UpscalerOption = "-net92-upscaler";

        /// <summary>The launch option; the number after it (0 to 1) replaces the fog's GI dimmer for tuning it (#79).</summary>
        public const string FogGiOption = "-net92-fog-gi";

        // Above the preferences' runtime volume (priority 100), so the tuning value wins.
        private const int TuningPriority = 1000;

        // Point and spot lights farther than this from the camera fade out
        // when distant lights are switched off. The hospital's practicals reach
        // at most 8.5 m, so the lights of the room the player is in stay.
        private const float NearLightDistance = 25f;

        // HDRP's Lit materials mark see-through surfaces with this property.
        private const string SurfaceType = "_SurfaceType";
        private const float TransparentSurface = 1f;

        /// <summary>
        /// The features this start of the game switches off. Unknown names are
        /// logged as an error and switch nothing off.
        /// </summary>
        public static RenderFeature FromLaunchOptions()
        {
            string list = LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), Option);
            RenderFeature features = RenderFeatureList.Parse(list, out IReadOnlyList<string> unknown);
            if (unknown.Count > 0)
            {
                Debug.LogError($"{Option}: unknown feature {string.Join(", ", unknown)}");
            }

            return features;
        }

        /// <summary>
        /// The upscaler this start of the game uses. An unknown name is logged
        /// as an error and keeps the pipeline's own.
        /// </summary>
        public static Upscaler UpscalerFromLaunchOptions()
        {
            string name = LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), UpscalerOption);
            if (!UpscalerList.TryParse(name, out Upscaler upscaler))
            {
                Debug.LogError($"{UpscalerOption}: unknown upscaler {name}");
            }

            return upscaler;
        }

        /// <summary>
        /// Applies the launch options to <paramref name="camera"/> and to the
        /// lights and renderers of the open scenes. Returns what was applied,
        /// for the first line of the benchmark's results.
        /// </summary>
        public static string ApplyFromLaunchOptions(Camera camera)
        {
            RenderFeature features = FromLaunchOptions();
            Upscaler upscaler = UpscalerFromLaunchOptions();
            int lights = Apply(features, camera, UnityEngine.Object.FindObjectsByType<Light>());
            int renderers = Hide(features, UnityEngine.Object.FindObjectsByType<Renderer>());
            SetUpscaler(upscaler, camera);
            string applied = $"without: {RenderFeatureList.Format(features)}; upscaler: {UpscalerList.Format(upscaler)}";
            string fogGi = LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), FogGiOption);
            if (fogGi != null && float.TryParse(fogGi, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float dimmer))
            {
                var tuning = new GameObject("Benchmark fog tuning").AddComponent<Volume>();
                tuning.isGlobal = true;
                tuning.priority = TuningPriority;
                tuning.profile.Add<Fog>().globalLightProbeDimmer.Override(Mathf.Clamp01(dimmer));
                applied += $"; fog GI: {Mathf.Clamp01(dimmer):0.##}";
            }

            Debug.Log($"NET92 WITHOUT | {applied} | {lights} lights and {renderers} renderers changed");
            return applied;
        }

        /// <summary>
        /// Switches <paramref name="features"/> off for <paramref name="camera"/>
        /// and <paramref name="lights"/>; everything else stays as it is.
        /// Returns how many lights it changed, for the log.
        /// </summary>
        public static int Apply(RenderFeature features, Camera camera, IEnumerable<Light> lights)
        {
            int changed = 0;
            if (features == RenderFeature.None)
            {
                return changed;
            }

            if ((features & RenderFeature.OcclusionCulling) != 0)
            {
                camera.useOcclusionCulling = false;
            }

            HDAdditionalCameraData cameraData = HdData<HDAdditionalCameraData>(camera.gameObject);
            SwitchOff(cameraData, FrameSettingsField.Volumetrics, (features & RenderFeature.VolumetricFog) != 0);
            SwitchOff(cameraData, FrameSettingsField.ShadowMaps, (features & RenderFeature.Shadows) != 0);
            SwitchOff(cameraData, FrameSettingsField.SSAO, (features & RenderFeature.AmbientOcclusion) != 0);
            SwitchOff(cameraData, FrameSettingsField.TransparentObjects, (features & RenderFeature.TransparentObjects) != 0);
            SwitchOff(cameraData, FrameSettingsField.Decals, (features & RenderFeature.Decals) != 0);

            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    continue;
                }

                if ((features & RenderFeature.FillLights) != 0 && light.name == FillLightNames.Light)
                {
                    light.enabled = false;
                    changed++;
                    continue;
                }

                if ((features & (RenderFeature.FogLights | RenderFeature.DistantLights)) == 0)
                {
                    continue;
                }

                HDAdditionalLightData lightData = HdData<HDAdditionalLightData>(light.gameObject);
                if ((features & RenderFeature.FogLights) != 0)
                {
                    lightData.affectsVolumetric = false;
                }

                if ((features & RenderFeature.DistantLights) != 0)
                {
                    lightData.fadeDistance = NearLightDistance;
                    lightData.volumetricFadeDistance = NearLightDistance;
                }

                changed++;
            }

            return changed;
        }

        /// <summary>
        /// Hides the <paramref name="renderers"/> that <paramref name="features"/>
        /// covers: the see-through surfaces for <see cref="RenderFeature.Glass"/>,
        /// the particle systems for <see cref="RenderFeature.Particles"/>.
        /// Returns how many it hid, for the log.
        /// </summary>
        public static int Hide(RenderFeature features, IEnumerable<Renderer> renderers)
        {
            int hidden = 0;
            bool glass = (features & RenderFeature.Glass) != 0;
            bool particles = (features & RenderFeature.Particles) != 0;
            if (!glass && !particles)
            {
                return hidden;
            }

            foreach (Renderer renderer in renderers)
            {
                if ((particles && renderer is ParticleSystemRenderer) || (glass && renderer is MeshRenderer && IsSeeThrough(renderer)))
                {
                    renderer.enabled = false;
                    hidden++;
                }
            }

            return hidden;
        }

        /// <summary>Makes <paramref name="camera"/> scale up with <paramref name="upscaler"/>; the default changes nothing.</summary>
        public static void SetUpscaler(Upscaler upscaler, Camera camera)
        {
            if (upscaler == Upscaler.Default)
            {
                return;
            }

            DynamicResolutionHandler.SetUpscaleFilter(camera,
                upscaler == Upscaler.Fsr1 ? DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres : DynamicResUpscaleFilter.CatmullRom);
        }

        private static bool IsSeeThrough(Renderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material && material.HasFloat(SurfaceType) && Mathf.Approximately(material.GetFloat(SurfaceType), TransparentSurface))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SwitchOff(HDAdditionalCameraData cameraData, FrameSettingsField field, bool switchOff)
        {
            if (!switchOff)
            {
                return;
            }

            cameraData.customRenderingSettings = true;
            cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
            cameraData.renderingPathCustomFrameSettings.SetEnabled(field, false);
        }

        private static T HdData<T>(GameObject owner) where T : Component
        {
            return owner.TryGetComponent(out T data) ? data : owner.AddComponent<T>();
        }
    }
}
