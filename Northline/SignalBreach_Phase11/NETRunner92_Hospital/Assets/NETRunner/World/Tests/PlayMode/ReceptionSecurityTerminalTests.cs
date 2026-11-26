using NetRunner.Player;
using NetRunner.Player.Tests;
using NUnit.Framework;
using UnityEngine;

namespace NetRunner.World.Tests
{
    /// <summary>
    /// PlayMode tests of the reception terminal's puzzle on test-built
    /// objects: E opens it and it takes the keyboard, a wrong route is
    /// rejected and the terminal stays open, the intercepted signature
    /// 03 01 04 unlocks the CCTV and closes it, and its prompt says which.
    /// Expected values come from docs/systems/doors-and-interaction.md and
    /// Northline/Documentation/cctv_unity.md.
    /// </summary>
    public sealed class ReceptionSecurityTerminalTests
    {
        private TestWorld world;
        private FirstPersonController player;
        private HospitalInteraction interaction;
        private SecurityCctv network;
        private ReceptionSecurityTerminal terminal;

        [SetUp]
        public void Build()
        {
            world = new TestWorld();
            world.HideHospital();
            player = world.Player(Vector3.zero);
            interaction = player.gameObject.AddComponent<HospitalInteraction>();
            interaction.enabled = false;
            network = world.Empty("Test CCTV", Vector3.zero).AddComponent<SecurityCctv>();
            terminal = world.Empty("Test terminal", Vector3.forward).AddComponent<ReceptionSecurityTerminal>();
            terminal.Configure(network, null);
        }

        [TearDown]
        public void Clear()
        {
            world.Clear();
        }

        [Test]
        public void UsingItOpensThePuzzleAndTakesTheKeyboard()
        {
            terminal.Interact(player);

            Assert.That(terminal.IsOpen, Is.True);
            Assert.That(interaction.TerminalActive, Is.True);
            Assert.That(terminal.Prompt.Text, Is.EqualTo("Recepciós terminál"));
        }

        [Test]
        public void AWrongRouteIsRejectedAndTheTerminalStaysOpen()
        {
            terminal.Interact(player);

            bool accepted = terminal.Submit();

            Assert.That(accepted, Is.False);
            Assert.That(terminal.IsOpen, Is.True);
            Assert.That(network.Unlocked, Is.False);
        }

        [Test]
        public void TheSignatureUnlocksTheCctvAndClosesTheTerminal()
        {
            terminal.Interact(player);

            // The route starts at 01 01 01: up twice on the first cell gives 03,
            // left wraps to the last cell, and down from 01 wraps to 04.
            terminal.Adjust(1);
            terminal.Adjust(1);
            terminal.Select(-1);
            terminal.Adjust(-1);
            bool accepted = terminal.Submit();

            Assert.That(accepted, Is.True);
            Assert.That(network.Unlocked, Is.True);
            Assert.That(terminal.IsOpen, Is.False);
            Assert.That(interaction.TerminalActive, Is.False);
            Assert.That(terminal.Prompt.Text, Is.EqualTo("Élő kamerák"));
        }

        [Test]
        public void EachCellCountsOneToFourAndRoundAgain()
        {
            terminal.Interact(player);

            // Four steps up bring 01 back to 01; the middle cell of the signature is 01.
            terminal.Select(1);
            for (int i = 0; i < 4; i++)
            {
                terminal.Adjust(1);
            }

            terminal.Select(-1);
            terminal.Adjust(-1);
            terminal.Adjust(-1);
            terminal.Select(-1);
            terminal.Adjust(-1);
            bool accepted = terminal.Submit();

            Assert.That(accepted, Is.True);
        }

        [Test]
        public void OnceUnlockedItDoesNotOpenAgain()
        {
            network.Unlock();

            terminal.Interact(player);

            Assert.That(terminal.IsOpen, Is.False);
        }

        [Test]
        public void ClosingGivesTheKeyboardBack()
        {
            terminal.Interact(player);

            terminal.Close();

            Assert.That(terminal.IsOpen, Is.False);
            Assert.That(interaction.TerminalActive, Is.False);
        }
    }
}
