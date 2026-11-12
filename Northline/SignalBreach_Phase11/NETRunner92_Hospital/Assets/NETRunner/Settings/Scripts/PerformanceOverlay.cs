using NetRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.Settings
{
    /// <summary>Shows the local hardware and live rendering statistics on demand.</summary>
    public sealed class PerformanceOverlay : MonoBehaviour
    {
        private const string ToggleBinding = "<Keyboard>/f4";
        private const string PanelTitle = "Teljesítmény  ·  F4";
        private const string MissingValue = "nincs adat";
        private const float SampleSeconds = 0.5f;
        private const float MillisecondsPerSecond = 1000f;
        private const float Margin = 12f;
        private const float Padding = 12f;
        private const float MaximumWidth = 390f;
        private const float HeadingHeight = 27f;
        private const int HeadingFontSize = 18;
        private const int BodyFontSize = 15;
        private static readonly Color PanelColor = new Color(0.04f, 0.07f, 0.09f, 0.94f);
        private static readonly Color HeadingColor = new Color(0.35f, 1f, 0.95f);

        private readonly FrameRateMeter meter = new FrameRateMeter(SampleSeconds);
        private readonly FrameTiming[] frameTimings = new FrameTiming[1];
        private readonly GUIContent body = new GUIContent();
        private Preferences preferences;
        private InputAction toggle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private string hardware;
        private float nextSampleAt;

        /// <summary>Whether the player has opened the panel with F4.</summary>
        public bool IsVisible { get; private set; }

        private void Awake()
        {
            toggle = new InputAction(type: InputActionType.Button, binding: ToggleBinding);
        }

        private void OnEnable()
        {
            toggle.performed += HandleToggle;
            toggle.Enable();
        }

        private void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            meter.Add(Time.unscaledDeltaTime);
            FrameTimingManager.CaptureFrameTimings();
            if (Time.unscaledTime >= nextSampleAt)
            {
                RefreshText();
                nextSampleAt = Time.unscaledTime + SampleSeconds;
            }
        }

        private void OnGUI()
        {
            if (!IsVisible || Event.current.type != EventType.Repaint)
            {
                return;
            }

            headingStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = HeadingFontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = HeadingColor }
            };
            bodyStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = BodyFontSize,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            float width = Mathf.Min(MaximumWidth, Screen.width - Margin * 2);
            float textWidth = width - Padding * 2;
            if (textWidth <= 0f)
            {
                return;
            }

            float bodyHeight = bodyStyle.CalcHeight(body, textWidth);
            float left = Screen.width - width - Margin;
            var bounds = new Rect(left, Margin, width, Padding * 2 + HeadingHeight + bodyHeight);
            Color previousColor = GUI.color;
            GUI.color = PanelColor;
            GUI.DrawTexture(bounds, Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Label(new Rect(left + Padding, Margin + Padding, textWidth, HeadingHeight), PanelTitle, headingStyle);
            GUI.Label(new Rect(left + Padding, Margin + Padding + HeadingHeight, textWidth, bodyHeight), body, bodyStyle);
        }

        private void OnDisable()
        {
            toggle.Disable();
            toggle.performed -= HandleToggle;
        }

        private void OnDestroy()
        {
            toggle.Dispose();
        }

        /// <summary>Uses the active preferences to report the configured render scale.</summary>
        public void Configure(Preferences source)
        {
            preferences = source;
        }

        private void HandleToggle(InputAction.CallbackContext context)
        {
            IsVisible = !IsVisible;
            if (IsVisible)
            {
                RefreshText();
                nextSampleAt = Time.unscaledTime + SampleSeconds;
            }
        }

        private void RefreshText()
        {
            hardware ??= $"CPU: {HardwareName(SystemInfo.processorType)} ({SystemInfo.processorCount} logikai szál)\n"
                + $"GPU: {HardwareName(SystemInfo.graphicsDeviceName)}\n"
                + $"VRAM-kapacitás: {FormatMemory(SystemInfo.graphicsMemorySize)} · {SystemInfo.graphicsDeviceType}\n"
                + $"RAM: {FormatMemory(SystemInfo.systemMemorySize)}\n"
                + $"Rendszer: {HardwareName(SystemInfo.operatingSystem)}";

            float fps = meter.FramesPerSecond;
            string rate = fps > 0f ? $"{fps:0} FPS · {MillisecondsPerSecond / fps:0.0} ms / képkocka" : "FPS: mérés folyamatban";
            bool hasTiming = FrameTimingManager.GetLatestTimings(1, frameTimings) > 0;
            FrameTiming timing = hasTiming ? frameTimings[0] : default;
            string quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            string scale = preferences && preferences.Active != null ? $"{preferences.Active.RenderScale}%" : MissingValue;
            string cap = Application.targetFrameRate > 0 ? $"{Application.targetFrameRate}" : "ki";
            string sync = QualitySettings.vSyncCount > 0 ? $"be (/{QualitySettings.vSyncCount})" : "ki";
            string limit = $"FPS-korlát: {cap} · VSync: {sync}";
            body.text = $"{rate}\nCPU főszál: {FormatTiming(timing.cpuMainThreadFrameTime)} · GPU: {FormatTiming(timing.gpuFrameTime)}\n"
                + $"{Screen.width} × {Screen.height} · {quality} · Render: {scale}\n{limit}\n\n{hardware}";
        }

        private static string HardwareName(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value == SystemInfo.unsupportedIdentifier ? MissingValue : value;
        }

        private static string FormatTiming(double milliseconds) => milliseconds > 0 ? $"{milliseconds:0.0} ms" : MissingValue;

        private static string FormatMemory(int megabytes) => megabytes > 0 ? $"{megabytes:N0} MB" : MissingValue;
    }
}
