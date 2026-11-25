using NUnit.Framework;
using UnityEngine;

namespace NetRunner.Player.Tests
{
    /// <summary>
    /// PlayMode tests of the LATCH-07 service relay: it links when the
    /// player's eyes are within the 2.65 m reach and can see it, and then
    /// says so and lights its lamp; out of reach or behind a wall it does
    /// not link. Expected values come from docs/systems/player.md.
    /// </summary>
    public sealed class LatchRelayTests
    {
        private const float Reach = 2.65f;
        private const string EmissionProperty = "_EmissiveColor";

        private TestWorld world;
        private FirstPersonController player;

        [SetUp]
        public void Build()
        {
            world = new TestWorld();
            world.HideHospital();
            player = world.Player(Vector3.zero);
        }

        [TearDown]
        public void Clear()
        {
            world.Clear();
        }

        [Test]
        public void ARelayInReachAndInSightLinksAndLightsItsLamp()
        {
            LatchRelay relay = world.Relay(Eyes() + Vector3.forward * 1.5f, out Renderer lamp);
            float before = Emission(lamp).g;

            bool linked = relay.TryLink(player);

            Assert.That(linked, Is.True);
            Assert.That(relay.Linked, Is.True);
            Assert.That(relay.Prompt, Is.EqualTo("Q  Service relay linked"));
            Assert.That(Emission(lamp).g, Is.GreaterThan(before), "The lamp did not light up");
        }

        [Test]
        public void ARelayOutOfReachDoesNotLink()
        {
            LatchRelay relay = world.Relay(Eyes() + Vector3.forward * (Reach + .2f), out Renderer _);

            Assert.That(relay.TryLink(player), Is.False);
            Assert.That(relay.Linked, Is.False);
            Assert.That(relay.Prompt, Is.EqualTo("Q  Open LATCH-07 / service relay"));
        }

        [Test]
        public void ARelayBehindAWallDoesNotLink()
        {
            LatchRelay relay = world.Relay(Eyes() + Vector3.forward * 1.5f, out Renderer _);
            world.Box("Wall", Eyes() + Vector3.forward * .75f, new Vector3(1f, 1f, .05f));

            Assert.That(relay.TryLink(player), Is.False);
            Assert.That(relay.Linked, Is.False);
        }

        private static Color Emission(Renderer lamp)
        {
            var block = new MaterialPropertyBlock();
            lamp.GetPropertyBlock(block);
            return block.GetColor(EmissionProperty);
        }

        private Vector3 Eyes()
        {
            return player.Eyes.transform.position;
        }
    }
}
