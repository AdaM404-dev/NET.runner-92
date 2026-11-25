using NUnit.Framework;
using UnityEngine;

namespace NetRunner.Player.Tests
{
    /// <summary>
    /// PlayMode tests of the player's body on a test-built floor: walking and
    /// sprinting speed up to their speeds, diagonals are no faster, it falls
    /// under gravity up to a limit and lands, mouse look stops at the pitch
    /// limit, and Warp and FollowPlatform move it. Each test moves the player
    /// itself, frame by frame, so the results do not depend on the machine.
    /// Expected values come from docs/systems/player.md.
    /// </summary>
    public sealed class FirstPersonControllerTests
    {
        private const float Frame = 1f / 60f;
        private const float WalkingSpeed = 3.5f;
        private const float SprintSpeed = 5.5f;
        private const float Acceleration = 22f;
        private const float Gravity = 18f;
        private const float FallLimit = 34f;
        private const float EyeHeight = 1.65f;
        private const float PitchLimit = 85f;
        private const float DetailCullDistance = 34f;
        private const string NearDetailLayer = "Near-detail geometry";

        private TestWorld world;

        [SetUp]
        public void Build()
        {
            world = new TestWorld();
            world.HideHospital();
        }

        [TearDown]
        public void Clear()
        {
            world.Clear();
        }

        [Test]
        public void WalkingSpeedsUpToThreeAndAHalfMetresPerSecond()
        {
            world.Floor();
            FirstPersonController player = StandingPlayer();
            Vector3 start = player.transform.position;

            Steps(player, Vector3.forward, false, 60);

            // Speeding up takes 3.5 / 22 s, which costs half that time at full speed.
            float expected = WalkingSpeed - WalkingSpeed * WalkingSpeed / (2f * Acceleration);
            Assert.That(Flat(player.transform.position - start).magnitude, Is.EqualTo(expected).Within(.04f));
            Assert.That(SpeedOver(player, Vector3.forward, false), Is.EqualTo(WalkingSpeed).Within(.01f));
        }

        [Test]
        public void SprintingReachesFiveAndAHalfMetresPerSecond()
        {
            world.Floor();
            FirstPersonController player = StandingPlayer();

            Steps(player, Vector3.forward, true, 60);

            Assert.That(SpeedOver(player, Vector3.forward, true), Is.EqualTo(SprintSpeed).Within(.01f));
        }

        [Test]
        public void WalkingDiagonallyIsNoFaster()
        {
            world.Floor();
            FirstPersonController player = StandingPlayer();
            var diagonal = new Vector3(1f, 0f, 1f);

            Steps(player, diagonal, false, 60);

            Assert.That(SpeedOver(player, diagonal, false), Is.EqualTo(WalkingSpeed).Within(.01f));
        }

        [Test]
        public void ItFallsUnderGravityAndLandsOnTheFloor()
        {
            world.Floor();
            const float Height = 10f;
            const float Seconds = .5f;
            FirstPersonController player = world.Player(new Vector3(0f, Height, 0f));

            Steps(player, Vector3.zero, false, Mathf.RoundToInt(Seconds / Frame));
            float fallen = Height - player.transform.position.y;
            Steps(player, Vector3.zero, false, 90);

            Assert.That(fallen, Is.EqualTo(.5f * Gravity * Seconds * Seconds).Within(.1f));
            Assert.That(player.IsGrounded, Is.True, "Not standing after the fall");
            Assert.That(player.transform.position.y, Is.EqualTo(0f).Within(.1f));
        }

        [Test]
        public void ALongFallIsCappedAtThirtyFourMetresPerSecond()
        {
            FirstPersonController player = world.Player(Vector3.zero);
            Steps(player, Vector3.zero, false, 150);
            float before = player.transform.position.y;

            Steps(player, Vector3.zero, false, 6);

            Assert.That((before - player.transform.position.y) / (6 * Frame), Is.EqualTo(FallLimit).Within(.01f));
        }

        [Test]
        public void TheEyesAreAtOnePointSixFiveMetres()
        {
            world.Floor();
            FirstPersonController player = StandingPlayer();

            Steps(player, Vector3.forward, false, 30);

            Assert.That(player.Eyes.transform.localPosition, Is.EqualTo(new Vector3(0f, EyeHeight, 0f)));
        }

        [Test]
        public void NearDetailIsCulledBeyondThirtyFourMetres()
        {
            FirstPersonController player = world.Player(Vector3.zero);
            int layer = LayerMask.NameToLayer(NearDetailLayer);

            Assert.That(layer, Is.GreaterThanOrEqualTo(0), "The near-detail layer is missing");
            Assert.That(player.Eyes.layerCullDistances[layer], Is.EqualTo(DetailCullDistance));
        }

        [Test]
        public void LookingUpOrDownStopsAtEightyFiveDegrees()
        {
            FirstPersonController player = world.Player(Vector3.zero);

            player.Look(new Vector2(0f, 100000f));
            Quaternion up = player.Eyes.transform.localRotation;
            player.Look(new Vector2(0f, -200000f));
            Quaternion down = player.Eyes.transform.localRotation;

            Assert.That(Quaternion.Angle(up, Quaternion.Euler(-PitchLimit, 0f, 0f)), Is.LessThan(.01f), "Looking up");
            Assert.That(Quaternion.Angle(down, Quaternion.Euler(PitchLimit, 0f, 0f)), Is.LessThan(.01f), "Looking down");
        }

        [Test]
        public void MovingTheMouseRightTurnsTheBodyRight()
        {
            FirstPersonController player = world.Player(Vector3.zero);

            player.Look(new Vector2(50f, 0f));

            Assert.That(player.transform.eulerAngles.y, Is.InRange(1f, 90f));
            Assert.That(player.Eyes.transform.localRotation, Is.EqualTo(Quaternion.identity));
        }

        [Test]
        public void AWarpStopsThePlayerAndLevelsTheView()
        {
            world.Floor();
            FirstPersonController player = StandingPlayer();
            Steps(player, Vector3.forward, true, 30);
            player.Look(new Vector2(0f, 300f));
            var target = new Vector3(5f, 0f, 5f);

            player.Warp(target, Quaternion.Euler(0f, 90f, 0f));
            Steps(player, Vector3.zero, false, 1);

            Assert.That(Flat(player.transform.position - target).magnitude, Is.LessThan(.001f), "Kept moving after the warp");
            Assert.That(player.transform.eulerAngles.y, Is.EqualTo(90f).Within(.01f));
            Assert.That(player.Eyes.transform.localRotation, Is.EqualTo(Quaternion.identity));
        }

        [Test]
        public void FollowingAPlatformLiftsThePlayerWithIt()
        {
            FirstPersonController player = world.Player(new Vector3(1f, 2f, 3f));

            player.FollowPlatform(.5f);

            Assert.That(player.transform.position, Is.EqualTo(new Vector3(1f, 2.5f, 3f)));
        }

        private static void Steps(FirstPersonController player, Vector3 wish, bool sprint, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                player.Move(wish, sprint, Frame);
            }
        }

        // Metres per second over the next ten frames, along the floor.
        private static float SpeedOver(FirstPersonController player, Vector3 wish, bool sprint)
        {
            const int Frames = 10;
            Vector3 start = player.transform.position;
            Steps(player, wish, sprint, Frames);
            return Flat(player.transform.position - start).magnitude / (Frames * Frame);
        }

        private static Vector3 Flat(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }

        // A player on the floor that has stood still long enough to touch it.
        private FirstPersonController StandingPlayer()
        {
            FirstPersonController player = world.Player(Vector3.zero);
            Steps(player, Vector3.zero, false, 10);
            return player;
        }
    }
}
