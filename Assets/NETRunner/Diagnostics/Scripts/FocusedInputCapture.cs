using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetRunner.Core;
using NetRunner.Player;
using NetRunner.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NetRunner.Diagnostics
{
    /// <summary>
    /// The input check of the built game (<c>-net92-input-check</c>), which
    /// needs the window to have focus: feeds real Input System events for
    /// walking, sprinting, the mouse, the cursor, the E key on doors and the
    /// lift, LATCH-07 (Q, E, R) and the CCTV (1, the arrows, Enter), and
    /// checks what the game did. Writes one PASS or FAIL line per check, then
    /// quits with 0, 2 when a check failed, or 3 without a player or focus.
    /// </summary>
    public sealed class FocusedInputCapture : MonoBehaviour
    {
        private const string Option = "-net92-input-check";
        private const string OutputOption = "-net92-output";
        private const string DefaultFile = "input_checks.txt";
        private const string LiftLanding = "F01_D25_Lift_Lobby";
        private const int FailedExitCode = 2;
        private const int UnavailableExitCode = 3;

        // Settling and how long to wait for focus; how long each walk key is held.
        private const float StartupSettle = 4f;
        private const float FocusWait = 8f;
        private const float HoldTime = .50f;

        // A walk must cover 30 cm in its direction and drift at most 8 cm sideways;
        // a diagonal at most 15% farther than straight, a sprint 27% farther.
        private const float MinimumWalk = .30f;
        private const float MaximumDrift = .08f;
        private const float DiagonalAllowance = 1.15f;
        private const float SprintGain = 1.27f;

        // Aiming: FirstPersonController's default mouse sensitivity (degrees per
        // count), and how far in front of the relay and terminal the player stands.
        private const float LookSensitivity = .13f;
        private const float RelayDistance = 1.48f;
        private const float TerminalDistance = 1.88f;
        private const float LiftCallPitch = -82f;
        private const float TurnThreshold = .1f;

        private static readonly Vector3 WalkStart = new Vector3(0f, .04f, -34.3f);
        private static readonly Vector3 EntranceView = new Vector3(0f, .04f, -20.65f);
        private static readonly Vector3 LiftCallView = new Vector3(-.88f, .034f, 4.55f);
        private static readonly Vector2 MouseTurn = new Vector2(65, -10);

        private IEnumerator Start()
        {
            FirstPersonController player = FindAnyObjectByType<FirstPersonController>();
            var results = new List<string>();
            if (!player)
            {
                Application.Quit(UnavailableExitCode);
                yield break;
            }

            yield return new WaitForSecondsRealtime(StartupSettle);
            float limit = Time.realtimeSinceStartup + FocusWait;
            while (!Application.isFocused && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            if (!Application.isFocused)
            {
                Save(new[] { "FAIL Focused player window was unavailable." });
                Application.Quit(UnavailableExitCode);
                yield break;
            }

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
            player.CaptureCursor(true);
            yield return null;

            yield return Walking(player, keyboard, results);
            yield return MouseAndCursor(player, keyboard, mouse, results);
            if (player.TryGetComponent(out HospitalInteraction interaction))
            {
                yield return Interaction(player, interaction, keyboard, results);
            }

            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            player.CaptureCursor(false);
            Save(results);
            Application.Quit(results.Any(r => r.StartsWith("FAIL")) ? FailedExitCode : 0);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (LaunchArguments.Has(Environment.GetCommandLineArgs(), Option))
            {
                new GameObject("Transient focused input check").AddComponent<FocusedInputCapture>();
            }
        }

        private static IEnumerator Walking(FirstPersonController player, Keyboard keyboard, List<string> results)
        {
            float walkDistance = 0f;
            float diagonalDistance = 0f;
            float sprintDistance = 0f;
            yield return Move(player, keyboard, new[] { Key.W }, Vector3.forward, "W", results, distance => walkDistance = distance);
            yield return Move(player, keyboard, new[] { Key.A }, Vector3.left, "A", results);
            yield return Move(player, keyboard, new[] { Key.S }, Vector3.back, "S", results);
            yield return Move(player, keyboard, new[] { Key.D }, Vector3.right, "D", results);
            yield return Move(player, keyboard, new[] { Key.W, Key.D }, (Vector3.forward + Vector3.right).normalized,
                "W+D", results, distance => diagonalDistance = distance);
            yield return Move(player, keyboard, new[] { Key.W, Key.LeftShift }, Vector3.forward, "Shift+W", results,
                distance => sprintDistance = distance);
            Report(results, diagonalDistance <= walkDistance * DiagonalAllowance, "Diagonal walking is normalized.");
            Report(results, sprintDistance > walkDistance * SprintGain, "Sprint input increases travel speed.");
            player.Warp(WalkStart, Quaternion.identity);
            yield return null;
        }

        private static IEnumerator MouseAndCursor(FirstPersonController player, Keyboard keyboard, Mouse mouse, List<string> results)
        {
            float yaw = player.transform.eulerAngles.y;
            InputSystem.QueueDeltaStateEvent(mouse.delta, MouseTurn);
            yield return null;
            yield return null;
            Report(results, Mathf.Abs(Mathf.DeltaAngle(yaw, player.transform.eulerAngles.y)) > TurnThreshold,
                "Focused mouse delta rotated the camera.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            Report(results, Cursor.lockState == CursorLockMode.None, "Escape released the cursor.");
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            Report(results, Cursor.lockState == CursorLockMode.Locked, "Left click restored cursor lock.");
        }

        private static IEnumerator Interaction(FirstPersonController player, HospitalInteraction interaction, Keyboard keyboard,
            List<string> results)
        {
            player.Warp(EntranceView, Quaternion.identity);
            Physics.SyncTransforms();
            var entrance = interaction.TargetInView() as HospitalDoor;
            Report(results, entrance && entrance.Closed, "The public entrance starts closed and can be targeted.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSecondsRealtime(.85f);
            Report(results, entrance && entrance.Open, "Focused E opened the public entrance.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSecondsRealtime(.85f);
            Report(results, entrance && entrance.Closed, "Focused E closed the public entrance again.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            player.Warp(LiftCallView, Quaternion.identity);
            player.Look(new Vector2(0, LiftCallPitch));
            Physics.SyncTransforms();
            var call = interaction.TargetInView() as HospitalLiftButton;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSecondsRealtime(.85f);
            HospitalDoor landing = GameObject.Find(LiftLanding)?.GetComponent<HospitalDoor>();
            Report(results, call && landing && landing.Open, "Focused E on the west call button opened the aligned lift landing.");
            if (player.TryGetComponent(out LatchViewmodel latch))
            {
                yield return LatchInput(player, interaction, latch, keyboard, results);
                if (FindAnyObjectByType<SecurityCctv>() is SecurityCctv network)
                {
                    yield return CctvInput(player, interaction, latch, network, keyboard, results);
                }
            }
        }

        private static IEnumerator LatchInput(FirstPersonController player, HospitalInteraction interaction, LatchViewmodel latch,
            Keyboard keyboard, List<string> results)
        {
            player.Warp(EntranceView, Quaternion.identity);
            Physics.SyncTransforms();
            var door = interaction.TargetInView() as HospitalDoor;
            yield return Press(keyboard, Key.Q, .80f);
            Report(results, latch.Current == LatchPhase.Ready, "Focused Q deployed LATCH-07.");
            yield return Press(keyboard, Key.E, 1.04f);
            Report(results, door && door.Closed && latch.Current == LatchPhase.Failed,
                "Focused E operated the tablet and left the aimed-at world door closed.");
            yield return Press(keyboard, Key.R, .56f);
            Report(results, !latch.Active && !latch.Model.activeSelf, "Focused R canceled and hid LATCH-07.");

            LatchRelay relay = FindAnyObjectByType<LatchRelay>();
            if (!relay)
            {
                Report(results, false, "Could not find the service relay.");
                yield break;
            }

            Vector3 target = relay.transform.position;
            player.Warp(new Vector3(target.x, .04f, target.z - RelayDistance), Quaternion.identity);
            player.Look(new Vector2(0f, -Mathf.Atan2(player.Eyes.transform.position.y - target.y, RelayDistance) * Mathf.Rad2Deg / LookSensitivity));
            Physics.SyncTransforms();
            Report(results, interaction.TargetInView() as UnityEngine.Object == relay, "Focused aim acquired the service relay.");
            yield return Press(keyboard, Key.Q, .82f);
            yield return Press(keyboard, Key.E, 1.05f);
            Report(results, relay.Linked && latch.Current == LatchPhase.Success, "Focused E linked the service relay.");
            yield return new WaitForSecondsRealtime(.76f);
            yield return Press(keyboard, Key.Q, .72f);
            Report(results, !latch.Active && !latch.Model.activeSelf, "Focused Q played unequip and returned to exploration.");
        }

        private static IEnumerator CctvInput(FirstPersonController player, HospitalInteraction interaction, LatchViewmodel latch,
            SecurityCctv network, Keyboard keyboard, List<string> results)
        {
            yield return Press(keyboard, Key.Digit1, .21f);
            Report(results, !network.Unlocked && !network.Watching && !latch.Active,
                "Focused 1 leaves CCTV locked before the reception breach.");
            ReceptionSecurityTerminal terminal = FindAnyObjectByType<ReceptionSecurityTerminal>();
            if (!terminal)
            {
                Report(results, false, "The reception terminal could not be found.");
                yield break;
            }

            Vector3 target = terminal.transform.position;
            player.Warp(new Vector3(target.x, .04f, target.z + TerminalDistance), Quaternion.Euler(0, 180f, 0));
            player.Look(new Vector2(0f, -Mathf.Atan2(player.Eyes.transform.position.y - target.y, TerminalDistance) * Mathf.Rad2Deg / LookSensitivity));
            Physics.SyncTransforms();
            Report(results, interaction.TargetInView() as UnityEngine.Object == terminal, "Focused aim acquired the counter's right monitor.");
            yield return Press(keyboard, Key.E, .22f);
            Report(results, terminal.IsOpen && interaction.TerminalActive, "Focused E opened the terminal puzzle.");
            ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(OutputPath()), "10_Reception_Puzzle.png"));
            yield return Press(keyboard, Key.Enter, .18f);
            Report(results, terminal.IsOpen && !network.Unlocked, "Focused Enter rejected the mismatched signature.");
            yield return Press(keyboard, Key.UpArrow, .06f);
            yield return Press(keyboard, Key.UpArrow, .06f);
            yield return Press(keyboard, Key.LeftArrow, .06f);
            yield return Press(keyboard, Key.DownArrow, .06f);
            yield return Press(keyboard, Key.Enter, .17f);
            Report(results, network.Unlocked && !terminal.IsOpen, "Focused arrows and Enter unlocked CCTV from the terminal.");
            yield return Press(keyboard, Key.Digit1, .82f);
            Report(results, latch.Current == LatchPhase.Ready && network.Watching && network.Selected == 0,
                "Focused 1 opened live camera 01 on the tablet.");
            yield return Press(keyboard, Key.RightArrow, .27f);
            Report(results, network.Selected == 1, "Focused Right switched to the next feed.");
            yield return Press(keyboard, Key.LeftArrow, .17f);
            for (int i = 0; i < 3; i++)
            {
                yield return Press(keyboard, Key.RightArrow, .12f);
            }

            ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(OutputPath()), "09_LATCH07_Live_CCTV.png"));
            for (int i = 0; i < 3; i++)
            {
                yield return Press(keyboard, Key.LeftArrow, .12f);
            }

            yield return Press(keyboard, Key.UpArrow, .17f);
            Report(results, network.Selected == network.Count - 1, "Focused Left and Up wrapped to the last feed.");
            yield return Press(keyboard, Key.DownArrow, .17f);
            Report(results, network.Selected == 0, "Focused Down wrapped to the first feed.");
            yield return Press(keyboard, Key.Digit1, .68f);
            Report(results, !latch.Active && !network.Watching && !network.FeedCamera.enabled,
                "Focused 1 closed CCTV and stopped the feed camera.");
        }

        // A key tap: release everything, press the key for a frame, release, then wait `settle` seconds.
        private static IEnumerator Press(Keyboard keyboard, Key key, float settle)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(settle);
        }

        // Holds `keys` for half a second from the walk start and checks the player moved along `direction`.
        private static IEnumerator Move(FirstPersonController player, Keyboard keyboard, Key[] keys,
            Vector3 direction, string label, List<string> results, Action<float> record = null)
        {
            player.Warp(WalkStart, Quaternion.identity);
            Vector3 start = player.transform.position;
            float started = Time.realtimeSinceStartup;
            do
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
                yield return null;
            }
            while (Time.realtimeSinceStartup - started < HoldTime);

            Vector3 offset = player.transform.position - start;
            offset.y = 0f;
            float distance = Vector3.Dot(offset, direction);
            float sideways = (offset - direction * distance).magnitude;
            Report(results, distance > MinimumWalk && sideways < MaximumDrift,
                FormattableString.Invariant($"Focused {label} advanced {distance:F2} m in the expected direction."));
            record?.Invoke(distance);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static void Report(List<string> lines, bool isPassed, string value)
        {
            lines.Add((isPassed ? "PASS " : "FAIL ") + value);
        }

        private static void Save(IEnumerable<string> results)
        {
            string path = OutputPath();
            File.WriteAllLines(path, results);
            Debug.Log("NET92 FOCUSED INPUT | " + path);
        }

        private static string OutputPath()
        {
            return Path.GetFullPath(LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), OutputOption)
                ?? Path.Combine(Application.persistentDataPath, DefaultFile));
        }
    }
}
