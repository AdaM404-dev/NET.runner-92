using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests: they run in a fraction of a second, without a scene,
    /// because VerticalMotion is plain maths.
    /// </summary>
    public class VerticalMotionTests
    {
        const float Frame = 1f / 60f;
        const float OneFrameOfGravity = VerticalMotion.Gravity * Frame;

        [Test]
        public void GravityTakesFourteenMetresPerSecondOffEverySecond()
        {
            float speed = VerticalMotion.Step(0f, grounded: false, jumpPressed: false, canJump: true, dt: 1f);

            Assert.That(speed, Is.EqualTo(-14f).Within(1e-4f));
        }

        [Test]
        public void OnTheFloorAFallingSpeedIsReplacedByTheGroundStick()
        {
            float speed = VerticalMotion.Step(-5f, grounded: true, jumpPressed: false, canJump: true, dt: Frame);

            Assert.That(speed, Is.EqualTo(VerticalMotion.GroundStick - OneFrameOfGravity).Within(1e-4f));
        }

        [Test]
        public void AJumpFromTheFloorStartsUpwardsAtTheJumpSpeed()
        {
            float speed = VerticalMotion.Step(VerticalMotion.GroundStick, grounded: true, jumpPressed: true, canJump: true, dt: Frame);

            Assert.That(speed, Is.EqualTo(VerticalMotion.JumpSpeed - OneFrameOfGravity).Within(1e-4f));
        }

        [Test]
        public void JumpingInTheAirDoesNothing()
        {
            float speed = VerticalMotion.Step(1f, grounded: false, jumpPressed: true, canJump: true, dt: Frame);

            Assert.That(speed, Is.EqualTo(1f - OneFrameOfGravity).Within(1e-4f));
        }

        [Test]
        public void NoJumpWhereJumpingIsSwitchedOff()
        {
            float speed = VerticalMotion.Step(VerticalMotion.GroundStick, grounded: true, jumpPressed: true, canJump: false, dt: Frame);

            Assert.That(speed, Is.EqualTo(VerticalMotion.GroundStick - OneFrameOfGravity).Within(1e-4f));
        }

        [Test]
        public void AJumpRisesAboutTwoThirdsOfAMetre()
        {
            // Same order as in the game: work out the speed, then move by speed × frame time.
            float speed = VerticalMotion.Step(VerticalMotion.GroundStick, grounded: true, jumpPressed: true, canJump: true, dt: Frame);
            float height = 0f;
            float peak = 0f;
            for (int frame = 0; frame < 120 && height >= 0f; frame++)
            {
                height += speed * Frame;
                peak = UnityEngine.Mathf.Max(peak, height);
                speed = VerticalMotion.Step(speed, grounded: false, jumpPressed: false, canJump: true, dt: Frame);
            }

            // 0.66 m was measured in the game on 2026-09-30 (docs/systems/player.md).
            Assert.That(peak, Is.EqualTo(0.66f).Within(0.03f));
        }
    }
}
