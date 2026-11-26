using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the averaged frames-per-second counter.
    /// </summary>
    public sealed class FrameRateMeterTests
    {
        [Test]
        public void IsZeroUntilTheFirstWindowIsComplete()
        {
            var meter = new FrameRateMeter(0.5f);

            meter.Add(0.1f);
            meter.Add(0.1f);

            Assert.That(meter.FramesPerSecond, Is.EqualTo(0f));
        }

        [Test]
        public void SixtyFourEvenFramesInOneSecondReadSixtyFour()
        {
            // 1/64 s is exact in floating point, so 64 frames make exactly one second.
            var meter = new FrameRateMeter(1f);

            for (int i = 0; i < 64; i++)
            {
                meter.Add(1f / 64f);
            }

            Assert.That(meter.FramesPerSecond, Is.EqualTo(64f).Within(1e-3f));
        }

        [Test]
        public void TheValueIsTheAverageOverTheWindow()
        {
            // Two frames of 0.1 s and one of 0.3 s: 3 frames in 0.5 s = 6 FPS.
            var meter = new FrameRateMeter(0.5f);

            meter.Add(0.1f);
            meter.Add(0.1f);
            meter.Add(0.3f);

            Assert.That(meter.FramesPerSecond, Is.EqualTo(6f).Within(1e-3f));
        }

        [Test]
        public void TheValueHoldsUntilTheNextWindowCompletes()
        {
            var meter = new FrameRateMeter(0.5f);
            meter.Add(0.5f);

            meter.Add(0.01f);

            Assert.That(meter.FramesPerSecond, Is.EqualTo(2f).Within(1e-3f));
        }

        [Test]
        public void FramesWithoutDurationAreIgnored()
        {
            var meter = new FrameRateMeter(0.5f);

            meter.Add(0f);
            meter.Add(-1f);
            meter.Add(0.5f);

            Assert.That(meter.FramesPerSecond, Is.EqualTo(2f).Within(1e-3f));
        }
    }
}
