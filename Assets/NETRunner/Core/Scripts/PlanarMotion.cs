using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The player's velocity along the floor: the walking input sets a target
    /// velocity, never faster on a diagonal, and the velocity moves towards
    /// it by at most the acceleration each frame. Velocities are (x, z) on
    /// the floor in metres per second. The arithmetic follows Unity's
    /// <c>Vector3.ClampMagnitude</c> and <c>Vector3.MoveTowards</c> step by
    /// step, so the player moves exactly as it did before #43. Plain maths
    /// with no scene objects (see PlanarMotionTests).
    /// </summary>
    public static class PlanarMotion
    {
        /// <summary>
        /// The velocity the input asks for: the input direction, shortened to
        /// length 1 when it is longer (a diagonal), times <paramref name="speed"/>.
        /// </summary>
        /// <param name="inputX">The input along the floor's x, in the scene's frame.</param>
        /// <param name="inputZ">The input along the floor's z.</param>
        /// <param name="speed">Walking or sprinting speed in metres per second.</param>
        public static (float X, float Z) Target(float inputX, float inputZ, float speed)
        {
            float squared = inputX * inputX + inputZ * inputZ;
            if (squared > 1f)
            {
                float length = (float)Math.Sqrt(squared);
                float unitX = inputX / length;
                float unitZ = inputZ / length;
                return (unitX * speed, unitZ * speed);
            }

            return (inputX * speed, inputZ * speed);
        }

        /// <summary>
        /// The velocity moved towards the target along a straight line by at
        /// most <paramref name="maxChange"/>; within that distance it becomes
        /// the target exactly.
        /// </summary>
        public static (float X, float Z) Approach(float x, float z, float targetX, float targetZ, float maxChange)
        {
            float toX = targetX - x;
            float toZ = targetZ - z;
            float squared = toX * toX + toZ * toZ;
            if (squared == 0f || (maxChange >= 0f && squared <= maxChange * maxChange))
            {
                return (targetX, targetZ);
            }

            float distance = (float)Math.Sqrt(squared);
            return (x + toX / distance * maxChange, z + toZ / distance * maxChange);
        }

        /// <summary>The speed of a velocity, in metres per second.</summary>
        public static float Speed(float x, float z)
        {
            return (float)Math.Sqrt(x * x + z * z);
        }
    }
}
