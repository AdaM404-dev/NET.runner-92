#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

namespace NETRunner92
{
    public sealed partial class ExplorationDiagnostics
    {
        IEnumerator TestLatch(LatchViewmodel latch)
        {
            var interaction = player.GetComponent<HospitalInteraction>();
            var screen = player.GetComponent<LatchScreen>();
            var relay = FindAnyObjectByType<LatchRelay>();
            Check(relay && !relay.Linked && !latch.Model.activeSelf, "LATCH-07 begins stowed and the optional service relay begins offline.");
            if (!relay) yield break;
            interaction.enabled = false;
            AimAtRelay(relay);
            Check(interaction.TargetInView() == relay, "The service relay is targetable from the south forecourt.");
            latch.Toggle(relay);
            yield return WaitLatch(latch, LatchViewmodel.Phase.Ready);
            latch.Tap();
            latch.Cancel();
            yield return WaitLatch(latch, LatchViewmodel.Phase.Stowed);
            Check(!relay.Linked && !latch.Model.activeSelf, "Cancel before the touch aborts the request and hides the arms.");

            latch.Toggle(null);
            yield return WaitLatch(latch, LatchViewmodel.Phase.Ready);
            latch.Tap();
            yield return WaitLatch(latch, LatchViewmodel.Phase.Failed);
            Check(screen.Status == "LINK FAILED" && !relay.Linked, "A touch without a target reaches failure and updates the live screen.");
            yield return WaitLatch(latch, LatchViewmodel.Phase.Ready);
            latch.Toggle(null);
            yield return WaitLatch(latch, LatchViewmodel.Phase.Stowed);
            Check(!latch.Model.activeSelf, "The regular unequip returns to the hidden state.");

            AimAtRelay(relay);
            latch.Toggle(interaction.TargetInView() as LatchRelay);
            yield return WaitLatch(latch, LatchViewmodel.Phase.Ready);
            latch.Tap();
            Check(!relay.Linked, "The relay waits for the animated touch before committing.");
            yield return WaitLatch(latch, LatchViewmodel.Phase.Success);
            Check(relay.Linked && screen.Status == "LINK ESTABLISHED", "The in-range tap links the relay and plays success.");
            yield return new WaitForSeconds(.22f);
            if (CaptureImages)
            {
                Check(CapturePanel(screen.Surface), "The 640 × 324 tablet RenderTexture contains visible UI pixels.");
                CaptureCamera("07_LATCH07_Linked", true);
            }
            yield return WaitLatch(latch, LatchViewmodel.Phase.Ready);
            latch.Toggle(null);
            yield return WaitLatch(latch, LatchViewmodel.Phase.Stowed);
            interaction.enabled = true;
        }

        void AimAtRelay(LatchRelay relay)
        {
            var at = relay.transform.position;
            player.Warp(new Vector3(at.x, .038f, at.z - 1.48f), Quaternion.identity);
            player.Look(new Vector2(0f, -Mathf.Atan2(player.Eyes.transform.position.y - at.y, 1.48f) * Mathf.Rad2Deg / .13f));
            Physics.SyncTransforms();
        }

        IEnumerator WaitLatch(LatchViewmodel latch, LatchViewmodel.Phase phase)
        {
            float until = Time.realtimeSinceStartup + 4f;
            while (latch.Current != phase && Time.realtimeSinceStartup < until) yield return null;
            Check(latch.Current == phase, $"LATCH-07 reached {phase} (observed {latch.Current}).");
        }

        bool CapturePanel(RenderTexture target)
        {
            if (!target || !target.IsCreated()) return false;
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(Output, "08_LATCH07_Screen.png"), image.EncodeToPNG());
            int colored = 0;
            foreach (Color32 pixel in image.GetPixels32())
                if (pixel.g > 120 && pixel.b > 115 && pixel.g > pixel.r * 1.2f) colored++;
            Destroy(image);
            return colored > 250;
        }
    }
}
#endif
