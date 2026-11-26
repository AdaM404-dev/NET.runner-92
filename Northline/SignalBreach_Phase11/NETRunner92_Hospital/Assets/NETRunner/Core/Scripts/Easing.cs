namespace NetRunner.Core
{
    /// <summary>
    /// Curves for an even 0-to-1 progress: a straight line between two values
    /// (<see cref="Lerp"/>) and an S-curve with a slow start and a slow end
    /// (<see cref="SmoothStep01"/>). Plain maths with no scene objects, so it
    /// is tested without starting the game (see EasingTests).
    /// </summary>
    public static class Easing
    {
        /// <summary>
        /// Slow start, slow end: 0 stays 0, 0.5 stays 0.5 and 1 stays 1, with an
        /// S-shaped curve in between. Values below 0 count as 0, above 1 as 1.
        /// </summary>
        /// <param name="t">Progress from 0 (start) to 1 (end).</param>
        public static float SmoothStep01(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// The straight line from <paramref name="from"/> at 0 to
        /// <paramref name="to"/> at 1, like Unity's Mathf.Lerp. Values below 0
        /// count as 0, above 1 as 1.
        /// </summary>
        /// <param name="t">Progress from 0 (start) to 1 (end).</param>
        public static float Lerp(float from, float to, float t)
        {
            if (t < 0f)
            {
                t = 0f;
            }
            else if (t > 1f)
            {
                t = 1f;
            }

            return from + (to - from) * t;
        }
    }
}
