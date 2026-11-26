using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the ambience mix. The numbers come from
    /// docs/systems/atmosphere.md: the interior blend follows the zones at
    /// 1.4 a second; rain and wind 0.48 outdoors to 0.034 inside, ventilation
    /// 0.012 to 0.27, thunder 0.72 to 0.26, creaks 0.34 to 0.15, the monitor
    /// 0.22 × the blend; 29 dust motes a second at full dust; the valve steams
    /// 6 puffs a second within 11 m.
    /// </summary>
    public sealed class AmbienceTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void TheBlendMovesTowardsTheCoverAtOnePointFourASecond()
        {
            Assert.That(Ambience.Follow(0f, 1f, 0.25f), Is.EqualTo(0.35f).Within(1e-6f));
            Assert.That(Ambience.Follow(1f, 0f, 0.25f), Is.EqualTo(0.65f).Within(1e-6f));
        }

        [Test]
        public void TheBlendStopsAtTheCover()
        {
            Assert.That(Ambience.Follow(0.9f, 1f, 0.25f), Is.EqualTo(1f));
            Assert.That(Ambience.Follow(0.4f, 0.3f, 0.25f), Is.EqualTo(0.3f));
            Assert.That(Ambience.Follow(0.5f, 0.5f, 0.25f), Is.EqualTo(0.5f));
        }

        [Test]
        public void FromOutdoorsToFullyInsideTakesAboutSevenTenthsOfASecond()
        {
            // 1 / 1.4 = 0.714 s.
            float blend = 0f;
            int frames = 0;
            while (blend < 1f && frames < 1000)
            {
                blend = Ambience.Follow(blend, 1f, Frame);
                frames++;
            }

            Assert.That(frames * Frame, Is.EqualTo(1f / 1.4f).Within(Frame));
        }

        [Test]
        public void OutdoorsTheRainAndWindIsLoudAndTheVentilationFaint()
        {
            Assert.That(Ambience.RainAndWindVolume(0f), Is.EqualTo(0.48f));
            Assert.That(Ambience.VentilationVolume(0f), Is.EqualTo(0.012f));
        }

        [Test]
        public void FullyInsideTheVentilationTakesOver()
        {
            Assert.That(Ambience.RainAndWindVolume(1f), Is.EqualTo(0.034f).Within(1e-6f));
            Assert.That(Ambience.VentilationVolume(1f), Is.EqualTo(0.27f).Within(1e-6f));
        }

        [Test]
        public void HalfwayInTheLoopsAreHalfwayBetween()
        {
            Assert.That(Ambience.RainAndWindVolume(0.5f), Is.EqualTo(0.257f).Within(1e-6f));
            Assert.That(Ambience.VentilationVolume(0.5f), Is.EqualTo(0.141f).Within(1e-6f));
        }

        [Test]
        public void ThunderAndCreaksAreQuieterInside()
        {
            Assert.That(Ambience.ThunderVolume(0f), Is.EqualTo(0.72f));
            Assert.That(Ambience.ThunderVolume(1f), Is.EqualTo(0.26f).Within(1e-6f));
            Assert.That(Ambience.CreakVolume(0f), Is.EqualTo(0.34f));
            Assert.That(Ambience.CreakVolume(1f), Is.EqualTo(0.15f).Within(1e-6f));
        }

        [Test]
        public void TheMonitorIsLouderFurtherInside()
        {
            Assert.That(Ambience.MonitorVolume(0.5f), Is.EqualTo(0.11f).Within(1e-6f));
            Assert.That(Ambience.MonitorVolume(1f), Is.EqualTo(0.22f).Within(1e-6f));
        }

        [Test]
        public void TheDustFollowsTheZonesDust()
        {
            Assert.That(Ambience.DustRate(0f), Is.EqualTo(0f));
            Assert.That(Ambience.DustRate(0.5f), Is.EqualTo(14.5f).Within(1e-5f));
            Assert.That(Ambience.DustRate(1f), Is.EqualTo(29f));
        }

        [Test]
        public void TheValveSteamsOnlyWithinElevenMetres()
        {
            Assert.That(Ambience.SteamRate(0f), Is.EqualTo(6f));
            Assert.That(Ambience.SteamRate(10.9f), Is.EqualTo(6f));
            Assert.That(Ambience.SteamRate(11f), Is.EqualTo(0f));
            Assert.That(Ambience.SteamRate(30f), Is.EqualTo(0f));
        }
    }
}
