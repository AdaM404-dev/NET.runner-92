using System.Collections;
using NetRunner.Player;
using NetRunner.Player.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetRunner.World.Tests
{
    /// <summary>
    /// PlayMode tests of the first ascent on test-built floors: standing on a
    /// new floor at the exit of the stair that leads there unlocks it, in
    /// order (west stair to L2, east stair to L3), and nowhere else, nor in
    /// the air. Expected values come from docs/systems/doors-and-interaction.md.
    /// </summary>
    public sealed class StairAccessTests
    {
        private const float FloorHeight = 4.2f;
        private const float WestExit = -32f + 1.05f;
        private const float EastExit = 32f + 1.05f;

        private TestWorld world;
        private StairAccess access;

        [SetUp]
        public void Build()
        {
            world = new TestWorld();
            world.HideHospital();
        }

        [TearDown]
        public void Clear()
        {
            world.Clear();
        }

        [UnityTest]
        public IEnumerator StandingOnL2AtTheWestStairExitUnlocksIt()
        {
            FirstPersonController player = PlayerOnFloor(1, WestExit);

            yield return null;

            Assert.That(access.Highest, Is.EqualTo(1));
            Assert.That(player.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator L3ComesOnlyFromTheEastStairAfterL2()
        {
            FirstPersonController player = PlayerOnFloor(2, EastExit);
            yield return null;
            int beforeL2 = access.Highest;

            Land(player, new Vector3(WestExit, FloorHeight, 0f));
            yield return null;
            Land(player, new Vector3(EastExit, FloorHeight * 2f, 0f));
            yield return null;

            Assert.That(beforeL2, Is.EqualTo(0), "L3 unlocked before L2");
            Assert.That(access.Highest, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator AwayFromTheStairExitNothingUnlocks()
        {
            PlayerOnFloor(1, WestExit + 3f);

            yield return null;

            Assert.That(access.Highest, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator InTheAirNothingUnlocks()
        {
            FirstPersonController player = world.Player(new Vector3(WestExit, FloorHeight + .05f, 0f));
            access = world.Empty("Test stairs", Vector3.zero).AddComponent<StairAccess>();
            access.Configure(player);

            yield return null;

            Assert.That(player.IsGrounded, Is.False);
            Assert.That(access.Highest, Is.EqualTo(0));
        }

        // A player standing on floor (0-based) at the given x, on a slab built for it.
        private FirstPersonController PlayerOnFloor(int floor, float x)
        {
            var feet = new Vector3(x, floor * FloorHeight, 0f);
            FirstPersonController player = world.Player(feet);
            access = world.Empty("Test stairs", Vector3.zero).AddComponent<StairAccess>();
            access.Configure(player);
            Land(player, feet);
            return player;
        }

        // Builds a slab under the feet, puts the player there and lets it settle onto it.
        private void Land(FirstPersonController player, Vector3 feet)
        {
            world.Box("Test slab", feet + Vector3.down * .5f, new Vector3(6f, 1f, 6f));
            player.Warp(feet, Quaternion.identity);
            for (int i = 0; i < 10; i++)
            {
                player.Move(Vector3.zero, false, 1f / 60f);
            }
        }
    }
}
