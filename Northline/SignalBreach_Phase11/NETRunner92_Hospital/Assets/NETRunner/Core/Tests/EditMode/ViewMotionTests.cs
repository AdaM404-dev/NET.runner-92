using System;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of mouse look and the head bob. Expected values come
    /// from docs/systems/player.md: the pitch stops at 85° up and down; the
    /// bob is off by default, and when on it rises with walking speed (4 mm
    /// per m/s, at most 21 mm) once per 2π/3 m walked.
    /// </summary>
    public sealed class ViewMotionTests
    {
        private const float Sensitivity = .13f;
        private const float Limit = 85f;
        private const float Walk = 3.5f;

        [Test]
        public void MovingTheMouseUpLooksUp()
        {
            float pitch = ViewMotion.Pitch(0f, 10f, Sensitivity, Limit);

            Assert.That(pitch, Is.EqualTo(-1.3f).Within(1e-5f));
        }

        [Test]
        public void ThePitchStopsAtTheLimitBothWays()
        {
            Assert.That(ViewMotion.Pitch(80f, -100f, Sensitivity, Limit), Is.EqualTo(Limit));
            Assert.That(ViewMotion.Pitch(-80f, 100f, Sensitivity, Limit), Is.EqualTo(-Limit));
        }

        [Test]
        public void TheBodyTurnsByTheMouseTimesTheSensitivity()
        {
            Assert.That(ViewMotion.Turn(100f, Sensitivity), Is.EqualTo(13f).Within(1e-5f));
            Assert.That(ViewMotion.Turn(-100f, Sensitivity), Is.EqualTo(-13f).Within(1e-5f));
        }

        [Test]
        public void TheStrideAdvancesThreeRadiansPerMetreWalked()
        {
            Assert.That(ViewMotion.Stride(1f, Walk, 2f), Is.EqualTo(1f + Walk * 2f * 3f).Within(1e-5f));
        }

        [Test]
        public void WithTheBobOffTheEyesStayStill()
        {
            Assert.That(ViewMotion.Bob(MathF.PI / 2f, Walk, true, false), Is.EqualTo(0f));
        }

        [Test]
        public void InTheAirTheEyesDoNotBob()
        {
            Assert.That(ViewMotion.Bob(MathF.PI / 2f, Walk, false, true), Is.EqualTo(0f));
        }

        [Test]
        public void AtWalkingSpeedTheBobPeaksAtFourteenMillimetres()
        {
            float peak = ViewMotion.Bob(MathF.PI / 2f, Walk, true, true);
            float trough = ViewMotion.Bob(MathF.PI * 1.5f, Walk, true, true);

            Assert.That(peak, Is.EqualTo(.014f).Within(1e-5f));
            Assert.That(trough, Is.EqualTo(-.014f).Within(1e-5f));
        }

        [Test]
        public void HoweverFastThePlayerTheBobStaysWithinTwentyOneMillimetres()
        {
            Assert.That(ViewMotion.Bob(MathF.PI / 2f, 20f, true, true), Is.EqualTo(.021f).Within(1e-6f));
        }
    }
}
