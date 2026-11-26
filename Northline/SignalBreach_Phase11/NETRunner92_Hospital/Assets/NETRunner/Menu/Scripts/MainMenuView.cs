using System;
using System.Collections;
using NetRunner.Core;
using NetRunner.Saves;
using NetRunner.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace NetRunner.Menu
{
    /// <summary>
    /// The main menu (Phase 11): Continue, New Game and Load Game with three
    /// save slots, Settings (<see cref="SettingsScreen"/>) and Exit, over a
    /// slowly drifting hospital backdrop with dust and music. Dialogs ask
    /// before overwriting, deleting or leaving. Starting a run hands the slot
    /// to the <see cref="RunSession"/> and loads the hospital. The menu
    /// scene's builder places it and sets it up through <see cref="Configure"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuView : MonoBehaviour
    {
        private const float FadeInDelayMs = 20;
        private const long BackdropFrameMs = 50;
        private const long StatusGlitchEveryMs = 11000;
        private const long StatusGlitchMs = 100;
        private const long ToastMs = 3200;

        // Seconds the screen takes to fade before the hospital loads (almost none with reduced motion).
        private const float LoadFadeSeconds = .46f;
        private const float ReducedLoadFadeSeconds = .05f;

        // The music and click play quieter than the settings' 0-to-1 volume.
        private const float MusicGain = .25f;
        private const float ClickGain = .30f;

        // The backdrop drifts up to 13 by 7 pixels, slowly; dust specks crawl across it.
        private const float DriftRate = .075f;
        private const float DriftX = 13f;
        private const float DriftY = 7f;
        private const int DustSpecks = 24;

        private const string StatusText = "RECOVERY CONSOLE   /   092";
        private const string StatusGlitch = "RECOVERY CONSO_E   /   092";

        [SerializeField] private StyleSheet menuStyle;
        [SerializeField] private Texture2D hospitalPlate;
        [SerializeField] private AudioSource music;
        [SerializeField] private AudioSource feedback;
        [SerializeField] private Preferences preferences;
        [SerializeField] private RunSession session;

        private CheckpointStore store;
        private MenuWidgets widgets;
        private SettingsScreen settings;
        private UIDocument document;
        private PanelSettings panel;
        private VisualElement root;
        private VisualElement home;
        private VisualElement drawer;
        private VisualElement drawerBody;
        private VisualElement scrim;
        private VisualElement dialog;
        private VisualElement fade;
        private VisualElement plate;
        private VisualElement dust;
        private Label drawerTitle;
        private Label drawerMark;
        private Label toast;
        private Label systemStatus;
        private Button dialogBack;
        private Action closeDialog;
        private bool loading;
        private float atmosphereTime;

        /// <summary>The settings the menu shows and changes.</summary>
        public Preferences Preferences => preferences;

        /// <summary>The menu's widgets, with the click sound.</summary>
        public MenuWidgets Widgets => widgets;

        /// <summary>The settings drawer's content area.</summary>
        public VisualElement DrawerBody => drawerBody;

        /// <summary>The drawer that holds the slots or the settings.</summary>
        public VisualElement Drawer => drawer;

        /// <summary>The confirmation dialog's card.</summary>
        public VisualElement Dialog => dialog;

        private void Awake()
        {
            document = GetComponent<UIDocument>();
            panel = Instantiate(document.panelSettings);
            document.panelSettings = panel;
            store = session.Store;
            widgets = new MenuWidgets(Click);
            settings = new SettingsScreen(this);
        }

        private void OnEnable()
        {
            preferences.Changed += ApplyAppearance;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Build();
            ShowHome();
            ApplyAppearance();
            root.schedule.Execute(() => fade.AddToClassList("transparent")).StartingIn((long)FadeInDelayMs);
            root.schedule.Execute(AnimateBackdrop).Every(BackdropFrameMs);
            root.schedule.Execute(GlitchStatus).StartingIn(StatusGlitchEveryMs).Every(StatusGlitchEveryMs);
        }

        private void Update()
        {
            if (loading || settings.CapturesEscape(Time.frameCount) || Keyboard.current?.escapeKey.wasPressedThisFrame != true)
            {
                return;
            }

            if (closeDialog != null)
            {
                closeDialog();
            }
            else if (drawer.style.display == DisplayStyle.Flex)
            {
                ReturnFromDrawer();
            }
        }

        private void OnDisable()
        {
            preferences.Changed -= ApplyAppearance;
            settings.CancelBinding();
        }

        private void OnDestroy()
        {
            if (panel)
            {
                Destroy(panel);
            }
        }

        /// <summary>Sets the menu up from the menu scene's builder.</summary>
        public void Configure(StyleSheet styles, Texture2D backdrop, AudioSource score, AudioSource click,
            Preferences playerSettings, RunSession runs)
        {
            menuStyle = styles;
            hospitalPlate = backdrop;
            music = score;
            feedback = click;
            preferences = playerSettings;
            session = runs;
        }

        /// <summary>Gives the menu the player's settings and the run session; called by the Phase 11 installer.</summary>
        public void SetShared(Preferences playerSettings, RunSession runs)
        {
            preferences = playerSettings;
            session = runs;
        }

        /// <summary>Shows the home screen with its main actions.</summary>
        public void ShowHome()
        {
            settings.Close();
            drawer.style.display = DisplayStyle.None;
            home.style.display = DisplayStyle.Flex;
            home.Clear();
            home.Add(MenuWidgets.Label("01   /   HELYI HOZZÁFÉRÉS", "eyebrow"));
            var wordmark = MenuWidgets.Box("wordmark");
            wordmark.Add(MenuWidgets.Label("NET.runner", "logo"));
            wordmark.Add(MenuWidgets.Label("–92", "logo-index"));
            home.Add(wordmark);
            home.Add(MenuWidgets.Label("NORTHLINE  //  KARANTÉNZÓNA", "subtitle"));
            var menu = MenuWidgets.Box("main-actions");
            int recent = store.MostRecent();
            if (recent >= 0)
            {
                menu.Add(HomeButton("Folytatás", "01", "A legutóbbi érvényes mentés", () => LoadSlot(recent), "continue"));
            }

            menu.Add(HomeButton("Új játék", "02", "Belépés a karanténzónába", () => ShowSlots(true), "new-game"));
            menu.Add(HomeButton("Játék betöltése", "03", "Mentéshelyek megnyitása", () => ShowSlots(false), "load-game"));
            menu.Add(HomeButton("Beállítások", "04", "Kép, hang, irányítás", settings.Open, "settings"));
            menu.Add(HomeButton("Kilépés", "05", "A munkamenet lezárása",
                () => Confirm("Kilépés a játékból?", "A mentéseid a gépen maradnak.", "Kilépés", Application.Quit), "exit"));
            home.Add(menu);
            var first = (Button)menu.ElementAt(0);
            home.schedule.Execute(first.Focus);
        }

        /// <summary>Opens the drawer with a heading and an empty body.</summary>
        public void OpenDrawer(string mark, string title)
        {
            home.style.display = DisplayStyle.None;
            drawerMark.text = mark;
            drawerTitle.text = title;
            drawer.AddToClassList("drawer-entering");
            drawer.style.display = DisplayStyle.Flex;
            drawer.schedule.Execute(() => drawer.RemoveFromClassList("drawer-entering")).StartingIn((long)FadeInDelayMs);
            drawerBody.Clear();
        }

        /// <summary>Asks before <paramref name="yes"/>; cancelling returns the focus to where it was.</summary>
        public void Confirm(string title, string message, string action, Action yes)
        {
            var previousFocus = root.focusController?.focusedElement as VisualElement;
            ShowDialog(title, message, action, () =>
            {
                HideDialog();
                yes();
            }, () =>
            {
                HideDialog();
                previousFocus?.Focus();
            });
        }

        /// <summary>Shows the dialog; returns its message label so a caller can update it (a countdown).</summary>
        public Label ShowDialog(string title, string message, string action, Action yes, Action no)
        {
            closeDialog = no;
            scrim.style.display = DisplayStyle.Flex;
            home.SetEnabled(false);
            drawer.SetEnabled(false);
            dialog.Clear();
            dialog.Add(MenuWidgets.Label("NORTHLINE   /   MEGERŐSÍTÉS", "eyebrow"));
            dialog.Add(MenuWidgets.Label(title, "dialog-title"));
            var copy = MenuWidgets.Label(message, "dialog-copy");
            dialog.Add(copy);
            var buttons = MenuWidgets.Box("dialog-actions");
            dialogBack = widgets.Button("Mégse", no);
            dialogBack.name = "dialog-cancel";
            buttons.Add(dialogBack);
            var confirm = widgets.Button(action, yes);
            confirm.name = "dialog-confirm";
            confirm.AddToClassList("primary");
            buttons.Add(confirm);
            dialog.Add(buttons);
            dialogBack.schedule.Execute(dialogBack.Focus);
            return copy;
        }

        public void HideDialog()
        {
            closeDialog = null;
            scrim.style.display = DisplayStyle.None;
            home.SetEnabled(true);
            drawer.SetEnabled(true);
        }

        /// <summary>Makes Escape do nothing while a dialog finishes on its own (a display rollback).</summary>
        public void LockDialog() => closeDialog = null;

        /// <summary>Shows a short message at the bottom of the screen.</summary>
        public void Notify(string message)
        {
            toast.text = message;
            toast.EnableInClassList("shown", true);
            toast.schedule.Execute(() =>
            {
                if (toast.text == message)
                {
                    toast.EnableInClassList("shown", false);
                }
            }).StartingIn(ToastMs);
        }

        private Button HomeButton(string title, string id, string description, Action action, string name)
        {
            var button = widgets.Button("", action);
            button.name = name;
            button.tooltip = description;
            button.AddToClassList("home-button");
            button.Add(MenuWidgets.Label(id, "button-index"));
            button.Add(MenuWidgets.Label(title, "button-title"));
            button.Add(MenuWidgets.Label("›", "button-indicator"));
            return button;
        }

        private static void PaintShade(MeshGenerationContext context)
        {
            // A dark band on the left fading out to the right, so the text stays readable over the backdrop.
            var size = context.visualElement.contentRect.size;
            var ink = new Color32(4, 9, 14, 255);
            var mesh = context.Allocate(8, 18);
            float[] boundaries = { 0f, .27f, .64f, 1f };
            byte[] alpha = { 252, 222, 54, 32 };
            for (int i = 0; i < boundaries.Length; i++)
            {
                ink.a = alpha[i];
                float x = boundaries[i] * size.x;
                mesh.SetNextVertex(new Vertex { position = new Vector3(x, 0, Vertex.nearZ), tint = ink });
                mesh.SetNextVertex(new Vertex { position = new Vector3(x, size.y, Vertex.nearZ), tint = ink });
            }

            for (ushort i = 0; i < 6; i += 2)
            {
                mesh.SetNextIndex(i);
                mesh.SetNextIndex((ushort)(i + 2));
                mesh.SetNextIndex((ushort)(i + 3));
                mesh.SetNextIndex(i);
                mesh.SetNextIndex((ushort)(i + 3));
                mesh.SetNextIndex((ushort)(i + 1));
            }
        }

        private void Build()
        {
            root = document.rootVisualElement;
            root.Clear();
            root.styleSheets.Add(menuStyle);
            root.AddToClassList("net-menu");

            plate = MenuWidgets.Box("plate");
            plate.style.backgroundImage = new StyleBackground(hospitalPlate);
            root.Add(plate);
            var gradient = MenuWidgets.Box("backdrop-gradient");
            gradient.generateVisualContent += PaintShade;
            root.Add(gradient);
            dust = MenuWidgets.Box("dust");
            dust.generateVisualContent += PaintDust;
            root.Add(dust);

            var top = MenuWidgets.Box("topline");
            top.Add(MenuWidgets.Label("+   N O R T H L I N E   M E D I C A L", "eyebrow"));
            systemStatus = MenuWidgets.Label(StatusText, "metadata");
            top.Add(systemStatus);
            root.Add(top);

            home = MenuWidgets.Box("home");
            home.RegisterCallback<NavigationMoveEvent>(HomeNavigation);
            root.Add(home);

            var bottom = MenuWidgets.Box("bottomline");
            bottom.Add(MenuWidgets.Label("NORTHLINE   /   RESTRICTED RESEARCH", "metadata"));
            bottom.Add(MenuWidgets.Label("04   /   CHECKPOINT A", "eyebrow"));
            root.Add(bottom);
            toast = MenuWidgets.Label("", "toast");
            root.Add(toast);

            BuildDrawer();
            scrim = MenuWidgets.Box("dialog-scrim");
            dialog = MenuWidgets.Box("dialog-card");
            scrim.Add(dialog);
            root.Add(scrim);
            HideDialog();

            fade = MenuWidgets.Box("fade");
            root.Add(fade);
        }

        private void BuildDrawer()
        {
            drawer = MenuWidgets.Box("drawer");
            var heading = MenuWidgets.Box("drawer-heading");
            var headingCopy = new VisualElement();
            drawerMark = MenuWidgets.Label("", "eyebrow");
            drawerTitle = MenuWidgets.Label("", "drawer-title");
            headingCopy.Add(drawerMark);
            headingCopy.Add(drawerTitle);
            heading.Add(headingCopy);
            var back = widgets.Button("Vissza", ReturnFromDrawer);
            back.name = "drawer-back";
            heading.Add(back);
            drawer.Add(heading);
            drawerBody = MenuWidgets.Box("drawer-body");
            drawer.Add(drawerBody);
            root.Add(drawer);
        }

        // Up and down move through the main actions and wrap round at the ends.
        private void HomeNavigation(NavigationMoveEvent navigation)
        {
            if (navigation.target is not Button button || button.parent?.ClassListContains("main-actions") != true)
            {
                return;
            }

            int direction = navigation.direction == NavigationMoveEvent.Direction.Up ? -1
                : navigation.direction == NavigationMoveEvent.Direction.Down ? 1
                : 0;
            if (direction == 0)
            {
                return;
            }

            var siblings = button.parent;
            int next = (siblings.IndexOf(button) + direction + siblings.childCount) % siblings.childCount;
            ((Button)siblings.ElementAt(next)).Focus();
            navigation.StopImmediatePropagation();
        }

        private void ReturnFromDrawer()
        {
            if (settings.IsOpen)
            {
                settings.Cancel();
            }
            else
            {
                ShowHome();
            }
        }

        private void ShowSlots(bool newGame)
        {
            OpenDrawer("HELYI TÁRHELY   /   3 HELY", newGame ? "Új játék" : "Játék betöltése");
            var slots = new ScrollView();
            slots.AddToClassList("slot-list");
            Button first = null;
            for (int i = 0; i < CheckpointRules.Slots; i++)
            {
                var row = SlotRow(i, newGame, out var focusable);
                first ??= focusable;
                slots.Add(row);
            }

            drawerBody.Add(slots);
            if (first != null)
            {
                first.schedule.Execute(first.Focus);
            }
            else
            {
                drawer.Q<Button>().Focus();
            }
        }

        private VisualElement SlotRow(int index, bool newGame, out Button focusable)
        {
            var checkpoint = store.Read(index);
            bool present = store.Exists(index);
            var row = MenuWidgets.Box("slot");
            var description = MenuWidgets.Box("slot-copy");
            description.Add(MenuWidgets.Label($"HELY   /   0{index + 1}", "eyebrow"));
            description.Add(MenuWidgets.Label(checkpoint?.Location ?? (present ? "Sérült mentés" : "Üres hely"), "slot-title"));
            if (checkpoint != null)
            {
                string date = new DateTime(checkpoint.Time, DateTimeKind.Utc).ToLocalTime().ToString("yyyy.MM.dd. HH:mm");
                string link = checkpoint.CctvUnlocked ? "CCTV feloldva" : "CCTV zárolva";
                description.Add(MenuWidgets.Label($"{date}   ·   Elért szint {checkpoint.HighestFloor + 1}/4   ·   {link}", "metadata"));
            }

            row.Add(description);
            var buttons = MenuWidgets.Box("slot-actions");
            var primary = widgets.Button(newGame ? "Indítás" : "Betöltés", () =>
            {
                if (newGame)
                {
                    NewSlot(index);
                }
                else
                {
                    LoadSlot(index);
                }
            });
            primary.name = $"slot-action-{index}";
            primary.SetEnabled(newGame || checkpoint != null);
            buttons.Add(primary);
            focusable = primary.enabledSelf ? primary : null;
            if (present)
            {
                var delete = widgets.Button("Törlés", () => Confirm("Törlöd ezt a mentést?",
                    $"A(z) {index + 1}. hely tartalma végleg törlődik.", "Törlés", () =>
                    {
                        if (!store.Delete(index))
                        {
                            Notify("A mentés nem törölhető.");
                        }

                        ShowSlots(newGame);
                    }));
                delete.name = $"slot-delete-{index}";
                buttons.Add(delete);
                focusable ??= delete;
            }

            row.Add(buttons);
            return row;
        }

        private void NewSlot(int slot)
        {
            if (store.Exists(slot))
            {
                Confirm("Felülírod ezt a mentést?", $"Az új játék a(z) {slot + 1}. helyre kerül.", "Új játék", () => StartRun(slot, null));
            }
            else
            {
                StartRun(slot, null);
            }
        }

        private void LoadSlot(int slot)
        {
            var checkpoint = store.Read(slot);
            if (checkpoint == null)
            {
                Notify("A mentés hiányzik vagy megsérült.");
                ShowSlots(false);
                return;
            }

            StartRun(slot, checkpoint);
        }

        private void StartRun(int slot, HospitalCheckpoint checkpoint)
        {
            if (loading)
            {
                return;
            }

            loading = true;
            StartCoroutine(LoadHospital(slot, checkpoint));
        }

        private IEnumerator LoadHospital(int slot, HospitalCheckpoint checkpoint)
        {
            fade.RemoveFromClassList("transparent");
            fade.pickingMode = PickingMode.Position;
            home.SetEnabled(false);
            drawer.SetEnabled(false);
            yield return new WaitForSecondsRealtime(preferences.Active.ReduceMotion ? ReducedLoadFadeSeconds : LoadFadeSeconds);
            session.Begin(slot, checkpoint);
            yield return SceneManager.LoadSceneAsync(HospitalRun.HospitalScene);
        }

        private void Click()
        {
            if (feedback && feedback.clip)
            {
                feedback.PlayOneShot(feedback.clip);
            }
        }

        private void ApplyAppearance()
        {
            if (root == null)
            {
                return;
            }

            var options = preferences.Active;
            panel.scale = options.UiScale;
            root.EnableInClassList("contrast", options.UseHighContrast);
            root.EnableInClassList("static-motion", options.ReduceMotion || options.ReduceFlashing);
            dust.MarkDirtyRepaint();
            if (music)
            {
                music.volume = options.Music * MusicGain;
            }

            if (feedback)
            {
                feedback.volume = options.Ui * ClickGain;
            }
        }

        // Every 11 s the status line flickers for a tenth of a second, unless motion or flashing is reduced.
        private void GlitchStatus()
        {
            var options = preferences.Active;
            if (options.ReduceFlashing || options.ReduceMotion)
            {
                return;
            }

            systemStatus.text = StatusGlitch;
            systemStatus.schedule.Execute(() => systemStatus.text = StatusText).StartingIn(StatusGlitchMs);
        }

        private void AnimateBackdrop()
        {
            if (preferences.Active.ReduceMotion)
            {
                plate.style.translate = new Translate(0, 0);
                return;
            }

            atmosphereTime = Time.unscaledTime;
            float t = atmosphereTime * DriftRate;
            plate.style.translate = new Translate(Mathf.Sin(t) * DriftX, Mathf.Cos(t * .7f) * DriftY);
            dust.MarkDirtyRepaint();
        }

        private void PaintDust(MeshGenerationContext context)
        {
            if (preferences.Active.ReduceMotion)
            {
                return;
            }

            // Each speck has its own speed and size from its index, so the pattern never visibly repeats.
            var size = dust.contentRect.size;
            var pen = context.painter2D;
            for (int i = 0; i < DustSpecks; i++)
            {
                float x = Mathf.Repeat(i * .618f + atmosphereTime * (.0007f + i % 4 * .0003f), 1) * size.x;
                float y = Mathf.Repeat(i * .173f - atmosphereTime * .001f, 1) * size.y;
                float radius = 1f + (i % 4) * .36f;
                pen.fillColor = new Color(.69f, .85f, .89f, .17f + i % 3 * .04f);
                pen.BeginPath();
                pen.Arc(new Vector2(x, y), radius, 0, 360);
                pen.Fill();
            }
        }
    }
}
