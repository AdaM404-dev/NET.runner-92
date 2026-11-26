using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace NetRunner.Menu
{
    /// <summary>
    /// Stops the hospital rendering behind the pause menu (#36, round 3). On
    /// <see cref="Freeze"/> the player camera draws one last frame into a
    /// texture, which is then shown full screen while that camera is off. A
    /// camera that draws only the UI presents the menus meanwhile, so a paused
    /// game no longer keeps the GPU at full load. <see cref="Thaw"/> turns the
    /// player camera back on. <see cref="HospitalRun"/> creates it on an object
    /// of its own and calls it when the game pauses and resumes. Its UI
    /// document must not sit under another UI document, which would merge it
    /// into that document's panel.
    /// </summary>
    public sealed class PausedView : MonoBehaviour
    {
        // Below every other panel: the frozen frame takes the place of the 3D view.
        private const int BehindEverything = -100;
        private const int NoDepthBuffer = 0;

        // The UI-only camera skips everything a camera that draws no objects would still compute.
        private static readonly FrameSettingsField[] SkippedEffects =
        {
            FrameSettingsField.Postprocess, FrameSettingsField.SSAO, FrameSettingsField.SSR, FrameSettingsField.Volumetrics,
            FrameSettingsField.ShadowMaps, FrameSettingsField.MotionVectors,
        };

        private Camera eyes;
        private Camera presenter;
        private PanelSettings panel;
        private VisualElement screen;
        private RenderTexture frame;
        private Coroutine capture;

        /// <summary>Whether the player camera is off and its last frame is shown.</summary>
        public bool IsFrozen { get; private set; }

        private void OnDestroy()
        {
            ReleaseFrame();
            if (panel)
            {
                Destroy(panel);
            }
        }

        /// <summary>
        /// Uses <paramref name="playerEyes"/> as the camera to freeze, and a copy
        /// of <paramref name="panelTemplate"/> for the frozen frame's screen.
        /// </summary>
        public void Configure(Camera playerEyes, PanelSettings panelTemplate)
        {
            eyes = playerEyes;
            presenter = CreatePresenter();
            panel = Instantiate(panelTemplate);
            panel.sortingOrder = BehindEverything;
            var host = new GameObject("Paused view screen");
            host.transform.SetParent(transform, false);
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            screen = new VisualElement { name = "paused-view", pickingMode = PickingMode.Ignore };
            screen.style.position = Position.Absolute;
            screen.style.left = 0;
            screen.style.top = 0;
            screen.style.right = 0;
            screen.style.bottom = 0;
            screen.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            screen.style.display = DisplayStyle.None;
            document.rootVisualElement.Add(screen);
        }

        /// <summary>
        /// Draws the player camera's view one last time and shows it in place of
        /// the live view; from the next frame on the player camera is off.
        /// </summary>
        public void Freeze()
        {
            if (frame)
            {
                return;
            }

            frame = new RenderTexture(Screen.width, Screen.height, NoDepthBuffer, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "Paused hospital view",
            };
            frame.Create();
            eyes.targetTexture = frame;
            presenter.enabled = true;
            screen.style.backgroundImage = Background.FromRenderTexture(frame);
            screen.style.display = DisplayStyle.Flex;
            capture = StartCoroutine(TurnOffAfterCapture());
        }

        /// <summary>Turns the player camera back on and removes the frozen frame.</summary>
        public void Thaw()
        {
            if (!frame)
            {
                return;
            }

            if (capture != null)
            {
                StopCoroutine(capture);
                capture = null;
            }

            eyes.targetTexture = null;
            eyes.enabled = true;
            presenter.enabled = false;
            screen.style.display = DisplayStyle.None;
            screen.style.backgroundImage = StyleKeyword.None;
            ReleaseFrame();
            IsFrozen = false;
        }

        // The camera drew into the frame while the frame that called Freeze was rendered.
        private IEnumerator TurnOffAfterCapture()
        {
            yield return null;
            eyes.enabled = false;
            eyes.targetTexture = null;
            IsFrozen = true;
            capture = null;
        }

        private Camera CreatePresenter()
        {
            var host = new GameObject("Paused view UI camera");
            host.transform.SetParent(transform, false);
            var camera = host.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            var hdrp = host.AddComponent<HDAdditionalCameraData>();
            hdrp.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hdrp.backgroundColorHDR = Color.black;
            hdrp.customRenderingSettings = true;
            foreach (FrameSettingsField setting in SkippedEffects)
            {
                hdrp.renderingPathCustomFrameSettings.SetEnabled(setting, false);
                hdrp.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)setting] = true;
            }

            return camera;
        }

        private void ReleaseFrame()
        {
            if (!frame)
            {
                return;
            }

            if (eyes && eyes.targetTexture == frame)
            {
                eyes.targetTexture = null;
            }

            frame.Release();
            Destroy(frame);
            frame = null;
        }
    }
}
