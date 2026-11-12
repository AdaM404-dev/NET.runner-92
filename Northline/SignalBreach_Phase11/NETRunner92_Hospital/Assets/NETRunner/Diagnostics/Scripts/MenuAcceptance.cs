using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NetRunner.Atmosphere;
using NetRunner.Core;
using NetRunner.Hud;
using NetRunner.Menu;
using NetRunner.Player;
using NetRunner.Saves;
using NetRunner.Settings;
using NetRunner.World;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;

namespace NetRunner.Diagnostics
{
    // Runs only in an explicitly requested, isolated Windows acceptance test.
    public sealed class MenuAcceptance : MonoBehaviour
    {
        const string RunFlag = "-net92-menu-check";
        const string ReloadFlag = "-net92-menu-reload-check";
        private const float PerformancePanelSettleSeconds = 0.75f;
        private const float MenuRateSampleSeconds = 2f;
        private const float MenuRateTolerance = 2f;
        readonly List<string> results = new();
        readonly CheckpointStore store = CheckpointStore.ForProfile();
        Preferences preferences;
        Keyboard keyboard;
        Mouse mouse;
        string path;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            var args = Environment.GetCommandLineArgs();
            if ((!Array.Exists(args, x => x == RunFlag || x == ReloadFlag))
                || Array.IndexOf(args, "-net92-profile") < 0) return;
            var node = new GameObject("Menu acceptance");
            DontDestroyOnLoad(node);
            node.AddComponent<MenuAcceptance>();
        }

        IEnumerator Start()
        {
            bool reload = Array.IndexOf(Environment.GetCommandLineArgs(), ReloadFlag) >= 0;
            // A diagnostic tool may look the shared settings asset up instead of being given it.
            preferences = Resources.Load<Preferences>(PreferencesRuntime.ResourcePath);
            path = Path.Combine(LocalGameFiles.ProfileDirectory, reload ? "reload-checks.txt" : "menu-checks.txt");
            yield return new WaitForSecondsRealtime(1.0f);
            var score = GameObject.Find("Menu score")?.GetComponent<AudioSource>();
            Check(score && score.clip && score.clip.name == "Deep Atmospheric Pads" && score.isPlaying
                && score.loop && score.timeSamples > 0,
                "Deep Atmospheric Pads is playing and configured to loop on the menu music source.");
            if (reload)
            {
                Check(Mathf.Abs(preferences.Saved.Master - .43f) < .001f && Mathf.Abs(AudioListener.volume - .43f) < .001f,
                    "After restarting, saved master volume is loaded and applied.");
                Check(preferences.Saved.KeyFor(GameAction.Interact) == Key.F,
                    "After restarting, the interaction binding is still F.");
                Check(preferences.Saved.DisplayMode == FullScreenMode.Windowed && Screen.fullScreenMode == FullScreenMode.Windowed,
                    "After restarting, the confirmed windowed display mode is applied.");
                Finish();
                yield break;
            }

            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
            var root = MenuRoot();
            if (!Check(root != null && SceneManager.GetActiveScene().name == HospitalRun.MenuScene,
                "The Windows player starts in MainMenu."))
            {
                Finish();
                yield break;
            }

            Check(root.Q<Button>("continue") == null, "An empty profile hides Continue.");
            Check(QualitySettings.vSyncCount == 0 && Application.targetFrameRate == FramePacing.MenuLimit,
                "The main menu uses its own frame limit at startup.");
            ScreenCapture.CaptureScreenshot(Path.Combine(LocalGameFiles.ProfileDirectory, "main-menu.png"));
            yield return null;
            Submit(root.Q<Button>("settings"));
            yield return null;
            if (!Check(root.Q("settings-rows") != null, "The settings screen is available."))
            {
                Finish();
                yield break;
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(LocalGameFiles.ProfileDirectory, "settings.png"));

            Submit(root.Q<Button>("category-1"));
            root.Q<Slider>("master").value = .43f;
            root.Q<Slider>("music").value = .54f;
            root.Q<Slider>("effects").value = .60f;
            root.Q<Slider>("ambience").value = .36f;
            root.Q<Slider>("ui-volume").value = .35f;
            yield return null;
            var mixer = Array.Find(Resources.FindObjectsOfTypeAll<AudioMixer>(), m => m.name == "Northline_Ambience");
            Check(Mathf.Abs(AudioListener.volume - .43f) < .001f
                && mixer && mixer.GetFloat("NorthlineExterior", out float outside)
                && Mathf.Abs(outside - PreferenceRules.ToDecibels(.36f)) < .01f
                && mixer.GetFloat("NorthlineEffects", out float sfx)
                && Mathf.Abs(sfx - PreferenceRules.ToDecibels(.60f)) < .01f,
                "Audio preview changes the actual master, ambience and effect buses.");
            Check(Mathf.Abs(GameObject.Find("Menu score").GetComponent<AudioSource>().volume - .54f * .25f) < .001f
                && Mathf.Abs(GameObject.Find("Menu interface feedback").GetComponent<AudioSource>().volume - .35f * .30f) < .001f,
                "Music and UI sliders change their actual independent AudioSource gains.");
            root.Q<Toggle>("mute").value = true;
            Check(AudioListener.volume == 0, "Mute takes effect immediately.");
            root.Q<Toggle>("mute").value = false;
            Submit(root.Q<Button>("settings-cancel"));
            Check(AudioListener.volume == 1 && preferences.Active == preferences.Saved, "Cancel rolls back live previews.");

            Submit(root.Q<Button>("settings"));
            Submit(root.Q<Button>("settings-defaults"));
            Submit(root.Q<Button>("dialog-confirm"));
            Check(preferences.Active.FieldOfView == 68 && AudioListener.volume == 1, "Restore Defaults previews factory preferences.");
            Submit(root.Q<Button>("category-1"));
            root.Q<Slider>("master").value = .43f;
            Submit(root.Q<Button>("category-2"));
            Submit(root.Q<Button>("binding-5"));
            yield return Press(Key.S);
            Check(preferences.Active.KeyFor(GameAction.Interact) == Key.E, "An existing movement key cannot be reused.");
            yield return Press(Key.F);
            Check(preferences.Active.KeyFor(GameAction.Interact) == Key.F, "A physical keypress rebinds Interact.");
            Submit(root.Q<Button>("binding-5"));
            yield return Press(Key.Escape);
            Check(root.Q<Button>("binding-5") != null && preferences.Active.KeyFor(GameAction.Interact) == Key.F,
                "Escape cancels only the binding capture and keeps Settings open.");
            Submit(root.Q<Button>("category-0"));
            root.Q<DropdownField>("quality").index = 0;
            Check(preferences.Active.Texture == 2 && QualitySettings.globalTextureMipmapLimit == 2,
                "The Low preset changes the actual global mip limit.");
            root.Q<DropdownField>("quality").index = 2;
            root.Q<Toggle>("vsync").value = false;
            root.Q<DropdownField>("frame-limit").index = 4;
            root.Q<Slider>("fov").value = 76;
            root.Q<Slider>("render-scale").value = 80;
            root.Q<Slider>("brightness").value = .4f;
            root.Q<DropdownField>("antialias").index = 2;
            root.Q<DropdownField>("shadows").index = 1;
            root.Q<DropdownField>("shadow-resolution").index = 1;
            root.Q<Toggle>("bloom").value = false;
            root.Q<Toggle>("ao").value = false;
            root.Q<Toggle>("volumetrics").value = false;
            root.Q<Toggle>("motion-blur").value = true;
            root.Q<Toggle>("depth-of-field").value = true;
            Submit(root.Q<Button>("category-3"));
            root.Q<Toggle>("prompts").value = false;
            root.Q<Toggle>("objectives").value = false;
            Submit(root.Q<Button>("category-4"));
            root.Q<Toggle>("high-contrast").value = true;
            root.Q<Toggle>("reduced-flashing").value = true;
            root.Q<Slider>("hud-opacity").value = .80f;
            float menuWidth = root.worldBound.width;
            root.Q<Slider>("ui-scale").value = 1.12f;
            yield return null;
            yield return null;
            Check(root.worldBound.width < menuWidth && root.ClassListContains("contrast"),
                "UI scale changes the rendered panel width and high contrast changes the menu style.");
            Submit(root.Q<Button>("category-0"));
            Submit(root.Q<Button>("settings-apply"));
            Check(preferences.Saved.FieldOfView == 76 && !preferences.Saved.VsyncEnabled
                && preferences.Saved.FrameLimit == 120 && Application.targetFrameRate == FramePacing.MenuLimit,
                "Apply saves the gameplay limit while the main menu keeps its own limit.");

            root.Q<DropdownField>("display-mode").index = 2;
            SetResolution(root, "1280 × 720");
            int oldWidth = Screen.width, oldHeight = Screen.height;
            var oldMode = Screen.fullScreenMode;
            Submit(root.Q<Button>("settings-apply"));
            yield return new WaitForSecondsRealtime(1);
            Check(root.Q<Button>("dialog-confirm") != null && Screen.fullScreenMode == FullScreenMode.Windowed,
                "The display preview opens a confirmation and applies windowed mode.");
            ScreenCapture.CaptureScreenshot(Path.Combine(LocalGameFiles.ProfileDirectory, "display-confirm.png"));
            yield return new WaitForSecondsRealtime(15.5f);
            float restoreLimit = Time.realtimeSinceStartup + 7;
            while ((Screen.width != oldWidth || Screen.height != oldHeight || Screen.fullScreenMode != oldMode)
                && Time.realtimeSinceStartup < restoreLimit)
                yield return null;
            yield return new WaitForSecondsRealtime(.4f);
            Check(Screen.width == oldWidth && Screen.height == oldHeight && Screen.fullScreenMode == oldMode
                && preferences.Saved.DisplayMode != FullScreenMode.Windowed,
                "The unconfirmed display trial times out and restores its original mode and dimensions.");

            Debug.Log($"Display restored from {oldWidth}×{oldHeight} {oldMode} to {Screen.width}×{Screen.height} {Screen.fullScreenMode}.");
            root = MenuRoot();
            if (!Check(root?.Q<DropdownField>("display-mode") != null
                && root.Q<Button>("settings-apply").enabledInHierarchy,
                "The graphics drawer is usable again after reverting a display trial."))
            {
                Finish();
                yield break;
            }
            root.Q<DropdownField>("display-mode").index = 2;
            SetResolution(root, "1280 × 720");
            Submit(root.Q<Button>("settings-apply"));
            yield return new WaitForSecondsRealtime(1.2f);
            Submit(root.Q<Button>("dialog-confirm"));
            yield return null;
            Check(preferences.Saved.DisplayMode == FullScreenMode.Windowed && preferences.Saved.Width == 1280,
                "Confirming a display trial saves the new windowed resolution.");
            Submit(root.Q<Button>("settings-cancel"));

            Submit(root.Q<Button>("new-game"));
            Submit(root.Q<Button>("slot-action-0"));
            yield return WaitForScene(HospitalRun.HospitalScene);
            var run = FindAnyObjectByType<HospitalRun>();
            var player = FindAnyObjectByType<FirstPersonController>();
            if (!Check(run && player && store.Read(0) != null, "New Game starts the hospital and writes its initial checkpoint."))
            {
                Finish();
                yield break;
            }

            Check(QualitySettings.vSyncCount == 0 && Application.targetFrameRate == 120,
                "Gameplay restores the player's selected frame limit and VSync.");

            var hudDoc = FindAnyObjectByType<HospitalHud>().GetComponent<UIDocument>();
            var hudRoot = hudDoc.rootVisualElement;
            Check(Mathf.Abs(hudDoc.panelSettings.scale - 1.12f) < .001f
                && Mathf.Abs(hudRoot.Q("world").resolvedStyle.opacity - .80f) < .001f
                && hudRoot.ClassListContains("hud-contrast") && hudRoot.Q("goal").ClassListContains("hidden"),
                "HUD scale, opacity, contrast and objective visibility affect the live gameplay UI.");
            var hd = player.Eyes.GetComponent<HDAdditionalCameraData>();
            var frames = hd.renderingPathCustomFrameSettings;
            Check(!frames.IsEnabled(FrameSettingsField.Decals)
                && hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Decals],
                "The hospital camera skips the unused decal pass.");
            Check(Mathf.Approximately(player.Eyes.fieldOfView, 76) && hd.allowDynamicResolution
                && hd.antialiasing == HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                "FOV, software render scale permission and SMAA apply to the real HDRP player camera.");
            Check(!frames.IsEnabled(FrameSettingsField.SSAO) && !frames.IsEnabled(FrameSettingsField.Volumetrics)
                && !frames.IsEnabled(FrameSettingsField.Bloom) && frames.IsEnabled(FrameSettingsField.MotionBlur)
                && frames.IsEnabled(FrameSettingsField.DepthOfField) && frames.IsEnabled(FrameSettingsField.ShadowMaps),
                "HDRP frame settings reflect the selected post-processing, AO, fog and shadow controls.");
            int renderedWidth = HDCamera.GetOrCreate(player.Eyes).actualWidth;
            Check(Mathf.Abs(renderedWidth - Screen.width * .8f) <= 2,
                $"HDRP renders {renderedWidth} internal pixels for a {Screen.width}-pixel window at 80% scale.");
            var stack = VolumeManager.instance.stack;
            var settings = GameObject.Find("Runtime HDRP preferences").GetComponent<Volume>().profile;
            Check(settings.TryGet<HDShadowSettings>(out var shadows) && Mathf.Approximately(shadows.maxShadowDistance.value, 18)
                && settings.TryGet<ColorAdjustments>(out var color) && Mathf.Abs(color.postExposure.value - .4f) < .01f
                && stack != null, "HDRP volume overrides store the applied 18 m shadow range and exposure.");

            player.CaptureCursor(true);
            player.Warp(new Vector3(0, .04f, -20.65f), Quaternion.identity);
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.25f);
            var interaction = player.GetComponent<HospitalInteraction>();
            var entrance = interaction.TargetInView() as HospitalDoor;
            Check(entrance && hudRoot.Q("prompt").ClassListContains("hidden"),
                "Disabled interaction prompts stay hidden while aiming at a real door.");
            var lamp = FindAnyObjectByType<PracticalPulse>().GetComponent<Light>();
            float lampIntensity = lamp.intensity;
            yield return new WaitForSecondsRealtime(.55f);
            Check(Mathf.Approximately(lamp.intensity, lampIntensity), "Reduced flashing keeps a real practical light steady.");
            yield return Press(Key.E);
            bool oldClosed = entrance && entrance.Closed;
            yield return Press(Key.F);
            yield return new WaitForSecondsRealtime(.85f);
            Debug.Log($"Input probe: focused={Application.isFocused}, cursor={Cursor.lockState}, target={entrance}, " +
                $"binding={preferences.Active.KeyFor(GameAction.Interact)}, position={player.transform.position}");
            Check(oldClosed && entrance && entrance.Open, "The rebound F operates the real entrance and E no longer does.");
            FindAnyObjectByType<StairAccess>().RestoreProgress(2);
            FindAnyObjectByType<ReceptionSecurityTerminal>().RestoreUnlock();
            FindAnyObjectByType<LatchRelay>().RestoreLink();
            yield return Press(Key.Escape);
            if (!Check(player.InputPaused, "Escape opens the in-game pause menu."))
            {
                Finish();
                yield break;
            }
            var pause = run.GetComponent<UIDocument>().rootVisualElement;
            Submit(pause.Q<Button>("pause-save"));
            yield return null;
            var checkpoint = store.Read(0);
            Check(player.InputPaused && checkpoint != null && checkpoint.CctvUnlocked && checkpoint.RelayLinked
                && checkpoint.HighestFloor == 2 && entrance && Array.IndexOf(checkpoint.OpenDoors, entrance.name) >= 0,
                "Pause saves actual player progress, unlocks and open doors.");
            Submit(pause.Q<Button>("pause-leave"));
            Submit(pause.Q<Button>("pause-confirm"));
            yield return WaitForScene(HospitalRun.MenuScene);
            root = MenuRoot();
            Check(root.Q<Button>("continue") != null, "Continue appears when a valid slot exists.");

            Submit(root.Q<Button>("new-game"));
            Submit(root.Q<Button>("slot-action-0"));
            Check(root.Q<Button>("dialog-confirm") != null, "Starting over in an occupied slot requires confirmation.");
            Submit(root.Q<Button>("dialog-cancel"));
            Check(store.Read(0).Time == checkpoint.Time, "Cancel preserves the existing checkpoint.");
            Submit(root.Q<Button>("drawer-back"));

            Submit(root.Q<Button>("load-game"));
            ScreenCapture.CaptureScreenshot(Path.Combine(LocalGameFiles.ProfileDirectory, "saves.png"));
            yield return new WaitForSecondsRealtime(.35f);
            Submit(root.Q<Button>("slot-action-0"));
            yield return WaitForScene(HospitalRun.HospitalScene);
            player = FindAnyObjectByType<FirstPersonController>();
            Check(player && Vector3.Distance(player.transform.position, checkpoint.Position) < .2f
                && FindAnyObjectByType<SecurityCctv>().Unlocked && FindAnyObjectByType<StairAccess>().Highest == 2
                && FindAnyObjectByType<LatchRelay>().Linked,
                "Load Game restores position and real stair, CCTV and relay progression.");

            player.CaptureCursor(true);
            yield return Press(Key.Escape);
            pause = FindAnyObjectByType<HospitalRun>().GetComponent<UIDocument>().rootVisualElement;
            Submit(pause.Q<Button>("pause-leave"));
            Submit(pause.Q<Button>("pause-confirm"));
            yield return WaitForScene(HospitalRun.MenuScene);
            root = MenuRoot();
            Submit(root.Q<Button>("continue"));
            yield return WaitForScene(HospitalRun.HospitalScene);
            Check(SceneManager.GetActiveScene().name == HospitalRun.HospitalScene, "Continue loads the valid latest checkpoint.");

            FindAnyObjectByType<FirstPersonController>().CaptureCursor(true);
            yield return Press(Key.Escape);
            pause = FindAnyObjectByType<HospitalRun>().GetComponent<UIDocument>().rootVisualElement;
            Submit(pause.Q<Button>("pause-leave"));
            Submit(pause.Q<Button>("pause-confirm"));
            yield return WaitForScene(HospitalRun.MenuScene);
            root = MenuRoot();
            Submit(root.Q<Button>("load-game"));
            Submit(root.Q<Button>("slot-delete-0"));
            Submit(root.Q<Button>("dialog-cancel"));
            Check(store.Read(0) != null, "Canceling deletion leaves the slot intact.");
            Submit(root.Q<Button>("slot-delete-0"));
            Submit(root.Q<Button>("dialog-confirm"));
            Check(!store.Exists(0), "Confirmed deletion removes the actual save file.");
            File.WriteAllText(Path.Combine(LocalGameFiles.ProfileDirectory, "slot-1.json"), "{ invalid");
            Submit(root.Q<Button>("drawer-back"));
            Check(store.MostRecent() < 0 && root.Q<Button>("continue") == null,
                "A corrupted slot does not appear under Continue.");
            Submit(root.Q<Button>("load-game"));
            Check(!root.Q<Button>("slot-action-0").enabledSelf, "A corrupted slot is visible but cannot be loaded.");
            Submit(root.Q<Button>("slot-delete-0"));
            Submit(root.Q<Button>("dialog-confirm"));
            Submit(root.Q<Button>("drawer-back"));

            foreach (var size in new[] { new Vector2Int(1600, 900), new Vector2Int(1440, 900), new Vector2Int(1680, 720) })
            {
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.45f);
                Rect panel = root.worldBound;
                Rect button = root.Q<Button>("exit").worldBound;
                Check(Screen.width == size.x && Screen.height == size.y && panel.Contains(button.min) && panel.Contains(button.max),
                    $"Menu actions fit in a real {size.x}×{size.y} window.");
                ScreenCapture.CaptureScreenshot(Path.Combine(LocalGameFiles.ProfileDirectory, $"menu-{size.x}x{size.y}.png"));
                yield return null;
            }

            yield return CheckMenuFrameRate();
            var menuCamera = Camera.main;
            Check(menuCamera && menuCamera.TryGetComponent<HDAdditionalCameraData>(out var menuCameraData)
                && !menuCameraData.allowDynamicResolution,
                "The menu camera leaves HDRP dynamic resolution off for the UI.");
            yield return CheckMenuInput(root);
            yield return CheckPerformanceOverlay();

            Submit(root.Q<Button>("exit"));
            Check(root.Q<Button>("dialog-confirm") != null, "Exit opens its confirmation.");
            Submit(root.Q<Button>("dialog-cancel"));
            Check(root.Q<Button>("exit").enabledInHierarchy, "Canceling Exit returns keyboard focus to the menu.");
            if (results.Exists(x => x.StartsWith("FAIL")))
            {
                Finish();
                yield break;
            }
            Submit(root.Q<Button>("exit"));
            Check(true, "The Exit confirmation was submitted to Application.Quit.");
            Submit(root.Q<Button>("dialog-confirm"));
            yield return new WaitForSecondsRealtime(4);
            Check(false, "The process did not quit after confirmation.");
            Finish();
        }

        static VisualElement MenuRoot() => FindAnyObjectByType<MainMenuView>()?.GetComponent<UIDocument>().rootVisualElement;

        private IEnumerator CheckMenuFrameRate()
        {
            int startFrame = Time.frameCount;
            double start = Time.realtimeSinceStartupAsDouble;
            yield return new WaitForSecondsRealtime(MenuRateSampleSeconds);
            double fps = (Time.frameCount - startFrame) / (Time.realtimeSinceStartupAsDouble - start);
            Check(fps > 0 && fps <= FramePacing.MenuLimit + MenuRateTolerance,
                $"The main menu averages {fps:F1} FPS with its {FramePacing.MenuLimit} FPS limit.");
        }

        private IEnumerator CheckMenuInput(VisualElement root)
        {
            // Run the pointer and focus checks once the window has rendered its layout.
            root.Q<Button>("new-game").Focus();
            yield return null;
            yield return Press(Key.DownArrow);
            Check(root.focusController.focusedElement == root.Q<Button>("load-game"),
                "Physical Input System navigation moves focus with the down arrow.");

            yield return Click(root, root.Q<Button>("settings"));
            bool mouseOpened = root.Q("settings-rows") != null;
            Check(mouseOpened, "A mouse click opens Settings.");
            if (mouseOpened)
            {
                Submit(root.Q<Button>("settings-cancel"));
                yield return null;
            }
        }

        private IEnumerator CheckPerformanceOverlay()
        {
            var overlay = FindAnyObjectByType<PerformanceOverlay>();
            if (!Check(overlay && !overlay.IsVisible, "The F4 panel stays hidden until the player opens it."))
            {
                yield break;
            }

            yield return Press(Key.F4);
            Check(overlay.IsVisible, "F4 opens the performance panel in the Windows player.");
            yield return new WaitForSecondsRealtime(PerformancePanelSettleSeconds);
            ScreenCapture.CaptureScreenshot(Path.Combine(LocalGameFiles.ProfileDirectory, "performance-panel.png"));
            yield return null;
            yield return null;
            yield return Press(Key.F4);
            Check(!overlay.IsVisible, "A second F4 press hides the performance panel.");
        }

        static void SetResolution(VisualElement root, string value)
        {
            var dropdown = root.Q<DropdownField>("resolution");
            int index = dropdown.choices.IndexOf(value);
            if (index >= 0) dropdown.index = index;
        }

        static void Submit(Button button)
        {
            if (button == null) return;
            using var evt = NavigationSubmitEvent.GetPooled();
            evt.target = button;
            button.SendEvent(evt);
        }

        IEnumerator Click(VisualElement root, Button button)
        {
            Vector2 center = button.worldBound.center;
            var point = new Vector2(center.x / root.worldBound.width * Screen.width,
                Screen.height - center.y / root.worldBound.height * Screen.height);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return new WaitForSecondsRealtime(.3f);
        }

        IEnumerator Press(Key key)
        {
            if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(.20f);
        }

        static IEnumerator WaitForScene(string scene)
        {
            float until = Time.realtimeSinceStartup + 30;
            while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup < until) yield return null;
            yield return new WaitForSecondsRealtime(.65f);
        }

        bool Check(bool condition, string description)
        {
            results.Add((condition ? "PASS " : "FAIL ") + description);
            Debug.Log(results[^1]);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, results);
            return condition;
        }

        void Finish() => Application.Quit(results.Exists(x => x.StartsWith("FAIL")) ? 2 : 0);
    }
}
