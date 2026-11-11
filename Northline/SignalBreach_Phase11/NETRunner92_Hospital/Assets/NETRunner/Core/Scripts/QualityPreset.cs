using System;

namespace NetRunner.Core
{
    /// <summary>
    /// How the settings menu's quality presets map onto the project's quality
    /// levels (Low, Medium, High) and their default render scale (#55,
    /// Samuel's decision of 2026-11-12). The presets are the menu's: Low,
    /// Medium, High, Ultra and Custom. Ultra uses the High level with more
    /// shadows; Custom keeps whichever level the player last chose. The
    /// default render scales are #34's 50, 67 and 100 %, which the #36
    /// benchmarks measured; the player can still move the slider afterwards.
    /// </summary>
    public static class QualityPreset
    {
        public const int Low = 0;
        public const int Medium = 1;
        public const int High = 2;
        public const int Ultra = 3;
        public const int Custom = 4;

        /// <summary>The highest quality level index: the project has Low, Medium and High.</summary>
        public const int HighestLevel = 2;

        /// <summary>The render scale range the slider allows, in percent.</summary>
        public const int MinimumRenderScale = 50;
        public const int MaximumRenderScale = 100;

        private static readonly int[] DefaultRenderScales = { 50, 67, 100, 100 };

        /// <summary>
        /// The quality level a preset selects; Custom (or anything unknown)
        /// keeps <paramref name="currentLevel"/>, limited to the levels that exist.
        /// </summary>
        public static int LevelFor(int preset, int currentLevel)
        {
            if (preset >= Low && preset <= Ultra)
            {
                return Math.Min(preset, HighestLevel);
            }

            return Math.Clamp(currentLevel, 0, HighestLevel);
        }

        /// <summary>The render scale a preset starts with, in percent; Custom has none and returns -1.</summary>
        public static int RenderScaleFor(int preset)
        {
            return preset >= Low && preset <= Ultra ? DefaultRenderScales[preset] : -1;
        }

        /// <summary>The preset that selects a quality level, used when F2 cycles the levels.</summary>
        public static int ForLevel(int level)
        {
            return Math.Clamp(level, 0, HighestLevel);
        }
    }
}
