using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of when the player camera needs motion vectors. Expected
    /// values come from what reads them in HDRP: TAA, a temporal upscaler,
    /// motion blur and the ambient occlusion's temporal filter (#36, round 3).
    /// FXAA and SMAA work on one frame and need none.
    /// </summary>
    public sealed class MotionVectorRuleTests
    {
        private const int NoAntialias = 0;
        private const int Fxaa = 1;
        private const int Smaa = 2;

        [TestCase(NoAntialias)]
        [TestCase(Fxaa)]
        [TestCase(Smaa)]
        public void SingleFrameAntialiasingAloneNeedsNoMotionVectors(int antialias)
        {
            Assert.That(MotionVectorRule.AreNeeded(antialias, false, false, false), Is.False);
        }

        [Test]
        public void TemporalAntialiasingNeedsMotionVectors()
        {
            Assert.That(MotionVectorRule.AreNeeded(QualityPreset.TemporalAntialias, false, false, false), Is.True);
        }

        [Test]
        public void MotionBlurNeedsMotionVectors()
        {
            Assert.That(MotionVectorRule.AreNeeded(Fxaa, true, false, false), Is.True);
        }

        [Test]
        public void AmbientOcclusionNeedsMotionVectors()
        {
            Assert.That(MotionVectorRule.AreNeeded(Smaa, false, true, false), Is.True);
        }

        [Test]
        public void ATemporalUpscalerNeedsMotionVectors()
        {
            Assert.That(MotionVectorRule.AreNeeded(Fxaa, false, false, true), Is.True);
        }

        [TestCase(QualityPreset.Low, false)]
        [TestCase(QualityPreset.Medium, false)]
        [TestCase(QualityPreset.High, true)]
        [TestCase(QualityPreset.Ultra, true)]
        public void OnlyTheTemporalPresetsNeedMotionVectorsWithoutAnUpscaler(int preset, bool needed)
        {
            QualityPreset.Settings settings = QualityPreset.For(preset);

            Assert.That(MotionVectorRule.AreNeeded(settings.Antialias, false, settings.AmbientOcclusion, false), Is.EqualTo(needed));
        }
    }
}
