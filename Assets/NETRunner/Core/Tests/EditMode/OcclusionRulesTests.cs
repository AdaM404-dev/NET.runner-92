using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of which part an object plays in the baked occlusion
    /// culling.
    /// </summary>
    public sealed class OcclusionRulesTests
    {
        [Test]
        public void AnythingThatMovesTakesNoPart()
        {
            Assert.That(OcclusionRules.RoleOf(true, false), Is.EqualTo(OcclusionRole.None));
            Assert.That(OcclusionRules.RoleOf(true, true), Is.EqualTo(OcclusionRole.None));
        }

        [Test]
        public void GlassCanBeHiddenButHidesNothing()
        {
            Assert.That(OcclusionRules.RoleOf(false, true), Is.EqualTo(OcclusionRole.Occludee));
        }

        [Test]
        public void SolidGeometryThatStaysHidesAndCanBeHidden()
        {
            Assert.That(OcclusionRules.RoleOf(false, false), Is.EqualTo(OcclusionRole.OccluderAndOccludee));
        }
    }
}
