using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NetRunner.Diagnostics.Tests
{
    /// <summary>
    /// Runs the hospital's Play mode checks (<see cref="ExplorationDiagnostics"/>,
    /// the checks the Northline validator ran) in the real scene: player,
    /// LATCH-07, CCTV, doors, lift, stairs, K-7 patrols, power and atmosphere.
    /// The check list and any pictures go to Logs/hospital-checks/.
    /// </summary>
    public sealed class HospitalChecksTests
    {
        private const string SceneName = "Hospital_Exploration";
        private const string OutputFolder = "Logs/hospital-checks";
        private const int TimeoutMs = 600000;

        [UnityTest]
        [Timeout(TimeoutMs)]
        public IEnumerator TheHospitalPassesAllPlayModeChecks()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            string output = Path.GetFullPath(OutputFolder);
            bool? passed = null;
            var host = new GameObject("Hospital checks");
            var checks = host.AddComponent<ExplorationDiagnostics>();
            checks.Output = output;
            checks.CaptureImages = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            checks.Complete = ok => passed = ok;

            yield return new WaitUntil(() => passed.HasValue);

            string report = Path.Combine(output, "playmode_checks.txt");
            var failures = File.Exists(report)
                ? File.ReadAllLines(report).Where(line => line.StartsWith("FAIL")).ToArray()
                : new[] { "No check list was written." };
            Assert.That(passed.Value, Is.True, string.Join("\n", failures));
        }
    }
}
