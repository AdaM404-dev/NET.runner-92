namespace NetRunner.Core
{
    /// <summary>
    /// The filter that scales a frame rendered below 100 % up to the window,
    /// as a benchmark run can choose it with <c>-net92-upscaler</c> (#36,
    /// round 3). <see cref="UpscalerList"/> reads and writes the names.
    /// </summary>
    public enum Upscaler
    {
        /// <summary>Whatever the quality level's pipeline asset sets.</summary>
        Default,

        /// <summary>Bicubic Catmull-Rom: cheap and soft; the pipeline assets' choice today.</summary>
        CatmullRom,

        /// <summary>AMD FidelityFX Super Resolution 1.0: sharper edges for a little more work.</summary>
        Fsr1
    }
}
