using System;
using System.Collections.Generic;

namespace NetRunner.Core
{
    /// <summary>
    /// A guard walking back and forth along a route: it turns towards the
    /// next point, walks there once it roughly faces it, and at either end
    /// of the route waits and turns round. Positions are points on the floor
    /// in metres (the scene's x and z; height is left to the caller).
    /// Headings are degrees clockwise from +z, like a Y rotation in Unity:
    /// 0 faces +z, 90 faces +x. Plain maths with no scene objects, so it is
    /// tested without starting the game (see PatrolTests).
    /// </summary>
    public sealed class Patrol
    {
        /// <summary>How fast it turns, in degrees per second.</summary>
        public const float TurnSpeed = 115f;

        /// <summary>It walks only while it faces the next point to within this many degrees.</summary>
        public const float WalkingAngle = 25f;

        /// <summary>How fast it turns while it faces a target it attacks, in degrees per second (Phase 11).</summary>
        public const float AttackTurnSpeed = 205f;

        /// <summary>Seconds it waits at either end of the route before walking back.</summary>
        public const float EndPause = 1.15f;

        /// <summary>Metres: this close to a point counts as having reached it.</summary>
        public const float ArrivalDistance = 0.01f;

        private const float DegreesPerRadian = 180f / MathF.PI;

        // How far past 180° a turn still counts as exactly half a turn, to absorb rounding.
        private const float HalfTurnTolerance = 0.01f;

        private readonly (float X, float Z)[] points;
        private readonly float speed;
        private int next = 1;
        private int direction = 1;
        private float pause;

        /// <summary>Starts on the first point of the route, facing the second.</summary>
        /// <param name="points">The route: at least two points, walked first to last and back.</param>
        /// <param name="speed">Walking speed in metres per second.</param>
        public Patrol(IReadOnlyList<(float X, float Z)> points, float speed)
        {
            if (points == null || points.Count < 2)
            {
                throw new ArgumentException("A patrol route needs at least two points.", nameof(points));
            }

            this.points = new (float X, float Z)[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                this.points[i] = points[i];
            }

            this.speed = speed;
            Position = this.points[0];
            Heading = Normalize(HeadingFrom(this.points[0], this.points[1]));
        }

        /// <summary>Where it stands, in metres on the floor.</summary>
        public (float X, float Z) Position { get; private set; }

        /// <summary>The way it faces, in degrees above -180 and up to 180.</summary>
        public float Heading { get; private set; }

        /// <summary>Whether it walked in the last step; false while it waits or turns on the spot.</summary>
        public bool IsWalking { get; private set; }

        /// <summary>Metres walked since it started; the walk animation follows it.</summary>
        public float DistanceWalked { get; private set; }

        /// <summary>
        /// Puts it somewhere else without changing where it is going: from
        /// there it turns and walks to the same next point.
        /// </summary>
        public void Place((float X, float Z) position, float heading)
        {
            Position = position;
            Heading = Normalize(heading);
        }

        /// <summary>
        /// Moves it on by one frame of <paramref name="dt"/> seconds: it turns
        /// towards the next point, counts down any wait, then walks if it
        /// faces the point closely enough.
        /// </summary>
        public void Step(float dt)
        {
            Step(dt, null, false);
        }

        /// <summary>
        /// Moves it on by one frame while it may be attacking. With an
        /// <paramref name="attackTarget"/> it turns to face that point at
        /// <see cref="AttackTurnSpeed"/> instead of the route, and walks on
        /// along the route only when <paramref name="walkWhileAttacking"/> is
        /// true (Phase 11: while the player traces a breach); without one it
        /// is the same as <see cref="Step(float)"/>.
        /// </summary>
        public void Step(float dt, (float X, float Z)? attackTarget, bool walkWhileAttacking)
        {
            var target = points[next];
            float distance = Distance(Position, target);
            bool attacking = attackTarget.HasValue;
            var facing = attacking ? attackTarget.Value : target;
            float wanted = Distance(Position, facing) < ArrivalDistance ? Heading : HeadingFrom(Position, facing);
            float stillToTurn = TurnTowards(wanted, (attacking ? AttackTurnSpeed : TurnSpeed) * dt);
            pause = MathF.Max(0f, pause - dt);
            IsWalking = pause <= 0f && (attacking ? walkWhileAttacking : stillToTurn < WalkingAngle);
            if (!IsWalking)
            {
                return;
            }

            float stride = MathF.Min(speed * dt, distance);
            Position = MoveTowards(Position, target, stride);
            DistanceWalked += stride;
            if (Distance(Position, target) < ArrivalDistance)
            {
                Arrive();
            }
        }

        // At either end of the route it turns back and waits; at a point in between it goes straight on.
        private void Arrive()
        {
            if (next == 0 || next == points.Length - 1)
            {
                direction = -direction;
                pause = EndPause;
            }

            next += direction;
        }

        // Turns by at most maxDegrees, the shorter way round, and returns how many degrees are still to turn.
        private float TurnTowards(float wanted, float maxDegrees)
        {
            float delta = wanted - Heading;

            // Half a turn, the way back at an end of the route, has no shorter way round.
            // It is then turned without crossing ±180°: on the hospital's east-west routes
            // the K-7 turns through north (+z) at both ends, as it did before this rule.
            if (delta > 180f + HalfTurnTolerance)
            {
                delta -= 360f;
            }
            else if (delta < -180f - HalfTurnTolerance)
            {
                delta += 360f;
            }

            if (MathF.Abs(delta) <= maxDegrees)
            {
                Heading = Normalize(wanted);
                return 0f;
            }

            Heading = Normalize(Heading + MathF.Sign(delta) * maxDegrees);
            return MathF.Abs(delta) - maxDegrees;
        }

        private static float HeadingFrom((float X, float Z) from, (float X, float Z) to)
        {
            return MathF.Atan2(to.X - from.X, to.Z - from.Z) * DegreesPerRadian;
        }

        // The same direction as a heading above -180 and up to 180 degrees.
        private static float Normalize(float degrees)
        {
            degrees %= 360f;
            if (degrees > 180f)
            {
                return degrees - 360f;
            }

            if (degrees <= -180f)
            {
                return degrees + 360f;
            }

            return degrees;
        }

        private static float Distance((float X, float Z) from, (float X, float Z) to)
        {
            float dx = to.X - from.X;
            float dz = to.Z - from.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        // Unity's Vector3.MoveTowards on the floor: at most maxDistance closer, landing exactly on the point when in reach.
        private static (float X, float Z) MoveTowards((float X, float Z) from, (float X, float Z) to, float maxDistance)
        {
            float dx = to.X - from.X;
            float dz = to.Z - from.Z;
            float squared = dx * dx + dz * dz;
            if (squared == 0f || squared <= maxDistance * maxDistance)
            {
                return to;
            }

            float distance = MathF.Sqrt(squared);
            return (from.X + dx / distance * maxDistance, from.Z + dz / distance * maxDistance);
        }
    }
}
