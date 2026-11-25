using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of what the LATCH-07 screen says in each phase. Expected
    /// values come from the screen table in docs/systems/player.md.
    /// </summary>
    public sealed class LatchStatusTests
    {
        private const int Bars = 14;

        [Test]
        public void RaisingItBoots()
        {
            LatchStatus status = LatchStatus.For(LatchPhase.Equip, false, false, false);

            Assert.That(status.Heading, Is.EqualTo("BOOTING"));
            Assert.That(status.Commands, Is.EqualTo("R  CANCEL"));
            Assert.That(status.Accent, Is.EqualTo(LatchAccent.Cyan));
        }

        [Test]
        public void ReadyWithARelayAcquiredShowsTheRelayInCyan()
        {
            LatchStatus status = LatchStatus.For(LatchPhase.Ready, true, false, false);

            Assert.That(status.Heading, Is.EqualTo("SIGNAL STANDBY"));
            Assert.That(status.Message, Is.EqualTo("TARGET  /  NORTHLINE SERVICE RELAY"));
            Assert.That(status.Accent, Is.EqualTo(LatchAccent.Cyan));
            Assert.That(status.LitBars(Bars), Is.EqualTo(12));
        }

        [Test]
        public void ReadyWithNoRelayIsAmberWithAWeakSignal()
        {
            LatchStatus status = LatchStatus.For(LatchPhase.Ready, false, false, false);

            Assert.That(status.Message, Is.EqualTo("TARGET  /  NO RELAY IN VIEW"));
            Assert.That(status.Accent, Is.EqualTo(LatchAccent.Amber));
            Assert.That(status.LitBars(Bars), Is.EqualTo(2));
        }

        [Test]
        public void ReadyWithTheRelayLinkedSaysRelayOnline()
        {
            LatchStatus status = LatchStatus.For(LatchPhase.Ready, true, true, false);

            Assert.That(status.Heading, Is.EqualTo("RELAY ONLINE"));
        }

        [Test]
        public void OnceTheCctvIsUnlockedReadyOffersOne()
        {
            LatchStatus locked = LatchStatus.For(LatchPhase.Ready, false, false, false);
            LatchStatus unlocked = LatchStatus.For(LatchPhase.Ready, false, false, true);

            Assert.That(locked.Commands, Is.EqualTo("E  LINK     Q  STOW     R  CANCEL"));
            Assert.That(unlocked.Commands, Is.EqualTo("E  LINK     Q  STOW     1  CCTV"));
        }

        [Test]
        public void ATapSaysHandshake()
        {
            Assert.That(LatchStatus.For(LatchPhase.Tap, true, false, false).Heading, Is.EqualTo("HANDSHAKE"));
        }

        [Test]
        public void ALinkLightsEveryBar()
        {
            LatchStatus status = LatchStatus.For(LatchPhase.Success, true, true, false);

            Assert.That(status.Heading, Is.EqualTo("LINK ESTABLISHED"));
            Assert.That(status.LitBars(Bars), Is.EqualTo(Bars));
        }

        [Test]
        public void AFailedLinkSaysSoInTheFaultColour()
        {
            LatchStatus status = LatchStatus.For(LatchPhase.Failed, false, false, false);

            Assert.That(status.Heading, Is.EqualTo("LINK FAILED"));
            Assert.That(status.Message, Is.EqualTo("EQUIP WHILE AIMING AT A RELAY"));
            Assert.That(status.Accent, Is.EqualTo(LatchAccent.Fault));
        }

        [Test]
        public void PuttingItAwaySaysDisconnecting()
        {
            LatchStatus unequip = LatchStatus.For(LatchPhase.Unequip, true, true, true);
            LatchStatus cancel = LatchStatus.For(LatchPhase.Cancel, true, true, true);

            Assert.That(unequip.Heading, Is.EqualTo("DISCONNECTING"));
            Assert.That(cancel.Heading, Is.EqualTo("DISCONNECTING"));
            Assert.That(cancel.Commands, Is.Empty);
            Assert.That(cancel.Accent, Is.EqualTo(LatchAccent.Muted));
        }

        [Test]
        public void AQuarterOfFourteenBarsRoundsToFour()
        {
            // 3.5 bars: a half rounds to the even neighbour.
            Assert.That(LatchStatus.For(LatchPhase.Equip, false, false, false).LitBars(Bars), Is.EqualTo(4));
        }

        [Test]
        public void TheCctvHintPointsToReceptionUntilTheBreach()
        {
            Assert.That(LatchStatus.CctvHint(false), Is.EqualTo("CCTV locked   /   Use the reception terminal"));
            Assert.That(LatchStatus.CctvHint(true), Is.EqualTo("CCTV ready   /   Press 1"));
        }
    }
}
