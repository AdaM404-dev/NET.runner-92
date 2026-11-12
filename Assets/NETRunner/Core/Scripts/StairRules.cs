using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The hospital's first ascent: closures force the west stair to L2, the
    /// east stair to L3 and the west stair again to L4, and standing on the
    /// new floor at the exit of the stair that leads there unlocks it in the
    /// lift. Floors are numbered from 0 (L1) to <see cref="TopFloor"/>;
    /// positions are the scene's, in metres. Plain maths (see StairRulesTests).
    /// </summary>
    public static class StairRules
    {
        /// <summary>Metres from one floor to the next.</summary>
        public const float FloorHeight = 4.2f;

        /// <summary>The highest floor, L4.</summary>
        public const int TopFloor = 3;

        // The stair cores stand 32 m either side of the centre line; the exit
        // into the concourse is 1.05 m beyond a core's centre, and counts within
        // 2 m along x, from z −1.95 to 2.15 m, and within 0.12 m of the floor's height.
        private const int CoreDistance = 32;
        private const float ExitOffset = 1.05f;
        private const float ExitReach = 2f;
        private const float ExitBack = -1.95f;
        private const float ExitFront = 2.15f;
        private const float LevelTolerance = .12f;

        /// <summary>The x of the stair core that climbs from <paramref name="lowerFloor"/>: east from L2, west otherwise.</summary>
        public static int CoreForLeg(int lowerFloor)
        {
            return lowerFloor == 1 ? CoreDistance : -CoreDistance;
        }

        /// <summary>
        /// The highest floor reached after the player stood at (x, y, z) this
        /// frame: one more when it is the next floor, at the exit of the stair
        /// that leads there, and the player stands on it.
        /// </summary>
        public static int Reached(int highest, bool grounded, float x, float y, float z)
        {
            if (highest >= TopFloor || !grounded)
            {
                return highest;
            }

            bool level = Math.Abs(y - (highest + 1) * FloorHeight) < LevelTolerance;
            bool atExit = Math.Abs(x - (CoreForLeg(highest) + ExitOffset)) < ExitReach && z > ExitBack && z < ExitFront;
            return level && atExit ? highest + 1 : highest;
        }
    }
}
