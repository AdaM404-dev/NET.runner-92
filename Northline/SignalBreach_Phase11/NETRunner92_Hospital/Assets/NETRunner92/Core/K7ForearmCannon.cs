using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NETRunner92
{
    sealed class K7ForearmCannon
    {
        const float Range = 12f;
        const float ChargeTime = .85f;
        const float RecoveryTime = 1.45f;
        const float StowedTravel = .036f;
        const float StowedDepth = .025f;

        readonly Transform root, head, upperArm, forearm, muzzle, breech;
        readonly Quaternion forearmRest;
        readonly Hatch[] hatches = new Hatch[8];
        readonly Slide[] emitters = new Slide[4];
        readonly LineRenderer pulse;
        float deployment, charge, recovery, pulseTime;
        bool tracking;

        readonly struct Hatch
        {
            readonly Transform hatch;
            readonly Quaternion closed, open;

            public Hatch(Transform hatch, Transform marker)
            {
                this.hatch = hatch;
                closed = marker.localRotation;
                open = hatch.localRotation;
            }

            public void Set(float amount) => hatch.localRotation = Quaternion.Slerp(closed, open, amount);
        }

        readonly struct Slide
        {
            readonly Transform emitter;
            readonly Vector3 deployed, stowed;

            public Slide(Transform emitter, Vector3 travel)
            {
                this.emitter = emitter;
                deployed = emitter.localPosition;
                stowed = deployed - emitter.parent.InverseTransformVector(travel);
            }

            public void Set(float amount) => emitter.localPosition = Vector3.Lerp(stowed, deployed, amount);
        }

        public static K7ForearmCannon Create(Transform root, Dictionary<string, Transform> bones)
        {
            return bones.ContainsKey("Cannon_L_Muzzle") ? new K7ForearmCannon(root, bones) : null;
        }

        K7ForearmCannon(Transform root, Dictionary<string, Transform> parts)
        {
            this.root = root;
            head = parts["Head"];
            upperArm = parts["UpperArm_L"];
            forearm = parts["LowerArm_L"];
            forearmRest = forearm.localRotation;
            muzzle = parts["Cannon_L_Muzzle"];
            breech = parts["Cannon_L_Breech"];
            Vector3 axis = (muzzle.position - breech.position).normalized;
            Vector3 outward = Vector3.ProjectOnPlane(breech.position - forearm.position, axis).normalized;
            Vector3 travel = axis * StowedTravel + outward * StowedDepth;
            for (int i = 0; i < emitters.Length; i++)
            {
                string suffix = i == 0 ? "" : $"_LOD{i}";
                string outer = "Armor_Forearm_L" + suffix;
                string inner = "Armor_Forearm_L_InnerHatch" + suffix;
                hatches[2 * i] = new Hatch(parts[outer], parts[outer + "_Closed"]);
                hatches[2 * i + 1] = new Hatch(parts[inner], parts[inner + "_Closed"]);
                emitters[i] = new Slide(parts["Cannon_L_Emitter" + suffix], travel);
            }
            var effect = new GameObject("K-7 energy pulse");
            effect.transform.SetParent(root, false);
            pulse = effect.AddComponent<LineRenderer>();
            var emitter = parts["Cannon_L_Emitter"].GetComponent<Renderer>();
            foreach (var material in emitter.sharedMaterials)
                if (material.name == "MAT_K7_Cannon_EmitterCore") pulse.sharedMaterial = material;
            pulse.startWidth = .065f;
            pulse.endWidth = .035f;
            pulse.positionCount = 2;
            pulse.numCapVertices = 2;
            pulse.generateLightingData = true;
            pulse.shadowCastingMode = ShadowCastingMode.Off;
            pulse.receiveShadows = false;
            pulse.enabled = false;
            SetDeployment();
        }

        public bool Track(FirstPersonController player, float dt)
        {
            pulseTime -= dt;
            pulse.enabled = pulseTime > 0f;
            recovery = Mathf.Max(0f, recovery - dt);
            tracking = CanSee(player);
            if (!tracking) charge = 0f;
            deployment = Mathf.MoveTowards(deployment, tracking ? 1f : 0f, dt / .48f);
            SetDeployment();
            return tracking;
        }

        public void Suspend()
        {
            tracking = false;
            charge = deployment = pulseTime = 0f;
            pulse.enabled = false;
            forearm.localRotation = forearmRest;
            SetDeployment();
        }

        void SetDeployment()
        {
            float doors = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(deployment / .74f));
            float extension = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((deployment - .26f) / .74f));
            foreach (var hatch in hatches) hatch.Set(doors);
            foreach (var emitter in emitters) emitter.Set(extension);
        }

        bool CanSee(FirstPersonController player)
        {
            if (!player || player.IsDown || Mathf.Abs(player.transform.position.y - root.position.y) > 1.45f)
                return false;
            Vector3 offset = player.TargetCenter - head.position;
            Vector3 flat = Vector3.ProjectOnPlane(offset, Vector3.up);
            if (flat.sqrMagnitude > .04f && Vector3.Dot(root.forward, flat.normalized) < (tracking ? -.17f : .35f))
                return false;
            return HitsPlayer(head.position, player, out _);
        }

        static bool HitsPlayer(Vector3 origin, FirstPersonController player, out RaycastHit hit)
        {
            Vector3 ray = player.TargetCenter - origin;
            return Physics.Raycast(origin, ray, out hit, Range, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore) && hit.collider == player.Capsule;
        }

        public void AimAndFire(FirstPersonController player, float dt)
        {
            forearm.localRotation = forearmRest;
            if (deployment <= 0f) return;

            Quaternion gaitPose = upperArm.localRotation;
            Vector3 liftAxis = upperArm.InverseTransformDirection(root.right);
            upperArm.localRotation = gaitPose * Quaternion.AngleAxis(-67f * deployment, liftAxis);
            if (!tracking) return;

            Vector3 target = player.TargetCenter;
            float aimWeight = Mathf.SmoothStep(0f, 1f, deployment);
            Vector3 barrel = muzzle.position - breech.position;
            Quaternion aim = Quaternion.FromToRotation(barrel, target - breech.position);
            forearm.rotation = Quaternion.Slerp(Quaternion.identity, aim, aimWeight) * forearm.rotation;

            if (recovery > 0f || deployment < 1f)
            {
                charge = 0f;
                return;
            }

            // Correct the aim for the muzzle's offset from the elbow after the first rotation.
            barrel = muzzle.position - breech.position;
            forearm.rotation = Quaternion.FromToRotation(barrel, target - muzzle.position) * forearm.rotation;
            if (!Physics.Raycast(muzzle.position, muzzle.position - breech.position, out var hit, Range,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || hit.collider != player.Capsule)
            {
                charge = 0f;
                return;
            }
            charge += dt;
            if (charge < ChargeTime) return;

            pulse.SetPosition(0, muzzle.position);
            pulse.SetPosition(1, hit.point);
            pulseTime = .12f;
            pulse.enabled = true;
            charge = 0f;
            recovery = RecoveryTime;
            player.TakeDamage(25f);
        }
    }
}
