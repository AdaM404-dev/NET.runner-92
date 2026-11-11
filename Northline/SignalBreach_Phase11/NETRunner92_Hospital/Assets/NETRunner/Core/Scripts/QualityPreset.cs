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
        /// <summary>
        /// The menu settings a preset sets (Phase 11's presets): texture
        /// reduction (0 full, 1 half, 2 quarter), shadow distance step (0 off
        /// to 3), shadow resolution step (0 to 3, 2 = as the scene sets it),
        /// antialiasing (0 off, 1 FXAA, 2 SMAA, 3 TAA), the effects that are
        /// on, and the render scale in percent. Motion blur and depth of field
        /// are off in every preset.
        /// </summary>
        public readonly struct Settings
        {
            public Settings(int texture, int shadows, int shadowResolution, int antialias,
                bool ambientOcclusion, bool volumetrics, bool bloom, int renderScale)
            {
                Texture = texture;
                Shadows = shadows;
                ShadowResolution = shadowResolution;
                Antialias = antialias;
                AmbientOcclusion = ambientOcclusion;
                Volumetrics = volumetrics;
                Bloom = bloom;
                RenderScale = renderScale;
            }

            public int Texture { get; }

            public int Shadows { get; }

            public int ShadowResolution { get; }

            public int Antialias { get; }

            public bool AmbientOcclusion { get; }

            public bool Volumetrics { get; }

            public bool Bloom { get; }

            public int RenderScale { get; }
        }

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

        /// <summary>The settings a preset sets; Custom sets none, so it is not accepted here.</summary>
        public static Settings For(int preset)
        {
            if (preset < Low || preset > Ultra)
            {
                throw new ArgumentOutOfRangeException(nameof(preset), "Custom has no settings of its own.");
            }

            int texture = preset == Low ? 2 : preset == Medium ? 1 : 0;
            int antialias = preset == Low ? 1 : preset == Medium ? 2 : 3;
            return new Settings(texture, preset, preset, antialias, preset >= High, preset >= Medium, preset >= Medium,
                DefaultRenderScales[preset]);
        }

        /// <summary>The preset that selects a quality level, used when F2 cycles the levels.</summary>
        public static int ForLevel(int level)
        {
            return Math.Clamp(level, 0, HighestLevel);
        }
    }
}
