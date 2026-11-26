using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of capping the first-start screen size.
    /// </summary>
    public sealed class StartResolutionTests
    {
        [Test]
        public void A4KScreenStartsAt1080p()
        {
            var size = StartResolution.Cap(3840, 2160, 1080);

            Assert.That(size, Is.EqualTo((1920, 1080)));
        }

        [Test]
        public void A1440pScreenStartsAt1080p()
        {
            var size = StartResolution.Cap(2560, 1440, 1080);

            Assert.That(size, Is.EqualTo((1920, 1080)));
        }

        [Test]
        public void AWideScreenKeepsItsShape()
        {
            var size = StartResolution.Cap(3440, 1440, 1080);

            Assert.That(size, Is.EqualTo((2580, 1080)));
        }

        [Test]
        public void TheWidthIsRoundedDownToAnEvenNumber()
        {
            // 2561 * 1080 / 1440 = 1920.75, so 1920 after rounding down to even.
            var size = StartResolution.Cap(2561, 1440, 1080);

            Assert.That(size.Width % 2, Is.EqualTo(0));
            Assert.That(size, Is.EqualTo((1920, 1080)));
        }

        [Test]
        public void ScreensThatFitAreUnchanged()
        {
            Assert.That(StartResolution.Cap(1920, 1080, 1080), Is.EqualTo((1920, 1080)));
            Assert.That(StartResolution.Cap(1366, 768, 1080), Is.EqualTo((1366, 768)));
        }

        [Test]
        public void SizesThatAreNotPositiveAreUnchanged()
        {
            Assert.That(StartResolution.Cap(0, 0, 1080), Is.EqualTo((0, 0)));
        }
    }
}
