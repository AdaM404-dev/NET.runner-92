using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hexacorp.K7.Editor
{
    /// <summary>Explicit menu action: no automatic changes to other project assets.</summary>
    public static class K7AssetSetup
    {
        private static readonly string[] Kinds = { "Armor", "Internal", "Metal", "Rubber", "SensorGlass", "Cables", "StatusLight" };

        [MenuItem("Tools/K7/Build Prefab from Selected FBX")]
        public static void Build()
        {
            string fbx = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!fbx.EndsWith("/FBX/K7_Robot.fbx", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("Select FBX/K7_Robot.fbx in the Project window, then run Tools > K7 > Build Prefab from Selected FBX.");
                return;
            }
            string root = fbx.Substring(0, fbx.Length - "/FBX/K7_Robot.fbx".Length);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) { Debug.LogError("K7 setup supports URP Lit or Built-in Standard. Configure HDRP materials manually."); return; }
            bool urp = shader.name == "Universal Render Pipeline/Lit";
            string materialDir = root + "/Materials";
            Directory.CreateDirectory(materialDir);
            Directory.CreateDirectory(root + "/Prefabs");
            AssetDatabase.Refresh();

            var textures = new Dictionary<string, Texture2D>();
            foreach (string map in new[] { "BaseColor", "Normal", "Metallic", "Roughness", "AO", "MetallicSmoothness" })
            {
                string path = root + "/Textures/4K/K7_" + map + ".png";
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) { Debug.LogError("Missing texture: " + path); return; }
                ti.textureType = map == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = map == "BaseColor";
                ti.maxTextureSize = 4096;
                ti.mipmapEnabled = true;
                ti.alphaSource = map == "MetallicSmoothness" ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
                textures[map] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            var materials = new Dictionary<string, Material>();
            foreach (string kind in Kinds)
            {
                string name = "MAT_K7_" + kind;
                string path = materialDir + "/" + name + ".mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(shader) { name = name }; AssetDatabase.CreateAsset(mat, path); }
                mat.shader = shader;
                if (kind == "StatusLight")
                {
                    mat.SetColor(urp ? "_BaseColor" : "_Color", Color.white * .8f);
                    mat.SetTexture(urp ? "_BaseMap" : "_MainTex", null);
                    mat.SetFloat("_Metallic", .05f);
                    mat.SetFloat(urp ? "_Smoothness" : "_Glossiness", .75f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1f, .012f, .007f) * 5f);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                else
                {
                    mat.SetColor(urp ? "_BaseColor" : "_Color", Color.white);
                    mat.SetTexture(urp ? "_BaseMap" : "_MainTex", textures["BaseColor"]);
                    mat.SetTexture("_BumpMap", textures["Normal"]);
                    mat.SetFloat("_BumpScale", .6f);
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_MetallicGlossMap", textures["MetallicSmoothness"]);
                    mat.EnableKeyword(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
                    mat.SetFloat("_Metallic", 1f);
                    mat.SetFloat(urp ? "_Smoothness" : "_GlossMapScale", 1f);
                    mat.SetTexture("_OcclusionMap", textures["AO"]);
                    mat.SetFloat("_OcclusionStrength", .7f);
                    if (urp) mat.EnableKeyword("_OCCLUSIONMAP");
                }
                EditorUtility.SetDirty(mat);
                materials[name] = mat;
            }

            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.preserveHierarchy = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.motionNodeName = "Root";
            importer.optimizeGameObjects = false;
            importer.importAnimation = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var pair in materials)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);
            importer.SaveAndReimport();

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "K7_Industrial_Robot";
            try
            {
                foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                {
                    var slots = r.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                        if (slots[i] != null && materials.TryGetValue(slots[i].name, out var mapped)) slots[i] = mapped;
                    r.sharedMaterials = slots;
                    if (r is SkinnedMeshRenderer skinned)
                    {
                        skinned.quality = SkinQuality.Bone1;
                        // Conservative bounds for reach, crouch, head scanning and leg lift.
                        skinned.localBounds = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(2.6f, 2.8f, 2.6f));
                        skinned.updateWhenOffscreen = false;
                    }
                }
                foreach (Transform t in instance.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("COL_", StringComparison.Ordinal)) continue;
                    var filter = t.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    Bounds b = filter.sharedMesh.bounds;
                    var box = t.gameObject.AddComponent<BoxCollider>();
                    box.center = b.center; box.size = b.size; box.isTrigger = true;
                    var renderer = t.GetComponent<MeshRenderer>();
                    if (renderer != null) UnityEngine.Object.DestroyImmediate(renderer);
                    UnityEngine.Object.DestroyImmediate(filter);
                }
                var group = instance.GetComponent<LODGroup>() ?? instance.AddComponent<LODGroup>();
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var lods = new LOD[4]; float[] thresholds = { .55f, .28f, .12f, .035f };
                for (int i = 0; i < 4; i++)
                {
                    int level = i;
                    var members = renderers.Where(r => Level(r.name) == level).ToArray();
                    if (members.Length == 0) throw new InvalidOperationException("No renderers for K7 LOD" + i);
                    lods[i] = new LOD(thresholds[i], members);
                }
                group.fadeMode = LODFadeMode.None;
                group.SetLODs(lods); group.RecalculateBounds();
                var robot = instance.GetComponent<K7Robot>() ?? instance.AddComponent<K7Robot>();
                robot.BindSockets();
                if (robot.visionOrigin == null || robot.hackPoint == null) throw new InvalidOperationException("Required K7 sockets missing.");
                string prefabPath = root + "/Prefabs/K7_Industrial_Robot.prefab";
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                ConfigureDiagnostic(root, asset.GetComponent<Animator>()?.avatar);
                AssetDatabase.SaveAssets();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Debug.Log("K7 prefab created. Validate 2 m scale, +Z facing, diagnostic animation, LOD transitions and emission in your target Unity project.");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static int Level(string name)
        {
            if (name.StartsWith("COL_", StringComparison.Ordinal)) return -1;
            for (int i = 1; i <= 3; i++)
                if (name == "K7_LOD" + i || name.EndsWith("_LOD" + i, StringComparison.Ordinal)) return i;
            return 0;
        }

        private static void ConfigureDiagnostic(string root, Avatar avatar)
        {
            var importer = AssetImporter.GetAtPath(root + "/FBX/K7_ArticulationCheck.fbx") as ModelImporter;
            if (importer == null) return;
            importer.globalScale = 1f; importer.useFileScale = true; importer.bakeAxisConversion = true;
            importer.preserveHierarchy = true;
            importer.animationType = ModelImporterAnimationType.Generic; importer.optimizeGameObjects = false;
            importer.importAnimation = true; importer.motionNodeName = "Root";
            if (avatar != null) { importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; importer.sourceAvatar = avatar; }
            importer.SaveAndReimport();
        }
    }
}
