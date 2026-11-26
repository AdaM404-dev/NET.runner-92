using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetRunner.Enemy.Tests
{
    /// <summary>
    /// PlayMode tests of the K-7 patrol component on a stand-in robot that the
    /// test builds from empty objects named like the real model's bones. It
    /// walks its route at its speed, swings its legs, stops and turns round
    /// at the end, and carries on from wherever it was put while switched
    /// off. The four real robots in the hospital are covered by
    /// HospitalChecksTests. Expected values come from docs/systems/enemy-k7.md.
    /// Since Phase 11 the robot swaps the model it is given for the combat
    /// model (Resources/K7_HospitalCombat) in the same place, so the bones
    /// and the model turned round are looked up on the model that is active.
    /// </summary>
    public sealed class K7PatrolTests
    {
        private const float Frame = 1f / 60f;
        private const float Speed = 1.2f;

        private static readonly string[] BoneNames =
        {
            "Head", "Pelvis", "Thigh_L", "Thigh_R", "Shin_L", "Shin_R", "Foot_L", "Foot_R", "UpperArm_L", "UpperArm_R",
        };

        private GameObject robot;

        [SetUp]
        public void UseEvenFrames()
        {
            // Every frame lasts exactly 1/60 s, however fast the machine runs the test.
            Time.captureDeltaTime = Frame;
        }

        [TearDown]
        public void RemoveRobot()
        {
            Time.captureDeltaTime = 0f;
            Object.Destroy(robot);
        }

        [UnityTest]
        public IEnumerator ItWalksTowardsTheNextPointAtItsSpeed()
        {
            var patrol = Build(new Vector3(10f, 0f, 0f));

            yield return Frames(60);

            Assert.That(patrol.transform.position.x, Is.EqualTo(Speed).Within(0.02f));
            Assert.That(patrol.transform.position.z, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(Vector3.Angle(patrol.transform.forward, Vector3.right), Is.LessThan(0.5f));
        }

        [UnityTest]
        public IEnumerator ItsLegsSwingWhileItWalks()
        {
            Build(new Vector3(10f, 0f, 0f));
            var thigh = Bone("Thigh_L");
            var rest = thigh.localRotation;

            // 10 frames at 1.2 m/s is 0.2 m: about a quarter of a stride, where a thigh swings furthest.
            yield return Frames(10);

            Assert.That(Quaternion.Angle(rest, thigh.localRotation), Is.GreaterThan(10f));
        }

        [UnityTest]
        public IEnumerator AtTheEndItStopsTurnsRoundAndWalksBack()
        {
            // 0.6 m at 1.2 m/s: the end is reached after half a second.
            var patrol = Build(new Vector3(0.6f, 0f, 0f));
            var thigh = Bone("Thigh_L");
            var rest = thigh.localRotation;
            yield return Frames(36);
            var end = patrol.transform.position;

            yield return Frames(30);

            Assert.That(Vector3.Distance(end, patrol.transform.position), Is.LessThan(1e-4f), "Moved while turning round");
            Assert.That(Quaternion.Angle(rest, thigh.localRotation), Is.LessThan(0.01f), "Legs not at rest while turning");
            Assert.That(Vector3.Angle(patrol.transform.forward, Vector3.right), Is.GreaterThan(40f), "Did not turn");

            // A half turn at 115°/s takes about 1.35 s to come within 25° of the way back, when it walks again.
            yield return Frames(60);

            Assert.That(patrol.transform.position.x, Is.LessThan(end.x - 0.1f), "Did not walk back");
            Assert.That(Vector3.Angle(patrol.transform.forward, Vector3.left), Is.LessThan(5f));
        }

        [UnityTest]
        public IEnumerator SwitchedOffAndMovedItCarriesOnFromThere()
        {
            // The hospital checks move a K-7 while it is switched off to photograph it.
            var patrol = Build(new Vector3(10f, 0f, 0f));
            yield return Frames(10);

            patrol.enabled = false;
            patrol.transform.position = new Vector3(5f, 0f, 0f);
            patrol.enabled = true;
            yield return Frames(1);

            Assert.That(patrol.transform.position.x, Is.EqualTo(5f + Speed * Frame).Within(1e-3f));
        }

        [UnityTest]
        public IEnumerator TheModelIsTurnedRoundToFaceTheWayItWalks()
        {
            // The imported model's visor faces -Z; the patrol walks along its own +Z.
            Build(new Vector3(10f, 0f, 0f));

            yield return null;

            var model = ActiveModel();
            Assert.That(model.name, Does.StartWith("K7_HospitalCombat"));
            Assert.That(Quaternion.Angle(model.localRotation, Quaternion.Euler(0f, 180f, 0f)), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator ABreachSwitchesItOffForTenSeconds()
        {
            // Phase 11: a successful Signal Breach stops the robot for 10 s; it cannot be breached again meanwhile.
            var patrol = Build(new Vector3(10f, 0f, 0f));
            yield return Frames(5);
            bool couldBreach = patrol.CanBreach;

            bool breached = patrol.TryBreachDisable();
            var stopped = patrol.transform.position;
            yield return Frames(30);

            Assert.That(couldBreach, Is.True);
            Assert.That(breached, Is.True);
            Assert.That(patrol.BreachRecovery, Is.EqualTo(10f - 30 * Frame).Within(.05f));
            Assert.That(patrol.CanBreach, Is.False);
            Assert.That(Vector3.Distance(stopped, patrol.transform.position), Is.LessThan(1e-4f), "Moved while switched off");
        }

        private K7Patrol Build(Vector3 end)
        {
            robot = new GameObject("Test K-7");
            robot.SetActive(false);
            var model = new GameObject("Model").transform;
            model.SetParent(robot.transform, false);
            foreach (string bone in BoneNames)
            {
                new GameObject(bone).transform.SetParent(model, false);
            }

            var patrol = robot.AddComponent<K7Patrol>();
            patrol.Configure(model, new[] { Vector3.zero, end }, Speed);
            robot.SetActive(true);
            return patrol;
        }

        private Transform Bone(string name)
        {
            foreach (var bone in ActiveModel().GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == name)
                {
                    return bone;
                }
            }

            return null;
        }

        private Transform ActiveModel()
        {
            foreach (Transform child in robot.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    return child;
                }
            }

            return null;
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }
    }
}
