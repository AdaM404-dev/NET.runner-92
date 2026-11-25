using UnityEditor;

namespace NetRunner.SceneBuilding.Editor
{
    /// <summary>
    /// The two layer slots the hospital's builders name in the project's tag
    /// manager. Runtime code finds them by name (a <c>LayerMask</c> or
    /// <c>LayerMask.NameToLayer</c>); only the builders that create the
    /// layers use the slot numbers.
    /// </summary>
    public static class HospitalLayers
    {
        /// <summary>Small furnishings and ground clutter that cameras stop drawing beyond a short distance.</summary>
        public const int NearDetail = 8;

        /// <summary>The name of <see cref="NearDetail"/>.</summary>
        public const string NearDetailName = "Near-detail geometry";

        /// <summary>The LATCH-07 arms and tablet, which the CCTV camera does not draw.</summary>
        public const int Viewmodel = 9;

        /// <summary>The name of <see cref="Viewmodel"/>.</summary>
        public const string ViewmodelName = "First-person viewmodel";

        private const string LayersProperty = "layers";

        /// <summary>The name currently in a layer slot, empty when the slot is free.</summary>
        public static string NameOf(int slot)
        {
            return Slot(out _, slot).stringValue;
        }

        /// <summary>Writes <paramref name="name"/> into a layer slot.</summary>
        public static void Assign(int slot, string name)
        {
            SerializedProperty layer = Slot(out SerializedObject tags, slot);
            layer.stringValue = name;
            tags.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Slot(out SerializedObject tags, int slot)
        {
            tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(HospitalAssets.TagManager)[0]);
            return tags.FindProperty(LayersProperty).GetArrayElementAtIndex(slot);
        }
    }
}
