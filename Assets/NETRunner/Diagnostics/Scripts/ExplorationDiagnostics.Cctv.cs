#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NetRunner.Core;
using NetRunner.Player;
using NetRunner.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace NetRunner.Diagnostics
{
    /// <summary>The CCTV: locked until the reception puzzle is solved, then six live, different feeds on the tablet.</summary>
    public sealed partial class ExplorationDiagnostics
    {
        private const int Feeds = 6;
        private const int WardFeed = 3;

        // A feed is a visible render when more than 250 of every 23rd pixel are
        // not black; the ward feed changes over time when more than 35 pixels
        // differ by more than 48 levels after 0.68 s.
        private const int PixelStride = 23;
        private const int BlackLevel = 25;
        private const int VisiblePixelsInFeed = 250;
        private const int ChangeLevel = 48;
        private const int ChangedPixels = 35;
        private const float FeedSettle = .31f;
        private const float ChangeWait = .68f;

        private IEnumerator TestCctv(LatchViewmodel latch, SecurityCctv network)
        {
            ReceptionSecurityTerminal terminal = FindAnyObjectByType<ReceptionSecurityTerminal>();
            HospitalInteraction interaction = player.GetComponent<HospitalInteraction>();
            Check(terminal && !network.Unlocked && !network.Open() && !latch.ToggleCctv(),
                "CCTV stays locked before the reception breach.");
            if (!terminal)
            {
                yield break;
            }

            interaction.enabled = false;
            player.Warp(new Vector3(-1f, .04f, -8.7f), Quaternion.identity);
            bool hasReached = Travel("Reception right monitor", new[] { P(-2.75f, -7.45f), P(-2.75f, -4.1f), P(-5.77f, -4.1f) });
            LookAtReceptionTerminal(terminal);
            Transform view = player.Eyes.transform;
            Physics.Raycast(view.position, view.forward, out RaycastHit hit, HospitalInteraction.Reach, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);
            Check(hasReached && interaction.TargetInView() as UnityEngine.Object == terminal,
                $"Player can walk around the reception counter and target the existing right monitor (ray hit {hit.collider?.name}).");
            terminal.Interact(player);
            Check(terminal.IsOpen && interaction.TerminalActive, "The desk terminal owns input while the puzzle is open.");
            Check(!terminal.Submit() && !network.Unlocked && terminal.IsOpen, "An incorrect signature keeps the CCTV locked.");
            terminal.Close();
            Check(!terminal.IsOpen && !interaction.TerminalActive && !network.Unlocked, "Exiting the terminal leaves the CCTV locked.");

            // The signature 03 01 04 from the start 01 01 01: the first cell up twice (03),
            // left wraps to the third cell, and down once wraps its 01 to 04.
            terminal.Interact(player);
            terminal.Adjust(1);
            terminal.Adjust(1);
            terminal.Select(-1);
            terminal.Adjust(-1);
            Check(terminal.Submit() && network.Unlocked && !terminal.IsOpen && latch.CctvAvailable,
                "Matching all three signal nodes unlocks the camera network for the tablet.");
            Check(latch.ToggleCctv(), "The unlocked CCTV request raises the tablet.");
            yield return WaitLatch(latch, LatchPhase.Ready);
            Check(network.Watching && network.Count == Feeds && network.Surface && network.Surface.IsCreated(),
                "The raised tablet opens the six-feed camera network.");
            if (isCapturingImages && network.Watching)
            {
                yield return TestFeeds(latch, network);
            }

            latch.ToggleCctv();
            yield return WaitLatch(latch, LatchPhase.Stowed);
            Check(!network.Watching && !network.FeedCamera.enabled && network.Unlocked,
                "Closing the tablet stops the extra camera and preserves unlocked access.");
            interaction.enabled = true;
        }

        // Each feed renders a visible picture that differs from the others; the
        // ward feed changes over time; next and previous wrap round.
        private IEnumerator TestFeeds(LatchViewmodel latch, SecurityCctv network)
        {
            int frames = 0;
            void CountFeed(ScriptableRenderContext context, Camera camera)
            {
                if (camera == network.FeedCamera)
                {
                    frames++;
                }
            }

            RenderPipelineManager.endCameraRendering += CountFeed;
            var fingerprints = new HashSet<int>();
            for (int i = 0; i < network.Count; i++)
            {
                int before = frames;
                yield return new WaitForSecondsRealtime(FeedSettle);
                Color32[] pixels = ReadFeed(network.Surface, $"CAM_{i + 1:00}");
                int visible = 0;
                int fingerprint = 17;
                for (int p = 0; p < pixels.Length; p += PixelStride)
                {
                    Color32 c = pixels[p];
                    if (c.r + c.g + c.b > BlackLevel)
                    {
                        visible++;
                    }

                    fingerprint = unchecked(fingerprint * 31 + c.r + c.g * 17 + c.b * 41);
                }

                fingerprints.Add(fingerprint);
                Check(frames > before && visible > VisiblePixelsInFeed, $"CCTV {i + 1:00} {network.Label} produces a visible scene render.");
                if (i == WardFeed)
                {
                    yield return new WaitForSecondsRealtime(ChangeWait);
                    Color32[] later = ReadFeed(network.Surface);
                    int changed = 0;
                    for (int p = 0; p < pixels.Length; p++)
                    {
                        if (Mathf.Abs(pixels[p].r - later[p].r) + Mathf.Abs(pixels[p].g - later[p].g)
                            + Mathf.Abs(pixels[p].b - later[p].b) > ChangeLevel)
                        {
                            changed++;
                        }
                    }

                    Check(changed > ChangedPixels, $"The ward feed changes over time ({changed} changed pixels), independent of channel switching.");
                    CaptureCamera("09_LATCH07_Live_CCTV_Editor", true);
                }

                latch.StepCctv(1);
            }

            Check(fingerprints.Count == network.Count && network.Selected == 0, "All six views differ and Next wraps to the first.");
            latch.StepCctv(-1);
            Check(network.Selected == network.Count - 1, "Previous wraps to the last camera.");
            RenderPipelineManager.endCameraRendering -= CountFeed;
        }

        private void LookAtReceptionTerminal(ReceptionSecurityTerminal terminal)
        {
            Vector3 aim = terminal.transform.position - player.Eyes.transform.position;
            float yaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;
            player.Warp(player.transform.position, Quaternion.Euler(0, yaw, 0));
            player.Look(new Vector2(0, Mathf.Atan2(aim.y, new Vector2(aim.x, aim.z).magnitude) * Mathf.Rad2Deg / LookSensitivity));
            Physics.SyncTransforms();
        }

        // The feed's pixels, saved as <name>.png when a name is given.
        private Color32[] ReadFeed(RenderTexture source, string name = null)
        {
            RenderTexture old = RenderTexture.active;
            RenderTexture.active = source;
            var image = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            image.Apply();
            RenderTexture.active = old;
            if (name != null)
            {
                File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
            }

            Color32[] pixels = image.GetPixels32();
            Destroy(image);
            return pixels;
        }
    }
}
#endif
