using System;
using System.IO;
using NetRunner.Core;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace NetRunner.Settings
{
    /// <summary>
    /// The player's settings while the game runs: the saved preferences, the
    /// ones in effect (a preview from the settings menu, or the preset given
    /// at launch), and the key bindings. One asset,
    /// <c>Settings/Resources/Preferences.asset</c>; components reach it
    /// through an Inspector reference (the player's controller holds it, the
    /// rest of the player's components ask the controller), and
    /// <see cref="PreferencesRuntime"/> applies it to the engine.
    /// Its runtime state is not saved in the asset: <see cref="Load"/>
    /// resets it at every start.
    /// </summary>
    [CreateAssetMenu(menuName = "NET.runner-92/Preferences", fileName = "Preferences")]
    public sealed class Preferences : ScriptableObject
    {
        private const string SettingsFile = "settings.json";
        private const string QualityOption = "-net92-quality";

        // Larger files are not settings files; they are ignored rather than read.
        private const long MaxFileBytes = 16384;

        [SerializeField] private AudioMixer mixer;

        [NonSerialized] private GamePreferences saved;
        [NonSerialized] private GamePreferences launchPreset;
        [NonSerialized] private string profileOverride;

        /// <summary>Raised whenever the preferences in effect change.</summary>
        public event Action Changed;

        /// <summary>The preferences in the settings file.</summary>
        public GamePreferences Saved => saved;

        /// <summary>The preferences in effect: the saved ones, a preview from the menu, or the launch preset.</summary>
        public GamePreferences Active { get; private set; }

        /// <summary>The preferences a scene starts with: the launch preset, or the saved ones.</summary>
        public GamePreferences StartingPreferences => launchPreset ?? saved;

        /// <summary>The hospital's audio mixer, whose groups the volume settings drive.</summary>
        public AudioMixer Mixer => mixer;

        /// <summary>The player's camera, which the graphics settings apply to; set by the player when it wakes.</summary>
        public Camera PlayerCamera { get; private set; }

        /// <summary>True when the game was started with -net92-quality: choices made this run are not saved.</summary>
        public bool QualityFixedAtLaunch => launchPreset != null;

        private string SettingsPath => Path.Combine(profileOverride ?? LocalGameFiles.ProfileDirectory, SettingsFile);

        /// <summary>
        /// Reads the settings file (defaults when it is missing, too large or
        /// of another version), repairs it, and applies a -net92-quality preset
        /// for this run. Clears the subscribers of a previous run.
        /// </summary>
        public void Load()
        {
            Changed = null;
            PlayerCamera = null;
            saved = ReadFile();
            saved.Validate();
            launchPreset = LaunchPreset(saved);
            Active = launchPreset ?? saved;
        }

        /// <summary>Uses another folder for the settings file; for tests, which may write only under Logs/.</summary>
        public void UseProfileDirectory(string directory)
        {
            profileOverride = directory;
        }

        public void UseCamera(Camera camera) => PlayerCamera = camera;

        /// <summary>Puts <paramref name="value"/> in effect without saving it, as the settings menu does while the player tries settings out.</summary>
        public void Preview(GamePreferences value)
        {
            Active = value;
            Changed?.Invoke();
        }

        /// <summary>Saves and applies <paramref name="value"/>; false with a message for the player when the file cannot be written.</summary>
        public bool Commit(GamePreferences value, out string error)
        {
            try
            {
                value.Validate();
                LocalGameFiles.Write(SettingsPath, JsonUtility.ToJson(value, true));
                saved = value.Copy();
                Preview(saved);
                error = null;
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning(exception.Message);
                error = "A beállításfájl nem írható.";
                return false;
            }
        }

        /// <summary>
        /// Switches to the preset of a quality level, as F2 does: saved like a
        /// choice in the menu when <paramref name="save"/> is true, otherwise
        /// only in effect for this run.
        /// </summary>
        public void SelectLevel(int level, bool save)
        {
            var next = Active.Copy();
            next.SetPreset(QualityPreset.ForLevel(level));
            if (!save || !Commit(next, out _))
            {
                Preview(next);
            }
        }

        public bool Held(Keyboard keyboard, GameAction action)
        {
            return keyboard != null && keyboard[Active.KeyFor(action)].isPressed;
        }

        public bool Pressed(Keyboard keyboard, GameAction action)
        {
            return keyboard != null && keyboard[Active.KeyFor(action)].wasPressedThisFrame;
        }

        /// <summary>The CCTV key; while it is bound to 1, keypad 1 works too.</summary>
        public bool CctvPressed(Keyboard keyboard)
        {
            return Pressed(keyboard, GameAction.Cctv)
                || (Active.KeyFor(GameAction.Cctv) == Key.Digit1 && keyboard?.numpad1Key.wasPressedThisFrame == true);
        }

        /// <summary>The label of the key bound to <paramref name="action"/>, as the keyboard layout names it.</summary>
        public string Label(GameAction action)
        {
            var key = Active.KeyFor(action);
            return Keyboard.current?[key].displayName ?? key.ToString();
        }

        private static GamePreferences LaunchPreset(GamePreferences basis)
        {
            string name = LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), QualityOption);
            int level = QualityChoice.FromName(name, QualitySettings.names);
            if (level < 0)
            {
                return null;
            }

            var run = basis.Copy();
            run.SetPreset(QualityPreset.ForLevel(level));
            return run;
        }

        private GamePreferences ReadFile()
        {
            try
            {
                var file = new FileInfo(SettingsPath);
                if (file.Exists && file.Length <= MaxFileBytes)
                {
                    var loaded = JsonUtility.FromJson<GamePreferences>(File.ReadAllText(SettingsPath));
                    if (loaded != null && loaded.IsCurrentVersion)
                    {
                        return loaded;
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Debug.LogWarning($"Northline settings defaults restored: {exception.Message}");
            }

            return GamePreferences.CreateDefault();
        }
    }
}
