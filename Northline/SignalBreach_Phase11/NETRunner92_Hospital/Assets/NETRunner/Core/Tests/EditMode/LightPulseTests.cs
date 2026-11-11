using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the pulsing lights. The numbers come from
    /// docs/systems/atmosphere.md: an emergency light at 0.5 + 0.5 sin(2π ×
    /// cycle); an intermittent light dimmed below noise 0.27 and full from
    /// 0.27 + 1/3.5, with an S-curve between; the intensity between full and
    /// full × (1 − depth).
    /// </summary>
    public sealed class LightPulseTests
    {
        [Test]
        public void TheCycleCountsCyclesFromTheTimeAndThePhase()
        {
            // 2 s at 0.8 cycles a second, starting a quarter of a cycle in.
            Assert.That(LightPulse.Cycle(2f, 0.8f, 0.25f), Is.EqualTo(1.85f).Within(1e-6f));
        }

        [Test]
        public void AnEmergencyLightIsHalfwayAtTheStartOfACycleFullAQuarterInAndDimmedThreeQuartersIn()
        {
            Assert.That(LightPulse.Emergency(0f), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(LightPulse.Emergency(0.25f), Is.EqualTo(1f).Within(1e-6f));
            Assert.That(LightPulse.Emergency(0.5f), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(LightPulse.Emergency(0.75f), Is.EqualTo(0f).Within(1e-6f));
        }

        [Test]
        public void AnEmergencyLightRepeatsEveryCycle()
        {
            Assert.That(LightPulse.Emergency(3.25f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(LightPulse.Emergency(7.75f), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void AnIntermittentLightIsDimmedWhileItsNoiseIsLow()
        {
            Assert.That(LightPulse.Intermittent(0f), Is.EqualTo(0f));
            Assert.That(LightPulse.Intermittent(0.27f), Is.EqualTo(0f));
        }

        [Test]
        public void AnIntermittentLightIsFullOnceItsNoiseIsHigh()
        {
            Assert.That(LightPulse.Intermittent(0.27f + 1f / 3.5f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(LightPulse.Intermittent(0.8f), Is.EqualTo(1f));
            Assert.That(LightPulse.Intermittent(1f), Is.EqualTo(1f));
        }

        [Test]
        public void AnIntermittentLightIsHalfwayInTheMiddleOfItsNoiseRange()
        {
            Assert.That(LightPulse.Intermittent(0.27f + 0.5f / 3.5f), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void AtFullLevelTheLightHasItsOwnIntensity()
        {
            Assert.That(LightPulse.Intensity(800f, 0.61f, 1f), Is.EqualTo(800f).Within(1e-3f));
        }

        [Test]
        public void AtLevelZeroTheLightIsDimmedByItsDepth()
        {
            Assert.That(LightPulse.Intensity(800f, 0.61f, 0f), Is.EqualTo(312f).Within(1e-3f));
            Assert.That(LightPulse.Intensity(800f, 1f, 0f), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void InBetweenTheIntensityFollowsTheLevelInAStraightLine()
        {
            // Depth 0.5: from 400 at level 0 to 800 at level 1.
            Assert.That(LightPulse.Intensity(800f, 0.5f, 0.5f), Is.EqualTo(600f).Within(1e-3f));
        }
    }
}
