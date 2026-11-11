using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of which saves the game accepts, from the Phase 11 save
    /// format: version 1, inside ±220 m and between 1 m below ground and 16 m
    /// up, floors 0 to 3, a named location of at most 128 characters and at
    /// most 200 open doors.
    /// </summary>
    public sealed class CheckpointRulesTests
    {
        private const long Ticks = 638_000_000_000_000_000;

        [Test]
        public void AnOrdinarySaveIsAccepted()
        {
            Assert.That(Valid(), Is.True);
        }

        [Test]
        public void AnotherVersionIsRejected()
        {
            Assert.That(CheckpointRules.IsValid(2, Ticks, 0, 0, 0, 0, 0, 0, "Recepció", 0), Is.False);
        }

        [Test]
        public void APositionOutsideTheCampusIsRejected()
        {
            Assert.That(Valid(x: 220f), Is.False);
            Assert.That(Valid(y: 16.1f), Is.False);
            Assert.That(Valid(y: -1.1f), Is.False);
        }

        [Test]
        public void AFloorThatDoesNotExistIsRejected()
        {
            Assert.That(Valid(floor: 4), Is.False);
            Assert.That(Valid(floor: -1), Is.False);
        }

        [Test]
        public void ALookingAngleBeyondStraightUpIsRejected()
        {
            Assert.That(Valid(pitch: 91f), Is.False);
        }

        [Test]
        public void AMissingOrOverlongLocationIsRejected()
        {
            Assert.That(Valid(location: " "), Is.False);
            Assert.That(Valid(location: new string('a', 129)), Is.False);
        }

        [Test]
        public void TooManyOpenDoorsAreRejected()
        {
            Assert.That(Valid(doors: 201), Is.False);
        }

        [Test]
        public void TheLatestSaveIsTheMostRecent()
        {
            Assert.That(CheckpointRules.MostRecent(new long[] { 5, 9, 7 }), Is.EqualTo(1));
        }

        [Test]
        public void WithoutSavesThereIsNoMostRecent()
        {
            Assert.That(CheckpointRules.MostRecent(new long[] { 0, 0, 0 }), Is.EqualTo(-1));
        }

        private static bool Valid(float x = 10f, float y = 0f, int floor = 1, float pitch = 10f, string location = "Recepció", int doors = 3)
        {
            return CheckpointRules.IsValid(CheckpointRules.Version, Ticks, x, y, 5f, 90f, pitch, floor, location, doors);
        }
    }
}
