using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the settings limits, from the Phase 11 settings menu:
    /// frame limits unlimited, 30, 60, 90, 120, 144, 165 or 240 (default 60),
    /// windows from 640 × 360, volumes on a decibel scale with silence at -80 dB.
    /// </summary>
    public sealed class PreferenceRulesTests
    {
        [Test]
        public void AValueOutOfRangeIsClamped()
        {
            Assert.That(PreferenceRules.Bounded(140f, 55f, 100f, 68f), Is.EqualTo(100f));
        }

        [Test]
        public void AValueThatIsNotANumberGetsItsDefault()
        {
            Assert.That(PreferenceRules.Bounded(float.NaN, 55f, 100f, 68f), Is.EqualTo(68f));
            Assert.That(PreferenceRules.Bounded(float.PositiveInfinity, 0f, 1f, .65f), Is.EqualTo(.65f));
        }

        [Test]
        public void OnlyTheOfferedFrameLimitsAreKept()
        {
            Assert.That(PreferenceRules.FrameLimit(144), Is.EqualTo(144));
            Assert.That(PreferenceRules.FrameLimit(-1), Is.EqualTo(-1));
            Assert.That(PreferenceRules.FrameLimit(75), Is.EqualTo(60));
        }

        [Test]
        public void WindowsSmallerThanSixHundredFortyByThreeHundredSixtyAreNotUsable()
        {
            Assert.That(PreferenceRules.IsWindowSize(640, 360), Is.True);
            Assert.That(PreferenceRules.IsWindowSize(639, 360), Is.False);
        }

        [Test]
        public void FullVolumeIsZeroDecibelsAndATenthIsMinusTwenty()
        {
            Assert.That(PreferenceRules.ToDecibels(1f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(PreferenceRules.ToDecibels(.1f), Is.EqualTo(-20f).Within(1e-3f));
        }

        [Test]
        public void ZeroVolumeIsSilence()
        {
            Assert.That(PreferenceRules.ToDecibels(0f), Is.EqualTo(-80f));
        }
    }
}
