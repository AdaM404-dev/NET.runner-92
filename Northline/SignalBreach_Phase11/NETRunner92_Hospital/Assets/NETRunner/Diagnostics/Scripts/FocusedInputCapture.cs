using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetRunner.Core;
using NetRunner.Hud;
using NetRunner.Player;
using NetRunner.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;

namespace NetRunner.Diagnostics
{
    /// <summary>
    /// The input check of the built game (<c>-net92-input-check</c>), which
    /// needs the window to have focus: feeds real Input System events for
    /// walking, sprinting, the mouse, the cursor, the E key on doors and the
    /// lift, LATCH-07 (Q, E, R), the CCTV (1, the arrows, Enter) and the HUD
    /// along the way, and checks what the game did. Writes one PASS or FAIL line per check, then
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
            HospitalHud hud = FindAnyObjectByType<HospitalHud>();
            VisualElement ui = hud ? hud.GetComponent<UIDocument>().rootVisualElement : null;
            if (ui != null)
            {
                yield return ExplorationHud(player, ui, results);
            }

            yield return Press(keyboard, Key.Digit1, .21f);
            Report(results, !network.Unlocked && !network.Watching && !latch.Active,
                "Focused 1 leaves CCTV locked before the reception breach.");
            if (ui != null)
            {
                Report(results, Shown(ui, "notice") && !Shown(ui, "prompt"), "HUD points the locked shortcut to reception.");
            }

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
            if (ui != null)
            {
                yield return new WaitForSecondsRealtime(.20f);
                Report(results, Shown(ui, "prompt") && !Shown(ui, "notice")
                    && ui.Q<Label>("prompt-key").text == "E" && ui.Q<Label>("prompt-text").text == "INTERAKCIÓ",
                    "The targeted terminal shows the reference E interaction prompt.");
                Capture("02_HUD_Reception.png");
            }

            yield return Press(keyboard, Key.E, .22f);
            Report(results, terminal.IsOpen && interaction.TerminalActive, "Focused E opened the terminal puzzle.");
            if (ui != null)
            {
                Report(results, Shown(ui, "terminal") && !Shown(ui, "world")
                    && ui.Q("route-cells").ElementAt(0).ClassListContains("selected"),
                    "HUD replaces the world controls with the terminal and highlights its first cell.");
                yield return Press(keyboard, Key.Escape, .16f);
                Report(results, !terminal.IsOpen && Cursor.lockState == CursorLockMode.Locked && Shown(ui, "world"),
                    "Escape closes the terminal and returns to captured exploration.");
                yield return Press(keyboard, Key.E, .22f);
            }

            Capture("10_Reception_Puzzle.png");
            yield return Press(keyboard, Key.Enter, .18f);
            Report(results, terminal.IsOpen && !network.Unlocked, "Focused Enter rejected the mismatched signature.");
            if (ui != null)
            {
                Report(results, ui.Q("terminal-status").ClassListContains("fault"), "HUD styles and displays the mismatched signature.");
            }

            yield return Press(keyboard, Key.UpArrow, .06f);
            yield return Press(keyboard, Key.UpArrow, .06f);
            yield return Press(keyboard, Key.LeftArrow, .06f);
            yield return Press(keyboard, Key.DownArrow, .06f);
            yield return Press(keyboard, Key.Enter, .17f);
            Report(results, network.Unlocked && !terminal.IsOpen, "Focused arrows and Enter unlocked CCTV from the terminal.");
            if (ui != null)
            {
                Report(results, Shown(ui, "world") && !Shown(ui, "terminal")
                    && Shown(ui, "goal-history") && ui.Q<Label>("goal-title").text == "Juss fel az emeletre"
                    && ui.Q<Label>("goal-detail").text == "CCTV-kamerák feltörve" && ui.Q<Label>("prompt-key").text == "1",
                    "After unlocking, the next objective and completed row appear and the prompt switches to 1.");
                Capture("03_HUD_Unlocked.png");
            }

            yield return Press(keyboard, Key.Digit1, .82f);
            Report(results, latch.Current == LatchPhase.Ready && network.Watching && network.Selected == 0,
                "Focused 1 opened live camera 01 on the tablet.");
            yield return Press(keyboard, Key.RightArrow, .27f);
            Report(results, network.Selected == 1, "Focused Right switched to the next feed.");
            if (ui != null)
            {
                Report(results, Shown(ui, "viewer") && !Shown(ui, "world") && ui.Q("channels").childCount == network.Count
                    && ui.Q("channels").ElementAt(1).ClassListContains("selected"),
                    "CCTV HUD displays only its controls and tracks the selected live channel.");
            }

            yield return Press(keyboard, Key.LeftArrow, .17f);
            for (int i = 0; i < 3; i++)
            {
                yield return Press(keyboard, Key.RightArrow, .12f);
            }

            Capture("09_LATCH07_Live_CCTV.png");
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
            if (ui != null)
            {
                Report(results, Shown(ui, "world") && !Shown(ui, "viewer"), "Closing the tablet restores the exploration HUD.");
                yield return Press(keyboard, Key.Digit1, .82f);
                yield return Press(keyboard, Key.R, .56f);
                Report(results, Shown(ui, "world") && !Shown(ui, "viewer") && !network.Watching,
                    "Focused R stows live CCTV and restores the exploration HUD.");
            }
        }

        // The exploration HUD at the walk start: reticle, objective card, health and stamina.
        private static IEnumerator ExplorationHud(FirstPersonController player, VisualElement ui, List<string> results)
        {
            player.Warp(WalkStart, Quaternion.identity);
            yield return new WaitForSecondsRealtime(.25f);
            Report(results, Shown(ui, "world") && Shown(ui, "reticle") && !Shown(ui, "prompt"),
                "The reticle stays visible and the interaction prompt is hidden without a target.");
            Report(results, ui.Q<Label>("goal-title").text == "Törd fel a CCTV-kamerákat" && !Shown(ui, "goal-history"),
                "The initial objective is active and the completed row is hidden.");
            Label title = ui.Q<Label>("goal-title");
            float titleWidth = title.MeasureTextSize(title.text, 0, VisualElement.MeasureMode.Undefined,
                0, VisualElement.MeasureMode.Undefined).x;
            Report(results, titleWidth <= title.contentRect.width
                && title.layout.xMax <= ui.Q("goal").contentRect.width * .70f,
                "The longest active objective fits inside the angled mission card.");
            Report(results, ui.Q("health-bar").childCount == 18 && ui.Q("stamina-bar").childCount == 18
                && ui.Q<Label>("health-value").text == "100",
                "The reference HP and stamina readouts have 18 segments and show full health.");
            VisualElement reticle = ui.Q("reticle");
            Report(results, Vector2.Distance(reticle.worldBound.center, ui.worldBound.center) < 1f
                && Mathf.Approximately(reticle.resolvedStyle.width, 40f),
                "The smaller 40-pixel reticle stays centered in reference panel coordinates.");
            Capture("01_HUD_Exterior.png");
        }

        // Whether the HUD element `name` is shown.
        private static bool Shown(VisualElement root, string name)
        {
            return root.Q(name).resolvedStyle.display == DisplayStyle.Flex;
        }

        // Saves a screenshot next to the report.
        private static void Capture(string name)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(OutputPath()), name));
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
