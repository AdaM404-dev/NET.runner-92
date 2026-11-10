using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System;
using NetRunner.Player;
using NetRunner.World;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem;
using UnityEngine;

namespace NetRunner.Diagnostics
{
    public sealed class FocusedInputCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (!Environment.GetCommandLineArgs().Contains("-net92-input-check")) return;
            new GameObject("Transient focused input check").AddComponent<FocusedInputCapture>();
        }

        IEnumerator Start()
        {
            var player = FindAnyObjectByType<FirstPersonController>();
            var results = new List<string>();
            if (!player) { Application.Quit(3); yield break; }
            yield return new WaitForSecondsRealtime(4f);
            float limit = Time.realtimeSinceStartup + 8f;
            while (!Application.isFocused && Time.realtimeSinceStartup < limit) yield return null;
            if (!Application.isFocused)
            {
                Save(new [] { "FAIL Focused player window was unavailable." });
                Application.Quit(3);
                yield break;
            }
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            player.CaptureCursor(true);
            yield return null;

            float walkDistance = 0f, diagonalDistance = 0f, sprintDistance = 0f;
            yield return Move(player, keyboard, new[] { Key.W }, Vector3.forward, "W", results,
                distance => walkDistance = distance);
            yield return Move(player, keyboard, new[] { Key.A }, Vector3.left, "A", results);
            yield return Move(player, keyboard, new[] { Key.S }, Vector3.back, "S", results);
            yield return Move(player, keyboard, new[] { Key.D }, Vector3.right, "D", results);
            yield return Move(player, keyboard, new[] { Key.W, Key.D }, (Vector3.forward + Vector3.right).normalized,
                "W+D", results, distance => diagonalDistance = distance);
            yield return Move(player, keyboard, new[] { Key.W, Key.LeftShift }, Vector3.forward, "Shift+W", results,
                distance => sprintDistance = distance);
            Report(results, diagonalDistance <= walkDistance * 1.15f, "Diagonal walking is normalized.");
            Report(results, sprintDistance > walkDistance * 1.27f, "Sprint input increases travel speed.");
            player.Warp(new Vector3(0f, .04f, -34.3f), Quaternion.identity);
            yield return null;

            float yaw=player.transform.eulerAngles.y;
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(65,-10));
            yield return null;
            yield return null;
            Report(results,Mathf.Abs(Mathf.DeltaAngle(yaw,player.transform.eulerAngles.y))>.1f,"Focused mouse delta rotated the camera.");

            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            Report(results,Cursor.lockState==CursorLockMode.None,"Escape released the cursor.");
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            Report(results,Cursor.lockState==CursorLockMode.Locked,"Left click restored cursor lock.");
            if (player.TryGetComponent<HospitalInteraction>(out var interaction))
            {
                player.Warp(new Vector3(0f, .04f, -20.65f), Quaternion.identity);
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
                player.Warp(new Vector3(-.88f, .034f, 4.55f), Quaternion.identity);
                player.Look(new Vector2(0, -82f));
                Physics.SyncTransforms();
                var call = interaction.TargetInView() as HospitalLiftButton;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                yield return new WaitForSecondsRealtime(.85f);
                var landing = GameObject.Find("F01_D25_Lift_Lobby")?.GetComponent<HospitalDoor>();
                Report(results, call && landing && landing.Open, "Focused E on the west call button opened the aligned lift landing.");
                if (player.TryGetComponent<LatchViewmodel>(out var latch))
                {
                    yield return LatchInput(player, interaction, latch, keyboard, results);
                    if (FindAnyObjectByType<SecurityCctv>() is SecurityCctv network)
                        yield return CctvInput(player, interaction, latch, network, keyboard, results);
                }
            }
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            player.CaptureCursor(false);
            Save(results);
            Application.Quit(results.Any(r=>r.StartsWith("FAIL"))?2:0);
        }

        static IEnumerator LatchInput(FirstPersonController player, HospitalInteraction interaction, LatchViewmodel latch,
            Keyboard keyboard, List<string> results)
        {
            player.Warp(new Vector3(0f, .04f, -20.65f), Quaternion.identity);
            Physics.SyncTransforms();
            var door = interaction.TargetInView() as HospitalDoor;
            yield return Press(keyboard, Key.Q, .80f);
            Report(results, latch.Current == LatchViewmodel.Phase.Ready, "Focused Q deployed LATCH-07.");
            yield return Press(keyboard, Key.E, 1.04f);
            Report(results, door && door.Closed && latch.Current == LatchViewmodel.Phase.Failed,
                "Focused E operated the tablet and left the aimed-at world door closed.");
            yield return Press(keyboard, Key.R, .56f);
            Report(results, !latch.Active && !latch.Model.activeSelf, "Focused R canceled and hid LATCH-07.");

            var relay = FindAnyObjectByType<LatchRelay>();
            if (!relay)
            {
                Report(results, false, "Could not find the service relay.");
                yield break;
            }
            Vector3 target = relay.transform.position;
            player.Warp(new Vector3(target.x, .04f, target.z - 1.48f), Quaternion.identity);
            player.Look(new Vector2(0f, -Mathf.Atan2(player.Eyes.transform.position.y - target.y, 1.48f) * Mathf.Rad2Deg / .13f));
            Physics.SyncTransforms();
            Report(results, interaction.TargetInView() == relay, "Focused aim acquired the service relay.");
            yield return Press(keyboard, Key.Q, .82f);
            yield return Press(keyboard, Key.E, 1.05f);
            Report(results, relay.Linked && latch.Current == LatchViewmodel.Phase.Success, "Focused E linked the service relay.");
            yield return new WaitForSecondsRealtime(.76f);
            yield return Press(keyboard, Key.Q, .72f);
            Report(results, !latch.Active && !latch.Model.activeSelf, "Focused Q played unequip and returned to exploration.");
        }

        static IEnumerator Press(Keyboard keyboard, Key key, float settle)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(settle);
        }

        static IEnumerator CctvInput(FirstPersonController player, HospitalInteraction interaction, LatchViewmodel latch,
            SecurityCctv network, Keyboard keyboard, List<string> results)
        {
            yield return Press(keyboard, Key.Digit1, .21f);
            Report(results, !network.Unlocked && !network.Watching && !latch.Active,
                "Focused 1 leaves CCTV locked before the reception breach.");
            var terminal = FindAnyObjectByType<ReceptionSecurityTerminal>();
            if (!terminal)
            {
                Report(results, false, "The reception terminal could not be found.");
                yield break;
            }
            var target = terminal.transform.position;
            player.Warp(new Vector3(target.x, .04f, target.z + 1.88f), Quaternion.Euler(0, 180f, 0));
            player.Look(new Vector2(0f, -Mathf.Atan2(player.Eyes.transform.position.y - target.y, 1.88f) * Mathf.Rad2Deg / .13f));
            Physics.SyncTransforms();
            Report(results, interaction.TargetInView() == terminal, "Focused aim acquired the counter's right monitor.");
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
            Report(results, latch.Current == LatchViewmodel.Phase.Ready && network.Watching && network.Selected == 0,
                "Focused 1 opened live camera 01 on the tablet.");
            yield return Press(keyboard, Key.RightArrow, .27f);
            Report(results, network.Selected == 1, "Focused Right switched to the next feed.");
            yield return Press(keyboard, Key.LeftArrow, .17f);
            for (int i = 0; i < 3; i++) yield return Press(keyboard, Key.RightArrow, .12f);
            ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(OutputPath()), "09_LATCH07_Live_CCTV.png"));
            for (int i = 0; i < 3; i++) yield return Press(keyboard, Key.LeftArrow, .12f);
            yield return Press(keyboard, Key.UpArrow, .17f);
            Report(results, network.Selected == network.Count - 1, "Focused Left and Up wrapped to the last feed.");
            yield return Press(keyboard, Key.DownArrow, .17f);
            Report(results, network.Selected == 0, "Focused Down wrapped to the first feed.");
            yield return Press(keyboard, Key.Digit1, .68f);
            Report(results, !latch.Active && !network.Watching && !network.FeedCamera.enabled,
                "Focused 1 closed CCTV and stopped the feed camera.");
        }

        static IEnumerator Move(FirstPersonController player, Keyboard keyboard, Key[] keys,
            Vector3 direction, string label, List<string> results, Action<float> record = null)
        {
            player.Warp(new Vector3(0f, .04f, -34.3f), Quaternion.identity);
            Vector3 start = player.transform.position;
            float started = Time.realtimeSinceStartup;
            do
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
                yield return null;
            } while (Time.realtimeSinceStartup - started < .50f);
            Vector3 offset = player.transform.position - start;
            offset.y = 0f;
            float distance = Vector3.Dot(offset, direction);
            float sideways = (offset - direction * distance).magnitude;
            Report(results, distance > .30f && sideways < .08f,
                FormattableString.Invariant($"Focused {label} advanced {distance:F2} m in the expected direction."));
            record?.Invoke(distance);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        static void Report(List<string> lines,bool ok,string value)
        {
            lines.Add((ok?"PASS ":"FAIL ")+value);
        }

        static void Save(IEnumerable<string> results)
        {
            string path=OutputPath();
            File.WriteAllLines(path,results);
            Debug.Log("NET92 FOCUSED INPUT | "+path);
        }

        static string OutputPath()
        {
            var args=Environment.GetCommandLineArgs();
            int i=Array.IndexOf(args,"-net92-output");
            return Path.GetFullPath(i>=0 && i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"input_checks.txt"));
        }
    }
}

