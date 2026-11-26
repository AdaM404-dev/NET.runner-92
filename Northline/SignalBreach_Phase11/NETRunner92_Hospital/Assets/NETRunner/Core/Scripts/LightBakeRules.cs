namespace NetRunner.Core
{
    /// <summary>
    /// What the hospital's lighting bake includes (#79, Samuel's decision of
    /// 2026-11-26; see LightBakeRulesTests). A lamp is baked, its light
    /// computed once in the editor and stored in lightmaps and light probes,
    /// unless its light changes while the game runs: it pulses or flickers, a
    /// script drives it (residual power, lightning), or it moves (the lift).
    /// Those stay live. A renderer takes part in the bake when it stays put
    /// and light cannot pass through it. Only surfaces that are wide in two
    /// directions get a lightmap: small objects, and thin ones such as poles
    /// and strips, which a lightmap covers with too few texels and lets light
    /// leak into, are lit by the probes.
    /// </summary>
    public static class LightBakeRules
    {
        /// <summary>The breadth, in metres, below which a renderer is lit by the probes and not lightmapped.</summary>
        public const float SmallestLightmapped = .5f;

        /// <summary>
        /// Whether a lamp stays live: it <paramref name="pulses"/>, is
        /// <paramref name="scripted"/>, or <paramref name="moves"/>.
        /// </summary>
        public static bool StaysLive(bool pulses, bool scripted, bool moves)
        {
            return pulses || scripted || moves;
        }

        /// <summary>
        /// The bake role of a renderer that <paramref name="moves"/> or not, can
        /// be <paramref name="seenThrough"/> or not, and has <paramref name="breadth"/>
        /// as the second-longest side of its bounds, in metres.
        /// </summary>
        public static BakeRole RoleOf(bool moves, bool seenThrough, float breadth)
        {
            if (moves || seenThrough)
            {
                return BakeRole.NotBaked;
            }

            return breadth < SmallestLightmapped ? BakeRole.ProbeLit : BakeRole.Lightmapped;
        }
    }
}
