using System;
using System.Collections.Generic;

namespace NetRunner.Core
{
    /// <summary>
    /// The Signal Breach hack (Phase 11): trace a path through a small node
    /// graph from the start node (0) to the target (7) within
    /// <see cref="Duration"/> seconds. Two nodes are security nodes, picked
    /// at random from four layouts; selecting one is a strike, and
    /// <see cref="StrikeLimit"/> strikes end the breach. The player moves a
    /// pointer with the mouse and clicks a node, or steps the focus between
    /// connected nodes with the arrow keys and confirms. Coordinates are the
    /// LATCH-07 screen's pixels (640 × 324, y down). Plain maths with no scene
    /// objects; time is passed in as seconds.
    /// </summary>
    public sealed class SignalBreach
    {
        /// <summary>Where a breach stands.</summary>
        public enum Outcome
        {
            Pending,
            Tracing,
            Successful,
            Terminated,
        }

        /// <summary>Seconds to reach the target once the trace starts.</summary>
        public const float Duration = 12f;

        /// <summary>Security nodes that may be selected before the breach ends.</summary>
        public const int StrikeLimit = 2;

        public const int Start = 0;
        public const int Target = 7;

        /// <summary>Pixels from a node's centre that a click still selects it.</summary>
        public const float HitRadius = 22f;

        public const string TimeLimitExceeded = "TIME LIMIT EXCEEDED";
        public const string NotConnected = "SELECT A CONNECTED NODE";
        public const string SecurityTriggered = "SECURITY NODE TRIGGERED";

        // The area of the screen the mouse pointer stays in.
        private const float PointerLeft = 26f;
        private const float PointerRight = 614f;
        private const float PointerTop = 62f;
        private const float PointerBottom = 239f;

        // Mouse movement smaller than this (squared, in pixels) is ignored.
        private const float PointerDeadZone = .01f;

        private static readonly (float X, float Y)[] NodePoints =
        {
            (60, 153), (180, 103), (180, 207), (318, 87), (318, 217), (456, 103), (456, 207), (580, 153),
        };

        private static readonly (int From, int To)[] Links =
        {
            (0, 1), (0, 2), (1, 3), (1, 4), (2, 4), (3, 5), (4, 5), (4, 6), (5, 7), (6, 7),
        };

        // Each layout is a bit mask of its two security nodes.
        private static readonly int[] SecurityLayouts =
        {
            (1 << 4) | (1 << 6),
            (1 << 3) | (1 << 5),
            (1 << 2) | (1 << 5),
            (1 << 3) | (1 << 6),
        };

        private readonly List<int> path = new List<int> { Start };
        private readonly int security;
        private float deadline;

        /// <summary>Picks one of the four security layouts with <paramref name="random"/>.</summary>
        public SignalBreach(Random random)
        {
            security = SecurityLayouts[random.Next(SecurityLayouts.Length)];
            Pointer = NodePoints[1];
        }

        /// <summary>The node positions on the LATCH-07 screen, in pixels.</summary>
        public static IReadOnlyList<(float X, float Y)> Nodes => NodePoints;

        /// <summary>The connections between nodes; each can be traced either way.</summary>
        public static IReadOnlyList<(int From, int To)> Edges => Links;

        public Outcome State { get; private set; }

        public int Strikes { get; private set; }

        /// <summary>The node the arrow keys have selected; Enter traces to it.</summary>
        public int Focus { get; private set; } = 1;

        /// <summary>The mouse pointer on the screen, in pixels.</summary>
        public (float X, float Y) Pointer { get; private set; }

        /// <summary>Whether the player last used the mouse (the pointer shows) or the keys (the focus ring shows).</summary>
        public bool UsingMouse { get; private set; }

        /// <summary>The last message for the player, or empty.</summary>
        public string Feedback { get; private set; } = "";

        /// <summary>The traced path, from the start node to the current one.</summary>
        public IReadOnlyList<int> Path => path;

        public int Current => path[path.Count - 1];

        /// <summary>Seconds left at <paramref name="now"/>, between 0 and <see cref="Duration"/>.</summary>
        public float Remaining(float now) => Math.Clamp(deadline - now, 0f, Duration);

        public bool Dangerous(int node) => (security & (1 << node)) != 0;

        /// <summary>Starts the clock: <see cref="Duration"/> seconds from <paramref name="now"/>.</summary>
        public void Begin(float now)
        {
            if (State != Outcome.Pending)
            {
                return;
            }

            deadline = now + Duration;
            State = Outcome.Tracing;
        }

        /// <summary>Ends the breach when the time has run out.</summary>
        public void Tick(float now)
        {
            if (State == Outcome.Tracing && now >= deadline)
            {
                Terminate(TimeLimitExceeded);
            }
        }

        public void Terminate(string reason)
        {
            Feedback = reason;
            State = Outcome.Terminated;
        }

        /// <summary>Whether <paramref name="node"/> is connected to the current node.</summary>
        public bool Linked(int node)
        {
            foreach (var (from, to) in Links)
            {
                if ((from == Current && to == node) || (to == Current && from == node))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Moves the pointer by a mouse delta (y up, as the mouse reports it), kept on the screen.</summary>
        public void MovePointer(float dx, float dy)
        {
            if (dx * dx + dy * dy < PointerDeadZone)
            {
                return;
            }

            UsingMouse = true;
            Pointer = (Math.Clamp(Pointer.X + dx, PointerLeft, PointerRight), Math.Clamp(Pointer.Y - dy, PointerTop, PointerBottom));
        }

        /// <summary>Selects the node under the pointer, if any.</summary>
        public void Click()
        {
            UsingMouse = true;
            for (int i = 0; i < NodePoints.Length; i++)
            {
                float dx = NodePoints[i].X - Pointer.X;
                float dy = NodePoints[i].Y - Pointer.Y;
                if (dx * dx + dy * dy <= HitRadius * HitRadius)
                {
                    Focus = i;
                    Select(i);
                    return;
                }
            }
        }

        /// <summary>Moves the focus to the next (1) or previous (-1) node connected to the current one.</summary>
        public void StepFocus(int direction)
        {
            UsingMouse = false;
            for (int i = 1; i <= NodePoints.Length; i++)
            {
                int node = (Focus + direction * i + NodePoints.Length) % NodePoints.Length;
                if (!Linked(node))
                {
                    continue;
                }

                Focus = node;
                Pointer = NodePoints[node];
                return;
            }
        }

        /// <summary>Steps back one node along the path.</summary>
        public void Undo()
        {
            if (path.Count == 1 || State != Outcome.Tracing)
            {
                return;
            }

            int previous = Current;
            path.RemoveAt(path.Count - 1);
            Focus = previous;
            Feedback = "";
        }

        /// <summary>Traces to the focused node.</summary>
        public void Confirm()
        {
            UsingMouse = false;
            Select(Focus);
        }

        private void Select(int node)
        {
            if (State != Outcome.Tracing || node == Current)
            {
                return;
            }

            if (!Linked(node))
            {
                Feedback = NotConnected;
                return;
            }

            if (Dangerous(node))
            {
                Strikes++;
                Feedback = SecurityTriggered;
                if (Strikes >= StrikeLimit)
                {
                    Terminate(Feedback);
                }

                return;
            }

            // Selecting a node already on the path cuts the path back to it.
            int existing = path.IndexOf(node);
            if (existing >= 0)
            {
                path.RemoveRange(existing + 1, path.Count - existing - 1);
            }
            else
            {
                path.Add(node);
            }

            Feedback = "";
            if (node == Target)
            {
                State = Outcome.Successful;
            }
            else if (!UsingMouse)
            {
                // Focus the next connected node ahead of the trace, with reverse links still selectable.
                StepFocus(1);
            }
        }
    }
}
