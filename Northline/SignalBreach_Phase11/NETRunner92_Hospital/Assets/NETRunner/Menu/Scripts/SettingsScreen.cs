using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetRunner.Core;
using NetRunner.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace NetRunner.Menu
{
    /// <summary>
    /// The settings drawer of the main menu (Phase 11): graphics, audio,
    /// controls with key rebinding, gameplay and accessibility. It edits a
    /// copy of the saved preferences (the draft) and previews every change at
    /// once; Apply saves it, Cancel goes back to the saved ones. A new window
    /// size or mode is tried first and kept only when the player confirms
    /// within 15 seconds. <see cref="MainMenuView"/> owns it and lends it the
    /// drawer, the dialogs and its coroutines.
    /// </summary>
    public sealed class SettingsScreen
    {
        private const int GraphicsTab = 0;
        private const int ControlsTab = 2;

        // Seconds a trial display mode waits for confirmation before it is undone.
        private const double DisplayTrialSeconds = 15;

        private static readonly string[] Categories = { "Grafika", "Hang", "Irányítás", "Játékmenet", "Akadálymentesítés" };

        private static readonly FullScreenMode[] ScreenModes =
        {
            FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed,
        };

        private readonly MainMenuView host;
        private readonly DisplayModes display = new DisplayModes();
        private GamePreferences draft;
        private ScrollView settingRows;
        private VisualElement tabs;
        private DropdownField qualityChoice;
        private IDisposable bindingListener;
        private Coroutine displayCountdown;
        private GamePreferences displayBeforeTrial;
        private int category;
        private int bindingStartFrame;
        private int bindingCancelFrame = -1;
        private bool switchingDisplay;

        public SettingsScreen(MainMenuView owner)
        {
            host = owner;
        }

        public bool IsOpen { get; private set; }

        private Preferences Preferences => host.Preferences;

        private MenuWidgets Widgets => host.Widgets;

        /// <summary>Whether Escape belongs to the key capture this frame (it cancels the capture, not the drawer).</summary>
        public bool CapturesEscape(int frame) => bindingListener != null || frame == bindingCancelFrame;

        /// <summary>Opens the drawer on the graphics tab with a fresh draft of the saved preferences.</summary>
        public void Open()
        {
            draft = Preferences.Saved.Copy();
            IsOpen = true;
            host.OpenDrawer("RENDSZER   /   FELHASZNÁLÓI PROFIL", "Beállítások");
            var layout = MenuWidgets.Box("settings-layout");
            tabs = MenuWidgets.Box("settings-tabs");
            for (int i = 0; i < Categories.Length; i++)
            {
                int index = i;
                var tab = Widgets.Button(Categories[i], () => ShowCategory(index));
                tab.name = $"category-{i}";
                tabs.Add(tab);
            }

            layout.Add(tabs);
            settingRows = new ScrollView { name = "settings-rows" };
            settingRows.AddToClassList("settings-rows");
            layout.Add(settingRows);
            host.DrawerBody.Add(layout);
            host.DrawerBody.Add(Footer());
            ShowCategory(GraphicsTab);
            tabs.ElementAt(0).schedule.Execute(tabs.ElementAt(0).Focus);
        }

        /// <summary>Marks the drawer closed; the home screen calls it when it takes over.</summary>
        public void Close()
        {
            IsOpen = false;
        }

        /// <summary>Drops the draft, goes back to the saved preferences and the home screen.</summary>
        public void Cancel()
        {
            if (switchingDisplay)
            {
                return;
            }

            CancelBinding();
            Preferences.Preview(Preferences.Saved);
            draft = null;
            host.ShowHome();
        }

        public void CancelBinding()
        {
            bindingListener?.Dispose();
            bindingListener = null;
        }

        private static string BindingLabel(Key key) => Keyboard.current?[key].displayName ?? key.ToString();

        private VisualElement Footer()
        {
            var footer = MenuWidgets.Box("settings-footer");
            var defaults = Widgets.Button("Alapértékek", () => host.Confirm("Visszaállítod az alapértékeket?",
                "A változtatásokat az Alkalmaz gomb menti.", "Visszaállítás", () =>
                {
                    draft = GamePreferences.CreateDefault();
                    Preferences.Preview(draft);
                    ShowCategory(category);
                }));
            defaults.name = "settings-defaults";
            footer.Add(defaults);
            var cancel = Widgets.Button("Mégse", Cancel);
            cancel.name = "settings-cancel";
            footer.Add(cancel);
            var apply = Widgets.Button("Alkalmaz", Apply);
            apply.name = "settings-apply";
            apply.AddToClassList("primary");
            footer.Add(apply);
            return footer;
        }

        private void ShowCategory(int index)
        {
            CancelBinding();
            category = index;
            qualityChoice = null;
            settingRows.Clear();
            settingRows.scrollOffset = Vector2.zero;
            for (int i = 0; i < tabs.childCount; i++)
            {
                tabs.ElementAt(i).EnableInClassList("selected-tab", i == index);
            }

            switch (index)
            {
                case 0:
                    Graphics();
                    break;
                case 1:
                    Audio();
                    break;
                case 2:
                    Controls();
                    break;
                case 3:
                    Gameplay();
                    break;
                case 4:
                    Accessibility();
                    break;
            }
        }

        private void Graphics()
        {
            Section("KIJELZŐ");
            var displays = display.Displays().Select((info, i) => $"{i + 1}. {info.name} · {info.width}×{info.height}").ToArray();
            if (displays.Length > 1)
            {
                Choice("monitor", "Monitor", displays, Mathf.Clamp(draft.Monitor, 0, displays.Length - 1), i =>
                {
                    draft.Monitor = i;
                    draft.Width = 0;
                    draft.Height = 0;
                    ShowCategory(GraphicsTab);
                    settingRows.Q<DropdownField>("monitor")?.Focus();
                });
            }

            Choice("display-mode", "Képernyőmód", new[] { "Teljes képernyő", "Keret nélküli", "Ablakos" },
                Array.IndexOf(ScreenModes, draft.DisplayMode), i =>
                {
                    draft.DisplayMode = ScreenModes[i];
                    ShowCategory(GraphicsTab);
                    settingRows.Q<DropdownField>("display-mode")?.Focus();
                });
            if (draft.DisplayMode != FullScreenMode.FullScreenWindow)
            {
                var modes = display.ResolutionChoices(draft).ToArray();
                int index = Array.IndexOf(modes, new Vector2Int(draft.Width, draft.Height));
                Choice("resolution", "Felbontás", modes.Select(x => x == Vector2Int.zero ? "Natív" : $"{x.x} × {x.y}").ToArray(),
                    Mathf.Max(0, index), i =>
                    {
                        draft.Width = modes[i].x;
                        draft.Height = modes[i].y;
                    });
            }

            int[] limits = PreferenceRules.FrameLimits;
            Toggle("vsync", "VSync · ha illik az FPS-korláthoz", draft.VsyncEnabled, x =>
            {
                draft.VsyncEnabled = x;
                Preview();
            });
            Choice("frame-limit", "Játék FPS-korlátja",
                limits.Select(x => x == PreferenceRules.Unlimited ? "Korlátlan" : $"{x} FPS").ToArray(),
                Array.IndexOf(limits, draft.FrameLimit), i =>
                {
                    draft.FrameLimit = limits[i];
                    Preview();
                });
            Rendering();
        }

        private void Rendering()
        {
            Section("HDRP  /  RENDERELÉS");
            qualityChoice = Choice("quality", "Minőségi profil", new[] { "Alacsony", "Közepes", "Magas", "Ultra", "Egyéni" },
                draft.Preset, i =>
                {
                    draft.SetPreset(i);
                    Preview();
                    if (i != QualityPreset.Custom)
                    {
                        ShowCategory(GraphicsTab);
                        qualityChoice.Focus();
                    }
                });
            Choice("textures", "Textúraminőség", new[] { "Teljes", "Felezett", "Negyedelt" }, draft.Texture, i =>
            {
                draft.Texture = i;
                Preview(true);
            });
            Choice("shadows", "Árnyéktáv", new[] { "Ki", "18 m", "36 m", "60 m" }, draft.Shadows, i =>
            {
                draft.Shadows = i;
                Preview(true);
            });
            Choice("shadow-resolution", "Árnyékfelbontás", new[] { "256", "512", "Jelenet szerinti", "2048" }, draft.ShadowResolution, i =>
            {
                draft.ShadowResolution = i;
                Preview(true);
            });
            Choice("antialias", "Élsimítás", new[] { "Ki", "FXAA", "SMAA", "TAA" }, draft.Antialias, i =>
            {
                draft.Antialias = i;
                Preview(true);
            });
            Toggle("ao", "Ambient occlusion", draft.AmbientOcclusionEnabled, x =>
            {
                draft.AmbientOcclusionEnabled = x;
                Preview(true);
            });
            Toggle("volumetrics", "Volumetrikus köd és fény", draft.VolumetricsEnabled, x =>
            {
                draft.VolumetricsEnabled = x;
                Preview(true);
            });
            Toggle("bloom", "Bloom", draft.BloomEnabled, x =>
            {
                draft.BloomEnabled = x;
                Preview(true);
            });
            Toggle("motion-blur", "Mozgáselmosás", draft.MotionBlurEnabled, x =>
            {
                draft.MotionBlurEnabled = x;
                Preview(true);
            });
            Toggle("depth-of-field", "Mélységélesség", draft.DepthOfFieldEnabled, x =>
            {
                draft.DepthOfFieldEnabled = x;
                Preview(true);
            });
            Range("fov", "Függőleges látószög", PreferenceRules.MinimumFieldOfView, PreferenceRules.MaximumFieldOfView, draft.FieldOfView, x =>
            {
                draft.FieldOfView = Mathf.Round(x);
                Preview();
            }, x => $"{Mathf.RoundToInt(x)}°");
            Range("brightness", "Fényerő", -PreferenceRules.BrightnessRange, PreferenceRules.BrightnessRange, draft.Brightness, x =>
            {
                draft.Brightness = x;
                Preview();
            }, x => $"{x:+0.0;-0.0;0.0} EV");
            Range("render-scale", "Belső renderfelbontás", QualityPreset.MinimumRenderScale, QualityPreset.MaximumRenderScale, draft.RenderScale, x =>
            {
                draft.RenderScale = Mathf.RoundToInt(x);
                Preview(true);
            }, x => $"{Mathf.RoundToInt(x)}%");
        }

        private void Audio()
        {
            Section("HANGKEVERŐ");
            Toggle("mute", "Minden hang némítása", draft.Muted, x =>
            {
                draft.Muted = x;
                Preview();
            });
            Volume("master", "Fő hangerő", draft.Master, x => draft.Master = x);
            Volume("music", "Zene", draft.Music, x => draft.Music = x);
            Volume("effects", "Hangeffektek", draft.Effects, x => draft.Effects = x);
            Volume("ambience", "Környezeti hangok", draft.Ambience, x => draft.Ambience = x);
            Volume("ui-volume", "Felületi hangok", draft.Ui, x => draft.Ui = x);
            settingRows.Add(MenuWidgets.Label("A zenei sáv a menüben, a környezeti és mechanikus sáv a kórházban hallható.", "setting-note"));
        }

        private void Controls()
        {
            Section("EGÉR");
            Range("sensitivity", "Érzékenység", PreferenceRules.MinimumSensitivity, PreferenceRules.MaximumSensitivity, draft.Sensitivity, x =>
            {
                draft.Sensitivity = x;
                Preview();
            }, x => $"{x:0.00}×");
            Toggle("invert-y", "Függőleges tengely megfordítása", draft.YInverted, x =>
            {
                draft.YInverted = x;
                Preview();
            });
            Range("smoothing", "Egérsimítás", 0f, PreferenceRules.MaximumSmoothing, draft.Smoothing, x =>
            {
                draft.Smoothing = x;
                Preview();
            });
            Section("BILLENTYŰZET");
            for (int i = 0; i < GamePreferences.ActionCount; i++)
            {
                var action = (GameAction)i;
                Button button = null;
                button = Widgets.Button(BindingLabel(draft.KeyFor(action)), () => BeginBinding(action, button));
                button.name = $"binding-{i}";
                Row(GamePreferences.ActionName(action), button);
            }

            settingRows.Add(Widgets.Button("Irányítás visszaállítása", () =>
            {
                draft.ResetBindings();
                draft.Sensitivity = 1f;
                draft.YInverted = false;
                draft.Smoothing = 0f;
                Preview();
                ShowCategory(ControlsTab);
                tabs.ElementAt(ControlsTab).Focus();
            }));
            settingRows.Add(MenuWidgets.Label("A nyilak, az Enter, az Esc és a Tab a terminál és a menü vezérléséhez foglaltak.", "setting-note"));
        }

        private void BeginBinding(GameAction action, Button button)
        {
            CancelBinding();
            button.text = "Nyomj billentyűt…";
            bindingStartFrame = Time.frameCount;
            bindingListener = InputSystem.onAnyButtonPress.Call(control =>
            {
                if (Time.frameCount == bindingStartFrame || control is not KeyControl key)
                {
                    return;
                }

                if (key.keyCode == Key.Escape)
                {
                    bindingCancelFrame = Time.frameCount;
                    FinishBinding(button, action);
                    return;
                }

                if (!GamePreferences.AllowedKey(key.keyCode))
                {
                    host.Notify("Ez a billentyű foglalt.");
                    return;
                }

                var previous = draft.ActionBoundTo(key.keyCode);
                if (previous.HasValue && previous.Value != action)
                {
                    host.Notify($"Ütközés · {GamePreferences.ActionName(previous.Value)}");
                    return;
                }

                draft.Bind(action, key.keyCode);
                Preview();
                FinishBinding(button, action);
            });
        }

        private void FinishBinding(Button button, GameAction action)
        {
            CancelBinding();
            button.text = BindingLabel(draft.KeyFor(action));
            button.Focus();
        }

        private void Gameplay()
        {
            Section("FELÜLET  /  MOZGÁS");
            Toggle("hud", "Felfedező HUD", draft.ShowHud, x =>
            {
                draft.ShowHud = x;
                Preview();
            });
            Toggle("prompts", "Interakciós jelzések", draft.ShowPrompts, x =>
            {
                draft.ShowPrompts = x;
                Preview();
            });
            Toggle("objectives", "Küldetés kijelzése", draft.ShowObjectives, x =>
            {
                draft.ShowObjectives = x;
                Preview();
            });
            Range("head-bob", "Fejmozgás járáskor", 0f, 1f, draft.HeadBob, x =>
            {
                draft.HeadBob = x;
                Preview();
            });
            Choice("sprint", "Sprint mód", new[] { "Nyomva tartás", "Kapcsoló" }, draft.SprintToggles ? 1 : 0, x =>
            {
                draft.SprintToggles = x == 1;
                Preview();
            });
        }

        private void Accessibility()
        {
            Section("OLVASHATÓSÁG  /  INGERTERHELÉS");
            Range("ui-scale", "Felület mérete", PreferenceRules.MinimumUiScale, PreferenceRules.MaximumUiScale, draft.UiScale, x =>
            {
                draft.UiScale = x;
                Preview();
            });
            Toggle("reduced-motion", "Csökkentett mozgás", draft.ReduceMotion, x =>
            {
                draft.ReduceMotion = x;
                Preview();
            });
            Toggle("reduced-flashing", "Csökkentett villogás", draft.ReduceFlashing, x =>
            {
                draft.ReduceFlashing = x;
                Preview();
            });
            Toggle("high-contrast", "Nagy kontraszt", draft.UseHighContrast, x =>
            {
                draft.UseHighContrast = x;
                Preview();
            });
            Range("hud-opacity", "HUD átlátszatlansága", PreferenceRules.MinimumHudOpacity, 1f, draft.HudOpacity, x =>
            {
                draft.HudOpacity = x;
                Preview();
            });
        }

        // A single graphics setting changed by hand turns the preset into Custom.
        private void Preview(bool custom = false)
        {
            if (custom)
            {
                draft.Preset = QualityPreset.Custom;
                if (qualityChoice != null)
                {
                    qualityChoice.index = QualityPreset.Custom;
                }
            }

            Preferences.Preview(draft);
        }

        private void Apply()
        {
            if (switchingDisplay)
            {
                return;
            }

            CancelBinding();
            draft.Validate();
            if (!Application.isEditor && draft.DisplayDiffers(Preferences.Saved))
            {
                host.StartCoroutine(TrialDisplay());
            }
            else
            {
                Commit();
            }
        }

        private IEnumerator TrialDisplay()
        {
            switchingDisplay = true;
            host.Drawer.SetEnabled(false);
            displayBeforeTrial = new GamePreferences
            {
                Width = Screen.width,
                Height = Screen.height,
                DisplayMode = Screen.fullScreenMode,
                Monitor = display.CurrentMonitor(),
            };
            yield return display.Apply(draft);
            var countdown = host.ShowDialog("Megtartod a kijelzőbeállítást?", "", "Megtartás", KeepDisplay, RevertDisplay);
            displayCountdown = host.StartCoroutine(CountDown(countdown));
        }

        private IEnumerator CountDown(Label label)
        {
            double expires = Time.realtimeSinceStartupAsDouble + DisplayTrialSeconds;
            int last = 0;
            while (Time.realtimeSinceStartupAsDouble < expires)
            {
                int left = (int)Math.Ceiling(expires - Time.realtimeSinceStartupAsDouble);
                if (left != last)
                {
                    last = left;
                    label.text = $"Automatikus visszaállítás {left} másodperc múlva.";
                }

                yield return null;
            }

            displayCountdown = null;
            RevertDisplay();
        }

        private void KeepDisplay()
        {
            StopCountdown();
            if (!Commit())
            {
                RevertDisplay();
                return;
            }

            switchingDisplay = false;
            host.HideDialog();
            host.Drawer.Q<Button>("settings-apply")?.Focus();
        }

        private void StopCountdown()
        {
            if (displayCountdown != null)
            {
                host.StopCoroutine(displayCountdown);
            }

            displayCountdown = null;
        }

        private void RevertDisplay()
        {
            StopCountdown();
            host.StartCoroutine(RollbackDisplay());
        }

        private IEnumerator RollbackDisplay()
        {
            host.LockDialog();
            host.Dialog.SetEnabled(false);
            host.Dialog.Q<Label>(className: "dialog-copy").text = "A korábbi kijelzőmód visszaállítása…";
            yield return display.Apply(displayBeforeTrial, rollback: true);
            draft.CopyDisplay(Preferences.Saved);
            switchingDisplay = false;
            host.Dialog.SetEnabled(true);
            host.HideDialog();
            if (IsOpen)
            {
                ShowCategory(GraphicsTab);
            }

            host.Drawer.Q<Button>("settings-apply")?.Focus();
            host.Notify(Screen.fullScreenMode == displayBeforeTrial.DisplayMode
                ? "Az előző kijelzőbeállítás visszaállt."
                : "A Windows az elérhető kijelzőmódot használja.");
        }

        private bool Commit()
        {
            if (!Preferences.Commit(draft, out string error))
            {
                host.Notify(error);
                return false;
            }

            draft = Preferences.Saved.Copy();
            host.Notify("Beállítások mentve.");
            return true;
        }

        private void Section(string text) => settingRows.Add(MenuWidgets.Label(text, "setting-section"));

        private void Row(string text, VisualElement control)
        {
            var row = MenuWidgets.Box("setting-row");
            row.Add(MenuWidgets.Label(text, "setting-label"));
            row.Add(control);
            settingRows.Add(row);
        }

        private DropdownField Choice(string name, string text, string[] options, int index, Action<int> update)
        {
            var field = new DropdownField(new List<string>(options), Mathf.Clamp(index, 0, options.Length - 1))
            {
                name = name,
                tooltip = text,
            };
            field.AddToClassList("setting-choice");
            field.RegisterValueChangedCallback(_ => update(field.index));
            Row(text, field);
            return field;
        }

        private void Toggle(string name, string text, bool value, Action<bool> update)
        {
            var field = new Toggle { name = name, value = value, tooltip = text };
            field.AddToClassList("setting-toggle");
            field.RegisterValueChangedCallback(change => update(change.newValue));
            Row(text, field);
        }

        private void Volume(string name, string text, float value, Action<float> update)
        {
            Range(name, text, 0f, 1f, value, x =>
            {
                update(x);
                Preview();
            });
        }

        private void Range(string name, string text, float minimum, float maximum, float value, Action<float> update,
            Func<float, string> format = null)
        {
            format ??= x => $"{Mathf.RoundToInt(x * 100)}%";
            var group = MenuWidgets.Box("setting-range");
            var slider = new Slider(minimum, maximum) { name = name, value = value, tooltip = text };
            var readout = MenuWidgets.Label(format(value), "setting-value");
            slider.RegisterValueChangedCallback(change =>
            {
                readout.text = format(change.newValue);
                update(change.newValue);
            });
            group.Add(slider);
            group.Add(readout);
            Row(text, group);
        }
    }
}
