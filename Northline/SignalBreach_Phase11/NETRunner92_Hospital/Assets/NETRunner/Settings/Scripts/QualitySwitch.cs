using NetRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Settings
{
    /// <summary>
    /// Graphics quality shortcuts for players: F2 cycles through the quality
    /// levels (Low, Medium, High) by choosing that preset of the settings menu
    /// (<see cref="Preferences.SelectLevel"/>), which saves it like the menu
    /// does; F3 shows an FPS counter. It also lets the game cameras use HDRP's
    /// resolution scaling, which the render-scale setting drives. Created
    /// by <see cref="PreferencesRuntime"/> when the game starts, so no scene
    /// needs it. In Phase 11 the preferences own the level and the display,
    /// so the first-start 1080p cap and the PlayerPrefs storage of main's
    /// version are gone; <c>-net92-quality</c> is read by <see cref="Preferences"/>.
    /// </summary>
    public sealed class QualitySwitch : MonoBehaviour
    {
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
        private Preferences preferences;

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
        /// game the choice is saved; in the editor, or when the level was given
        /// at launch, it is not, so editor Play mode, tests and benchmarks
        /// leave no saved setting behind.
        /// </summary>
        public void CycleQuality()
        {
            int next = QualityChoice.Next(QualitySettings.GetQualityLevel(), QualitySettings.count);
            preferences.SelectLevel(next, !Application.isEditor && !preferences.QualityFixedAtLaunch);
            noteUntil = Time.unscaledTime + NoteSeconds;
        }

        /// <summary>Uses <paramref name="source"/> for the level F2 picks; <see cref="PreferencesRuntime"/> calls it when the game starts.</summary>
        public void Configure(Preferences source)
        {
            preferences = source;
        }

        private static void HandleCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
        {
            // The menu camera only presents UI. CCTV draws into textures at its own size.
            if (renderingCamera.cameraType != CameraType.Game || renderingCamera.targetTexture != null
                || renderingCamera.cullingMask == 0)
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
