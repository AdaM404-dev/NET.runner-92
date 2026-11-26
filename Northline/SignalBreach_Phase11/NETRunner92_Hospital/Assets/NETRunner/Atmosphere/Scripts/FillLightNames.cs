namespace NetRunner.Atmosphere
{
    /// <summary>
    /// The names of the hospital's fill lights: dim point lights that imitate
    /// light bouncing off the clinical fittings, added by the editor's
    /// ClinicalFillLights. The benchmark finds them by these names to measure
    /// what baking them into light probes would save (#36, round 3).
    /// </summary>
    public static class FillLightNames
    {
        /// <summary>The scene node that holds every fill light.</summary>
        public const string Group = "Low-output clinical fill";

        /// <summary>The name of each fill light.</summary>
        public const string Light = "Reflected clinical practical";
    }
}
