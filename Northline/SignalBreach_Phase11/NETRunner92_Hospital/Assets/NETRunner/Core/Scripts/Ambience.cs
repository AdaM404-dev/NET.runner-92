using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The hospital's ambience as the listener goes in and out. The interior
    /// blend (0 outdoors, 1 deep inside) follows the zones at a steady rate,
    /// and the sound volumes follow the blend: the rain and wind fade as the
    /// ventilation rises, thunder and the structure's creaks are quieter
    /// inside, and the distant monitor grows louder. The dust and the valve
    /// steam come from the zones and the distance to the valve. Plain maths
    /// with no scene objects (see AmbienceTests).
    /// </summary>
    public static class Ambience
    {
        /// <summary>How fast the interior blend follows the zones, per second: from outdoors to fully inside in about 0.7 s.</summary>
        public const float BlendRate = 1.4f;

        /// <summary>The valve steams while the listener is closer to it than this, in metres.</summary>
        public const float SteamRange = 11f;

        private const float RainAndWindOutdoors = 0.48f;
        private const float RainAndWindIndoors = 0.034f;
        private const float VentilationOutdoors = 0.012f;
        private const float VentilationIndoors = 0.27f;
        private const float ThunderOutdoors = 0.72f;
        private const float ThunderIndoors = 0.26f;
        private const float CreakOutdoors = 0.34f;
        private const float CreakIndoors = 0.15f;
        private const float MonitorFullyInside = 0.22f;
        private const float DustPerSecond = 29f;
        private const float SteamPerSecond = 6f;

        /// <summary>
        /// The interior blend one frame of <paramref name="dt"/> seconds later:
        /// <see cref="BlendRate"/> × dt closer to the zones' cover, without
        /// passing it.
        /// </summary>
        /// <param name="blend">The interior blend now, 0 to 1.</param>
        /// <param name="cover">How much the listener is inside the zones now, 0 to 1.</param>
        public static float Follow(float blend, float cover, float dt)
        {
            float step = dt * BlendRate;
            float gap = cover - blend;
            if (MathF.Abs(gap) <= step)
            {
                return cover;
            }

            return gap > 0f ? blend + step : blend - step;
        }

        /// <summary>Volume of the rain and wind loop: 0.48 outdoors, 0.034 fully inside.</summary>
        public static float RainAndWindVolume(float blend) => Easing.Lerp(RainAndWindOutdoors, RainAndWindIndoors, blend);

        /// <summary>Volume of the ventilation loop: 0.012 outdoors, 0.27 fully inside.</summary>
        public static float VentilationVolume(float blend) => Easing.Lerp(VentilationOutdoors, VentilationIndoors, blend);

        /// <summary>Volume of a thunderclap: 0.72 outdoors, 0.26 fully inside.</summary>
        public static float ThunderVolume(float blend) => Easing.Lerp(ThunderOutdoors, ThunderIndoors, blend);

        /// <summary>Volume of a creak of the fence and structure: 0.34 outdoors, 0.15 fully inside.</summary>
        public static float CreakVolume(float blend) => Easing.Lerp(CreakOutdoors, CreakIndoors, blend);

        /// <summary>Volume of the distant monitor's beep: 0.22 × the blend, so 0.22 fully inside.</summary>
        public static float MonitorVolume(float blend) => MonitorFullyInside * blend;

        /// <summary>Dust motes a second around the listener; <paramref name="dust"/> is the zones' dust where the listener is, 0 to 1.</summary>
        public static float DustRate(float dust) => DustPerSecond * dust;

        /// <summary>Steam puffs a second from the damaged valve, at this many metres from the listener.</summary>
        public static float SteamRate(float distance) => distance < SteamRange ? SteamPerSecond : 0f;
    }
}
