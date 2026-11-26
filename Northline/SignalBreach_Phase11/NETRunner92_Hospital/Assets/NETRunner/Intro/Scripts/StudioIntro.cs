using System;
using NetRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace NetRunner.Intro
{
    /// <summary>
    /// Plays the Sanhilzon Studio intro full screen at start, in place of
    /// Unity's logo (#66), while the next scene loads in the background, then
    /// shows that scene. <see cref="IntroRules"/> decides when: the video
    /// ended, Space was pressed, playback failed or never started. Test and
    /// benchmark runs go straight on.
    /// </summary>
    public sealed class StudioIntro : MonoBehaviour
    {
        private const int FrameWidth = 1920;
        private const int FrameHeight = 1080;

        [SerializeField] private VideoPlayer video;
        [SerializeField] private UIDocument screen;
        [SerializeField] private string nextScene = "MainMenu";

        private RenderTexture frame;
        private AsyncOperation loading;
        private bool hasStarted;
        private bool hasEnded;
        private bool hasFailed;
        private float elapsed;

        /// <summary>Whether the intro has finished and the next scene is being shown.</summary>
        public bool IsLeaving { get; private set; }

        private void Awake()
        {
            if (IntroRules.IsSkippedAtLaunch(Environment.GetCommandLineArgs()))
            {
                IsLeaving = true;
                SceneManager.LoadScene(nextScene);
            }
        }

        private void Start()
        {
            if (IsLeaving)
            {
                return;
            }

            video.started += HandleStarted;
            video.loopPointReached += HandleEnded;
            video.errorReceived += HandleError;
            frame = new RenderTexture(FrameWidth, FrameHeight, 0);
            video.renderMode = VideoRenderMode.RenderTexture;
            video.targetTexture = frame;
            ShowFrame();
            loading = SceneManager.LoadSceneAsync(nextScene);
            loading.allowSceneActivation = false;
            video.Play();
        }

        private void Update()
        {
            if (IsLeaving)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            bool isSkipPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (IntroRules.IsOver(hasEnded, isSkipPressed, hasFailed, hasStarted, elapsed))
            {
                Skip();
            }
        }

        private void OnDestroy()
        {
            if (video)
            {
                video.started -= HandleStarted;
                video.loopPointReached -= HandleEnded;
                video.errorReceived -= HandleError;
            }

            if (frame)
            {
                frame.Release();
                Destroy(frame);
            }
        }

        /// <summary>Ends the intro now, as Space does, and shows the next scene once it has loaded.</summary>
        public void Skip()
        {
            if (!IsLeaving && loading != null)
            {
                Leave();
            }
        }

        /// <summary>Sets the video, the screen it is shown on and the scene that follows; the intro's builder calls it.</summary>
        public void Configure(VideoPlayer player, UIDocument document, string followingScene)
        {
            video = player;
            screen = document;
            nextScene = followingScene;
        }

        // A full-screen element showing the video's frames, letterboxed on black.
        private void ShowFrame()
        {
            var picture = new VisualElement();
            picture.style.flexGrow = 1;
            picture.style.backgroundColor = Color.black;
            picture.style.backgroundImage = Background.FromRenderTexture(frame);
            picture.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            screen.rootVisualElement.Add(picture);
        }

        private void Leave()
        {
            IsLeaving = true;
            video.Stop();
            loading.allowSceneActivation = true;
        }

        private void HandleStarted(VideoPlayer source)
        {
            hasStarted = true;
        }

        private void HandleEnded(VideoPlayer source)
        {
            hasEnded = true;
        }

        private void HandleError(VideoPlayer source, string message)
        {
            Debug.LogWarning("NET92 INTRO | " + message);
            hasFailed = true;
        }
    }
}
