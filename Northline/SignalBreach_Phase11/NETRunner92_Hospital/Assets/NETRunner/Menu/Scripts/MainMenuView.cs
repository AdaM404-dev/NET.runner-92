using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using NetRunner.Player;
using NetRunner.Saves;
using NetRunner.Settings;
using NetRunner.World;

namespace NetRunner.Menu
{
    public static class MenuControls
    {
        public static Action Feedback;

        public static Button Button(string text, Action action)
        {
            var button = new Button(() =>
            {
                Feedback?.Invoke();
                action();
            }) { text = text };
            button.AddToClassList("menu-button");
            return button;
        }

        public static Label Label(string text, string style)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(style);
            return label;
        }
    }

    [RequireComponent(typeof(UIDocument))]
    public sealed partial class MainMenuView : MonoBehaviour
    {
        [SerializeField] StyleSheet menuStyle;
        [SerializeField] Texture2D hospitalPlate;
        [SerializeField] TextAsset roomSchedule;
        [SerializeField] AudioSource music, feedback;
        [SerializeField] UnityEngine.Audio.AudioMixer hospitalMixer;

        UIDocument document;
        PanelSettings panel, panelTemplate;
        VisualElement root, home, drawer, scrim, dialog, fade, plate, dust;
        Label drawerTitle, drawerMark, toast, systemStatus;
        VisualElement drawerBody;
        Button homeFocus, dialogBack;
        Action closeDialog;
        IDisposable bindingListener;
        GamePreferences draft;
        bool loading;
        float atmosphereTime;
        int destinationSlot = -1;
        bool settingsOpen;

        public void Configure(StyleSheet styles, Texture2D backdrop, TextAsset schedule,
            AudioSource score, AudioSource click, UnityEngine.Audio.AudioMixer mixer)
        {
            menuStyle = styles;
            hospitalPlate = backdrop;
            roomSchedule = schedule;
            music = score;
            feedback = click;
            hospitalMixer = mixer;
        }

        void Awake()
        {
            document = GetComponent<UIDocument>();
            panelTemplate = document.panelSettings;
            panel = Instantiate(panelTemplate);
            document.panelSettings = panel;
        }

        void OnEnable()
        {
            GameOptions.Changed += ApplyAppearance;
            MenuControls.Feedback = Click;
        }

        void Start()
        {
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameOptions.SetMixer(hospitalMixer);
            Build();
            ShowHome();
            ApplyAppearance();
            root.schedule.Execute(() => fade.AddToClassList("transparent")).StartingIn(20);
            root.schedule.Execute(AnimateBackdrop).Every(50);
            root.schedule.Execute(SyncStatus).StartingIn(11000).Every(11000);
        }

        void OnDisable()
        {
            GameOptions.Changed -= ApplyAppearance;
            MenuControls.Feedback = null;
            CancelBinding();
        }

        void OnDestroy()
        {
            if (panel) Destroy(panel);
        }

        void Build()
        {
            root = document.rootVisualElement;
            root.Clear();
            root.styleSheets.Add(menuStyle);
            root.AddToClassList("net-menu");

            plate = Box("plate");
            plate.style.backgroundImage = new StyleBackground(hospitalPlate);
            root.Add(plate);
            var gradient = Box("backdrop-gradient");
            gradient.generateVisualContent += PaintShade;
            root.Add(gradient);
            dust = Box("dust");
            dust.generateVisualContent += PaintDust;
            root.Add(dust);

            var top = Box("topline");
            top.Add(MenuControls.Label("+   N O R T H L I N E   M E D I C A L", "eyebrow"));
            systemStatus = MenuControls.Label("RECOVERY CONSOLE   /   092", "metadata");
            top.Add(systemStatus);
            root.Add(top);

            home = Box("home");
            home.RegisterCallback<NavigationMoveEvent>(HomeNavigation);
            root.Add(home);

            var bottom = Box("bottomline");
            bottom.Add(MenuControls.Label("NORTHLINE   /   RESTRICTED RESEARCH", "metadata"));
            bottom.Add(MenuControls.Label("04   /   CHECKPOINT A", "eyebrow"));
            root.Add(bottom);
            toast = MenuControls.Label("", "toast");
            root.Add(toast);

            drawer = Box("drawer");
            var heading = Box("drawer-heading");
            var headingCopy = new VisualElement();
            drawerMark = MenuControls.Label("", "eyebrow");
            drawerTitle = MenuControls.Label("", "drawer-title");
            headingCopy.Add(drawerMark);
            headingCopy.Add(drawerTitle);
            heading.Add(headingCopy);
            var back = MenuControls.Button("Vissza", ReturnFromDrawer);
            back.name = "drawer-back";
            heading.Add(back);
            drawer.Add(heading);
            drawerBody = Box("drawer-body");
            drawer.Add(drawerBody);
            root.Add(drawer);

            scrim = Box("dialog-scrim");
            dialog = Box("dialog-card");
            scrim.Add(dialog);
            root.Add(scrim);
            HideDialog();

            fade = Box("fade");
            root.Add(fade);
        }

        void ShowHome()
        {
            settingsOpen = false;
            drawer.style.display = DisplayStyle.None;
            home.style.display = DisplayStyle.Flex;
            home.Clear();
            home.Add(MenuControls.Label("01   /   HELYI HOZZÁFÉRÉS", "eyebrow"));
            var wordmark = Box("wordmark");
            wordmark.Add(MenuControls.Label("NET.runner", "logo"));
            wordmark.Add(MenuControls.Label("–92", "logo-index"));
            home.Add(wordmark);
            home.Add(MenuControls.Label("NORTHLINE  //  KARANTÉNZÓNA", "subtitle"));
            var menu = Box("main-actions");
            int recent = CheckpointStore.MostRecent();
            if (recent >= 0)
                menu.Add(HomeButton("Folytatás", "01", "A legutóbbi érvényes mentés", () => LoadSlot(recent), "continue"));
            menu.Add(HomeButton("Új játék", "02", "Belépés a karanténzónába", () => ShowSlots(true), "new-game"));
            menu.Add(HomeButton("Játék betöltése", "03", "Mentéshelyek megnyitása", () => ShowSlots(false), "load-game"));
            menu.Add(HomeButton("Beállítások", "04", "Kép, hang, irányítás", ShowSettings, "settings"));
            menu.Add(HomeButton("Kilépés", "05", "A munkamenet lezárása",
                () => Confirm("Kilépés a játékból?", "A mentéseid a gépen maradnak.", "Kilépés", Application.Quit),
                "exit"));
            home.Add(menu);
            homeFocus = (Button)menu.ElementAt(0);
            home.schedule.Execute(homeFocus.Focus);
        }

        static Button HomeButton(string title, string id, string description, Action action, string name)
        {
            var button = MenuControls.Button("", action);
            button.name = name;
            button.tooltip = description;
            button.AddToClassList("home-button");
            button.Add(MenuControls.Label(id, "button-index"));
            button.Add(MenuControls.Label(title, "button-title"));
            button.Add(MenuControls.Label("›", "button-indicator"));
            return button;
        }

        void HomeNavigation(NavigationMoveEvent evt)
        {
            if (evt.target is not Button button || button.parent?.ClassListContains("main-actions") != true) return;
            int direction = evt.direction == NavigationMoveEvent.Direction.Up ? -1
                : evt.direction == NavigationMoveEvent.Direction.Down ? 1 : 0;
            if (direction == 0) return;
            var siblings = button.parent;
            int next = (siblings.IndexOf(button) + direction + siblings.childCount) % siblings.childCount;
            ((Button)siblings.ElementAt(next)).Focus();
            evt.StopImmediatePropagation();
        }

        void OpenDrawer(string mark, string title)
        {
            home.style.display = DisplayStyle.None;
            drawerMark.text = mark;
            drawerTitle.text = title;
            drawer.AddToClassList("drawer-entering");
            drawer.style.display = DisplayStyle.Flex;
            drawer.schedule.Execute(() => drawer.RemoveFromClassList("drawer-entering")).StartingIn(20);
            drawerBody.Clear();
        }

        void ReturnFromDrawer()
        {
            if (settingsOpen) CancelSettings();
            else ShowHome();
        }

        void ShowSlots(bool newGame)
        {
            OpenDrawer("HELYI TÁRHELY   /   3 HELY", newGame ? "Új játék" : "Játék betöltése");
            var slots = new ScrollView();
            slots.AddToClassList("slot-list");
            Button first = null;
            for (int i = 0; i < CheckpointStore.Slots; i++)
            {
                int index = i;
                var checkpoint = CheckpointStore.Read(i);
                bool present = CheckpointStore.Exists(i);
                var row = Box("slot");
                var description = Box("slot-copy");
                description.Add(MenuControls.Label($"HELY   /   0{i + 1}", "eyebrow"));
                description.Add(MenuControls.Label(checkpoint?.location ?? (present ? "Sérült mentés" : "Üres hely"), "slot-title"));
                if (checkpoint != null)
                {
                    string date = new DateTime(checkpoint.time, DateTimeKind.Utc).ToLocalTime().ToString("yyyy.MM.dd. HH:mm");
                    string link = checkpoint.cctv ? "CCTV feloldva" : "CCTV zárolva";
                    description.Add(MenuControls.Label($"{date}   ·   Elért szint {checkpoint.highestFloor + 1}/4   ·   {link}", "metadata"));
                }
                row.Add(description);
                var buttons = Box("slot-actions");
                var primary = MenuControls.Button(newGame ? "Indítás" : "Betöltés", () =>
                {
                    if (newGame) NewSlot(index);
                    else LoadSlot(index);
                });
                primary.name = $"slot-action-{index}";
                primary.SetEnabled(newGame || checkpoint != null);
                buttons.Add(primary);
                if (primary.enabledSelf) first ??= primary;
                if (present)
                {
                    var delete = MenuControls.Button("Törlés", () =>
                    {
                        Confirm("Törlöd ezt a mentést?", $"A(z) {index + 1}. hely tartalma végleg törlődik.", "Törlés", () =>
                        {
                            if (!CheckpointStore.Delete(index)) Notify("A mentés nem törölhető.");
                            ShowSlots(newGame);
                        });
                    });
                    delete.name = $"slot-delete-{index}";
                    buttons.Add(delete);
                    first ??= delete;
                }
                row.Add(buttons);
                slots.Add(row);
            }
            drawerBody.Add(slots);
            if (first != null) first.schedule.Execute(first.Focus);
            else drawer.Q<Button>().Focus();
        }

        void NewSlot(int slot)
        {
            if (CheckpointStore.Exists(slot))
                Confirm("Felülírod ezt a mentést?", $"Az új játék a(z) {slot + 1}. helyre kerül.", "Új játék",
                    () => StartRun(slot, null));
            else StartRun(slot, null);
        }

        void LoadSlot(int slot)
        {
            var checkpoint = CheckpointStore.Read(slot);
            if (checkpoint == null)
            {
                Notify("A mentés hiányzik vagy megsérült.");
                ShowSlots(false);
                return;
            }
            StartRun(slot, checkpoint);
        }

        void StartRun(int slot, HospitalCheckpoint checkpoint)
        {
            if (loading) return;
            loading = true;
            destinationSlot = slot;
            StartCoroutine(LoadHospital(checkpoint));
        }

        IEnumerator LoadHospital(HospitalCheckpoint checkpoint)
        {
            fade.RemoveFromClassList("transparent");
            fade.pickingMode = PickingMode.Position;
            home.SetEnabled(false);
            drawer.SetEnabled(false);
            yield return new WaitForSecondsRealtime(GameOptions.Active.reducedMotion ? .05f : .46f);
            HospitalRun.Enter(destinationSlot, checkpoint, panelTemplate, menuStyle, roomSchedule);
            yield return SceneManager.LoadSceneAsync(HospitalRun.HospitalScene);
        }

        void Confirm(string title, string message, string action, Action yes)
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

        Label ShowDialog(string title, string message, string action, Action yes, Action no)
        {
            closeDialog = no;
            scrim.style.display = DisplayStyle.Flex;
            home.SetEnabled(false);
            drawer.SetEnabled(false);
            dialog.Clear();
            dialog.Add(MenuControls.Label("NORTHLINE   /   MEGERŐSÍTÉS", "eyebrow"));
            dialog.Add(MenuControls.Label(title, "dialog-title"));
            var copy = MenuControls.Label(message, "dialog-copy");
            dialog.Add(copy);
            var buttons = Box("dialog-actions");
            dialogBack = MenuControls.Button("Mégse", no);
            dialogBack.name = "dialog-cancel";
            buttons.Add(dialogBack);
            var confirm = MenuControls.Button(action, yes);
            confirm.name = "dialog-confirm";
            confirm.AddToClassList("primary");
            buttons.Add(confirm);
            dialog.Add(buttons);
            dialogBack.schedule.Execute(dialogBack.Focus);
            return copy;
        }

        void HideDialog()
        {
            closeDialog = null;
            scrim.style.display = DisplayStyle.None;
            home.SetEnabled(true);
            drawer.SetEnabled(true);
        }

        void Update()
        {
            if (loading || bindingListener != null || Time.frameCount == bindingCancelFrame
                || Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            if (closeDialog != null) closeDialog();
            else if (drawer.style.display == DisplayStyle.Flex) ReturnFromDrawer();
        }

        void Notify(string message)
        {
            toast.text = message;
            toast.EnableInClassList("shown", true);
            toast.schedule.Execute(() =>
            {
                if (toast.text == message) toast.EnableInClassList("shown", false);
            }).StartingIn(3200);
        }

        void Click()
        {
            if (feedback && feedback.clip) feedback.PlayOneShot(feedback.clip);
        }

        void ApplyAppearance()
        {
            if (root == null) return;
            var options = GameOptions.Active;
            panel.scale = options.uiScale;
            root.EnableInClassList("contrast", options.highContrast);
            root.EnableInClassList("static-motion", options.reducedMotion || options.reducedFlashing);
            dust.MarkDirtyRepaint();
            if (music) music.volume = options.music * .25f;
            if (feedback) feedback.volume = options.ui * .30f;
        }

        void SyncStatus()
        {
            var options = GameOptions.Active;
            if (options.reducedFlashing || options.reducedMotion) return;
            systemStatus.text = "RECOVERY CONSO_E   /   092";
            systemStatus.schedule.Execute(() => systemStatus.text = "RECOVERY CONSOLE   /   092").StartingIn(100);
        }

        void AnimateBackdrop()
        {
            if (GameOptions.Active.reducedMotion)
            {
                plate.style.translate = new Translate(0, 0);
                return;
            }
            atmosphereTime = Time.unscaledTime;
            float t = atmosphereTime * .075f;
            plate.style.translate = new Translate(Mathf.Sin(t) * 13, Mathf.Cos(t * .7f) * 7);
            dust.MarkDirtyRepaint();
        }

        void PaintDust(MeshGenerationContext context)
        {
            if (GameOptions.Active.reducedMotion) return;
            var size = dust.contentRect.size;
            var pen = context.painter2D;
            for (int i = 0; i < 24; i++)
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

        static void PaintShade(MeshGenerationContext context)
        {
            var size = context.visualElement.contentRect.size;
            var ink = new Color32(4, 9, 14, 255);
            var mesh = context.Allocate(8, 18);
            float[] boundaries = { 0, .27f, .64f, 1 };
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

        static VisualElement Box(string style)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(style);
            return element;
        }
    }
}
