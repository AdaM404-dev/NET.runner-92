namespace NetRunner.Core
{
    /// <summary>
    /// The player's up-and-down speed for one frame: gravity, jumping, and
    /// staying pressed to the floor. Plain maths with no scene objects, so it
    /// is tested without starting the game (see VerticalMotionTests).
    /// </summary>
    public static class VerticalMotion
    {
        /// <summary>Downward acceleration in metres per second, per second.</summary>
        public const float Gravity = 14f;

        /// <summary>Upward speed in metres per second at the moment of a jump.</summary>
        public const float JumpSpeed = 4.4f;

        /// <summary>Speed used while standing, so the capsule stays on the floor.</summary>
        public const float GroundStick = -2f;

        /// <summary>
        /// Returns the vertical speed after one frame that lasts <paramref name="dt"/> seconds.
        /// </summary>
        /// <param name="verticalSpeed">Speed at the start of the frame; positive is up.</param>
        /// <param name="grounded">Whether the player stands on something.</param>
        /// <param name="jumpPressed">Whether jump was pressed this frame.</param>
        /// <param name="canJump">False where jumping is switched off, such as the preview scene.</param>
        /// <param name="dt">Frame length in seconds.</param>
        public static float Step(float verticalSpeed, bool grounded, bool jumpPressed, bool canJump, float dt)
        {
            if (grounded && verticalSpeed < 0f)
            {
                verticalSpeed = GroundStick;
            }

            if (jumpPressed && grounded && canJump)
            {
                verticalSpeed = JumpSpeed;
            }

            return verticalSpeed - Gravity * dt;
        }
    }
}
