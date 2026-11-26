using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NetRunner.Atmosphere;
using NetRunner.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NetRunner.Diagnostics.Tests
{
    /// <summary>
    /// PlayMode tests of the baked lighting in the real hospital (#79). The
    /// lamps whose light changes while the game runs (pulsing lamps, the
    /// residual-power lamps, the lightning, the lift's lamp) stay realtime;
    /// every other lamp is baked; and the scene's lightmaps are loaded.
    /// </summary>
    public sealed class BakedLightingTests
    {
        private const string HospitalScene = "Hospital_Exploration";
        private const int StartupFrames = 3;

        [UnityTest]
        public IEnumerator OnlyTheLampsThatChangeStayLiveAndTheLightmapsAreLoaded()
        {
            yield return SceneManager.LoadSceneAsync(HospitalScene, LoadSceneMode.Single);
            for (int i = 0; i < StartupFrames; i++)
            {
                yield return null;
            }

            var scripted = new HashSet<Light>(Object.FindObjectsByType<ResidualPower>().Select(power => power.DiagnosticFixture).Where(lamp => lamp));
            scripted.Add(Object.FindAnyObjectByType<HospitalAtmosphere>().Lightning);
            Light[] lamps = Object.FindObjectsByType<Light>();
            string[] wrong = lamps.Where(lamp =>
                {
                    bool changes = lamp.GetComponent<PracticalPulse>() || scripted.Contains(lamp) || lamp.GetComponentInParent<HospitalLift>();
                    bool live = !lamp.bakingOutput.isBaked;
                    return changes != live;
                })
                .Select(lamp => lamp.name).ToArray();

            Assert.That(wrong, Is.Empty, "Lamps baked or live against the rule");
            Assert.That(lamps.Count(lamp => !lamp.bakingOutput.isBaked), Is.GreaterThan(0), "No lamp stays live.");
            Assert.That(LightmapSettings.lightmaps.Length, Is.GreaterThan(0), "No lightmaps are loaded.");
        }
    }
}
