using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The K-7 robot's walk, worked out from how far it has walked. Its model
    /// has no walk animation, so the patrol component turns the bones by
    /// these angles, in degrees from each bone's rest pose: the thighs swing
    /// around a slight lean, the leg swinging forward bends its knee, the
    /// feet roll, each arm swings against its leg, the pelvis rolls and the
    /// body bobs once per step. Standing, all of that rests and only the
    /// head moves, scanning faster and wider than while walking. Plain maths
    /// with no scene objects (see K7GaitTests).
    /// </summary>
    public static class K7Gait
    {
        /// <summary>Side index of the left limbs.</summary>
        public const int Left = 0;

        /// <summary>Side index of the right limbs.</summary>
        public const int Right = 1;

        // One full stride, a left and a right step, every 2π / 8.1 = 0.78 m.
        private const float StrideRadiansPerMetre = 8.1f;
        private const float ThighSwing = 23f;
        private const float ThighLean = 4f;
        private const float KneeBend = 26f;
        private const float FootSwing = 10f;
        private const float FootLift = 11f;
        private const float ArmSwing = 15f;
        private const float PelvisRoll = 2.2f;
        private const float BobHeight = 0.032f;

        // Head scan speeds are in radians per second of the sweep: one sweep every 2π / speed seconds.
        private const float WalkingScanSpeed = 0.72f;
        private const float WalkingScanAngle = 8f;
        private const float StandingScanSpeed = 2.1f;
        private const float StandingScanAngle = 23f;

        /// <summary>Thigh angle; positive swings the leg back, negative forward.</summary>
        public static float Thigh(float distanceWalked, int side, bool walking)
        {
            return walking ? ThighSwing * Swing(distanceWalked, side) - ThighLean : 0f;
        }

        /// <summary>Knee bend; the knee bends only while its leg swings forward.</summary>
        public static float Knee(float distanceWalked, int side, bool walking)
        {
            return walking ? KneeBend * Flex(distanceWalked, side) : 0f;
        }

        /// <summary>Foot angle, rolling with the stride and further while the knee bends.</summary>
        public static float Foot(float distanceWalked, int side, bool walking)
        {
            return walking ? -FootSwing * Swing(distanceWalked, side) - FootLift * Flex(distanceWalked, side) : 0f;
        }

        /// <summary>Upper arm angle, swinging against the leg on the same side.</summary>
        public static float Arm(float distanceWalked, int side, bool walking)
        {
            return walking ? -ArmSwing * Swing(distanceWalked, side) : 0f;
        }

        /// <summary>Side-to-side roll of the pelvis.</summary>
        public static float Pelvis(float distanceWalked, bool walking)
        {
            return walking ? MathF.Sin(Phase(distanceWalked)) * PelvisRoll : 0f;
        }

        /// <summary>How far the body rises, in metres.</summary>
        public static float Bob(float distanceWalked, bool walking)
        {
            return walking ? MathF.Abs(MathF.Sin(Phase(distanceWalked))) * BobHeight : 0f;
        }

        /// <summary>
        /// Head turn to the left or right of straight ahead.
        /// </summary>
        /// <param name="time">Seconds since the game started.</param>
        /// <param name="offset">Where in its sweep this robot starts, in radians, so several robots do not scan in step.</param>
        /// <param name="walking">Whether the robot is walking.</param>
        public static float Head(float time, float offset, bool walking)
        {
            return walking
                ? MathF.Sin(time * WalkingScanSpeed + offset) * WalkingScanAngle
                : MathF.Sin(time * StandingScanSpeed + offset) * StandingScanAngle;
        }

        private static float Phase(float distanceWalked)
        {
            return distanceWalked * StrideRadiansPerMetre;
        }

        // From -1 to 1 through the stride; the right leg is half a stride behind the left.
        private static float Swing(float distanceWalked, int side)
        {
            return MathF.Sin(Phase(distanceWalked) + side * MathF.PI);
        }

        // From 0 to 1 while the leg swings forward, 0 while it swings back.
        private static float Flex(float distanceWalked, int side)
        {
            return MathF.Max(0f, -Swing(distanceWalked, side));
        }
    }
}
