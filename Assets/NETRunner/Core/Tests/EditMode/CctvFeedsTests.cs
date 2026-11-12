using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the CCTV feeds. Expected values come from
    /// Northline/Documentation/cctv_unity.md: six feeds, selection wraps at
    /// both ends, and the watched feed renders at up to 15 frames a second.
    /// </summary>
    public sealed class CctvFeedsTests
    {
        private const int Feeds = 6;

        [Test]
        public void StepsMoveOneFeedEitherWay()
        {
            Assert.That(CctvFeeds.Step(2, 1, Feeds), Is.EqualTo(3));
            Assert.That(CctvFeeds.Step(2, -1, Feeds), Is.EqualTo(1));
        }

        [Test]
        public void StepsWrapRoundAtBothEnds()
        {
            Assert.That(CctvFeeds.Step(Feeds - 1, 1, Feeds), Is.EqualTo(0));
            Assert.That(CctvFeeds.Step(0, -1, Feeds), Is.EqualTo(Feeds - 1));
        }

        [Test]
        public void AtOneHundredAndFortyFourFramesASecondTheFeedRendersFifteenTimesASecond()
        {
            const int GameFrames = 144;
            float next = 0f;
            int renders = 0;
            for (int frame = 0; frame < GameFrames; frame++)
            {
                float now = frame / (float)GameFrames;
                if (CctvFeeds.IsFrameDue(now, next))
                {
                    renders++;
                    next = CctvFeeds.NextFrame(now);
                }
            }

            Assert.That(renders, Is.EqualTo(15));
        }

        [Test]
        public void AFeedThatWasJustSwitchedRendersAtOnce()
        {
            // Switching camera plans the next render for time 0, which is always due.
            Assert.That(CctvFeeds.IsFrameDue(123.4f, 0f), Is.True);
        }
    }
}
