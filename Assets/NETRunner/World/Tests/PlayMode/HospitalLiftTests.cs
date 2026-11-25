using System.Collections;
using System.Linq;
using NetRunner.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NetRunner.World.Tests
{
    /// <summary>
    /// PlayMode tests of the west lift in the real Hospital_Exploration scene:
    /// it refuses floors the stairs have not reached, opens its doors when
    /// called to the floor it is on, and once the stairs have reached L2 it
    /// closes, travels up 4.2 m and opens there. Expected behaviour comes
    /// from docs/systems/doors-and-interaction.md.
    /// </summary>
    public sealed class HospitalLiftTests
    {
        private const string SceneName = "Hospital_Exploration";
        private const float FloorHeight = 4.2f;
        private const float WestStairExit = -32f + 1.05f;
        private const int TimeoutFrames = 1200;

        private HospitalLift lift;
        private StairAccess access;
        private FirstPersonController player;

        [UnitySetUp]
        public IEnumerator LoadHospital()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Time.captureDeltaTime = 1f / 60f;
            lift = Object.FindAnyObjectByType<HospitalLift>();
            access = Object.FindAnyObjectByType<StairAccess>();
            player = Object.FindAnyObjectByType<FirstPersonController>();
            player.GetComponent<HospitalInteraction>().enabled = false;
            player.enabled = false;
        }

        [TearDown]
        public void UseRealTime()
        {
            Time.captureDeltaTime = 0f;
        }

        [Test]
        public void ItRefusesFloorsTheStairsHaveNotReached()
        {
            Assert.That(lift.Unlocked(0), Is.True);
            Assert.That(lift.Unlocked(1), Is.False);
            Assert.That(lift.Request(1, false), Is.False);
            Assert.That(lift.Request(3, true), Is.False);
        }

        [UnityTest]
        public IEnumerator CalledToItsOwnFloorItOpensItsDoors()
        {
            HospitalDoor landing = LandingDoor(0);

            bool accepted = lift.Request(0, false);
            for (int i = 0; i < TimeoutFrames && !landing.Open; i++)
            {
                yield return null;
            }

            Assert.That(accepted, Is.True);
            Assert.That(landing.Open, Is.True, "The L1 landing door did not open");
            Assert.That(lift.Busy, Is.False);
        }

        [UnityTest]
        public IEnumerator OnceTheStairsReachL2ItTravelsThereAndOpens()
        {
            player.Warp(new Vector3(WestStairExit, FloorHeight, 0f), Quaternion.identity);
            for (int i = 0; i < 10; i++)
            {
                player.Move(Vector3.zero, false, 1f / 60f);
            }

            yield return null;
            Assert.That(access.Highest, Is.EqualTo(1), "The stair exit did not unlock L2");
            HospitalDoor upper = LandingDoor(1);

            bool accepted = lift.Request(1, false);
            for (int i = 0; i < TimeoutFrames && !(lift.Floor == 1 && upper.Open); i++)
            {
                yield return null;
            }

            Assert.That(accepted, Is.True);
            Assert.That(lift.Floor, Is.EqualTo(1));
            Assert.That(upper.Open, Is.True, "The L2 landing door did not open");
        }

        // The west lift's landing door on a floor (0-based).
        private static HospitalDoor LandingDoor(int floor)
        {
            return Object.FindObjectsByType<HospitalDoor>()
                .Where(door => door.LiftOnly && door.transform.position.x < 0f)
                .OrderBy(door => Mathf.Abs(door.transform.position.y - floor * FloorHeight))
                .First();
        }
    }
}
