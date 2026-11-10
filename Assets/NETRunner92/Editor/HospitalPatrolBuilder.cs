using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NETRunner92.Editor
{
    public static partial class HospitalSceneBuilder
    {
        const string K7Root = Root + "/Environment/K7";

        static GameObject BuildPatrolPrefab()
        {
            string path = K7Root + "/K7_Robot.fbx";
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(path);
            if (modelImporter == null) throw new FileNotFoundException("Copy the original K-7 source FBX into Assets.", path);
            var textures = new Dictionary<string, Texture2D>();
            foreach (string kind in new[] { "BaseColor", "Normal", "MetallicSmoothness", "AO" })
            {
                string image = $"{K7Root}/Textures/K7_{kind}.png";
                var t = (TextureImporter)AssetImporter.GetAtPath(image);
                t.textureType = kind == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                t.sRGBTexture = kind == "BaseColor";
                t.alphaSource = kind == "MetallicSmoothness" ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                t.wrapMode = TextureWrapMode.Clamp;
                t.SaveAndReimport();
                textures.Add(kind, AssetDatabase.LoadAssetAtPath<Texture2D>(image));
            }
            const int size = 2048;
            string maskPath = K7Root + "/K7_HDRP_Mask.asset";
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
            if (!mask)
            {
                Color32[] metal = ReadPixels(textures["MetallicSmoothness"], size);
                Color32[] occlusion = ReadPixels(textures["AO"], size);
                for (int i = 0; i < metal.Length; i++)
                    metal[i] = new Color32(metal[i].r, occlusion[i].r, 255, metal[i].a);
                mask = new Texture2D(size, size, TextureFormat.RGBA32, true, true) { name = "K-7 HDRP mask" };
                mask.SetPixels32(metal);
                mask.Apply(true, false);
                EditorUtility.CompressTexture(mask, TextureFormat.BC7, TextureCompressionQuality.Normal);
                AssetDatabase.CreateAsset(mask, maskPath);
            }
            foreach (string kind in new[] { "Armor", "Internal", "Metal", "Rubber", "SensorGlass", "Cables", "StatusLight" })
            {
                bool status = kind == "StatusLight";
                string slot = "MAT_K7_" + kind;
                string materialPath = $"{K7Root}/{slot}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material)
                {
                    material = new Material(Shader.Find("HDRP/Lit")) { name = slot };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.SetColor("_BaseColor", status ? new Color(.50f, .68f, .72f) : Color.white);
                material.SetFloat("_ReceivesSSR", 0f);
                if (status)
                {
                    material.SetColor("_EmissiveColor", new Color(.53f, .79f, 1f) * 4.5f);
                    material.SetFloat("_EmissiveExposureWeight", .8f);
                }
                else
                {
                    material.SetTexture("_BaseColorMap", textures["BaseColor"]);
                    material.SetTexture("_NormalMap", textures["Normal"]);
                    material.SetFloat("_NormalScale", .65f);
                    material.SetTexture("_MaskMap", mask);
                    material.SetFloat("_AORemapMin", .28f);
                    material.SetFloat("_SmoothnessRemapMax", .70f);
                    material.SetFloat("_MetallicRemapMax", .85f);
                }
                HDMaterial.ValidateMaterial(material);
                EditorUtility.SetDirty(material);
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), slot), material);
            }
            modelImporter.useFileScale = true;
            modelImporter.bakeAxisConversion = true;
            modelImporter.preserveHierarchy = true;
            modelImporter.animationType = ModelImporterAnimationType.Generic;
            modelImporter.importAnimation = false;
            modelImporter.importNormals = ModelImporterNormals.Import;
            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
            modelImporter.SaveAndReimport();

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            try
            {
                foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                    if (t && t.name.StartsWith("COL_")) UnityEngine.Object.DestroyImmediate(t.gameObject);
                foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(animator);
                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var r in renderers)
                {
                    r.quality = SkinQuality.Bone1;
                    r.localBounds = new Bounds(Vector3.up, new Vector3(2.6f, 2.8f, 2.6f));
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
                var lods = new LOD[4];
                float[] sizes = { .42f, .22f, .095f, .037f };
                for (int i = 0; i < sizes.Length; i++)
                {
                    int level = i;
                    var members = renderers.Where(r => LodIndex(r.name) == level).ToArray();
                    if (members.Length != 12) throw new InvalidDataException($"K-7 LOD {i} has {members.Length} meshes.");
                    lods[i] = new LOD(sizes[i], members);
                }
                var group = instance.GetComponent<LODGroup>() ?? instance.AddComponent<LODGroup>();
                group.SetLODs(lods);
                group.RecalculateBounds();
                return PrefabUtility.SaveAsPrefabAsset(instance, K7Root + "/K7_Patrol.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        static int LodIndex(string name) => name.EndsWith("_LOD1") ? 1 : name.EndsWith("_LOD2") ? 2 : name.EndsWith("_LOD3") ? 3 : 0;

        static Color32[] ReadPixels(Texture2D input, int size)
        {
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
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
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        static void AddK7Patrols(Transform systems, GameObject prefab)
        {
            var group = Node("K-7 autonomous patrols", systems);
            Vector2[] routes = { new(-26, -6), new(-18, 17), new(5, 27), new(-23, 6) };
            for (int i = 0; i < routes.Length; i++)
            {
                var robot = Node($"K-7 Patrol L{i + 1:00}", group);
                var model = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, robot)).transform;
                float y = i * StairAccess.FloorHeight + .015f;
                float z = i % 2 == 0 ? .72f : -.72f;
                robot.gameObject.AddComponent<K7Patrol>().Configure(model,
                    new[] { new Vector3(routes[i].x, y, z), new Vector3(routes[i].y, y, z) }, 1.03f + i * .055f);
            }
        }

        static void AddResidualPower(Transform systems, MeshRenderer[] geometry)
        {
            var root = Node("Residual power", systems);
            var fixtures = GameObject.Find("Hospital practicals").GetComponentsInChildren<Light>()
                .Where(l => l.name.Contains("_Strip_Concourse_")).ToArray();
            var wanted = new[] { new Vector3(-16, 3.5f, 0), new Vector3(19, 7.7f, 0),
                new Vector3(-14, 11.9f, 0), new Vector3(16, 16.1f, 0) };
            for (int i = 0; i < wanted.Length; i++)
            {
                var lamp = fixtures.OrderBy(l => (l.transform.position - wanted[i]).sqrMagnitude).First();
                var other = lamp.GetComponent<PracticalPulse>();
                if (other) UnityEngine.Object.DestroyImmediate(other);
                var screen = geometry.Where(r => r.name.Contains("_Strip_Concourse_"))
                    .OrderBy(r => (r.transform.position - lamp.transform.position).sqrMagnitude).FirstOrDefault();
                int slot = EmissiveSlot(screen);
                Node($"Failing practical L{i + 1:00}", root).gameObject.AddComponent<ResidualPower>()
                    .Configure(slot >= 0 ? screen : null, slot, lamp, i * 2.23f);
            }
            var readouts = geometry.Where(r => r.name.EndsWith("_Concourse_Index_Readout")
                || r.name.EndsWith("_Transfer_Console_Display")).OrderBy(r => r.name).ToArray();
            for (int i = 0; i < readouts.Length; i++)
            {
                var readout = readouts[i];
                int slot = EmissiveSlot(readout);
                if (slot >= 0)
                    Node(readout.name, root).gameObject.AddComponent<ResidualPower>().Configure(readout, slot, null, i * 1.74f);
            }
        }

        static int EmissiveSlot(MeshRenderer r) => r ? Array.FindIndex(r.sharedMaterials, m =>
            m && m.HasProperty("_EmissiveColor") && m.GetColor("_EmissiveColor").maxColorComponent > .04f) : -1;
    }
}
