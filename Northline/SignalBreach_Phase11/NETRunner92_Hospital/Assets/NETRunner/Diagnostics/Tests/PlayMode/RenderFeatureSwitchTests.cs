using NetRunner.Atmosphere;
using NetRunner.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Diagnostics.Tests
{
    /// <summary>
    /// PlayMode tests of switching rendering features off for a benchmark run:
    /// each feature changes its own camera, light or renderer setting and
    /// nothing else. Ported from main's alpha test 4 with the round 3 features
    /// (#36): fill lights, glass and particles.
    /// </summary>
    public sealed class RenderFeatureSwitchTests
    {
        private const float DefaultFadeDistance = 10000f;
        private const float NearLightDistance = 25f;

        private const string LitShader = "HDRP/Lit";
        private const string SurfaceType = "_SurfaceType";

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

        [Test]
        public void SwitchingOffFillLightsTurnsOffOnlyTheFillLights()
        {
            HDAdditionalLightData fillData = MakeLight(FillLightNames.Light, LightType.Point, out GameObject fill);
            try
            {
                RenderFeatureSwitch.Apply(RenderFeature.FillLights, camera,
                    new[] { lamp.GetComponent<Light>(), sun.GetComponent<Light>(), fill.GetComponent<Light>() });

                Assert.That(fill.GetComponent<Light>().enabled, Is.False);
                Assert.That(lamp.GetComponent<Light>().enabled, Is.True);
                Assert.That(fillData.fadeDistance, Is.EqualTo(DefaultFadeDistance));
            }
            finally
            {
                Object.Destroy(fill);
            }
        }

        [Test]
        public void SwitchingOffGlassHidesOnlySeeThroughSurfaces()
        {
            var glass = new Material(Shader.Find(LitShader));
            glass.SetFloat(SurfaceType, 1f);
            var wall = new Material(Shader.Find(LitShader));
            MeshRenderer pane = MakeRenderer("Test pane", glass);
            MeshRenderer brick = MakeRenderer("Test wall", wall);
            ParticleSystemRenderer rain = new GameObject("Test rain", typeof(ParticleSystem)).GetComponent<ParticleSystemRenderer>();
            try
            {
                RenderFeatureSwitch.Hide(RenderFeature.Glass, new Renderer[] { pane, brick, rain });

                Assert.That(pane.enabled, Is.False);
                Assert.That(brick.enabled, Is.True);
                Assert.That(rain.enabled, Is.True);
            }
            finally
            {
                Object.Destroy(pane.gameObject);
                Object.Destroy(brick.gameObject);
                Object.Destroy(rain.gameObject);
                Object.Destroy(glass);
                Object.Destroy(wall);
            }
        }

        [Test]
        public void SwitchingOffParticlesHidesOnlyParticleSystems()
        {
            var glass = new Material(Shader.Find(LitShader));
            glass.SetFloat(SurfaceType, 1f);
            MeshRenderer pane = MakeRenderer("Test pane", glass);
            ParticleSystemRenderer rain = new GameObject("Test rain", typeof(ParticleSystem)).GetComponent<ParticleSystemRenderer>();
            try
            {
                RenderFeatureSwitch.Hide(RenderFeature.Particles, new Renderer[] { pane, rain });

                Assert.That(rain.enabled, Is.False);
                Assert.That(pane.enabled, Is.True);
            }
            finally
            {
                Object.Destroy(pane.gameObject);
                Object.Destroy(rain.gameObject);
                Object.Destroy(glass);
            }
        }

        private static MeshRenderer MakeRenderer(string name, Material material)
        {
            var renderer = new GameObject(name, typeof(MeshFilter)).AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
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
