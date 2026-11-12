using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The first-person view: mouse look, with the pitch held within a limit,
    /// and the optional head bob, which follows a stride that advances with
    /// the distance walked. Angles are degrees; a positive pitch looks down,
    /// as an X rotation does in Unity. Plain maths with no scene objects (see
    /// ViewMotionTests).
    /// </summary>
    public static class ViewMotion
    {
        /// <summary>Stride radians per metre walked: one bob cycle per 2π/3 ≈ 2.1 m.</summary>
        public const float StrideRate = 3f;

        /// <summary>The highest bob, in metres.</summary>
        public const float MaxBob = .021f;

        /// <summary>Bob height in metres per metre per second of walking speed.</summary>
        public const float BobPerSpeed = .004f;

        /// <summary>Degrees the body turns for a sideways mouse movement; right is positive.</summary>
        public static float Turn(float mouseX, float sensitivity)
        {
            return mouseX * sensitivity;
        }

        /// <summary>
        /// The pitch after an upward mouse movement of <paramref name="mouseY"/>
        /// (moving up looks up), held within ±<paramref name="limit"/>.
        /// </summary>
        public static float Pitch(float pitch, float mouseY, float sensitivity, float limit)
        {
            float value = pitch - mouseY * sensitivity;
            if (value < -limit)
            {
                return -limit;
            }

            if (value > limit)
            {
                return limit;
            }

            return value;
        }

        /// <summary>The stride after walking at <paramref name="speed"/> for <paramref name="dt"/> seconds.</summary>
        public static float Stride(float stride, float speed, float dt)
        {
            return stride + speed * dt * StrideRate;
        }

        /// <summary>
        /// How far the eyes bob up (or down) at this point of the stride: none
        /// when the bob is switched off or the player is in the air, and
        /// higher the faster the player walks, up to <see cref="MaxBob"/>.
        /// </summary>
        public static float Bob(float stride, float speed, bool grounded, bool bobbing)
        {
            if (!bobbing || !grounded)
            {
                return 0f;
            }

            return (float)Math.Sin(stride) * Math.Min(MaxBob, speed * BobPerSpeed);
        }
    }
}
