using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the reception puzzle. Expected behaviour comes from
    /// docs/systems/doors-and-interaction.md and
    /// Northline/Documentation/cctv_unity.md: three cells start at 01, the
    /// signature is 03 01 04, selection and values wrap, and a wrong route
    /// allows another attempt.
    /// </summary>
    public sealed class SignalPuzzleTests
    {
        private SignalPuzzle puzzle;

        [SetUp]
        public void Open()
        {
            puzzle = new SignalPuzzle();
            puzzle.Reset();
        }

        [Test]
        public void ItStartsAtZeroOneEverywhereOnTheFirstCell()
        {
            Assert.That(puzzle.Length, Is.EqualTo(3));
            Assert.That(puzzle.Cursor, Is.EqualTo(0));
            for (int cell = 0; cell < puzzle.Length; cell++)
            {
                Assert.That(puzzle.RouteAt(cell), Is.EqualTo(1));
            }
        }

        [Test]
        public void TheSignatureIsThreeOneFour()
        {
            Assert.That(new[] { puzzle.SignatureAt(0), puzzle.SignatureAt(1), puzzle.SignatureAt(2) }, Is.EqualTo(new[] { 3, 1, 4 }));
        }

        [Test]
        public void SelectionWrapsAtBothEnds()
        {
            puzzle.Select(-1);
            int fromFirst = puzzle.Cursor;
            puzzle.Select(1);

            Assert.That(fromFirst, Is.EqualTo(2));
            Assert.That(puzzle.Cursor, Is.EqualTo(0));
        }

        [Test]
        public void ValuesWrapBetweenFourAndOne()
        {
            puzzle.Adjust(-1);
            int belowOne = puzzle.RouteAt(0);
            puzzle.Adjust(1);

            Assert.That(belowOne, Is.EqualTo(4));
            Assert.That(puzzle.RouteAt(0), Is.EqualTo(1));
        }

        [Test]
        public void AWrongRouteFailsAndShowsAMismatchUntilTheNextChange()
        {
            bool accepted = puzzle.Submit();
            bool shown = puzzle.Mismatch;
            puzzle.Adjust(1);

            Assert.That(accepted, Is.False);
            Assert.That(shown, Is.True);
            Assert.That(puzzle.Mismatch, Is.False);
        }

        [Test]
        public void TheSignatureIsAccepted()
        {
            puzzle.Adjust(1);
            puzzle.Adjust(1);
            puzzle.Select(-1);
            puzzle.Adjust(-1);

            Assert.That(puzzle.Submit(), Is.True);
            Assert.That(puzzle.Mismatch, Is.False);
        }

        [Test]
        public void ResettingStartsOver()
        {
            puzzle.Adjust(1);
            puzzle.Select(1);
            puzzle.Submit();

            puzzle.Reset();

            Assert.That(puzzle.RouteAt(0), Is.EqualTo(1));
            Assert.That(puzzle.Cursor, Is.EqualTo(0));
            Assert.That(puzzle.Mismatch, Is.False);
        }

        [Test]
        public void ThePromptSaysWhetherTheCctvIsUnlocked()
        {
            Assert.That(SignalPuzzle.Prompt(false), Is.EqualTo("E  Breach reception CCTV terminal"));
            Assert.That(SignalPuzzle.Prompt(true), Is.EqualTo("CCTV unlocked   /   1  Live cameras"));
        }
    }
}
