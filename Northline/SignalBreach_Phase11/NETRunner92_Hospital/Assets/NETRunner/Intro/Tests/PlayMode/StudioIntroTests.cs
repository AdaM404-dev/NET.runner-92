using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace NetRunner.Intro.Tests
{
    /// <summary>
    /// PlayMode tests of the studio intro (#66) on a test-built intro, each
    /// in its own empty scene, playing the real video file: it decodes and
    /// shows the Sanhilzon logo, Space goes on to the main menu, and a video
    /// that cannot play goes on to the main menu by itself. "Goes on" means
    /// the menu replaced the intro's scene. The frame at 20 s is saved to Logs/intro/.
    /// </summary>
    public sealed class StudioIntroTests
    {
        private const string VideoFile = "Assets/NETRunner92/Intro/Sanhilzon_Studio_Intro_1080p60.webm";
        private const string NextScene = "MainMenu";
        private const string OutputFolder = "Logs/intro";

        // The logo is fully shown 20 s in (checked with ffprobe); at least 2%
        // of the picture is then clearly brighter than the black background.
        private const double LogoTime = 20;
        private const float Bright = .3f;
        private const float LogoShare = .02f;
        private const float Wait = 8f;

        private Keyboard keyboard;
        private StudioIntro intro;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode routing;

        // A batch run has no focused window, so the test keyboard is told to
        // ignore focus, as PerformanceOverlayTests does.
        [UnitySetUp]
        public IEnumerator StartInAnEmptyScene()
        {
            InputSettings settings = InputSystem.settings;
            background = settings.backgroundBehavior;
            routing = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            Scene empty = SceneManager.CreateScene("Intro test " + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            yield return null;
        }

        // An intro that is still playing holds the menu's loading back, and
        // every later scene load would wait behind it, so it is always let go.
        [UnityTearDown]
        public IEnumerator ReleaseTheIntro()
        {
            if (intro)
            {
                intro.Skip();
                yield return Until(() => !intro, Wait);
            }

            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
            InputSystem.settings.backgroundBehavior = background;
        }

        [UnityTest]
        public IEnumerator TheIntroPlaysAndShowsTheStudioLogo()
        {
            intro = BuildIntro(Path.GetFullPath(VideoFile), out VideoPlayer video);
            yield return Until(() => video.isPlaying && video.time >= LogoTime, LogoTime + Wait);
            Assert.That(video.isPlaying && video.time >= LogoTime, Is.True, "The video did not play to the logo.");
            Texture2D picture = Read(video.targetTexture);
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllBytes(Path.Combine(OutputFolder, "StudioIntro_20s.png"), picture.EncodeToPNG());
            int bright = 0;
            foreach (Color pixel in picture.GetPixels())
            {
                if (pixel.maxColorComponent > Bright)
                {
                    bright++;
                }
            }

            float logo = picture.width * picture.height * LogoShare;
            UnityEngine.Object.Destroy(picture);
            Assert.That(bright, Is.GreaterThan(logo), "The logo is not visible.");
            Assert.That(intro.IsLeaving, Is.False);
        }

        [UnityTest]
        public IEnumerator SpaceGoesOnToTheMainMenu()
        {
            intro = BuildIntro(Path.GetFullPath(VideoFile), out VideoPlayer video);
            yield return Until(() => video.isPlaying, Wait);
            Assert.That(video.isPlaying, Is.True, "The video did not start.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Until(() => !intro, Wait);
            Assert.That(!intro && SceneManager.GetActiveScene().name == NextScene, Is.True, "Space did not open the menu.");
        }

        [UnityTest]
        public IEnumerator AVideoThatCannotPlayGoesOnToTheMainMenu()
        {
            LogAssert.ignoreFailingMessages = true;
            intro = BuildIntro(Path.GetFullPath(Path.Combine(OutputFolder, "no-such-video.webm")), out _);
            yield return Until(() => !intro, Wait);
            Assert.That(!intro && SceneManager.GetActiveScene().name == NextScene, Is.True, "The menu did not open.");
        }

        private static StudioIntro BuildIntro(string file, out VideoPlayer video)
        {
            var host = new GameObject("Test studio intro");
            AudioSource sound = host.AddComponent<AudioSource>();
            video = host.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.source = VideoSource.Url;
            video.url = file;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.SetTargetAudioSource(0, sound);
            UIDocument document = host.AddComponent<UIDocument>();
            document.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            StudioIntro built = host.AddComponent<StudioIntro>();
            built.Configure(video, document, NextScene);
            return built;
        }

        private static IEnumerator Until(Func<bool> condition, double seconds)
        {
            float end = Time.realtimeSinceStartup + (float)seconds;
            while (!condition() && Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
        }

        private static Texture2D Read(RenderTexture source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;
            var picture = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            picture.Apply();
            RenderTexture.active = previous;
            return picture;
        }
    }
}
