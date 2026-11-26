using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The hospital's timed atmosphere events: lightning flashes, each
    /// followed by a thunderclap a moment later; a distant medical monitor
    /// that beeps now and then, heard only inside; and the creak of the fence
    /// and structure. It is stepped once a frame with the current time, in
    /// seconds, and says what happens in that frame. All randomness comes from
    /// the <see cref="Random"/> it is given, so a seeded one repeats exactly
    /// (see AmbientEventsTests).
    /// </summary>
    public sealed class AmbientEvents
    {
        /// <summary>How long one lightning flash lasts, in seconds.</summary>
        public const float FlashLength = 0.7f;

        /// <summary>How far in front of the listener the monitor beeps, in metres.</summary>
        public const float MonitorAhead = 3f;

        // A flash is a bright strike that fades fast, and a weaker flicker 0.19 s after it.
        private const float StrikeIntensity = 34f;
        private const float StrikeFade = 22f;
        private const float FlickerIntensity = 17f;
        private const float FlickerSharpness = 65f;
        private const float FlickerTime = 0.19f;

        // Seconds from the end of one flash to the next, and from a strike to its thunder.
        private const float FlashGapLow = 20f;
        private const float FlashGapHigh = 39f;
        private const float ThunderDelayLow = 0.65f;
        private const float ThunderDelayHigh = 1.5f;

        // The monitor beeps first 4.5 s after the start, then every 7 to 16 s, 3 to 6.5 m to the
        // listener's right; it is heard only with an interior blend above 0.45.
        private const float FirstMonitor = 4.5f;
        private const float MonitorGapLow = 7f;
        private const float MonitorGapHigh = 16f;
        private const float MonitorAudibleBlend = 0.45f;
        private const float MonitorSideLow = 3f;
        private const float MonitorSideHigh = 6.5f;

        // The structure creaks first 8 s after the start, then every 10 to 23 s.
        private const float FirstCreak = 8f;
        private const float CreakGapLow = 10f;
        private const float CreakGapHigh = 23f;

        private readonly Random random;
        private float nextFlash;
        private float nextThunder = float.PositiveInfinity;
        private float nextMonitor;
        private float nextCreak;
        private bool thunderScheduled;

        /// <summary>Starts the clock at <paramref name="now"/>.</summary>
        /// <param name="random">Where the events' randomness comes from.</param>
        /// <param name="now">The time now, in seconds.</param>
        /// <param name="firstFlashDelay">Seconds from now to the first lightning flash.</param>
        public AmbientEvents(Random random, float now, float firstFlashDelay)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            this.random = random;
            nextFlash = now + firstFlashDelay;
            nextMonitor = now + FirstMonitor;
            nextCreak = now + FirstCreak;
        }

        /// <summary>The lightning light's intensity in the last step; 0 between flashes.</summary>
        public float Lightning { get; private set; }

        /// <summary>Whether a thunderclap started in the last step.</summary>
        public bool ThunderStarts { get; private set; }

        /// <summary>Whether the distant monitor beeped in the last step.</summary>
        public bool MonitorBeeps { get; private set; }

        /// <summary>
        /// How far to the listener's right the last beep sounded, in metres
        /// (3 to 6.5); it sounds <see cref="MonitorAhead"/> in front.
        /// </summary>
        public float MonitorSide { get; private set; }

        /// <summary>Whether the fence and structure creaked in the last step.</summary>
        public bool CreakSounds { get; private set; }

        /// <summary>
        /// The lightning's intensity <paramref name="sinceStrike"/> seconds
        /// into a flash: 34 at the strike, fading fast, with a second flicker
        /// of 17 at 0.19 s; 0 before the strike and once the flash is over.
        /// </summary>
        public static float FlashIntensity(float sinceStrike)
        {
            if (sinceStrike < 0f || sinceStrike >= FlashLength)
            {
                return 0f;
            }

            // Like Unity's Mathf.Exp: the power rounded to a float, the exponential worked out in double.
            // The flash then matches the Northline code exactly.
            float strikePower = -StrikeFade * sinceStrike;
            float flickerPower = -FlickerSharpness * MathF.Abs(sinceStrike - FlickerTime);
            return StrikeIntensity * (float)Math.Exp(strikePower) + FlickerIntensity * (float)Math.Exp(flickerPower);
        }

        /// <summary>
        /// Moves the clock on to <paramref name="now"/> and works out what
        /// happens in this step: the lightning's intensity and whether the
        /// thunder, a monitor beep or a creak starts.
        /// </summary>
        /// <param name="now">The time now, in seconds; it only goes forward.</param>
        /// <param name="blend">The listener's interior blend, 0 outdoors to 1 inside.</param>
        public void Step(float now, float blend)
        {
            ThunderStarts = false;
            MonitorBeeps = false;
            CreakSounds = false;
            Flash(now);

            if (now >= nextThunder)
            {
                nextThunder = float.PositiveInfinity;
                ThunderStarts = true;
            }

            if (now >= nextMonitor)
            {
                // The next beep is timed whether or not this one is heard.
                nextMonitor = now + RandomRange.Between(random, MonitorGapLow, MonitorGapHigh);
                if (blend > MonitorAudibleBlend)
                {
                    MonitorSide = RandomRange.Between(random, MonitorSideLow, MonitorSideHigh);
                    MonitorBeeps = true;
                }
            }

            if (now >= nextCreak)
            {
                nextCreak = now + RandomRange.Between(random, CreakGapLow, CreakGapHigh);
                CreakSounds = true;
            }
        }

        // Lights the flash while one is due, times its thunder at the strike, and times the next flash once it is over.
        private void Flash(float now)
        {
            float sinceStrike = now - nextFlash;
            if (sinceStrike >= 0f && sinceStrike < FlashLength)
            {
                Lightning = FlashIntensity(sinceStrike);
                if (!thunderScheduled)
                {
                    thunderScheduled = true;
                    nextThunder = now + RandomRange.Between(random, ThunderDelayLow, ThunderDelayHigh);
                }

                return;
            }

            Lightning = 0f;
            if (sinceStrike >= FlashLength)
            {
                nextFlash = now + RandomRange.Between(random, FlashGapLow, FlashGapHigh);
                thunderScheduled = false;
            }
        }
    }
}
