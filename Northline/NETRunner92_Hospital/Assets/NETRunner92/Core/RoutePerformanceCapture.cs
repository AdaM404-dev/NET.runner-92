using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NETRunner92
{
    public sealed class RoutePerformanceCapture : MonoBehaviour
    {
        readonly struct Frame
        {
            public readonly string Segment;
            public readonly float Seconds;
            public readonly Vector3 Position;
            public readonly float Milliseconds;
            public readonly double GpuMilliseconds;
            public readonly double MainMilliseconds;

            public Frame(string segment, float seconds, Vector3 position, float milliseconds, double gpu, double main)
            {
                Segment = segment;
                Seconds = seconds;
                Position = position;
                Milliseconds = milliseconds;
                GpuMilliseconds = gpu;
                MainMilliseconds = main;
            }
        }

        readonly struct MixSample
        {
            public readonly float Seconds, Blend, Rain, Ventilation, Rms, Peak;

            public MixSample(float seconds, float blend, float rain, float ventilation, float rms, float peak)
            {
                Seconds = seconds;
                Blend = blend;
                Rain = rain;
                Ventilation = ventilation;
                Rms = rms;
                Peak = peak;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (!Environment.GetCommandLineArgs().Contains("-net92-route-benchmark")) return;
            new GameObject("Transient moving-route QA").AddComponent<RoutePerformanceCapture>();
        }

        readonly List<Frame> frames = new(40000);
        readonly List<MixSample> mix = new(600);
        readonly List<string> checks = new();
        readonly FrameTiming[] timings = new FrameTiming[1];
        readonly float[] audio = new float[512];
        FirstPersonController player;
        HospitalAtmosphere atmosphere;
        AudioSource rain, ventilation;
        float started, audioAt;
        bool failed;
        float travelled;
        string output;

        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-net92-output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Application.persistentDataPath;
            Directory.CreateDirectory(output);

            player = FindAnyObjectByType<FirstPersonController>();
            atmosphere = FindAnyObjectByType<HospitalAtmosphere>();
            rain = GameObject.Find("Rain and wind")?.GetComponent<AudioSource>();
            ventilation = GameObject.Find("Clinical ventilation")?.GetComponent<AudioSource>();
            Check(player && atmosphere && rain && ventilation, "Player, weather and both ambience buses exist.");
            if (failed)
            {
                Finish();
                yield break;
            }

            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            player.enabled = false;
            player.CaptureCursor(false);
            player.Warp(new Vector3(0f, .04f, -36.45f), Quaternion.identity);
            yield return new WaitForSecondsRealtime(10f);
            yield return null;
            started = Time.realtimeSinceStartup;

            yield return Travel("South forecourt", new[] { P(0, -23.0f) });
            yield return Travel("Public intake", new[] { P(0, -19f), P(0, -16f), P(0, -12f) });
            yield return Travel("Reception", new[] { P(0, -5f), P(0, 0) });
            int[] cores = { -32, 32, -32 };
            for (int i = 0; i < cores.Length && !failed; i++)
            {
                yield return Travel($"L{i + 1} concourse", new[] { P(cores[i] + 1.05f, 0) });
                if (!failed) yield return Stair(cores[i], i, true);
            }
            if (!failed)
            {
                yield return Travel("L4 research approach", new[] { P(18f, 0), P(cores[2] + 1.05f, 0) });
            }
            for (int i = cores.Length - 1; i >= 0 && !failed; i--)
            {
                yield return Travel($"L{i + 2} return concourse", new[] { P(cores[i] + 1.05f, 0) });
                if (!failed) yield return Stair(cores[i], i, false);
            }
            if (!failed)
            {
                yield return Travel("Reception return", new[] { P(0, 0), P(0, -5), P(0, -12) });
                yield return Travel("Public intake return", new[] { P(0, -16), P(0, -19), P(0, -23) });
                yield return Travel("South forecourt return", new[] { P(0, -36.45f) });
                Check(player.IsGrounded && Mathf.Abs(player.transform.position.y) < .14f, "Returned to the south forecourt on foot.");
            }

            Check(mix.Any(x => x.Blend < .12f && x.Rain > .40f && x.Ventilation < .05f && x.Rms > .0001f),
                "Audible mixer signal sampled in the outdoor mix.");
            Check(mix.Any(x => x.Blend > .90f && x.Rain < .09f && x.Ventilation > .23f && x.Rms > .0001f),
                "Audible mixer signal sampled in the interior mix.");
            Check(mix.Any(x => x.Blend > .15f && x.Blend < .85f), "Entrance crossfade passed through intermediate mix values.");
            Check(mix.Count > 0 && mix.Max(x => x.Peak) < .98f, "No clipping in sampled listener buffers.");
            Finish();
        }

        static Vector2 P(float x, float z) => new(x, z);

        IEnumerator Stair(int core, int lower, bool up)
        {
            string name = $"L{lower + (up ? 1 : 2)} to L{lower + (up ? 2 : 1)} stair";
            yield return Travel(name, ExplorationRoute.StairLeg(core, up));
            float expected = (lower + (up ? 1 : 0)) * 4.2f;
            Check(player.IsGrounded && Mathf.Abs(player.transform.position.y - expected) < .16f,
                FormattableString.Invariant($"{name} settled at Y {player.transform.position.y:F3} m."));
        }

        IEnumerator Travel(string segment, Vector2[] targets)
        {
            if (failed) yield break;
            foreach (var target in targets)
            {
                float distance = Vector2.Distance(target, P(player.transform.position.x, player.transform.position.z));
                float deadline = Time.realtimeSinceStartup + 5f + distance * .75f;
                while (true)
                {
                    Vector3 position = player.transform.position;
                    Vector2 delta = target - P(position.x, position.z);
                    if (delta.sqrMagnitude <= .074f * .074f) break;
                    if (Time.realtimeSinceStartup > deadline || position.y < -1f)
                    {
                        Check(false, $"{segment}: stuck at {position}, target {target}.");
                        yield break;
                    }

                    float dt = Time.unscaledDeltaTime;
                    var wish = new Vector3(delta.x, 0f, delta.y);
                    var aim = Quaternion.LookRotation(wish);
                    player.transform.rotation = Quaternion.RotateTowards(player.transform.rotation, aim, 300f * dt);
                    HospitalDoor.OpenOnDiagnosticPath(player, wish, false);
                    player.Move(wish * 7f, false, dt);
                    travelled += Vector3.Distance(position, player.transform.position);
                    Record(segment, position, dt);
                    yield return null;
                }
            }
        }

        void Record(string segment, Vector3 position, float dt)
        {
            float elapsed = Time.realtimeSinceStartup - started;
            FrameTimingManager.CaptureFrameTimings();
            bool valid = FrameTimingManager.GetLatestTimings(1, timings) > 0;
            frames.Add(new Frame(segment, elapsed, position, dt * 1000f,
                valid ? timings[0].gpuFrameTime : 0d, valid ? timings[0].cpuMainThreadFrameTime : 0d));
            if (elapsed < audioAt) return;
            audioAt = elapsed + .22f;
            AudioListener.GetOutputData(audio, 0);
            float power = 0f, peak = 0f;
            foreach (float sample in audio)
            {
                power += sample * sample;
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }
            mix.Add(new MixSample(elapsed, atmosphere.InteriorBlend, rain.volume, ventilation.volume,
                Mathf.Sqrt(power / audio.Length), peak));
        }

        void Check(bool passed, string message)
        {
            checks.Add((passed ? "PASS " : "FAIL ") + message);
            failed |= !passed;
        }

        void Finish()
        {
            var results = new List<string>
            {
                $"{SystemInfo.graphicsDeviceName}; {SystemInfo.graphicsDeviceType}; {Screen.width}x{Screen.height}",
                "Segment;Frames;Seconds;AvgFPS;P95FrameMs;P99FrameMs;P95GpuMs;P95MainMs;FramesOver16.67Ms"
            };
            foreach (var group in frames.GroupBy(f => f.Segment).Prepend(frames.GroupBy(_ => "Full route").FirstOrDefault()))
            {
                if (group == null) continue;
                double seconds = group.Sum(x => x.Milliseconds) / 1000d;
                results.Add(FormattableString.Invariant(
                    $"{group.Key};{group.Count()};{seconds:F2};{group.Count() / seconds:F1};{Percentile(group.Select(x => (double)x.Milliseconds), .95):F2};{Percentile(group.Select(x => (double)x.Milliseconds), .99):F2};{Percentile(group.Select(x => x.GpuMilliseconds).Where(v => v > 0), .95):F2};{Percentile(group.Select(x => x.MainMilliseconds).Where(v => v > 0), .95):F2};{group.Count(x => x.Milliseconds > 1000f / 60f)}"));
            }
            File.WriteAllLines(Path.Combine(output, "route_performance.csv"), results);
            File.WriteAllLines(Path.Combine(output, "route_frames.csv"),
                frames.Select(x => FormattableString.Invariant($"{x.Segment};{x.Seconds:F4};{x.Position.x:F3};{x.Position.y:F3};{x.Position.z:F3};{x.Milliseconds:F3};{x.GpuMilliseconds:F3};{x.MainMilliseconds:F3}"))
                    .Prepend("Segment;Seconds;X;Y;Z;FrameMs;GpuMs;MainMs"));
            File.WriteAllLines(Path.Combine(output, "route_audio.csv"),
                mix.Select(x => FormattableString.Invariant($"{x.Seconds:F3};{x.Blend:F4};{x.Rain:F4};{x.Ventilation:F4};{x.Rms:F6};{x.Peak:F6}"))
                    .Prepend("Seconds;InteriorBlend;RainGain;VentilationGain;Rms;Peak"));
            checks.Add(FormattableString.Invariant($"Distance {travelled:F1} m; {frames.Count} frames; {mix.Count} audio samples."));
            File.WriteAllLines(Path.Combine(output, "route_checks.txt"), checks);
            foreach (string check in checks) Debug.Log("NET92 ROUTE | " + check);
            Application.Quit(failed ? 2 : 0);
        }

        static double Percentile(IEnumerable<double> values, double p)
        {
            var sorted = values.OrderBy(v => v).ToArray();
            return sorted.Length == 0 ? -1 : sorted[(int)Math.Ceiling(sorted.Length * p) - 1];
        }
    }
}

