using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NetRunner.Atmosphere;
using NetRunner.Core;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Bakes the hospital's static lighting (#79): every lamp whose light never
    /// changes is baked into lightmaps and light probes once, so the GPU no
    /// longer lights it every frame; the lamps that pulse, are scripted or move
    /// stay live (<see cref="LightBakeRules"/>). It marks the static, solid
    /// renderers for the bake (large ones lightmapped, small ones probe-lit),
    /// adds a global probe volume and its baking set, sets the lighting
    /// settings and bakes. Runs last in the hospital build, and headless with
    /// <c>-executeMethod NetRunner.World.Editor.HospitalLighting.Bake</c>
    /// (HDRP must run: use <c>-force-vulkan</c>, not <c>-nographics</c>).
    /// </summary>
    public static class HospitalLighting
    {
        // The scene's baked data sits next to it, in its own folder, as the occlusion data does.
        private const string DataFolder = HospitalAssets.Root + "/Scenes/Hospital_Exploration";
        private const string SettingsPath = DataFolder + "/Hospital_Lighting.lighting";
        private const string BakingSetPath = DataFolder + "/Hospital_Exploration Baking Set.asset";
        private const string SurfaceTypeProperty = "_SurfaceType";
        private const string ProbeVolumeName = "Baked light probes";

        // Lightmap texels per metre, the largest lightmap, and the gap between charts in texels.
        private const float TexelsPerMetre = 6f;
        private const int LargestLightmap = 2048;
        private const int ChartPadding = 2;

        // Light bounces, and the samples per texel for direct, indirect and sky light.
        private const int Bounces = 2;
        private const int DirectSamples = 32;
        private const int IndirectSamples = 256;
        private const int SkySamples = 128;

        private const StaticEditorFlags BakeFlag = StaticEditorFlags.ContributeGI;

        /// <summary>
        /// Sets the lamps' bake modes and the renderers' bake roles in the
        /// hospital scene, bakes its lighting and saves the scene.
        /// </summary>
        [MenuItem("NET.runner-92/Bake hospital lighting")]
        public static void Bake()
        {
            Scene scene = EditorSceneManager.OpenScene(HospitalAssets.ScenePath);
            var (live, baked) = SetLampModes();
            var (lightmapped, probeLit, notBaked) = SetRendererRoles();
            AddProbeVolume(scene);
            Lightmapping.lightingSettings = LightingSettingsAsset();
            AddToBakingSet(scene);
            EditorSceneManager.SaveScene(scene);

            var clock = Stopwatch.StartNew();
            if (!Lightmapping.Bake())
            {
                throw new InvalidOperationException("The lighting bake failed or was cancelled.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            long bytes = LightmapSettings.lightmaps.Sum(map => map.lightmapColor ? Profiler.GetRuntimeMemorySizeLong(map.lightmapColor) : 0);
            Debug.Log($"NET92 LIGHTING | {baked} baked lamps, {live} live | {lightmapped} lightmapped, {probeLit} probe-lit, "
                + $"{notBaked} not baked renderers | {LightmapSettings.lightmaps.Length} lightmaps, {bytes / 1048576f:F1} MB | {clock.Elapsed:hh\\:mm\\:ss}");
        }

        // Live: pulsing lamps, the residual-power lamps, the lightning, and anything in the lift.
        private static (int Live, int Baked) SetLampModes()
        {
            var scripted = new HashSet<Light>(Object.FindObjectsByType<ResidualPower>().Select(power => power.DiagnosticFixture).Where(lamp => lamp));
            HospitalAtmosphere atmosphere = Object.FindAnyObjectByType<HospitalAtmosphere>();
            if (atmosphere && atmosphere.Lightning)
            {
                scripted.Add(atmosphere.Lightning);
            }

            int live = 0;
            int baked = 0;
            foreach (Light lamp in Object.FindObjectsByType<Light>())
            {
                bool pulses = lamp.GetComponent<PracticalPulse>();
                bool moves = lamp.GetComponentInParent<HospitalLift>();
                bool stays = LightBakeRules.StaysLive(pulses, scripted.Contains(lamp), moves);
                lamp.lightmapBakeType = stays ? LightmapBakeType.Realtime : LightmapBakeType.Baked;
                if (stays)
                {
                    live++;
                }
                else
                {
                    baked++;
                }
            }

            return (live, baked);
        }

        // In the bake: what stays put (under the imported environment) and is solid.
        private static (int Lightmapped, int ProbeLit, int NotBaked) SetRendererRoles()
        {
            Transform environment = GameObject.Find(HospitalHierarchy.ImportedEnvironment).transform;
            int lightmapped = 0;
            int probeLit = 0;
            int notBaked = 0;
            foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                bool moves = !renderer.transform.IsChildOf(environment);
                BakeRole role = LightBakeRules.RoleOf(moves, IsSeeThrough(renderer), Breadth(renderer.bounds.size));
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & ~BakeFlag;
                if (role != BakeRole.NotBaked)
                {
                    flags |= BakeFlag;
                }

                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
                renderer.receiveGI = role == BakeRole.Lightmapped ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
                if (role == BakeRole.Lightmapped)
                {
                    lightmapped++;
                }
                else if (role == BakeRole.ProbeLit)
                {
                    probeLit++;
                }
                else
                {
                    notBaked++;
                }
            }

            return (lightmapped, probeLit, notBaked);
        }

        // One global probe volume: it places probes around every renderer in the bake.
        private static void AddProbeVolume(Scene scene)
        {
            ProbeVolume volume = Object.FindAnyObjectByType<ProbeVolume>();
            if (!volume)
            {
                Transform atmosphere = GameObject.Find(HospitalHierarchy.Atmosphere).transform;
                volume = SceneParts.Node(ProbeVolumeName, atmosphere).gameObject.AddComponent<ProbeVolume>();
            }

            volume.mode = ProbeVolume.Mode.Global;
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static LightingSettings LightingSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if (!settings)
            {
                settings = new LightingSettings { name = Path.GetFileNameWithoutExtension(SettingsPath) };
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.lightmapResolution = TexelsPerMetre;
            settings.lightmapMaxSize = LargestLightmap;
            settings.lightmapPadding = ChartPadding;
            settings.directionalityMode = LightmapsMode.NonDirectional;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            settings.maxBounces = Bounces;
            settings.directSampleCount = DirectSamples;
            settings.indirectSampleCount = IndirectSamples;
            settings.environmentSampleCount = SkySamples;
            settings.ao = false;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        // The probe volume bakes only for a scene in a baking set. The Lighting
        // window makes one itself; headless, the builder does, using HDRP's own
        // defaults through SetDefaults, which HDRP 17 keeps internal.
        private static void AddToBakingSet(Scene scene)
        {
            string sceneGuid = AssetDatabase.AssetPathToGUID(scene.path);
            var set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(BakingSetPath);
            if (!set)
            {
                set = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
                MethodInfo defaults = typeof(ProbeVolumeBakingSet).GetMethod("SetDefaults", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingMethodException("ProbeVolumeBakingSet.SetDefaults is gone; update HospitalLighting for this HDRP version.");
                defaults.Invoke(set, null);
                AssetDatabase.CreateAsset(set, BakingSetPath);
            }

            if (!set.sceneGUIDs.Contains(sceneGuid) && !set.TryAddScene(sceneGuid))
            {
                throw new InvalidOperationException($"{scene.name} already belongs to another probe volume baking set.");
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
        }

        // The second-longest side: walls and floors are wide in two directions, poles and strips in one.
        private static float Breadth(Vector3 size)
        {
            float[] sides = { size.x, size.y, size.z };
            Array.Sort(sides);
            return sides[1];
        }

        private static bool IsSeeThrough(Renderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material && material.HasProperty(SurfaceTypeProperty) && material.GetFloat(SurfaceTypeProperty) > .5f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
