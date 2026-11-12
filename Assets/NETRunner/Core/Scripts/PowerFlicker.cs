using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The hospital's residual power: how bright a failing light or screen is
    /// at a moment (0 to 1). Each fixture repeats an outage every 11.5 s plus
    /// 9% of its own phase, so no two run in step. A lamp stutters to 4%
    /// twice, browns out to 27%, then burns fully; a screen sits at 18% for
    /// 0.28 s and then waves around 78%. Plain maths (see PowerFlickerTests).
    /// </summary>
    public static class PowerFlicker
    {
        /// <summary>Seconds between outages, before the phase's share is added.</summary>
        public const float Cycle = 11.5f;

        /// <summary>The share of a fixture's phase added to its cycle.</summary>
        public const float CycleSpread = .09f;

        private const float Stutter = .04f;
        private const float BrownOut = .27f;
        private const float FirstStutterEnd = .13f;
        private const float SecondStutterStart = .29f;
        private const float SecondStutterEnd = .43f;
        private const float BrownOutStart = .64f;
        private const float BrownOutEnd = 1.20f;

        private const float ScreenLow = .18f;
        private const float ScreenLowEnd = .28f;
        private const float ScreenLevel = .78f;
        private const float ScreenWave = .20f;
        private const float ScreenWaveRate = 3.1f;

        /// <summary>
        /// The brightness at <paramref name="time"/> seconds of a fixture with
        /// this <paramref name="phase"/>: a lamp, or else a screen.
        /// </summary>
        public static float Level(float time, float phase, bool lamp)
        {
            float shifted = time + phase;
            float cycle = Scalar.Repeat(shifted, Cycle + phase * CycleSpread);
            if (lamp)
            {
                if (cycle < FirstStutterEnd || (cycle > SecondStutterStart && cycle < SecondStutterEnd))
                {
                    return Stutter;
                }

                return cycle < BrownOutEnd && cycle > BrownOutStart ? BrownOut : 1f;
            }

            if (cycle < ScreenLowEnd)
            {
                return ScreenLow;
            }

            float wave = shifted * ScreenWaveRate;
            return ScreenLevel + (float)Math.Sin(wave) * ScreenWave;
        }
    }
}
