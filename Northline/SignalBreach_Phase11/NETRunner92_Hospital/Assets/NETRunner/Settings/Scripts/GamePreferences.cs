using System;
using NetRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.Settings
{
    /// <summary>
    /// Everything the settings menu changes (Phase 11): display, graphics,
    /// audio, controls, gameplay and accessibility. Saved as JSON in the
    /// player's profile by <see cref="Preferences"/>; the field names are the
    /// file's keys, so they must not be renamed. The menu edits a copy (the
    /// draft) and previews it; <see cref="Validate"/> repairs values a file
    /// may hold that the menu cannot produce, using <see cref="PreferenceRules"/>.
    /// </summary>
    [Serializable]
    public sealed class GamePreferences
    {
        /// <summary>The version of the file format; a file of another version is replaced by the defaults.</summary>
        public const int FormatVersion = 1;

        private static readonly Key[] DefaultBindings =
        {
            Key.W, Key.S, Key.A, Key.D, Key.LeftShift, Key.E, Key.Q, Key.Digit1, Key.R,
        };

        private static readonly string[] Names =
        {
            "Előre", "Hátra", "Balra", "Jobbra", "Sprint", "Interakció", "LATCH-07", "CCTV", "Tablet elrakása",
        };

        [SerializeField] private int version = FormatVersion;
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private int monitor;
        [SerializeField] private FullScreenMode displayMode = FullScreenMode.FullScreenWindow;
        [SerializeField] private bool vsync = true;
        [SerializeField] private int frameLimit = PreferenceRules.DefaultFrameLimit;
        [SerializeField] private int preset = QualityPreset.High;
        [SerializeField] private int quality = QualityPreset.High;
        [SerializeField] private int texture;
        [SerializeField] private int shadows = 2;
        [SerializeField] private int shadowResolution = 2;
        [SerializeField] private int antialias = 3;
        [SerializeField] private int renderScale = QualityPreset.MaximumRenderScale;
        [SerializeField] private bool ao = true;
        [SerializeField] private bool volumetrics = true;
        [SerializeField] private bool bloom = true;
        [SerializeField] private bool motionBlur;
        [SerializeField] private bool depthOfField;
        [SerializeField] private float fov = PreferenceRules.DefaultFieldOfView;
        [SerializeField] private float brightness;
        [SerializeField] private float master = 1f;
        [SerializeField] private float music = PreferenceRules.DefaultMusic;
        [SerializeField] private float effects = 1f;
        [SerializeField] private float ambience = 1f;
        [SerializeField] private float ui = PreferenceRules.DefaultUi;
        [SerializeField] private bool mute;
        [SerializeField] private float sensitivity = 1f;
        [SerializeField] private float smoothing;
        [SerializeField] private bool invertY;
        [SerializeField] private bool sprintToggle;
        [SerializeField] private Key[] keys = (Key[])DefaultBindings.Clone();
        [SerializeField] private bool hud = true;
        [SerializeField] private bool prompts = true;
        [SerializeField] private bool objectives = true;
        [SerializeField] private float headBob;
        [SerializeField] private float uiScale = 1f;
        [SerializeField] private float hudOpacity = 1f;
        [SerializeField] private bool reducedMotion;
        [SerializeField] private bool reducedFlashing;
        [SerializeField] private bool highContrast;

        /// <summary>The number of rebindable actions.</summary>
        public static int ActionCount => DefaultBindings.Length;

        public bool IsCurrentVersion => version == FormatVersion;

        /// <summary>Window or exclusive screen size; 0 × 0 is the desktop's size.</summary>
        public int Width { get => width; set => width = value; }

        public int Height { get => height; set => height = value; }

        public int Monitor { get => monitor; set => monitor = value; }

        public FullScreenMode DisplayMode { get => displayMode; set => displayMode = value; }

        public bool VsyncEnabled { get => vsync; set => vsync = value; }

        /// <summary>Frames per second without VSync; <see cref="PreferenceRules.Unlimited"/> for none.</summary>
        public int FrameLimit { get => frameLimit; set => frameLimit = value; }

        /// <summary>The menu's quality preset (<see cref="QualityPreset"/>); Custom once a single setting changes.</summary>
        public int Preset { get => preset; set => preset = value; }

        /// <summary>The project quality level the preset selected (0 Low, 1 Medium, 2 High); kept for Custom.</summary>
        public int Quality => quality;

        /// <summary>Texture reduction: 0 full, 1 half, 2 quarter.</summary>
        public int Texture { get => texture; set => texture = value; }

        /// <summary>Shadow distance step: 0 off, then 18, 36 and 60 m.</summary>
        public int Shadows { get => shadows; set => shadows = value; }

        /// <summary>Shadow resolution step: 256, 512, as the scene sets it, 2048.</summary>
        public int ShadowResolution { get => shadowResolution; set => shadowResolution = value; }

        /// <summary>0 off, 1 FXAA, 2 SMAA, 3 TAA.</summary>
        public int Antialias { get => antialias; set => antialias = value; }

        /// <summary>Internal render resolution in percent.</summary>
        public int RenderScale { get => renderScale; set => renderScale = value; }

        public bool AmbientOcclusionEnabled { get => ao; set => ao = value; }

        public bool VolumetricsEnabled { get => volumetrics; set => volumetrics = value; }

        public bool BloomEnabled { get => bloom; set => bloom = value; }

        public bool MotionBlurEnabled { get => motionBlur; set => motionBlur = value; }

        public bool DepthOfFieldEnabled { get => depthOfField; set => depthOfField = value; }

        /// <summary>Vertical field of view in degrees.</summary>
        public float FieldOfView { get => fov; set => fov = value; }

        /// <summary>Exposure offset in EV.</summary>
        public float Brightness { get => brightness; set => brightness = value; }

        public float Master { get => master; set => master = value; }

        public float Music { get => music; set => music = value; }

        public float Effects { get => effects; set => effects = value; }

        public float Ambience { get => ambience; set => ambience = value; }

        public float Ui { get => ui; set => ui = value; }

        public bool Muted { get => mute; set => mute = value; }

        public float Sensitivity { get => sensitivity; set => sensitivity = value; }

        /// <summary>0 for raw mouse input, up to 0.85 for heavy smoothing.</summary>
        public float Smoothing { get => smoothing; set => smoothing = value; }

        public bool YInverted { get => invertY; set => invertY = value; }

        /// <summary>Whether the sprint key toggles sprinting instead of being held.</summary>
        public bool SprintToggles { get => sprintToggle; set => sprintToggle = value; }

        public bool ShowHud { get => hud; set => hud = value; }

        public bool ShowPrompts { get => prompts; set => prompts = value; }

        public bool ShowObjectives { get => objectives; set => objectives = value; }

        /// <summary>Head bob while walking, 0 (none) to 1 (full).</summary>
        public float HeadBob { get => headBob; set => headBob = value; }

        public float UiScale { get => uiScale; set => uiScale = value; }

        public float HudOpacity { get => hudOpacity; set => hudOpacity = value; }

        public bool ReduceMotion { get => reducedMotion; set => reducedMotion = value; }

        public bool ReduceFlashing { get => reducedFlashing; set => reducedFlashing = value; }

        public bool UseHighContrast { get => highContrast; set => highContrast = value; }

        /// <summary>The action's name in the settings menu.</summary>
        public static string ActionName(GameAction action) => Names[(int)action];

        /// <summary>
        /// Whether a key may be bound: a keyboard key, but not the keys the
        /// terminal and menus need (Escape, Enter, Tab, the arrows, keypad 1).
        /// </summary>
        public static bool AllowedKey(Key key)
        {
            return key > Key.None && key <= Key.OEM5
                && key is not (Key.Escape or Key.Enter or Key.NumpadEnter or Key.Tab or Key.LeftArrow
                    or Key.RightArrow or Key.UpArrow or Key.DownArrow or Key.Numpad1);
        }

        public Key KeyFor(GameAction action) => keys[(int)action];

        public void Bind(GameAction action, Key key) => keys[(int)action] = key;

        /// <summary>The action bound to <paramref name="key"/>, or null.</summary>
        public GameAction? ActionBoundTo(Key key)
        {
            int index = Array.IndexOf(keys, key);
            return index >= 0 ? (GameAction)index : null;
        }

        public GamePreferences Copy()
        {
            var copy = (GamePreferences)MemberwiseClone();
            copy.keys = (Key[])keys.Clone();
            return copy;
        }

        /// <summary>Repairs every value the menu cannot produce, so a hand-edited or old file cannot break the game.</summary>
        public void Validate()
        {
            monitor = Math.Clamp(monitor, 0, PreferenceRules.HighestMonitor);
            if (!PreferenceRules.IsWindowSize(width, height))
            {
                width = 0;
                height = 0;
            }

            if (displayMode != FullScreenMode.ExclusiveFullScreen && displayMode != FullScreenMode.FullScreenWindow
                && displayMode != FullScreenMode.Windowed)
            {
                displayMode = FullScreenMode.FullScreenWindow;
            }

            frameLimit = PreferenceRules.FrameLimit(frameLimit);
            preset = Math.Clamp(preset, QualityPreset.Low, QualityPreset.Custom);
            quality = QualityPreset.LevelFor(preset, quality);
            texture = Math.Clamp(texture, 0, 2);
            shadows = Math.Clamp(shadows, 0, 3);
            shadowResolution = Math.Clamp(shadowResolution, 0, 3);
            antialias = Math.Clamp(antialias, 0, 3);
            renderScale = Math.Clamp(renderScale, QualityPreset.MinimumRenderScale, QualityPreset.MaximumRenderScale);
            fov = PreferenceRules.Bounded(fov, PreferenceRules.MinimumFieldOfView, PreferenceRules.MaximumFieldOfView, PreferenceRules.DefaultFieldOfView);
            brightness = PreferenceRules.Bounded(brightness, -PreferenceRules.BrightnessRange, PreferenceRules.BrightnessRange, 0f);
            master = PreferenceRules.Bounded(master, 0f, 1f, 1f);
            music = PreferenceRules.Bounded(music, 0f, 1f, PreferenceRules.DefaultMusic);
            effects = PreferenceRules.Bounded(effects, 0f, 1f, 1f);
            ambience = PreferenceRules.Bounded(ambience, 0f, 1f, 1f);
            ui = PreferenceRules.Bounded(ui, 0f, 1f, PreferenceRules.DefaultUi);
            sensitivity = PreferenceRules.Bounded(sensitivity, PreferenceRules.MinimumSensitivity, PreferenceRules.MaximumSensitivity, 1f);
            smoothing = PreferenceRules.Bounded(smoothing, 0f, PreferenceRules.MaximumSmoothing, 0f);
            headBob = PreferenceRules.Bounded(headBob, 0f, 1f, 0f);
            uiScale = PreferenceRules.Bounded(uiScale, PreferenceRules.MinimumUiScale, PreferenceRules.MaximumUiScale, 1f);
            hudOpacity = PreferenceRules.Bounded(hudOpacity, PreferenceRules.MinimumHudOpacity, 1f, 1f);
            if (keys == null || keys.Length != DefaultBindings.Length)
            {
                ResetBindings();
                return;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                if (!AllowedKey(keys[i]) || Array.IndexOf(keys, keys[i]) != i)
                {
                    ResetBindings();
                    break;
                }
            }
        }

        /// <summary>Applies a preset's settings (<see cref="QualityPreset.For"/>); Custom keeps the current ones.</summary>
        public void SetPreset(int value)
        {
            preset = value;
            quality = QualityPreset.LevelFor(value, quality);
            if (value == QualityPreset.Custom)
            {
                return;
            }

            var settings = QualityPreset.For(value);
            texture = settings.Texture;
            shadows = settings.Shadows;
            shadowResolution = settings.ShadowResolution;
            antialias = settings.Antialias;
            ao = settings.AmbientOcclusion;
            volumetrics = settings.Volumetrics;
            bloom = settings.Bloom;
            motionBlur = false;
            depthOfField = false;
            renderScale = settings.RenderScale;
        }

        /// <summary>Whether applying these preferences would change the window or screen mode.</summary>
        public bool DisplayDiffers(GamePreferences other)
        {
            return width != other.width || height != other.height || displayMode != other.displayMode || monitor != other.monitor;
        }

        /// <summary>Takes the display part (size, mode, monitor) from <paramref name="other"/>.</summary>
        public void CopyDisplay(GamePreferences other)
        {
            width = other.width;
            height = other.height;
            displayMode = other.displayMode;
            monitor = other.monitor;
        }

        public void ResetBindings() => keys = (Key[])DefaultBindings.Clone();
    }
}
