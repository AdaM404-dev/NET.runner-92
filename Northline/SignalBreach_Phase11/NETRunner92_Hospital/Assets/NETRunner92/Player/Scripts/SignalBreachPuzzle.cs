using System.Collections.Generic;
using UnityEngine;

namespace NETRunner92
{
    sealed class SignalBreachPuzzle
    {
        public const float Duration = 12f;
        public const int StrikeLimit = 2;
        public const int Target = 7;
        public const float HitRadius = 22f;

        public static readonly Vector2[] Nodes =
        {
            new(60, 153), new(180, 103), new(180, 207), new(318, 87),
            new(318, 217), new(456, 103), new(456, 207), new(580, 153)
        };
        public static readonly (int From, int To)[] Edges =
        {
            (0, 1), (0, 2), (1, 3), (1, 4), (2, 4), (3, 5), (4, 5), (4, 6), (5, 7), (6, 7)
        };
        static readonly int[] SecurityLayouts =
        {
            (1 << 4) | (1 << 6), (1 << 3) | (1 << 5), (1 << 2) | (1 << 5), (1 << 3) | (1 << 6)
        };

        public enum Outcome { Pending, Tracing, Successful, Terminated }
        readonly List<int> path = new() { 0 };
        readonly int security = SecurityLayouts[Random.Range(0, SecurityLayouts.Length)];
        float deadline;

        public Outcome State { get; private set; }
        public int Strikes { get; private set; }
        public int Focus { get; private set; } = 1;
        public Vector2 Pointer { get; private set; } = Nodes[1];
        public bool UsingMouse { get; private set; }
        public string Feedback { get; private set; } = "";
        public IReadOnlyList<int> Path => path;
        public int Current => path[^1];

        public float Remaining(float now) => Mathf.Clamp(deadline - now, 0f, Duration);
        public bool Dangerous(int node) => (security & (1 << node)) != 0;

        public void Start(float now)
        {
            if (State != Outcome.Pending) return;
            deadline = now + Duration;
            State = Outcome.Tracing;
        }

        public void Tick(float now)
        {
            if (State == Outcome.Tracing && now >= deadline) Terminate("TIME LIMIT EXCEEDED");
        }

        public void Terminate(string reason)
        {
            Feedback = reason;
            State = Outcome.Terminated;
        }

        public bool Linked(int node)
        {
            foreach (var edge in Edges)
                if ((edge.From == Current && edge.To == node) || (edge.To == Current && edge.From == node))
                    return true;
            return false;
        }

        public void MovePointer(Vector2 delta)
        {
            if (delta.sqrMagnitude < .01f) return;
            UsingMouse = true;
            Pointer = new Vector2(Mathf.Clamp(Pointer.x + delta.x, 26f, 614f),
                Mathf.Clamp(Pointer.y - delta.y, 62f, 239f));
        }

        public void Click()
        {
            UsingMouse = true;
            for (int i = 0; i < Nodes.Length; i++)
                if ((Nodes[i] - Pointer).sqrMagnitude <= HitRadius * HitRadius)
                {
                    Focus = i;
                    Select(i);
                    return;
                }
        }

        public void StepFocus(int direction)
        {
            UsingMouse = false;
            for (int i = 1; i <= Nodes.Length; i++)
            {
                int node = (Focus + direction * i + Nodes.Length) % Nodes.Length;
                if (!Linked(node)) continue;
                Focus = node;
                Pointer = Nodes[node];
                return;
            }
        }

        public void Undo()
        {
            if (path.Count == 1 || State != Outcome.Tracing) return;
            int previous = Current;
            path.RemoveAt(path.Count - 1);
            Focus = previous;
            Feedback = "";
        }

        public void Confirm()
        {
            UsingMouse = false;
            Select(Focus);
        }

        void Select(int node)
        {
            if (State != Outcome.Tracing || node == Current) return;
            if (!Linked(node))
            {
                Feedback = "SELECT A CONNECTED NODE";
                return;
            }
            if (Dangerous(node))
            {
                Strikes++;
                Feedback = "SECURITY NODE TRIGGERED";
                if (Strikes >= StrikeLimit) Terminate(Feedback);
                return;
            }
            int existing = path.IndexOf(node);
            if (existing >= 0) path.RemoveRange(existing + 1, path.Count - existing - 1);
            else path.Add(node);
            Feedback = "";
            if (node == Target) State = Outcome.Successful;
            else if (!UsingMouse)
            {
                // Focus the next connected node ahead of the trace, with reverse links still selectable.
                StepFocus(1);
            }
        }
    }
}
