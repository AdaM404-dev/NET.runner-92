using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetRunner.Core;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Enemy.Editor
{
    /// <summary>
    /// Builds the K-7 patrol robot from AdaM404's source FBX: imports its
    /// textures, packs the HDRP mask map, sets up its seven HDRP materials,
    /// strips the collision helpers and the animator, groups its four LODs
    /// and saves <c>K7_Patrol.prefab</c>. Then places the hospital's four
    /// patrols, one per floor, each walking its own stretch of the concourse.
    /// </summary>
    public static class K7PatrolBuilder
    {
        private const string Folder = HospitalAssets.Environment + "/K7";
        private const string ModelPath = Folder + "/K7_Robot.fbx";
        private const string MaskPath = Folder + "/K7_HDRP_Mask.asset";
        private const string PrefabPath = Folder + "/K7_Patrol.prefab";
        private const string MaterialPrefix = "MAT_K7_";
        private const string CollisionPrefix = "COL_";
        private const string LitShader = "HDRP/Lit";
        private const string GroupName = "K-7 autonomous patrols";

        private const string BaseColorMap = "BaseColor";
        private const string NormalMap = "Normal";
        private const string MetallicSmoothnessMap = "MetallicSmoothness";
        private const string OcclusionMap = "AO";
        private const string StatusLight = "StatusLight";

        // The packed HDRP mask: metal in red, occlusion in green, detail mask
        // white in blue, smoothness in alpha, at the source maps' resolution.
        private const int MaskSize = 2048;
        private const byte DetailMaskOn = 255;

        // The armour's response: softer normals, darker occlusion floor, and
        // smoothness and metalness capped below the source maps.
        private const float NormalScale = .65f;
        private const float OcclusionFloor = .28f;
        private const float SmoothnessCeiling = .70f;
        private const float MetallicCeiling = .85f;
        private const float EmissiveExposureWeight = .8f;

        // Each LOD level has the robot's twelve parts.
        private const int PartsPerLod = 12;

        // How far the walkers stand off the concourse's centre line, the
        // first robot's walking speed and how much faster each next one is.
        private const float FloorClearance = .015f;
        private const float LaneOffset = .72f;
        private const float BaseSpeed = 1.03f;
        private const float SpeedStep = .055f;

        private static readonly string[] TextureKinds = { BaseColorMap, NormalMap, MetallicSmoothnessMap, OcclusionMap };

        private static readonly string[] MaterialKinds =
        {
            "Armor", "Internal", "Metal", "Rubber", "SensorGlass", "Cables", StatusLight,
        };

        private static readonly Color StatusBaseColor = new Color(.50f, .68f, .72f);
        private static readonly Color StatusGlow = new Color(.53f, .79f, 1f) * 4.5f;

        // A box round the standing robot, so skinning never culls a limb.
        private static readonly Bounds SkinBounds = new Bounds(Vector3.up, new Vector3(2.6f, 2.8f, 2.6f));

        // Screen height at which each LOD level ends.
        private static readonly float[] LodHeights = { .42f, .22f, .095f, .037f };

        // Each floor's patrol, L1 to L4: the x of its two route ends.
        private static readonly Vector2[] Routes = { new Vector2(-26, -6), new Vector2(-18, 17), new Vector2(5, 27), new Vector2(-23, 6) };

        /// <summary>
        /// Imports the K-7 and saves its prefab. Rewrites the textures' and the
        /// FBX's import settings and the seven materials in AdaM404's K7 folder.
        /// </summary>
        public static GameObject BuildPrefab()
        {
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (modelImporter == null)
            {
                throw new FileNotFoundException("Copy the original K-7 source FBX into Assets.", ModelPath);
            }

            Dictionary<string, Texture2D> textures = ImportTextures();
            Texture2D mask = Mask(textures);
            foreach (string kind in MaterialKinds)
            {
                string slot = MaterialPrefix + kind;
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), slot),
                    SetUpMaterial(slot, kind == StatusLight, textures, mask));
            }

            modelImporter.useFileScale = true;
            modelImporter.bakeAxisConversion = true;
            modelImporter.preserveHierarchy = true;
            modelImporter.animationType = ModelImporterAnimationType.Generic;
            modelImporter.importAnimation = false;
            modelImporter.importNormals = ModelImporterNormals.Import;
            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
            modelImporter.SaveAndReimport();

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            try
            {
                Prepare(instance);
                return PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>Places the four patrols under <paramref name="systems"/>, one per floor.</summary>
        public static void AddPatrols(Transform systems, GameObject prefab)
        {
            Transform group = SceneParts.Node(GroupName, systems);
            for (int i = 0; i < Routes.Length; i++)
            {
                Transform robot = SceneParts.Node($"K-7 Patrol L{i + 1:00}", group);
                Transform model = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, robot)).transform;
                float y = i * StairRules.FloorHeight + FloorClearance;
                float z = i % 2 == 0 ? LaneOffset : -LaneOffset;
                robot.gameObject.AddComponent<K7Patrol>().Configure(model,
                    new[] { new Vector3(Routes[i].x, y, z), new Vector3(Routes[i].y, y, z) }, BaseSpeed + i * SpeedStep);
            }
        }

        private static Dictionary<string, Texture2D> ImportTextures()
        {
            var textures = new Dictionary<string, Texture2D>();
            foreach (string kind in TextureKinds)
            {
                string image = $"{Folder}/Textures/K7_{kind}.png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(image);
                importer.textureType = kind == NormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = kind == BaseColorMap;
                importer.alphaSource = kind == MetallicSmoothnessMap ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
                textures.Add(kind, AssetDatabase.LoadAssetAtPath<Texture2D>(image));
            }

            return textures;
        }

        private static Texture2D Mask(Dictionary<string, Texture2D> textures)
        {
            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath);
            if (mask)
            {
                return mask;
            }

            Color32[] metal = ReadPixels(textures[MetallicSmoothnessMap], MaskSize);
            Color32[] occlusion = ReadPixels(textures[OcclusionMap], MaskSize);
            for (int i = 0; i < metal.Length; i++)
            {
                metal[i] = new Color32(metal[i].r, occlusion[i].r, DetailMaskOn, metal[i].a);
            }

            mask = new Texture2D(MaskSize, MaskSize, TextureFormat.RGBA32, true, true) { name = "K-7 HDRP mask" };
            mask.SetPixels32(metal);
            mask.Apply(true, false);
            EditorUtility.CompressTexture(mask, TextureFormat.BC7, TextureCompressionQuality.Normal);
            AssetDatabase.CreateAsset(mask, MaskPath);
            return mask;
        }

        private static Material SetUpMaterial(string slot, bool isStatusLight, Dictionary<string, Texture2D> textures, Texture2D mask)
        {
            string path = $"{Folder}/{slot}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find(LitShader)) { name = slot };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", isStatusLight ? StatusBaseColor : Color.white);
            material.SetFloat("_ReceivesSSR", 0f);
            if (isStatusLight)
            {
                material.SetColor("_EmissiveColor", StatusGlow);
                material.SetFloat("_EmissiveExposureWeight", EmissiveExposureWeight);
            }
            else
            {
                material.SetTexture("_BaseColorMap", textures[BaseColorMap]);
                material.SetTexture("_NormalMap", textures[NormalMap]);
                material.SetFloat("_NormalScale", NormalScale);
                material.SetTexture("_MaskMap", mask);
                material.SetFloat("_AORemapMin", OcclusionFloor);
                material.SetFloat("_SmoothnessRemapMax", SmoothnessCeiling);
                material.SetFloat("_MetallicRemapMax", MetallicCeiling);
            }

            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Removes the collision helpers and the animator (K7Patrol turns the
        // bones itself), sets up skinning and groups the four LOD levels.
        private static void Prepare(GameObject instance)
        {
            foreach (Transform part in instance.GetComponentsInChildren<Transform>(true))
            {
                if (part && part.name.StartsWith(CollisionPrefix))
                {
                    Object.DestroyImmediate(part.gameObject);
                }
            }

            foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
            {
                Object.DestroyImmediate(animator);
            }

            SkinnedMeshRenderer[] renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                renderer.quality = SkinQuality.Bone1;
                renderer.localBounds = SkinBounds;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            var lods = new LOD[LodHeights.Length];
            for (int i = 0; i < LodHeights.Length; i++)
            {
                int level = i;
                SkinnedMeshRenderer[] members = renderers.Where(r => LodIndex(r.name) == level).ToArray();
                if (members.Length != PartsPerLod)
                {
                    throw new InvalidDataException($"K-7 LOD {i} has {members.Length} meshes.");
                }

                lods[i] = new LOD(LodHeights[i], members);
            }

            LODGroup group = instance.GetComponent<LODGroup>();
            if (!group)
            {
                group = instance.AddComponent<LODGroup>();
            }

            group.SetLODs(lods);
            group.RecalculateBounds();
        }

        private static int LodIndex(string name)
        {
            return name.EndsWith("_LOD1") ? 1 : name.EndsWith("_LOD2") ? 2 : name.EndsWith("_LOD3") ? 3 : 0;
        }

        private static Color32[] ReadPixels(Texture2D input, int size)
        {
            RenderTexture target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            var copy = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            try
            {
                Graphics.Blit(input, target);
                RenderTexture.active = target;
                copy.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                return copy.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(copy);
            }
        }
    }
}
