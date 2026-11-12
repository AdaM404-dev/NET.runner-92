using System.Collections.Generic;
using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace NetRunner.Enemy
{
    /// <summary>
    /// The K-7's left forearm cannon (Phase 11): it opens its armour hatches,
    /// slides the emitters out, raises the arm, aims at the player it sees and
    /// fires an energy pulse. The timing comes from <see cref="CannonCycle"/>;
    /// this class checks sight and aim with raycasts and moves the bones.
    /// <see cref="K7Patrol"/> owns it and calls it every frame.
    /// </summary>
    public sealed class K7ForearmCannon
    {
        private readonly struct Hatch
        {
            private readonly Transform hatch;
            private readonly Quaternion closed;
            private readonly Quaternion open;

            public Hatch(Transform hatch, Transform marker)
            {
                this.hatch = hatch;
                closed = marker.localRotation;
                open = hatch.localRotation;
            }

            public void Set(float amount) => hatch.localRotation = Quaternion.Slerp(closed, open, amount);
        }

        private readonly struct Slide
        {
            private readonly Transform emitter;
            private readonly Vector3 deployed;
            private readonly Vector3 stowed;

            public Slide(Transform emitter, Vector3 travel)
            {
                this.emitter = emitter;
                deployed = emitter.localPosition;
                stowed = deployed - emitter.parent.InverseTransformVector(travel);
            }

            public void Set(float amount) => emitter.localPosition = Vector3.Lerp(stowed, deployed, amount);
        }

        /// <summary>Metres the cannon sees and shoots.</summary>
        public const float Range = 12f;

        private const string MuzzleBone = "Cannon_L_Muzzle";
        private const string BreechBone = "Cannon_L_Breech";
        private const string EmitterBone = "Cannon_L_Emitter";
        private const string EmitterMaterial = "MAT_K7_Cannon_EmitterCore";
        private const int LevelsOfDetail = 4;

        // The emitters slide back along the barrel and into the armour when stowed.
        private const float StowedTravel = .036f;
        private const float StowedDepth = .025f;

        // The player is ignored more than this many metres above or below the robot (another floor).
        private const float FloorTolerance = 1.45f;

        // Facing limits as cosines: it starts tracking within about 70° of its front and keeps tracking up to about 100°.
        private const float StartTrackingCosine = .35f;
        private const float KeepTrackingCosine = -.17f;

        // Below this horizontal distance (squared) the direction check is skipped: the player is right on top of it.
        private const float CloseRangeSquared = .04f;

        // Degrees the upper arm lifts when the cannon is fully deployed.
        private const float ArmLift = 67f;

        private const float PulseStartWidth = .065f;
        private const float PulseEndWidth = .035f;

        private readonly CannonCycle cycle = new CannonCycle();
        private readonly Transform root;
        private readonly Transform head;
        private readonly Transform upperArm;
        private readonly Transform forearm;
        private readonly Transform muzzle;
        private readonly Transform breech;
        private readonly Quaternion forearmRest;
        private readonly Hatch[] hatches = new Hatch[2 * LevelsOfDetail];
        private readonly Slide[] emitters = new Slide[LevelsOfDetail];
        private readonly LineRenderer pulse;
        private bool tracking;

        private K7ForearmCannon(Transform root, IReadOnlyDictionary<string, Transform> parts)
        {
            this.root = root;
            head = parts["Head"];
            upperArm = parts["UpperArm_L"];
            forearm = parts["LowerArm_L"];
            forearmRest = forearm.localRotation;
            muzzle = parts[MuzzleBone];
            breech = parts[BreechBone];
            Vector3 axis = (muzzle.position - breech.position).normalized;
            Vector3 outward = Vector3.ProjectOnPlane(breech.position - forearm.position, axis).normalized;
            Vector3 travel = axis * StowedTravel + outward * StowedDepth;
            for (int i = 0; i < LevelsOfDetail; i++)
            {
                string suffix = i == 0 ? "" : $"_LOD{i}";
                string outer = "Armor_Forearm_L" + suffix;
                string inner = "Armor_Forearm_L_InnerHatch" + suffix;
                hatches[2 * i] = new Hatch(parts[outer], parts[outer + "_Closed"]);
                hatches[2 * i + 1] = new Hatch(parts[inner], parts[inner + "_Closed"]);
                emitters[i] = new Slide(parts[EmitterBone + suffix], travel);
            }

            pulse = CreatePulse(root, parts[EmitterBone].GetComponent<Renderer>());
            SetDeployment();
        }

        /// <summary>The cannon on a combat model's bones, or null when the model has none.</summary>
        public static K7ForearmCannon Create(Transform root, IReadOnlyDictionary<string, Transform> bones)
        {
            return bones.ContainsKey(MuzzleBone) ? new K7ForearmCannon(root, bones) : null;
        }

        /// <summary>
        /// Looks for the player and deploys or stows; returns whether it sees
        /// the player, which makes the robot turn to face them.
        /// </summary>
        public bool Track(FirstPersonController player, float dt)
        {
            tracking = CanSee(player);
            cycle.Step(dt, tracking);
            pulse.enabled = cycle.PulseVisible;
            SetDeployment();
            return tracking;
        }

        /// <summary>Stows the cannon at once, as a successful Signal Breach does.</summary>
        public void Suspend()
        {
            tracking = false;
            cycle.Suspend();
            pulse.enabled = false;
            forearm.localRotation = forearmRest;
            SetDeployment();
        }

        /// <summary>
        /// Raises the arm by how far the cannon is deployed, aims at the
        /// player it tracks, and charges and fires while the barrel points at
        /// them. Called after the robot's walk pose, which it adds to.
        /// </summary>
        public void AimAndFire(FirstPersonController player, float dt)
        {
            forearm.localRotation = forearmRest;
            if (cycle.Deployment <= 0f)
            {
                return;
            }

            Quaternion gaitPose = upperArm.localRotation;
            Vector3 liftAxis = upperArm.InverseTransformDirection(root.right);
            upperArm.localRotation = gaitPose * Quaternion.AngleAxis(-ArmLift * cycle.Deployment, liftAxis);
            if (!tracking)
            {
                return;
            }

            Vector3 target = player.TargetCenter;
            float aimWeight = Easing.SmoothStep01(cycle.Deployment);
            Vector3 barrel = muzzle.position - breech.position;
            Quaternion aim = Quaternion.FromToRotation(barrel, target - breech.position);
            forearm.rotation = Quaternion.Slerp(Quaternion.identity, aim, aimWeight) * forearm.rotation;
            if (!cycle.CanCharge)
            {
                cycle.Charge(dt, false);
                return;
            }

            // Correct the aim for the muzzle's offset from the elbow after the first rotation.
            barrel = muzzle.position - breech.position;
            forearm.rotation = Quaternion.FromToRotation(barrel, target - muzzle.position) * forearm.rotation;
            bool onTarget = Physics.Raycast(muzzle.position, muzzle.position - breech.position, out var hit, Range,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider == player.Capsule;
            if (!cycle.Charge(dt, onTarget))
            {
                return;
            }

            pulse.SetPosition(0, muzzle.position);
            pulse.SetPosition(1, hit.point);
            pulse.enabled = true;
            player.TakeDamage(CannonCycle.Damage);
        }

        private static LineRenderer CreatePulse(Transform root, Renderer emitter)
        {
            var effect = new GameObject("K-7 energy pulse");
            effect.transform.SetParent(root, false);
            var line = effect.AddComponent<LineRenderer>();
            foreach (var material in emitter.sharedMaterials)
            {
                if (material.name == EmitterMaterial)
                {
                    line.sharedMaterial = material;
                }
            }

            line.startWidth = PulseStartWidth;
            line.endWidth = PulseEndWidth;
            line.positionCount = 2;
            line.numCapVertices = 2;
            line.generateLightingData = true;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private void SetDeployment()
        {
            foreach (var hatch in hatches)
            {
                hatch.Set(cycle.HatchOpening);
            }

            foreach (var emitter in emitters)
            {
                emitter.Set(cycle.EmitterExtension);
            }
        }

        private bool CanSee(FirstPersonController player)
        {
            if (!player || player.IsDown || Mathf.Abs(player.transform.position.y - root.position.y) > FloorTolerance)
            {
                return false;
            }

            Vector3 offset = player.TargetCenter - head.position;
            Vector3 flat = Vector3.ProjectOnPlane(offset, Vector3.up);
            float limit = tracking ? KeepTrackingCosine : StartTrackingCosine;
            if (flat.sqrMagnitude > CloseRangeSquared && Vector3.Dot(root.forward, flat.normalized) < limit)
            {
                return false;
            }

            return Physics.Raycast(head.position, offset, out var hit, Range,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider == player.Capsule;
        }
    }
}
