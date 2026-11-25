using System;
using NetRunner.Atmosphere.Editor;
using UnityEngine;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// One part of the Northline hospital as exported to <c>SceneSource.json</c>
    /// (the building or the grounds): the placed objects, their materials,
    /// the light fixtures and how many meshes <c>MeshData.bytes</c> holds.
    /// The field names are the file's keys, so they must not be renamed.
    /// </summary>
    [Serializable]
    public sealed class SceneSource
    {
        [SerializeField] private SourcePlacement[] objects;
        [SerializeField] private SourceSurface[] materials;
        [SerializeField] private PracticalLight[] lights;
        [SerializeField] private int meshCount;

        /// <summary>Every placed object, in the file's order.</summary>
        public SourcePlacement[] Objects => objects;

        /// <summary>The materials the meshes' submeshes refer to by index.</summary>
        public SourceSurface[] Materials => materials;

        /// <summary>The light fixtures.</summary>
        public PracticalLight[] Lights => lights;

        /// <summary>How many meshes the binary mesh file holds.</summary>
        public int MeshCount => meshCount;
    }
}
