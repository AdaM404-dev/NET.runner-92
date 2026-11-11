namespace NetRunner.Core
{
    /// <summary>
    /// The screen size a first start uses: the display's own size, but never
    /// taller than a limit, keeping the display's shape. A 4K screen then
    /// starts at 1080p instead of drawing four times as many pixels
    /// (see StartResolutionTests).
    /// </summary>
    public static class StartResolution
    {
        /// <summary>
        /// Scales <paramref name="width"/> × <paramref name="height"/> down to at
        /// most <paramref name="maxHeight"/> rows. Sizes that already fit, or
        /// that are not positive, come back unchanged. The width is rounded to
        /// an even number of pixels.
        /// </summary>
        public static (int Width, int Height) Cap(int width, int height, int maxHeight)
        {
            if (width <= 0 || height <= 0 || maxHeight <= 0 || height <= maxHeight)
            {
                return (width, height);
            }

            long scaledWidth = (long)width * maxHeight / height;
            int evenWidth = (int)(scaledWidth - scaledWidth % 2);
            return (evenWidth, maxHeight);
        }
    }
}
