using System;
using NetRunner.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Bakes the hospital's occlusion culling (#36): marks the imported
    /// building and exterior static, so their walls and floors hide what is
    /// behind them, then computes the data the cameras cull with. What moves
    /// (doors, the lift, the K-7s, the player) stays dynamic; the roles come
    /// from <see cref="OcclusionRules"/>.
    /// </summary>
    public static partial class HospitalSceneBuilder
    {
        private const string ImportedEnvironment = "Northline Medical — imported environment";
        private const string SurfaceTypeProperty = "_SurfaceType";
        private const string AlphaClipProperty = "_AlphaCutoffEnable";

        // Umbra's resolution. Walls and floors are far larger than the smallest
        // occluder; doorways and windows are far wider than the smallest hole,
        // so the camera still sees through them.
        private const float SmallestOccluder = 2f;
        private const float SmallestHole = .25f;
        private const float BackfaceThreshold = 100f;

        private const StaticEditorFlags OcclusionFlags = StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

        /// <summary>
        /// Marks the hospital scene's renderers for occlusion culling, bakes
        /// the data and saves the scene. Runs headless with
        /// <c>-executeMethod NetRunner.World.Editor.HospitalSceneBuilder.BakeOcclusion</c>.
        /// </summary>
        [MenuItem("NET.runner-92/Bake Occlusion Culling")]
        public static void BakeOcclusion()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath);
            Transform environment = GameObject.Find(ImportedEnvironment).transform;
            int occluders = 0;
            int occludeesOnly = 0;
            int dynamic = 0;
            foreach (MeshRenderer renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>())
            {
                bool moves = !renderer.transform.IsChildOf(environment);
                OcclusionRole role = OcclusionRules.RoleOf(moves, IsSeenThrough(renderer));
                SetRole(renderer.gameObject, role);
                if (role == OcclusionRole.OccluderAndOccludee)
                {
                    occluders++;
                }
                else if (role == OcclusionRole.Occludee)
                {
                    occludeesOnly++;
                }
                else
                {
                    dynamic++;
                }
            }

            StaticOcclusionCulling.smallestOccluder = SmallestOccluder;
            StaticOcclusionCulling.smallestHole = SmallestHole;
            StaticOcclusionCulling.backfaceThreshold = BackfaceThreshold;
            if (!StaticOcclusionCulling.Compute())
            {
                throw new InvalidOperationException("The occlusion culling bake failed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"NET92 OCCLUSION | {occluders} occluders | {occludeesOnly} see-through occludees | {dynamic} dynamic renderers");
        }

        private static bool IsSeenThrough(Renderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material && (IsOn(material, SurfaceTypeProperty) || IsOn(material, AlphaClipProperty)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsOn(Material material, string property)
        {
            return material.HasProperty(property) && material.GetFloat(property) > .5f;
        }

        private static void SetRole(GameObject owner, OcclusionRole role)
        {
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(owner) & ~OcclusionFlags;
            if (role == OcclusionRole.OccluderAndOccludee)
            {
                flags |= OcclusionFlags;
            }
            else if (role == OcclusionRole.Occludee)
            {
                flags |= StaticEditorFlags.OccludeeStatic;
            }

            GameObjectUtility.SetStaticEditorFlags(owner, flags);
        }
    }
}
