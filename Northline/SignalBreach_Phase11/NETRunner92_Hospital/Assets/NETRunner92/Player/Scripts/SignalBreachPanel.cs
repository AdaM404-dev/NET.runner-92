using UnityEngine;
using UnityEngine.UIElements;

namespace NETRunner92
{
    sealed class SignalBreachPanel : VisualElement
    {
        static readonly Color Trace = new(.28f, .95f, .90f);
        static readonly Color Security = new(1f, .29f, .30f);
        static readonly Color Inactive = new(.16f, .39f, .43f);
        static readonly Color Fill = new(.031f, .09f, .11f);
        readonly Label heading, timer, message, commands;
        readonly VisualElement meter, glitch;
        SignalBreachPuzzle puzzle;
        SignalBreachPuzzle.Outcome renderedOutcome;
        float visibleAt, faultAt, nextRepaint;
        int strikes;

        public string Heading => heading.text;

        public SignalBreachPanel()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.width = 640;
            style.height = 324;
            style.backgroundColor = new Color(.011f, .033f, .050f, .94f);
            LatchScreen.Box(this, 14, 10, 612, 2, Trace);
            heading = LatchScreen.Text(this, "SIGNAL BREACH", 22, 17, 465, 43, 32, Trace);
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

        public void Present(SignalBreachPuzzle next, float seconds)
        {
            float now = Time.unscaledTime;
            if (puzzle == next && next.State == renderedOutcome && next.Strikes == strikes && now < nextRepaint) return;
            nextRepaint = now + 1f / 30f;
            renderedOutcome = next.State;
            if (puzzle != next)
            {
                puzzle = next;
                visibleAt = now;
                faultAt = -10f;
                strikes = 0;
            }
            if (puzzle.Strikes != strikes)
            {
                strikes = puzzle.Strikes;
                faultAt = now;
            }
            bool successful = puzzle.State == SignalBreachPuzzle.Outcome.Successful;
            bool terminated = puzzle.State == SignalBreachPuzzle.Outcome.Terminated;
            bool fault = terminated || now - faultAt < .22f;
            bool reducedMotion = GameOptions.Active.reducedMotion;
            style.opacity = reducedMotion ? 1f : Mathf.SmoothStep(.55f, 1f, (now - visibleAt) * 8f);
            heading.text = successful ? "BREACH SUCCESSFUL" : terminated ? "CONNECTION TERMINATED" : "SIGNAL BREACH";
            heading.style.color = fault ? Security : Trace;
            timer.text = terminated ? "" : $"{Mathf.CeilToInt(seconds):00}s";
            timer.style.color = fault || !successful && seconds <= 3f ? Security : Trace;
            meter.style.width = 596f * Mathf.Clamp01(seconds / (successful ? K7Patrol.BreachDisableSeconds : SignalBreachPuzzle.Duration));
            meter.style.backgroundColor = timer.style.color.value;
            glitch.style.opacity = !reducedMotion && now - faultAt < .22f ? .65f : 0f;
            glitch.style.top = 66f + 10f * Mathf.PingPong((now - faultAt) * 24f, 1f);

            message.text = successful ? "K-7 OFFLINE  /  RECOVERY COUNTDOWN" : puzzle.Feedback.Length > 0
                ? puzzle.Feedback : $"TRACE START TO TARGET     STRIKES {puzzle.Strikes}/{SignalBreachPuzzle.StrikeLimit}";
            message.style.color = fault || puzzle.Feedback == "SECURITY NODE TRIGGERED" ? Security : Trace;
            commands.text = successful ? "ESC  CLOSE TABLET" : terminated ? "RETRY AFTER COOLDOWN" :
                "MOUSE  CLICK    ARROWS  SELECT    ENTER  LINK    BACKSPACE  UNDO";
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            if (puzzle == null) return;
            var pen = context.painter2D;
            var nodes = SignalBreachPuzzle.Nodes;
            bool tracing = puzzle.State == SignalBreachPuzzle.Outcome.Tracing;
            for (int y = 80; y <= 230; y += 25)
                Line(pen, new Vector2(22, y), new Vector2(618, y), new Color(.10f, .32f, .39f, .35f), 1);
            foreach (var edge in SignalBreachPuzzle.Edges)
            {
                bool live = tracing && (edge.From == puzzle.Current || edge.To == puzzle.Current);
                Line(pen, nodes[edge.From], nodes[edge.To], live ? LatchScreen.Muted : Inactive, live ? 2.5f : 1.5f);
            }
            for (int i = 1; i < puzzle.Path.Count; i++)
            {
                Vector2 a = nodes[puzzle.Path[i - 1]], b = nodes[puzzle.Path[i]];
                Line(pen, a, b, new Color(.18f, .63f, .62f, .6f), 8);
                Line(pen, a, b, Trace, 3);
                float progress = GameOptions.Active.reducedMotion ? .5f : Mathf.Repeat(Time.unscaledTime * 1.55f - i * .23f, 1f);
                Ring(pen, Vector2.Lerp(a, b, progress), 3, Trace, Trace, 1);
            }
            for (int i = 0; i < nodes.Length; i++)
            {
                bool dangerous = puzzle.Dangerous(i);
                Color color = dangerous ? Security : puzzle.Linked(i) || i == puzzle.Current ? Trace : Inactive;
                Vector2 center = nodes[i];
                Ring(pen, center, 15, Fill, color, 2);
                if (dangerous)
                {
                    Line(pen, center - new Vector2(5, 5), center + new Vector2(5, 5), Security, 2);
                    Line(pen, center - new Vector2(5, -5), center + new Vector2(5, -5), Security, 2);
                }
                else if (i == SignalBreachPuzzle.Target || i == 0 || i == puzzle.Current)
                    Ring(pen, center, i == 0 ? 5 : 8, i == puzzle.Current ? Trace : Fill, Trace, 2);
                else
                    Ring(pen, center, 3, color, color, 1);
                if (tracing && !puzzle.UsingMouse && i == puzzle.Focus)
                    Ring(pen, center, 22, Color.clear, color, 2);
            }
            if (tracing && puzzle.UsingMouse)
            {
                Vector2 pointer = puzzle.Pointer;
                Ring(pen, pointer, 7, Color.clear, Color.white, 1.5f);
                Line(pen, pointer + Vector2.left * 11f, pointer + Vector2.left * 7f, Color.white, 1.5f);
                Line(pen, pointer + Vector2.right * 7f, pointer + Vector2.right * 11f, Color.white, 1.5f);
            }
        }

        static void Line(Painter2D pen, Vector2 a, Vector2 b, Color color, float width)
        {
            pen.strokeColor = color;
            pen.lineWidth = width;
            pen.BeginPath();
            pen.MoveTo(a);
            pen.LineTo(b);
            pen.Stroke();
        }

        static void Ring(Painter2D pen, Vector2 center, float radius, Color fill, Color stroke, float width)
        {
            pen.fillColor = fill;
            pen.strokeColor = stroke;
            pen.lineWidth = width;
            pen.BeginPath();
            pen.Arc(center, radius, 0, 360);
            pen.ClosePath();
            if (fill.a > 0) pen.Fill();
            pen.Stroke();
        }
    }
}
