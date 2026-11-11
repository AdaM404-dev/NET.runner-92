using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using NetRunner.Player;
using NetRunner.Settings;
using NetRunner.World;

namespace NetRunner.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class HospitalHud : MonoBehaviour
    {
        const int VitalSegments = 18;
        static readonly Color Cyan = new Color32(0, 255, 234, 255);
        static readonly Color PanelInk = new Color(29f / 255f, 29f / 255f, 29f / 255f, .44f);
        static readonly string[] Digits = { "", "01", "02", "03", "04" };

        [SerializeField] HospitalInteraction interaction;
        [SerializeField] LatchViewmodel latch;
        [SerializeField] SecurityCctv network;
        [SerializeField] HospitalLift lift;

        UIDocument document;
        VisualElement root, world, terminal, viewer, goal, goalHistory, prompt, notice;
        VisualElement healthBar, hitOverlay;
        Label defeat;
        FirstPersonController player;
        PanelSettings runtimePanel;
        Label goalTitle, goalDetail, promptKey, promptText, terminalStatus;
        readonly List<VisualElement> channels = new();
        readonly List<Label> routeCells = new();
        int currentGoal = -1;

        public void Configure(HospitalInteraction input, LatchViewmodel tablet, SecurityCctv cameras,
            HospitalLift elevator)
        {
            interaction = input;
            latch = tablet;
            network = cameras;
            lift = elevator;
        }

        void Awake()
        {
            document = GetComponent<UIDocument>();
            runtimePanel = Instantiate(document.panelSettings);
            document.panelSettings = runtimePanel;
            player = interaction.GetComponent<FirstPersonController>();
        }

        void OnEnable() => GameOptions.Changed += ApplyAppearance;
        void OnDisable() => GameOptions.Changed -= ApplyAppearance;
        void OnDestroy() { if (runtimePanel) Destroy(runtimePanel); }

        void ApplyAppearance()
        {
            var options = GameOptions.Active;
            if (runtimePanel) runtimePanel.scale = options.uiScale;
            if (root == null) return;
            world.style.opacity = options.hudOpacity;
            root.EnableInClassList("hud-contrast", options.highContrast);
            Visible(goal, options.objectives);
            root.Q("goal-shape").MarkDirtyRepaint();
            root.Q<Label>("terminal-exit-key").text = HospitalKeys.Label(GameAction.Interact) + " / Esc";
            root.Q<Label>("viewer-exit-key").text = HospitalKeys.Label(GameAction.Cctv);
            root.Q<Label>("viewer-stow-key").text = HospitalKeys.Label(GameAction.Stow);
        }

        void Bind(VisualElement tree)
        {
            root = tree;
            root.pickingMode = PickingMode.Ignore;
            root.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);
            world = root.Q("world");
            terminal = root.Q("terminal");
            viewer = root.Q("viewer");
            prompt = root.Q("prompt");
            notice = root.Q("notice");
            goal = root.Q("goal");
            goalTitle = root.Q<Label>("goal-title");
            goalHistory = root.Q("goal-history");
            goalDetail = goalHistory.Q<Label>("goal-detail");

            var goalShape = root.Q("goal-shape");
            goalShape.generateVisualContent += context => DrawGoal(context.painter2D, goalShape.contentRect.size);
            root.Q("health-icon").generateVisualContent += context => DrawHeart(context.painter2D);
            root.Q("stamina-icon").generateVisualContent += context => DrawBolt(context.painter2D);
            healthBar = root.Q("health-bar");
            PopulateBar(healthBar);
            PopulateBar(root.Q("stamina-bar"));
            hitOverlay = new VisualElement { pickingMode = PickingMode.Ignore };
            hitOverlay.style.position = Position.Absolute;
            hitOverlay.style.left = hitOverlay.style.right = hitOverlay.style.top = hitOverlay.style.bottom = 0;
            hitOverlay.style.alignItems = Align.Center;
            hitOverlay.style.justifyContent = Justify.Center;
            defeat = new Label("HARCKÉPTELEN  /  ELLENŐRZŐPONT BETÖLTÉSE") { pickingMode = PickingMode.Ignore };
            defeat.style.color = Color.white;
            defeat.style.fontSize = 22;
            hitOverlay.Add(defeat);
            root.Add(hitOverlay);
            promptKey = root.Q<Label>("prompt-key");
            promptText = root.Q<Label>("prompt-text");
            terminalStatus = root.Q<Label>("terminal-status");

            var reference = root.Q("reference-cells");
            var route = root.Q("route-cells");
            reference.Clear();
            route.Clear();
            routeCells.Clear();
            for (int i = 0; i < ReceptionSecurityTerminal.CellCount; i++)
            {
                var cell = new Label(Digits[ReceptionSecurityTerminal.ReferenceCell(i)]) { pickingMode = PickingMode.Ignore };
                cell.AddToClassList("cell");
                cell.AddToClassList("reference");
                reference.Add(cell);

                var editable = new Label { pickingMode = PickingMode.Ignore };
                editable.AddToClassList("cell");
                route.Add(editable);
                routeCells.Add(editable);
            }

            var markers = root.Q("channels");
            markers.Clear();
            channels.Clear();
            for (int i = 0; i < network.Count; i++)
            {
                var mark = new VisualElement { pickingMode = PickingMode.Ignore };
                mark.AddToClassList("channel");
                markers.Add(mark);
                channels.Add(mark);
            }
            currentGoal = -1;
            ApplyAppearance();
        }

        void LateUpdate()
        {
            var tree = document.rootVisualElement;
            if (tree == null) return;
            if (root != tree) Bind(tree);
            UpdateHealth();
            bool captured = Cursor.lockState == CursorLockMode.Locked;
            var activeTerminal = interaction.ActiveTerminal as ReceptionSecurityTerminal;
            bool inTerminal = captured && activeTerminal;
            bool inCctv = captured && network.Watching;
            bool exploring = captured && !inTerminal && !latch.Active && GameOptions.Active.hud;
            Visible(world, exploring);
            Visible(terminal, inTerminal);
            Visible(viewer, inCctv);
            Visible(notice, captured && !inTerminal && !inCctv && !network.Unlocked
                && latch.CctvHintVisible && interaction.Target is not ReceptionSecurityTerminal);

            if (inTerminal)
            {
                for (int i = 0; i < routeCells.Count; i++)
                {
                    routeCells[i].text = Digits[activeTerminal.RouteCell(i)];
                    routeCells[i].EnableInClassList("selected", i == activeTerminal.SelectedCell);
                }
                terminalStatus.text = activeTerminal.Mismatch ? "A jel eltér. Ellenőrizd a három cellát." : "Hozzáférés zárolva";
                terminalStatus.EnableInClassList("fault", activeTerminal.Mismatch);
            }
            else if (inCctv)
            {
                for (int i = 0; i < channels.Count; i++)
                    channels[i].EnableInClassList("selected", i == network.Selected);
            }
            else if (exploring)
            {
                RefreshExploration();
            }
        }

        void UpdateHealth()
        {
            if (!player) return;
            int lit = Mathf.CeilToInt(player.Health * VitalSegments / 100f);
            for (int i = 0; i < VitalSegments; i++)
                healthBar[i].EnableInClassList("depleted", i >= lit);
            float flash = Mathf.Clamp01(1f - (Time.unscaledTime - player.LastHitTime) / .42f);
            float intensity = player.IsDown ? .58f : flash * (GameOptions.Active.reducedMotion ? .15f : .36f);
            hitOverlay.style.backgroundColor = new Color(.50f, .018f, .025f, intensity);
            defeat.style.display = player.IsDown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void RefreshExploration()
        {
            int goal = !network.Unlocked ? 0 : lift && lift.Unlocked(3) ? 2 : 1;
            if (currentGoal != goal)
            {
                currentGoal = goal;
                goalTitle.text = goal switch
                {
                    0 => "Törd fel a CCTV-kamerákat",
                    1 => "Juss fel az emeletre",
                    _ => "Derítsd fel a kutatást"
                };
                goalDetail.text = goal == 2 ? "Feljutottál az emeletre" : "CCTV-kamerák feltörve";
                Visible(goalHistory, goal > 0);
            }

            var target = interaction.Target;
            var (key, text) = target != null ? target.Prompt : (null, null);
            bool breachable = BreachTargets.Alive(interaction.BreachTarget);
            if (breachable)
            {
                key = HospitalKeys.Label(GameAction.Tablet);
                int retry = latch.BreachRetrySeconds;
                text = retry > 0 ? $"SIGNAL BREACH  /  RETRY {retry:00}s" : "SIGNAL BREACH";
            }
            Visible(prompt, key != null && GameOptions.Active.prompts);
            if (key == null) return;

            promptKey.text = key;
            promptText.text = !breachable && key == HospitalKeys.Label(GameAction.Interact)
                ? "INTERAKCIÓ" : text.ToUpper(CultureInfo.GetCultureInfo("hu-HU"));
        }

        static void PopulateBar(VisualElement bar)
        {
            bar.Clear();
            for (int i = 0; i < VitalSegments; i++)
            {
                var segment = new VisualElement { pickingMode = PickingMode.Ignore };
                segment.AddToClassList("segment");
                segment.EnableInClassList("last", i == VitalSegments - 1);
                bar.Add(segment);
            }
        }

        static void DrawGoal(Painter2D pen, Vector2 size)
        {
            float right = size.x - 2.5f;
            float bottom = size.y - 2.5f;
            float notch = size.x * .70f;
            pen.fillColor = GameOptions.Active.highContrast ? new Color(.015f, .015f, .015f, .88f) : PanelInk;
            pen.BeginPath();
            pen.MoveTo(new Vector2(2.5f, 2.5f));
            pen.LineTo(new Vector2(right, 2.5f));
            pen.LineTo(new Vector2(notch, bottom));
            pen.LineTo(new Vector2(2.5f, bottom));
            pen.ClosePath();
            pen.Fill();

            pen.strokeColor = Color.white;
            pen.lineWidth = 5;
            pen.BeginPath();
            pen.MoveTo(new Vector2(2.5f, 22));
            pen.LineTo(new Vector2(2.5f, 2.5f));
            pen.LineTo(new Vector2(22, 2.5f));
            pen.MoveTo(new Vector2(right - 22, 2.5f));
            pen.LineTo(new Vector2(right, 2.5f));
            pen.MoveTo(new Vector2(2.5f, bottom - 19.5f));
            pen.LineTo(new Vector2(2.5f, bottom));
            pen.LineTo(new Vector2(22, bottom));
            pen.MoveTo(new Vector2(notch - 22, bottom));
            pen.LineTo(new Vector2(notch, bottom));
            pen.Stroke();
        }

        static void DrawHeart(Painter2D pen)
        {
            pen.strokeColor = Cyan;
            pen.lineWidth = 3.5f;
            pen.lineJoin = LineJoin.Round;
            pen.BeginPath();
            pen.MoveTo(new Vector2(29, 53));
            pen.BezierCurveTo(new Vector2(23, 47), new Vector2(3, 35), new Vector2(3, 20));
            pen.BezierCurveTo(new Vector2(3, 5), new Vector2(20, 3), new Vector2(29, 14));
            pen.BezierCurveTo(new Vector2(38, 3), new Vector2(55, 5), new Vector2(55, 20));
            pen.BezierCurveTo(new Vector2(55, 35), new Vector2(35, 47), new Vector2(29, 53));
            pen.ClosePath();
            pen.MoveTo(new Vector2(6, 30));
            pen.LineTo(new Vector2(18, 30));
            pen.LineTo(new Vector2(23, 22));
            pen.LineTo(new Vector2(30, 37));
            pen.LineTo(new Vector2(36, 29));
            pen.LineTo(new Vector2(52, 29));
            pen.Stroke();
        }

        static void DrawBolt(Painter2D pen)
        {
            pen.strokeColor = Color.white;
            pen.lineWidth = 3.5f;
            pen.lineJoin = LineJoin.Round;
            pen.BeginPath();
            pen.MoveTo(new Vector2(35, 4));
            pen.LineTo(new Vector2(15, 32));
            pen.LineTo(new Vector2(30, 32));
            pen.LineTo(new Vector2(27, 54));
            pen.LineTo(new Vector2(49, 24));
            pen.LineTo(new Vector2(34, 24));
            pen.ClosePath();
            pen.Stroke();
        }

        static void Visible(VisualElement element, bool shown) => element.EnableInClassList("hidden", !shown);
    }
}
