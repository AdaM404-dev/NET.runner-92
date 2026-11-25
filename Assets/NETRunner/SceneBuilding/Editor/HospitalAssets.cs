namespace NetRunner.SceneBuilding.Editor
{
    /// <summary>
    /// Where the hospital scene lives and where its builders read and write
    /// assets. The art in these folders is AdaM404's; the builders rewrite
    /// some of it, so they run only when the source changes.
    /// </summary>
    public static class HospitalAssets
    {
        /// <summary>The hospital's art and scenes.</summary>
        public const string Root = "Assets/NETRunner92";

        /// <summary>The hospital scene the builders create and update.</summary>
        public const string ScenePath = Root + "/Scenes/Hospital_Exploration.unity";

        /// <summary>The HDRP pipeline asset and the sky.</summary>
        public const string Rendering = Root + "/Rendering";

        /// <summary>Fog profiles, particle materials, the mixer and the ambience clips.</summary>
        public const string Atmosphere = Root + "/Atmosphere";

        /// <summary>The imported building, its materials and the K-7.</summary>
        public const string Environment = Root + "/Environment";

        /// <summary>The building's materials and the builders' own system materials.</summary>
        public const string Materials = Environment + "/Materials";

        /// <summary>The player prefab and the LATCH-07 viewmodel.</summary>
        public const string Player = Root + "/Player";

        /// <summary>Unity's tag and layer settings.</summary>
        public const string TagManager = "ProjectSettings/TagManager.asset";
    }
}
