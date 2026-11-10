using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The rain around the listener, worked out drop by drop. Each frame some
    /// drops fall due, and each lands on a random column within
    /// <see cref="Reach"/> of the listener. The component probes the column for
    /// its highest surface (a roof, or the ground); from that height this class
    /// works out where the drop's streak starts and how long it falls: from
    /// above both the listener and the roof down to just above the roof, at
    /// <see cref="FallSpeed"/>. Under a roof high overhead, streaks that would
    /// start far above the listener are left out. Every seventh drop on open
    /// ground also splashes, and deep inside the hospital no rain is worked
    /// out at all. Heights are in metres, times in seconds. All randomness
    /// comes from the <see cref="Random"/> it is given, so a seeded one
    /// repeats exactly (see RainFieldTests).
    /// </summary>
    public sealed class RainField
    {
        /// <summary>How fast a streak falls, in metres a second.</summary>
        public const float FallSpeed = 16f;

        /// <summary>Drops land within this many metres of the listener, along x and along z.</summary>
        public const float Reach = 14f;

        // A long frame brings at most this many drops, so a stall is not followed by a burst.
        private const float MostDropsInOneFrame = 90f;

        // No rain is worked out above this interior blend inside the hospital's central block
        // (29 m east and west of the centre, 9.5 m north and south): there it cannot be seen.
        private const float ShelterBlend = 0.92f;
        private const float ShelterHalfWidth = 29f;
        private const float ShelterHalfDepth = 9.5f;

        // A streak starts at the higher of these heights above the listener and above the roof.
        private const float AboveListenerLow = 2.7f;
        private const float AboveListenerHigh = 9.5f;
        private const float AboveRoofLow = 1.6f;
        private const float AboveRoofHigh = 5.6f;

        // It ends this far above the roof, so it does not poke through.
        private const float RoofClearance = 0.18f;

        // A roof more than this above the listener is overhead, and under it a streak
        // starting more than OutOfSight above the listener is left out.
        private const float Overhead = 0.6f;
        private const float OutOfSight = 12f;

        // A streak with something in its way ends this far short of it, but lasts at least ShortestFall.
        private const float ImpactGap = 0.07f;
        private const float ShortestFall = 0.02f;

        private const float DropSizeLow = 0.013f;
        private const float DropSizeHigh = 0.026f;

        // Every seventh drop splashes, where it lands on open ground and the listener is not deep inside.
        private const int SplashEvery = 7;
        private const float SplashGroundHeight = 0.35f;
        private const float SplashBlend = 0.90f;
        private const float SplashSideways = 0.3f;
        private const float SplashUpLow = 0.35f;
        private const float SplashUpHigh = 0.75f;
        private const float SplashSizeLow = 0.028f;
        private const float SplashSizeHigh = 0.067f;
        private const float SplashLifeLow = 0.14f;
        private const float SplashLifeHigh = 0.22f;

        private readonly Random random;
        private float due;

        /// <summary>Starts with no drops due.</summary>
        /// <param name="random">Where the drops' randomness comes from.</param>
        public RainField(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            this.random = random;
        }

        /// <summary>
        /// Whether the listener is so deep inside the hospital that no rain
        /// needs working out: an interior blend above 0.92, within 29 m east
        /// or west and 9.5 m north or south of the centre. Along the facades
        /// and at the entrances it still rains.
        /// </summary>
        public static bool IsSheltered(float blend, float x, float z)
        {
            return blend > ShelterBlend && MathF.Abs(x) < ShelterHalfWidth && MathF.Abs(z) < ShelterHalfDepth;
        }

        /// <summary>
        /// The fall time of a streak once a probe along its path has found
        /// something in the way <paramref name="impactDistance"/> metres from
        /// its start: it ends 7 cm short of it, lasts at least 0.02 s, and
        /// never lasts longer than it would have.
        /// </summary>
        /// <param name="speed">The streak's speed along its path, in metres a second, wind included.</param>
        public static float FallTimeToImpact(float fallTime, float impactDistance, float speed)
        {
            return MathF.Min(fallTime, MathF.Max(ShortestFall, (impactDistance - ImpactGap) / speed));
        }

        /// <summary>
        /// Whether drop number <paramref name="index"/> of a frame also
        /// splashes: every seventh, counting from the first, on ground lower
        /// than 0.35 m, while the listener's interior blend is below 0.9.
        /// </summary>
        public static bool Splashes(int index, float groundHeight, float blend)
        {
            return index % SplashEvery == 0 && groundHeight < SplashGroundHeight && blend < SplashBlend;
        }

        /// <summary>
        /// How many drops fall in a frame of <paramref name="dt"/> seconds. A
        /// fraction of a drop carries over to the next frame; one frame brings
        /// at most 90.
        /// </summary>
        public int DropsDue(float dt, float dropsPerSecond)
        {
            due = MathF.Min(MostDropsInOneFrame, due + dt * dropsPerSecond);
            int count = (int)due;
            due -= count;
            return count;
        }

        /// <summary>A random column for the next drop, within <see cref="Reach"/> of the listener along x and z.</summary>
        public (float X, float Z) Column(float listenerX, float listenerZ)
        {
            float x = listenerX + RandomRange.Between(random, -Reach, Reach);
            float z = listenerZ + RandomRange.Between(random, -Reach, Reach);
            return (x, z);
        }

        /// <summary>
        /// Where a drop's streak starts and how long it falls, from the heights
        /// of the listener and of the highest surface in the drop's column. It
        /// starts at the higher of 2.7–9.5 m above the listener and 1.6–5.6 m
        /// above the roof, and falls at <see cref="FallSpeed"/> to 18 cm above
        /// the roof. Returns false when the roof is overhead (more than 0.6 m
        /// above the listener) and the streak would start more than 12 m above
        /// the listener; such a streak is left out.
        /// </summary>
        public bool TryStreak(float listenerHeight, float roofHeight, out float top, out float fallTime)
        {
            float aboveListener = listenerHeight + RandomRange.Between(random, AboveListenerLow, AboveListenerHigh);
            float aboveRoof = roofHeight + RandomRange.Between(random, AboveRoofLow, AboveRoofHigh);
            top = MathF.Max(aboveListener, aboveRoof);
            if (roofHeight > listenerHeight + Overhead && top > listenerHeight + OutOfSight)
            {
                fallTime = 0f;
                return false;
            }

            fallTime = (top - (roofHeight + RoofClearance)) / FallSpeed;
            return true;
        }

        /// <summary>The thickness of the next drop's streak, in metres.</summary>
        public float DropSize() => RandomRange.Between(random, DropSizeLow, DropSizeHigh);

        /// <summary>The next splash's velocity in metres a second: up 0.35–0.75, and at most 0.3 sideways along x and z.</summary>
        public (float X, float Y, float Z) SplashVelocity()
        {
            float x = RandomRange.Between(random, -SplashSideways, SplashSideways);
            float y = RandomRange.Between(random, SplashUpLow, SplashUpHigh);
            float z = RandomRange.Between(random, -SplashSideways, SplashSideways);
            return (x, y, z);
        }

        /// <summary>The size of the next splash, in metres.</summary>
        public float SplashSize() => RandomRange.Between(random, SplashSizeLow, SplashSizeHigh);

        /// <summary>How long the next splash lasts, in seconds.</summary>
        public float SplashLife() => RandomRange.Between(random, SplashLifeLow, SplashLifeHigh);
    }
}
