using NetRunner.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Diagnostics.Tests
{
    /// <summary>
    /// PlayMode tests of switching rendering features off for a benchmark run:
    /// each feature changes its own camera or light setting and nothing else.
    /// </summary>
    public sealed class RenderFeatureSwitchTests
    {
        private const float DefaultFadeDistance = 10000f;
        private const float NearLightDistance = 25f;

        private GameObject eyes;
        private GameObject lamp;
        private GameObject sun;
        private Camera camera;
        private HDAdditionalCameraData cameraData;
        private HDAdditionalLightData lampData;
        private HDAdditionalLightData sunData;

        [SetUp]
        public void BuildCameraAndLights()
        {
            eyes = new GameObject("Test eyes", typeof(Camera), typeof(HDAdditionalCameraData));
            camera = eyes.GetComponent<Camera>();
            cameraData = eyes.GetComponent<HDAdditionalCameraData>();
            lampData = MakeLight("Test lamp", LightType.Point, out lamp);
            sunData = MakeLight("Test sun", LightType.Directional, out sun);
        }

        [TearDown]
        public void RemoveCameraAndLights()
        {
            Object.Destroy(eyes);
            Object.Destroy(lamp);
            Object.Destroy(sun);
        }

        [Test]
        public void SwitchingOffNothingChangesNothing()
        {
            Apply(RenderFeature.None);

            Assert.That(camera.useOcclusionCulling, Is.True);
            Assert.That(cameraData.customRenderingSettings, Is.False);
            Assert.That(lampData.affectsVolumetric, Is.True);
            Assert.That(lampData.fadeDistance, Is.EqualTo(DefaultFadeDistance));
        }

        [Test]
        public void SwitchingOffOcclusionCullingStopsTheCameraUsingIt()
        {
            Apply(RenderFeature.OcclusionCulling);

            Assert.That(camera.useOcclusionCulling, Is.False);
            Assert.That(cameraData.customRenderingSettings, Is.False);
        }

        [Test]
        public void EachRenderingFeatureOverridesOnlyItsOwnFrameSetting()
        {
            var cases = new[]
            {
                (RenderFeature.VolumetricFog, FrameSettingsField.Volumetrics),
                (RenderFeature.Shadows, FrameSettingsField.ShadowMaps),
                (RenderFeature.AmbientOcclusion, FrameSettingsField.SSAO),
                (RenderFeature.TransparentObjects, FrameSettingsField.TransparentObjects),
                (RenderFeature.Decals, FrameSettingsField.Decals)
            };
            foreach (var (feature, field) in cases)
            {
                RemoveCameraAndLights();
                BuildCameraAndLights();

                Apply(feature);

                Assert.That(cameraData.customRenderingSettings, Is.True, feature.ToString());
                foreach (var (_, other) in cases)
                {
                    bool overridden = cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)other];
                    Assert.That(overridden, Is.EqualTo(other == field), $"{feature}: {other} overridden");
                }

                Assert.That(cameraData.renderingPathCustomFrameSettings.IsEnabled(field), Is.False, feature.ToString());
            }
        }

        [Test]
        public void SwitchingOffFogLightsTakesLampsOutOfTheFogButNotTheSun()
        {
            Apply(RenderFeature.FogLights);

            Assert.That(lampData.affectsVolumetric, Is.False);
            Assert.That(sunData.affectsVolumetric, Is.True);
            Assert.That(cameraData.customRenderingSettings, Is.False);
        }

        [Test]
        public void SwitchingOffDistantLightsFadesLampsOutPast25Metres()
        {
            Apply(RenderFeature.DistantLights);

            Assert.That(lampData.fadeDistance, Is.EqualTo(NearLightDistance));
            Assert.That(lampData.volumetricFadeDistance, Is.EqualTo(NearLightDistance));
            Assert.That(lampData.affectsVolumetric, Is.True);
            Assert.That(sunData.fadeDistance, Is.EqualTo(DefaultFadeDistance));
        }

        private void Apply(RenderFeature features)
        {
            RenderFeatureSwitch.Apply(features, camera, new[] { lamp.GetComponent<Light>(), sun.GetComponent<Light>() });
        }

        private static HDAdditionalLightData MakeLight(string name, LightType type, out GameObject owner)
        {
            owner = new GameObject(name);
            owner.AddComponent<Light>().type = type;
            return owner.TryGetComponent(out HDAdditionalLightData data) ? data : owner.AddComponent<HDAdditionalLightData>();
        }
    }
}
