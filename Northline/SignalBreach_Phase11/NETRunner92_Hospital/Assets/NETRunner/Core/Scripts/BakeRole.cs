namespace NetRunner.Core
{
    /// <summary>
    /// How a renderer takes part in the hospital's lighting bake (#79); see
    /// <see cref="LightBakeRules"/>.
    /// </summary>
    public enum BakeRole
    {
        /// <summary>Moves or is see-through: not in the bake; lit at runtime by the light probes.</summary>
        NotBaked,

        /// <summary>Stays put but is small: blocks and bounces light in the bake, and is lit by the light probes.</summary>
        ProbeLit,

        /// <summary>Stays put and is large enough: blocks and bounces light, and gets its own lightmap.</summary>
        Lightmapped
    }
}
