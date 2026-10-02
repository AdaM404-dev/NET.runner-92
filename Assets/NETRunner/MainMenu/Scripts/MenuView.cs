using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace NetRunner.MenuPrototype
{
    // Owns presentation only. Controllers own sequence timing and menu state.
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuView : MonoBehaviour
    {
        public StyleSheet styleSheet;
        public MenuPrototypeSettings settings;
        public event Action<string> ActionSelected;
        public event Action<float> VolumeChanged;
        public event Action<float> FeedBrightnessChanged;
        public event Action<bool> CameraCycleChanged;
        public VisualElement Root { get; private set; }
        public VisualElement Menu { get; private set; }
        public VisualElement Loading { get; private set; }
        public VisualElement Terminal { get; private set; }
        public VisualElement Modal { get; private set; }
        public Label BootText { get; private set; }
        public Label Title { get; private set; }
        public Label ProgressText { get; private set; }
        public Label LoadingLog { get; private set; }
        public Label ArchiveLog { get; private set; }
        public Label CameraText { get; private set; }
        public Label SignalText { get; private set; }
        public Label ActivityText { get; private set; }
        public VisualElement Signal { get; private set; }
        public VisualElement FeedShade { get; private set; }
        private VisualElement segments;
        private VisualElement network;
        private Label facility;
        private Label networkStatus;
        private Label cameras;
        private Label power;
        private Button skipButton;
        private IVisualElementScheduledItem clock;
        private float volume = 0.35f;
        private float brightness = 0.12f;
        private bool cycle = true;

        public void Build()
        {
            Root = GetComponent<UIDocument>().rootVisualElement;
            Root.Clear();
            Root.name = "network-terminal";
            Root.AddToClassList("terminal-root");
            Root.styleSheets.Add(styleSheet);
            Root.style.color = settings.text;
            FeedShade = Element(Root, "feed-shade");
            var wash = Element(Root, "left-wash");
            wash.pickingMode = PickingMode.Ignore;
            var footerWash = Element(Root, "footer-wash");
            footerWash.pickingMode = PickingMode.Ignore;
            var header = Element(Root, "header");
            Label(header, "ACCESS NODE 92 / REMOTE TERMINAL", "micro");
            Label(header, "NR / INDUSTRIAL SYSTEMS", "micro quiet");
            var cameraStrip = Element(Root, "camera-strip");
            CameraText = Label(cameraStrip, "CAM_02 // INTERNAL NETWORK", "micro");
            SignalText = Label(cameraStrip, "SIGNAL: STABLE", "micro accent");
            var timestamp = Label(cameraStrip, "", "micro quiet");
            clock = Root.schedule.Execute(() => timestamp.text = DateTime.Now.ToString("yyyy.MM.dd  HH:mm:ss") + " / LIVE").Every(1000);

            Menu = Element(Root, "menu");
            Label(Menu, "WAREHOUSE / SECTOR 04", "eyebrow");
            Title = Label(Menu, "NET.RUNNER", "title");
            Label(Menu, "UNATTENDED FACILITY ACCESS", "subtitle");
            var actions = Element(Menu, "actions");
            AddButton(actions, "continue", "01   CONTINUE");
            AddButton(actions, "new-session", "02   NEW SESSION");
            AddButton(actions, "load-session", "03   LOAD SESSION");
            AddButton(actions, "configuration", "04   SYSTEM CONFIGURATION");
            AddButton(actions, "disconnect", "05   DISCONNECT");
            Label(Menu, "LOCAL CREDENTIALS / READ ONLY", "micro quiet menu-footnote");

            var status = Element(Root, "status");
            facility = Status(status, "FACILITY STATUS", settings.facilityStatus);
            facility.AddToClassList("warning");
            networkStatus = Status(status, "NETWORK STATUS", settings.networkStatus);
            cameras = Status(status, "ACTIVE CAMERAS", settings.activeCameras);
            power = Status(status, "POWER DRAW", settings.powerDraw);
            var bottom = Element(Root, "bottom-strip");
            ActivityText = Label(bottom, "OBSERVATION CHANNEL OPEN / NO OPERATOR PRESENT", "micro quiet");
            Label(bottom, "↑ ↓ SELECT    ENTER ACCESS    ESC BACK", "micro quiet");

            Loading = Element(Root, "loading fullscreen");
            var loadingContent = Element(Loading, "loading-content");
            Label(loadingContent, "DEEP ACCESS / NETWORK RECONSTRUCTION", "eyebrow");
            Label(loadingContent, "CONNECTING TO NODE...", "loading-title");
            Label(loadingContent, "WAREHOUSE_INTERNAL_04", "subtitle accent");
            network = Element(loadingContent, "network-map");
            for (int i = 0; i < 9; i++)
            {
                var node = Element(network, "network-node");
                node.name = "node-" + i;
                node.style.left = 34 + (i % 3) * 195;
                node.style.top = 24 + (i / 3) * 52;
                Label(node, new[] { "ENTRY", "RELAY", "CAM_02", "BAY_04", "BUS_09", "CAM_07", "POWER", "ARCHIVE", "LOCAL" }[i], "node-text");
                if (i % 3 != 2)
                {
                    var wire = Element(network, "network-wire");
                    wire.style.left = 104 + (i % 3) * 195;
                    wire.style.top = 33 + (i / 3) * 52;
                }
                if (i < 6)
                {
                    var wire = Element(network, "network-wire vertical");
                    wire.style.left = 44 + (i % 3) * 195;
                    wire.style.top = 43 + (i / 3) * 52;
                }
            }
            ProgressText = Label(loadingContent, "NETWORK ACCESS: 0%", "progress-label");
            segments = Element(loadingContent, "segments");
            for (int i = 0; i < 32; i++)
            {
                Element(segments, "segment");
            }
            var logs = Element(loadingContent, "logs");
            LoadingLog = Label(logs, "", "system-log");
            ArchiveLog = Label(logs, "", "system-log archive quiet");
            Label(loadingContent, "SANDBOX CHANNEL / NO EXTERNAL HANDOFF", "micro quiet");

            Terminal = Element(Root, "terminal fullscreen");
            var bootContent = Element(Terminal, "boot-content");
            Label(bootContent, "NR // NETWORK DIAGNOSTICS", "eyebrow");
            BootText = Label(bootContent, "", "boot-text");
            skipButton = AddButton(bootContent, "skip-boot", "ENTER / SKIP INITIALIZATION");
            skipButton.AddToClassList("small-button");
            Modal = Element(Root, "modal fullscreen");
            Signal = Element(Root, "signal fullscreen");
            Signal.pickingMode = PickingMode.Ignore;
            SetFeedBrightness(brightness);
            SetVisible(Menu, false);
            SetVisible(Loading, false);
            SetVisible(Modal, false);
            Signal.style.opacity = 0;
            Root.RegisterCallback<KeyDownEvent>(OnKey);
        }

        private void OnKey(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                ActionSelected?.Invoke("back");
                evt.StopPropagation();
            }
        }

        public void SetFacilityStatus(string state, string connection, string active, string draw)
        {
            facility.text = state;
            networkStatus.text = connection;
            cameras.text = active;
            power.text = draw;
        }

        public void ShowMenu()
        {
            SetVisible(skipButton, false);
            SetVisible(Terminal, false);
            SetVisible(Loading, false);
            SetVisible(Modal, false);
            SetVisible(Menu, true);
            Menu.style.opacity = 1;
            Focus("continue");
        }

        public void ShowModal(string kind)
        {
            Modal.Clear();
            SetVisible(Modal, true);
            Menu.SetEnabled(false);
            var card = Element(Modal, "modal-card");
            Label(card, "LOCAL TERMINAL / " + kind.ToUpperInvariant(), "eyebrow");
            if (kind == "configuration")
            {
                Label(card, "SYSTEM CONFIGURATION", "modal-title");
                Label(card, "SESSION-LOCAL CONTROLS", "micro quiet");
                var volumeSlider = new Slider("AMBIENCE", 0f, 1f) { name = "ambience-volume", value = volume };
                card.Add(volumeSlider);
                volumeSlider.RegisterValueChangedCallback(evt =>
                {
                    volume = evt.newValue;
                    VolumeChanged?.Invoke(volume);
                });
                var exposure = new Slider("FEED ATTENUATION", 0f, 0.5f) { name = "feed-attenuation", value = brightness };
                card.Add(exposure);
                exposure.RegisterValueChangedCallback(evt =>
                {
                    brightness = evt.newValue;
                    FeedBrightnessChanged?.Invoke(brightness);
                });
                var toggle = new Toggle("AUTOMATIC CAMERA CYCLING") { name = "camera-cycling", value = cycle };
                card.Add(toggle);
                toggle.RegisterValueChangedCallback(evt =>
                {
                    cycle = evt.newValue;
                    CameraCycleChanged?.Invoke(cycle);
                });
                Label(card, "Configuration resets when this scene is reopened.", "modal-copy quiet");
            }
            else if (kind == "load-session")
            {
                Label(card, "SESSION ARCHIVE", "modal-title");
                Label(card, "NO LOCAL RECORDS FOUND\n\nArchive storage is unavailable on this observation channel.", "modal-copy");
                Label(card, "SAVE SYSTEM / OFFLINE", "micro warning");
            }
            else
            {
                Label(card, "TERMINATE CONNECTION?", "modal-title");
                Label(card, "Release the remote access channel and disconnect from the facility.", "modal-copy");
                AddButton(card, "confirm-disconnect", "CONFIRM DISCONNECT");
            }
            AddButton(card, "close-modal", "RETURN TO OBSERVATION");
            Focus(kind == "disconnect" ? "confirm-disconnect" : "close-modal");
        }

        public void CloseModal()
        {
            SetVisible(Modal, false);
            Menu.SetEnabled(true);
        }

        public void SetProgress(float progress)
        {
            ProgressText.text = "NETWORK ACCESS: " + Mathf.FloorToInt(progress * 100f) + "%";
            for (int i = 0; i < segments.childCount; i++)
            {
                segments[i].style.backgroundColor = i < Mathf.FloorToInt(progress * 32) ? settings.accent : new Color(0.13f, 0.17f, 0.18f);
            }
            for (int i = 0; i < 9; i++)
            {
                var node = network.Q("node-" + i);
                node.style.backgroundColor = progress >= (i + 1) / 9f ? settings.accent * new Color(0.5f, 0.5f, 0.5f, 1f) : new Color(0.09f, 0.12f, 0.13f);
            }
        }

        public void SetFeedBrightness(float value) => FeedShade.style.backgroundColor = new Color(0, 0, 0, value);
        public void Focus(string name) => Root.Q<Button>(name)?.Focus();
        public static void SetVisible(VisualElement element, bool visible) => element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public static IEnumerator Fade(VisualElement element, float from, float to, float duration)
        {
            float start = Time.unscaledTime;
            do
            {
                element.style.opacity = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, (Time.unscaledTime - start) / Mathf.Max(0.01f, duration)));
                yield return null;
            } while (Time.unscaledTime - start < duration);
            element.style.opacity = to;
        }

        private Button AddButton(VisualElement parent, string action, string title)
        {
            var button = new Button(() => ActionSelected?.Invoke(action)) { name = action, text = title };
            button.AddToClassList("menu-button");
            parent.Add(button);
            return button;
        }

        private static VisualElement Element(VisualElement parent, string classes)
        {
            var element = new VisualElement();
            foreach (string token in classes.Split(' '))
            {
                element.AddToClassList(token);
            }
            parent.Add(element);
            return element;
        }

        private static Label Label(VisualElement parent, string text, string classes)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            foreach (string token in classes.Split(' '))
            {
                label.AddToClassList(token);
            }
            parent.Add(label);
            return label;
        }

        private static Label Status(VisualElement parent, string heading, string value)
        {
            var block = Element(parent, "status-block");
            Label(block, heading, "micro quiet");
            return Label(block, value, "status-value");
        }

        private void OnDisable()
        {
            clock?.Pause();
        }
    }
}
