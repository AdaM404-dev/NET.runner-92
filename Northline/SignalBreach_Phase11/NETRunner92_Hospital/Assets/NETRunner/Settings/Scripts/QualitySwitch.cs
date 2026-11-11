using System;
using NetRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Settings
{
    /// <summary>
    /// Graphics quality for players: F2 cycles through the quality levels
    /// (Low, Medium, High) and remembers the choice, F3 shows an FPS counter.
    /// On a first start the screen is capped at 1080p. It also lets the
    /// game cameras use HDRP's resolution scaling, which the Low and Medium
    /// pipeline assets switch on. Created automatically when the game starts,
    /// so no scene needs it. Starting the game with <c>-net92-quality Low</c>
    /// (or Medium, High) uses that level for this run without saving it,
    /// which the benchmark script relies on.
    /// </summary>
    public sealed class QualitySwitch : MonoBehaviour
    {
        private const string SavedLevelKey = "NetRunner.Settings.QualityLevel";
        private const string QualityOption = "-net92-quality";
        private const int StartMaxHeight = 1080;
        private const float NoteSeconds = 2f;
        private const float MeterWindowSeconds = 0.5f;
        private const int LabelFontSize = 18;
        private const float LabelMargin = 12f;
        private const float LabelWidth = 260f;
        private const float LabelHeight = 28f;

        private readonly FrameRateMeter meter = new FrameRateMeter(MeterWindowSeconds);
        private float noteUntil;
        private bool showFrameRate;
        private GUIStyle labelStyle;
        private bool remember;

        private static string CurrentLevelName => QualitySettings.names[QualitySettings.GetQualityLevel()];

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += HandleCameraRendering;
        }

        private void Update()
        {
            meter.Add(Time.unscaledDeltaTime);
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                CycleQuality();
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                showFrameRate = !showFrameRate;
            }
        }

        private void OnGUI()
        {
            bool showNote = Time.unscaledTime < noteUntil;
            if (!showNote && !showFrameRate)
            {
                return;
            }

            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = LabelFontSize };
            float y = LabelMargin;
            if (showFrameRate)
            {
                GUI.Label(new Rect(LabelMargin, y, LabelWidth, LabelHeight), $"{meter.FramesPerSecond:0} FPS", labelStyle);
                y += LabelHeight;
            }

            if (showNote)
            {
                GUI.Label(new Rect(LabelMargin, y, LabelWidth, LabelHeight), $"Quality: {CurrentLevelName}", labelStyle);
            }
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= HandleCameraRendering;
        }

        /// <summary>
        /// Switches to the next quality level and shows its name. In a built
        /// game the choice is saved; in the editor it is not, so editor Play
        /// mode and tests leave no saved setting behind.
        /// </summary>
        public void CycleQuality()
        {
            int next = QualityChoice.Next(QualitySettings.GetQualityLevel(), QualitySettings.count);
            QualitySettings.SetQualityLevel(next, true);
            noteUntil = Time.unscaledTime + NoteSeconds;
            if (remember)
            {
                PlayerPrefs.SetInt(SavedLevelKey, next);
                PlayerPrefs.Save();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnStart()
        {
            var host = new GameObject(nameof(QualitySwitch));
            DontDestroyOnLoad(host);
            var qualitySwitch = host.AddComponent<QualitySwitch>();
            if (!Application.isEditor)
            {
                qualitySwitch.remember = ApplyStartSettings();
            }
        }

        /// <returns>Whether this run saves the player's choices: not when a level was given at launch.</returns>
        private static bool ApplyStartSettings()
        {
            bool firstStart = !PlayerPrefs.HasKey(SavedLevelKey);
            string launchName = LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), QualityOption);
            int launchLevel = QualityChoice.FromName(launchName, QualitySettings.names);
            bool remember = launchLevel < 0;
            int saved = PlayerPrefs.GetInt(SavedLevelKey, -1);
            int level = remember ? QualityChoice.StartLevel(saved, QualitySettings.count, QualitySettings.GetQualityLevel()) : launchLevel;
            QualitySettings.SetQualityLevel(level, true);
            if (remember)
            {
                PlayerPrefs.SetInt(SavedLevelKey, level);
                PlayerPrefs.Save();
            }

            CapFirstStart(firstStart);
            return remember;
        }

        private static void CapFirstStart(bool firstStart)
        {
            if (!firstStart)
            {
                return;
            }

            // Unity remembers the window size itself after this, so only the
            // very first start is capped.
            var display = Screen.currentResolution;
            var size = StartResolution.Cap(display.width, display.height, StartMaxHeight);
            Screen.SetResolution(size.Width, size.Height, Screen.fullScreenMode);
        }

        private static void HandleCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
        {
            // Only cameras that draw to the screen are scaled; the CCTV cameras
            // render into textures at their own fixed size.
            if (renderingCamera.cameraType != CameraType.Game || renderingCamera.targetTexture != null)
            {
                return;
            }

            if (renderingCamera.TryGetComponent<HDAdditionalCameraData>(out var cameraData) && !cameraData.allowDynamicResolution)
            {
                cameraData.allowDynamicResolution = true;
            }
        }
    }
}
