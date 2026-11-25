namespace NetRunner.SceneBuilding.Editor
{
    /// <summary>
    /// The names of the hospital scene's top-level objects. Each builder runs
    /// on its own after reopening the saved scene, so it finds the others'
    /// work by these names.
    /// </summary>
    public static class HospitalHierarchy
    {
        /// <summary>The imported building and grounds, everything that is drawn.</summary>
        public const string ImportedEnvironment = "Northline Medical — imported environment";

        /// <summary>The building's colliders, kept apart from what is drawn.</summary>
        public const string Collision = "Collision — renderer-free";

        /// <summary>Lights, zones, weather and sound.</summary>
        public const string Atmosphere = "Atmosphere";

        /// <summary>Doors, stairs, the lift, residual power, the K-7s, LATCH-07's relay and the CCTV.</summary>
        public const string Systems = "Hospital systems — circulation and patrols";

        /// <summary>The imported hospital under <see cref="ImportedEnvironment"/> and <see cref="Collision"/>.</summary>
        public const string Hospital = "Hospital";

        /// <summary>The hospital's light fixtures under <see cref="Atmosphere"/>.</summary>
        public const string HospitalPracticals = "Hospital practicals";
    }
}
