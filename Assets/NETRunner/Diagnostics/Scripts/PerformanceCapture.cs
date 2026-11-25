using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NetRunner.Core;
using NetRunner.Player;
using Unity.Profiling;
using UnityEngine;

namespace NetRunner.Diagnostics
{
    /// <summary>
    /// The view benchmark of the built game (<c>-net92-benchmark</c>): with
    /// VSync and the frame cap off, warps the player to five fixed views and
    /// measures 4.5 s at each, after a warm-up. Writes one CSV: the graphics
    /// card, resolution, quality level and switched-off features, then per
    /// view the samples, average FPS, P95 frame and GPU times, draw calls and
    /// triangles; then quits. See docs/systems/performance.md.
    /// </summary>
    public sealed class PerformanceCapture : MonoBehaviour
    {
        private const string Option = "-net92-benchmark";
        private const string OutputOption = "-net92-output";
        private const string DefaultFile = "performance.csv";
        private const string Header = "Zone;Samples;AvgFPS;P95FrameMs;P95GPUms;MeanDrawCalls;MeanTriangles";
        private const int NoPlayerExitCode = 2;
        private const int Uncapped = -1;
        private const float FirstWarmUp = 8f;
        private const float WarmUp = 5f;
        private const float Measured = 4.5f;
        private const double Percentile = .95;
        private const float MillisecondsPerSecond = 1000f;

        private static readonly (string Label, Vector3 Position, float Yaw)[] Views =
        {
            ("South checkpoint", new Vector3(0, .05f, -36.4f), 0f),
            ("Reception", new Vector3(-1, .05f, -13.4f), 337f),
            ("Ward", new Vector3(-24, 4.24f, 0), 90f),
            ("Surgery", new Vector3(7, 8.44f, 0), 270f),
            ("Research", new Vector3(6, 12.64f, 0), 90f),
        };

        private readonly FrameTiming[] timings = new FrameTiming[1];

        private IEnumerator Start()
        {
            Application.targetFrameRate = Uncapped;
            QualitySettings.vSyncCount = 0;
            FirstPersonController player = FindAnyObjectByType<FirstPersonController>();
            if (!player)
            {
                Application.Quit(NoPlayerExitCode);
                yield break;
            }

            player.enabled = false;
            player.CaptureCursor(false);
            var results = new List<string> { BenchmarkReport.Context(), Header };
            using (ProfilerRecorder draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
            using (ProfilerRecorder triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count"))
            {
                for (int i = 0; i < Views.Length; i++)
                {
                    (string label, Vector3 position, float yaw) = Views[i];
                    player.Warp(position, Quaternion.Euler(0, yaw, 0));
                    yield return new WaitForSecondsRealtime(i == 0 ? FirstWarmUp : WarmUp);
                    yield return null;
                    var frame = new List<double>();
                    var gpu = new List<double>();
                    var draw = new List<double>();
                    var tris = new List<double>();
                    float end = Time.realtimeSinceStartup + Measured;
                    while (Time.realtimeSinceStartup < end)
                    {
                        FrameTimingManager.CaptureFrameTimings();
                        if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0)
                        {
                            gpu.Add(timings[0].gpuFrameTime);
                        }

                        if (draws.Valid && draws.LastValue > 0)
                        {
                            draw.Add(draws.LastValue);
                        }

                        if (triangles.Valid)
                        {
                            tris.Add(triangles.LastValue);
                        }

                        frame.Add(Time.unscaledDeltaTime * MillisecondsPerSecond);
                        yield return null;
                    }

                    results.Add(FormattableString.Invariant(
                        $"{label};{frame.Count};{MillisecondsPerSecond / SampleStatistics.Mean(frame):F1};{SampleStatistics.Percentile(frame, Percentile):F2};{SampleStatistics.Percentile(gpu, Percentile):F2};{SampleStatistics.Mean(draw):F0};{SampleStatistics.Mean(tris):F0}"));
                }
            }

            string path = LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), OutputOption)
                ?? Path.Combine(Application.persistentDataPath, DefaultFile);
            File.WriteAllLines(path, results);
            Debug.Log("NET92 BENCHMARK COMPLETE | " + path);
            Application.Quit();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (LaunchArguments.Has(Environment.GetCommandLineArgs(), Option))
            {
                new GameObject("Transient performance capture").AddComponent<PerformanceCapture>();
            }
        }
    }
}
