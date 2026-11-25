using System;
using System.IO;
using NetRunner.Atmosphere.Editor;
using NetRunner.Diagnostics;
using NetRunner.Player;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Runs the hospital's 95 checks (<see cref="ExplorationDiagnostics"/>) in
    /// the editor's Play mode and quits with 0 when they pass, 2 when they
    /// fail and 3 when the build after them fails. This is AdaM404's
    /// workflow; the same checks run as the NUnit test <c>HospitalChecksTests</c>.
    /// Runs headless with <c>-executeMethod NetRunner.World.Editor.PlayModeValidation.Run</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeValidation
    {
        private const string Session = "NET92.PlayModeValidation";
        private const string BuildAfter = Session + ".build";
        private const string Result = Session + ".result";
        private const int Passed = 0;
        private const int Failed = 2;
        private const int BuildFailed = 3;

        // Where the checks write their pictures, by how far the scene has been installed.
        private const string CctvRenders = "Northline/Renders/Phase_07_CCTV";
        private const string LatchRenders = "Northline/Renders/Phase_06_Latch07";
        private const string InteractiveRenders = "Northline/Renders/Phase_05_Unity/Interactive";
        private const string SceneRenders = "Northline/Renders/Phase_05_Unity";

        static PlayModeValidation()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        /// <summary>Adds the clinical fill lights, then runs the checks.</summary>
        public static void RefineAndRun()
        {
            ClinicalFillLights.Refine();
            Run();
        }

        /// <summary>Runs the checks and builds the Windows player if they pass.</summary>
        public static void RunAndBuild()
        {
            SessionState.SetBool(BuildAfter, true);
            Run();
        }

        /// <summary>Installs the hospital's systems, then runs the checks.</summary>
        public static void InstallAndRun()
        {
            HospitalSystemsBuilder.Install();
            Run();
        }

        /// <summary>Installs LATCH-07, then runs the checks.</summary>
        public static void InstallLatchAndRun()
        {
            HospitalLatchInstaller.Install();
            Run();
        }

        /// <summary>Installs the CCTV, then runs the checks.</summary>
        public static void InstallCctvAndRun()
        {
            HospitalCctvInstaller.Install();
            Run();
        }

        /// <summary>Opens the hospital scene and enters Play mode, where the checks run.</summary>
        public static void Run()
        {
            if (!File.Exists(HospitalAssets.ScenePath))
            {
                throw new FileNotFoundException(HospitalAssets.ScenePath);
            }

            EditorSceneManager.OpenScene(HospitalAssets.ScenePath);
            SessionState.SetBool(Session, true);
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Session, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                StartChecks();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                FinishChecks();
            }
        }

        private static void StartChecks()
        {
            var host = new GameObject("Transient validation");
            ExplorationDiagnostics diagnostics = host.AddComponent<ExplorationDiagnostics>();
            diagnostics.Output = Path.GetFullPath(UnityEngine.Object.FindAnyObjectByType<SecurityCctv>() ? CctvRenders
                : UnityEngine.Object.FindAnyObjectByType<LatchViewmodel>() ? LatchRenders
                : UnityEngine.Object.FindAnyObjectByType<StairAccess>() ? InteractiveRenders : SceneRenders);
            diagnostics.CaptureImages = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            diagnostics.Complete = passed =>
            {
                SessionState.SetInt(Result, passed ? Passed : Failed);
                EditorApplication.isPlaying = false;
            };
        }

        private static void FinishChecks()
        {
            SessionState.SetBool(Session, false);
            int result = SessionState.GetInt(Result, Failed);
            if (result == Passed && SessionState.GetBool(BuildAfter, false))
            {
                SessionState.SetBool(BuildAfter, false);
                try
                {
                    HospitalWindowsBuilds.Optimized();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    result = BuildFailed;
                }
            }

            EditorApplication.Exit(result);
        }
    }
}
