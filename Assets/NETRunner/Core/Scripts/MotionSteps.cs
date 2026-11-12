using System;

namespace NetRunner.Core
{
    /// <summary>
    /// How one frame of player movement is split up: into steps of at most
    /// <see cref="MaxStep"/> seconds, so the capsule keeps its contact with
    /// stair edges, and <see cref="MaxFrame"/> seconds in all, so after a long
    /// hitch the player is not thrown through a wall to catch up. A caller
    /// takes <see cref="Budget"/> of the frame and asks <see cref="Next"/> for
    /// steps until it returns 0. Plain maths (see MotionStepsTests).
    /// </summary>
    public static class MotionSteps
    {
        /// <summary>The longest step, in seconds.</summary>
        public const float MaxStep = 1f / 90f;

        /// <summary>The most of one frame that is moved, in seconds.</summary>
        public const float MaxFrame = .10f;

        /// <summary>Seconds left over that are too few for a step of their own and are dropped.</summary>
        public const float Leftover = .00001f;

        /// <summary>The part of a frame of <paramref name="dt"/> seconds that is moved.</summary>
        public static float Budget(float dt)
        {
            return Math.Min(dt, MaxFrame);
        }

        /// <summary>The length of the next step out of <paramref name="remaining"/> seconds, or 0 when none is left.</summary>
        public static float Next(float remaining)
        {
            return remaining > Leftover ? Math.Min(remaining, MaxStep) : 0f;
        }
    }
}
