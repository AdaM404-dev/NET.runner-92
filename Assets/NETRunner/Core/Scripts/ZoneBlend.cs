using System;

namespace NetRunner.Core
{
    /// <summary>
    /// How much a point counts as inside an atmosphere zone: 1 inside the
    /// zone's box, falling along an S-curve to 0 at the blend distance outside
    /// it. Outside, what counts is the straight distance to the nearest point
    /// of the box, so the weight fades evenly round its edges and corners.
    /// Points are in metres in the zone's own frame, relative to the box's
    /// centre. Plain maths with no scene objects (see ZoneBlendTests).
    /// </summary>
    public static class ZoneBlend
    {
        /// <summary>The weight, from 0 (beyond the blend distance) to 1 (inside the box).</summary>
        /// <param name="point">The point in the zone's own frame, relative to the box's centre.</param>
        /// <param name="size">The box's full width, height and depth.</param>
        /// <param name="blendDistance">How far outside the box the weight reaches 0; more than 0.</param>
        public static float Weight((float X, float Y, float Z) point, (float X, float Y, float Z) size, float blendDistance)
        {
            float x = Beyond(point.X, size.X);
            float y = Beyond(point.Y, size.Y);
            float z = Beyond(point.Z, size.Z);
            float distance = MathF.Sqrt(x * x + y * y + z * z);
            return 1f - Easing.SmoothStep01(distance / blendDistance);
        }

        // How far a coordinate lies beyond the box along one axis; 0 within it.
        private static float Beyond(float coordinate, float size)
        {
            return MathF.Max(0f, MathF.Abs(coordinate) - size * 0.5f);
        }
    }
}
