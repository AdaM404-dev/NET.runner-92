using NetRunner.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace NetRunner.Player
{
    /// <summary>
    /// The Signal Breach page of the LATCH-07 screen (Phase 11): the node
    /// graph, the traced path with moving pulses, the security nodes, the
    /// timer and its meter, the messages and the controls. It draws a
    /// <see cref="SignalBreach"/>; <see cref="LatchScreen"/> shows it and
    /// <see cref="LatchViewmodel"/> calls <see cref="Present"/> every frame.
    /// Layout numbers are pixels of the 640 × 324 tablet screen.
    /// </summary>
    public sealed class SignalBreachPanel : VisualElement
    {
        private const string Title = "SIGNAL BREACH";
        private const string Success = "BREACH SUCCESSFUL";
        private const string Terminated = "CONNECTION TERMINATED";
        private const float ScreenWidth = 640f;
        private const float ScreenHeight = 324f;
        private const float MeterWidth = 596f;

        // Repaint at most 30 times a second unless something changed.
        private const float RepaintSeconds = 1f / 30f;

        // A fault (a strike or the end) flashes red for this long, with a glitch line jumping 10 px at 24 Hz.
        private const float FaultSeconds = .22f;
        private const float GlitchTop = 66f;
        private const float GlitchTravel = 10f;
        private const float GlitchRate = 24f;
        private const float GlitchOpacity = .65f;

        // Before the first strike the last fault lies far enough back that nothing flashes.
        private const float NoFault = -10f;

        // The panel fades in from 55 % over about an eighth of a second.
        private const float FadeFrom = .55f;
        private const float FadeRate = 8f;

        // The timer turns red in the last three seconds.
        private const float WarningSeconds = 3f;

        // Pulses run along the path at 1.55 lengths a second, each segment a little behind the previous one.
        private const float PulseRate = 1.55f;
        private const float PulseSpacing = .23f;

        private static readonly Color Trace = new Color(.28f, .95f, .90f);
        private static readonly Color Security = new Color(1f, .29f, .30f);
        private static readonly Color Inactive = new Color(.16f, .39f, .43f);
        private static readonly Color Fill = new Color(.031f, .09f, .11f);
        private static readonly Color Background = new Color(.011f, .033f, .050f, .94f);
        private static readonly Color Grid = new Color(.10f, .32f, .39f, .35f);
        private static readonly Color PathGlow = new Color(.18f, .63f, .62f, .6f);

        private readonly Label heading;
        private readonly Label timer;
        private readonly Label message;
        private readonly Label commands;
        private readonly VisualElement meter;
        private readonly VisualElement glitch;
        private SignalBreach puzzle;
        private SignalBreach.Outcome renderedOutcome;
        private float visibleAt;
        private float faultAt;
        private float nextRepaint;
        private int strikes;
        private bool reduceMotion;

        public SignalBreachPanel()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.width = ScreenWidth;
            style.height = ScreenHeight;
            style.backgroundColor = Background;
            LatchScreen.Box(this, 14, 10, 612, 2, Trace);
            heading = LatchScreen.Text(this, Title, 22, 17, 465, 43, 32, Trace);
            timer = LatchScreen.Text(this, "", 486, 17, 130, 43, 29, Trace);
            timer.style.unityTextAlign = TextAnchor.MiddleRight;
            LatchScreen.Box(this, 22, 59, 596, 2, Inactive);
            meter = LatchScreen.Box(this, 22, 59, 596, 2, Trace);
            message = LatchScreen.Text(this, "", 22, 244, 596, 33, 23, Trace);
            commands = LatchScreen.Text(this, "", 22, 279, 596, 28, 17, LatchScreen.Muted);
            LatchScreen.Text(this, "START", 22, 175, 76, 24, 17, Trace).style.unityTextAlign = TextAnchor.MiddleCenter;
            LatchScreen.Text(this, "TARGET", 542, 175, 76, 24, 17, Trace).style.unityTextAlign = TextAnchor.MiddleCenter;
            glitch = LatchScreen.Box(this, 22, 67, 596, 2, Security);
            glitch.style.opacity = 0;
            generateVisualContent += Draw;
        }

        /// <summary>The heading shown, which LATCH-07 reports as the screen's status.</summary>
        public string Heading => heading.text;

        /// <summary>
        /// Shows <paramref name="next"/> with <paramref name="seconds"/> on the
        /// timer; after a success the meter counts down from
        /// <paramref name="disableSeconds"/>. Reduced motion stops the fade,
        /// the glitch and the moving pulses.
        /// </summary>
        public void Present(SignalBreach next, float seconds, float disableSeconds, bool reducedMotion)
        {
            float now = Time.unscaledTime;
            if (puzzle == next && next.State == renderedOutcome && next.Strikes == strikes && now < nextRepaint)
            {
                return;
            }

            reduceMotion = reducedMotion;
            nextRepaint = now + RepaintSeconds;
            renderedOutcome = next.State;
            if (puzzle != next)
            {
                puzzle = next;
                visibleAt = now;
                faultAt = NoFault;
                strikes = 0;
            }

            if (puzzle.Strikes != strikes)
            {
                strikes = puzzle.Strikes;
                faultAt = now;
            }

            bool successful = puzzle.State == SignalBreach.Outcome.Successful;
            bool terminated = puzzle.State == SignalBreach.Outcome.Terminated;
            bool faultFlash = now - faultAt < FaultSeconds;
            bool fault = terminated || faultFlash;
            style.opacity = reduceMotion ? 1f : Mathf.SmoothStep(FadeFrom, 1f, (now - visibleAt) * FadeRate);
            heading.text = successful ? Success : terminated ? Terminated : Title;
            heading.style.color = fault ? Security : Trace;
            timer.text = terminated ? "" : $"{Mathf.CeilToInt(seconds):00}s";
            timer.style.color = fault || (!successful && seconds <= WarningSeconds) ? Security : Trace;
            meter.style.width = MeterWidth * Mathf.Clamp01(seconds / (successful ? disableSeconds : SignalBreach.Duration));
            meter.style.backgroundColor = timer.style.color.value;
            glitch.style.opacity = !reduceMotion && faultFlash ? GlitchOpacity : 0f;
            glitch.style.top = GlitchTop + GlitchTravel * Mathf.PingPong((now - faultAt) * GlitchRate, 1f);
            message.text = successful ? "K-7 OFFLINE  /  RECOVERY COUNTDOWN"
                : puzzle.Feedback.Length > 0 ? puzzle.Feedback
                : $"TRACE START TO TARGET     STRIKES {puzzle.Strikes}/{SignalBreach.StrikeLimit}";
            message.style.color = fault || puzzle.Feedback == SignalBreach.SecurityTriggered ? Security : Trace;
            commands.text = successful ? "ESC  CLOSE TABLET"
                : terminated ? "RETRY AFTER COOLDOWN"
                : "MOUSE  CLICK    ARROWS  SELECT    ENTER  LINK    BACKSPACE  UNDO";
            MarkDirtyRepaint();
        }

        private static Vector2 Point((float X, float Y) node) => new Vector2(node.X, node.Y);

        private static void Line(Painter2D pen, Vector2 a, Vector2 b, Color color, float width)
        {
            pen.strokeColor = color;
            pen.lineWidth = width;
            pen.BeginPath();
            pen.MoveTo(a);
            pen.LineTo(b);
            pen.Stroke();
        }

        private static void Ring(Painter2D pen, Vector2 center, float radius, Color fill, Color stroke, float width)
        {
            pen.fillColor = fill;
            pen.strokeColor = stroke;
            pen.lineWidth = width;
            pen.BeginPath();
            pen.Arc(center, radius, 0, 360);
            pen.ClosePath();
            if (fill.a > 0)
            {
                pen.Fill();
            }

            pen.Stroke();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (puzzle == null)
            {
                return;
            }

            var pen = context.painter2D;
            bool tracing = puzzle.State == SignalBreach.Outcome.Tracing;
            for (int y = 80; y <= 230; y += 25)
            {
                Line(pen, new Vector2(22, y), new Vector2(618, y), Grid, 1);
            }

            DrawEdges(pen, tracing);
            DrawPath(pen);
            DrawNodes(pen, tracing);
            if (tracing && puzzle.UsingMouse)
            {
                DrawPointer(pen, Point(puzzle.Pointer));
            }
        }

        private void DrawEdges(Painter2D pen, bool tracing)
        {
            var nodes = SignalBreach.Nodes;
            foreach (var (from, to) in SignalBreach.Edges)
            {
                bool live = tracing && (from == puzzle.Current || to == puzzle.Current);
                Line(pen, Point(nodes[from]), Point(nodes[to]), live ? LatchScreen.Muted : Inactive, live ? 2.5f : 1.5f);
            }
        }

        private void DrawPath(Painter2D pen)
        {
            var nodes = SignalBreach.Nodes;
            for (int i = 1; i < puzzle.Path.Count; i++)
            {
                Vector2 a = Point(nodes[puzzle.Path[i - 1]]);
                Vector2 b = Point(nodes[puzzle.Path[i]]);
                Line(pen, a, b, PathGlow, 8);
                Line(pen, a, b, Trace, 3);
                float progress = reduceMotion ? .5f : Mathf.Repeat(Time.unscaledTime * PulseRate - i * PulseSpacing, 1f);
                Ring(pen, Vector2.Lerp(a, b, progress), 3, Trace, Trace, 1);
            }
        }

        private void DrawNodes(Painter2D pen, bool tracing)
        {
            var nodes = SignalBreach.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                bool dangerous = puzzle.Dangerous(i);
                Color color = dangerous ? Security : puzzle.Linked(i) || i == puzzle.Current ? Trace : Inactive;
                Vector2 center = Point(nodes[i]);
                Ring(pen, center, 15, Fill, color, 2);
                if (dangerous)
                {
                    Line(pen, center - new Vector2(5, 5), center + new Vector2(5, 5), Security, 2);
                    Line(pen, center - new Vector2(5, -5), center + new Vector2(5, -5), Security, 2);
                }
                else if (i == SignalBreach.Target || i == SignalBreach.Start || i == puzzle.Current)
                {
                    Ring(pen, center, i == SignalBreach.Start ? 5 : 8, i == puzzle.Current ? Trace : Fill, Trace, 2);
                }
                else
                {
                    Ring(pen, center, 3, color, color, 1);
                }

                if (tracing && !puzzle.UsingMouse && i == puzzle.Focus)
                {
                    Ring(pen, center, 22, Color.clear, color, 2);
                }
            }
        }

        private static void DrawPointer(Painter2D pen, Vector2 pointer)
        {
            Ring(pen, pointer, 7, Color.clear, Color.white, 1.5f);
            Line(pen, pointer + Vector2.left * 11f, pointer + Vector2.left * 7f, Color.white, 1.5f);
            Line(pen, pointer + Vector2.right * 7f, pointer + Vector2.right * 11f, Color.white, 1.5f);
        }
    }
}
