using System;
using System.Collections.Generic;
using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Diagnostics
{
    /// <summary>
    /// Switches rendering features off for one benchmark run when the game is
    /// started with <c>-net92-without volumetric-fog,shadows</c> (the names
    /// are in <see cref="RenderFeatureList"/>), so that the run's frame times
    /// show what those features cost on the machine that runs it. Nothing is
    /// saved. Like the other benchmark modes it is not placed in the scene, so
    /// it looks up the player's camera and the lights itself.
    /// </summary>
    public static class RenderFeatureSwitch
    {
        /// <summary>The launch option; the word after it lists the features.</summary>
        public const string Option = "-net92-without";

        // Point and spot lights farther than this from the camera fade out
        // when distant lights are switched off. The hospital's practicals reach
        // at most 8.5 m, so the lights of the room the player is in stay.
        private const float NearLightDistance = 25f;

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
        /// Switches <paramref name="features"/> off for <paramref name="camera"/>
        /// and <paramref name="lights"/>; everything else stays as it is.
        /// </summary>
        public static void Apply(RenderFeature features, Camera camera, IEnumerable<Light> lights)
        {
            if (features == RenderFeature.None)
            {
                return;
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
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyAtStart()
        {
            RenderFeature features = FromLaunchOptions();
            if (features == RenderFeature.None)
            {
                return;
            }

            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();
            if (!player)
            {
                Debug.LogError($"{Option}: no player camera in this scene");
                return;
            }

            Apply(features, player.Eyes, UnityEngine.Object.FindObjectsByType<Light>());
            Debug.Log("NET92 WITHOUT | " + RenderFeatureList.Format(features));
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
