using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NETRunner92.Editor
{
    public static partial class LatchViewmodelBuilder
    {
        const string Folder = "Assets/NETRunner92/Player/Latch07";
        const string ModelPath = Folder + "/NETRunner_FP_Arms.fbx";
        const string PrefabPath = Folder + "/Latch07_Viewmodel.prefab";
        const string TestScene = "Assets/NETRunner92/Scenes/LATCH07_ViewmodelCheck.unity";
        const string Report = "Northline/Renders/Phase_06_Latch07";
        static readonly string[] Clips = { "Hack_Equip", "Hack_Idle", "Hack_Interact", "Hack_Success",
            "Hack_Failed", "Hack_Unequip", "Hack_Cancel" };
        static readonly float[] Durations = { .5f, 2f, .8f, .8f, .6f, .4f, .25f };

        [MenuItem("NET.runner-92/LATCH-07/Prepare Import Check")]
        public static void Prepare()
        {
            var model = Import(out var motions, out var controller);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("First-person import camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 1.65f, 0);
            camera.fieldOfView = 50.53f;
            camera.nearClipPlane = .016f;
            camera.farClipPlane = 25f;
            var lamp = new GameObject("Soft frontal key").AddComponent<Light>();
            lamp.type = LightType.Directional;
            lamp.transform.rotation = Quaternion.Euler(39, 175, 0);
            lamp.intensity = 700f;
            lamp.color = new Color(.82f, .92f, 1f);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, camera.transform);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "LATCH-07 viewmodel";
            var animator = instance.GetComponentInChildren<Animator>(true);
            if (!animator) animator = instance.AddComponent<Animator>();
            if (!animator.avatar || !animator.avatar.isValid)
            {
                string avatarPath = Folder + "/Latch07_Avatar.asset";
                var avatar = AvatarBuilder.BuildGenericAvatar(animator.gameObject, "FP_Root");
                Require(avatar && avatar.isValid, "Could not build the FP_Root Generic avatar.");
                var saved = AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
                if (!saved)
                {
                    AssetDatabase.CreateAsset(avatar, avatarPath);
                    saved = avatar;
                }
                else
                {
                    EditorUtility.CopySerialized(avatar, saved);
                    UnityEngine.Object.DestroyImmediate(avatar);
                }
                animator.avatar = saved;
            }
            Require(animator && animator.avatar && animator.avatar.isValid && !animator.avatar.isHuman,
                "FBX did not import a valid Generic avatar.");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(renderers.Length == 9, $"Expected nine skinned assemblies, found {renderers.Length}.");
            foreach (var renderer in renderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
                renderer.updateWhenOffscreen = true;
            }
            var screen = renderers.Single(r => r.name == "NR92_Display_Surface");
            var rows = new List<string> { "PASS Generic avatar and nine skinned assemblies." };
            using (var geometry = new DisplayProbe(screen))
            {
                motions["Hack_Idle"].SampleAnimation(animator.gameObject, 1f);
                float depth = camera.transform.InverseTransformPoint(geometry.Center()).z;
                if (depth < 0f) instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                for (int i = 0; i < Clips.Length; i++)
                {
                    var clip = motions[Clips[i]];
                    Require(Mathf.Abs(clip.length - Durations[i]) <= .025f, $"{clip.name} is {clip.length:F3} s.");
                    Require(clip.isLooping == (clip.name == "Hack_Idle"), $"Unexpected loop on {clip.name}.");
                    clip.SampleAnimation(animator.gameObject, clip.length * .45f);
                    rows.Add(FormattableString.Invariant($"PASS {clip.name}: {clip.length:F3} s at {clip.frameRate:F0} Hz."));
                }
                motions["Hack_Idle"].SampleAnimation(animator.gameObject, 1f);
                float[] edges = geometry.Edges();
                Require(Mathf.Abs(edges[0] - .076f) < .002f && Mathf.Abs(edges[^1] - .150f) < .002f,
                    $"Screen edges {string.Join(", ", edges)} are not the expected metre scale.");
                Require(geometry.InFrame(camera, 16f / 9f), $"Ready screen is outside the 16:9 reference frame at {geometry.Center()}.");
                Require(geometry.InFrame(camera, 4f / 3f, 68f), "Ready screen is outside the 4:3 hospital framing.");
                Require(geometry.FaceCamera(camera) > .55f, "Screen normals face away from the camera.");
                rows.Add(FormattableString.Invariant($"PASS Screen {edges[^1]*1000:F1} × {edges[0]*1000:F1} mm, front visible at 16:9 reference and 4:3 hospital framing."));
                rows.Add($"PASS Eye-relative model orientation is {instance.transform.localEulerAngles}.");
                var uv = screen.sharedMesh.uv;
                foreach (var corner in new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up })
                    Require(uv.Any(p => (p - corner).sqrMagnitude < .00001f), $"Screen is missing UV corner {corner}.");
                rows.Add("PASS Four distinct 0–1 screen UV corners.");
            }
            // Save the untouched bind hierarchy as the reusable prefab, then pose the review instance.
            var source = (GameObject)PrefabUtility.InstantiatePrefab(model);
            var importTransforms = source.GetComponentsInChildren<Transform>(true).ToDictionary(t => t.name);
            foreach (var t in instance.GetComponentsInChildren<Transform>(true).Skip(1))
            {
                var reference = importTransforms[t.name];
                t.SetLocalPositionAndRotation(reference.localPosition, reference.localRotation);
                t.localScale = reference.localScale;
            }
            UnityEngine.Object.DestroyImmediate(source);
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Require(prefab, "Failed to save the viewmodel prefab.");
            animator.enabled = false;
            motions["Hack_Idle"].SampleAnimation(animator.gameObject, 1f);
            EditorSceneManager.SaveScene(scene, TestScene);
            Directory.CreateDirectory(Report);
            File.WriteAllLines(Path.Combine(Report, "import_checks.txt"), rows);
            foreach (var row in rows) Debug.Log("LATCH07 | " + row);
            AssetDatabase.SaveAssets();
        }

        static GameObject Import(out Dictionary<string, AnimationClip> motions, out AnimatorController controller)
        {
            AssetDatabase.Refresh();
            var materials = Materials().ToArray();
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            Require(importer, "Missing LATCH-07 FBX.");
            importer.useFileScale = true;
            importer.globalScale = 1f;
            importer.bakeAxisConversion = true;
            importer.preserveHierarchy = true;
            importer.optimizeGameObjects = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var material in materials)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), material);
            importer.SaveAndReimport();
            var takes = importer.defaultClipAnimations;
            Require(takes.Length == Clips.Length, $"Expected seven animation takes, found {takes.Length}.");
            foreach (var take in takes)
            {
                string name = Clips.SingleOrDefault(n => take.takeName.EndsWith(n, StringComparison.Ordinal));
                Require(name != null, "Unknown FBX take: " + take.takeName);
                take.name = name;
                take.loopTime = name == "Hack_Idle";
                take.lockRootRotation = true;
                take.lockRootHeightY = true;
                take.lockRootPositionXZ = true;
            }
            importer.clipAnimations = takes;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            motions = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(clip => Clips.Contains(clip.name)).ToDictionary(clip => clip.name);
            Require(motions.Count == Clips.Length, "Could not load all seven imported clips.");
            string controllerPath = Folder + "/Latch07.controller";
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            for (int i = 0; i < Clips.Length; i++)
            {
                string name = Clips[i];
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name)
                    ?? machine.AddState(name, new Vector3(270 * (i % 3), 80 + 90 * (i / 3)));
                state.motion = motions[name];
                if (i == 0) machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        }

        static IEnumerable<Material> Materials()
        {
            foreach (var type in new[] { "Fabric", "Glove", "Grip", "Titanium", "Ceramic" })
            {
                var mat = MaterialAsset("NR92_" + type);
                mat.SetTexture("_BaseColorMap", ConfigureTexture(type + "_BaseColor", true, false));
                mat.SetTexture("_NormalMap", ConfigureTexture(type + "_Normal", false, true));
                mat.SetTexture("_MaskMap", Mask(type));
                mat.SetFloat("_NormalScale", type == "Titanium" ? .34f : .43f);
                HDMaterial.ValidateMaterial(mat);
                yield return mat;
            }
            yield return Solid("NR92_Display_Surface", new Color(.013f,.035f,.044f), .40f);
            yield return Solid("NR92_Thread", new Color(.045f,.057f,.065f), .19f);
            yield return Solid("NR92_Satin_Steel", new Color(.16f,.21f,.24f), .67f, .68f);
            yield return Solid("NR92_Ceramic_Orange", new Color(.48f,.122f,.024f), .49f, .15f);
            yield return Solid("NR92_Etched_Label", new Color(.36f,.45f,.48f), .39f, .2f);
            yield return Solid("NR92_Status_Cyan", new Color(.075f,.66f,.68f), .69f, .08f, 2f);
            yield return Solid("NR92_Standby_Amber", new Color(.67f,.23f,.04f), .52f, .1f, 1.35f);
        }

        static Material MaterialAsset(string name)
        {
            string path = $"{Folder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find("HDRP/Lit")) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_ReceivesSSR", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Solid(string name, Color color, float smooth, float metallic = 0f, float emission = 0f)
        {
            var mat = MaterialAsset(name);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smooth);
            mat.SetFloat("_Metallic", metallic);
            mat.SetColor("_EmissiveColor", color * emission);
            HDMaterial.ValidateMaterial(mat);
            return mat;
        }

        static Texture2D ConfigureTexture(string name, bool srgb, bool normal)
        {
            string path = $"{Folder}/Textures/{name}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = srgb;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D Mask(string type)
        {
            // Decode source PNG bytes directly to preserve their linear mask channels.
            var rough = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var metal = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            rough.LoadImage(File.ReadAllBytes($"{Folder}/Textures/{type}_Roughness.png"));
            metal.LoadImage(File.ReadAllBytes($"{Folder}/Textures/{type}_Metallic.png"));
            var pixels = metal.GetPixels32();
            var roughness = rough.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(pixels[i].r, 255, 255, (byte)(255 - roughness[i].r));
            string path = $"{Folder}/Textures/{type}_HDRP_Mask.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var packed = new Texture2D(rough.width, rough.height, TextureFormat.RGBA32, true, true) { name = type + "_HDRP_Mask" };
            packed.SetPixels32(pixels);
            packed.Apply();
            EditorUtility.CompressTexture(packed, TextureFormat.BC7, TextureCompressionQuality.Normal);
            if (!existing) AssetDatabase.CreateAsset(packed, path);
            else
            {
                EditorUtility.CopySerialized(packed, existing);
                UnityEngine.Object.DestroyImmediate(packed);
                EditorUtility.SetDirty(existing);
            }
            UnityEngine.Object.DestroyImmediate(rough);
            UnityEngine.Object.DestroyImmediate(metal);
            return existing ? existing : packed;
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException("LATCH-07: " + message);
        }

        sealed class DisplayProbe : IDisposable
        {
            readonly SkinnedMeshRenderer renderer;
            readonly Mesh mesh = new();
            public DisplayProbe(SkinnedMeshRenderer renderer) => this.renderer = renderer;
            Vector3[] Points()
            {
                renderer.BakeMesh(mesh);
                return mesh.vertices.Select(renderer.transform.TransformPoint).ToArray();
            }
            public Vector3 Center() => Points().Aggregate(Vector3.zero, (sum, p) => sum + p) / mesh.vertexCount;
            public float[] Edges()
            {
                var p = Points();
                var uv = renderer.sharedMesh.uv;
                Vector3 At(Vector2 value) => p[Array.FindIndex(uv, v => (v - value).sqrMagnitude < .00001f)];
                return new[] { Vector3.Distance(At(Vector2.zero), At(Vector2.up)), Vector3.Distance(At(Vector2.zero), At(Vector2.right)) }
                    .OrderBy(x => x).ToArray();
            }
            public bool InFrame(Camera camera, float aspect, float verticalFov = 50.53f)
            {
                float limit = Mathf.Tan(verticalFov * Mathf.Deg2Rad * .5f);
                return Points().Select(camera.transform.InverseTransformPoint)
                    .All(p => p.z > camera.nearClipPlane && Mathf.Abs(p.x) < p.z * limit * aspect && Mathf.Abs(p.y) < p.z * limit);
            }
            public float FaceCamera(Camera camera)
            {
                Vector3 center = Center();
                return Vector3.Dot(renderer.transform.TransformDirection(mesh.normals[0]).normalized,
                    (camera.transform.position - center).normalized);
            }
            public void Dispose() => UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
}
