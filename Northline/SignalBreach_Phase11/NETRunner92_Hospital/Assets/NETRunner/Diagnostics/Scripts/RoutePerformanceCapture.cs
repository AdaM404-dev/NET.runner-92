using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetRunner.Atmosphere;
using NetRunner.Core;
using NetRunner.Player;
using NetRunner.World;
using UnityEngine;

namespace NetRunner.Diagnostics
{
    /// <summary>
    /// The route benchmark of the built game (<c>-net92-route-benchmark</c>):
    /// walks the player from the south gate up the stairs the first ascent
    /// requires to L4 and back, opening doors on the way, and records every
    /// frame's time and the ambience mix. Writes route_performance.csv (per
    /// segment and the full route), route_frames.csv, route_audio.csv and
    /// route_checks.txt, then quits with 0, or 2 when a check failed (for
    /// example the walker got stuck). See docs/systems/performance.md.
    /// </summary>
    public sealed class RoutePerformanceCapture : MonoBehaviour
    {
        /// <summary>One measured frame.</summary>
        private readonly struct Frame
        {
            public Frame(string segment, float seconds, Vector3 position, float milliseconds, double gpu, double main)
            {
                Segment = segment;
                Seconds = seconds;
                Position = position;
                Milliseconds = milliseconds;
                GpuMilliseconds = gpu;
                MainMilliseconds = main;
            }

            public string Segment { get; }

            public float Seconds { get; }

            public Vector3 Position { get; }

            public float Milliseconds { get; }

            public double GpuMilliseconds { get; }

            public double MainMilliseconds { get; }
        }

        /// <summary>One sample of the ambience mix and the listener's output.</summary>
        private readonly struct MixSample
        {
            public MixSample(float seconds, float blend, float rain, float ventilation, float rms, float peak)
            {
                Seconds = seconds;
                Blend = blend;
                Rain = rain;
                Ventilation = ventilation;
                Rms = rms;
                Peak = peak;
            }

            public float Seconds { get; }

            public float Blend { get; }

            public float Rain { get; }

            public float Ventilation { get; }

            public float Rms { get; }

            public float Peak { get; }
        }

        private const string Option = "-net92-route-benchmark";
        private const string OutputOption = "-net92-output";
        private const string RainSource = "Rain and wind";
        private const string VentilationSource = "Clinical ventilation";
        private const int FailedExitCode = 2;
        private const int Uncapped = -1;
        private const int Legs = 3;

        // The most one frame counts towards a leg's walking time: a stall of the
        // whole computer (a screensaver, disk work) is not taken for the walker
        // being stuck. The movement itself is capped the same way (FirstPersonController.Move).
        private const float MaxCountedFrame = .1f;

        // The walk: a warm-up at the gate, then towards each point at a
        // walking push, turning at most 300° a second, until within 7.4 cm.
        // A point allows 5 s plus 0.75 s per metre before the walker counts as stuck.
        private const float WarmUp = 10f;
        private const float Push = 7f;
        private const float TurnRate = 300f;
        private const float Arrival = .074f;
        private const float AllowedBase = 5f;
        private const float AllowedPerMetre = .75f;
        private const float FallenBelow = -1f;
        private const float LevelTolerance = .16f;
        private const float GroundTolerance = .14f;

        // The stair core a concourse leg ends beside, and the ambience sampling interval.
        private const float CoreApproach = 1.05f;
        private const float AudioInterval = .22f;
        private const int AudioBuffer = 512;

        private const float MillisecondsPerSecond = 1000f;
        private const double Percentile95 = .95;
        private const double Percentile99 = .99;
        private const float SixtyFpsFrame = 1000f / 60f;

        private static readonly Vector3 SouthGate = new Vector3(0f, .04f, -36.45f);

        private readonly List<Frame> frames = new List<Frame>(40000);
        private readonly List<MixSample> mix = new List<MixSample>(600);
        private readonly List<string> checks = new List<string>();
        private readonly FrameTiming[] timings = new FrameTiming[1];
        private readonly float[] audio = new float[AudioBuffer];
        private FirstPersonController player;
        private HospitalAtmosphere atmosphere;
        private AudioSource rain;
        private AudioSource ventilation;
        private float started;
        private float audioAt;
        private bool failed;
        private float travelled;
        private string output;

        private IEnumerator Start()
        {
            output = LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), OutputOption) ?? Application.persistentDataPath;
            Directory.CreateDirectory(output);
            player = FindAnyObjectByType<FirstPersonController>();
            atmosphere = FindAnyObjectByType<HospitalAtmosphere>();
            rain = GameObject.Find(RainSource)?.GetComponent<AudioSource>();
            ventilation = GameObject.Find(VentilationSource)?.GetComponent<AudioSource>();
            Check(player && atmosphere && rain && ventilation, "Player, weather and both ambience buses exist.");
            if (failed)
            {
                Finish();
                yield break;
            }

            Application.targetFrameRate = Uncapped;
            QualitySettings.vSyncCount = 0;
            player.enabled = false;
            player.CaptureCursor(false);
            player.Warp(SouthGate, Quaternion.identity);
            yield return new WaitForSecondsRealtime(WarmUp);
            yield return null;
            started = Time.realtimeSinceStartup;

            yield return Travel("South forecourt", new[] { P(0, -23.0f) });
            yield return Travel("Public intake", new[] { P(0, -19f), P(0, -16f), P(0, -12f) });
            yield return Travel("Reception", new[] { P(0, -5f), P(0, 0) });
            for (int leg = 0; leg < Legs && !failed; leg++)
            {
                yield return Travel($"L{leg + 1} concourse", new[] { P(StairRules.CoreForLeg(leg) + CoreApproach, 0) });
                if (!failed)
                {
                    yield return Stair(StairRules.CoreForLeg(leg), leg, true);
                }
            }

            if (!failed)
            {
                yield return Travel("L4 research approach", new[] { P(18f, 0), P(StairRules.CoreForLeg(Legs - 1) + CoreApproach, 0) });
            }

            for (int leg = Legs - 1; leg >= 0 && !failed; leg--)
            {
                yield return Travel($"L{leg + 2} return concourse", new[] { P(StairRules.CoreForLeg(leg) + CoreApproach, 0) });
                if (!failed)
                {
                    yield return Stair(StairRules.CoreForLeg(leg), leg, false);
                }
            }

            if (!failed)
            {
                yield return Travel("Reception return", new[] { P(0, 0), P(0, -5), P(0, -12) });
                yield return Travel("Public intake return", new[] { P(0, -16), P(0, -19), P(0, -23) });
                yield return Travel("South forecourt return", new[] { P(0, SouthGate.z) });
                Check(player.IsGrounded && Mathf.Abs(player.transform.position.y) < GroundTolerance, "Returned to the south forecourt on foot.");
            }

            Check(mix.Any(x => x.Blend < .12f && x.Rain > .40f && x.Ventilation < .05f && x.Rms > .0001f),
                "Audible mixer signal sampled in the outdoor mix.");
            Check(mix.Any(x => x.Blend > .90f && x.Rain < .09f && x.Ventilation > .23f && x.Rms > .0001f),
                "Audible mixer signal sampled in the interior mix.");
            Check(mix.Any(x => x.Blend > .15f && x.Blend < .85f), "Entrance crossfade passed through intermediate mix values.");
            Check(mix.Count > 0 && mix.Max(x => x.Peak) < .98f, "No clipping in sampled listener buffers.");
            Finish();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (LaunchArguments.Has(Environment.GetCommandLineArgs(), Option))
            {
                new GameObject("Transient moving-route QA").AddComponent<RoutePerformanceCapture>();
            }
        }

        private static Vector2 P(float x, float z)
        {
            return new Vector2(x, z);
        }

        private IEnumerator Stair(int core, int lower, bool isUp)
        {
            string name = $"L{lower + (isUp ? 1 : 2)} to L{lower + (isUp ? 2 : 1)} stair";
            yield return Travel(name, ExplorationRoute.StairLeg(core, isUp));
            float expected = (lower + (isUp ? 1 : 0)) * StairAccess.FloorHeight;
            Check(player.IsGrounded && Mathf.Abs(player.transform.position.y - expected) < LevelTolerance,
                FormattableString.Invariant($"{name} settled at Y {player.transform.position.y:F3} m."));
        }

        private IEnumerator Travel(string segment, Vector2[] targets)
        {
            if (failed)
            {
                yield break;
            }

            foreach (Vector2 target in targets)
            {
                float distance = Vector2.Distance(target, P(player.transform.position.x, player.transform.position.z));
                float allowed = AllowedBase + distance * AllowedPerMetre;
                float walked = 0f;
                while (true)
                {
                    Vector3 position = player.transform.position;
                    Vector2 delta = target - P(position.x, position.z);
                    if (delta.sqrMagnitude <= Arrival * Arrival)
                    {
                        break;
                    }

                    if (walked > allowed || position.y < FallenBelow)
                    {
                        Check(false, $"{segment}: stuck at {position}, target {target}.");
                        yield break;
                    }

                    float dt = Time.unscaledDeltaTime;
                    walked += Mathf.Min(dt, MaxCountedFrame);
                    var wish = new Vector3(delta.x, 0f, delta.y);
                    Quaternion aim = Quaternion.LookRotation(wish);
                    player.transform.rotation = Quaternion.RotateTowards(player.transform.rotation, aim, TurnRate * dt);

                    // Open doors at once, as the hospital checks do: a leaf that is still
                    // swinging when the walker arrives shoves it aside and traps it.
                    HospitalDoor.OpenOnDiagnosticPath(player, wish, true);
                    player.Move(wish * Push, false, dt);
                    travelled += Vector3.Distance(position, player.transform.position);
                    Record(segment, position, dt);
                    yield return null;
                }
            }
        }

        private void Record(string segment, Vector3 position, float dt)
        {
            float elapsed = Time.realtimeSinceStartup - started;
            FrameTimingManager.CaptureFrameTimings();
            bool isValid = FrameTimingManager.GetLatestTimings(1, timings) > 0;
            frames.Add(new Frame(segment, elapsed, position, dt * MillisecondsPerSecond,
                isValid ? timings[0].gpuFrameTime : 0d, isValid ? timings[0].cpuMainThreadFrameTime : 0d));
            if (elapsed < audioAt)
            {
                return;
            }

            audioAt = elapsed + AudioInterval;
            AudioListener.GetOutputData(audio, 0);
            float power = 0f;
            float peak = 0f;
            foreach (float sample in audio)
            {
                power += sample * sample;
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            mix.Add(new MixSample(elapsed, atmosphere.InteriorBlend, rain.volume, ventilation.volume,
                Mathf.Sqrt(power / audio.Length), peak));
        }

        private void Check(bool passed, string message)
        {
            checks.Add((passed ? "PASS " : "FAIL ") + message);
            failed |= !passed;
        }

        private void Finish()
        {
            var results = new List<string>
            {
                $"{SystemInfo.graphicsDeviceName}; {SystemInfo.graphicsDeviceType}; {Screen.width}x{Screen.height}",
                "Segment;Frames;Seconds;AvgFPS;P95FrameMs;P99FrameMs;P95GpuMs;P95MainMs;FramesOver16.67Ms",
            };
            foreach (IGrouping<string, Frame> group in frames.GroupBy(f => f.Segment).Prepend(frames.GroupBy(_ => "Full route").FirstOrDefault()))
            {
                if (group == null)
                {
                    continue;
                }

                results.Add(SegmentLine(group));
            }

            File.WriteAllLines(Path.Combine(output, "route_performance.csv"), results);
            File.WriteAllLines(Path.Combine(output, "route_frames.csv"),
                frames.Select(x => FormattableString.Invariant(
                    $"{x.Segment};{x.Seconds:F4};{x.Position.x:F3};{x.Position.y:F3};{x.Position.z:F3};{x.Milliseconds:F3};{x.GpuMilliseconds:F3};{x.MainMilliseconds:F3}"))
                    .Prepend("Segment;Seconds;X;Y;Z;FrameMs;GpuMs;MainMs"));
            File.WriteAllLines(Path.Combine(output, "route_audio.csv"),
                mix.Select(x => FormattableString.Invariant($"{x.Seconds:F3};{x.Blend:F4};{x.Rain:F4};{x.Ventilation:F4};{x.Rms:F6};{x.Peak:F6}"))
                    .Prepend("Seconds;InteriorBlend;RainGain;VentilationGain;Rms;Peak"));
            checks.Add(FormattableString.Invariant($"Distance {travelled:F1} m; {frames.Count} frames; {mix.Count} audio samples."));
            File.WriteAllLines(Path.Combine(output, "route_checks.txt"), checks);
            foreach (string check in checks)
            {
                Debug.Log("NET92 ROUTE | " + check);
            }

            Application.Quit(failed ? FailedExitCode : 0);
        }

        private static string SegmentLine(IGrouping<string, Frame> group)
        {
            double seconds = group.Sum(x => x.Milliseconds) / 1000d;
            IEnumerable<double> times = group.Select(x => (double)x.Milliseconds);
            return FormattableString.Invariant(
                $"{group.Key};{group.Count()};{seconds:F2};{group.Count() / seconds:F1};{SampleStatistics.Percentile(times, Percentile95):F2};{SampleStatistics.Percentile(times, Percentile99):F2};{SampleStatistics.Percentile(group.Select(x => x.GpuMilliseconds).Where(v => v > 0), Percentile95):F2};{SampleStatistics.Percentile(group.Select(x => x.MainMilliseconds).Where(v => v > 0), Percentile95):F2};{group.Count(x => x.Milliseconds > SixtyFpsFrame)}");
        }
    }
}
