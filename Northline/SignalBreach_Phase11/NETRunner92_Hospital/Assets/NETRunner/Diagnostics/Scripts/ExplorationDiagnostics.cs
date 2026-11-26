#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetRunner.Atmosphere;
using NetRunner.Player;
using NetRunner.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Diagnostics
{
    /// <summary>
    /// The hospital's 95 Play mode checks, which the Northline validator ran:
    /// the player's movement and input, LATCH-07, the CCTV, doors, stairs,
    /// the lift, the K-7 patrols, residual power, the building's collision,
    /// rooms and weather, with pictures of six views. Writes one PASS or FAIL
    /// line per check to playmode_checks.txt and raises <see cref="Completed"/>.
    /// Run by the NUnit test HospitalChecksTests and by PlayModeValidation.
    /// The parts are in ExplorationDiagnostics.Systems, .Latch and .Cctv.
    /// </summary>
    public sealed partial class ExplorationDiagnostics : MonoBehaviour
    {
        private const string ReportFile = "playmode_checks.txt";
        private const string DefaultLayerName = "Default";

        // The player moves in 1/90 s steps, as FirstPersonController does;
        // FirstPersonController's default mouse sensitivity is 0.13° per count.
        private const float Step = 1f / 90f;
        private const float LookSensitivity = .13f;

        // Pictures: 1920 × 1080, taken after the view has settled for 1.08 s.
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;
        private const int CaptureDepth = 24;
        private const float CaptureSettle = 1.08f;

        // A walk towards a point ends within 7.4 cm; the walker pushes at 7,
        // checks for doors every eighth step, and gives up after 80 steps a metre (plus 4 m).
        private const float Arrival = .074f;
        private const float Push = 7f;
        private const int DoorCheckMask = 7;
        private const float StepsPerMetre = 80f;
        private const float SpareMetres = 4f;
        private const float FallenBelow = -1.5f;

        private static readonly Vector3 SouthGate = new Vector3(0, .04f, -36.45f);

        private readonly List<string> log = new List<string>();
        private string output;
        private bool isCapturingImages;
        private FirstPersonController player;
        private HospitalAtmosphere weather;
        private bool failed;
        private float maxStep;
        private int renderedFrames;

        /// <summary>Raised once all checks have run: true when every one passed.</summary>
        public event Action<bool> Completed;

        // Only layer 0: the building's own colliders, not triggers or the viewmodel.
        private static int DefaultLayer => LayerMask.GetMask(DefaultLayerName);

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += CountFrames;
        }

        private IEnumerator Start()
        {
            player = FindAnyObjectByType<FirstPersonController>();
            weather = FindAnyObjectByType<HospitalAtmosphere>();
            Check(player && weather, "Player and atmosphere present in Play Mode.");
            if (!player || !weather)
            {
                Completed?.Invoke(false);
                yield break;
            }

            Check(player.GetComponentsInChildren<Renderer>().Length == 0, "Player has no renderable body.");
            Directory.CreateDirectory(output);
            yield return null;
            if (!Application.isBatchMode)
            {
                yield return InputTest();
            }
            else
            {
                log.Add("Focused keyboard and mouse require a foreground Game or Player window.");
                SprintTest();
            }

            player.enabled = false;
            if (player.TryGetComponent(out LatchViewmodel latch))
            {
                yield return TestLatch(latch);
            }

            if (latch && FindAnyObjectByType<SecurityCctv>() is SecurityCctv network)
            {
                yield return TestCctv(latch, network);
            }

            StairAccess access = FindAnyObjectByType<StairAccess>();
            Coroutine residualPower = null;
            if (access)
            {
                TestClosedSystems();
                residualPower = StartCoroutine(TrackResidualPower());
                yield return TestPatrols();
            }

            player.Warp(SouthGate, Quaternion.identity);
            yield return Capture("01_PlayerSpawn", SouthGate, 0);
            TestGroundsAndBoundaries();
            if (access)
            {
                yield return TestStairsAndLift(access);
            }
            else
            {
                player.Warp(new Vector3(0, .03f, 0), Quaternion.identity);
                Check(Circuit(-32), "Walked levels 1→4→1 using west/east/west, without jumping.");
                player.Warp(new Vector3(0, .03f, 0), Quaternion.identity);
                Check(Circuit(32), "Walked the complementary east/west/east circuit in both directions.");
            }

            Check(maxStep < .23f, $"Largest vertical delta per 1/90-second motion step {maxStep:F3} m.");
            TestBuilding(access);
            yield return TestWeatherAndViews();
            if (residualPower != null)
            {
                yield return residualPower;
            }

            log.Add($"Graphics {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsDeviceType}, VRAM reported {SystemInfo.graphicsMemorySize} MB.");
            log.Add($"Player-camera HDRP begin-render callbacks: {renderedFrames}.");
            File.WriteAllLines(Path.Combine(output, ReportFile), log);
            Completed?.Invoke(!failed);
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= CountFrames;
        }

        /// <summary>
        /// Where the report and pictures go, and whether to take the pictures
        /// (not without a graphics device). Call before the first frame.
        /// </summary>
        public void Configure(string folder, bool isCapturing)
        {
            output = folder;
            isCapturingImages = isCapturing;
        }

        private static Vector2 P(float x, float z)
        {
            return new Vector2(x, z);
        }

        private void CountFrames(ScriptableRenderContext context, Camera camera)
        {
            if (player && player.Eyes == camera)
            {
                renderedFrames++;
            }
        }

        private void Check(bool passed, string text)
        {
            string line = (passed ? "PASS " : "FAIL ") + text;
            Debug.Log("NET92 QA | " + line);
            log.Add(line);
            if (!passed)
            {
                failed = true;
            }
        }

        // The routes in from the gate and the ambulance bay, the gate and the
        // perimeter fence against a sprint, and the ground slab under gravity.
        private void TestGroundsAndBoundaries()
        {
            Check(Travel("Public entrance to reception", new[] { P(0, -24), P(0, -19.0f), P(0, -16.0f), P(0, -5.0f), P(0, 0) }),
                "Continuous exterior-to-reception route.");
            player.Warp(new Vector3(-22, .04f, 23.6f), Quaternion.Euler(0, 180, 0));
            Check(Travel("Ambulance vestibule", new[] { P(-22, 20.4f), P(-22, 18.3f), P(-22, 16.25f), P(-22, 15.10f) }),
                "Ambulance apron to emergency department.");
            player.Warp(new Vector3(0, .03f, -38.4f), Quaternion.identity);
            for (int i = 0; i < 160; i++)
            {
                player.Move(Vector3.back, true, Step);
            }

            Check(player.transform.position.z > -40.08f && player.IsGrounded, "Closed gate blocks outward sprint.");
            player.Warp(new Vector3(48, .02f, 8), Quaternion.identity);
            for (int i = 0; i < 185; i++)
            {
                player.Move(Vector3.right, true, Step);
            }

            Check(player.transform.position.x < 52.02f && player.IsGrounded, "Perimeter blocks lateral sprint.");
            player.Warp(new Vector3(-6, .72f, 0), Quaternion.identity);
            for (int i = 0; i < 90; i++)
            {
                player.Move(Vector3.zero, false, Step);
            }

            Check(player.IsGrounded && Mathf.Abs(player.transform.position.y) < .09f, "Gravity settles onto the original ground slab.");
        }

        // The four concourse slabs, every room reachable, the reception desk and
        // a concourse wall as obstacles, and the see-through materials.
        private void TestBuilding(StairAccess access)
        {
            player.Warp(new Vector3(-41, .03f, -31), Quaternion.identity);
            Physics.SyncTransforms();
            int floorHits = 0;
            for (int f = 0; f < 4; f++)
            {
                if (Physics.Raycast(new Vector3(0, f * StairAccess.FloorHeight + 2f, 0), Vector3.down, out RaycastHit hit, 2.6f, DefaultLayer,
                        QueryTriggerInteraction.Ignore)
                    && Mathf.Abs(hit.point.y - f * StairAccess.FloorHeight) < .05f)
                {
                    floorHits++;
                }
                else
                {
                    Debug.LogWarning($"NET92 FLOOR {f + 1}: {hit.collider?.name} at {hit.point.y}");
                }
            }

            Check(floorHits == 4, "Four concourse slabs align at 0.0, 4.2, 8.4 and 12.6 m.");
            player.Warp(new Vector3(-41, .03f, -31), Quaternion.identity);
            if (access)
            {
                OpenRoomDoorsForSweep();
            }

            string rooms = RoomReachability.Probe();
            Check(!rooms.Contains("Missing:"), "Room sweep: " + rooms);
            int transparent = FindObjectsByType<MeshRenderer>().SelectMany(r => r.sharedMaterials)
                .Distinct().Count(m => m && m.HasProperty("_SurfaceType") && m.GetFloat("_SurfaceType") > .5f);
            bool desk = Physics.Raycast(new Vector3(-6.39f, .73f, -8.8f), Vector3.forward, out RaycastHit deskHit, 3.5f, DefaultLayer,
                QueryTriggerInteraction.Ignore);
            Check(desk && deskHit.collider.name.Contains("Reception_Counter"), "Major reception desk blocks chest-height movement.");
            bool wall = Physics.Raycast(new Vector3(-8f, 1.05f, 0), Vector3.back, out RaycastHit wallHit, 3.1f, DefaultLayer,
                QueryTriggerInteraction.Ignore);
            Check(wall && wallHit.collider.name.Contains("_Wall_"), "Concourse wall blocks a lateral route.");
            Check(transparent >= 2, $"{transparent} distinct transparent window/water materials.");
        }

        // Rain outdoors and its rejection under roofs, the interior ambience,
        // and pictures of the weather and the four floors.
        private IEnumerator TestWeatherAndViews()
        {
            int rainBefore = weather.EmittedRain;
            int coverBefore = weather.RejectedByCover;
            yield return Capture("02_ExteriorWeather", new Vector3(0, .03f, -25.6f), 0);
            Check(weather.EmittedRain > rainBefore, $"Rain emitted outdoors ({weather.EmittedRain - rainBefore} new drops).");
            Check(weather.RejectedByCover > coverBefore, $"Roof-height rejection registered ({weather.RejectedByCover - coverBefore} probes).");
            yield return Capture("03_Reception", new Vector3(-1, .03f, -13.4f), 337);
            Check(weather.InteriorBlend > .80f, $"Interior ambience blend reached {weather.InteriorBlend:F2}.");
            yield return Capture("04_Patient_Ward", new Vector3(-24, 4.23f, 0), 90);
            yield return Capture("05_Surgery", new Vector3(7, 8.43f, 0), 270);
            yield return Capture("06_Research", new Vector3(6, 12.63f, 0), 90);
        }

        // Real Input System events: W walks, the mouse turns, Escape frees the
        // cursor and a click captures it again. Needs a window with focus.
        private IEnumerator InputTest()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
            player.CaptureCursor(true);
            yield return null;
            Vector3 start = player.transform.position;
            for (int i = 0; i < 22; i++)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return null;
            }

            Check(player.transform.position.z > start.z + .06f, $"WASD forward Input System event advanced {player.transform.position.z - start.z:F2} m.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            float yaw = player.transform.eulerAngles.y;
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(55, -12));
            yield return null;
            yield return null;
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw, player.transform.eulerAngles.y)) > .1f, "Mouse delta rotated the first-person view.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            Check(Cursor.lockState == CursorLockMode.None, "Escape released the cursor.");
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            Check(Cursor.lockState == CursorLockMode.Locked, "Left click captured the cursor.");
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            player.CaptureCursor(false);
            SprintTest();
        }

        private void SprintTest()
        {
            player.Warp(new Vector3(-10, .02f, -35.65f), Quaternion.identity);
            Vector3 original = player.transform.position;
            for (int i = 0; i < 90; i++)
            {
                player.Move(Vector3.right, false, Step);
            }

            float walk = player.transform.position.x - original.x;
            player.Warp(original, Quaternion.identity);
            for (int i = 0; i < 90; i++)
            {
                player.Move(Vector3.right, true, Step);
            }

            float run = player.transform.position.x - original.x;
            Check(run > walk + 0.85f, $"Sprint/normal travel in one second {run:F2}/{walk:F2} m.");
        }

        // Up the stairs at `first`, the other core, then `first` again, and back down.
        private bool Circuit(int first)
        {
            int[] order = { first, -first, first };
            for (int i = 0; i < 3; i++)
            {
                if (!Connect(order[i], i, true))
                {
                    return false;
                }
            }

            for (int i = 2; i >= 0; i--)
            {
                if (!Connect(order[i], i, false))
                {
                    return false;
                }
            }

            return true;
        }

        private bool Connect(int core, int baseLevel, bool isUp)
        {
            Vector2[] path = ExplorationRoute.StairLeg(core, isUp);
            string name = $"{(core < 0 ? "West" : "East")} core L{baseLevel + (isUp ? 1 : 2)} to L{baseLevel + (isUp ? 2 : 1)}";
            bool traveled = Travel(name, path);
            float expected = (baseLevel + (isUp ? 1 : 0)) * StairAccess.FloorHeight;
            bool isLevel = Mathf.Abs(player.transform.position.y - expected) < .16f && player.IsGrounded;
            Check(traveled && isLevel, $"{name}; settled Y={player.transform.position.y:F3}, expected {expected:F2}.");
            return traveled && isLevel;
        }

        // Walks the player through `path` in 1/90 s steps, opening doors ahead,
        // and records the largest vertical jump per step.
        private bool Travel(string label, IEnumerable<Vector2> path)
        {
            foreach (Vector2 target in path)
            {
                Vector3 start = player.transform.position;
                int cap = Mathf.CeilToInt((Vector2.Distance(target, new Vector2(start.x, start.z)) + SpareMetres) * StepsPerMetre);
                bool hasArrived = false;
                for (int step = 0; step < cap; step++)
                {
                    Vector3 position = player.transform.position;
                    Vector2 delta = target - new Vector2(position.x, position.z);
                    if (delta.magnitude < Arrival)
                    {
                        hasArrived = true;
                        break;
                    }

                    if ((step & DoorCheckMask) == 0)
                    {
                        HospitalDoor.OpenOnDiagnosticPath(player, new Vector3(delta.x, 0, delta.y), true);
                    }

                    player.Move(new Vector3(delta.x, 0, delta.y) * Push, false, Step);
                    maxStep = Mathf.Max(maxStep, Mathf.Abs(player.transform.position.y - position.y));
                    if (player.transform.position.y < FallenBelow)
                    {
                        break;
                    }
                }

                if (!hasArrived)
                {
                    Debug.LogWarning($"NET92 STUCK {label}: target {target}; player {player.transform.position}");
                    return false;
                }
            }

            return true;
        }

        private IEnumerator Capture(string label, Vector3 position, float yaw, bool isStill = false)
        {
            player.Warp(position, Quaternion.Euler(0, yaw, 0));
            float until = Time.realtimeSinceStartup + CaptureSettle;
            while (Time.realtimeSinceStartup < until)
            {
                player.Move(Vector3.zero, false, Step);
                yield return null;
            }

            CaptureCamera(label, isStill);
        }

        // Renders the player camera into a 1920 × 1080 picture; a still one
        // without temporal anti-aliasing, so it is not blurred by the warp.
        private void CaptureCamera(string label, bool isStill = false)
        {
            if (!isCapturingImages)
            {
                return;
            }

            Camera camera = player.Eyes;
            HDAdditionalCameraData hdrp = camera.GetComponent<HDAdditionalCameraData>();
            HDAdditionalCameraData.AntialiasingMode antialiasing = hdrp.antialiasing;
            if (isStill)
            {
                hdrp.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                HDCamera.GetOrCreate(camera).Reset();
            }

            var target = new RenderTexture(CaptureWidth, CaptureHeight, CaptureDepth, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                var picture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                picture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                picture.Apply();
                File.WriteAllBytes(Path.Combine(output, label + ".png"), picture.EncodeToPNG());
                RenderTexture.active = previous;
                Destroy(picture);
            }

            target.Release();
            Destroy(target);
            hdrp.antialiasing = antialiasing;
        }
    }
}
#endif
