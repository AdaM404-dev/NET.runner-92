using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of <see cref="Scalar"/>, which must give what Unity's
    /// <c>Mathf.MoveTowards</c> and <c>Mathf.Repeat</c> give.
    /// </summary>
    public sealed class ScalarTests
    {
        [Test]
        public void MovingTowardsATargetInReachLandsOnIt()
        {
            Assert.That(Scalar.MoveTowards(.9f, 1f, .25f), Is.EqualTo(1f));
        }

        [Test]
        public void MovingTowardsATargetOutOfReachMovesByTheStep()
        {
            Assert.That(Scalar.MoveTowards(0f, 1f, .25f), Is.EqualTo(.25f));
            Assert.That(Scalar.MoveTowards(1f, 0f, .25f), Is.EqualTo(.75f));
        }

        [Test]
        public void RepeatWrapsLikeAClock()
        {
            Assert.That(Scalar.Repeat(12.5f, 10f), Is.EqualTo(2.5f).Within(1e-5f));
            Assert.That(Scalar.Repeat(-1f, 10f), Is.EqualTo(9f).Within(1e-5f));
            Assert.That(Scalar.Repeat(20f, 10f), Is.EqualTo(0f));
        }
    }
}
