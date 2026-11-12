using NetRunner.Core;
using NetRunner.Settings;
using UnityEngine;

namespace NetRunner.Atmosphere
{
    /// <summary>
    /// Makes one of the hospital's lights pulse: an emergency light swells and
    /// fades smoothly, an intermittent one flickers. Its intensity swings
    /// between the intensity it started with and that × (1 − depth). The
    /// levels come from <see cref="LightPulse"/>; the Perlin noise behind the
    /// flicker is sampled here, because only the engine has it. The hospital's
    /// scene builder adds it to some fixtures and sets it up through
    /// <see cref="Configure"/>.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class PracticalPulse : MonoBehaviour
    {
        /// <summary>How the light pulses. Scenes save the number, so a new pattern goes at the end.</summary>
        public enum Pattern
        {
            /// <summary>Flickers irregularly, following Perlin noise.</summary>
            Intermittent,

            /// <summary>Swells and fades smoothly, like a warning beacon.</summary>
            Emergency,
        }

        [SerializeField] private Pattern pattern;
        [SerializeField, Range(0, 1)] private float depth = 0.4f;
        [SerializeField, Min(0.1f)] private float frequency = 0.8f;
        [SerializeField] private float phase;

        // Optional: without it the light pulses as if reduced flashing were off. Set by the hospital's installer.
        [SerializeField] private Preferences preferences;

        private Light fixture;
        private float full;

        private void Awake()
        {
            fixture = GetComponent<Light>();
            full = fixture.intensity;
        }

        private void Update()
        {
            // Reduced flashing (accessibility setting) holds the light at its full intensity.
            if (preferences && preferences.Active.ReduceFlashing)
            {
                fixture.intensity = full;
                return;
            }

            float cycle = LightPulse.Cycle(Time.time, frequency, phase);

            // The phase also picks the noise row, so neighbouring lights flicker differently.
            float level = pattern == Pattern.Emergency
                ? LightPulse.Emergency(cycle)
                : LightPulse.Intermittent(Mathf.PerlinNoise(cycle, phase));
            fixture.intensity = LightPulse.Intensity(full, depth, level);
        }

        /// <summary>Gives it the player's settings; called by the hospital's installer.</summary>
        public void SetPreferences(Preferences settings)
        {
            preferences = settings;
        }

        /// <summary>
        /// Sets the pulse up from the scene builder: the pattern, how far the
        /// light dims (0 to 1), how many cycles a second, and where in its
        /// cycle it starts.
        /// </summary>
        public void Configure(Pattern mode, float amount, float speed, float offset)
        {
            pattern = mode;
            depth = amount;
            frequency = speed;
            phase = offset;
        }
    }
}
