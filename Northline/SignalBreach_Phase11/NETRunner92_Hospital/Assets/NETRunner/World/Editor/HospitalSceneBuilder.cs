using System.IO;
using NetRunner.Atmosphere.Editor;
using NetRunner.Player;
using NetRunner.Player.Editor;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Builds the hospital scene from scratch, each feature with its own
    /// builder: the pipeline and player settings, the imported building and
    /// grounds, the player, the atmosphere, the clinical fill lights, the
    /// optimisation pass, the hospital's systems, LATCH-07 with the CCTV and
    /// the HUD, and the occlusion bake. The main menu scene and the runtime
    /// references are separate steps (<see cref="HospitalWindowsBuilds.MainMenu"/>,
    /// <see cref="RuntimeReferencesInstaller"/>). It rewrites the mesh, material and profile assets,
    /// so it is run only when the source changes. Runs headless with
    /// <c>-executeMethod NetRunner.World.Editor.HospitalSceneBuilder.BuildScene</c>.
    /// </summary>
    public static class HospitalSceneBuilder
    {
        private const string PipelinePath = HospitalAssets.Rendering + "/Northline_HDRP.asset";
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
        private const string ActiveInputHandler = "activeInputHandler";
        private const int InputSystemOnly = 1;
        private const string ProductName = "NET.runner-92 — Hospital Exploration";
        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        // The source model has three stair flights between each of its four floors, on both cores.
        private const int ExpectedRamps = 12;

        /// <summary>Builds and saves the whole hospital scene and puts it first in Build Settings.</summary>
        [MenuItem("NET.runner-92/Build Hospital Exploration Scene")]
        public static void BuildScene()
        {
            var importer = new HospitalImporter();
            AssetDatabase.Refresh();
            ConfigurePipeline();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform model = SceneParts.Node(HospitalHierarchy.ImportedEnvironment);
            Transform collision = SceneParts.Node(HospitalHierarchy.Collision);
            Transform atmosphere = SceneParts.Node(HospitalHierarchy.Atmosphere);
            importer.Import("Hospital", model, collision, atmosphere);
            importer.Import("Exterior", model, collision, atmosphere);
            if (importer.Ramps != ExpectedRamps)
            {
                throw new InvalidDataException($"Expected twelve stair ramps, imported {importer.Ramps}");
            }

            FirstPersonController player = PlayerPrefabBuilder.Add();
            HospitalAtmosphereBuilder.Add(atmosphere, player.Eyes.transform);
            EditorSceneManager.SaveScene(scene, HospitalAssets.ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(HospitalAssets.ScenePath, true) };
            AssetDatabase.SaveAssets();
            ClinicalFillLights.Refine();
            HospitalOptimization.Optimize();
            HospitalSystemsBuilder.Install();
            if (Latch07Installer.IsViewmodelReady())
            {
                HospitalHudInstaller.Install();
            }

            HospitalOcclusion.Bake();
            Debug.Log($"NET92 SCENE | {importer.Visuals} visual meshes | {importer.Solids} colliders | {importer.Ramps} stair ramps");
        }

        // HDRP with SSR, SSAO and volumetrics but no ray tracing; linear
        // colour; a 1080p window that keeps running in the background; frame
        // timing for the benchmarks; Direct3D 11 on Windows; Input System only.
        private static void ConfigurePipeline()
        {
            HDRenderPipelineAsset hdrp = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(PipelinePath);
            if (!hdrp)
            {
                hdrp = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                AssetDatabase.CreateAsset(hdrp, PipelinePath);
            }

            RenderPipelineSettings settings = hdrp.currentPlatformRenderPipelineSettings;
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
            PlayerSettings.productName = ProductName;
            PlayerSettings.defaultScreenWidth = ScreenWidth;
            PlayerSettings.defaultScreenHeight = ScreenHeight;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            Object[] playerSettings = AssetDatabase.LoadAllAssetsAtPath(ProjectSettingsPath);
            if (playerSettings.Length > 0)
            {
                var serialized = new SerializedObject(playerSettings[0]);
                SerializedProperty input = serialized.FindProperty(ActiveInputHandler);
                if (input != null)
                {
                    input.intValue = InputSystemOnly;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }
}
