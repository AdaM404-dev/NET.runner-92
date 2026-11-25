using System;

namespace NetRunner.Core
{
    /// <summary>
    /// How a hospital door behaves: what it says, when E moves it, how fast
    /// its leaves swing or slide (progress 0 shut to 1 open), and when the
    /// player stands in its doorway, where it never closes. Positions are in
    /// the door's own frame: x across the opening, y up from its base, z
    /// through it. Plain maths with no scene objects (see DoorRulesTests).
    /// </summary>
    public static class DoorRules
    {
        /// <summary>Progress per second of a swing door: fully open in 1/1.9 ≈ 0.53 s.</summary>
        public const float SwingSpeed = 1.9f;

        /// <summary>Progress per second of a sliding door or the barrier: fully open in 1/1.55 ≈ 0.65 s.</summary>
        public const float SlideSpeed = 1.55f;

        // The doorway the player must keep clear: within half a metre of the
        // door's base in height, 0.28 m beyond the opening on either side, and
        // as deep as a swinging leaf reaches (1.32 m) or a sliding pair needs (0.43 m).
        private const float DoorwayHeight = .5f;
        private const float DoorwaySide = .28f;
        private const float SwingDepth = 1.32f;
        private const float SlideDepth = .43f;

        /// <summary>The prompt shown while the player aims at the door.</summary>
        /// <param name="offline">An out-of-service lift door.</param>
        /// <param name="liftOnly">A lift door, opened only by the lift.</param>
        /// <param name="locked">Locked: E does nothing.</param>
        /// <param name="requested">Opening or open, so E would close it.</param>
        public static string Prompt(bool offline, bool liftOnly, bool locked, bool requested)
        {
            if (offline)
            {
                return "Lift offline";
            }

            if (liftOnly)
            {
                return "Use the lift call button";
            }

            if (locked)
            {
                return "Locked";
            }

            return requested ? "E  Close door" : "E  Open door";
        }

        /// <summary>
        /// Whether E opens or closes the door: never a lift door or a locked
        /// one, and an open door does not close on a player in its doorway.
        /// </summary>
        public static bool AnswersUse(bool liftOnly, bool locked, bool requested, bool inDoorway)
        {
            return !(liftOnly || locked || (requested && inDoorway));
        }

        /// <summary>The door's progress after <paramref name="dt"/> seconds of moving towards open or shut.</summary>
        public static float Advance(float progress, bool requested, bool swing, float dt)
        {
            return Scalar.MoveTowards(progress, requested ? 1f : 0f, dt * (swing ? SwingSpeed : SlideSpeed));
        }

        /// <summary>
        /// Whether a player standing at (x, y, z) in the door's frame is in its
        /// doorway, for an opening <paramref name="width"/> metres wide.
        /// </summary>
        public static bool InDoorway(float x, float y, float z, float width, bool swing)
        {
            return Math.Abs(y) < DoorwayHeight && Math.Abs(x) < width * .5f + DoorwaySide
                && Math.Abs(z) < (swing ? SwingDepth : SlideDepth);
        }
    }
}
