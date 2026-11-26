using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.SceneBuilding.Editor
{
    /// <summary>
    /// The small pieces every hospital builder uses: an empty node in the
    /// hierarchy, a box with a material, a plain HDRP material for the
    /// builders' own machinery, and file names made from display names.
    /// </summary>
    public static class SceneParts
    {
        private const string LitShader = "HDRP/Lit";
        private const string SystemMaterialPrefix = HospitalAssets.Materials + "/System_";
        private const string BaseColor = "_BaseColor";
        private const string Metallic = "_Metallic";
        private const string Smoothness = "_Smoothness";
        private const string EmissiveColor = "_EmissiveColor";

        // Every system material has the same satin finish.
        private const float SystemSmoothness = .38f;

        /// <summary>An empty object named <paramref name="name"/>, under <paramref name="parent"/> when given.</summary>
        public static Transform Node(string name, Transform parent = null)
        {
            Transform node = new GameObject(name).transform;
            if (parent)
            {
                node.SetParent(parent, false);
            }

            return node;
        }

        /// <summary>
        /// A box of <paramref name="size"/> at <paramref name="position"/> under
        /// <paramref name="parent"/>, with a box collider unless
        /// <paramref name="hasCollider"/> is false.
        /// </summary>
        public static Transform Block(Transform parent, string name, Vector3 position, Vector3 size, Material material,
            bool hasCollider = true)
        {
            Transform block = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            block.name = name;
            block.SetParent(parent, false);
            block.localPosition = position;
            block.localScale = size;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!hasCollider)
            {
                Object.DestroyImmediate(block.GetComponent<Collider>());
            }

            return block;
        }

        /// <summary>
        /// The HDRP Lit material <c>System_&lt;name&gt;.mat</c> in the building's
        /// materials, created when missing and set to the given colour,
        /// metalness and glow.
        /// </summary>
        public static Material SystemMaterial(string name, Color color, float metallic, Color emission = default)
        {
            string path = SystemMaterialPrefix + AssetName(name) + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find(LitShader)) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor(BaseColor, color);
            material.SetFloat(Metallic, metallic);
            material.SetFloat(Smoothness, SystemSmoothness);
            material.SetColor(EmissiveColor, emission);
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>The asset at <paramref name="path"/>, created (with its folder) when missing.</summary>
        public static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset)
            {
                return asset;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>The asset at <paramref name="path"/>; a missing one is an error.</summary>
        public static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset ? asset : throw new FileNotFoundException(path);
        }

        /// <summary>
        /// The game's scenes in start order, those that exist: the studio
        /// intro, the main menu, the hospital.
        /// </summary>
        public static string[] StartOrder()
        {
            return new[] { HospitalAssets.IntroScenePath, HospitalAssets.MainMenuPath, HospitalAssets.ScenePath }
                .Where(File.Exists).ToArray();
        }

        /// <summary>Puts <see cref="StartOrder"/> first in Build Settings, enabled, and keeps any other scenes after it.</summary>
        public static void SetBuildOrder()
        {
            string[] first = StartOrder();
            EditorBuildSettings.scenes = first.Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(EditorBuildSettings.scenes.Where(scene => !first.Contains(scene.path))).ToArray();
        }

        /// <summary><paramref name="name"/> with every character that is not a letter or digit replaced by '_'.</summary>
        public static string AssetName(string name)
        {
            return new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        }
    }
}
