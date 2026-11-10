using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the easing curves: the S-curve the doors and the
    /// atmosphere zones use, and the straight line the ambience volumes and
    /// light pulses use.
    /// </summary>
    public sealed class EasingTests
    {
        [Test]
        public void StartsAtZeroAndEndsAtOne()
        {
            float atStart = Easing.SmoothStep01(0f);
            float atEnd = Easing.SmoothStep01(1f);

            Assert.That(atStart, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(atEnd, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void HalfwayIsHalf()
        {
            float eased = Easing.SmoothStep01(0.5f);

            Assert.That(eased, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void StartsSlowly()
        {
            // A tenth of the way through, the door has moved less than a tenth (0.028).
            float eased = Easing.SmoothStep01(0.1f);

            Assert.That(eased, Is.LessThan(0.1f));
        }

        [Test]
        public void StaysBetweenZeroAndOneOutsideTheRange()
        {
            float beforeStart = Easing.SmoothStep01(-0.5f);
            float afterEnd = Easing.SmoothStep01(1.5f);

            Assert.That(beforeStart, Is.EqualTo(0f));
            Assert.That(afterEnd, Is.EqualTo(1f));
        }

        [Test]
        public void LerpGoesInAStraightLineFromOneValueToTheOther()
        {
            Assert.That(Easing.Lerp(2f, 6f, 0f), Is.EqualTo(2f));
            Assert.That(Easing.Lerp(2f, 6f, 0.25f), Is.EqualTo(3f).Within(1e-6f));
            Assert.That(Easing.Lerp(2f, 6f, 1f), Is.EqualTo(6f).Within(1e-6f));
            Assert.That(Easing.Lerp(6f, 2f, 0.25f), Is.EqualTo(5f).Within(1e-6f));
        }

        [Test]
        public void LerpStaysAtTheEndsOutsideTheRange()
        {
            Assert.That(Easing.Lerp(2f, 6f, -1f), Is.EqualTo(2f));
            Assert.That(Easing.Lerp(2f, 6f, 3f), Is.EqualTo(6f).Within(1e-6f));
        }
    }
}
