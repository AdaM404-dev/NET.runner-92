using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace NETRunner92
{
    public sealed class PerformanceCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (!Environment.GetCommandLineArgs().Contains("-net92-benchmark")) return;
            new GameObject("Transient performance capture").AddComponent<PerformanceCapture>();
        }

        IEnumerator Start()
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            var p = FindAnyObjectByType<FirstPersonController>();
            if (!p) { Application.Quit(2); yield break; }
            p.enabled = false;
            p.CaptureCursor(false);
            var results = new List<string> {
                $"{SystemInfo.graphicsDeviceName}; {SystemInfo.graphicsDeviceType}; {Screen.width}x{Screen.height}; {QualitySettings.names[QualitySettings.GetQualityLevel()]}",
                "Zone;Samples;AvgFPS;P95FrameMs;P95GPUms;MeanDrawCalls;MeanTriangles"
            };
            using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            var positions = new[] {
                ("South checkpoint",new Vector3(0,.05f,-36.4f),0f),
                ("Reception",new Vector3(-1,.05f,-13.4f),337f),
                ("Ward",new Vector3(-24,4.24f,0),90f),
                ("Surgery",new Vector3(7,8.44f,0),270f),
                ("Research",new Vector3(6,12.64f,0),90f)
            };
            var timings = new FrameTiming[1];
            foreach (var (label,position,yaw) in positions)
            {
                p.Warp(position,Quaternion.Euler(0,yaw,0));
                yield return new WaitForSecondsRealtime(label == "South checkpoint" ? 8f : 5f);
                yield return null;
                var frame = new List<double>();
                var gpu = new List<double>();
                var draw = new List<double>();
                var tris = new List<double>();
                float end = Time.realtimeSinceStartup + 4.5f;
                while (Time.realtimeSinceStartup < end)
                {
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1,timings)>0 && timings[0].gpuFrameTime>0) gpu.Add(timings[0].gpuFrameTime);
                    if (draws.Valid && draws.LastValue>0) draw.Add(draws.LastValue);
                    if (triangles.Valid) tris.Add(triangles.LastValue);
                    frame.Add(Time.unscaledDeltaTime*1000);
                    yield return null;
                }
                results.Add(FormattableString.Invariant($"{label};{frame.Count};{1000/frame.Average():F1};{P95(frame):F2};{P95(gpu):F2};{Mean(draw):F0};{Mean(tris):F0}"));
            }
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-net92-output");
            string path = i >= 0 && i+1 < args.Length ? args[i+1] : Path.Combine(Application.persistentDataPath,"performance.csv");
            File.WriteAllLines(path,results);
            Debug.Log("NET92 BENCHMARK COMPLETE | " + path);
            Application.Quit();
        }

        static double Mean(List<double> n) => n.Count==0 ? -1 : n.Average();
        static double P95(List<double> n)
        {
            if(n.Count==0)return -1;
            n.Sort();
            return n[Math.Min(n.Count-1,Mathf.CeilToInt(n.Count*.95f)-1)];
        }
    }
}

