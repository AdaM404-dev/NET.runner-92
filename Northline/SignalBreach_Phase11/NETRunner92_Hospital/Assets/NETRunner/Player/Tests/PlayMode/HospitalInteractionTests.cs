using NUnit.Framework;
using UnityEngine;

namespace NetRunner.Player.Tests
{
    /// <summary>
    /// PlayMode tests of what the player aims at: an interactable in front of
    /// the eyes within the 2.65 m reach, through objects on the Ignore
    /// Raycast layer, and also when its collider is a trigger; nothing
    /// further away or behind a wall. Expected values come from
    /// docs/systems/player.md.
    /// </summary>
    public sealed class HospitalInteractionTests
    {
        private const float Reach = 2.65f;
        private const float TargetSize = .5f;

        private TestWorld world;
        private FirstPersonController player;
        private HospitalInteraction interaction;

        [SetUp]
        public void Build()
        {
            world = new TestWorld();
            world.HideHospital();
            player = world.Player(Vector3.zero);
            interaction = player.gameObject.AddComponent<HospitalInteraction>();
            interaction.enabled = false;
        }

        [TearDown]
        public void Clear()
        {
            world.Clear();
        }

        [Test]
        public void ItTargetsAnInteractableInFrontOfTheEyesWithinReach()
        {
            TestInteractable target = Target(Reach - .1f);

            Assert.That(interaction.TargetInView() as Object, Is.SameAs(target));
        }

        [Test]
        public void NothingBeyondReachIsTargeted()
        {
            Target(Reach + .1f);

            Assert.That(interaction.TargetInView(), Is.Null);
        }

        [Test]
        public void ItAimsThroughObjectsThatRaysIgnore()
        {
            TestInteractable target = Target(2f);
            world.IgnoredBox("Ignored pane", Eyes() + Vector3.forward, new Vector3(1f, 1f, .05f));

            Assert.That(interaction.TargetInView() as Object, Is.SameAs(target));
        }

        [Test]
        public void AWallBlocksTheAim()
        {
            Target(2f);
            world.Box("Wall", Eyes() + Vector3.forward, new Vector3(1f, 1f, .05f));

            Assert.That(interaction.TargetInView(), Is.Null);
        }

        [Test]
        public void ATriggerColliderCanBeAimedAt()
        {
            TestInteractable target = Target(2f);
            target.GetComponent<BoxCollider>().isTrigger = true;
            Physics.SyncTransforms();

            Assert.That(interaction.TargetInView() as Object, Is.SameAs(target));
        }

        private Vector3 Eyes()
        {
            return player.Eyes.transform.position;
        }

        // An interactable box whose near face is this far in front of the eyes.
        private TestInteractable Target(float distance)
        {
            Vector3 centre = Eyes() + Vector3.forward * (distance + TargetSize * .5f);
            GameObject box = world.Box("Target", centre, Vector3.one * TargetSize);
            return box.AddComponent<TestInteractable>();
        }
    }
}
