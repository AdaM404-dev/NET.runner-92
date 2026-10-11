using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the easing curve the doors use.
    /// </summary>
    public class EasingTests
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
    }
}
