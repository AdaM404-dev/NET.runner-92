using System.Linq;

namespace NetRunner.Core
{
    /// <summary>
    /// When the studio intro gives way to the main menu (#66): when the video
    /// has finished, when the player presses Space, when playback fails, or
    /// when no frame has appeared after <see cref="StartTimeout"/> seconds, so
    /// the game never waits on a black screen. Test and benchmark runs skip
    /// it, because they expect the menu or the hospital right away (see
    /// IntroRulesTests).
    /// </summary>
    public static class IntroRules
    {
        /// <summary>The launch option that skips the intro.</summary>
        public const string SkipOption = "-net92-skip-intro";

        /// <summary>Seconds to wait for the first frame before going on without the video.</summary>
        public const float StartTimeout = 5f;

        // The built game's checks and benchmarks; each starts with one of these.
        private static readonly string[] TestRuns =
        {
            "-net92-benchmark", "-net92-route-benchmark", "-net92-menu-check", "-net92-menu-reload-check", "-net92-input-check",
        };

        /// <summary>Whether the game was started with the skip option or for a check or benchmark run.</summary>
        public static bool IsSkippedAtLaunch(string[] arguments)
        {
            return LaunchArguments.Has(arguments, SkipOption) || TestRuns.Any(option => LaunchArguments.Has(arguments, option));
        }

        /// <summary>
        /// Whether the intro is over: the video ended, Space was pressed,
        /// playback failed, or <paramref name="elapsed"/> seconds passed
        /// without a first frame.
        /// </summary>
        public static bool IsOver(bool hasEnded, bool isSkipPressed, bool hasFailed, bool hasStarted, float elapsed)
        {
            return hasEnded || isSkipPressed || hasFailed || (!hasStarted && elapsed >= StartTimeout);
        }
    }
}
