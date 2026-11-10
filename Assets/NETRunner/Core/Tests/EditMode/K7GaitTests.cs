using System;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the K-7's walk. The numbers come from
    /// docs/systems/enemy-k7.md: a full stride every 0.78 m (2π / 8.1),
    /// thighs swinging 23° around a 4° lean, knees bending up to 26°, arms
    /// swinging 15°, a 3.2 cm bob, and the head scanning ±8° walking and
    /// ±23° standing.
    /// </summary>
    public sealed class K7GaitTests
    {
        private const float Stride = 2f * MathF.PI / 8.1f;

        // A quarter of a stride in, the left leg is furthest back and the right furthest forward.
        private const float QuarterStride = Stride / 4f;

        [Test]
        public void StandingStillTheLimbsRestAndTheBodyDoesNotBob()
        {
            foreach (float walked in new[] { 0f, QuarterStride, 12.7f })
            {
                for (int side = K7Gait.Left; side <= K7Gait.Right; side++)
                {
                    Assert.That(K7Gait.Thigh(walked, side, false), Is.Zero);
                    Assert.That(K7Gait.Knee(walked, side, false), Is.Zero);
                    Assert.That(K7Gait.Foot(walked, side, false), Is.Zero);
                    Assert.That(K7Gait.Arm(walked, side, false), Is.Zero);
                }

                Assert.That(K7Gait.Pelvis(walked, false), Is.Zero);
                Assert.That(K7Gait.Bob(walked, false), Is.Zero);
            }
        }

        [Test]
        public void TheThighsSwingTwentyThreeDegreesEachWayAroundAFourDegreeLean()
        {
            Assert.That(K7Gait.Thigh(QuarterStride, K7Gait.Left, true), Is.EqualTo(23f - 4f).Within(1e-3f));
            Assert.That(K7Gait.Thigh(QuarterStride, K7Gait.Right, true), Is.EqualTo(-23f - 4f).Within(1e-3f));
        }

        [Test]
        public void OnlyTheLegSwingingForwardBendsItsKnee()
        {
            Assert.That(K7Gait.Knee(QuarterStride, K7Gait.Left, true), Is.EqualTo(0f).Within(1e-3f));
            Assert.That(K7Gait.Knee(QuarterStride, K7Gait.Right, true), Is.EqualTo(26f).Within(1e-3f));
            for (float walked = 0f; walked < Stride; walked += Stride / 16f)
            {
                Assert.That(K7Gait.Knee(walked, K7Gait.Left, true), Is.GreaterThanOrEqualTo(0f));
            }
        }

        [Test]
        public void EachArmSwingsFifteenDegreesAgainstItsLeg()
        {
            Assert.That(K7Gait.Arm(QuarterStride, K7Gait.Left, true), Is.EqualTo(-15f).Within(1e-3f));
            Assert.That(K7Gait.Arm(QuarterStride, K7Gait.Right, true), Is.EqualTo(15f).Within(1e-3f));
        }

        [Test]
        public void TheBodyBobsThreePointTwoCentimetresOnEveryStep()
        {
            Assert.That(K7Gait.Bob(0f, true), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(K7Gait.Bob(QuarterStride, true), Is.EqualTo(0.032f).Within(1e-5f));
            Assert.That(K7Gait.Bob(Stride / 2f, true), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(K7Gait.Bob(3f * QuarterStride, true), Is.EqualTo(0.032f).Within(1e-5f));
        }

        [Test]
        public void TheWalkRepeatsAfterOneStride()
        {
            const float walked = 0.3f;

            Assert.That(K7Gait.Thigh(walked + Stride, K7Gait.Left, true), Is.EqualTo(K7Gait.Thigh(walked, K7Gait.Left, true)).Within(1e-3f));
            Assert.That(K7Gait.Foot(walked + Stride, K7Gait.Right, true), Is.EqualTo(K7Gait.Foot(walked, K7Gait.Right, true)).Within(1e-3f));
            Assert.That(K7Gait.Pelvis(walked + Stride, true), Is.EqualTo(K7Gait.Pelvis(walked, true)).Within(1e-3f));
        }

        [Test]
        public void StandingTheHeadScansTwentyThreeDegreesEachWayEveryThreeSeconds()
        {
            // 2.1 radians of sweep per second: a full sweep every 2π / 2.1 = 3.0 s.
            float quarterSweep = MathF.PI / 2f / 2.1f;

            Assert.That(K7Gait.Head(quarterSweep, 0f, false), Is.EqualTo(23f).Within(1e-3f));
            Assert.That(K7Gait.Head(3f * quarterSweep, 0f, false), Is.EqualTo(-23f).Within(1e-3f));
        }

        [Test]
        public void WalkingTheHeadScansEightDegreesEachWayEveryNineSeconds()
        {
            // 0.72 radians of sweep per second: a full sweep every 2π / 0.72 = 8.7 s.
            float quarterSweep = MathF.PI / 2f / 0.72f;

            Assert.That(K7Gait.Head(quarterSweep, 0f, true), Is.EqualTo(8f).Within(1e-3f));
        }

        [Test]
        public void TheOffsetMovesWhereInItsSweepTheHeadStarts()
        {
            Assert.That(K7Gait.Head(0f, 0f, false), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(K7Gait.Head(0f, MathF.PI / 2f, false), Is.EqualTo(23f).Within(1e-3f));
        }
    }
}
