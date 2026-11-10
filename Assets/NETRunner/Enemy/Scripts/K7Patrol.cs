using System.Collections.Generic;
using NetRunner.Core;
using UnityEngine;

namespace NetRunner.Enemy
{
    /// <summary>
    /// A K-7 robot on patrol: it walks back and forth along its route and
    /// animates its rig as it goes. Where it goes and when it walks come from
    /// <see cref="Patrol"/>, the angles of its legs, arms and head from
    /// <see cref="K7Gait"/>; this component moves the robot and turns the
    /// bones. The model has no walk animation, so the bones are turned in
    /// code. The hospital's scene builder places the robots and sets them up
    /// through <see cref="Configure"/>.
    /// </summary>
    public sealed class K7Patrol : MonoBehaviour
    {
        // A bone that turns about one axis from its rest pose, such as a knee or the neck.
        private readonly struct Hinge
        {
            private readonly Transform bone;
            private readonly Quaternion rest;
            private readonly Vector3 axis;

            public Hinge(Transform bone, Vector3 worldAxis)
            {
                this.bone = bone;
                rest = bone.localRotation;
                axis = bone.InverseTransformDirection(worldAxis);
            }

            public void Pose(float degrees)
            {
                if (bone)
                {
                    bone.localRotation = rest * Quaternion.AngleAxis(degrees, axis);
                }
            }
        }

        [SerializeField] private Transform model;
        [SerializeField] private Vector3[] route;
        [SerializeField] private float speed = 1.10f;

        private readonly Hinge[] thighs = new Hinge[2];
        private readonly Hinge[] knees = new Hinge[2];
        private readonly Hinge[] feet = new Hinge[2];
        private readonly Hinge[] arms = new Hinge[2];
        private Hinge head;
        private Hinge pelvis;
        private Patrol patrol;
        private float height;
        private float scanOffset;

        /// <summary>The route in the scene, first point to last; the robot walks it back and forth.</summary>
        public IReadOnlyList<Vector3> Waypoints => route;

        private void Awake()
        {
            // The FBX imports with its visor towards -Z; turn the model round to look the way the robot walks.
            model.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.localRotation;
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                AddHinge(bone);
            }

            var points = new (float X, float Z)[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                points[i] = (route[i].x, route[i].z);
            }

            patrol = new Patrol(points, speed);

            // Robots start at different places along their head sweep, so they do not scan in step.
            scanOffset = route[0].x;
        }

        private void OnEnable()
        {
            // Carry on from wherever the robot stands: the hospital checks move one while it is switched off.
            var position = transform.position;
            patrol.Place((position.x, position.z), transform.eulerAngles.y);
            height = position.y;
        }

        private void Update()
        {
            patrol.Step(Time.deltaTime);
            var (x, z) = patrol.Position;
            transform.SetPositionAndRotation(new Vector3(x, height, z), Quaternion.Euler(0f, patrol.Heading, 0f));
            Pose(patrol.DistanceWalked, patrol.IsWalking, Time.time);
        }

        /// <summary>
        /// Sets the robot up from the scene builder: the model to animate, the
        /// route (it starts on the first point, facing the second) and its
        /// walking speed.
        /// </summary>
        public void Configure(Transform rig, Vector3[] waypoints, float metersPerSecond)
        {
            model = rig;
            route = waypoints;
            speed = metersPerSecond;
            transform.SetPositionAndRotation(route[0], Quaternion.LookRotation(route[1] - route[0]));
        }

        private void AddHinge(Transform bone)
        {
            int side = bone.name.EndsWith("_L") ? K7Gait.Left : K7Gait.Right;
            switch (bone.name)
            {
                case "Head":
                    head = new Hinge(bone, transform.up);
                    break;
                case "Pelvis":
                    pelvis = new Hinge(bone, transform.forward);
                    break;
                case "Thigh_L":
                case "Thigh_R":
                    thighs[side] = new Hinge(bone, transform.right);
                    break;
                case "Shin_L":
                case "Shin_R":
                    knees[side] = new Hinge(bone, transform.right);
                    break;
                case "Foot_L":
                case "Foot_R":
                    feet[side] = new Hinge(bone, transform.right);
                    break;
                case "UpperArm_L":
                case "UpperArm_R":
                    arms[side] = new Hinge(bone, transform.right);
                    break;
            }
        }

        private void Pose(float distanceWalked, bool walking, float time)
        {
            for (int side = K7Gait.Left; side <= K7Gait.Right; side++)
            {
                thighs[side].Pose(K7Gait.Thigh(distanceWalked, side, walking));
                knees[side].Pose(K7Gait.Knee(distanceWalked, side, walking));
                feet[side].Pose(K7Gait.Foot(distanceWalked, side, walking));
                arms[side].Pose(K7Gait.Arm(distanceWalked, side, walking));
            }

            pelvis.Pose(K7Gait.Pelvis(distanceWalked, walking));
            head.Pose(K7Gait.Head(time, scanOffset, walking));
            model.localPosition = Vector3.up * K7Gait.Bob(distanceWalked, walking);
        }
    }
}
