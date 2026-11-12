namespace NetRunner.Core
{
    /// <summary>
    /// The security cameras shown on the LATCH-07: stepping through the feeds
    /// wraps round at both ends, and the watched feed renders at most
    /// <see cref="FramesPerSecond"/> times a second, which spares the GPU a
    /// second full camera. Plain maths (see CctvFeedsTests).
    /// </summary>
    public static class CctvFeeds
    {
        /// <summary>How often the watched feed renders, at most.</summary>
        public const float FramesPerSecond = 15f;

        /// <summary>Seconds between two renders of the feed.</summary>
        public const float FrameInterval = 1f / FramesPerSecond;

        /// <summary>The feed after stepping <paramref name="direction"/> (+1 or −1) from <paramref name="selected"/>, wrapping round.</summary>
        public static int Step(int selected, int direction, int count)
        {
            return (selected + direction + count) % count;
        }

        /// <summary>Whether a render is due at <paramref name="now"/> when the next one was planned for <paramref name="nextFrame"/>.</summary>
        public static bool IsFrameDue(float now, float nextFrame)
        {
            return now >= nextFrame;
        }

        /// <summary>When the next render is due after one at <paramref name="now"/>.</summary>
        public static float NextFrame(float now)
        {
            return now + FrameInterval;
        }
    }
}
