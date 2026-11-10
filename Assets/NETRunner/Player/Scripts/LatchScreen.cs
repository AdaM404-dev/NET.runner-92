using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace NetRunner.Player
{
    public sealed class LatchScreen : MonoBehaviour
    {
        static readonly int ColorMap = Shader.PropertyToID("_UnlitColorMap");
        public static readonly Color Cyan = new(.40f, .93f, .89f);
        public static readonly Color Muted = new(.40f, .66f, .70f);
        public static readonly Color Amber = new(1f, .66f, .32f);
        public static readonly Color Fault = new(1f, .47f, .37f);

        [SerializeField] PanelSettings template;
        [SerializeField] Renderer display;
        UIDocument document;
        PanelSettings panel;
        RenderTexture surface;
        Label title, detail, instructions;
        VisualElement statusPage, cctvPage;
        Image feedImage;
        Label feedTitle, feedNumber;
        readonly VisualElement[] bars = new VisualElement[14];

        public RenderTexture Surface => surface;
        public string Status => title?.text;

        public void Configure(PanelSettings settings, Renderer screen)
        {
            template = settings;
            display = screen;
        }

        public void Open()
        {
            if (!document)
            {
                var description = new RenderTextureDescriptor(640, 324, GraphicsFormat.R8G8B8A8_SRGB,
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

        public void Close()
        {
            if (document) document.gameObject.SetActive(false);
            title = detail = instructions = null;
            statusPage = cctvPage = null;
            feedImage = null;
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
            cctvPage = Box(root, 0, 0, 640, 324, new Color(.025f, .055f, .062f));
            cctvPage.style.display = DisplayStyle.None;
            Box(cctvPage, 12, 8, 616, 2, Cyan);
            feedTitle = Text(cctvPage, "", 14, 12, 470, 29, 20, Cyan);
            feedNumber = Text(cctvPage, "", 480, 12, 147, 29, 17, Cyan);
            Box(cctvPage, 12, 44, 616, 244, new Color(.011f, .022f, .027f));
            feedImage = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
            Place(feedImage, 12, 44, 616, 244);
            cctvPage.Add(feedImage);
            Text(cctvPage, "ARROWS  SELECT CAMERA      1  CLOSE      R  CANCEL", 14, 292, 613, 24, 17, Cyan);
        }

        void BuildStatus(VisualElement root)
        {
            var frame = Box(root, 14, 12, 612, 300, new Color(.044f, .117f, .132f));
            frame.style.borderTopColor = Cyan;
            frame.style.borderTopWidth = 3;
            Text(root, "LATCH-07 / NORTHLINE SERVICE", 25, 22, 558, 26, 19, Cyan);
            Text(root, "07", 580, 24, 42, 24, 17, Muted);
            Box(root, 25, 56, 589, 1, Muted);
            title = Text(root, "", 25, 75, 596, 58, 41, Cyan);
            detail = Text(root, "", 27, 140, 590, 39, 23, Muted);
            Box(root, 25, 199, 589, 1, Muted);
            Text(root, "RELAY SIGNAL", 27, 212, 170, 24, 17, Muted);
            for (int i = 0; i < bars.Length; i++)
                bars[i] = Box(root, 194 + i * 29, 220, 22, 10, Cyan);
            instructions = Text(root, "", 25, 260, 590, 29, 20, Cyan);
        }

        public void Present(string heading, string message, string commands, Color accent, float strength)
        {
            if (title == null) return;
            statusPage.style.display = DisplayStyle.Flex;
            cctvPage.style.display = DisplayStyle.None;
            title.text = heading;
            title.style.color = accent;
            detail.text = message;
            instructions.text = commands;
            for (int i = 0; i < bars.Length; i++)
                bars[i].style.opacity = i < Mathf.RoundToInt(strength * bars.Length) ? .94f : .15f;
        }

        public void PresentFeed(SecurityCctv network)
        {
            if (feedImage == null) return;
            statusPage.style.display = DisplayStyle.None;
            cctvPage.style.display = DisplayStyle.Flex;
            feedImage.image = network.Surface;
            feedTitle.text = "CCTV / " + network.Label;
            feedNumber.text = $"{network.Selected + 1:00}/{network.Count:00}   LIVE";
        }

        static VisualElement Box(VisualElement parent, int x, int y, int width, int height, Color color)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            Place(element, x, y, width, height);
            element.style.backgroundColor = color;
            parent.Add(element);
            return element;
        }

        static Label Text(VisualElement parent, string text, int x, int y, int width, int height, int size, Color color)
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

        static void Place(VisualElement element, int x, int y, int width, int height)
        {
            element.style.position = Position.Absolute;
            element.style.left = x;
            element.style.top = y;
            element.style.width = width;
            element.style.height = height;
        }

        void OnDestroy()
        {
            if (document) document.gameObject.SetActive(false);
            if (panel) Destroy(panel);
            if (surface) Destroy(surface);
        }
    }
}
