#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;

namespace NetRunner.Diagnostics
{
    /// <summary>LATCH-07: the tablet's phases, cancelling, a failed link, and linking the service relay.</summary>
    public sealed partial class ExplorationDiagnostics
    {
        private const string ScreenPicture = "08_LATCH07_Screen.png";
        private const float RelayDistance = 1.48f;
        private const float PhaseTimeout = 4f;

        // The screen shows the cyan UI: a pixel counts when green and blue are
        // bright and green outweighs red; more than 250 such pixels pass.
        private const byte BrightGreen = 120;
        private const byte BrightBlue = 115;
        private const float GreenOverRed = 1.2f;
        private const int VisiblePixels = 250;

        private IEnumerator TestLatch(LatchViewmodel latch)
        {
            HospitalInteraction interaction = player.GetComponent<HospitalInteraction>();
            LatchScreen screen = player.GetComponent<LatchScreen>();
            LatchRelay relay = FindAnyObjectByType<LatchRelay>();
            Check(relay && !relay.Linked && !latch.Model.activeSelf, "LATCH-07 begins stowed and the optional service relay begins offline.");
            if (!relay)
            {
                yield break;
            }

            interaction.enabled = false;
            AimAtRelay(relay);
            Check(interaction.TargetInView() as UnityEngine.Object == relay, "The service relay is targetable from the south forecourt.");
            latch.Toggle(relay);
            yield return WaitLatch(latch, LatchPhase.Ready);
            latch.Tap();
            latch.Cancel();
            yield return WaitLatch(latch, LatchPhase.Stowed);
            Check(!relay.Linked && !latch.Model.activeSelf, "Cancel before the touch aborts the request and hides the arms.");

            latch.Toggle(null);
            yield return WaitLatch(latch, LatchPhase.Ready);
            latch.Tap();
            yield return WaitLatch(latch, LatchPhase.Failed);
            Check(screen.Status == "LINK FAILED" && !relay.Linked, "A touch without a target reaches failure and updates the live screen.");
            yield return WaitLatch(latch, LatchPhase.Ready);
            latch.Toggle(null);
            yield return WaitLatch(latch, LatchPhase.Stowed);
            Check(!latch.Model.activeSelf, "The regular unequip returns to the hidden state.");

            AimAtRelay(relay);
            latch.Toggle(interaction.TargetInView() as LatchRelay);
            yield return WaitLatch(latch, LatchPhase.Ready);
            latch.Tap();
            Check(!relay.Linked, "The relay waits for the animated touch before committing.");
            yield return WaitLatch(latch, LatchPhase.Success);
            Check(relay.Linked && screen.Status == "LINK ESTABLISHED", "The in-range tap links the relay and plays success.");
            yield return new WaitForSeconds(.22f);
            if (isCapturingImages)
            {
                Check(CapturePanel(screen.Surface), "The 640 × 324 tablet RenderTexture contains visible UI pixels.");
                CaptureCamera("07_LATCH07_Linked", true);
            }

            yield return WaitLatch(latch, LatchPhase.Ready);
            latch.Toggle(null);
            yield return WaitLatch(latch, LatchPhase.Stowed);
            interaction.enabled = true;
        }

        // Stands 1.48 m in front of the relay and looks at it.
        private void AimAtRelay(LatchRelay relay)
        {
            Vector3 at = relay.transform.position;
            player.Warp(new Vector3(at.x, .038f, at.z - RelayDistance), Quaternion.identity);
            player.Look(new Vector2(0f, -Mathf.Atan2(player.Eyes.transform.position.y - at.y, RelayDistance) * Mathf.Rad2Deg / LookSensitivity));
            Physics.SyncTransforms();
        }

        private IEnumerator WaitLatch(LatchViewmodel latch, LatchPhase phase)
        {
            float until = Time.realtimeSinceStartup + PhaseTimeout;
            while (latch.Current != phase && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            Check(latch.Current == phase, $"LATCH-07 reached {phase} (observed {latch.Current}).");
        }

        // Saves the tablet's screen and whether it shows enough of the cyan UI.
        private bool CapturePanel(RenderTexture target)
        {
            if (!target || !target.IsCreated())
            {
                return false;
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(output, ScreenPicture), image.EncodeToPNG());
            int colored = 0;
            foreach (Color32 pixel in image.GetPixels32())
            {
                if (pixel.g > BrightGreen && pixel.b > BrightBlue && pixel.g > pixel.r * GreenOverRed)
                {
                    colored++;
                }
            }

            Destroy(image);
            return colored > VisiblePixels;
        }
    }
}
#endif
