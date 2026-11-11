using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NETRunner92
{
    public sealed class K7Patrol : MonoBehaviour
    {
        internal const float BreachDisableSeconds = 10f;
        const float BreachRange = 8f;
        static readonly List<K7Patrol> Patrols = new();
        static readonly int Emissive = Shader.PropertyToID("_EmissiveColor");

        [SerializeField] Transform model;
        [SerializeField] Vector3[] route;
        [SerializeField] float speed = 1.10f;
        int target = 1;
        int direction = 1;
        float gait, pause;
        Joint head, pelvis;
        FirstPersonController player;
        K7ForearmCannon cannon;
        Transform breachPoint;
        bool breachTracing;
        float disabledAt, disabledUntil;
        int malfunctionFrame = -1;
        readonly List<(Renderer Renderer, int Slot, Color Emission)> indicators = new();
        MaterialPropertyBlock indicatorBlock;
        readonly Joint[] thighs = new Joint[2], knees = new Joint[2], feet = new Joint[2], arms = new Joint[2];
        public IReadOnlyList<Vector3> Waypoints => route;
        internal float BreachRecovery => Mathf.Max(0f, disabledUntil - Time.time);
        internal bool CanBreach => isActiveAndEnabled && cannon != null && BreachRecovery <= 0f;

        void OnEnable() => Patrols.Add(this);
        void OnDisable()
        {
            Patrols.Remove(this);
            if (malfunctionFrame >= 0) RestoreIndicators();
        }

        internal static K7Patrol FindBreachTarget(Camera eyes)
        {
            Vector3 origin = eyes.transform.position;
            Vector3 forward = eyes.transform.forward;
            K7Patrol best = null;
            float alignment = .975f;
            foreach (var patrol in Patrols)
            {
                if (!patrol.CanBreach) continue;
                Vector3 point = patrol.breachPoint.position;
                Vector3 offset = point - origin;
                if (offset.sqrMagnitude > BreachRange * BreachRange) continue;
                float dot = Vector3.Dot(forward, offset.normalized);
                if (dot < alignment || Physics.Linecast(origin, point, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    continue;
                alignment = dot;
                best = patrol;
            }
            return best;
        }

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
            InstallCombatModel();
            // The FBX imports with its visor toward -Z. Keep the patrol root's +Z along the route.
            model.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.localRotation;
            var parts = new Dictionary<string, Transform>();
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                string name = bone.name;
                parts.TryAdd(name, bone);
                if (name == "Head") head = new Joint(bone, transform.up);
                if (name == "Pelvis") pelvis = new Joint(bone, transform.forward);
                int side = name.EndsWith("_L") ? 0 : 1;
                if (name == "Thigh_L" || name == "Thigh_R") thighs[side] = new Joint(bone, transform.right);
                if (name == "Shin_L" || name == "Shin_R") knees[side] = new Joint(bone, transform.right);
                if (name == "Foot_L" || name == "Foot_R") feet[side] = new Joint(bone, transform.right);
                if (name == "UpperArm_L" || name == "UpperArm_R") arms[side] = new Joint(bone, transform.right);
            }
            cannon = K7ForearmCannon.Create(transform, parts);
            breachPoint = parts["Chest"];
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    var material = materials[slot];
                    if (material && material.name == "MAT_K7_StatusLight")
                        indicators.Add((renderer, slot, material.GetColor(Emissive)));
                }
            }
            indicatorBlock = new MaterialPropertyBlock();
        }

        void InstallCombatModel()
        {
            var prefab = Resources.Load<GameObject>("K7_HospitalCombat");
            if (!prefab)
            {
                Debug.LogError("The K-7 combat model is missing from Resources.", this);
                return;
            }

            var replacement = Instantiate(prefab, model.parent, false).transform;
            replacement.SetLocalPositionAndRotation(model.localPosition, model.localRotation);
            replacement.localScale = model.localScale;
            if (replacement.TryGetComponent<Animator>(out var animator)) animator.enabled = false;
            var renderers = new List<Renderer>[] { new(), new(), new(), new() };
            foreach (var renderer in replacement.GetComponentsInChildren<Renderer>())
            {
                if (renderer.name.StartsWith("COL_"))
                {
                    renderer.gameObject.SetActive(false);
                    continue;
                }

                renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (renderer is SkinnedMeshRenderer skin)
                {
                    skin.quality = SkinQuality.Bone1;
                    skin.localBounds = new Bounds(Vector3.up, new Vector3(2.6f, 2.8f, 2.6f));
                }
                string name = renderer.name;
                int level = name.EndsWith("_LOD1") ? 1 : name.EndsWith("_LOD2") ? 2 : name.EndsWith("_LOD3") ? 3 : 0;
                renderers[level].Add(renderer);
            }

            var lod = replacement.GetComponent<LODGroup>() ?? replacement.gameObject.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(.42f, renderers[0].ToArray()), new LOD(.22f, renderers[1].ToArray()),
                new LOD(.095f, renderers[2].ToArray()), new LOD(.037f, renderers[3].ToArray()) });
            lod.RecalculateBounds();
            model.gameObject.SetActive(false);
            model = replacement;
        }

        void Start() => player = FindAnyObjectByType<FirstPersonController>();

        internal void SetBreachTracing(bool active) => breachTracing = active;

        internal bool TryBreachDisable()
        {
            if (!CanBreach) return false;
            disabledAt = Time.time;
            disabledUntil = disabledAt + BreachDisableSeconds;
            cannon.Suspend();
            arms[0].Pose(0f);
            AnimateMalfunction();
            return true;
        }

        void AnimateMalfunction()
        {
            int frame = Mathf.Min(8, Mathf.FloorToInt((Time.time - disabledAt) * 12f));
            if (frame == malfunctionFrame) return;
            malfunctionFrame = frame;
            float brightness = !GameOptions.Active.reducedMotion && frame < 8 && frame % 2 == 0 ? 2.6f : .16f;
            foreach (var indicator in indicators)
                SetIndicator(indicator.Renderer, indicator.Slot, new Color(1f, .12f, .06f) * brightness);
        }

        void RestoreIndicators()
        {
            malfunctionFrame = -1;
            foreach (var indicator in indicators)
                SetIndicator(indicator.Renderer, indicator.Slot, indicator.Emission);
        }

        void SetIndicator(Renderer renderer, int slot, Color color)
        {
            if (!renderer) return;
            renderer.GetPropertyBlock(indicatorBlock, slot);
            indicatorBlock.SetColor(Emissive, color);
            renderer.SetPropertyBlock(indicatorBlock, slot);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || route == null || route.Length < 2) return;
            if (BreachRecovery > 0f)
            {
                AnimateMalfunction();
                return;
            }
            if (malfunctionFrame >= 0) RestoreIndicators();
            bool attacking = cannon != null && cannon.Track(player, dt);
            var displacement = route[target] - transform.position;
            var heading = attacking ? player.transform.position - transform.position : displacement;
            heading.y = 0f;
            var facing = Quaternion.LookRotation(heading.sqrMagnitude > .00001f ? heading : transform.forward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, dt * (attacking ? 205f : 115f));
            pause -= dt;
            // Maintain the patrol route during a live breach, including when the cannon tracks the player.
            bool moving = pause <= 0f && (attacking ? breachTracing : Quaternion.Angle(transform.rotation, facing) < 25f);
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
            head.Pose(attacking ? 0f : Mathf.Sin(Time.time * (moving ? .72f : 2.1f) + route[0].x) * (moving ? 8f : 23f));
            model.localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(gait)) * .032f * amount);
            cannon?.AimAndFire(player, dt);
        }
    }
}
