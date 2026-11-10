using System;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the rain around the listener. The numbers come from
    /// docs/systems/atmosphere.md: at most 90 drops a frame, fractions carried
    /// over; drops within 14 m; a streak starts at the higher of 2.7–9.5 m
    /// above the listener and 1.6–5.6 m above the roof and falls at 16 m/s to
    /// 0.18 m above the roof; under a roof more than 0.6 m overhead, a streak
    /// starting more than 12 m above the listener is left out; a streak with
    /// something in its way ends 0.07 m short, after at least 0.02 s; every
    /// seventh drop splashes on ground below 0.35 m while the blend is below
    /// 0.9; no rain is worked out above blend 0.92 within 29 m by 9.5 m of
    /// the centre. Random draws are checked against their ranges, on many
    /// draws from a seeded stream.
    /// </summary>
    public sealed class RainFieldTests
    {
        private const int Draws = 2000;

        [Test]
        public void ARainFieldNeedsARandomStream()
        {
            Assert.Throws<ArgumentNullException>(() => new RainField(null));
        }

        [Test]
        public void At730DropsASecondASixtiethOfASecondBringsTwelveDrops()
        {
            var rain = new RainField(new Random(1));

            Assert.That(rain.DropsDue(1f / 60f, 730f), Is.EqualTo(12));
        }

        [Test]
        public void FractionsOfADropCarryOverToTheNextFrame()
        {
            // Half a drop a frame: one drop every second frame.
            var rain = new RainField(new Random(1));

            Assert.That(rain.DropsDue(0.25f, 2f), Is.EqualTo(0));
            Assert.That(rain.DropsDue(0.25f, 2f), Is.EqualTo(1));
            Assert.That(rain.DropsDue(0.25f, 2f), Is.EqualTo(0));
            Assert.That(rain.DropsDue(0.25f, 2f), Is.EqualTo(1));
        }

        [Test]
        public void ALongFrameBringsAtMostNinetyDrops()
        {
            var rain = new RainField(new Random(1));

            Assert.That(rain.DropsDue(1f, 730f), Is.EqualTo(90));
            Assert.That(rain.DropsDue(0f, 730f), Is.EqualTo(0), "The rest of the long frame was carried over");
        }

        [Test]
        public void DropsLandWithinFourteenMetresOfTheListener()
        {
            var rain = new RainField(new Random(2087));
            float farthest = 0f;
            for (int i = 0; i < Draws; i++)
            {
                var (x, z) = rain.Column(5f, -3f);
                Assert.That(x - 5f, Is.InRange(-14f, 14f));
                Assert.That(z + 3f, Is.InRange(-14f, 14f));
                farthest = MathF.Max(farthest, MathF.Max(MathF.Abs(x - 5f), MathF.Abs(z + 3f)));
            }

            Assert.That(farthest, Is.GreaterThan(13f), "Drops do not spread over the whole 14 m");
        }

        [Test]
        public void AStreakStartsAboveBothTheListenerAndTheRoof()
        {
            // A roof 3 m up, the listener's eyes at 1.6 m: the top is at least 1.6 + 2.7 and 3 + 1.6 m.
            var rain = new RainField(new Random(2087));
            for (int i = 0; i < Draws; i++)
            {
                Assert.That(rain.TryStreak(1.6f, 3f, out float top, out _), Is.True);
                Assert.That(top, Is.GreaterThanOrEqualTo(1.6f + 2.7f - 1e-4f));
                Assert.That(top, Is.GreaterThanOrEqualTo(3f + 1.6f - 1e-4f));
                Assert.That(top, Is.LessThanOrEqualTo(MathF.Max(1.6f + 9.5f, 3f + 5.6f) + 1e-4f));
            }
        }

        [Test]
        public void AStreakFallsAtSixteenMetresASecondToEighteenCentimetresAboveTheRoof()
        {
            var rain = new RainField(new Random(2087));
            for (int i = 0; i < Draws; i++)
            {
                rain.TryStreak(1.6f, 0f, out float top, out float fallTime);
                Assert.That(top - fallTime * 16f, Is.EqualTo(0.18f).Within(1e-4f));
            }
        }

        [Test]
        public void UnderARoofFarOverheadStreaksStartingMoreThanTwelveMetresAboveTheListenerAreLeftOut()
        {
            // A roof 11 m up: every streak starts at least 11 + 1.6 m up, more than 12 m above the listener.
            var rain = new RainField(new Random(2087));
            for (int i = 0; i < Draws; i++)
            {
                Assert.That(rain.TryStreak(0f, 11f, out _, out _), Is.False);
            }
        }

        [Test]
        public void UnderARoofEightMetresUpOnlyTheStreaksStartingWithinTwelveMetresAreKept()
        {
            // Streaks start 9.6–13.6 m up here, so some are kept and some left out.
            var rain = new RainField(new Random(2087));
            int kept = 0;
            for (int i = 0; i < Draws; i++)
            {
                if (rain.TryStreak(0f, 8f, out float top, out _))
                {
                    kept++;
                    Assert.That(top, Is.LessThanOrEqualTo(12f));
                }
            }

            Assert.That(kept, Is.InRange(1, Draws - 1));
        }

        [Test]
        public void UnderALowCeilingEveryStreakIsKept()
        {
            // A roof 3 m up is overhead, but no streak can start 12 m above the listener.
            var rain = new RainField(new Random(2087));
            for (int i = 0; i < Draws; i++)
            {
                Assert.That(rain.TryStreak(0f, 3f, out _, out _), Is.True);
            }
        }

        [Test]
        public void AStreakWithSomethingInTheWayEndsSevenCentimetresShortOfIt()
        {
            // 8.07 m along the path at 16 m/s: 8 m to fall, half a second.
            Assert.That(RainField.FallTimeToImpact(1f, 8.07f, 16f), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void AStreakLastsAtLeastTwoHundredthsOfASecond()
        {
            Assert.That(RainField.FallTimeToImpact(1f, 0.05f, 16f), Is.EqualTo(0.02f));
        }

        [Test]
        public void SomethingFurtherAwayThanTheStreakFallsDoesNotLengthenIt()
        {
            Assert.That(RainField.FallTimeToImpact(0.3f, 100f, 16f), Is.EqualTo(0.3f));
        }

        [Test]
        public void EverySeventhDropOnOpenGroundSplashes()
        {
            Assert.That(RainField.Splashes(0, 0f, 0f), Is.True);
            Assert.That(RainField.Splashes(7, 0f, 0f), Is.True);
            Assert.That(RainField.Splashes(14, 0f, 0f), Is.True);
            for (int i = 1; i < 7; i++)
            {
                Assert.That(RainField.Splashes(i, 0f, 0f), Is.False);
            }
        }

        [Test]
        public void DropsSplashOnlyOnGroundLowerThanThirtyFiveCentimetres()
        {
            Assert.That(RainField.Splashes(0, 0.34f, 0f), Is.True);
            Assert.That(RainField.Splashes(0, 0.35f, 0f), Is.False);
            Assert.That(RainField.Splashes(0, 4.2f, 0f), Is.False);
        }

        [Test]
        public void DeepInsideNoSplashesAreShown()
        {
            Assert.That(RainField.Splashes(0, 0f, 0.89f), Is.True);
            Assert.That(RainField.Splashes(0, 0f, 0.9f), Is.False);
        }

        [Test]
        public void DeepInsideTheCentralBlockNoRainIsWorkedOut()
        {
            Assert.That(RainField.IsSheltered(0.93f, 0f, 0f), Is.True);
            Assert.That(RainField.IsSheltered(1f, -28.9f, 9.4f), Is.True);
        }

        [Test]
        public void AlongTheFacadesAndAtTheEntrancesItStillRains()
        {
            Assert.That(RainField.IsSheltered(0.92f, 0f, 0f), Is.False, "Not deep enough inside");
            Assert.That(RainField.IsSheltered(1f, 29f, 0f), Is.False, "East or west of the central block");
            Assert.That(RainField.IsSheltered(1f, 0f, -9.5f), Is.False, "North or south of the central block");
        }

        [Test]
        public void DropsAndSplashesStayWithinTheirRanges()
        {
            var rain = new RainField(new Random(2087));
            for (int i = 0; i < Draws; i++)
            {
                Assert.That(rain.DropSize(), Is.InRange(0.013f, 0.026f));
                var (x, y, z) = rain.SplashVelocity();
                Assert.That(x, Is.InRange(-0.3f, 0.3f));
                Assert.That(y, Is.InRange(0.35f, 0.75f));
                Assert.That(z, Is.InRange(-0.3f, 0.3f));
                Assert.That(rain.SplashSize(), Is.InRange(0.028f, 0.067f));
                Assert.That(rain.SplashLife(), Is.InRange(0.14f, 0.22f));
            }
        }

        [Test]
        public void TheSameSeedGivesTheSameRain()
        {
            var first = new RainField(new Random(2087));
            var second = new RainField(new Random(2087));
            for (int i = 0; i < 100; i++)
            {
                Assert.That(first.Column(0f, 0f), Is.EqualTo(second.Column(0f, 0f)));
            }
        }
    }
}
