using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.World.Editor
{
    public static partial class HospitalSceneBuilder
    {
        public static void OptimizeAndBuild()
        {
            OptimizeScene();
            PlayModeValidation.RunAndBuild();
        }

        static void OptimizeScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            var tags = new SerializedObject(settings[0]);
            tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue = "Near-detail geometry";
            tags.ApplyModifiedPropertiesWithoutUndo();

            var global = GameObject.Find("ZONE_Exterior_Quarantine").GetComponent<Volume>().sharedProfile;
            if (!global.TryGet<HDShadowSettings>(out var shadow))
            {
                shadow = global.Add<HDShadowSettings>(true);
                AssetDatabase.AddObjectToAsset(shadow, global);
            }
            shadow.maxShadowDistance.Override(36f);
            shadow.cascadeShadowSplitCount.Override(2);
            shadow.cascadeShadowSplit0.Override(.32f);
            EditorUtility.SetDirty(shadow);
            EditorUtility.SetDirty(global);
            foreach (var volume in Object.FindObjectsByType<Volume>())
            {
                if (!volume.sharedProfile || !volume.sharedProfile.TryGet<Fog>(out var fog)) continue;
                fog.quality.Override(0);
                EditorUtility.SetDirty(fog);
            }

            int closeDetail = 0, simplifiedShadows = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                var b = r.bounds;
                bool furnishing = r.name.Contains("_FF_");
                bool fine = r.name.StartsWith("Q4_") && (r.name.Contains("sedge", System.StringComparison.OrdinalIgnoreCase)
                    || r.name.Contains("Volunteer grass") || r.name.Contains("Local debris") || r.name.Contains("Windblown leaf"));
                if (furnishing || fine)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    simplifiedShadows++;
                }
                if (fine || furnishing && Mathf.Max(b.size.x, Mathf.Max(b.size.y,b.size.z)) < .86f)
                {
                    r.gameObject.layer = 8;
                    closeDetail++;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"NET92 OPTIMIZED | {closeDetail} distance-culled fine renderers | {simplifiedShadows} fine shadow casters removed");
        }
    }
}


