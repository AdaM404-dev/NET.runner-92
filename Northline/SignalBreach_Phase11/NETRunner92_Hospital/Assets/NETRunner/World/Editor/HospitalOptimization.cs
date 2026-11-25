using System;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// The hospital's first optimisation pass (Northline Phase 05): shadows
    /// only within 36 m in two cascades, the cheapest volumetric fog quality,
    /// no shadows from furnishings and ground clutter, and the small ones on
    /// the near-detail layer, which cameras stop drawing beyond a short
    /// distance (the player's camera and the CCTV camera set that distance).
    /// </summary>
    public static class HospitalOptimization
    {
        private const string StormVolume = "ZONE_Exterior_Quarantine";
        private const string FurnishingMark = "_FF_";
        private const string GroundCoverPrefix = "Q4_";
        private const float ShadowDistance = 36f;
        private const int ShadowCascades = 2;
        private const float FirstCascadeSplit = .32f;
        private const int LowestFogQuality = 0;

        // Furnishings smaller than this in every direction count as near detail.
        private const float SmallFurnishing = .86f;

        /// <summary>Optimises the hospital scene, then runs the hospital checks and builds the Windows player if they pass.</summary>
        public static void OptimizeAndBuild()
        {
            Optimize();
            PlayModeValidation.RunAndBuild();
        }

        /// <summary>Applies the optimisation to the hospital scene and saves it.</summary>
        public static void Optimize()
        {
            Scene scene = EditorSceneManager.OpenScene(HospitalAssets.ScenePath);
            HospitalLayers.Assign(HospitalLayers.NearDetail, HospitalLayers.NearDetailName);
            LimitShadows(GameObject.Find(StormVolume).GetComponent<Volume>().sharedProfile);
            foreach (Volume volume in UnityEngine.Object.FindObjectsByType<Volume>())
            {
                if (!volume.sharedProfile || !volume.sharedProfile.TryGet<Fog>(out Fog fog))
                {
                    continue;
                }

                fog.quality.Override(LowestFogQuality);
                EditorUtility.SetDirty(fog);
            }

            int closeDetail = 0;
            int simplifiedShadows = 0;
            foreach (MeshRenderer renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>())
            {
                Bounds bounds = renderer.bounds;
                bool isFurnishing = renderer.name.Contains(FurnishingMark);
                bool isGroundCover = IsGroundCover(renderer.name);
                if (isFurnishing || isGroundCover)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    simplifiedShadows++;
                }

                if (isGroundCover || isFurnishing && Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) < SmallFurnishing)
                {
                    renderer.gameObject.layer = HospitalLayers.NearDetail;
                    closeDetail++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"NET92 OPTIMIZED | {closeDetail} distance-culled fine renderers | {simplifiedShadows} fine shadow casters removed");
        }

        private static void LimitShadows(VolumeProfile storm)
        {
            if (!storm.TryGet<HDShadowSettings>(out HDShadowSettings shadow))
            {
                shadow = storm.Add<HDShadowSettings>(true);
                AssetDatabase.AddObjectToAsset(shadow, storm);
            }

            shadow.maxShadowDistance.Override(ShadowDistance);
            shadow.cascadeShadowSplitCount.Override(ShadowCascades);
            shadow.cascadeShadowSplit0.Override(FirstCascadeSplit);
            EditorUtility.SetDirty(shadow);
            EditorUtility.SetDirty(storm);
        }

        // The grounds' sedge, grass, debris and leaves.
        private static bool IsGroundCover(string name)
        {
            return name.StartsWith(GroundCoverPrefix) && (name.Contains("sedge", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Volunteer grass") || name.Contains("Local debris") || name.Contains("Windblown leaf"));
        }
    }
}
