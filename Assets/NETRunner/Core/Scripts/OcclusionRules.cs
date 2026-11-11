namespace NetRunner.Core
{
    /// <summary>
    /// Which part an object plays in the baked occlusion culling (see
    /// OcclusionRulesTests). The data is baked once for objects that stay
    /// where they are, so anything that moves takes no part; see-through
    /// surfaces such as glass can be hidden but must not hide what is behind
    /// them.
    /// </summary>
    public static class OcclusionRules
    {
        /// <summary>
        /// The role of an object that <paramref name="moves"/> or not and that
        /// can be <paramref name="seenThrough"/> or not.
        /// </summary>
        public static OcclusionRole RoleOf(bool moves, bool seenThrough)
        {
            if (moves)
            {
                return OcclusionRole.None;
            }

            return seenThrough ? OcclusionRole.Occludee : OcclusionRole.OccluderAndOccludee;
        }
    }
}
