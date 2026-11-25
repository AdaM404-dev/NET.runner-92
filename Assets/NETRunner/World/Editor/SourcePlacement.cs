using System;
using UnityEngine;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// One placed object in <c>SceneSource.json</c>: which mesh, where, on
    /// which floor and in which group, whether it is drawn and what kind of
    /// collider it gets. The field names are the file's keys.
    /// </summary>
    [Serializable]
    public sealed class SourcePlacement
    {
        [SerializeField] private string name;
        [SerializeField] private string group;
        [SerializeField] private string parent;
        [SerializeField] private int floor;
        [SerializeField] private int mesh;
        [SerializeField] private int collision;
        [SerializeField] private int detail;
        [SerializeField] private bool render;
        [SerializeField] private float[] position;
        [SerializeField] private float[] rotation;
        [SerializeField] private float[] scale;

        /// <summary>The object's name.</summary>
        public string Name => name;

        /// <summary>The group it is filed under on its floor.</summary>
        public string Group => group;

        /// <summary>The assembly it belongs to within its group, or empty.</summary>
        public string Parent => parent;

        /// <summary>0 for the grounds and envelope, 1 to 4 for the floors.</summary>
        public int Floor => floor;

        /// <summary>The index of its mesh in the binary mesh file.</summary>
        public int Mesh => mesh;

        /// <summary>0 none, 1 its mesh, 2 its bounding box, 3 a stair ramp.</summary>
        public int Collision => collision;

        /// <summary>1 for fine detail that casts no shadow.</summary>
        public int Detail => detail;

        /// <summary>Whether it is drawn, or only collides.</summary>
        public bool IsRendered => render;

        /// <summary>Where it stands.</summary>
        public Vector3 Position => new Vector3(position[0], position[1], position[2]);

        /// <summary>How it is turned.</summary>
        public Quaternion Rotation => new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);

        /// <summary>Its scale.</summary>
        public Vector3 Scale => new Vector3(scale[0], scale[1], scale[2]);
    }
}
