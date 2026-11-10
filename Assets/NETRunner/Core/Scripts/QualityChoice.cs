namespace NetRunner.Core
{
    /// <summary>
    /// Rules for picking a graphics quality level: which level comes next when
    /// the player cycles, and which level a start uses. Levels are indexes
    /// 0 .. count-1, as in Unity's quality settings (see QualityChoiceTests).
    /// </summary>
    public static class QualityChoice
    {
        /// <summary>
        /// The level after <paramref name="current"/>; after the last level the
        /// cycle starts again at 0. An out-of-range current level counts as 0.
        /// </summary>
        public static int Next(int current, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            if (current < 0 || current >= count)
            {
                return 0;
            }

            return (current + 1) % count;
        }

        /// <summary>
        /// The level to start with: the saved one when it is a valid level,
        /// otherwise <paramref name="fallback"/> (the platform's default).
        /// </summary>
        /// <param name="saved">The remembered level, or a negative number when there is none.</param>
        public static int StartLevel(int saved, int count, int fallback)
        {
            if (saved >= 0 && saved < count)
            {
                return saved;
            }

            return fallback;
        }
    }
}
