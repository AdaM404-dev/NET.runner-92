using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The limits and defaults of the settings menu (Phase 11), used to repair
    /// a preferences file that holds values the menu cannot produce: a value
    /// out of range is clamped, one that is not a number gets its default.
    /// Plain maths; the Settings feature applies the values to the engine.
    /// </summary>
    public static class PreferenceRules
    {
        /// <summary>No frame cap.</summary>
        public const int Unlimited = -1;

        public const int DefaultFrameLimit = 60;

        public const int MinimumWidth = 640;
        public const int MinimumHeight = 360;
        public const int MaximumWidth = 16384;
        public const int MaximumHeight = 8640;

        /// <summary>The highest monitor index the menu accepts.</summary>
        public const int HighestMonitor = 31;

        public const float MinimumFieldOfView = 55f;
        public const float MaximumFieldOfView = 100f;
        public const float DefaultFieldOfView = 68f;

        /// <summary>Brightness in EV, either way.</summary>
        public const float BrightnessRange = 1.5f;

        public const float MinimumSensitivity = .25f;
        public const float MaximumSensitivity = 3f;
        public const float MaximumSmoothing = .85f;

        public const float MinimumUiScale = .8f;
        public const float MaximumUiScale = 1.25f;
        public const float MinimumHudOpacity = .25f;

        public const float DefaultMusic = .65f;
        public const float DefaultUi = .8f;

        /// <summary>The volume of silence on the audio mixer, in decibels.</summary>
        public const float SilentDecibels = -80f;

        private const float SilentLinear = .0001f;
        private const float DecibelsPerDecade = 20f;

        private static readonly int[] FrameLimitChoices = { Unlimited, 30, 60, 90, 120, 144, 165, 240 };

        /// <summary>The frame limits the menu offers, in its order.</summary>
        public static int[] FrameLimits => (int[])FrameLimitChoices.Clone();

        /// <summary>The value clamped to its range; <paramref name="fallback"/> when it is not a number.</summary>
        public static float Bounded(float value, float minimum, float maximum, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Clamp(value, minimum, maximum);
        }

        /// <summary>The frame limit if the menu offers it, otherwise the default.</summary>
        public static int FrameLimit(int value)
        {
            return Array.IndexOf(FrameLimitChoices, value) >= 0 ? value : DefaultFrameLimit;
        }

        /// <summary>Whether a saved window size is usable; 0 × 0 means the desktop's size and is handled by the caller.</summary>
        public static bool IsWindowSize(int width, int height)
        {
            return width >= MinimumWidth && height >= MinimumHeight && width <= MaximumWidth && height <= MaximumHeight;
        }

        /// <summary>A 0-to-1 volume on the mixer's decibel scale; very quiet is silence.</summary>
        public static float ToDecibels(float linear)
        {
            return linear <= SilentLinear ? SilentDecibels : DecibelsPerDecade * MathF.Log10(linear);
        }
    }
}
