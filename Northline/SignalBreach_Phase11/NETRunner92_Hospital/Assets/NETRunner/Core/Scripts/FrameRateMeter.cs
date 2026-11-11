namespace NetRunner.Core
{
    /// <summary>
    /// Frames per second, averaged over a short window so the number is
    /// readable instead of jumping every frame. Feed it each frame's
    /// duration; the value updates once per window (see FrameRateMeterTests).
    /// </summary>
    public sealed class FrameRateMeter
    {
        private readonly float window;
        private float elapsed;
        private int frames;

        /// <param name="window">Seconds to average over; values below a millisecond count as one millisecond.</param>
        public FrameRateMeter(float window)
        {
            this.window = window < 0.001f ? 0.001f : window;
        }

        /// <summary>The average of the last full window; 0 until the first window is complete.</summary>
        public float FramesPerSecond { get; private set; }

        /// <summary>Counts one frame that took <paramref name="dt"/> seconds. Non-positive durations are ignored.</summary>
        public void Add(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            elapsed += dt;
            frames++;
            if (elapsed < window)
            {
                return;
            }

            FramesPerSecond = frames / elapsed;
            elapsed = 0f;
            frames = 0;
        }
    }
}
