using System;
using UnityEngine;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// One material in <c>SceneSource.json</c>: colour and transparency,
    /// roughness, metalness, clear coat, glow, and the wear pattern on its
    /// surface ("plain" for none). The field names are the file's keys.
    /// </summary>
    [Serializable]
    public sealed class SourceSurface
    {
        [SerializeField] private string name;
        [SerializeField] private string surface;
        [SerializeField] private float[] color;
        [SerializeField] private float[] emission;
        [SerializeField] private float rough;
        [SerializeField] private float metal;
        [SerializeField] private float alpha;
        [SerializeField] private float emissionStrength;
        [SerializeField] private float coat;

        /// <summary>The material's name.</summary>
        public string Name => name;

        /// <summary>Its wear pattern: "plain", "coating", "mineral" or "grit".</summary>
        public string Surface => surface;

        /// <summary>Its colour with its opacity.</summary>
        public Color Color => new Color(color[0], color[1], color[2], alpha);

        /// <summary>Its glow colour, before <see cref="EmissionStrength"/>.</summary>
        public Color Emission => new Color(emission[0], emission[1], emission[2], 1f);

        /// <summary>Roughness, 0 polished to 1 matte.</summary>
        public float Roughness => rough;

        /// <summary>Metalness, 0 to 1.</summary>
        public float Metallic => metal;

        /// <summary>Opacity, 0 to 1.</summary>
        public float Alpha => alpha;

        /// <summary>How strongly it glows.</summary>
        public float EmissionStrength => emissionStrength;

        /// <summary>Clear-coat amount, 0 to 1.</summary>
        public float Coat => coat;
    }
}
