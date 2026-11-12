using System.Collections.Generic;
using System.Globalization;
using NetRunner.Core;
using NetRunner.Player;
using NetRunner.Settings;
using NetRunner.World;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace NetRunner.Hud
{
    /// <summary>
    /// The hospital's on-screen display (Phase 11): the objective card, health
    /// and stamina bars, the interaction prompt with the player's own key, the
    /// reception terminal's cells, the CCTV channel markers, a red flash when
    /// hit and the defeat notice. It reads the player's interaction, LATCH-07,
    /// the CCTV network and the lift each frame, and follows the HUD settings
    /// (opacity, contrast, prompts, objectives). The layout is in
    /// <c>UI/HospitalHud.uxml</c>; the hospital's HUD installer places it.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HospitalHud : MonoBehaviour
    {
        private const int VitalSegments = 18;
        private const float FullHealth = 100f;

        // The red flash fades over 0.42 s; reduced motion keeps it faint. While down, the screen stays red.
        private const float HitFlashSeconds = .42f;
        private const float HitFlashStrength = .36f;
        private const float ReducedHitFlashStrength = .15f;
        private const float DownTint = .58f;
        private const int DefeatFontSize = 22;

        // Objectives: crack the CCTV, reach the top floor (level 4, index 3), explore the research wing.
        private const int GoalCctv = 0;
        private const int GoalClimb = 1;
        private const int GoalExplore = 2;
        private const int TopFloor = 3;

        private const string Interaction = "INTERAKCIÓ";

        private static readonly Color Cyan = new Color32(0, 255, 234, 255);
        private static readonly Color PanelInk = new Color(29f / 255f, 29f / 255f, 29f / 255f, .44f);
        private static readonly Color ContrastInk = new Color(.015f, .015f, .015f, .88f);
        private static readonly Color HitColor = new Color(.50f, .018f, .025f);
        private static readonly string[] Digits = { "", "01", "02", "03", "04" };
        private static readonly CultureInfo Hungarian = CultureInfo.GetCultureInfo("hu-HU");

        [SerializeField] private HospitalInteraction interaction;
        [SerializeField] private LatchViewmodel latch;
        [SerializeField] private SecurityCctv network;
        [SerializeField] private HospitalLift lift;

        private readonly List<VisualElement> channels = new List<VisualElement>();
        private readonly List<Label> routeCells = new List<Label>();
        private UIDocument document;
        private FirstPersonController player;
        private Preferences preferences;
        private PanelSettings runtimePanel;
        private VisualElement root;
        private VisualElement world;
        private VisualElement terminal;
        private VisualElement viewer;
        private VisualElement goal;
        private VisualElement goalHistory;
        private VisualElement prompt;
        private VisualElement notice;
        private VisualElement healthBar;
        private VisualElement hitOverlay;
        private Label defeat;
        private Label goalTitle;
        private Label goalDetail;
        private Label promptKey;
        private Label promptText;
        private Label terminalStatus;
        private int currentGoal = -1;

        private void Awake()
        {
            document = GetComponent<UIDocument>();
            runtimePanel = Instantiate(document.panelSettings);
            document.panelSettings = runtimePanel;
            player = interaction.GetComponent<FirstPersonController>();
            preferences = player.Preferences;
        }

        private void OnEnable()
        {
            preferences.Changed += ApplyAppearance;
        }

        private void LateUpdate()
        {
            var tree = document.rootVisualElement;
            if (tree == null)
            {
                return;
            }

            if (root != tree)
            {
                Bind(tree);
            }

            UpdateHealth();
            bool captured = Cursor.lockState == CursorLockMode.Locked;
            var activeTerminal = interaction.ActiveTerminal as ReceptionSecurityTerminal;
            bool inTerminal = captured && activeTerminal;
            bool inCctv = captured && network.Watching;
            bool exploring = captured && !inTerminal && !latch.Active && preferences.Active.ShowHud;
            Visible(world, exploring);
            Visible(terminal, inTerminal);
            Visible(viewer, inCctv);
            Visible(notice, captured && !inTerminal && !inCctv && !network.Unlocked
                && latch.CctvHintVisible && interaction.Target is not ReceptionSecurityTerminal);
            if (inTerminal)
            {
                ShowTerminal(activeTerminal);
            }
            else if (inCctv)
            {
                for (int i = 0; i < channels.Count; i++)
                {
                    channels[i].EnableInClassList("selected", i == network.Selected);
                }
            }
            else if (exploring)
            {
                RefreshExploration();
            }
        }

        private void OnDisable()
        {
            preferences.Changed -= ApplyAppearance;
        }

        private void OnDestroy()
        {
            if (runtimePanel)
            {
                Destroy(runtimePanel);
            }
        }

        /// <summary>Sets the HUD up from the hospital's HUD installer.</summary>
        public void Configure(HospitalInteraction input, LatchViewmodel tablet, SecurityCctv cameras, HospitalLift elevator)
        {
            interaction = input;
            latch = tablet;
            network = cameras;
            lift = elevator;
        }

        private static void PopulateBar(VisualElement bar)
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

        private static void DrawHeart(Painter2D pen)
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

        private static void DrawBolt(Painter2D pen)
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

        private static void Visible(VisualElement element, bool shown) => element.EnableInClassList("hidden", !shown);

        private void ApplyAppearance()
        {
            var options = preferences.Active;
            if (runtimePanel)
            {
                runtimePanel.scale = options.UiScale;
            }

            if (root == null)
            {
                return;
            }

            world.style.opacity = options.HudOpacity;
            root.EnableInClassList("hud-contrast", options.UseHighContrast);
            Visible(goal, options.ShowObjectives);
            root.Q("goal-shape").MarkDirtyRepaint();
            root.Q<Label>("terminal-exit-key").text = preferences.Label(GameAction.Interact) + " / Esc";
            root.Q<Label>("viewer-exit-key").text = preferences.Label(GameAction.Cctv);
            root.Q<Label>("viewer-stow-key").text = preferences.Label(GameAction.Stow);
        }

        private void Bind(VisualElement tree)
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
            promptKey = root.Q<Label>("prompt-key");
            promptText = root.Q<Label>("prompt-text");
            terminalStatus = root.Q<Label>("terminal-status");

            var goalShape = root.Q("goal-shape");
            goalShape.generateVisualContent += context => DrawGoal(context.painter2D, goalShape.contentRect.size);
            root.Q("health-icon").generateVisualContent += context => DrawHeart(context.painter2D);
            root.Q("stamina-icon").generateVisualContent += context => DrawBolt(context.painter2D);
            healthBar = root.Q("health-bar");
            PopulateBar(healthBar);
            PopulateBar(root.Q("stamina-bar"));
            BuildHitOverlay();
            BuildTerminalCells();
            BuildChannels();
            currentGoal = -1;
            ApplyAppearance();
        }

        private void BuildHitOverlay()
        {
            hitOverlay = new VisualElement { pickingMode = PickingMode.Ignore };
            hitOverlay.style.position = Position.Absolute;
            hitOverlay.style.left = 0;
            hitOverlay.style.right = 0;
            hitOverlay.style.top = 0;
            hitOverlay.style.bottom = 0;
            hitOverlay.style.alignItems = Align.Center;
            hitOverlay.style.justifyContent = Justify.Center;
            defeat = new Label("HARCKÉPTELEN  /  ELLENŐRZŐPONT BETÖLTÉSE") { pickingMode = PickingMode.Ignore };
            defeat.style.color = Color.white;
            defeat.style.fontSize = DefeatFontSize;
            hitOverlay.Add(defeat);
            root.Add(hitOverlay);
        }

        private void BuildTerminalCells()
        {
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
        }

        private void BuildChannels()
        {
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
        }

        private void ShowTerminal(ReceptionSecurityTerminal activeTerminal)
        {
            for (int i = 0; i < routeCells.Count; i++)
            {
                routeCells[i].text = Digits[activeTerminal.RouteCell(i)];
                routeCells[i].EnableInClassList("selected", i == activeTerminal.SelectedCell);
            }

            terminalStatus.text = activeTerminal.Mismatch ? "A jel eltér. Ellenőrizd a három cellát." : "Hozzáférés zárolva";
            terminalStatus.EnableInClassList("fault", activeTerminal.Mismatch);
        }

        private void UpdateHealth()
        {
            int lit = Mathf.CeilToInt(player.Health * VitalSegments / FullHealth);
            for (int i = 0; i < VitalSegments; i++)
            {
                healthBar[i].EnableInClassList("depleted", i >= lit);
            }

            float flash = Mathf.Clamp01(1f - (Time.unscaledTime - player.LastHitTime) / HitFlashSeconds);
            float strength = preferences.Active.ReduceMotion ? ReducedHitFlashStrength : HitFlashStrength;
            float intensity = player.IsDown ? DownTint : flash * strength;
            hitOverlay.style.backgroundColor = new Color(HitColor.r, HitColor.g, HitColor.b, intensity);
            defeat.style.display = player.IsDown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshExploration()
        {
            int next = !network.Unlocked ? GoalCctv : lift && lift.Unlocked(TopFloor) ? GoalExplore : GoalClimb;
            if (currentGoal != next)
            {
                currentGoal = next;
                goalTitle.text = next switch
                {
                    GoalCctv => "Törd fel a CCTV-kamerákat",
                    GoalClimb => "Juss fel az emeletre",
                    _ => "Derítsd fel a kutatást",
                };
                goalDetail.text = next == GoalExplore ? "Feljutottál az emeletre" : "CCTV-kamerák feltörve";
                Visible(goalHistory, next > GoalCctv);
            }

            var target = interaction.Target;
            var (action, text) = target != null ? target.Prompt : (null, null);
            bool breachable = BreachTargets.Alive(interaction.BreachTarget);
            if (breachable)
            {
                action = GameAction.Tablet;
                int retry = latch.BreachRetrySeconds;
                text = retry > 0 ? $"SIGNAL BREACH  /  RETRY {retry:00}s" : "SIGNAL BREACH";
            }

            Visible(prompt, action.HasValue && preferences.Active.ShowPrompts);
            if (!action.HasValue)
            {
                return;
            }

            promptKey.text = preferences.Label(action.Value);
            promptText.text = !breachable && action == GameAction.Interact ? Interaction : text.ToUpper(Hungarian);
        }

        private void DrawGoal(Painter2D pen, Vector2 size)
        {
            // An angled card: the bottom edge stops at 70 % of the width, with white corner brackets.
            const float Edge = 2.5f;
            const float Bracket = 22f;
            float right = size.x - Edge;
            float bottom = size.y - Edge;
            float notch = size.x * .70f;
            pen.fillColor = preferences.Active.UseHighContrast ? ContrastInk : PanelInk;
            pen.BeginPath();
            pen.MoveTo(new Vector2(Edge, Edge));
            pen.LineTo(new Vector2(right, Edge));
            pen.LineTo(new Vector2(notch, bottom));
            pen.LineTo(new Vector2(Edge, bottom));
            pen.ClosePath();
            pen.Fill();

            pen.strokeColor = Color.white;
            pen.lineWidth = 5;
            pen.BeginPath();
            pen.MoveTo(new Vector2(Edge, Bracket));
            pen.LineTo(new Vector2(Edge, Edge));
            pen.LineTo(new Vector2(Bracket, Edge));
            pen.MoveTo(new Vector2(right - Bracket, Edge));
            pen.LineTo(new Vector2(right, Edge));
            pen.MoveTo(new Vector2(Edge, bottom - 19.5f));
            pen.LineTo(new Vector2(Edge, bottom));
            pen.LineTo(new Vector2(Bracket, bottom));
            pen.MoveTo(new Vector2(notch - Bracket, bottom));
            pen.LineTo(new Vector2(notch, bottom));
            pen.Stroke();
        }
    }
}
