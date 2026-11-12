using System.Collections;
using NetRunner.Player;
using NetRunner.Player.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetRunner.World.Tests
{
    /// <summary>
    /// PlayMode tests of one hospital door on test-built leaves: how long a
    /// swing and a sliding door take, how the leaf eases, that E never shuts
    /// a door on the player and a closing door opens again when the player
    /// steps in, and what locked, lift and offline doors say. Expected
    /// values come from docs/systems/doors-and-interaction.md.
    /// </summary>
    public sealed class HospitalDoorTests
    {
        private const float Frame = 1f / 60f;
        private const float SwingSeconds = 1f / 1.9f;
        private const float SlideSeconds = 1f / 1.55f;
        private const float Width = 1.2f;

        private static readonly Vector3 ClosedPose = new Vector3(.3f, 1f, 0f);
        private static readonly Vector3 OpenPose = new Vector3(1.3f, 1f, 0f);

        private TestWorld world;
        private FirstPersonController player;

        [SetUp]
        public void Build()
        {
            world = new TestWorld();
            world.HideHospital();
            player = world.Player(new Vector3(0f, 0f, -3f));
        }

        [TearDown]
        public void Clear()
        {
            Time.captureDeltaTime = 0f;
            world.Clear();
        }

        [Test]
        public void ASwingDoorOpensInAboutHalfASecond()
        {
            HospitalDoor door = Door(true);

            door.Interact(player);
            door.Advance(SwingSeconds - .03f);
            bool openEarly = door.Open;
            door.Advance(.03f);

            Assert.That(openEarly, Is.False);
            Assert.That(door.Open, Is.True);
            Assert.That(door.Prompt, Is.EqualTo("E  Close door"));
        }

        [Test]
        public void ASlidingDoorOpensInAboutTwoThirdsOfASecond()
        {
            HospitalDoor door = Door(false);

            door.Interact(player);
            door.Advance(SlideSeconds - .03f);
            bool openEarly = door.Open;
            door.Advance(.03f);

            Assert.That(openEarly, Is.False);
            Assert.That(door.Open, Is.True);
        }

        [Test]
        public void TheLeafEasesInAndOut()
        {
            HospitalDoor door = Door(true);
            Transform leaf = door.transform.GetChild(0);

            door.Interact(player);
            door.Advance(SwingSeconds * .25f);
            Vector3 quarter = leaf.localPosition;
            door.Advance(SwingSeconds * .25f);
            Vector3 half = leaf.localPosition;

            // A quarter of the time moves the leaf 15.6% of the way (an S-curve), half the time half the way.
            Assert.That(Vector3.Distance(quarter, Vector3.Lerp(ClosedPose, OpenPose, .15625f)), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(half, Vector3.Lerp(ClosedPose, OpenPose, .5f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void EDoesNotShutADoorOnThePlayer()
        {
            HospitalDoor door = Door(true);
            door.Interact(player);
            door.Advance(1f);
            player.Warp(door.transform.position, Quaternion.identity);

            door.Interact(player);

            Assert.That(door.Prompt, Is.EqualTo("E  Close door"), "The door agreed to close on the player");
        }

        [UnityTest]
        public IEnumerator AClosingDoorOpensAgainWhenThePlayerStepsIn()
        {
            Time.captureDeltaTime = Frame;
            HospitalDoor door = Door(true);
            door.Interact(player);
            door.Advance(1f);
            door.Interact(player);
            player.Warp(door.transform.position, Quaternion.identity);

            yield return null;

            Assert.That(door.Prompt, Is.EqualTo("E  Close door"), "The closing door did not open again");
        }

        [Test]
        public void ALockedDoorSaysLockedAndIgnoresE()
        {
            HospitalDoor door = Door(true);
            door.SetLocked(true);

            door.Interact(player);
            door.Advance(1f);

            Assert.That(door.Prompt, Is.EqualTo("Locked"));
            Assert.That(door.Closed, Is.True);
        }

        [Test]
        public void LiftDoorsIgnoreEAndOpenOnlyForTheLift()
        {
            HospitalDoor door = Door(false, liftOnly: true);

            door.Interact(player);
            door.Advance(1f);
            bool openedByHand = !door.Closed;
            door.SetLiftOpen(true);
            door.Advance(1f);

            Assert.That(door.Prompt, Is.EqualTo("Use the lift call button"));
            Assert.That(openedByHand, Is.False);
            Assert.That(door.Open, Is.True);
        }

        [Test]
        public void AnOfflineLiftDoorStaysShut()
        {
            HospitalDoor door = Door(false, liftOnly: true, offline: true);

            door.SetLiftOpen(true);
            door.Advance(1f);

            Assert.That(door.Prompt, Is.EqualTo("Lift offline"));
            Assert.That(door.Closed, Is.True);
        }

        [Test]
        public void TheDoorwayIsAsDeepAsTheLeafSwings()
        {
            HospitalDoor swing = Door(true);
            HospitalDoor slide = Door(false);

            // A metre in front of the opening: inside a swing door's 1.32 m, outside a sliding door's 0.43 m.
            player.Warp(swing.transform.position + Vector3.back, Quaternion.identity);
            bool blocksSwing = swing.Obstructed(player);
            player.Warp(slide.transform.position + Vector3.back, Quaternion.identity);
            bool blocksSlide = slide.Obstructed(player);

            Assert.That(blocksSwing, Is.True);
            Assert.That(blocksSlide, Is.False);
        }

        [Test]
        public void BesideTheOpeningThePlayerIsOutOfTheWay()
        {
            HospitalDoor door = Door(true);

            // 0.28 m beyond the opening's edge is still in the way; 0.3 m is not.
            player.Warp(door.transform.position + Vector3.right * (Width * .5f + .27f), Quaternion.identity);
            bool justInside = door.Obstructed(player);
            player.Warp(door.transform.position + Vector3.right * (Width * .5f + .3f), Quaternion.identity);
            bool outside = door.Obstructed(player);

            Assert.That(justInside, Is.True);
            Assert.That(outside, Is.False);
        }

        private HospitalDoor Door(bool swing, bool liftOnly = false, bool offline = false)
        {
            GameObject root = world.Empty("Test door", new Vector3(swing ? 0f : 10f, 0f, 0f));
            GameObject leaf = world.Box("Test leaf", Vector3.zero, new Vector3(.9f, 2f, .05f));
            leaf.transform.SetParent(root.transform, false);
            var panel = new HospitalDoor.Panel(leaf.transform, ClosedPose, OpenPose, Quaternion.identity, Quaternion.identity);
            var door = root.AddComponent<HospitalDoor>();
            door.Configure(new[] { panel }, Width, swing, liftOnly, offline);
            return door;
        }
    }
}
