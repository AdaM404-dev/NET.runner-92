using System;
using System.IO;
using NetRunner.Menu.Editor;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// The Windows builds of the Phase 11 hospital, into <c>../Builds/</c>
    /// next to this project: the development builds Northline's phases used,
    /// each after its own installation step, and the release build that
    /// starts in the main menu.
    /// </summary>
    public static class HospitalWindowsBuilds
    {
        private const string Folder = "../Builds/";
        private const string Executable = "/NETRunner92_Hospital.exe";
        private const string MenuPhase = "Phase_11_Frame_Pacing";
        private const string FontLicence = HospitalAssets.Ui + "/RobotoMono-LICENSE.txt";
        private const string FontLicenceFile = "RobotoMono-LICENSE.txt";

        /// <summary>Builds the scene as it is (Phase 05, optimised).</summary>
        public static void Optimized()
        {
            Build("Phase_05_Optimized");
        }

        /// <summary>Optimises the scene, then builds it (Phase 05, traversal).</summary>
        public static void Traversal()
        {
            HospitalOptimization.Optimize();
            Build("Phase_05_Traversal");
        }

        /// <summary>Installs the hospital's systems, then builds (Phase 05, interactive).</summary>
        public static void Interactive()
        {
            HospitalSystemsBuilder.Install();
            Build("Phase_05_Interactive");
        }

        /// <summary>Installs LATCH-07, then builds (Phase 06).</summary>
        public static void Latch07()
        {
            HospitalLatchInstaller.Install();
            Build("Phase_06_Latch07");
        }

        /// <summary>Installs the CCTV, then builds (Phase 07).</summary>
        public static void Cctv()
        {
            HospitalCctvInstaller.Install();
            Build("Phase_07_CCTV");
        }

        /// <summary>Builds the scene with its HUD as it is (Phase 08).</summary>
        public static void Hud()
        {
            Build("Phase_08_HUD_Reference");
        }

        /// <summary>
        /// Builds the release game: the studio intro, the main menu, then the
        /// hospital, creating the menu scene when it is missing, with the
        /// interface font's licence.
        /// </summary>
        [MenuItem("NET.runner-92/Main Menu/Build Windows player")]
        public static void MainMenu()
        {
            if (!File.Exists(HospitalAssets.MainMenuPath))
            {
                MainMenuBuilder.Create();
            }

            string file = Build(MenuPhase, SceneParts.StartOrder(), BuildOptions.None);
            File.Copy(FontLicence, Path.Combine(Path.GetDirectoryName(file), FontLicenceFile), true);
        }

        private static string Build(string phase, string[] scenes = null, BuildOptions options = BuildOptions.Development)
        {
            PlayerSettings.enableFrameTimingStats = true;
            string file = Path.GetFullPath(Folder + phase + Executable);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            BuildReport result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes ?? new[] { HospitalAssets.ScenePath },
                locationPathName = file,
                target = BuildTarget.StandaloneWindows64,
                options = options,
            });
            if (result.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(result.summary.result.ToString());
            }

            Debug.Log("NET92 WINDOWS BUILD | " + file + " | " + result.summary.totalSize);
            return file;
        }
    }
}
