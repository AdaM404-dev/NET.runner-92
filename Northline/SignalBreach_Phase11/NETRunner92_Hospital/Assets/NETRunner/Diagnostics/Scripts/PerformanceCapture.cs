using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetRunner.Core;
using NetRunner.Menu;
using NetRunner.Player;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NetRunner.Diagnostics
{
    /// <summary>Measures five stationary views of the hospital in a standalone player.</summary>
    public sealed class PerformanceCapture : MonoBehaviour
    {
        private const string BenchmarkOption = "-net92-benchmark";
        private const string OutputOption = "-net92-output";
        private const float WindowRenderTimeout = 2f;
        private const float FirstWarmupSeconds = 8f;
        private const float WarmupSeconds = 5f;
        private const float SampleSeconds = 4.5f;

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
            if (Array.IndexOf(Environment.GetCommandLineArgs(), BenchmarkOption) < 0)
            {
                return;
            }

            var host = new GameObject("Transient performance capture");
            DontDestroyOnLoad(host);
            host.AddComponent<PerformanceCapture>();
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
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
                $"{SystemInfo.graphicsDeviceName}; {SystemInfo.graphicsDeviceType}; {width}x{height}; {quality}; {mode}",
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

                    frame.Add(Time.unscaledDeltaTime * 1000.0);
                    if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0)
                    {
                        gpu.Add(timings[0].gpuFrameTime);
                    }

                    if (draws.Valid && draws.LastValue > 0) draw.Add(draws.LastValue);
                    if (triangles.Valid && triangles.LastValue > 0) tris.Add(triangles.LastValue);
                }

                if (frame.Count == 0)
                {
                    Fail($"The hospital camera produced no rendered frames in {label}.");
                    yield break;
                }

                results.Add(FormattableString.Invariant(
                    $"{label};{frame.Count};{1000 / frame.Average():F1};{P95(frame):F2};{P95(gpu):F2};{Mean(draw):F0};{Mean(tris):F0}"));
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

        private void CountCameraRender(ScriptableRenderContext context, Camera camera)
        {
            if (camera == eyes)
            {
                renderedFrames++;
            }
        }

        private bool SetUpOffscreenRender()
        {
            target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32)
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
            if (QualitySettings.vSyncCount != 0) QualitySettings.vSyncCount = 0;
            if (Application.targetFrameRate != -1) Application.targetFrameRate = -1;
        }

        private static void WriteResults(List<string> results)
        {
            string path = LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), OutputOption)
                ?? Path.Combine(Application.persistentDataPath, "performance.csv");
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
            Application.Quit(2);
        }

        private static double Mean(List<double> values) => values.Count == 0 ? -1 : values.Average();

        private static double P95(List<double> values)
        {
            if (values.Count == 0) return -1;
            values.Sort();
            return values[Mathf.CeilToInt(values.Count * .95f) - 1];
        }
    }
}

