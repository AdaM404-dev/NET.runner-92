#if UNITY_EDITOR
using System.Collections.Generic;
using System.Collections;
using System.IO;
using NetRunner.Core;
using NetRunner.Player;
using NetRunner.World;
using UnityEngine.Rendering;
using UnityEngine;

namespace NetRunner.Diagnostics
{
    public sealed partial class ExplorationDiagnostics
    {
        IEnumerator TestCctv(LatchViewmodel latch, SecurityCctv network)
        {
            var terminal = FindAnyObjectByType<ReceptionSecurityTerminal>();
            var interaction = player.GetComponent<HospitalInteraction>();
            Check(terminal && !network.Unlocked && !network.Open() && !latch.ToggleCctv(),
                "CCTV stays locked before the reception breach.");
            if (!terminal) yield break;
            interaction.enabled = false;
            player.Warp(new Vector3(-1f, .04f, -8.7f), Quaternion.identity);
            bool reached = Travel("Reception right monitor", new[] { P(-2.75f, -7.45f), P(-2.75f, -4.1f), P(-5.77f, -4.1f) });
            LookAtReceptionTerminal(player, terminal);
            var view = player.Eyes.transform;
            Physics.Raycast(view.position, view.forward, out var hit, HospitalInteraction.Reach, ~(1 << 2), QueryTriggerInteraction.Collide);
            Check(reached && interaction.TargetInView() as UnityEngine.Object == terminal,
                $"Player can walk around the reception counter and target the existing right monitor (ray hit {hit.collider?.name}).");
            terminal.Interact(player);
            Check(terminal.IsOpen && interaction.TerminalActive, "The desk terminal owns input while the puzzle is open.");
            Check(!terminal.Submit() && !network.Unlocked && terminal.IsOpen, "An incorrect signature keeps the CCTV locked.");
            terminal.Close();
            Check(!terminal.IsOpen && !interaction.TerminalActive && !network.Unlocked, "Exiting the terminal leaves the CCTV locked.");
            terminal.Interact(player);
            terminal.Adjust(1);
            terminal.Adjust(1);
            terminal.Select(-1);
            terminal.Adjust(-1);
            Check(terminal.Submit() && network.Unlocked && !terminal.IsOpen && latch.CctvAvailable,
                "Matching all three signal nodes unlocks the camera network for the tablet.");
            Check(latch.ToggleCctv(), "The unlocked CCTV request raises the tablet.");
            yield return WaitLatch(latch, LatchPhase.Ready);
            Check(network.Watching && network.Count == 6 && network.Surface && network.Surface.IsCreated(),
                "The raised tablet opens the six-feed camera network.");

            if (CaptureImages && network.Watching)
            {
                int frames = 0;
                void CountFeed(ScriptableRenderContext _, Camera camera)
                {
                    if (camera == network.FeedCamera) frames++;
                }
                RenderPipelineManager.endCameraRendering += CountFeed;
                var fingerprints = new HashSet<int>();
                for (int i = 0; i < network.Count; i++)
                {
                    int before = frames;
                    yield return new WaitForSecondsRealtime(.31f);
                    var pixels = ReadFeed(network.Surface, $"CAM_{i + 1:00}");
                    int visible = 0, fingerprint = 17;
                    for (int p = 0; p < pixels.Length; p += 23)
                    {
                        var c = pixels[p];
                        if (c.r + c.g + c.b > 25) visible++;
                        fingerprint = unchecked(fingerprint * 31 + c.r + c.g * 17 + c.b * 41);
                    }
                    fingerprints.Add(fingerprint);
                    Check(frames > before && visible > 250, $"CCTV {i + 1:00} {network.Label} produces a visible scene render.");
                    if (i == 3)
                    {
                        yield return new WaitForSecondsRealtime(.68f);
                        var later = ReadFeed(network.Surface);
                        int changed = 0;
                        for (int p = 0; p < pixels.Length; p++)
                            if (Mathf.Abs(pixels[p].r - later[p].r) + Mathf.Abs(pixels[p].g - later[p].g)
                                + Mathf.Abs(pixels[p].b - later[p].b) > 48) changed++;
                        Check(changed > 35, $"The ward feed changes over time ({changed} changed pixels), independent of channel switching.");
                        CaptureCamera("09_LATCH07_Live_CCTV_Editor", true);
                    }
                    latch.StepCctv(1);
                }
                Check(fingerprints.Count == network.Count && network.Selected == 0, "All six views differ and Next wraps to the first.");
                latch.StepCctv(-1);
                Check(network.Selected == network.Count - 1, "Previous wraps to the last camera.");
                RenderPipelineManager.endCameraRendering -= CountFeed;
            }
            latch.ToggleCctv();
            yield return WaitLatch(latch, LatchPhase.Stowed);
            Check(!network.Watching && !network.FeedCamera.enabled && network.Unlocked,
                "Closing the tablet stops the extra camera and preserves unlocked access.");
            interaction.enabled = true;
        }

        static void LookAtReceptionTerminal(FirstPersonController player, ReceptionSecurityTerminal terminal)
        {
            var aim = terminal.transform.position - player.Eyes.transform.position;
            float yaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;
            player.Warp(player.transform.position, Quaternion.Euler(0, yaw, 0));
            player.Look(new Vector2(0, Mathf.Atan2(aim.y, new Vector2(aim.x, aim.z).magnitude) * Mathf.Rad2Deg / .13f));
            Physics.SyncTransforms();
        }

        Color32[] ReadFeed(RenderTexture source, string name = null)
        {
            var old = RenderTexture.active;
            RenderTexture.active = source;
            var image = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            image.Apply();
            RenderTexture.active = old;
            if (name != null) File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
            var pixels = image.GetPixels32();
            Destroy(image);
            return pixels;
        }
    }
}
#endif
