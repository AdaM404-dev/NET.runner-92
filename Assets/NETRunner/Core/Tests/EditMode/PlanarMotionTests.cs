using System;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the player's velocity along the floor. Expected
    /// values come from docs/systems/player.md: walk 3.5 m/s, sprint
    /// 5.5 m/s, 22 m/s² acceleration, diagonals no faster.
    /// </summary>
    public sealed class PlanarMotionTests
    {
        private const float Walk = 3.5f;
        private const float Sprint = 5.5f;
        private const float Acceleration = 22f;
        private const float Frame = 1f / 60f;

        [Test]
        public void StraightAheadTheTargetIsTheWalkingSpeed()
        {
            (float x, float z) = PlanarMotion.Target(0f, 1f, Walk);

            Assert.That(x, Is.EqualTo(0f));
            Assert.That(z, Is.EqualTo(Walk));
        }

        [Test]
        public void ADiagonalIsNoFasterThanStraightAhead()
        {
            (float x, float z) = PlanarMotion.Target(1f, 1f, Sprint);

            Assert.That(PlanarMotion.Speed(x, z), Is.EqualTo(Sprint).Within(1e-5f));
            Assert.That(x, Is.EqualTo(z).Within(1e-5f));
        }

        [Test]
        public void AHalfPushAsksForHalfTheSpeed()
        {
            (float x, float z) = PlanarMotion.Target(.5f, 0f, Walk);

            Assert.That(x, Is.EqualTo(Walk * .5f));
            Assert.That(z, Is.EqualTo(0f));
        }

        [Test]
        public void EachFrameTheVelocityChangesByAtMostTheAcceleration()
        {
            (float x, float z) = PlanarMotion.Approach(0f, 0f, 0f, Walk, Acceleration * Frame);

            Assert.That(x, Is.EqualTo(0f));
            Assert.That(z, Is.EqualTo(Acceleration * Frame).Within(1e-6f));
        }

        [Test]
        public void CloseToTheTargetTheVelocityBecomesItExactly()
        {
            (float x, float z) = PlanarMotion.Approach(.1f, 3.4f, 0f, Walk, Acceleration * Frame);

            Assert.That(x, Is.EqualTo(0f));
            Assert.That(z, Is.EqualTo(Walk));
        }

        [Test]
        public void TurningChangesTheVelocityAlongAStraightLine()
        {
            (float x, float z) = PlanarMotion.Approach(Walk, 0f, 0f, Walk, 1f);

            float along = 1f / (float)Math.Sqrt(2.0);
            Assert.That(x, Is.EqualTo(Walk - along).Within(1e-5f));
            Assert.That(z, Is.EqualTo(along).Within(1e-5f));
        }

        [Test]
        public void FromStandingWalkingSpeedIsReachedInTheTenthFrame()
        {
            // 3.5 m/s at 22 m/s² takes 0.159 s: nine frames of 1/60 s are not quite enough.
            float x = 0f;
            float z = 0f;
            int frames = 0;
            while (z < Walk && frames < 60)
            {
                (x, z) = PlanarMotion.Approach(x, z, 0f, Walk, Acceleration * Frame);
                frames++;
            }

            Assert.That(frames, Is.EqualTo(10));
        }

        [Test]
        public void SpeedIsTheLengthOfTheVelocity()
        {
            Assert.That(PlanarMotion.Speed(3f, 4f), Is.EqualTo(5f));
        }
    }
}
