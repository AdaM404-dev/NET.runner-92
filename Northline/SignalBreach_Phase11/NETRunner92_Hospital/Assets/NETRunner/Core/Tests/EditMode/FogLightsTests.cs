using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the lights in the fog per quality level. Expected
    /// values come from Samuel's decisions of 2026-11-26 on #36 (round 3,
    /// step 2): on Medium the point and spot lights leave the fog; High keeps
    /// them in it; Low has no fog.
    /// </summary>
    public sealed class FogLightsTests
    {
        private const int Low = 0;
        private const int Medium = 1;
        private const int High = 2;

        [TestCase(Low, false)]
        [TestCase(Medium, false)]
        [TestCase(High, true)]
        public void OnlyHighKeepsThePointAndSpotLightsInTheFog(int level, bool inFog)
        {
            Assert.That(FogLights.AreOnFor(level), Is.EqualTo(inFog));
        }
    }
}
