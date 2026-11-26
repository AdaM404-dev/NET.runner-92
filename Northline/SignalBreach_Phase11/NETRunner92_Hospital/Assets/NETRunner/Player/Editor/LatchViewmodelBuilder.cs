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
using UnityEngine.SceneManagement;

namespace NetRunner.Player.Editor
{
    /// <summary>
    /// Imports AdaM404's first-person arms with the LATCH-07 tablet
    /// (<c>NETRunner_FP_Arms.fbx</c>) and checks them: one Generic avatar,
    /// nine skinned parts, seven animation clips of the expected lengths, a
    /// 150 × 76 mm screen that the camera sees in both framings, and the HDRP
    /// materials with their packed mask maps. It saves the viewmodel prefab,
    /// a check scene and the list of checks. It rewrites assets in AdaM404's
    /// <c>Latch07</c> folder, so it is run only when the model changes.
    /// </summary>
    public static class LatchViewmodelBuilder
    {
        /// <summary>Measures the tablet's display mesh in the pose the animation gives it.</summary>
        private sealed class DisplayProbe : IDisposable
        {
            private readonly SkinnedMeshRenderer renderer;
            private readonly Mesh mesh = new Mesh();

            public DisplayProbe(SkinnedMeshRenderer renderer)
            {
                this.renderer = renderer;
            }

            /// <summary>The centre of the screen's vertices, in world space.</summary>
            public Vector3 Center()
            {
                return Points().Aggregate(Vector3.zero, (sum, p) => sum + p) / mesh.vertexCount;
            }

            /// <summary>The screen's two edge lengths in metres, shorter first.</summary>
            public float[] Edges()
            {
                Vector3[] points = Points();
                Vector2[] uv = renderer.sharedMesh.uv;
                Vector3 At(Vector2 value) => points[Array.FindIndex(uv, v => (v - value).sqrMagnitude < UvTolerance)];
                return new[] { Vector3.Distance(At(Vector2.zero), At(Vector2.up)), Vector3.Distance(At(Vector2.zero), At(Vector2.right)) }
                    .OrderBy(x => x).ToArray();
            }

            /// <summary>Whether the whole screen is inside the camera's view at this aspect and vertical field of view.</summary>
            public bool InFrame(Camera camera, float aspect, float verticalFov = ReferenceFieldOfView)
            {
                float limit = Mathf.Tan(verticalFov * Mathf.Deg2Rad * .5f);
                return Points().Select(camera.transform.InverseTransformPoint)
                    .All(p => p.z > camera.nearClipPlane && Mathf.Abs(p.x) < p.z * limit * aspect && Mathf.Abs(p.y) < p.z * limit);
            }

            /// <summary>How directly the screen faces the camera: 1 straight on, below 0 facing away.</summary>
            public float FaceCamera(Camera camera)
            {
                Vector3 center = Center();
                return Vector3.Dot(renderer.transform.TransformDirection(mesh.normals[0]).normalized,
                    (camera.transform.position - center).normalized);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }

            private Vector3[] Points()
            {
                renderer.BakeMesh(mesh);
                return mesh.vertices.Select(renderer.transform.TransformPoint).ToArray();
            }
        }

        private const string Folder = "Assets/NETRunner92/Player/Latch07";
        private const string ModelPath = Folder + "/NETRunner_FP_Arms.fbx";
        private const string PrefabPath = Folder + "/Latch07_Viewmodel.prefab";
        private const string AvatarPath = Folder + "/Latch07_Avatar.asset";
        private const string ControllerPath = Folder + "/Latch07.controller";
        private const string TestScene = "Assets/NETRunner92/Scenes/LATCH07_ViewmodelCheck.unity";
        private const string Report = "Northline/Renders/Phase_06_Latch07";
        private const string IdleClip = "Hack_Idle";
        private const string DisplayMesh = "NR92_Display_Surface";
        private const string RootBone = "FP_Root";
        private const string TitaniumPart = "Titanium";
        private const int SkinnedParts = 9;

        // The Blender reference camera, and the hospital's own field of view.
        private const float EyeHeight = 1.65f;
        private const float ReferenceFieldOfView = 50.53f;
        private const float HospitalFieldOfView = 68f;
        private const float ReferenceAspect = 16f / 9f;
        private const float HospitalAspect = 4f / 3f;
        private const float NearClip = .016f;
        private const float FarClip = 25f;
        private const float KeyLightIntensity = 700f;

        // When the poses are sampled: the idle clip at 1 s, the others at 45% of their length.
        private const float IdlePoseTime = 1f;
        private const float SamplePoint = .45f;

        // What the checks allow: clip lengths within 25 ms, the screen's edges
        // (76 and 150 mm) within 2 mm, and the screen facing the camera.
        private const float ClipTolerance = .025f;
        private const float ScreenShortEdge = .076f;
        private const float ScreenLongEdge = .150f;
        private const float ScreenTolerance = .002f;
        private const float MinimumFacing = .55f;
        private const float UvTolerance = .00001f;
        private const float MillimetresPerMetre = 1000f;

        // HDRP normal map strength: the titanium is smoother than the soft parts.
        private const float TitaniumNormalScale = .34f;
        private const float NormalScale = .43f;

        // Where the clips' states sit in the Animator window: three to a row.
        private const int GraphColumns = 3;
        private const float GraphColumnWidth = 270f;
        private const float GraphTop = 80f;
        private const float GraphRowHeight = 90f;

        // Any size: LoadImage resizes the texture to the image.
        private const int PlaceholderSize = 2;

        private static readonly string[] Clips =
        {
            "Hack_Equip", "Hack_Idle", "Hack_Interact", "Hack_Success", "Hack_Failed", "Hack_Unequip", "Hack_Cancel",
        };

        // Each clip's expected length in seconds, in the order of Clips.
        private static readonly float[] Durations = { .5f, 2f, .8f, .8f, .6f, .4f, .25f };

        private static readonly string[] TexturedParts = { "Fabric", "Glove", "Grip", TitaniumPart, "Ceramic" };
        private static readonly Quaternion KeyLightAngle = Quaternion.Euler(39f, 175f, 0f);
        private static readonly Color KeyLightColour = new Color(.82f, .92f, 1f);
        private static readonly Quaternion HalfTurn = Quaternion.Euler(0f, 180f, 0f);

        /// <summary>Imports and checks the arms, then saves the prefab, the check scene and the report.</summary>
        [MenuItem("NET.runner-92/LATCH-07/Prepare Import Check")]
        public static void Prepare()
        {
            GameObject model = Import(out Dictionary<string, AnimationClip> motions, out AnimatorController controller);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("First-person import camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, EyeHeight, 0f);
            camera.fieldOfView = ReferenceFieldOfView;
            camera.nearClipPlane = NearClip;
            camera.farClipPlane = FarClip;
            var lamp = new GameObject("Soft frontal key").AddComponent<Light>();
            lamp.type = LightType.Directional;
            lamp.transform.rotation = KeyLightAngle;
            lamp.intensity = KeyLightIntensity;
            lamp.color = KeyLightColour;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, camera.transform);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "LATCH-07 viewmodel";
            Animator animator = instance.GetComponentInChildren<Animator>(true);
            if (!animator)
            {
                animator = instance.AddComponent<Animator>();
            }

            if (!animator.avatar || !animator.avatar.isValid)
            {
                Avatar avatar = AvatarBuilder.BuildGenericAvatar(animator.gameObject, RootBone);
                Require(avatar && avatar.isValid, "Could not build the FP_Root Generic avatar.");
                Avatar saved = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
                if (!saved)
                {
                    AssetDatabase.CreateAsset(avatar, AvatarPath);
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
            SkinnedMeshRenderer[] renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(renderers.Length == SkinnedParts, $"Expected nine skinned assemblies, found {renderers.Length}.");
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
                renderer.updateWhenOffscreen = true;
            }

            SkinnedMeshRenderer screen = renderers.Single(r => r.name == DisplayMesh);
            var rows = new List<string> { "PASS Generic avatar and nine skinned assemblies." };
            using (var geometry = new DisplayProbe(screen))
            {
                motions[IdleClip].SampleAnimation(animator.gameObject, IdlePoseTime);
                float depth = camera.transform.InverseTransformPoint(geometry.Center()).z;
                if (depth < 0f)
                {
                    instance.transform.localRotation = HalfTurn;
                }

                for (int i = 0; i < Clips.Length; i++)
                {
                    AnimationClip clip = motions[Clips[i]];
                    Require(Mathf.Abs(clip.length - Durations[i]) <= ClipTolerance, $"{clip.name} is {clip.length:F3} s.");
                    Require(clip.isLooping == (clip.name == IdleClip), $"Unexpected loop on {clip.name}.");
                    clip.SampleAnimation(animator.gameObject, clip.length * SamplePoint);
                    rows.Add(FormattableString.Invariant($"PASS {clip.name}: {clip.length:F3} s at {clip.frameRate:F0} Hz."));
                }

                motions[IdleClip].SampleAnimation(animator.gameObject, IdlePoseTime);
                float[] edges = geometry.Edges();
                Require(Mathf.Abs(edges[0] - ScreenShortEdge) < ScreenTolerance && Mathf.Abs(edges[^1] - ScreenLongEdge) < ScreenTolerance,
                    $"Screen edges {string.Join(", ", edges)} are not the expected metre scale.");
                Require(geometry.InFrame(camera, ReferenceAspect), $"Ready screen is outside the 16:9 reference frame at {geometry.Center()}.");
                Require(geometry.InFrame(camera, HospitalAspect, HospitalFieldOfView), "Ready screen is outside the 4:3 hospital framing.");
                Require(geometry.FaceCamera(camera) > MinimumFacing, "Screen normals face away from the camera.");
                rows.Add(FormattableString.Invariant(
                    $"PASS Screen {edges[^1] * MillimetresPerMetre:F1} × {edges[0] * MillimetresPerMetre:F1} mm, front visible at 16:9 reference and 4:3 hospital framing."));
                rows.Add($"PASS Eye-relative model orientation is {instance.transform.localEulerAngles}.");
                Vector2[] uv = screen.sharedMesh.uv;
                foreach (Vector2 corner in new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up })
                {
                    Require(uv.Any(p => (p - corner).sqrMagnitude < UvTolerance), $"Screen is missing UV corner {corner}.");
                }

                rows.Add("PASS Four distinct 0–1 screen UV corners.");
            }

            // Save the untouched bind hierarchy as the reusable prefab, then pose the review instance.
            var source = (GameObject)PrefabUtility.InstantiatePrefab(model);
            Dictionary<string, Transform> importTransforms = source.GetComponentsInChildren<Transform>(true).ToDictionary(t => t.name);
            foreach (Transform part in instance.GetComponentsInChildren<Transform>(true).Skip(1))
            {
                Transform reference = importTransforms[part.name];
                part.SetLocalPositionAndRotation(reference.localPosition, reference.localRotation);
                part.localScale = reference.localScale;
            }

            UnityEngine.Object.DestroyImmediate(source);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Require(prefab, "Failed to save the viewmodel prefab.");
            animator.enabled = false;
            motions[IdleClip].SampleAnimation(animator.gameObject, IdlePoseTime);
            EditorSceneManager.SaveScene(scene, TestScene);
            Directory.CreateDirectory(Report);
            File.WriteAllLines(Path.Combine(Report, "import_checks.txt"), rows);
            foreach (string row in rows)
            {
                Debug.Log("LATCH07 | " + row);
            }

            AssetDatabase.SaveAssets();
        }

        private static GameObject Import(out Dictionary<string, AnimationClip> motions, out AnimatorController controller)
        {
            AssetDatabase.Refresh();
            Material[] materials = Materials().ToArray();
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
            foreach (Material material in materials)
            {
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), material);
            }

            importer.SaveAndReimport();
            ModelImporterClipAnimation[] takes = importer.defaultClipAnimations;
            Require(takes.Length == Clips.Length, $"Expected seven animation takes, found {takes.Length}.");
            foreach (ModelImporterClipAnimation take in takes)
            {
                string clipName = Clips.SingleOrDefault(n => take.takeName.EndsWith(n, StringComparison.Ordinal));
                Require(clipName != null, "Unknown FBX take: " + take.takeName);
                take.name = clipName;
                take.loopTime = clipName == IdleClip;
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
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
                ?? AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            for (int i = 0; i < Clips.Length; i++)
            {
                string clipName = Clips[i];
                var place = new Vector3(GraphColumnWidth * (i % GraphColumns), GraphTop + GraphRowHeight * (i / GraphColumns));
                AnimatorState state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == clipName)
                    ?? machine.AddState(clipName, place);
                state.motion = motions[clipName];
                if (i == 0)
                {
                    machine.defaultState = state;
                }
            }

            EditorUtility.SetDirty(controller);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        }

        private static IEnumerable<Material> Materials()
        {
            foreach (string part in TexturedParts)
            {
                Material material = MaterialAsset("NR92_" + part);
                material.SetTexture("_BaseColorMap", ConfigureTexture(part + "_BaseColor", true, false));
                material.SetTexture("_NormalMap", ConfigureTexture(part + "_Normal", false, true));
                material.SetTexture("_MaskMap", Mask(part));
                material.SetFloat("_NormalScale", part == TitaniumPart ? TitaniumNormalScale : NormalScale);
                HDMaterial.ValidateMaterial(material);
                yield return material;
            }

            // The untextured parts: colour, smoothness, metallic and emission strength, as authored in Blender.
            yield return Solid("NR92_Display_Surface", new Color(.013f, .035f, .044f), .40f);
            yield return Solid("NR92_Thread", new Color(.045f, .057f, .065f), .19f);
            yield return Solid("NR92_Satin_Steel", new Color(.16f, .21f, .24f), .67f, .68f);
            yield return Solid("NR92_Ceramic_Orange", new Color(.48f, .122f, .024f), .49f, .15f);
            yield return Solid("NR92_Etched_Label", new Color(.36f, .45f, .48f), .39f, .2f);
            yield return Solid("NR92_Status_Cyan", new Color(.075f, .66f, .68f), .69f, .08f, 2f);
            yield return Solid("NR92_Standby_Amber", new Color(.67f, .23f, .04f), .52f, .1f, 1.35f);
        }

        private static Material MaterialAsset(string name)
        {
            string path = $"{Folder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("HDRP/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetFloat("_ReceivesSSR", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Solid(string name, Color color, float smooth, float metallic = 0f, float emission = 0f)
        {
            Material material = MaterialAsset(name);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smooth);
            material.SetFloat("_Metallic", metallic);
            material.SetColor("_EmissiveColor", color * emission);
            HDMaterial.ValidateMaterial(material);
            return material;
        }

        private static Texture2D ConfigureTexture(string name, bool srgb, bool normal)
        {
            string path = $"{Folder}/Textures/{name}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = srgb;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D Mask(string part)
        {
            // Decode source PNG bytes directly to preserve their linear mask channels.
            var rough = new Texture2D(PlaceholderSize, PlaceholderSize, TextureFormat.RGBA32, false, true);
            var metal = new Texture2D(PlaceholderSize, PlaceholderSize, TextureFormat.RGBA32, false, true);
            rough.LoadImage(File.ReadAllBytes($"{Folder}/Textures/{part}_Roughness.png"));
            metal.LoadImage(File.ReadAllBytes($"{Folder}/Textures/{part}_Metallic.png"));
            Color32[] pixels = metal.GetPixels32();
            Color32[] roughness = rough.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                // HDRP mask map: metallic, no occlusion, full detail mask, smoothness = 1 - roughness.
                pixels[i] = new Color32(pixels[i].r, byte.MaxValue, byte.MaxValue, (byte)(byte.MaxValue - roughness[i].r));
            }

            string path = $"{Folder}/Textures/{part}_HDRP_Mask.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var packed = new Texture2D(rough.width, rough.height, TextureFormat.RGBA32, true, true) { name = part + "_HDRP_Mask" };
            packed.SetPixels32(pixels);
            packed.Apply();
            EditorUtility.CompressTexture(packed, TextureFormat.BC7, TextureCompressionQuality.Normal);
            if (!existing)
            {
                AssetDatabase.CreateAsset(packed, path);
            }
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException("LATCH-07: " + message);
            }
        }
    }
}
