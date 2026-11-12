using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the residual power. Expected values come from
    /// docs/systems/level-hospital.md: the cycle is 11.5 s plus 9% of the
    /// phase; a lamp is at 4% from 0 to 0.13 s and 0.29 to 0.43 s, at 27%
    /// from 0.64 to 1.20 s and full otherwise; a screen is at 18% for the
    /// first 0.28 s and then at 78% ± 20%.
    /// </summary>
    public sealed class PowerFlickerTests
    {
        [Test]
        public void ALampStuttersTwiceBrownsOutAndBurnsFully()
        {
            Assert.That(PowerFlicker.Level(.05f, 0f, true), Is.EqualTo(.04f));
            Assert.That(PowerFlicker.Level(.2f, 0f, true), Is.EqualTo(1f));
            Assert.That(PowerFlicker.Level(.35f, 0f, true), Is.EqualTo(.04f));
            Assert.That(PowerFlicker.Level(.5f, 0f, true), Is.EqualTo(1f));
            Assert.That(PowerFlicker.Level(.9f, 0f, true), Is.EqualTo(.27f));
            Assert.That(PowerFlicker.Level(5f, 0f, true), Is.EqualTo(1f));
        }

        [Test]
        public void TheOutageComesBackEveryCycle()
        {
            Assert.That(PowerFlicker.Level(11.5f + .05f, 0f, true), Is.EqualTo(.04f));
            Assert.That(PowerFlicker.Level(23f + .9f, 0f, true), Is.EqualTo(.27f));
        }

        [Test]
        public void APhaseShiftsTheOutageAndLengthensTheCycle()
        {
            // Phase 10: the fixture's clock runs 10 s ahead, and its cycle is 11.5 + 0.9 = 12.4 s.
            Assert.That(PowerFlicker.Level(12.4f - 10f + .05f, 10f, true), Is.EqualTo(.04f));
            Assert.That(PowerFlicker.Level(11.5f - 10f + .05f, 10f, true), Is.EqualTo(1f));
        }

        [Test]
        public void AScreenStartsLowThenWavesAroundSeventyEightPercent()
        {
            Assert.That(PowerFlicker.Level(.1f, 0f, false), Is.EqualTo(.18f));
            for (float time = .3f; time < 11f; time += .37f)
            {
                Assert.That(PowerFlicker.Level(time, 0f, false), Is.InRange(.58f, .98f));
            }
        }
    }
}
