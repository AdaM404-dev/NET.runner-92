using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of what the lighting bake includes. Expected values come
    /// from decision issue #79: lamps whose light changes at runtime (pulsing,
    /// scripted, moving) stay live and every other lamp is baked; renderers
    /// that move or are see-through stay out of the bake; static ones narrower
    /// than half a metre in their second-longest side (small or thin) are lit
    /// by the probes, and the rest get lightmaps.
    /// </summary>
    public sealed class LightBakeRulesTests
    {
        [Test]
        public void ALampThatNeverChangesIsBaked()
        {
            Assert.That(LightBakeRules.StaysLive(false, false, false), Is.False);
        }

        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, true)]
        public void ALampThatPulsesIsScriptedOrMovesStaysLive(bool pulses, bool scripted, bool moves)
        {
            Assert.That(LightBakeRules.StaysLive(pulses, scripted, moves), Is.True);
        }

        [Test]
        public void AMovingRendererStaysOutOfTheBake()
        {
            Assert.That(LightBakeRules.RoleOf(true, false, 4f), Is.EqualTo(BakeRole.NotBaked));
        }

        [Test]
        public void ASeeThroughRendererStaysOutOfTheBake()
        {
            Assert.That(LightBakeRules.RoleOf(false, true, 4f), Is.EqualTo(BakeRole.NotBaked));
        }

        [TestCase(.2f, BakeRole.ProbeLit)]
        [TestCase(.49f, BakeRole.ProbeLit)]
        [TestCase(.5f, BakeRole.Lightmapped)]
        [TestCase(12f, BakeRole.Lightmapped)]
        public void AStaticRendererIsLightmappedFromHalfAMetreOfBreadth(float breadth, BakeRole role)
        {
            Assert.That(LightBakeRules.RoleOf(false, false, breadth), Is.EqualTo(role));
        }
    }
}
