using System.Collections;
using System.Linq;
using NUnit.Framework;
using NetRunner.Core;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NetRunner.Settings.Tests
{
    /// <summary>
    /// PlayMode tests of the lights in the fog in the real hospital (#36 round
    /// 3, step 2). Choosing a quality level takes every point and spot light out
    /// of the fog below High and gives it back on High; no light fades out on
    /// any level. Expected values come from Samuel's decisions of 2026-11-26.
    /// Nothing is saved.
    /// </summary>
    public sealed class FogLightsInHospitalTests
    {
        private const string HospitalScene = "Hospital_Exploration";
        private const int StartupFrames = 5;

        // HDRP's default fade distance, which every light in the hospital keeps.
        private const float SceneFade = 10000f;

        // The hospital has 265 point and spot lights with HDRP data; far fewer means the lookup failed.
        private const int LeastLights = 200;

        private Preferences preferences;
        private GamePreferences before;

        [UnitySetUp]
        public IEnumerator LoadTheHospital()
        {
            preferences = Resources.Load<Preferences>(PreferencesRuntime.ResourcePath);
            before = preferences.Active.Copy();
            yield return SceneManager.LoadSceneAsync(HospitalScene, LoadSceneMode.Single);
            for (int i = 0; i < StartupFrames; i++)
            {
                yield return null;
            }
        }

        [TearDown]
        public void RestoreThePreferences()
        {
            preferences.Preview(before);
        }

        [UnityTest]
        public IEnumerator OnlyHighKeepsEveryPointAndSpotLightInTheFog()
        {
            var levels = new[] { (QualityPreset.Low, false), (QualityPreset.Medium, false), (QualityPreset.High, true) };
            foreach (var (level, inFog) in levels)
            {
                preferences.SelectLevel(level, false);
                yield return null;

                HDAdditionalLightData[] lights = Object.FindObjectsByType<HDAdditionalLightData>()
                    .Where(light => light.GetComponent<Light>().type != LightType.Directional).ToArray();
                Assert.That(lights.Length, Is.GreaterThan(LeastLights), "The hospital's lights were not found.");
                Assert.That(lights.Count(light => light.affectsVolumetric == inFog), Is.EqualTo(lights.Length), $"Fog on level {level}");
                Assert.That(lights.Count(light => Mathf.Approximately(light.fadeDistance, SceneFade)), Is.EqualTo(lights.Length),
                    $"A light fades out on level {level}");
            }
        }
    }
}
