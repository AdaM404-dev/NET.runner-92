using System.Collections.Generic;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the LATCH-07 tablet's phases. Expected behaviour
    /// comes from docs/systems/player.md and
    /// Northline/Documentation/latch07_unity.md and cctv_unity.md: Q raises
    /// and stows, E taps Link, R cancels, a tap commits at 42% of the touch
    /// clip so a cancel before that leaves the relay offline, and 1 opens the
    /// CCTV page once the reception breach unlocked it.
    /// </summary>
    public sealed class LatchSessionTests
    {
        private const float End = 1f;

        private LatchSession session;
        private List<LatchPhase> announced;
        private int cctvOpened;
        private int linkChecks;

        [SetUp]
        public void NewSession()
        {
            session = new LatchSession();
            announced = new List<LatchPhase>();
            cctvOpened = 0;
            linkChecks = 0;
            session.PhaseChanged += phase => announced.Add(phase);
            session.CctvOpened += () => cctvOpened++;
        }

        [Test]
        public void ItStartsStowed()
        {
            Assert.That(session.Current, Is.EqualTo(LatchPhase.Stowed));
            Assert.That(session.Active, Is.False);
        }

        [Test]
        public void QRaisesTheTabletAndItIsReadyWhenTheEquipClipEnds()
        {
            session.Toggle();
            LatchPhase raising = session.Current;
            session.Advance(End, Linked);

            Assert.That(raising, Is.EqualTo(LatchPhase.Equip));
            Assert.That(session.Current, Is.EqualTo(LatchPhase.Ready));
            Assert.That(announced, Is.EqualTo(new[] { LatchPhase.Equip, LatchPhase.Ready }));
        }

        [Test]
        public void AClipThatHasNotEndedChangesNothing()
        {
            session.Toggle();

            session.Advance(.998f, Linked);

            Assert.That(session.Current, Is.EqualTo(LatchPhase.Equip));
        }

        [Test]
        public void QFromReadyPutsItAwayWithTheUnequipClip()
        {
            Ready();

            session.Toggle();
            LatchPhase leaving = session.Current;
            session.Advance(End, Linked);

            Assert.That(leaving, Is.EqualTo(LatchPhase.Unequip));
            Assert.That(session.Current, Is.EqualTo(LatchPhase.Stowed));
        }

        [Test]
        public void QWhileItIsBusyCancels()
        {
            session.Toggle();

            session.Toggle();

            Assert.That(session.Current, Is.EqualTo(LatchPhase.Cancel));
        }

        [Test]
        public void QWhileItIsBeingPutAwayDoesNothing()
        {
            Ready();
            session.Toggle();

            session.Toggle();

            Assert.That(session.Current, Is.EqualTo(LatchPhase.Unequip));
        }

        [Test]
        public void RCancelsAndItIsStowedWhenTheCancelClipEnds()
        {
            Ready();

            session.Cancel();
            LatchPhase cancelling = session.Current;
            session.Advance(End, Linked);

            Assert.That(cancelling, Is.EqualTo(LatchPhase.Cancel));
            Assert.That(session.Current, Is.EqualTo(LatchPhase.Stowed));
        }

        [Test]
        public void RWhileStowedOrAlreadyCancellingDoesNothing()
        {
            session.Cancel();
            int whileStowed = announced.Count;
            Ready();
            session.Cancel();
            int beforeSecondCancel = announced.Count;
            session.Cancel();

            Assert.That(whileStowed, Is.EqualTo(0));
            Assert.That(announced.Count, Is.EqualTo(beforeSecondCancel));
        }

        [Test]
        public void ETapsOnlyWhileReady()
        {
            session.Toggle();

            session.Tap();

            Assert.That(session.Current, Is.EqualTo(LatchPhase.Equip));
        }

        [Test]
        public void ATapAsksForTheLinkOnceWhenTheFingerReachesTheScreen()
        {
            Ready();
            session.Tap();

            session.Advance(.41f, Linked);
            int beforeTheTouch = linkChecks;
            session.Advance(.42f, Linked);
            session.Advance(.8f, Linked);

            Assert.That(session.Current, Is.EqualTo(LatchPhase.Tap));
            Assert.That(beforeTheTouch, Is.EqualTo(0));
            Assert.That(linkChecks, Is.EqualTo(1));
        }

        [Test]
        public void ATapThatLinksSucceedsAndOneThatDoesNotFails()
        {
            Ready();
            session.Tap();
            session.Advance(End, Linked);
            LatchPhase linked = session.Current;
            session.Advance(End, Linked);
            session.Tap();
            session.Advance(End, NotLinked);

            Assert.That(linked, Is.EqualTo(LatchPhase.Success));
            Assert.That(session.Current, Is.EqualTo(LatchPhase.Failed));
        }

        [Test]
        public void AfterSuccessOrFailureItIsReadyAgain()
        {
            Ready();
            session.Tap();
            session.Advance(End, NotLinked);

            session.Advance(End, Linked);

            Assert.That(session.Current, Is.EqualTo(LatchPhase.Ready));
        }

        [Test]
        public void CancellingBeforeTheTouchNeverAsksForTheLink()
        {
            Ready();
            session.Tap();
            session.Advance(.3f, Linked);

            session.Cancel();
            session.Advance(End, Linked);

            Assert.That(linkChecks, Is.EqualTo(0));
            Assert.That(session.Current, Is.EqualTo(LatchPhase.Stowed));
        }

        [Test]
        public void StowPutsItAwayAtOnceAndAlwaysSaysSo()
        {
            session.Stow();
            Ready();

            session.Stow();

            Assert.That(session.Current, Is.EqualTo(LatchPhase.Stowed));
            Assert.That(announced[0], Is.EqualTo(LatchPhase.Stowed));
            Assert.That(announced[announced.Count - 1], Is.EqualTo(LatchPhase.Stowed));
        }

        [Test]
        public void OneWhileTheCctvIsLockedOnlyShowsTheHintForThreeSeconds()
        {
            bool answered = session.ToggleCctv(false, 10f);

            Assert.That(answered, Is.False);
            Assert.That(session.Current, Is.EqualTo(LatchPhase.Stowed));
            Assert.That(session.IsHintShown(10f), Is.True);
            Assert.That(session.IsHintShown(12.9f), Is.True);
            Assert.That(session.IsHintShown(13f), Is.False);
        }

        [Test]
        public void OneOnceUnlockedRaisesTheTabletWithTheCctvPage()
        {
            bool answered = session.ToggleCctv(true, 0f);
            LatchPhase raising = session.Current;
            int openedWhileRaising = cctvOpened;
            session.Advance(End, Linked);

            Assert.That(answered, Is.True);
            Assert.That(raising, Is.EqualTo(LatchPhase.Equip));
            Assert.That(openedWhileRaising, Is.EqualTo(0));
            Assert.That(session.Current, Is.EqualTo(LatchPhase.Ready));
            Assert.That(session.CctvRequested, Is.True);
            Assert.That(cctvOpened, Is.EqualTo(1));
        }

        [Test]
        public void OneWhileReadyShowsTheCctvAtOnce()
        {
            Ready();

            session.ToggleCctv(true, 0f);

            Assert.That(session.Current, Is.EqualTo(LatchPhase.Ready));
            Assert.That(cctvOpened, Is.EqualTo(1));
        }

        [Test]
        public void OneAgainClosesTheCctvAndPutsTheTabletAway()
        {
            Ready();
            session.ToggleCctv(true, 0f);

            bool answered = session.ToggleCctv(true, 1f);

            Assert.That(answered, Is.True);
            Assert.That(session.CctvRequested, Is.False);
            Assert.That(session.Current, Is.EqualTo(LatchPhase.Unequip));
        }

        [Test]
        public void OpeningTheCctvTakesTheHintDown()
        {
            session.ToggleCctv(false, 0f);

            session.ToggleCctv(true, 1f);

            Assert.That(session.IsHintShown(1f), Is.False);
        }

        [Test]
        public void ETapsNothingWhileTheCctvIsShown()
        {
            Ready();
            session.ToggleCctv(true, 0f);

            session.Tap();

            Assert.That(session.Current, Is.EqualTo(LatchPhase.Ready));
        }

        [Test]
        public void OneDuringATapDoesNothing()
        {
            Ready();
            session.Tap();

            bool answered = session.ToggleCctv(true, 0f);

            Assert.That(answered, Is.False);
            Assert.That(session.Current, Is.EqualTo(LatchPhase.Tap));
            Assert.That(session.CctvRequested, Is.False);
        }

        [Test]
        public void CancellingClosesTheCctv()
        {
            Ready();
            session.ToggleCctv(true, 0f);

            session.Cancel();

            Assert.That(session.CctvRequested, Is.False);
        }

        private bool Linked()
        {
            linkChecks++;
            return true;
        }

        private bool NotLinked()
        {
            linkChecks++;
            return false;
        }

        private void Ready()
        {
            session.Toggle();
            session.Advance(End, Linked);
        }
    }
}
