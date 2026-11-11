using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the room lookup: floors numbered from 1 and counted
    /// from 0.2 m below their level, tiles with their edges included.
    /// </summary>
    public sealed class RoomPlanTests
    {
        private const float FloorHeight = 4.2f;

        [Test]
        public void TheGroundIsFloorOne()
        {
            Assert.That(RoomPlan.FloorAt(0f, FloorHeight), Is.EqualTo(1));
        }

        [Test]
        public void ALittleBelowAFloorStillCountsAsThatFloor()
        {
            Assert.That(RoomPlan.FloorAt(4.0f, FloorHeight), Is.EqualTo(2));
            Assert.That(RoomPlan.FloorAt(-.19f, FloorHeight), Is.EqualTo(1));
        }

        [Test]
        public void MoreThanTwentyCentimetresBelowGroundIsBelowFloorOne()
        {
            Assert.That(RoomPlan.FloorAt(-.25f, FloorHeight), Is.EqualTo(0));
        }

        [Test]
        public void ATileIncludesItsEdges()
        {
            Assert.That(RoomPlan.OnTile(1f, 2f, 1f, 3f, 2f, 4f), Is.True);
            Assert.That(RoomPlan.OnTile(3.01f, 2f, 1f, 3f, 2f, 4f), Is.False);
        }
    }
}
