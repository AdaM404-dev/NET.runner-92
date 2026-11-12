using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the player's vertical speed. Expected values come
    /// from docs/systems/player.md: gravity 18 m/s², falls capped at 34 m/s,
    /// and a push down of 4.2 m/s while standing.
    /// </summary>
    public sealed class VerticalMotionTests
    {
        private const float Gravity = 18f;
        private const float GroundAdhesion = 4.2f;
        private const float Step = 1f / 90f;

        [Test]
        public void InTheAirGravityTakesEighteenMetresPerSecondOffEverySecond()
        {
            float speed = VerticalMotion.Step(0f, false, Gravity, GroundAdhesion, .5f);

            Assert.That(speed, Is.EqualTo(-9f).Within(1e-5f));
        }

        [Test]
        public void ARisingPlayerSlowsDownUnderGravity()
        {
            float speed = VerticalMotion.Step(2f, false, Gravity, GroundAdhesion, .1f);

            Assert.That(speed, Is.EqualTo(.2f).Within(1e-5f));
        }

        [Test]
        public void AFallStopsSpeedingUpAtThirtyFourMetresPerSecond()
        {
            float nearly = VerticalMotion.Step(-33.9f, false, Gravity, GroundAdhesion, .1f);
            float atTheLimit = VerticalMotion.Step(-34f, false, Gravity, GroundAdhesion, Step);

            Assert.That(nearly, Is.EqualTo(-34f));
            Assert.That(atTheLimit, Is.EqualTo(-34f));
        }

        [Test]
        public void OnTheFloorItIsPressedDownAtTheGroundAdhesion()
        {
            float fromAFall = VerticalMotion.Step(-12f, true, Gravity, GroundAdhesion, Step);
            float fromStanding = VerticalMotion.Step(-GroundAdhesion, true, Gravity, GroundAdhesion, Step);

            Assert.That(fromAFall, Is.EqualTo(-GroundAdhesion));
            Assert.That(fromStanding, Is.EqualTo(-GroundAdhesion));
        }

        [Test]
        public void HittingTheCeilingStopsARise()
        {
            Assert.That(VerticalMotion.AfterHittingCeiling(3f), Is.EqualTo(0f));
        }

        [Test]
        public void HittingTheCeilingDoesNotStopAFall()
        {
            Assert.That(VerticalMotion.AfterHittingCeiling(-3f), Is.EqualTo(-3f));
        }
    }
}
