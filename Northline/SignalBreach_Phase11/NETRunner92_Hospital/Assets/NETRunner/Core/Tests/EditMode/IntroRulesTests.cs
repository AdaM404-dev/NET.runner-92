using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the studio intro's rules. Expected behaviour comes
    /// from #66: the intro ends when the video finishes, on Space, on a
    /// playback failure, or after 5 s without a first frame; test and
    /// benchmark runs and -net92-skip-intro skip it.
    /// </summary>
    public sealed class IntroRulesTests
    {
        [Test]
        public void ThePlayingIntroContinues()
        {
            Assert.That(IntroRules.IsOver(false, false, false, true, 12f), Is.False);
        }

        [Test]
        public void TheIntroEndsWithTheVideo()
        {
            Assert.That(IntroRules.IsOver(true, false, false, true, 30f), Is.True);
        }

        [Test]
        public void SpaceEndsTheIntroAtOnce()
        {
            Assert.That(IntroRules.IsOver(false, true, false, true, .5f), Is.True);
            Assert.That(IntroRules.IsOver(false, true, false, false, 0f), Is.True);
        }

        [Test]
        public void AFailedVideoGoesOnToTheMenu()
        {
            Assert.That(IntroRules.IsOver(false, false, true, false, .1f), Is.True);
        }

        [Test]
        public void AVideoWithoutAFrameGetsFiveSeconds()
        {
            Assert.That(IntroRules.IsOver(false, false, false, false, 4.9f), Is.False);
            Assert.That(IntroRules.IsOver(false, false, false, false, 5f), Is.True);
        }

        [Test]
        public void ANormalStartPlaysTheIntro()
        {
            Assert.That(IntroRules.IsSkippedAtLaunch(new[] { "NETRunner92_Hospital.exe" }), Is.False);
            Assert.That(IntroRules.IsSkippedAtLaunch(new[] { "NETRunner92_Hospital.exe", "-net92-quality", "Low" }), Is.False);
            Assert.That(IntroRules.IsSkippedAtLaunch(null), Is.False);
        }

        [TestCase("-net92-skip-intro")]
        [TestCase("-net92-benchmark")]
        [TestCase("-net92-route-benchmark")]
        [TestCase("-net92-menu-check")]
        [TestCase("-net92-menu-reload-check")]
        [TestCase("-net92-input-check")]
        public void ChecksBenchmarksAndTheSkipOptionSkipTheIntro(string option)
        {
            Assert.That(IntroRules.IsSkippedAtLaunch(new[] { "NETRunner92_Hospital.exe", option }), Is.True);
        }
    }
}
