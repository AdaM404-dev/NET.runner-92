using System.Collections.Generic;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of how a frame of player movement is split. Expected
    /// values come from docs/systems/player.md: steps of at most 1/90 s,
    /// and at most 0.1 s of a frame is moved.
    /// </summary>
    public sealed class MotionStepsTests
    {
        [Test]
        public void AFrameAtSixtyFramesASecondIsMovedInTwoSteps()
        {
            List<float> steps = Split(1f / 60f);

            Assert.That(steps.Count, Is.EqualTo(2));
            Assert.That(steps[0], Is.EqualTo(1f / 90f));
            Assert.That(steps[1], Is.EqualTo(1f / 180f).Within(1e-6f));
        }

        [Test]
        public void NoStepIsLongerThanANinetiethOfASecond()
        {
            List<float> steps = Split(1f / 20f);

            Assert.That(steps, Has.All.LessThanOrEqualTo(1f / 90f));
            Assert.That(Sum(steps), Is.EqualTo(1f / 20f).Within(1e-5f));
        }

        [Test]
        public void AfterALongHitchOnlyATenthOfASecondIsMoved()
        {
            List<float> steps = Split(.5f);

            Assert.That(Sum(steps), Is.EqualTo(.1f).Within(1e-5f));
            Assert.That(steps.Count, Is.EqualTo(9));
        }

        [Test]
        public void ATinyLeftoverIsNotAStep()
        {
            Assert.That(MotionSteps.Next(.000005f), Is.EqualTo(0f));
            Assert.That(MotionSteps.Next(0f), Is.EqualTo(0f));
        }

        // The steps a caller takes, as FirstPersonController does.
        private static List<float> Split(float dt)
        {
            var steps = new List<float>();
            float remaining = MotionSteps.Budget(dt);
            for (float step = MotionSteps.Next(remaining); step > 0f; step = MotionSteps.Next(remaining))
            {
                steps.Add(step);
                remaining -= step;
            }

            return steps;
        }

        private static float Sum(List<float> steps)
        {
            float sum = 0f;
            foreach (float step in steps)
            {
                sum += step;
            }

            return sum;
        }
    }
}
