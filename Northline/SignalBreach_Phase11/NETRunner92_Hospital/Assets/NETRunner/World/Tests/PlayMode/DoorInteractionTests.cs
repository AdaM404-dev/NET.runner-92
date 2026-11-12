using System.Collections;
using System.Linq;
using NUnit.Framework;
using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NetRunner.World.Tests
{
    /// <summary>
    /// PlayMode tests of the hospital doors and the player's interaction
    /// (#10, #16), in the real Hospital_Exploration scene: every door starts
    /// shut, aiming at a door from 2 m targets it, E opens and closes it, and
    /// a locked door stays shut.
    /// </summary>
    public sealed class DoorInteractionTests
    {
        private const string SceneName = "Hospital_Exploration";
        private const float AimDistance = 2f;
        private const float DoorSeconds = 1.5f;

        private FirstPersonController player;
        private HospitalInteraction interaction;
        private HospitalDoor[] doors;

        [UnitySetUp]
        public IEnumerator LoadHospital()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            player = Object.FindAnyObjectByType<FirstPersonController>();
            interaction = player.GetComponent<HospitalInteraction>();
            doors = Object.FindObjectsByType<HospitalDoor>().OrderBy(door => door.name).ToArray();
        }

        [Test]
        public void EveryDoorStartsShut()
        {
            var open = doors.Where(door => !door.Closed || !door.IsPosedShut()).Select(door => door.name).ToArray();

            Assert.That(doors, Is.Not.Empty);
            Assert.That(open, Is.Empty, "Doors not shut at load");
        }

        [UnityTest]
        public IEnumerator AimingAtADoorFromTwoMetresTargetsIt()
        {
            var door = AimAtAnOrdinaryDoor();
            yield return null;

            Assert.That(door, Is.Not.Null, "No ordinary door could be aimed at from 2 m");
            Assert.That(interaction.TargetInView() as Object, Is.SameAs(door));
            Assert.That(door.Prompt.Text, Is.EqualTo("Ajtó kinyitása"));
        }

        [UnityTest]
        public IEnumerator UsingADoorOpensAndClosesIt()
        {
            var door = AimAtAnOrdinaryDoor();

            door.Interact(player);
            yield return new WaitForSeconds(DoorSeconds);
            bool opened = door.Open;
            string openPrompt = door.Prompt.Text;
            door.Interact(player);
            yield return new WaitForSeconds(DoorSeconds);

            Assert.That(opened, Is.True, "Door did not open");
            Assert.That(openPrompt, Is.EqualTo("Ajtó bezárása"));
            Assert.That(door.Closed && door.IsPosedShut(), Is.True, "Door did not close");
        }

        [UnityTest]
        public IEnumerator ALockedDoorSaysLockedAndStaysShut()
        {
            var door = AimAtAnOrdinaryDoor();
            door.SetLocked(true);

            door.Interact(player);
            yield return new WaitForSeconds(DoorSeconds);

            Assert.That(door.Prompt, Is.EqualTo(((GameAction?)null, "Zárva")));
            Assert.That(door.Closed && door.IsPosedShut(), Is.True, "Locked door moved");
        }

        [UnityTest]
        public IEnumerator AHalfOpenDoorIsNotPosedShut()
        {
            var door = AimAtAnOrdinaryDoor();
            door.Interact(player);
            door.Advance(DoorSeconds * .2f);
            yield return null;

            Assert.That(door.IsPosedShut(), Is.False);
        }

        /// <summary>
        /// Stands the player 2 m in front of the first ordinary door (not a
        /// lift door) whose leaf the eyes can see from either side, facing it.
        /// </summary>
        private HospitalDoor AimAtAnOrdinaryDoor()
        {
            player.enabled = false;
            foreach (var door in doors.Where(door => !door.LiftOnly))
            {
                foreach (float side in new[] { 1f, -1f })
                {
                    var facing = -door.transform.forward * side;
                    var position = door.transform.position - facing * AimDistance;
                    player.Warp(position, Quaternion.LookRotation(facing, Vector3.up));
                    Physics.SyncTransforms();
                    if (interaction.TargetInView() as Object == door)
                    {
                        return door;
                    }
                }
            }

            return null;
        }
    }
}
