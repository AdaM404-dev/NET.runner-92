using System.Collections.Generic;
using UnityEngine;

namespace NETRunner92
{
    public sealed class K7Patrol : MonoBehaviour
    {
        [SerializeField] Transform model;
        [SerializeField] Vector3[] route;
        [SerializeField] float speed = 1.10f;
        int target = 1;
        int direction = 1;
        float gait, pause;
        Joint head, pelvis;
        readonly Joint[] thighs = new Joint[2], knees = new Joint[2], feet = new Joint[2], arms = new Joint[2];
        public IReadOnlyList<Vector3> Waypoints => route;

        struct Joint
        {
            Transform bone;
            Quaternion rest;
            Vector3 axis;

            public Joint(Transform joint, Vector3 worldAxis)
            {
                bone = joint;
                rest = joint ? joint.localRotation : Quaternion.identity;
                axis = joint ? joint.InverseTransformDirection(worldAxis) : Vector3.right;
            }

            public void Pose(float angle)
            {
                if (bone) bone.localRotation = rest * Quaternion.AngleAxis(angle, axis);
            }
        }

        public void Configure(Transform rig, Vector3[] waypoints, float metersPerSecond)
        {
            model = rig;
            route = waypoints;
            speed = metersPerSecond;
            transform.SetPositionAndRotation(route[0], Quaternion.LookRotation(route[1] - route[0]));
        }

        void Awake()
        {
            // The FBX imports with its visor toward -Z. Keep the patrol root's +Z along the route.
            model.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.localRotation;
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                string name = bone.name;
                if (name == "Head") head = new Joint(bone, transform.up);
                if (name == "Pelvis") pelvis = new Joint(bone, transform.forward);
                int side = name.EndsWith("_L") ? 0 : 1;
                if (name == "Thigh_L" || name == "Thigh_R") thighs[side] = new Joint(bone, transform.right);
                if (name == "Shin_L" || name == "Shin_R") knees[side] = new Joint(bone, transform.right);
                if (name == "Foot_L" || name == "Foot_R") feet[side] = new Joint(bone, transform.right);
                if (name == "UpperArm_L" || name == "UpperArm_R") arms[side] = new Joint(bone, transform.right);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var displacement = route[target] - transform.position;
            var facing = Quaternion.LookRotation(displacement.sqrMagnitude > .00001f ? displacement : transform.forward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, dt * 115f);
            pause -= dt;
            bool moving = pause <= 0f && Quaternion.Angle(transform.rotation, facing) < 25f;
            if (moving)
            {
                float step = Mathf.Min(speed * dt, displacement.magnitude);
                transform.position = Vector3.MoveTowards(transform.position, route[target], step);
                gait += step * 8.1f;
                if ((route[target] - transform.position).sqrMagnitude < .0001f)
                {
                    if (target == 0 || target == route.Length - 1)
                    {
                        direction = -direction;
                        pause = 1.15f;
                    }
                    target += direction;
                }
            }
            float amount = moving ? 1f : 0f;
            for (int i = 0; i < 2; i++)
            {
                float swing = Mathf.Sin(gait + i * Mathf.PI) * amount;
                float flex = Mathf.Max(0f, -swing);
                thighs[i].Pose(23f * swing - 4f * amount);
                knees[i].Pose(26f * flex);
                feet[i].Pose(-10f * swing - 11f * flex);
                arms[i].Pose(-15f * swing);
            }
            pelvis.Pose(Mathf.Sin(gait) * 2.2f * amount);
            head.Pose(Mathf.Sin(Time.time * (moving ? .72f : 2.1f) + route[0].x) * (moving ? 8f : 23f));
            model.localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(gait)) * .032f * amount);
        }
    }
}
