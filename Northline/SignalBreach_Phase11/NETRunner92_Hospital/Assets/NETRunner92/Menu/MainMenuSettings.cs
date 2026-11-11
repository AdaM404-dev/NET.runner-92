using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace NETRunner92
{
    public sealed partial class MainMenuView
    {
        static readonly string[] Categories = { "Grafika", "Hang", "Irányítás", "Játékmenet", "Akadálymentesítés" };
        static readonly FullScreenMode[] ScreenModes =
        {
            FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed
        };
        static readonly int[] FrameLimits = { -1, 30, 60, 90, 120, 144, 165, 240 };

        ScrollView settingRows;
        VisualElement tabs;
        int category, bindingStartFrame, bindingCancelFrame = -1;
        bool switchingDisplay;
        GamePreferences displayBeforeTrial;
        Coroutine displayCountdown;
        DropdownField qualityChoice;

        void ShowSettings()
        {
            draft = GameOptions.Saved.Copy();
            settingsOpen = true;
            OpenDrawer("RENDSZER   /   FELHASZNÁLÓI PROFIL", "Beállítások");
            var layout = Box("settings-layout");
            tabs = Box("settings-tabs");
            for (int i = 0; i < Categories.Length; i++)
            {
                int index = i;
                var tab = MenuControls.Button(Categories[i], () => ShowCategory(index));
                tab.name = $"category-{i}";
                tabs.Add(tab);
            }
            layout.Add(tabs);
            settingRows = new ScrollView { name = "settings-rows" };
            settingRows.AddToClassList("settings-rows");
            layout.Add(settingRows);
            drawerBody.Add(layout);

            var footer = Box("settings-footer");
            var defaults = MenuControls.Button("Alapértékek", () =>
            {
                Confirm("Visszaállítod az alapértékeket?", "A változtatásokat az Alkalmaz gomb menti.", "Visszaállítás", () =>
                {
                    draft = new GamePreferences();
                    GameOptions.Preview(draft);
                    ShowCategory(category);
                });
            });
            defaults.name = "settings-defaults";
            footer.Add(defaults);
            var cancel = MenuControls.Button("Mégse", CancelSettings);
            cancel.name = "settings-cancel";
            footer.Add(cancel);
            var apply = MenuControls.Button("Alkalmaz", ApplySettings);
            apply.name = "settings-apply";
            apply.AddToClassList("primary");
            footer.Add(apply);
            drawerBody.Add(footer);
            ShowCategory(0);
            tabs.ElementAt(0).schedule.Execute(tabs.ElementAt(0).Focus);
        }

        void ShowCategory(int index)
        {
            CancelBinding();
            category = index;
            qualityChoice = null;
            settingRows.Clear();
            settingRows.scrollOffset = Vector2.zero;
            for (int i = 0; i < tabs.childCount; i++)
                tabs.ElementAt(i).EnableInClassList("selected-tab", i == index);

            switch (index)
            {
                case 0: Graphics(); break;
                case 1: Audio(); break;
                case 2: Controls(); break;
                case 3: Gameplay(); break;
                case 4: Accessibility(); break;
            }
        }

        void Graphics()
        {
            Section("KIJELZŐ");
            var displays = GameOptions.GetDisplays().Select((display, i) =>
                $"{i + 1}. {display.name} · {display.width}×{display.height}").ToArray();
            if (displays.Length > 1)
                Choice("monitor", "Monitor", displays, Mathf.Clamp(draft.monitor, 0, displays.Length - 1), i =>
                {
                    draft.monitor = i;
                    draft.width = draft.height = 0;
                    ShowCategory(0);
                    settingRows.Q<DropdownField>("monitor")?.Focus();
                });

            Choice("display-mode", "Képernyőmód", new[] { "Teljes képernyő", "Keret nélküli", "Ablakos" },
                Array.IndexOf(ScreenModes, draft.displayMode), i =>
                {
                    draft.displayMode = ScreenModes[i];
                    ShowCategory(0);
                    settingRows.Q<DropdownField>("display-mode")?.Focus();
                });
            if (draft.displayMode != FullScreenMode.FullScreenWindow)
            {
                var modes = GameOptions.ResolutionChoices(draft).ToArray();
                int index = Array.IndexOf(modes, new Vector2Int(draft.width, draft.height));
                Choice("resolution", "Felbontás", modes.Select(x => x == Vector2Int.zero ? "Natív" : $"{x.x} × {x.y}").ToArray(),
                    Mathf.Max(0, index), i =>
                    {
                        draft.width = modes[i].x;
                        draft.height = modes[i].y;
                    });
            }
            DropdownField frameLimit = null;
            Toggle("vsync", "VSync", draft.vsync, x =>
            {
                draft.vsync = x;
                frameLimit?.SetEnabled(!x);
                Preview();
            });
            frameLimit = Choice("frame-limit", "Képkockakorlát · VSync nélkül",
                FrameLimits.Select(x => x < 0 ? "Korlátlan" : $"{x} FPS").ToArray(),
                Array.IndexOf(FrameLimits, draft.frameLimit), i => { draft.frameLimit = FrameLimits[i]; Preview(); });
            frameLimit.SetEnabled(!draft.vsync);

            Section("HDRP  /  RENDERELÉS");
            qualityChoice = Choice("quality", "Minőségi profil", new[] { "Alacsony", "Közepes", "Magas", "Ultra", "Egyéni" },
                draft.preset, i =>
                {
                    draft.SetPreset(i);
                    Preview();
                    if (i != 4)
                    {
                        ShowCategory(0);
                        qualityChoice.Focus();
                    }
                });
            Choice("textures", "Textúraminőség", new[] { "Teljes", "Felezett", "Negyedelt" }, draft.texture,
                i => { draft.texture = i; Preview(true); });
            Choice("shadows", "Árnyéktáv", new[] { "Ki", "18 m", "36 m", "60 m" }, draft.shadows,
                i => { draft.shadows = i; Preview(true); });
            Choice("shadow-resolution", "Árnyékfelbontás", new[] { "256", "512", "Jelenet szerinti", "2048" }, draft.shadowResolution,
                i => { draft.shadowResolution = i; Preview(true); });
            Choice("antialias", "Élsimítás", new[] { "Ki", "FXAA", "SMAA", "TAA" }, draft.antialias,
                i => { draft.antialias = i; Preview(true); });
            Toggle("ao", "Ambient occlusion", draft.ao, x => { draft.ao = x; Preview(true); });
            Toggle("volumetrics", "Volumetrikus köd és fény", draft.volumetrics, x => { draft.volumetrics = x; Preview(true); });
            Toggle("bloom", "Bloom", draft.bloom, x => { draft.bloom = x; Preview(true); });
            Toggle("motion-blur", "Mozgáselmosás", draft.motionBlur, x => { draft.motionBlur = x; Preview(true); });
            Toggle("depth-of-field", "Mélységélesség", draft.depthOfField, x => { draft.depthOfField = x; Preview(true); });
            Range("fov", "Függőleges látószög", 55, 100, draft.fov, x => { draft.fov = Mathf.Round(x); Preview(); },
                x => $"{Mathf.RoundToInt(x)}°");
            Range("brightness", "Fényerő", -1.5f, 1.5f, draft.brightness,
                x => { draft.brightness = x; Preview(); }, x => $"{x:+0.0;-0.0;0.0} EV");
            Range("render-scale", "Belső renderfelbontás", 50, 100, draft.renderScale,
                x => { draft.renderScale = Mathf.RoundToInt(x); Preview(true); }, x => $"{Mathf.RoundToInt(x)}%");
        }

        void Audio()
        {
            Section("HANGKEVERŐ");
            Toggle("mute", "Minden hang némítása", draft.mute, x => { draft.mute = x; Preview(); });
            Range("master", "Fő hangerő", 0, 1, draft.master, x => { draft.master = x; Preview(); });
            Range("music", "Zene", 0, 1, draft.music, x => { draft.music = x; Preview(); });
            Range("effects", "Hangeffektek", 0, 1, draft.effects, x => { draft.effects = x; Preview(); });
            Range("ambience", "Környezeti hangok", 0, 1, draft.ambience, x => { draft.ambience = x; Preview(); });
            Range("ui-volume", "Felületi hangok", 0, 1, draft.ui, x => { draft.ui = x; Preview(); });
            settingRows.Add(MenuControls.Label("A zenei sáv a menüben, a környezeti és mechanikus sáv a kórházban hallható.", "setting-note"));
        }

        void Controls()
        {
            Section("EGÉR");
            Range("sensitivity", "Érzékenység", .25f, 3, draft.sensitivity, x => { draft.sensitivity = x; Preview(); },
                x => $"{x:0.00}×");
            Toggle("invert-y", "Függőleges tengely megfordítása", draft.invertY, x => { draft.invertY = x; Preview(); });
            Range("smoothing", "Egérsimítás", 0, .85f, draft.smoothing, x => { draft.smoothing = x; Preview(); });
            Section("BILLENTYŰZET");
            for (int i = 0; i < draft.keys.Length; i++)
            {
                int index = i;
                Button button = null;
                button = MenuControls.Button(BindingLabel(draft.keys[i]), () => BeginBinding(index, button));
                button.name = $"binding-{i}";
                Row(GamePreferences.ActionNames[i], button);
            }
            settingRows.Add(MenuControls.Button("Irányítás visszaállítása", () =>
            {
                draft.ResetBindings();
                draft.sensitivity = 1;
                draft.invertY = false;
                draft.smoothing = 0;
                Preview();
                ShowCategory(2);
                tabs.ElementAt(2).Focus();
            }));
            settingRows.Add(MenuControls.Label("A nyilak, az Enter, az Esc és a Tab a terminál és a menü vezérléséhez foglaltak.", "setting-note"));
        }

        void BeginBinding(int index, Button button)
        {
            CancelBinding();
            button.text = "Nyomj billentyűt…";
            bindingStartFrame = Time.frameCount;
            bindingListener = InputSystem.onAnyButtonPress.Call(control =>
            {
                if (Time.frameCount == bindingStartFrame || control is not KeyControl key) return;
                if (key.keyCode == Key.Escape)
                {
                    bindingCancelFrame = Time.frameCount;
                    FinishBinding(button, index);
                    return;
                }
                if (!GamePreferences.AllowedKey(key.keyCode))
                {
                    Notify("Ez a billentyű foglalt.");
                    return;
                }
                int previous = Array.IndexOf(draft.keys, key.keyCode);
                if (previous >= 0 && previous != index)
                {
                    Notify($"Ütközés · {GamePreferences.ActionNames[previous]}");
                    return;
                }
                draft.keys[index] = key.keyCode;
                Preview();
                FinishBinding(button, index);
            });
        }

        void FinishBinding(Button button, int index)
        {
            CancelBinding();
            button.text = BindingLabel(draft.keys[index]);
            button.Focus();
        }

        void CancelBinding()
        {
            bindingListener?.Dispose();
            bindingListener = null;
        }

        static string BindingLabel(Key key) => Keyboard.current?[key].displayName ?? key.ToString();

        void Gameplay()
        {
            Section("FELÜLET  /  MOZGÁS");
            Toggle("hud", "Felfedező HUD", draft.hud, x => { draft.hud = x; Preview(); });
            Toggle("prompts", "Interakciós jelzések", draft.prompts, x => { draft.prompts = x; Preview(); });
            Toggle("objectives", "Küldetés kijelzése", draft.objectives, x => { draft.objectives = x; Preview(); });
            Range("head-bob", "Fejmozgás járáskor", 0, 1, draft.headBob, x => { draft.headBob = x; Preview(); });
            Choice("sprint", "Sprint mód", new[] { "Nyomva tartás", "Kapcsoló" }, draft.sprintToggle ? 1 : 0,
                x => { draft.sprintToggle = x == 1; Preview(); });
        }

        void Accessibility()
        {
            Section("OLVASHATÓSÁG  /  INGERTERHELÉS");
            Range("ui-scale", "Felület mérete", .8f, 1.25f, draft.uiScale, x => { draft.uiScale = x; Preview(); });
            Toggle("reduced-motion", "Csökkentett mozgás", draft.reducedMotion, x => { draft.reducedMotion = x; Preview(); });
            Toggle("reduced-flashing", "Csökkentett villogás", draft.reducedFlashing, x => { draft.reducedFlashing = x; Preview(); });
            Toggle("high-contrast", "Nagy kontraszt", draft.highContrast, x => { draft.highContrast = x; Preview(); });
            Range("hud-opacity", "HUD átlátszatlansága", .25f, 1, draft.hudOpacity, x => { draft.hudOpacity = x; Preview(); });
        }

        void Preview(bool custom = false)
        {
            if (custom)
            {
                draft.preset = 4;
                if (qualityChoice != null) qualityChoice.index = 4;
            }
            GameOptions.Preview(draft);
        }

        void ApplySettings()
        {
            if (switchingDisplay) return;
            CancelBinding();
            draft.Validate();
            if (!Application.isEditor && draft.DisplayDiffers(GameOptions.Saved))
                StartCoroutine(TrialDisplay());
            else CommitSettings();
        }

        IEnumerator TrialDisplay()
        {
            switchingDisplay = true;
            drawer.SetEnabled(false);
            var displays = GameOptions.GetDisplays();
            int monitor = 0;
            for (int i = 0; i < displays.Count; i++)
                if (displays[i].Equals(Screen.mainWindowDisplayInfo)) monitor = i;

            displayBeforeTrial = new GamePreferences
            {
                width = Screen.width, height = Screen.height, displayMode = Screen.fullScreenMode, monitor = monitor
            };
            yield return GameOptions.ApplyDisplay(draft);
            var countdown = ShowDialog("Megtartod a kijelzőbeállítást?", "", "Megtartás", KeepDisplay, RevertDisplay);
            displayCountdown = StartCoroutine(CountDown(countdown));
        }

        IEnumerator CountDown(Label label)
        {
            double expires = Time.realtimeSinceStartupAsDouble + 15;
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

        void KeepDisplay()
        {
            StopCountdown();
            if (!CommitSettings())
            {
                RevertDisplay();
                return;
            }
            switchingDisplay = false;
            HideDialog();
            drawer.Q<Button>("settings-apply")?.Focus();
        }

        void StopCountdown()
        {
            if (displayCountdown != null) StopCoroutine(displayCountdown);
            displayCountdown = null;
        }

        void RevertDisplay()
        {
            StopCountdown();
            StartCoroutine(RollbackDisplay());
        }

        IEnumerator RollbackDisplay()
        {
            closeDialog = null;
            dialog.SetEnabled(false);
            dialog.Q<Label>(className: "dialog-copy").text = "A korábbi kijelzőmód visszaállítása…";
            yield return GameOptions.ApplyDisplay(displayBeforeTrial, rollback: true);
            var saved = GameOptions.Saved;
            draft.width = saved.width;
            draft.height = saved.height;
            draft.displayMode = saved.displayMode;
            draft.monitor = saved.monitor;
            switchingDisplay = false;
            dialog.SetEnabled(true);
            HideDialog();
            if (settingsOpen) ShowCategory(0);
            drawer.Q<Button>("settings-apply")?.Focus();
            Notify(Screen.fullScreenMode == displayBeforeTrial.displayMode
                ? "Az előző kijelzőbeállítás visszaállt."
                : "A Windows az elérhető kijelzőmódot használja.");
        }

        bool CommitSettings()
        {
            if (!GameOptions.Commit(draft, out string error))
            {
                Notify(error);
                return false;
            }
            draft = GameOptions.Saved.Copy();
            Notify("Beállítások mentve.");
            return true;
        }

        void CancelSettings()
        {
            if (switchingDisplay) return;
            CancelBinding();
            GameOptions.Preview(GameOptions.Saved);
            draft = null;
            ShowHome();
        }

        void Section(string text) => settingRows.Add(MenuControls.Label(text, "setting-section"));

        void Row(string text, VisualElement control)
        {
            var row = Box("setting-row");
            row.Add(MenuControls.Label(text, "setting-label"));
            row.Add(control);
            settingRows.Add(row);
        }

        DropdownField Choice(string name, string text, string[] options, int index, Action<int> update)
        {
            var field = new DropdownField(new List<string>(options), Mathf.Clamp(index, 0, options.Length - 1))
                { name = name, tooltip = text };
            field.AddToClassList("setting-choice");
            field.RegisterValueChangedCallback(_ => update(field.index));
            Row(text, field);
            return field;
        }

        void Toggle(string name, string text, bool value, Action<bool> update)
        {
            var field = new Toggle { name = name, value = value, tooltip = text };
            field.AddToClassList("setting-toggle");
            field.RegisterValueChangedCallback(evt => update(evt.newValue));
            Row(text, field);
        }

        void Range(string name, string text, float minimum, float maximum, float value, Action<float> update,
            Func<float, string> format = null)
        {
            format ??= x => $"{Mathf.RoundToInt(x * 100)}%";
            var group = Box("setting-range");
            var slider = new Slider(minimum, maximum) { name = name, value = value, tooltip = text };
            var readout = MenuControls.Label(format(value), "setting-value");
            slider.RegisterValueChangedCallback(evt =>
            {
                readout.text = format(evt.newValue);
                update(evt.newValue);
            });
            group.Add(slider);
            group.Add(readout);
            Row(text, group);
        }
    }
}
