using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The player's up-and-down speed for one movement step: gravity in the
    /// air, falls capped at <see cref="FallLimit"/>, and a steady push down
    /// while standing ("ground adhesion") so the capsule stays on stairs and
    /// ramps. The hospital player has no jump (#43 replaced the warehouse
    /// player's jump maths that lived here). Plain maths with no scene
    /// objects, so it is tested without starting the game (see
    /// VerticalMotionTests).
    /// </summary>
    public static class VerticalMotion
    {
        /// <summary>The fastest fall, in metres per second.</summary>
        public const float FallLimit = 34f;

        /// <summary>
        /// Returns the vertical speed for a step that lasts <paramref name="dt"/>
        /// seconds; positive is up.
        /// </summary>
        /// <param name="verticalSpeed">Speed before the step.</param>
        /// <param name="grounded">Whether the capsule touched the floor in the last step.</param>
        /// <param name="gravity">Downward acceleration in metres per second, per second.</param>
        /// <param name="groundAdhesion">Downward speed while standing, in metres per second.</param>
        /// <param name="dt">Step length in seconds.</param>
        public static float Step(float verticalSpeed, bool grounded, float gravity, float groundAdhesion, float dt)
        {
            if (grounded)
            {
                return -groundAdhesion;
            }

            return Math.Max(-FallLimit, verticalSpeed - gravity * dt);
        }

        /// <summary>The speed after a step that bumped into the ceiling: a rise stops, a fall goes on.</summary>
        public static float AfterHittingCeiling(float verticalSpeed)
        {
            return verticalSpeed > 0f ? 0f : verticalSpeed;
        }
    }
}
