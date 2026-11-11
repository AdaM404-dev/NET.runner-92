using System;

namespace NetRunner.Core
{
    /// <summary>
    /// Rendering features that a benchmark run can switch off, so that the run
    /// shows what each one costs on the machine that runs it. Several can be
    /// combined; <see cref="RenderFeatureList"/> reads and writes their names.
    /// </summary>
    [Flags]
    public enum RenderFeature
    {
        None = 0,

        /// <summary>Hiding what walls and floors cover, using the baked occlusion data.</summary>
        OcclusionCulling = 1 << 0,

        /// <summary>The volumetric fog, and every light shining into it.</summary>
        VolumetricFog = 1 << 1,

        /// <summary>The point and spot lights' part of the volumetric fog; the fog itself stays.</summary>
        FogLights = 1 << 2,

        /// <summary>Every shadow map.</summary>
        Shadows = 1 << 3,

        /// <summary>Screen-space ambient occlusion.</summary>
        AmbientOcclusion = 1 << 4,

        /// <summary>Point and spot lights far from the camera; the near ones stay.</summary>
        DistantLights = 1 << 5,

        /// <summary>See-through surfaces: glass, rain, dust and steam.</summary>
        TransparentObjects = 1 << 6,

        /// <summary>
        /// Decals, and with them the depth pass that HDRP draws first for every
        /// surface that can receive one, even when the scene has no decals.
        /// </summary>
        Decals = 1 << 7
    }
}
