using System;

namespace NetRunner.Core
{
    /// <summary>Chooses hardware sync when it can honor the frame limit on a desktop display.</summary>
    public static class FramePacing
    {
        /// <summary>The menu backdrop animates at 20 Hz, so it can run below the gameplay limit.</summary>
        public const int MenuLimit = 30;

        private const int MaximumSyncInterval = 4;
        private const double RefreshTolerance = 0.25;

        /// <summary>
        /// Returns the number of vertical blanks per frame, or zero to use the software limit.
        /// A fixed limit uses hardware sync only when the display can match it closely or is slower.
        /// </summary>
        public static int SyncInterval(bool useVsync, int frameLimit, double refreshRate)
        {
            if (!useVsync)
            {
                return 0;
            }

            if (frameLimit == PreferenceRules.Unlimited)
            {
                return 1;
            }

            if (!(refreshRate > 0) || double.IsInfinity(refreshRate))
            {
                return 0;
            }

            if (refreshRate <= frameLimit + RefreshTolerance)
            {
                return 1;
            }

            for (int interval = 2; interval <= MaximumSyncInterval; interval++)
            {
                if (Math.Abs(refreshRate / interval - frameLimit) <= RefreshTolerance)
                {
                    return interval;
                }
            }

            return 0;
        }
    }
}
