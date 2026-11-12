using System;
using System.Collections;
using System.Collections.Generic;
using NetRunner.Core;
using UnityEngine;

namespace NetRunner.Settings
{
    /// <summary>
    /// The monitors, the screen sizes the settings menu offers, and switching
    /// the window to a size and mode (Phase 11). Monitors are listed on the
    /// Windows player only; elsewhere the current display is the only one.
    /// Switching is a coroutine because the engine applies a new mode over
    /// the next frames.
    /// </summary>
    public sealed class DisplayModes
    {
        // Windowed sizes offered besides the display's own modes, where they fit.
        private static readonly Vector2Int[] WindowSizes =
        {
            new Vector2Int(960, 540), new Vector2Int(1280, 720), new Vector2Int(1280, 800), new Vector2Int(1600, 900),
            new Vector2Int(1680, 1050), new Vector2Int(1920, 1080), new Vector2Int(1920, 1200), new Vector2Int(2560, 1080),
            new Vector2Int(2560, 1440), new Vector2Int(3440, 1440), new Vector2Int(3840, 2160),
        };

        // A window that no longer fits falls back to 1280 × 720, or the desktop when that is smaller.
        private static readonly Vector2Int FallbackWindow = new Vector2Int(1280, 720);

        // Seconds between attempts to restore a mode Windows did not accept at once.
        private const float RetrySeconds = .45f;
        private const int RollbackAttempts = 3;

        private readonly List<DisplayInfo> displays = new List<DisplayInfo>();
        private readonly List<Vector2Int> modes = new List<Vector2Int>();

        /// <summary>The monitors, freshly listed.</summary>
        public IReadOnlyList<DisplayInfo> Displays()
        {
            displays.Clear();
            if (!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer)
            {
                Screen.GetDisplayLayout(displays);
            }

            return displays;
        }

        /// <summary>The index of the monitor the window is on.</summary>
        public int CurrentMonitor()
        {
            var list = Displays();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(Screen.mainWindowDisplayInfo))
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>The sizes the menu offers for these preferences; the first, 0 × 0, is the desktop's size.</summary>
        public IReadOnlyList<Vector2Int> ResolutionChoices(GamePreferences value)
        {
            Displays();
            modes.Clear();
            modes.Add(Vector2Int.zero);
            bool current = displays.Count <= 1 || displays[Math.Clamp(value.Monitor, 0, displays.Count - 1)].Equals(Screen.mainWindowDisplayInfo);
            if (current)
            {
                foreach (var mode in Screen.resolutions)
                {
                    AddMode(new Vector2Int(mode.width, mode.height));
                }
            }

            if (!current || value.DisplayMode == FullScreenMode.Windowed || modes.Count == 1)
            {
                Vector2Int desktop = Desktop(value.Monitor);
                foreach (var size in WindowSizes)
                {
                    if (size.x <= desktop.x && size.y <= desktop.y)
                    {
                        AddMode(size);
                    }
                }

                AddMode(desktop);
            }

            return modes;
        }

        /// <summary>
        /// Moves the window to the chosen monitor and applies the size and
        /// mode. With <paramref name="rollback"/> it insists until Windows
        /// reports the mode, because a return to fullscreen can be deferred
        /// until the window has focus again.
        /// </summary>
        public IEnumerator Apply(GamePreferences value, bool rollback = false)
        {
            if (Application.isEditor)
            {
                yield break;
            }

            Displays();
            if (displays.Count > 1)
            {
                var target = displays[Math.Clamp(value.Monitor, 0, displays.Count - 1)];
                if (!target.Equals(Screen.mainWindowDisplayInfo))
                {
                    Screen.fullScreenMode = FullScreenMode.Windowed;
                    yield return null;
                    yield return Screen.MoveMainWindowTo(in target, Vector2Int.zero);
                }
            }

            var size = TargetSize(value);
            yield return Request(size, value.DisplayMode);
            if (!rollback || Matches(size, value.DisplayMode))
            {
                yield break;
            }

            while (!Application.isFocused && !Matches(size, value.DisplayMode))
            {
                yield return null;
            }

            for (int attempt = 0; attempt < RollbackAttempts && !Matches(size, value.DisplayMode); attempt++)
            {
                yield return Request(size, value.DisplayMode);
                yield return new WaitForSecondsRealtime(RetrySeconds);
            }

            if (!Matches(size, value.DisplayMode))
            {
                Debug.LogWarning($"Display rollback requested {size.x}×{size.y} {value.DisplayMode}, "
                    + $"Windows reports {Screen.width}×{Screen.height} {Screen.fullScreenMode}.");
            }
        }

        private static IEnumerator Request(Vector2Int size, FullScreenMode mode)
        {
            if (Screen.fullScreenMode != mode)
            {
                Screen.fullScreenMode = mode;
                yield return null;
            }

            Screen.SetResolution(size.x, size.y, mode);
            yield return null;
            yield return null;
        }

        private static bool Matches(Vector2Int size, FullScreenMode mode)
        {
            return Screen.fullScreenMode == mode && Screen.width == size.x && Screen.height == size.y;
        }

        private Vector2Int TargetSize(GamePreferences value)
        {
            var size = new Vector2Int(value.Width, value.Height);
            var desktop = Desktop(value.Monitor);
            if (value.DisplayMode == FullScreenMode.FullScreenWindow || size == Vector2Int.zero)
            {
                return desktop;
            }

            if (value.DisplayMode == FullScreenMode.ExclusiveFullScreen)
            {
                return Array.Exists(Screen.resolutions, r => r.width == size.x && r.height == size.y) ? size : desktop;
            }

            return size.x > desktop.x || size.y > desktop.y
                ? new Vector2Int(Mathf.Min(FallbackWindow.x, desktop.x), Mathf.Min(FallbackWindow.y, desktop.y))
                : size;
        }

        private void AddMode(Vector2Int size)
        {
            if (size.x >= PreferenceRules.MinimumWidth && size.y >= PreferenceRules.MinimumHeight && !modes.Contains(size))
            {
                modes.Add(size);
            }
        }

        private Vector2Int Desktop(int monitor)
        {
            if (displays.Count != 0)
            {
                var display = displays[Math.Clamp(monitor, 0, displays.Count - 1)];
                return new Vector2Int(display.width, display.height);
            }

            var current = Screen.currentResolution;
            return new Vector2Int(current.width, current.height);
        }
    }
}
