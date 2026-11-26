namespace NetRunner.Core
{
    /// <summary>
    /// Whether the hospital's point and spot lights shine into the volumetric
    /// fog on each quality level (#36 round 3, step 2). Alpha test 6 measured
    /// on integrated graphics: taking them out of the fog saved 10–19% on
    /// Medium, and the screenshots barely changed. High keeps them in the fog;
    /// Low has no volumetric fog at all. Directional lights are not affected.
    /// Fading distant lights out was measured too, but Samuel decided against
    /// it (2026-11-26): it turned the far end of long corridors dark.
    /// </summary>
    public static class FogLights
    {
        /// <summary>
        /// How strongly indirect light lights the volumetric fog (HDRP's fog "GI
        /// dimmer"). With the lighting baked (#79) the light probes hold every
        /// baked lamp's light and would fill the rooms with a bright haze at
        /// HDRP's default of 1; before the bake the fog got only the dim sky.
        /// </summary>
        public const float BakedGiDimmer = .1f;

        /// <summary>
        /// Whether point and spot lights shine into the volumetric fog on quality
        /// <paramref name="level"/> (0 Low, 1 Medium, 2 High): only on High.
        /// </summary>
        public static bool AreOnFor(int level)
        {
            return level >= QualityPreset.HighestLevel;
        }
    }
}
