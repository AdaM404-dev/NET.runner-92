using System;
using System.Collections.Generic;

namespace NetRunner.Core
{
    /// <summary>
    /// Reads and writes lists of <see cref="RenderFeature"/> names, such as the
    /// <c>volumetric-fog,shadows</c> after <c>-net92-without</c>
    /// (see RenderFeatureListTests).
    /// </summary>
    public static class RenderFeatureList
    {
        private const char Separator = ',';
        private const string NoFeatures = "none";

        // The order in which Format writes them.
        private static readonly (RenderFeature Feature, string Name)[] Names =
        {
            (RenderFeature.OcclusionCulling, "occlusion-culling"),
            (RenderFeature.VolumetricFog, "volumetric-fog"),
            (RenderFeature.FogLights, "fog-lights"),
            (RenderFeature.Shadows, "shadows"),
            (RenderFeature.AmbientOcclusion, "ambient-occlusion"),
            (RenderFeature.DistantLights, "distant-lights"),
            (RenderFeature.TransparentObjects, "transparent-objects"),
            (RenderFeature.Decals, "decals")
        };

        /// <summary>
        /// The features named in <paramref name="text"/>, separated by commas,
        /// in any letter case and with spaces around the names. "none" names no
        /// feature. Names that mean nothing are left out and returned in
        /// <paramref name="unknown"/>, so a typing error cannot pass unnoticed.
        /// </summary>
        public static RenderFeature Parse(string text, out IReadOnlyList<string> unknown)
        {
            var features = RenderFeature.None;
            var notFound = new List<string>();
            unknown = notFound;
            if (string.IsNullOrWhiteSpace(text))
            {
                return features;
            }

            foreach (string part in text.Split(Separator))
            {
                string name = part.Trim();
                if (name.Length == 0 || string.Equals(name, NoFeatures, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                RenderFeature feature = Find(name);
                if (feature == RenderFeature.None)
                {
                    notFound.Add(name);
                }
                else
                {
                    features |= feature;
                }
            }

            return features;
        }

        /// <summary>
        /// The names of <paramref name="features"/>, separated by commas, always
        /// in the same order; "none" when there are none.
        /// </summary>
        public static string Format(RenderFeature features)
        {
            var names = new List<string>();
            foreach (var (feature, name) in Names)
            {
                if ((features & feature) != 0)
                {
                    names.Add(name);
                }
            }

            return names.Count == 0 ? NoFeatures : string.Join(Separator.ToString(), names);
        }

        private static RenderFeature Find(string name)
        {
            foreach (var (feature, known) in Names)
            {
                if (string.Equals(name, known, StringComparison.OrdinalIgnoreCase))
                {
                    return feature;
                }
            }

            return RenderFeature.None;
        }
    }
}
