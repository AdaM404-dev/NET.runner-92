using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the first ascent. Expected values come from
    /// docs/systems/doors-and-interaction.md: the west stair leads to L2, the
    /// east stair to L3 and the west stair to L4; a floor unlocks when the
    /// player stands on it within 2 m of its stair exit (1.05 m beyond the
    /// core, z −1.95 to 2.15 m) and within 0.12 m of its height.
    /// </summary>
    public sealed class StairRulesTests
    {
        private const float WestExit = -32f + 1.05f;
        private const float EastExit = 32f + 1.05f;

        [Test]
        public void TheStairsAlternateWestEastWest()
        {
            Assert.That(StairRules.CoreForLeg(0), Is.EqualTo(-32));
            Assert.That(StairRules.CoreForLeg(1), Is.EqualTo(32));
            Assert.That(StairRules.CoreForLeg(2), Is.EqualTo(-32));
        }

        [Test]
        public void StandingOnL2AtTheWestStairExitUnlocksIt()
        {
            Assert.That(StairRules.Reached(0, true, WestExit, 4.2f, 0f), Is.EqualTo(1));
        }

        [Test]
        public void L3UnlocksAtTheEastStairExitAfterL2()
        {
            Assert.That(StairRules.Reached(0, true, EastExit, 8.4f, 0f), Is.EqualTo(0));
            Assert.That(StairRules.Reached(1, true, EastExit, 8.4f, 0f), Is.EqualTo(2));
        }

        [Test]
        public void TheExitCountsWithinTwoMetresAndBetweenItsWalls()
        {
            Assert.That(StairRules.Reached(0, true, WestExit + 1.9f, 4.2f, 2.1f), Is.EqualTo(1));
            Assert.That(StairRules.Reached(0, true, WestExit + 2.1f, 4.2f, 0f), Is.EqualTo(0));
            Assert.That(StairRules.Reached(0, true, WestExit, 4.2f, -2f), Is.EqualTo(0));
            Assert.That(StairRules.Reached(0, true, WestExit, 4.2f, 2.2f), Is.EqualTo(0));
        }

        [Test]
        public void OnlyTheFloorItselfCountsNotTheStairAboveOrBelowIt()
        {
            Assert.That(StairRules.Reached(0, true, WestExit, 4.31f, 0f), Is.EqualTo(1));
            Assert.That(StairRules.Reached(0, true, WestExit, 4.33f, 0f), Is.EqualTo(0));
        }

        [Test]
        public void InTheAirNothingUnlocks()
        {
            Assert.That(StairRules.Reached(0, false, WestExit, 4.2f, 0f), Is.EqualTo(0));
        }

        [Test]
        public void L4IsTheLastFloor()
        {
            Assert.That(StairRules.Reached(2, true, WestExit, 12.6f, 0f), Is.EqualTo(3));
            Assert.That(StairRules.Reached(3, true, EastExit, 16.8f, 0f), Is.EqualTo(3));
        }
    }
}
