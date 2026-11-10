using System.Collections;
using NetRunner.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TestTools;

namespace NetRunner.Settings.Tests
{
    /// <summary>
    /// PlayMode tests of the quality switch: it exists without being placed in
    /// a scene, cycles the quality level, and lets screen cameras (not texture
    /// cameras such as CCTV) use resolution scaling.
    /// </summary>
    public sealed class QualitySwitchTests
    {
        private int levelBefore;

        [SetUp]
        public void RememberLevel()
        {
            levelBefore = QualitySettings.GetQualityLevel();
        }

        [TearDown]
        public void RestoreLevel()
        {
            QualitySettings.SetQualityLevel(levelBefore, true);
        }

        [Test]
        public void TheProjectHasLowMediumAndHigh()
        {
            Assert.That(QualitySettings.names, Is.EqualTo(new[] { "Low", "Medium", "High" }));
        }

        [UnityTest]
        public IEnumerator ExistsWithoutBeingPlacedInAScene()
        {
            yield return null;

            Assert.That(Object.FindAnyObjectByType<QualitySwitch>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator CyclingSwitchesToTheNextLevel()
        {
            yield return null;
            var qualitySwitch = Object.FindAnyObjectByType<QualitySwitch>();
            int expected = QualityChoice.Next(QualitySettings.GetQualityLevel(), QualitySettings.count);

            qualitySwitch.CycleQuality();

            Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator AScreenCameraIsAllowedResolutionScaling()
        {
            var host = new GameObject("Test screen camera", typeof(Camera), typeof(HDAdditionalCameraData));
            var cameraData = host.GetComponent<HDAdditionalCameraData>();
            cameraData.allowDynamicResolution = false;
            yield return null;

            // Batch-mode test runs have no window, so cameras only draw when asked.
            host.GetComponent<Camera>().Render();

            Assert.That(cameraData.allowDynamicResolution, Is.True);
            Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator ATextureCameraKeepsItsOwnResolution()
        {
            var texture = new RenderTexture(64, 64, 24);
            var host = new GameObject("Test texture camera", typeof(Camera), typeof(HDAdditionalCameraData));
            var textureCamera = host.GetComponent<Camera>();
            textureCamera.targetTexture = texture;
            var cameraData = host.GetComponent<HDAdditionalCameraData>();
            cameraData.allowDynamicResolution = false;
            yield return null;

            textureCamera.Render();

            Assert.That(cameraData.allowDynamicResolution, Is.False);
            Object.Destroy(host);
            texture.Release();
        }
    }
}
