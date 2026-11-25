using System;
using System.IO;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// The development Windows builds of the hospital that Northline's phases
    /// used, each after its own installation step, into
    /// <c>Northline/Builds/</c>. The game's own builds use <c>bin/unity-build</c>.
    /// </summary>
    public static class HospitalWindowsBuilds
    {
        private const string Folder = "Northline/Builds/";
        private const string Executable = "/NETRunner92_Hospital.exe";

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

        private static void Build(string phase)
        {
            PlayerSettings.enableFrameTimingStats = true;
            string file = Path.GetFullPath(Folder + phase + Executable);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            BuildReport result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { HospitalAssets.ScenePath },
                locationPathName = file,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            if (result.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(result.summary.result.ToString());
            }

            Debug.Log("NET92 WINDOWS BUILD | " + file + " | " + result.summary.totalSize);
        }
    }
}
