using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetRunner.Atmosphere.Tests
{
    /// <summary>
    /// PlayMode tests of a pulsing hospital light: an emergency light swings
    /// smoothly between full and dimmed once per cycle, an intermittent one
    /// flickers on noise, and neither goes outside the range set by its depth.
    /// Expected values come from docs/systems/atmosphere.md.
    /// </summary>
    public sealed class PracticalPulseTests
    {
        private const float Frame = 1f / 60f;
        private const float Nominal = 10f;

        private GameObject lamp;

        [SetUp]
        public void UseEvenFrames()
        {
            // Every frame lasts exactly 1/60 s, however fast the machine runs the test.
            Time.captureDeltaTime = Frame;
        }

        [TearDown]
        public void RemoveLamp()
        {
            Time.captureDeltaTime = 0f;
            Object.Destroy(lamp);
        }

        [UnityTest]
        public IEnumerator AnEmergencyLightSwingsBetweenFullAndDimmedOncePerCycle()
        {
            // One cycle a second and depth 0.5: a 10-unit light swings between 5 and 10.
            var light = Build(PracticalPulse.Pattern.Emergency, 0.5f, 1f, 0f);
            float low = float.MaxValue;
            float high = float.MinValue;

            for (int i = 0; i < 60; i++)
            {
                yield return null;
                low = Mathf.Min(low, light.intensity);
                high = Mathf.Max(high, light.intensity);
            }

            Assert.That(low, Is.EqualTo(Nominal * 0.5f).Within(0.01f));
            Assert.That(high, Is.EqualTo(Nominal).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator AnIntermittentLightFlickersWithinItsDepth()
        {
            // The hospital's intermittent fixtures: depth 0.61, 0.9 noise cycles a second.
            var light = Build(PracticalPulse.Pattern.Intermittent, 0.61f, 0.9f, 3f);
            float low = float.MaxValue;
            float high = float.MinValue;

            for (int i = 0; i < 600; i++)
            {
                yield return null;
                low = Mathf.Min(low, light.intensity);
                high = Mathf.Max(high, light.intensity);
            }

            Assert.That(low, Is.GreaterThanOrEqualTo(Nominal * (1f - 0.61f) - 1e-4f), "Dimmer than its depth allows");
            Assert.That(high, Is.LessThanOrEqualTo(Nominal + 1e-4f), "Brighter than its own intensity");
            Assert.That(low, Is.LessThan(Nominal * 0.99f), "Never dimmed in ten seconds");
        }

        private Light Build(PracticalPulse.Pattern pattern, float depth, float frequency, float phase)
        {
            lamp = new GameObject("Test lamp");
            var light = lamp.AddComponent<Light>();

            // Only the intensity number is tested; the lamp need not light anything.
            light.enabled = false;
            light.intensity = Nominal;
            lamp.AddComponent<PracticalPulse>().Configure(pattern, depth, frequency, phase);
            return light;
        }
    }
}
