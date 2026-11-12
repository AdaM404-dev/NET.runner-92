using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>Checks the desktop frame limit against native and fractional display rates.</summary>
    public sealed class FramePacingTests
    {
        [TestCase(60, 59.94, 1)]
        [TestCase(60, 120, 2)]
        [TestCase(60, 144, 0)]
        [TestCase(60, 240, 4)]
        [TestCase(60, 360, 0)]
        [TestCase(30, 60, 2)]
        [TestCase(30, 120, 4)]
        [TestCase(30, 144, 0)]
        [TestCase(60, 0, 0)]
        public void SyncIsUsedWhenTheDisplayCanHonorTheLimit(int limit, double refreshRate, int interval)
        {
            Assert.That(FramePacing.SyncInterval(true, limit, refreshRate), Is.EqualTo(interval));
        }

        [Test]
        public void UnlimitedWithVsyncUsesTheNativeRate()
        {
            Assert.That(FramePacing.SyncInterval(true, PreferenceRules.Unlimited, 144), Is.EqualTo(1));
        }

        [Test]
        public void DisablingVsyncUsesTheSoftwareLimitEvenAtAMatchingRate()
        {
            Assert.That(FramePacing.SyncInterval(false, 60, 120), Is.Zero);
        }
    }
}
