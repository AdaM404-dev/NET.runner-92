using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using NetRunner.Player;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine;

namespace NetRunner.World.Editor
{
    public static partial class HospitalSceneBuilder
    {
        const string Root = "Assets/NETRunner92";
        public const string ScenePath = Root + "/Scenes/Hospital_Exploration.unity";
        const string RenderAssets = Root + "/Rendering";
        const string Atmos = Root + "/Atmosphere";
        [Serializable] class Source { public Placement[] objects; public Surface[] materials; public Fixture[] lights; public int meshCount; }
        [Serializable] class Placement
        {
            public string name, group, parent;
            public int floor, mesh, collision, detail;
            public bool render;
            public float[] position, rotation, scale;
        }
        [Serializable] class Surface
        {
            public string name, surface;
            public float[] color, emission;
            public float rough, metal, alpha, emissionStrength, coat;
        }
        [Serializable] class Fixture
        {
            public string name;
            public int floor;
            public float[] position, color, direction;
            public float energy, size;
        }
        sealed class MeshEntry { public Mesh mesh; public Material[] surfaces; }
        static readonly Dictionary<string, Material> materialCache = new();
        static int visuals, solids, ramps;

        [MenuItem("NET.runner-92/Build Hospital Exploration Scene")]
        public static void BuildScene()
        {
            materialCache.Clear();
            visuals = solids = ramps = 0;
            AssetDatabase.Refresh();
            ConfigurePipeline();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var model = Node("Northline Medical — imported environment");
            var collision = Node("Collision — renderer-free");
            var atmosphere = Node("Atmosphere");
            Import("Hospital", model, collision, atmosphere);
            Import("Exterior", model, collision, atmosphere);
            if (ramps != 12) throw new InvalidDataException($"Expected twelve stair ramps, imported {ramps}");
            AddAtmosphere(atmosphere, AddPlayer());
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            RefineReadability();
            OptimizeScene();
            InstallHospitalSystems();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LatchPrefab)) InstallHud();
            Debug.Log($"NET92 SCENE | {visuals} visual meshes | {solids} colliders | {ramps} stair ramps");
        }

        static void ConfigurePipeline()
        {
            string path = RenderAssets + "/Northline_HDRP.asset";
            var hdrp = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(path);
            if (!hdrp)
            {
                hdrp = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                AssetDatabase.CreateAsset(hdrp, path);
            }
            var settings = hdrp.currentPlatformRenderPipelineSettings;
            settings.supportSSR = true;
            settings.supportSSAO = true;
            settings.supportVolumetrics = true;
            settings.supportRayTracing = false;
            hdrp.currentPlatformRenderPipelineSettings = settings;
            GraphicsSettings.defaultRenderPipeline = hdrp;
            QualitySettings.renderPipeline = hdrp;
            QualitySettings.vSyncCount = 0;
            EditorUtility.SetDirty(hdrp);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.productName = "NET.runner-92 — Hospital Exploration";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            var playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettings.Length > 0)
            {
                var serialized = new SerializedObject(playerSettings[0]);
                var input = serialized.FindProperty("activeInputHandler");
                if (input != null)
                {
                    input.intValue = 1;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        static Transform Node(string name, Transform parent = null)
        {
            var n = new GameObject(name).transform;
            if (parent) n.SetParent(parent, false);
            return n;
        }
        static Vector3 V(float[] v) => new(v[0], v[1], v[2]);
        static Color C(float[] v, float alpha = 1f) => new(v[0], v[1], v[2], alpha);
        static string Safe(string name) => new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        static void Place(Transform t, Placement p)
        {
            t.SetPositionAndRotation(V(p.position), new Quaternion(p.rotation[0], p.rotation[1], p.rotation[2], p.rotation[3]));
            t.localScale = V(p.scale);
        }

        static void Import(string label, Transform visible, Transform physics, Transform atmosphere)
        {
            string path = Root + "/Environment/" + label;
            var source = JsonUtility.FromJson<Source>(File.ReadAllText(path + "/SceneSource.json"));
            var meshes = Meshes(path, source.materials.Select(MaterialFor).ToArray(), source.meshCount);
            var root = Node(label, visible);
            var shapes = Node(label, physics);
            var groups = new Dictionary<(int, string), Transform>();
            var parents = new Dictionary<string, Transform>();
            var floors = new Transform[5];
            var collisionFloors = new Transform[5];
            for (int i = 0; i < 5; i++)
            {
                floors[i] = Node(i == 0 ? "Grade and envelope" : $"Level {i:00}", root);
                collisionFloors[i] = Node(floors[i].name, shapes);
            }
            foreach (var p in source.objects)
            {
                var mesh = meshes[p.mesh];
                if (p.render)
                {
                    var key = (p.floor, p.group);
                    if (!groups.TryGetValue(key, out var group))
                    {
                        group = Node(p.group, floors[p.floor]);
                        groups.Add(key, group);
                    }
                    if (!string.IsNullOrEmpty(p.parent))
                    {
                        if (!parents.TryGetValue(p.parent, out var assembly))
                        {
                            assembly = Node(p.parent, group);
                            parents.Add(p.parent, assembly);
                        }
                        group = assembly;
                    }
                    var item = Node(p.name, group);
                    Place(item, p);
                    item.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.mesh;
                    var r = item.gameObject.AddComponent<MeshRenderer>();
                    r.sharedMaterials = mesh.surfaces;
                    r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                    r.lightProbeUsage = LightProbeUsage.Off;
                    if (p.detail == 1) r.shadowCastingMode = ShadowCastingMode.Off;
                    visuals++;
                }
                if (p.collision == 0) continue;
                var collider = Node(p.name, collisionFloors[p.floor]);
                if (p.collision == 3)
                {
                    var a = V(p.position);
                    var b = a + new Vector3(0, 2.10f, p.name.EndsWith("_A") ? -3.19f : 3.19f);
                    var rotation = Quaternion.LookRotation(b - a);
                    collider.SetPositionAndRotation((a + b) * 0.5f - rotation * Vector3.up * 0.06f, rotation);
                    collider.gameObject.AddComponent<BoxCollider>().size = new Vector3(1.97f, .12f, Vector3.Distance(a, b) + .014f);
                    ramps++;
                }
                else
                {
                    Place(collider, p);
                    if (p.collision == 1)
                        collider.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.mesh;
                    else
                    {
                        var box = collider.gameObject.AddComponent<BoxCollider>();
                        box.center = mesh.mesh.bounds.center;
                        box.size = Vector3.Max(mesh.mesh.bounds.size, Vector3.one * 0.025f);
                    }
                }
                solids++;
            }
            AddFixtures(source.lights, Node(label + " practicals", atmosphere));
        }

        static MeshEntry[] Meshes(string directory, Material[] materials, int count)
        {
            using var r = new BinaryReader(File.OpenRead(directory + "/MeshData.bytes"));
            if (r.ReadUInt32() != 0x4e523932 || r.ReadInt32() != count) throw new InvalidDataException(directory);
            var list = new MeshEntry[count];
            AssetDatabase.StartAssetEditing();
            try
            {
                string path = "";
                for (int m = 0; m < count; m++)
                {
                    if (m % 260 == 0)
                    {
                        path = $"{directory}/Meshes_{m / 260:D2}.asset";
                        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                    }
                    int n = r.ReadInt32(), subs = r.ReadInt32();
                    var mesh = new Mesh { name = $"Northline_mesh_{m:D4}" };
                    if (n > 65535) mesh.indexFormat = IndexFormat.UInt32;
                    var vertices = new Vector3[n];
                    var normals = new Vector3[n];
                    var uv = new Vector2[n];
                    for (int i = 0; i < n; i++)
                    {
                        vertices[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                        normals[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                        uv[i] = new Vector2(r.ReadSingle(), r.ReadSingle());
                    }
                    mesh.vertices = vertices;
                    mesh.normals = normals;
                    mesh.uv = uv;
                    mesh.subMeshCount = subs;
                    var slots = new Material[subs];
                    for (int s = 0; s < subs; s++)
                    {
                        slots[s] = materials[r.ReadInt32()];
                        var ids = new int[r.ReadInt32()];
                        for (int j = 0; j < ids.Length; j++) ids[j] = r.ReadInt32();
                        mesh.SetTriangles(ids, s, false);
                    }
                    mesh.RecalculateBounds();
                    if (m % 260 == 0) AssetDatabase.CreateAsset(mesh, path);
                    else AssetDatabase.AddObjectToAsset(mesh, path);
                    list[m] = new MeshEntry { mesh = mesh, surfaces = slots };
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            if (r.BaseStream.Position != r.BaseStream.Length) throw new InvalidDataException(directory + " trailing geometry data");
            return list;
        }

        static Material MaterialFor(Surface s)
        {
            if (materialCache.TryGetValue(s.name, out var material)) return material;
            string path = Root + "/Environment/Materials/" + Safe(s.name) + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("HDRP/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.name = s.name;
            material.SetColor("_BaseColor", C(s.color, s.alpha));
            material.SetFloat("_Metallic", s.metal);
            material.SetFloat("_Smoothness", Mathf.Clamp01(1f - s.rough));
            material.SetFloat("_CoatMask", s.coat * .65f);
            material.SetFloat("_DoubleSidedEnable", 1f);
            material.SetFloat("_SurfaceType", s.alpha < .99f ? 1f : 0f);
            material.SetColor("_EmissiveColor", C(s.emission) * s.emissionStrength * 12f);
            material.SetFloat("_EmissiveExposureWeight", .8f);
            material.enableInstancing = s.alpha >= .99f;
            if (s.surface != "plain")
            {
                material.SetFloat("_UVBase", 5);
                material.SetFloat("_TexWorldScale", s.surface == "grit" ? .50f : .85f);
                material.SetTexture("_BaseColorMap", WearMap(s.surface));
            }
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            return materialCache[s.name] = material;
        }

        static Texture2D WearMap(string label)
        {
            string path = Root + "/Environment/Textures/" + label + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing) return existing;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat };
            var colors = new Color[n * n];
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.PI * 2 * i / n, y = Mathf.PI * 2 * j / n;
                float broad = Mathf.PerlinNoise(3 + 1.7f * Mathf.Cos(x) + .7f * Mathf.Sin(y), 9 + .6f * Mathf.Sin(x) + 1.7f * Mathf.Cos(y));
                float detail = Mathf.PerlinNoise(8 + Mathf.Cos(11 * x) + Mathf.Sin(9 * y), 4 + Mathf.Sin(10 * x) + Mathf.Cos(7 * y));
                float g = Mathf.Clamp01(.70f + broad * (label == "grit" ? .26f : .16f) + detail * .08f);
                colors[j*n+i] = new Color(g, g, g);
            }
            tex.SetPixels(colors);
            tex.Apply();
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        static FirstPersonController AddPlayer()
        {
            var player = Node("PlayerController");
            player.position = new Vector3(-.55f, .03f, -36.45f);
            var controller = player.gameObject.AddComponent<CharacterController>();
            controller.height = 1.80f;
            controller.radius = .30f;
            controller.center = Vector3.up * .90f;
            controller.stepOffset = .35f;
            controller.slopeLimit = 50f;
            controller.skinWidth = .034f;
            controller.minMoveDistance = 0f;
            var eye = Node("First-person eyes", player);
            eye.localPosition = Vector3.up * 1.65f;
            eye.gameObject.tag = "MainCamera";
            var camera = eye.gameObject.AddComponent<Camera>();
            camera.fieldOfView = 68f;
            camera.nearClipPlane = .055f;
            camera.farClipPlane = 150f;
            eye.gameObject.AddComponent<AudioListener>();
            eye.gameObject.AddComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            var movement = player.gameObject.AddComponent<FirstPersonController>();
            movement.SetCamera(camera);
            PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject, Root + "/Player/Prefabs/PlayerController.prefab", InteractionMode.AutomatedAction);
            return movement;
        }

        public static void BuildWindows() => BuildWindows("../Builds/Phase_05_Optimized/NETRunner92_Hospital.exe");
        public static void BuildTraversalWindows()
        {
            OptimizeScene();
            BuildWindows("../Builds/Phase_05_Traversal/NETRunner92_Hospital.exe");
        }

        static void BuildWindows(string output, string[] scenes = null)
        {
            PlayerSettings.enableFrameTimingStats = true;
            string file = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes ?? new[] { ScenePath }, locationPathName = file,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException(result.summary.result.ToString());
            Debug.Log("NET92 WINDOWS BUILD | " + file + " | " + result.summary.totalSize);
        }
    }
}


