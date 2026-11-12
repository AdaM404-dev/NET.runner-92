using System;
using System.Collections;
using NetRunner.Player.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetRunner.World.Tests
{
    /// <summary>
    /// PlayMode test of a residual-power lamp on a test-built light: every
    /// frame its intensity follows the outage cycle in
    /// docs/systems/level-hospital.md, which the test computes on its own
    /// from the time the frame ran at.
    /// </summary>
    public sealed class ResidualPowerTests
    {
        private const float Nominal = 10f;
        private const float Phase = 1.7f;

        private TestWorld world;

        [SetUp]
        public void Build()
        {
            world = new TestWorld();
            world.HideHospital();
        }

        [TearDown]
        public void Clear()
        {
            Time.captureDeltaTime = 0f;
            world.Clear();
        }

        [UnityTest]
        public IEnumerator TheLampFollowsItsOutageCycle()
        {
            // 0.05 s frames sample every part of the 11.65 s cycle within 15 s.
            Time.captureDeltaTime = .05f;
            GameObject host = world.Empty("Test power", Vector3.zero);
            host.SetActive(false);
            var lamp = host.AddComponent<Light>();
            lamp.intensity = Nominal;
            host.AddComponent<ResidualPower>().Configure(null, 0, lamp, Phase);
            host.SetActive(true);

            float worst = 0f;
            int dimmed = 0;
            for (int frame = 0; frame < 300; frame++)
            {
                yield return null;
                float expected = Nominal * Level(Time.time + Phase, Phase);
                worst = Math.Max(worst, Math.Abs(lamp.intensity - expected));
                dimmed += lamp.intensity < Nominal ? 1 : 0;
            }

            Assert.That(worst, Is.LessThan(1e-4f));
            Assert.That(dimmed, Is.GreaterThan(0), "The lamp never dimmed");
        }

        // The outage cycle from the system note: 11.5 s plus 9% of the phase;
        // 4% at 0–0.13 s and 0.29–0.43 s, 27% at 0.64–1.20 s, full otherwise.
        private static float Level(float time, float phase)
        {
            float length = 11.5f + phase * .09f;
            float cycle = time - (float)Math.Floor(time / length) * length;
            if (cycle < .13f || (cycle > .29f && cycle < .43f))
            {
                return .04f;
            }

            return cycle < 1.20f && cycle > .64f ? .27f : 1f;
        }
    }
}
