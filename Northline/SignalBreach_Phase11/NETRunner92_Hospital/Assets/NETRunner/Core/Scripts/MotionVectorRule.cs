namespace NetRunner.Core
{
    /// <summary>
    /// Whether the player camera needs HDRP's motion vectors (#36, round 3).
    /// Only temporal effects read them: temporal antialiasing, a temporal
    /// upscaler (STP), motion blur and the ambient occlusion's temporal
    /// filter. HDRP draws them every frame while they are on, so the game
    /// switches them off when none of those runs, as on Low and Medium.
    /// </summary>
    public static class MotionVectorRule
    {
        /// <summary>
        /// True when something that runs reads motion vectors:
        /// <paramref name="antialias"/> is TAA (<see cref="QualityPreset.TemporalAntialias"/>),
        /// or motion blur, ambient occlusion or a temporal upscaler is on.
        /// </summary>
        public static bool AreNeeded(int antialias, bool motionBlur, bool ambientOcclusion, bool temporalUpscaler)
        {
            return antialias == QualityPreset.TemporalAntialias || motionBlur || ambientOcclusion || temporalUpscaler;
        }
    }
}
