using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace NetRunner.Menu.Tests
{
    /// <summary>
    /// PlayMode tests of the paused view (#36, round 3). While paused, the
    /// player camera is off and its last frame is shown, with a UI-only camera
    /// presenting the menus. Resuming turns the player camera back on, even
    /// when it comes in the same frame as the pause.
    /// </summary>
    public sealed class PausedViewTests
    {
        private const int CaptureFrames = 2;

        private Camera eyes;
        private PanelSettings panel;
        private PausedView view;

        [SetUp]
        public void CreateCameraAndView()
        {
            var player = new GameObject("Test player eyes");
            eyes = player.AddComponent<Camera>();
            player.AddComponent<HDAdditionalCameraData>();
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            view = new GameObject("Test paused view").AddComponent<PausedView>();
            view.Configure(eyes, panel);
        }

        [TearDown]
        public void RemoveCameraAndView()
        {
            Object.Destroy(view.gameObject);
            Object.Destroy(eyes.gameObject);
            Object.Destroy(panel);
        }

        [UnityTest]
        public IEnumerator FreezingShowsTheLastFrameWithThePlayerCameraOff()
        {
            view.Freeze();

            Assert.That(eyes.targetTexture, Is.Not.Null, "The player camera does not draw its last frame into a texture.");
            yield return WaitForCapture();

            Assert.That(view.IsFrozen, Is.True);
            Assert.That(eyes.enabled, Is.False, "The player camera still renders while paused.");
            Camera presenter = view.GetComponentInChildren<Camera>(true);
            Assert.That(presenter.enabled && presenter.cullingMask == 0, Is.True, "No UI-only camera presents the menus.");
            VisualElement screen = view.GetComponentInChildren<UIDocument>().rootVisualElement.Q("paused-view");
            Assert.That(screen.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "The last frame is not shown.");
        }

        [UnityTest]
        public IEnumerator ThawingTurnsThePlayerCameraBackOn()
        {
            view.Freeze();
            yield return WaitForCapture();

            view.Thaw();

            Assert.That(view.IsFrozen, Is.False);
            Assert.That(eyes.enabled, Is.True);
            Assert.That(eyes.targetTexture, Is.Null, "The player camera still draws into the paused frame.");
            Assert.That(view.GetComponentInChildren<Camera>(true).enabled, Is.False, "The UI-only camera still renders.");
        }

        [UnityTest]
        public IEnumerator ThawingInThePauseFrameStillRestoresThePlayerCamera()
        {
            view.Freeze();
            view.Thaw();
            yield return WaitForCapture();

            Assert.That(view.IsFrozen, Is.False);
            Assert.That(eyes.enabled, Is.True);
            Assert.That(eyes.targetTexture, Is.Null);
        }

        private static IEnumerator WaitForCapture()
        {
            for (int i = 0; i < CaptureFrames; i++)
            {
                yield return null;
            }
        }
    }
}
