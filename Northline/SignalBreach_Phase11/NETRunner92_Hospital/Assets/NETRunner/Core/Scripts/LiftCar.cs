using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The west lift's car: which floor it is at, where it is going and what
    /// it is doing (<see cref="State"/>). It opens and shuts the doors of its
    /// floor through <see cref="DoorsCommanded"/>, keeps them open for
    /// <see cref="OpenSeconds"/> (counting again while someone stands in the
    /// doorway), and travels at up to <see cref="TopSpeed"/>, gaining and
    /// shedding speed at <see cref="Acceleration"/> and never slower than
    /// <see cref="CreepSpeed"/>. Only floors the stairs have reached can be
    /// chosen. The component moves the doors and the cabin and tells the car
    /// when the doors are fully open or shut. Plain C# (see LiftCarTests).
    /// </summary>
    public sealed class LiftCar
    {
        /// <summary>What the car is doing.</summary>
        public enum State
        {
            /// <summary>Standing with its doors shut.</summary>
            Closed,

            /// <summary>Opening the doors of its floor.</summary>
            Opening,

            /// <summary>Standing with its doors open.</summary>
            Open,

            /// <summary>Shutting its doors, before it leaves or to stand closed.</summary>
            Closing,

            /// <summary>Travelling to its destination.</summary>
            Moving,
        }

        /// <summary>Seconds the doors stay open.</summary>
        public const float OpenSeconds = 7f;

        /// <summary>The fastest travel, in metres per second.</summary>
        public const float TopSpeed = 2.25f;

        /// <summary>How fast the car gains or sheds speed, in metres per second, per second.</summary>
        public const float Acceleration = 2.1f;

        /// <summary>The slowest travel, so the car always arrives.</summary>
        public const float CreepSpeed = .15f;

        // The cabin floor in the cabin's own frame: 1.28 m either side, from
        // 1.34 m behind to 1.52 m in front of its centre, within 0.22 m in height.
        private const float CabinHalfWidth = 1.28f;
        private const float CabinBack = -1.34f;
        private const float CabinFront = 1.52f;
        private const float CabinStep = .22f;

        private float openFor;
        private float travelSpeed;

        /// <summary>The doors of the car's floor are to open (true) or shut (false).</summary>
        public event Action<bool> DoorsCommanded;

        /// <summary>What the car is doing; it starts closed.</summary>
        public State Current { get; private set; }

        /// <summary>The floor the car is at or last left.</summary>
        public int Floor { get; private set; }

        /// <summary>The floor the car is going to.</summary>
        public int Destination { get; private set; }

        /// <summary>Whether the car is shutting its doors or travelling, and takes no requests.</summary>
        public bool Busy => Current == State.Moving || Current == State.Closing;

        /// <summary>Whether <paramref name="floor"/> can be chosen once the stairs have reached <paramref name="highestReached"/>.</summary>
        public static bool IsReached(int floor, int highestReached)
        {
            return floor >= 0 && floor <= highestReached;
        }

        /// <summary>
        /// The prompt of a lift button for <paramref name="floor"/> (0-based):
        /// a floor the stairs have not reached says so, a busy lift says it
        /// is in transit, otherwise E selects or calls.
        /// </summary>
        public static string ButtonPrompt(int floor, bool reached, bool busy, bool inCabin)
        {
            int level = floor + 1;
            if (!reached)
            {
                return $"L{level}  Reach this level by stairs";
            }

            if (busy)
            {
                return "Lift in transit";
            }

            return inCabin ? $"E  Select level {level}" : $"E  Call lift to level {level}";
        }

        /// <summary>Whether a point at (x, y, z) in the cabin's frame stands on the cabin floor.</summary>
        public static bool Carries(float x, float y, float z)
        {
            return Math.Abs(x) < CabinHalfWidth && z > CabinBack && z < CabinFront && Math.Abs(y) < CabinStep;
        }

        /// <summary>
        /// A call button (<paramref name="fromCabin"/> false) or a cabin button
        /// asks for <paramref name="target"/>. To the car's own floor it opens
        /// the doors; to another it shuts them and then leaves, unless the
        /// doors are still opening or someone stands in the doorway. A cabin
        /// button works only for a passenger. Returns whether it was accepted.
        /// </summary>
        public bool Request(int target, int highestReached, bool fromCabin, bool passengerInside, bool doorwayBlocked)
        {
            if (!IsReached(target, highestReached) || Busy || (fromCabin && !passengerInside))
            {
                return false;
            }

            if (target == Floor)
            {
                OpenDoors();
                return true;
            }

            if (Current == State.Opening || doorwayBlocked)
            {
                return false;
            }

            Destination = target;
            ShutDoors();
            return true;
        }

        /// <summary>Someone stepped into the doorway while the doors shut: they open again and the car stays.</summary>
        public void Reopen()
        {
            Destination = Floor;
            OpenDoors();
        }

        /// <summary>
        /// The doors moved this frame: once fully open the car stands open;
        /// once fully shut it leaves for its destination, or stands closed.
        /// </summary>
        public void DoorsMoved(bool allOpen, bool allShut)
        {
            if (Current == State.Opening && allOpen)
            {
                Current = State.Open;
                openFor = OpenSeconds;
            }
            else if (Current == State.Closing && allShut)
            {
                Current = Destination == Floor ? State.Closed : State.Moving;
            }
        }

        /// <summary>
        /// The car stands open for <paramref name="dt"/> more seconds; someone
        /// in the doorway starts the wait again. When it runs out the doors shut.
        /// </summary>
        public void Wait(float dt, bool doorwayBlocked)
        {
            if (doorwayBlocked)
            {
                openFor = OpenSeconds;
            }

            openFor -= dt;
            if (openFor <= 0f)
            {
                Destination = Floor;
                ShutDoors();
            }
        }

        /// <summary>
        /// Travels for <paramref name="dt"/> seconds from <paramref name="height"/>
        /// (metres) and returns the new height. On arrival the car is at its
        /// destination and opens the doors.
        /// </summary>
        public float Travel(float height, float dt)
        {
            float target = Destination * StairRules.FloorHeight;
            float distance = Math.Abs(target - height);
            float braking = 2f * Acceleration * distance;
            float speed = Math.Min(TopSpeed, (float)Math.Sqrt(braking));
            travelSpeed = Scalar.MoveTowards(travelSpeed, speed, dt * Acceleration);
            float next = Scalar.MoveTowards(height, target, Math.Max(CreepSpeed, travelSpeed) * dt);
            if (next == target)
            {
                Floor = Destination;
                travelSpeed = 0f;
                OpenDoors();
            }

            return next;
        }

        private void OpenDoors()
        {
            DoorsCommanded?.Invoke(true);
            Current = State.Opening;
        }

        private void ShutDoors()
        {
            DoorsCommanded?.Invoke(false);
            Current = State.Closing;
        }
    }
}
