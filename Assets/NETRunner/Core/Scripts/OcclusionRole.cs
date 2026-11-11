namespace NetRunner.Core
{
    /// <summary>
    /// The part an object plays in occlusion culling, which skips drawing what
    /// walls and floors hide from the camera. <see cref="OcclusionRules"/>
    /// picks it.
    /// </summary>
    public enum OcclusionRole
    {
        /// <summary>No part: the culling data is baked, and the object moves.</summary>
        None,

        /// <summary>Can be hidden behind others, but hides nothing itself.</summary>
        Occludee,

        /// <summary>Hides what is behind it, and can be hidden itself.</summary>
        OccluderAndOccludee
    }
}
