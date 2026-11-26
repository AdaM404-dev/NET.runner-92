using System;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the Signal Breach puzzle. The rules come from the
    /// Phase 11 hack: 12 s to trace from node 0 to node 7, two strikes on
    /// security nodes end it, a click within 22 px selects a node. Layout 0
    /// makes nodes 4 and 6 security nodes, layout 1 nodes 3 and 5.
    /// </summary>
    public sealed class SignalBreachTests
    {
        [Test]
        public void ItStartsPendingOnTheStartNodeWithTheFocusOnTheFirstNeighbour()
        {
            var breach = new SignalBreach(new FixedRandom(0));

            Assert.That(breach.State, Is.EqualTo(SignalBreach.Outcome.Pending));
            Assert.That(breach.Path, Is.EqualTo(new[] { 0 }));
            Assert.That(breach.Focus, Is.EqualTo(1));
        }

        [Test]
        public void TheRandomSourcePicksTheSecurityLayout()
        {
            var first = new SignalBreach(new FixedRandom(0));
            var second = new SignalBreach(new FixedRandom(1));

            Assert.That(first.Dangerous(4) && first.Dangerous(6) && !first.Dangerous(3), Is.True);
            Assert.That(second.Dangerous(3) && second.Dangerous(5) && !second.Dangerous(4), Is.True);
        }

        [Test]
        public void NothingCanBeTracedBeforeItBegins()
        {
            var breach = new SignalBreach(new FixedRandom(0));

            breach.Confirm();

            Assert.That(breach.Path, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void ConfirmingAlongASafeRouteReachesTheTarget()
        {
            // With nodes 4 and 6 guarded, the safe route is 0, 1, 3, 5, 7; the focus moves ahead after each step.
            var breach = Begun(0);

            breach.Confirm();
            breach.Confirm();
            breach.Confirm();
            breach.Confirm();

            Assert.That(breach.Path, Is.EqualTo(new[] { 0, 1, 3, 5, 7 }));
            Assert.That(breach.State, Is.EqualTo(SignalBreach.Outcome.Successful));
        }

        [Test]
        public void ANodeThatIsNotConnectedIsRefused()
        {
            var breach = Begun(0);

            // The pointer starts on node 1 (180, 103); node 7 is at (580, 153). The mouse reports y up.
            breach.MovePointer(400f, -50f);
            breach.Click();

            Assert.That(breach.Path, Is.EqualTo(new[] { 0 }));
            Assert.That(breach.Feedback, Is.EqualTo(SignalBreach.NotConnected));
        }

        [Test]
        public void AClickWithinTwentyTwoPixelsSelectsTheNode()
        {
            var breach = Begun(0);

            breach.MovePointer(21f, 0f);
            breach.Click();

            Assert.That(breach.Path, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void ASecurityNodeIsAStrikeAndTheSecondStrikeEndsTheBreach()
        {
            var breach = Begun(0);
            breach.Confirm();

            // The keys moved the focus, and the pointer with it, to node 3 (318, 87); node 4 (318, 217) is guarded.
            breach.MovePointer(0f, -130f);
            breach.Click();
            int afterFirst = breach.Strikes;
            var stateAfterFirst = breach.State;
            breach.Click();

            Assert.That(afterFirst, Is.EqualTo(1));
            Assert.That(stateAfterFirst, Is.EqualTo(SignalBreach.Outcome.Tracing));
            Assert.That(breach.Strikes, Is.EqualTo(2));
            Assert.That(breach.State, Is.EqualTo(SignalBreach.Outcome.Terminated));
            Assert.That(breach.Feedback, Is.EqualTo(SignalBreach.SecurityTriggered));
            Assert.That(breach.Path, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void TheBreachEndsTwelveSecondsAfterItBegins()
        {
            var breach = new SignalBreach(new FixedRandom(0));
            breach.Begin(10f);

            breach.Tick(21.9f);
            var before = breach.State;
            float remaining = breach.Remaining(16f);
            breach.Tick(22f);

            Assert.That(before, Is.EqualTo(SignalBreach.Outcome.Tracing));
            Assert.That(remaining, Is.EqualTo(6f).Within(1e-4f));
            Assert.That(breach.State, Is.EqualTo(SignalBreach.Outcome.Terminated));
            Assert.That(breach.Feedback, Is.EqualTo(SignalBreach.TimeLimitExceeded));
        }

        [Test]
        public void UndoStepsBackOneNode()
        {
            var breach = Begun(0);
            breach.Confirm();
            breach.Confirm();

            breach.Undo();

            Assert.That(breach.Path, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(breach.Focus, Is.EqualTo(3));
        }

        [Test]
        public void SelectingANodeOnThePathCutsThePathBackToIt()
        {
            var breach = Begun(0);
            breach.Confirm();
            breach.Confirm();

            // The focus is on node 5; one step back among node 3's neighbours is node 1, already on the path.
            breach.StepFocus(-1);
            breach.Confirm();

            Assert.That(breach.Path, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void ThePointerStaysOnTheScreen()
        {
            var breach = Begun(0);

            breach.MovePointer(-1000f, 1000f);

            Assert.That(breach.Pointer, Is.EqualTo((26f, 62f)));
            Assert.That(breach.UsingMouse, Is.True);
        }

        private static SignalBreach Begun(int layout)
        {
            var breach = new SignalBreach(new FixedRandom(layout));
            breach.Begin(0f);
            return breach;
        }

        // Always returns the same layout index, so a test knows which nodes are guarded.
        private sealed class FixedRandom : Random
        {
            private readonly int value;

            public FixedRandom(int value)
            {
                this.value = value;
            }

            public override int Next(int maxValue) => value;
        }
    }
}
