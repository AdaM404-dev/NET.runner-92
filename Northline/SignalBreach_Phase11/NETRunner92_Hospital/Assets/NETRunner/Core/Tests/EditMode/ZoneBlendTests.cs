using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of an atmosphere zone's weight. The numbers come from
    /// docs/systems/atmosphere.md: 1 inside the box, an S-curve
    /// (3t² − 2t³) down to 0 at the blend distance outside, measured as the
    /// straight distance to the nearest point of the box.
    /// </summary>
    public sealed class ZoneBlendTests
    {
        private const float Blend = 2f;

        // 4 m wide, 3 m high and 6 m deep: the faces are 2, 1.5 and 3 m from the centre.
        private static readonly (float X, float Y, float Z) Size = (4f, 3f, 6f);

        [Test]
        public void InsideTheBoxAndOnItsFacesTheWeightIsOne()
        {
            Assert.That(ZoneBlend.Weight((0f, 0f, 0f), Size, Blend), Is.EqualTo(1f));
            Assert.That(ZoneBlend.Weight((1.9f, -1.4f, 2.9f), Size, Blend), Is.EqualTo(1f));
            Assert.That(ZoneBlend.Weight((2f, 0f, 0f), Size, Blend), Is.EqualTo(1f));
        }

        [Test]
        public void OutsideAFaceItFollowsAnSCurveOverTheBlendDistance()
        {
            // A quarter, half and three quarters of the 2 m blend: the S-curve gives 0.15625, 0.5 and 0.84375.
            Assert.That(ZoneBlend.Weight((2.5f, 0f, 0f), Size, Blend), Is.EqualTo(0.84375f).Within(1e-5f));
            Assert.That(ZoneBlend.Weight((3f, 0f, 0f), Size, Blend), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(ZoneBlend.Weight((3.5f, 0f, 0f), Size, Blend), Is.EqualTo(0.15625f).Within(1e-5f));
        }

        [Test]
        public void AtTheBlendDistanceAndBeyondTheWeightIsZero()
        {
            Assert.That(ZoneBlend.Weight((4f, 0f, 0f), Size, Blend), Is.EqualTo(0f));
            Assert.That(ZoneBlend.Weight((0f, 0f, 100f), Size, Blend), Is.EqualTo(0f));
        }

        [Test]
        public void EverySideOfTheBoxCountsAlike()
        {
            // 1 m outside the faces at -x, -y and +y.
            Assert.That(ZoneBlend.Weight((-3f, 0f, 0f), Size, Blend), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(ZoneBlend.Weight((0f, -2.5f, 0f), Size, Blend), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(ZoneBlend.Weight((0f, 2.5f, 0f), Size, Blend), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void PastACornerItCountsTheStraightDistanceToTheBox()
        {
            // 0.6 m past the x face and 0.8 m past the z face is 1 m from the box: halfway down.
            Assert.That(ZoneBlend.Weight((2.6f, 0f, 3.8f), Size, Blend), Is.EqualTo(0.5f).Within(1e-5f));

            // 0.6 m past the x face alone is 0.3 of the blend: 1 − 0.216.
            Assert.That(ZoneBlend.Weight((2.6f, 0f, 0f), Size, Blend), Is.EqualTo(0.784f).Within(1e-5f));
        }
    }
}
