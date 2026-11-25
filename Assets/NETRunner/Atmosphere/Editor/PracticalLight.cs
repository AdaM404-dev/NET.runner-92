using System;
using UnityEngine;

namespace NetRunner.Atmosphere.Editor
{
    /// <summary>
    /// One light fixture as the hospital's <c>SceneSource.json</c> lists it.
    /// The field names are the file's keys, so they must not be renamed.
    /// </summary>
    [Serializable]
    public sealed class PracticalLight
    {
        [SerializeField] private string name;
        [SerializeField] private int floor;
        [SerializeField] private float[] position;
        [SerializeField] private float[] color;
        [SerializeField] private float[] direction;
        [SerializeField] private float energy;
        [SerializeField] private float size;

        /// <summary>The fixture's name in the source model.</summary>
        public string Name => name;

        /// <summary>0 for the grounds, 1 to 4 for the hospital's floors.</summary>
        public int Floor => floor;

        /// <summary>Where it hangs.</summary>
        public Vector3 Position => new Vector3(position[0], position[1], position[2]);

        /// <summary>Its light colour.</summary>
        public Color Color => new Color(color[0], color[1], color[2], 1f);

        /// <summary>Where it points; near zero for a fixture that shines all round.</summary>
        public Vector3 Direction => new Vector3(direction[0], direction[1], direction[2]);

        /// <summary>Its power in the source model.</summary>
        public float Energy => energy;

        /// <summary>The size of the fitting in the source model.</summary>
        public float Size => size;
    }
}
