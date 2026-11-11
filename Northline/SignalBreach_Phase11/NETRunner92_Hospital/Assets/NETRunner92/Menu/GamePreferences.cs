using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NETRunner92
{
    internal enum GameAction { Forward, Back, Left, Right, Sprint, Interact, Tablet, Cctv, Stow }

    [Serializable]
    internal sealed class GamePreferences
    {
        internal static readonly Key[] DefaultBindings =
        {
            Key.W, Key.S, Key.A, Key.D, Key.LeftShift, Key.E, Key.Q, Key.Digit1, Key.R
        };
        internal static readonly string[] ActionNames =
        {
            "Előre", "Hátra", "Balra", "Jobbra", "Sprint", "Interakció", "LATCH-07", "CCTV", "Tablet elrakása"
        };

        public int version = 1;
        public int width, height, monitor;
        public FullScreenMode displayMode = FullScreenMode.FullScreenWindow;
        public bool vsync = true;
        public int frameLimit = 60;
        public int preset = 2;
        public int texture, shadows = 2, shadowResolution = 2, antialias = 3, renderScale = 100;
        public bool ao = true, volumetrics = true, bloom = true, motionBlur, depthOfField;
        public float fov = 68, brightness;
        public float master = 1, music = .65f, effects = 1, ambience = 1, ui = .8f;
        public bool mute;
        public float sensitivity = 1, smoothing;
        public bool invertY, sprintToggle;
        public Key[] keys = (Key[])DefaultBindings.Clone();
        public bool hud = true, prompts = true, objectives = true;
        public float headBob;
        public float uiScale = 1, hudOpacity = 1;
        public bool reducedMotion, reducedFlashing, highContrast;

        public GamePreferences Copy()
        {
            var copy = (GamePreferences)MemberwiseClone();
            copy.keys = (Key[])keys.Clone();
            return copy;
        }

        public void Validate()
        {
            monitor = Mathf.Clamp(monitor, 0, 31);
            if (width < 640 || height < 360 || width > 16384 || height > 8640)
                width = height = 0;
            if (displayMode != FullScreenMode.ExclusiveFullScreen && displayMode != FullScreenMode.FullScreenWindow
                && displayMode != FullScreenMode.Windowed)
                displayMode = FullScreenMode.FullScreenWindow;

            if (frameLimit != -1 && frameLimit != 30 && frameLimit != 60 && frameLimit != 90
                && frameLimit != 120 && frameLimit != 144 && frameLimit != 165 && frameLimit != 240)
                frameLimit = 60;

            preset = Mathf.Clamp(preset, 0, 4);
            texture = Mathf.Clamp(texture, 0, 2);
            shadows = Mathf.Clamp(shadows, 0, 3);
            shadowResolution = Mathf.Clamp(shadowResolution, 0, 3);
            antialias = Mathf.Clamp(antialias, 0, 3);
            renderScale = Mathf.Clamp(renderScale, 50, 100);
            fov = Bounded(fov, 55, 100, 68);
            brightness = Bounded(brightness, -1.5f, 1.5f, 0);
            master = Bounded(master, 0, 1, 1);
            music = Bounded(music, 0, 1, .65f);
            effects = Bounded(effects, 0, 1, 1);
            ambience = Bounded(ambience, 0, 1, 1);
            ui = Bounded(ui, 0, 1, .8f);
            sensitivity = Bounded(sensitivity, .25f, 3, 1);
            smoothing = Bounded(smoothing, 0, .85f, 0);
            headBob = Bounded(headBob, 0, 1, 0);
            uiScale = Bounded(uiScale, .8f, 1.25f, 1);
            hudOpacity = Bounded(hudOpacity, .25f, 1, 1);
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

        public void SetPreset(int value)
        {
            preset = value;
            if (value == 4) return;
            texture = value == 0 ? 2 : value == 1 ? 1 : 0;
            shadows = value == 0 ? 0 : value == 1 ? 1 : value == 2 ? 2 : 3;
            shadowResolution = value;
            antialias = value == 0 ? 1 : value == 1 ? 2 : 3;
            ao = value >= 2;
            volumetrics = value >= 1;
            bloom = value >= 1;
            motionBlur = depthOfField = false;
            renderScale = value == 0 ? 75 : value == 1 ? 90 : 100;
        }

        public bool DisplayDiffers(GamePreferences other) => width != other.width || height != other.height
            || displayMode != other.displayMode || monitor != other.monitor;

        public void ResetBindings() => keys = (Key[])DefaultBindings.Clone();

        public static bool AllowedKey(Key key) => key > Key.None && key <= Key.OEM5
            && key is not (Key.Escape or Key.Enter or Key.NumpadEnter or Key.Tab or Key.LeftArrow
                or Key.RightArrow or Key.UpArrow or Key.DownArrow or Key.Numpad1);

        static float Bounded(float value, float min, float max, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }

    internal static class HospitalKeys
    {
        public static bool Held(Keyboard keyboard, GameAction action) =>
            keyboard != null && keyboard[GameOptions.Active.keys[(int)action]].isPressed;

        public static bool Pressed(Keyboard keyboard, GameAction action) =>
            keyboard != null && keyboard[GameOptions.Active.keys[(int)action]].wasPressedThisFrame;

        public static bool CctvPressed(Keyboard keyboard) => Pressed(keyboard, GameAction.Cctv)
            || (GameOptions.Active.keys[(int)GameAction.Cctv] == Key.Digit1 && keyboard?.numpad1Key.wasPressedThisFrame == true);

        public static string Label(GameAction action)
        {
            var key = GameOptions.Active.keys[(int)action];
            return Keyboard.current?[key].displayName ?? key.ToString();
        }
    }

    internal static class LocalGameFiles
    {
        public static string ProfileDirectory
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, "-net92-profile");
                return i >= 0 && i + 1 < args.Length
                    ? Path.GetFullPath(args[i + 1]) : Path.Combine(Application.persistentDataPath, "Northline");
            }
        }

        public static void Write(string path, string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, json);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
