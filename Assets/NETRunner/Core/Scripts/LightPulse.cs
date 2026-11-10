using System;

namespace NetRunner.Core
{
    /// <summary>
    /// How bright a pulsing hospital light is. An emergency light swells and
    /// fades smoothly, once per cycle; an intermittent one flickers, dimming
    /// while its noise is low. Both swing between their full intensity and
    /// full × (1 − depth). The intermittent light's noise is Perlin noise,
    /// which only the engine has, so the component samples it and
    /// <see cref="Intermittent"/> shapes the sample. Plain maths with no scene
    /// objects (see LightPulseTests).
    /// </summary>
    public static class LightPulse
    {
        // An intermittent light is fully dimmed while its noise is below 0.27,
        // and comes up to full over the next 1 / 3.5 ≈ 0.29 of noise.
        private const float NoiseFloor = 0.27f;
        private const float NoiseGain = 3.5f;

        /// <summary>
        /// How many cycles a light has run: <paramref name="time"/> seconds ×
        /// <paramref name="frequency"/> cycles a second, plus its
        /// <paramref name="phase"/> in cycles, so lights set up with different
        /// phases do not pulse in step.
        /// </summary>
        public static float Cycle(float time, float frequency, float phase) => time * frequency + phase;

        /// <summary>
        /// An emergency light's level, 0 (dimmed) to 1 (full): halfway at the
        /// start of each cycle, full a quarter in, dimmed three quarters in.
        /// </summary>
        public static float Emergency(float cycle)
        {
            // Like Unity's Mathf.Sin: the angle rounded to a float, the sine worked out in double.
            // The light then matches the Northline code exactly.
            float angle = cycle * MathF.PI * 2f;
            return 0.5f + 0.5f * (float)Math.Sin(angle);
        }

        /// <summary>An intermittent light's level, 0 (dimmed) to 1 (full), from a noise sample between 0 and 1.</summary>
        public static float Intermittent(float noise) => Easing.SmoothStep01((noise - NoiseFloor) * NoiseGain);

        /// <summary>The light's intensity at a level: <paramref name="full"/> at 1, full × (1 − depth) at 0.</summary>
        /// <param name="full">The light's own intensity, when it is not dimmed at all.</param>
        /// <param name="depth">How far it dims, 0 (not at all) to 1 (fully off).</param>
        /// <param name="level">The level from <see cref="Emergency"/> or <see cref="Intermittent"/>.</param>
        public static float Intensity(float full, float depth, float level) => full * Easing.Lerp(1f - depth, 1f, level);
    }
}
