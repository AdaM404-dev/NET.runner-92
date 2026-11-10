using System;

namespace NetRunner.Core
{
    /// <summary>
    /// A random number between two values from a given random stream. The
    /// weather rules (<see cref="RainField"/>, <see cref="AmbientEvents"/>)
    /// draw all their randomness through it.
    /// </summary>
    internal static class RandomRange
    {
        /// <summary>A number from <paramref name="low"/> up to, but not including, <paramref name="high"/>.</summary>
        public static float Between(Random random, float low, float high)
        {
            return low + (high - low) * (float)random.NextDouble();
        }
    }
}
