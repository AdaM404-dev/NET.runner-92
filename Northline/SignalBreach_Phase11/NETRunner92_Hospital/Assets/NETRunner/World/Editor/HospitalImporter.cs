using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetRunner.Atmosphere.Editor;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Imports one part of the Northline hospital (the building or the
    /// grounds) from its exported <c>SceneSource.json</c> and
    /// <c>MeshData.bytes</c>: the meshes into mesh assets, the materials as
    /// HDRP Lit with generated wear textures, each object under its floor and
    /// group, its collider under a separate renderer-free tree, and its light
    /// fixtures. One importer is used for both parts, so they share materials.
    /// </summary>
    public sealed class HospitalImporter
    {
        /// <summary>A mesh and the materials of its submeshes.</summary>
        private sealed class MeshEntry
        {
            public MeshEntry(Mesh mesh, Material[] surfaces)
            {
                Mesh = mesh;
                Surfaces = surfaces;
            }

            public Mesh Mesh { get; }

            public Material[] Surfaces { get; }
        }

        private const string SourceFile = "/SceneSource.json";
        private const string MeshFile = "/MeshData.bytes";
        private const string TextureFolder = HospitalAssets.Environment + "/Textures/";
        private const string LitShader = "HDRP/Lit";
        private const string PlainSurface = "plain";
        private const string GritSurface = "grit";
        private const string RampMark = "_A";

        // MeshData.bytes starts with "NR92" and the mesh count; meshes are
        // saved 260 to an asset file.
        private const uint MeshFileMagic = 0x4e523932;
        private const int MeshesPerAsset = 260;
        private const int MaxShortIndex = 65535;

        private const int Floors = 5;
        private const int FineDetail = 1;
        private const int NoCollider = 0;
        private const int MeshShape = 1;
        private const int StairRamp = 3;

        // A stair ramp: a 1.97 m wide, 12 cm thick plank rising 2.10 m over
        // 3.19 m, centred 6 cm below the line from its foot to its head.
        private const float RampRise = 2.10f;
        private const float RampRun = 3.19f;
        private const float RampWidth = 1.97f;
        private const float RampThickness = .12f;
        private const float RampSink = .06f;
        private const float RampOverlap = .014f;
        private const float ThinnestBox = .025f;

        // Material response: clear coat at 65%, glow scaled for HDRP's
        // physical units, opaque from 99% and world-space wear textures.
        private const float CoatScale = .65f;
        private const float EmissionScale = 12f;
        private const float EmissiveExposureWeight = .8f;
        private const float OpaqueAlpha = .99f;
        private const float WorldSpaceUv = 5;
        private const float GritTiling = .50f;
        private const float WearTiling = .85f;

        // The wear textures: 128 × 128, light grey with soft broad blotches
        // (stronger on grit) and fine speckle; they tile seamlessly.
        private const int WearSize = 128;
        private const float WearBase = .70f;
        private const float GritBlotches = .26f;
        private const float WearBlotches = .16f;
        private const float WearSpeckle = .08f;

        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        /// <summary>How many drawn objects the imports placed.</summary>
        public int Visuals { get; private set; }

        /// <summary>How many colliders the imports placed.</summary>
        public int Solids { get; private set; }

        /// <summary>How many stair ramps the imports placed.</summary>
        public int Ramps { get; private set; }

        /// <summary>
        /// Imports the part <paramref name="label"/> (the folder under
        /// Environment): drawn objects under <paramref name="visible"/>,
        /// colliders under <paramref name="physics"/>, lights under
        /// <paramref name="atmosphere"/>.
        /// </summary>
        public void Import(string label, Transform visible, Transform physics, Transform atmosphere)
        {
            string folder = HospitalAssets.Environment + "/" + label;
            SceneSource source = JsonUtility.FromJson<SceneSource>(File.ReadAllText(folder + SourceFile));
            MeshEntry[] meshes = Meshes(folder, source.Materials.Select(MaterialFor).ToArray(), source.MeshCount);
            Transform root = SceneParts.Node(label, visible);
            Transform shapes = SceneParts.Node(label, physics);
            var groups = new Dictionary<(int, string), Transform>();
            var assemblies = new Dictionary<string, Transform>();
            var floors = new Transform[Floors];
            var collisionFloors = new Transform[Floors];
            for (int i = 0; i < Floors; i++)
            {
                floors[i] = SceneParts.Node(i == 0 ? "Grade and envelope" : $"Level {i:00}", root);
                collisionFloors[i] = SceneParts.Node(floors[i].name, shapes);
            }

            foreach (SourcePlacement placement in source.Objects)
            {
                MeshEntry mesh = meshes[placement.Mesh];
                if (placement.IsRendered)
                {
                    Draw(placement, mesh, Group(placement, floors, groups, assemblies));
                }

                if (placement.Collision == NoCollider)
                {
                    continue;
                }

                Collide(placement, mesh, SceneParts.Node(placement.Name, collisionFloors[placement.Floor]));
            }

            PracticalLights.Add(source.Lights, SceneParts.Node(label + " practicals", atmosphere));
        }

        private static Transform Group(SourcePlacement placement, Transform[] floors, Dictionary<(int, string), Transform> groups,
            Dictionary<string, Transform> assemblies)
        {
            (int, string) key = (placement.Floor, placement.Group);
            if (!groups.TryGetValue(key, out Transform group))
            {
                group = SceneParts.Node(placement.Group, floors[placement.Floor]);
                groups.Add(key, group);
            }

            if (string.IsNullOrEmpty(placement.Parent))
            {
                return group;
            }

            if (!assemblies.TryGetValue(placement.Parent, out Transform assembly))
            {
                assembly = SceneParts.Node(placement.Parent, group);
                assemblies.Add(placement.Parent, assembly);
            }

            return assembly;
        }

        private static void Place(Transform node, SourcePlacement placement)
        {
            node.SetPositionAndRotation(placement.Position, placement.Rotation);
            node.localScale = placement.Scale;
        }

        private void Draw(SourcePlacement placement, MeshEntry mesh, Transform group)
        {
            Transform item = SceneParts.Node(placement.Name, group);
            Place(item, placement);
            item.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.Mesh;
            MeshRenderer renderer = item.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = mesh.Surfaces;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            if (placement.Detail == FineDetail)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            Visuals++;
        }

        private void Collide(SourcePlacement placement, MeshEntry mesh, Transform collider)
        {
            if (placement.Collision == StairRamp)
            {
                // The ramp's foot is the placement's position; flights named "_A" run towards -z.
                Vector3 foot = placement.Position;
                Vector3 head = foot + new Vector3(0, RampRise, placement.Name.EndsWith(RampMark) ? -RampRun : RampRun);
                Quaternion rotation = Quaternion.LookRotation(head - foot);
                collider.SetPositionAndRotation((foot + head) * 0.5f - rotation * Vector3.up * RampSink, rotation);
                collider.gameObject.AddComponent<BoxCollider>().size =
                    new Vector3(RampWidth, RampThickness, Vector3.Distance(foot, head) + RampOverlap);
                Ramps++;
            }
            else
            {
                Place(collider, placement);
                if (placement.Collision == MeshShape)
                {
                    collider.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.Mesh;
                }
                else
                {
                    BoxCollider box = collider.gameObject.AddComponent<BoxCollider>();
                    box.center = mesh.Mesh.bounds.center;
                    box.size = Vector3.Max(mesh.Mesh.bounds.size, Vector3.one * ThinnestBox);
                }
            }

            Solids++;
        }

        // Reads every mesh from MeshData.bytes: per mesh its vertex count and
        // submesh count, the vertices (position, normal, uv), then each
        // submesh's material index and triangles. Rewrites the mesh assets.
        private static MeshEntry[] Meshes(string folder, Material[] surfaces, int count)
        {
            using (var reader = new BinaryReader(File.OpenRead(folder + MeshFile)))
            {
                if (reader.ReadUInt32() != MeshFileMagic || reader.ReadInt32() != count)
                {
                    throw new InvalidDataException(folder);
                }

                var list = new MeshEntry[count];
                AssetDatabase.StartAssetEditing();
                try
                {
                    string path = "";
                    for (int m = 0; m < count; m++)
                    {
                        if (m % MeshesPerAsset == 0)
                        {
                            path = $"{folder}/Meshes_{m / MeshesPerAsset:D2}.asset";
                            if (File.Exists(path))
                            {
                                AssetDatabase.DeleteAsset(path);
                            }
                        }

                        Mesh mesh = ReadMesh(reader, m, surfaces, out Material[] slots);
                        if (m % MeshesPerAsset == 0)
                        {
                            AssetDatabase.CreateAsset(mesh, path);
                        }
                        else
                        {
                            AssetDatabase.AddObjectToAsset(mesh, path);
                        }

                        list[m] = new MeshEntry(mesh, slots);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                if (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    throw new InvalidDataException(folder + " trailing geometry data");
                }

                return list;
            }
        }

        private static Mesh ReadMesh(BinaryReader reader, int index, Material[] surfaces, out Material[] slots)
        {
            int vertexCount = reader.ReadInt32();
            int submeshCount = reader.ReadInt32();
            var mesh = new Mesh { name = $"Northline_mesh_{index:D4}" };
            if (vertexCount > MaxShortIndex)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                uv[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.subMeshCount = submeshCount;
            slots = new Material[submeshCount];
            for (int s = 0; s < submeshCount; s++)
            {
                slots[s] = surfaces[reader.ReadInt32()];
                var triangles = new int[reader.ReadInt32()];
                for (int j = 0; j < triangles.Length; j++)
                {
                    triangles[j] = reader.ReadInt32();
                }

                mesh.SetTriangles(triangles, s, false);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private Material MaterialFor(SourceSurface surface)
        {
            if (materials.TryGetValue(surface.Name, out Material material))
            {
                return material;
            }

            string path = HospitalAssets.Materials + "/" + SceneParts.AssetName(surface.Name) + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find(LitShader));
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = surface.Name;
            material.SetColor("_BaseColor", surface.Color);
            material.SetFloat("_Metallic", surface.Metallic);
            material.SetFloat("_Smoothness", Mathf.Clamp01(1f - surface.Roughness));
            material.SetFloat("_CoatMask", surface.Coat * CoatScale);
            material.SetFloat("_DoubleSidedEnable", 1f);
            material.SetFloat("_SurfaceType", surface.Alpha < OpaqueAlpha ? 1f : 0f);
            material.SetColor("_EmissiveColor", surface.Emission * surface.EmissionStrength * EmissionScale);
            material.SetFloat("_EmissiveExposureWeight", EmissiveExposureWeight);
            material.enableInstancing = surface.Alpha >= OpaqueAlpha;
            if (surface.Surface != PlainSurface)
            {
                material.SetFloat("_UVBase", WorldSpaceUv);
                material.SetFloat("_TexWorldScale", surface.Surface == GritSurface ? GritTiling : WearTiling);
                material.SetTexture("_BaseColorMap", WearMap(surface.Surface));
            }

            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            return materials[surface.Name] = material;
        }

        private static Texture2D WearMap(string label)
        {
            string path = TextureFolder + label + ".asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing)
            {
                return existing;
            }

            var texture = new Texture2D(WearSize, WearSize, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat };
            var colors = new Color[WearSize * WearSize];
            float blotches = label == GritSurface ? GritBlotches : WearBlotches;
            for (int j = 0; j < WearSize; j++)
            {
                for (int i = 0; i < WearSize; i++)
                {
                    float grey = Wear(Mathf.PI * 2 * i / WearSize, Mathf.PI * 2 * j / WearSize, blotches);
                    colors[j * WearSize + i] = new Color(grey, grey, grey);
                }
            }

            texture.SetPixels(colors);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        // Perlin noise sampled along closed loops of the angles x and y, so the
        // texture repeats without a seam; the numbers only shape the pattern.
        private static float Wear(float x, float y, float blotches)
        {
            float broad = Mathf.PerlinNoise(3 + 1.7f * Mathf.Cos(x) + .7f * Mathf.Sin(y), 9 + .6f * Mathf.Sin(x) + 1.7f * Mathf.Cos(y));
            float speckle = Mathf.PerlinNoise(8 + Mathf.Cos(11 * x) + Mathf.Sin(9 * y), 4 + Mathf.Sin(10 * x) + Mathf.Cos(7 * y));
            return Mathf.Clamp01(WearBase + broad * blotches + speckle * WearSpeckle);
        }
    }
}
