using System;

namespace NetRunner.Core
{
    /// <summary>
    /// Reads and writes <see cref="Upscaler"/> names, such as the <c>fsr1</c>
    /// after <c>-net92-upscaler</c> (see UpscalerListTests).
    /// </summary>
    public static class UpscalerList
    {
        private static readonly (Upscaler Upscaler, string Name)[] Names =
        {
            (Upscaler.Default, "default"),
            (Upscaler.CatmullRom, "catmull-rom"),
            (Upscaler.Fsr1, "fsr1")
        };

        /// <summary>
        /// The upscaler named in <paramref name="text"/>, in any letter case and
        /// with spaces around it. Nothing given means <see cref="Upscaler.Default"/>;
        /// a name that means nothing returns false, so a typing error cannot pass unnoticed.
        /// </summary>
        public static bool TryParse(string text, out Upscaler upscaler)
        {
            upscaler = Upscaler.Default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            foreach (var (candidate, name) in Names)
            {
                if (string.Equals(text.Trim(), name, StringComparison.OrdinalIgnoreCase))
                {
                    upscaler = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>The name of <paramref name="upscaler"/>, as <see cref="TryParse"/> reads it.</summary>
        public static string Format(Upscaler upscaler)
        {
            foreach (var (candidate, name) in Names)
            {
                if (candidate == upscaler)
                {
                    return name;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(upscaler), upscaler, "No name for this upscaler.");
        }
    }
}
