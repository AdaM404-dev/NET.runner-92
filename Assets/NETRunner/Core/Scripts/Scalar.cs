using System;

namespace NetRunner.Core
{
    /// <summary>
    /// Unity's <c>Mathf.MoveTowards</c> and <c>Mathf.Repeat</c> for rules in
    /// <c>Core</c>, which cannot call <c>Mathf</c>: the same arithmetic in the
    /// same order, so a rule moved out of a component keeps its numbers
    /// (see ScalarTests).
    /// </summary>
    public static class Scalar
    {
        /// <summary>
        /// <paramref name="current"/> moved towards <paramref name="target"/> by at
        /// most <paramref name="maxDelta"/>, landing exactly on it when in reach.
        /// </summary>
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            float difference = target - current;
            if (Math.Abs(difference) <= maxDelta)
            {
                return target;
            }

            return current + (difference >= 0f ? 1f : -1f) * maxDelta;
        }

        /// <summary><paramref name="t"/> wrapped into 0 to <paramref name="length"/>, like the hand of a clock.</summary>
        public static float Repeat(float t, float length)
        {
            float turns = t / length;
            float value = t - (float)Math.Floor(turns) * length;
            if (value < 0f)
            {
                return 0f;
            }

            return value > length ? length : value;
        }
    }
}
