using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using NetRunner.Core;
using NetRunner.Player;
using NetRunner.Saves;
using NetRunner.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace NetRunner.Menu.Tests
{
    /// <summary>
    /// PlayMode tests of a hospital run in the real hospital scene (Phase 11):
    /// a new game writes its first save, loading a checkpoint restores the
    /// player's place and progress, a hospital started without a run has no
    /// pause menu, and pausing stops the hospital rendering behind the menu and
    /// drops to the menu's frame limit until the game resumes (#36, round 3).
    /// Saves go to Logs/Tests only.
    /// </summary>
    public sealed class HospitalRunTests
    {
        private const string SessionPath = "Assets/NETRunner/Menu/Data/RunSession.asset";
        private const int StartupFrames = 5;
        private const int PauseFrames = 3;
        private const string ShotFolder = "Logs/pause";

        // A dark scene, but not black: at least 2% of the frozen frame is clearly lit.
        private const float Lit = .1f;
        private const float LitShare = .02f;

        private string folder;
        private RunSession session;

        [SetUp]
        public void UseAnEmptySaveFolder()
        {
            folder = Path.Combine(Application.dataPath, "..", "Logs", "Tests", "Run-" + Guid.NewGuid().ToString("N"));
#if UNITY_EDITOR
            session = UnityEditor.AssetDatabase.LoadAssetAtPath<RunSession>(SessionPath);
#endif
            session.UseSaveFolder(folder);
        }

        [TearDown]
        public void RemoveFolder()
        {
            session.TryTake(out _, out _);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        [UnityTest]
        public IEnumerator ANewGameWritesItsFirstSave()
        {
            session.Begin(0, null);

            yield return LoadHospital();

            var saved = session.Store.Read(0);
            Assert.That(saved, Is.Not.Null, "No first save");
            Assert.That(saved.HighestFloor, Is.EqualTo(0));
            Assert.That(saved.CctvUnlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator LoadingACheckpointRestoresThePlayersPlaceAndProgress()
        {
            var start = new Vector3(0f, .04f, -34.3f);
            session.Begin(1, new HospitalCheckpoint(DateTime.UtcNow, start, 90f, 0f, 2, true, true, "Teszt", new string[0]));

            yield return LoadHospital();

            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();
            Assert.That(Vector3.Distance(player.transform.position, start), Is.LessThan(.1f));
            Assert.That(UnityEngine.Object.FindAnyObjectByType<StairAccess>().Highest, Is.EqualTo(2));
            Assert.That(UnityEngine.Object.FindAnyObjectByType<SecurityCctv>().Unlocked, Is.True);
            Assert.That(UnityEngine.Object.FindAnyObjectByType<LatchRelay>().Linked, Is.True);
        }

        [UnityTest]
        public IEnumerator WithoutARunTheHospitalHasNoPauseMenuOrSaves()
        {
            yield return LoadHospital();

            var run = UnityEngine.Object.FindAnyObjectByType<HospitalRun>(FindObjectsInactive.Include);
            Assert.That(run.enabled, Is.False);
            Assert.That(run.GetComponent<UIDocument>(), Is.Null);
            Assert.That(Directory.Exists(folder), Is.False);
        }

        [UnityTest]
        public IEnumerator PausingShowsTheLastFrameAndResumingRendersTheHospitalAgain()
        {
            // A batch run has no focused window, so the test keyboard ignores focus, as StudioIntroTests does.
            InputSettings settings = InputSystem.settings;
            InputSettings.BackgroundBehavior background = settings.backgroundBehavior;
            InputSettings.EditorInputBehaviorInPlayMode routing = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            try
            {
                session.Begin(0, null);
                yield return LoadHospital();
                var run = UnityEngine.Object.FindAnyObjectByType<HospitalRun>();
                var runner = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();
                Camera eyes = runner.Eyes;

                // Escape pauses only while the game holds the mouse, which a batch run's missing window cannot.
                run.Pause();
                yield return WaitFrames();

                var view = UnityEngine.Object.FindAnyObjectByType<PausedView>();
                Assert.That(run.Paused, Is.True, "The run did not pause.");
                Assert.That(view && view.IsFrozen, Is.True, "The paused view does not show the last frame.");
                Assert.That(eyes.enabled, Is.False, "The hospital still renders behind the pause menu.");
                VisualElement screen = view.GetComponentInChildren<UIDocument>().rootVisualElement.Q("paused-view");
                float litShare = SaveAndMeasure(screen.resolvedStyle.backgroundImage.renderTexture, "PausedView.png");
                Assert.That(litShare, Is.GreaterThan(LitShare), "The frozen frame is black.");
                Assert.That(Camera.allCamerasCount, Is.EqualTo(0), "A camera still renders while paused.");
                Assert.That(Application.targetFrameRate, Is.EqualTo(FramePacing.MenuLimit), "The pause does not run at the menu's limit.");
                Assert.That(QualitySettings.vSyncCount, Is.EqualTo(0), "The pause keeps VSync on.");

                yield return PressEscape(keyboard);

                Assert.That(run.Paused, Is.False, "Escape did not resume the run.");
                Assert.That(view.IsFrozen, Is.False);
                Assert.That(eyes.enabled, Is.True, "The hospital does not render again after resuming.");
                Assert.That(Application.targetFrameRate, Is.EqualTo(runner.Preferences.Active.FrameLimit), "Resuming does not restore the game's limit.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                settings.editorInputBehaviorInPlayMode = routing;
                settings.backgroundBehavior = background;
            }
        }

        private static IEnumerator PressEscape(Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return WaitFrames();
        }

        // Saves the frame under Logs/pause for a look, and returns the share of clearly lit pixels.
        private static float SaveAndMeasure(RenderTexture frame, string file)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = frame;
            var picture = new Texture2D(frame.width, frame.height, TextureFormat.RGBA32, false);
            picture.ReadPixels(new Rect(0, 0, frame.width, frame.height), 0, 0);
            picture.Apply();
            RenderTexture.active = previous;
            Directory.CreateDirectory(ShotFolder);
            File.WriteAllBytes(Path.Combine(ShotFolder, file), picture.EncodeToPNG());
            Color32[] pixels = picture.GetPixels32();
            int lit = 0;
            foreach (Color32 pixel in pixels)
            {
                if (Mathf.Max(pixel.r, pixel.g, pixel.b) / 255f > Lit)
                {
                    lit++;
                }
            }

            UnityEngine.Object.Destroy(picture);
            return (float)lit / pixels.Length;
        }

        private static IEnumerator WaitFrames()
        {
            for (int i = 0; i < PauseFrames; i++)
            {
                yield return null;
            }
        }

        private static IEnumerator LoadHospital()
        {
            yield return SceneManager.LoadSceneAsync(HospitalRun.HospitalScene, LoadSceneMode.Single);
            for (int i = 0; i < StartupFrames; i++)
            {
                yield return null;
            }
        }
    }
}
