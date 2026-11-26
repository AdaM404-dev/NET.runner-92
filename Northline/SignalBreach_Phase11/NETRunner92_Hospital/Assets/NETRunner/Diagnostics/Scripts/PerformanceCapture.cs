using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NetRunner.Core;
using NetRunner.Menu;
using NetRunner.Player;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NetRunner.Diagnostics
{
    /// <summary>
    /// Measures five stationary views of the hospital in a standalone player,
    /// with the features that <see cref="RenderFeatureSwitch"/> switches off
    /// for the run (<c>-net92-without</c>, <c>-net92-upscaler</c>).
    /// </summary>
    public sealed class PerformanceCapture : MonoBehaviour
    {
        private const string BenchmarkOption = "-net92-benchmark";
        private const string OutputOption = "-net92-output";
        private const string DefaultFile = "performance.csv";
        private const int FailedExitCode = 2;
        private const int Uncapped = -1;
        private const int TargetDepth = 24;
        private const double MillisecondsPerSecond = 1000.0;
        private const double Percentile = .95;
        private const float WindowRenderTimeout = 2f;
        private const float FirstWarmupSeconds = 8f;
        private const float WarmupSeconds = 5f;
        private const float SampleSeconds = 4.5f;
        private const string PausedLabel = "Paused";
        private const int NotMeasured = -1;

        private static readonly (string Label, Vector3 Position, float Yaw)[] Zones =
        {
            ("South checkpoint", new Vector3(0f, .05f, -36.4f), 0f),
            ("Reception", new Vector3(-1f, .05f, -13.4f), 337f),
            ("Ward", new Vector3(-24f, 4.24f, 0f), 90f),
            ("Surgery", new Vector3(7f, 8.44f, 0f), 270f),
            ("Research", new Vector3(6f, 12.64f, 0f), 90f)
        };

        private Camera eyes;
        private int renderedFrames;
        private RenderTexture target;
        private RenderPipeline.StandardRequest renderRequest;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (!LaunchArguments.Has(Environment.GetCommandLineArgs(), BenchmarkOption))
            {
                return;
            }

            // The game pauses in the background; an unattended benchmark must not.
            Application.runInBackground = true;
            var host = new GameObject("Transient performance capture");
            DontDestroyOnLoad(host);
            host.AddComponent<PerformanceCapture>();
        }

        private IEnumerator Start()
        {
            if (SceneManager.GetActiveScene().name != HospitalRun.HospitalScene)
            {
                if (!Application.CanStreamedLevelBeLoaded(HospitalRun.HospitalScene))
                {
                    Fail($"Scene '{HospitalRun.HospitalScene}' is missing from the build.");
                    yield break;
                }

                yield return SceneManager.LoadSceneAsync(HospitalRun.HospitalScene, LoadSceneMode.Single);
            }

            // PreferencesRuntime applies the scene's quality and pacing on its first frame.
            yield return null;
            yield return null;
            var player = FindAnyObjectByType<FirstPersonController>();
            if (!player || !player.Eyes || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Fail("The hospital needs a player camera and an active graphics device.");
                yield break;
            }

            player.PauseInput(true);
            player.CaptureCursor(false);
            eyes = player.Eyes;
            string switches = RenderFeatureSwitch.ApplyFromLaunchOptions(eyes);
            RenderPipelineManager.endCameraRendering += CountCameraRender;
            Uncap();
            float deadline = Time.realtimeSinceStartup + WindowRenderTimeout;
            while (renderedFrames == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            // A hidden window can keep running game updates without rendering.
            if (renderedFrames == 0 && !SetUpOffscreenRender())
            {
                Fail("The hospital camera did not render, and its pipeline cannot render offscreen.");
                yield break;
            }

            string mode = renderRequest == null ? "Window" : "Offscreen";
            int width = target ? target.width : Screen.width;
            int height = target ? target.height : Screen.height;
            string quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            var results = new List<string>
            {
                $"{SystemInfo.graphicsDeviceName}; {SystemInfo.graphicsDeviceType}; {width}x{height}; {quality}; {mode}; {switches}",
                "Zone;Samples;AvgFPS;P95FrameMs;P95GPUms;MeanDrawCalls;MeanTriangles"
            };
            using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            var timings = new FrameTiming[1];

            for (int i = 0; i < Zones.Length; i++)
            {
                var (label, position, yaw) = Zones[i];
                player.Warp(position, Quaternion.Euler(0f, yaw, 0f));
                Physics.SyncTransforms();
                float warmUntil = Time.realtimeSinceStartup + (i == 0 ? FirstWarmupSeconds : WarmupSeconds);
                while (Time.realtimeSinceStartup < warmUntil)
                {
                    Uncap();
                    RenderOffscreen();
                    yield return null;
                }

                var frame = new List<double>();
                var gpu = new List<double>();
                var draw = new List<double>();
                var tris = new List<double>();
                float end = Time.realtimeSinceStartup + SampleSeconds;
                while (Time.realtimeSinceStartup < end)
                {
                    int before = renderedFrames;
                    Uncap();
                    FrameTimingManager.CaptureFrameTimings();
                    RenderOffscreen();
                    yield return null;
                    if (renderedFrames == before)
                    {
                        continue;
                    }

                    frame.Add(Time.unscaledDeltaTime * MillisecondsPerSecond);
                    if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0)
                    {
                        gpu.Add(timings[0].gpuFrameTime);
                    }

                    if (draws.Valid && draws.LastValue > 0)
                    {
                        draw.Add(draws.LastValue);
                    }

                    if (triangles.Valid && triangles.LastValue > 0)
                    {
                        tris.Add(triangles.LastValue);
                    }
                }

                if (frame.Count == 0)
                {
                    Fail($"The hospital camera produced no rendered frames in {label}.");
                    yield break;
                }

                results.Add(FormattableString.Invariant(
                    $"{label};{frame.Count};{MillisecondsPerSecond / SampleStatistics.Mean(frame):F1};{SampleStatistics.Percentile(frame, Percentile):F2};{SampleStatistics.Percentile(gpu, Percentile):F2};{SampleStatistics.Mean(draw):F0};{SampleStatistics.Mean(tris):F0}"));
            }

            // Offscreen runs draw the camera by request, which a frozen view would stop.
            if (renderRequest == null)
            {
                yield return MeasurePaused(results);
            }

            WriteResults(results);
        }

        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= CountCameraRender;
            if (target)
            {
                target.Release();
                Destroy(target);
            }
        }

        // The pause menu's frozen frame at the last view (#36, round 3). The player
        // camera is off then, so every frame counts, not only the camera's.
        private IEnumerator MeasurePaused(List<string> results)
        {
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            var view = new GameObject("Benchmark paused view").AddComponent<PausedView>();
            view.Configure(eyes, panel);
            view.Freeze();
            float warmUntil = Time.realtimeSinceStartup + WarmupSeconds;
            while (Time.realtimeSinceStartup < warmUntil)
            {
                Uncap();
                yield return null;
            }

            var frame = new List<double>();
            var gpu = new List<double>();
            var timings = new FrameTiming[1];
            float end = Time.realtimeSinceStartup + SampleSeconds;
            while (Time.realtimeSinceStartup < end)
            {
                Uncap();
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                frame.Add(Time.unscaledDeltaTime * MillisecondsPerSecond);
                if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0)
                {
                    gpu.Add(timings[0].gpuFrameTime);
                }
            }

            view.Thaw();
            Destroy(view.gameObject);
            Destroy(panel);
            results.Add(FormattableString.Invariant(
                $"{PausedLabel};{frame.Count};{MillisecondsPerSecond / SampleStatistics.Mean(frame):F1};{SampleStatistics.Percentile(frame, Percentile):F2};{SampleStatistics.Percentile(gpu, Percentile):F2};{NotMeasured};{NotMeasured}"));
        }

        private void CountCameraRender(ScriptableRenderContext context, Camera camera)
        {
            if (camera == eyes)
            {
                renderedFrames++;
            }
        }

        private bool SetUpOffscreenRender()
        {
            target = new RenderTexture(Screen.width, Screen.height, TargetDepth, RenderTextureFormat.ARGB32)
            {
                name = "Hospital benchmark",
                useDynamicScale = true,
            };
            renderRequest = new RenderPipeline.StandardRequest { destination = target };
            if (!RenderPipeline.SupportsRenderRequest(eyes, renderRequest) || !target.Create())
            {
                return false;
            }

            eyes.enabled = false;
            return true;
        }

        private void RenderOffscreen()
        {
            if (renderRequest != null)
            {
                RenderPipeline.SubmitRenderRequest(eyes, renderRequest);
            }
        }

        private static void Uncap()
        {
            // A display refresh change can cause PreferencesRuntime to reapply pacing.
            if (QualitySettings.vSyncCount != 0)
            {
                QualitySettings.vSyncCount = 0;
            }

            if (Application.targetFrameRate != Uncapped)
            {
                Application.targetFrameRate = Uncapped;
            }
        }

        private static void WriteResults(List<string> results)
        {
            string path = LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), OutputOption)
                ?? Path.Combine(Application.persistentDataPath, DefaultFile);
            try
            {
                path = Path.GetFullPath(path);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllLines(path, results);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Fail($"Could not write the CSV to '{path}': {error.Message}");
                return;
            }

            Debug.Log("NET92 BENCHMARK COMPLETE | " + path);
            Application.Quit();
        }

        private static void Fail(string message)
        {
            Debug.LogError("NET92 BENCHMARK FAILED | " + message);
            Application.Quit(FailedExitCode);
        }

    }
}

