using NetRunner.Core;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace NetRunner.Player
{
    /// <summary>
    /// The LATCH-07 tablet's live screen: a UI Toolkit panel rendered into a
    /// 640 × 324 texture that is shown on the tablet's display mesh. It has
    /// two pages, the status (<see cref="Present"/>) and the CCTV feed
    /// (<see cref="PresentFeed"/>). The colours are also used by the
    /// reception terminal, so both screens match.
    /// </summary>
    public sealed class LatchScreen : MonoBehaviour
    {
        /// <summary>Normal text and frames.</summary>
        public static readonly Color Cyan = new Color(.40f, .93f, .89f);

        /// <summary>Secondary text.</summary>
        public static readonly Color Muted = new Color(.40f, .66f, .70f);

        /// <summary>Waiting without a target.</summary>
        public static readonly Color Amber = new Color(1f, .66f, .32f);

        /// <summary>Errors.</summary>
        public static readonly Color Fault = new Color(1f, .47f, .37f);

        // The display's resolution, in pixels; every layout number below is in these pixels.
        private const int Width = 640;
        private const int Height = 324;
        private const int SignalBars = 14;
        private const float LitBar = .94f;
        private const float DarkBar = .15f;

        private static readonly int ColorMap = Shader.PropertyToID("_UnlitColorMap");
        private static readonly Color Background = new Color(.032f, .079f, .091f);
        private static readonly Color StatusFrame = new Color(.044f, .117f, .132f);
        private static readonly Color FeedBackground = new Color(.025f, .055f, .062f);
        private static readonly Color FeedWell = new Color(.011f, .022f, .027f);

        [SerializeField] private PanelSettings template;
        [SerializeField] private Renderer display;

        private readonly VisualElement[] bars = new VisualElement[SignalBars];
        private UIDocument document;
        private PanelSettings panel;
        private RenderTexture surface;
        private Label title;
        private Label detail;
        private Label instructions;
        private VisualElement statusPage;
        private VisualElement cctvPage;
        private Image feedImage;
        private Label feedTitle;
        private Label feedNumber;
        private SignalBreachPanel breachPage;

        /// <summary>The texture the screen renders into.</summary>
        public RenderTexture Surface => surface;
        public string Status => breachPage != null && breachPage.style.display == DisplayStyle.Flex
            ? breachPage.Heading : title?.text;

        private void OnDestroy()
        {
            if (document)
            {
                document.gameObject.SetActive(false);
            }

            if (panel)
            {
                Destroy(panel);
            }

            if (surface)
            {
                Destroy(surface);
            }
        }

        /// <summary>Sets the panel template and the display mesh; the LATCH-07 installer calls it.</summary>
        public void Configure(PanelSettings settings, Renderer screen)
        {
            template = settings;
            display = screen;
        }

        /// <summary>Switches the screen on, creating its texture and panel the first time.</summary>
        public void Open()
        {
            if (!document)
            {
                var description = new RenderTextureDescriptor(Width, Height, GraphicsFormat.R8G8B8A8_SRGB,
                    GraphicsFormat.D24_UNorm_S8_UInt);
                surface = new RenderTexture(description) { name = "LATCH-07 live display", wrapMode = TextureWrapMode.Clamp };
                surface.Create();
                panel = Instantiate(template);
                panel.targetTexture = surface;
                var host = new GameObject("LATCH-07 display panel");
                host.SetActive(false);
                host.transform.SetParent(transform, false);
                document = host.AddComponent<UIDocument>();
                document.panelSettings = panel;
                var block = new MaterialPropertyBlock();
                display.GetPropertyBlock(block);
                block.SetTexture(ColorMap, surface);
                display.SetPropertyBlock(block);
            }

            document.gameObject.SetActive(true);
            Build();
        }

        /// <summary>Switches the screen off.</summary>
        public void Close()
        {
            if (document) document.gameObject.SetActive(false);
            title = detail = instructions = null;
            statusPage = cctvPage = null;
            feedImage = null;
            breachPage = null;
        }

        void Build()
        {
            var root = document.rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            root.style.width = 640;
            root.style.height = 324;
            root.style.backgroundColor = new Color(.032f, .079f, .091f);
            statusPage = Box(root, 0, 0, 640, 324, Color.clear);
            BuildStatus(statusPage);
            cctvPage = Box(root, 0, 0, 640, 324, new Color(.027f, .059f, .075f));
            cctvPage.style.display = DisplayStyle.None;
            Box(cctvPage, 12, 8, 616, 2, Cyan);
            feedTitle = Text(cctvPage, "", 14, 12, 465, 29, 24, Cyan);
            feedNumber = Text(cctvPage, "", 480, 12, 146, 29, 17, Cyan);
            feedNumber.style.unityTextAlign = TextAnchor.MiddleRight;
            Box(cctvPage, 12, 44, 616, 244, new Color(.011f, .022f, .027f));
            feedImage = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
            Place(feedImage, 12, 44, 616, 244);
            cctvPage.Add(feedImage);
            Text(cctvPage, "LATCH-07 / NORTHLINE", 14, 292, 320, 24, 17, Cyan);
            Text(cctvPage, "ÉLŐ ADÁS", 465, 292, 161, 24, 17, Cyan).style.unityTextAlign = TextAnchor.MiddleRight;
        }

        /// <summary>Shows the status page with <paramref name="status"/>.</summary>
        public void Present(LatchStatus status)
        {
            if (title == null)
            {
                return;
            }

            statusPage.style.display = DisplayStyle.Flex;
            cctvPage.style.display = DisplayStyle.None;
            if (breachPage != null) breachPage.style.display = DisplayStyle.None;
            title.text = status.Heading;
            title.style.color = ColourOf(status.Accent);
            detail.text = status.Message;
            instructions.text = status.Commands;
            int lit = status.LitBars(bars.Length);
            for (int i = 0; i < bars.Length; i++)
            {
                bars[i].style.opacity = i < lit ? LitBar : DarkBar;
            }
        }

        /// <summary>Shows the CCTV page with the selected feed of <paramref name="network"/>.</summary>
        public void PresentFeed(SecurityCctv network)
        {
            if (feedImage == null)
            {
                return;
            }

            statusPage.style.display = DisplayStyle.None;
            cctvPage.style.display = DisplayStyle.Flex;
            if (breachPage != null) breachPage.style.display = DisplayStyle.None;
            feedImage.image = network.Surface;
            feedTitle.text = network.Label switch
            {
                "PUBLIC ENTRY" => "Főbejárat",
                "RECEPTION" => "Recepció",
                "EMERGENCY CONCOURSE" => "Sürgősségi",
                "PATIENT WARDS" => "Betegosztály",
                "SURGERY CONCOURSE" => "Műtőfolyosó",
                "RESEARCH ACCESS" => "Kutatási beléptetés",
                _ => network.Label
            };
            feedNumber.text = $"CAM {network.Selected + 1:00} / {network.Count:00}";
        }

        public void PresentBreach(SignalBreach puzzle, float seconds, float disableSeconds, bool reduceMotion)
        {
            if (title == null) return;
            if (breachPage == null)
            {
                breachPage = new SignalBreachPanel();
                document.rootVisualElement.Add(breachPage);
            }
            statusPage.style.display = DisplayStyle.None;
            cctvPage.style.display = DisplayStyle.None;
            breachPage.style.display = DisplayStyle.Flex;
            breachPage.Present(puzzle, seconds, disableSeconds, reduceMotion);
        }

        private static Color ColourOf(LatchAccent accent)
        {
            switch (accent)
            {
                case LatchAccent.Amber:
                    return Amber;
                case LatchAccent.Fault:
                    return Fault;
                case LatchAccent.Muted:
                    return Muted;
                default:
                    return Cyan;
            }
        }

        public static VisualElement Box(VisualElement parent, int x, int y, int width, int height, Color color)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            Place(element, x, y, width, height);
            element.style.backgroundColor = color;
            parent.Add(element);
            return element;
        }

        public static Label Text(VisualElement parent, string text, int x, int y, int width, int height, int size, Color color)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            Place(label, x, y, width, height);
            label.style.fontSize = size;
            label.style.color = color;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.marginLeft = 0;
            label.style.paddingLeft = 0;
            parent.Add(label);
            return label;
        }

        private static void Place(VisualElement element, int x, int y, int width, int height)
        {
            element.style.position = Position.Absolute;
            element.style.left = x;
            element.style.top = y;
            element.style.width = width;
            element.style.height = height;
        }

        private void BuildStatus(VisualElement root)
        {
            VisualElement frame = Box(root, 14, 12, 612, 300, StatusFrame);
            frame.style.borderTopColor = Cyan;
            frame.style.borderTopWidth = 3;
            Text(root, "LATCH-07 / NORTHLINE SERVICE", 25, 22, 558, 26, 19, Cyan);
            Text(root, "07", 580, 24, 42, 24, 17, Muted);
            Box(root, 25, 56, 589, 1, Muted);
            title = Text(root, string.Empty, 25, 75, 596, 58, 41, Cyan);
            detail = Text(root, string.Empty, 27, 140, 590, 39, 23, Muted);
            Box(root, 25, 199, 589, 1, Muted);
            Text(root, "RELAY SIGNAL", 27, 212, 170, 24, 17, Muted);
            for (int i = 0; i < bars.Length; i++)
            {
                bars[i] = Box(root, 194 + i * 29, 220, 22, 10, Cyan);
            }

            instructions = Text(root, string.Empty, 25, 260, 590, 29, 20, Cyan);
        }
    }
}
