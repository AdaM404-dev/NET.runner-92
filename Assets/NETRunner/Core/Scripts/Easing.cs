namespace NetRunner.Core
{
    /// <summary>
    /// Curves that turn an even 0-to-1 progress into a smoother movement.
    /// Plain maths with no scene objects, so it is tested without starting
    /// the game (see EasingTests).
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
    }
}
