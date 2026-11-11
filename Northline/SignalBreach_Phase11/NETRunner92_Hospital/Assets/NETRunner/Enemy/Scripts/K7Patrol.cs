using System.Collections.Generic;
using NetRunner.Core;
using NetRunner.Player;
using NetRunner.Settings;
using UnityEngine;
using UnityEngine.Rendering;

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
    /// Since Phase 11 it carries the forearm cannon (<see cref="K7ForearmCannon"/>):
    /// while the cannon tracks the player the robot faces the player, and
    /// LATCH-07's Signal Breach (<see cref="IBreachable"/>) can switch it off
    /// for <see cref="BreachDisableSeconds"/>.
    /// </summary>
    public sealed class K7Patrol : MonoBehaviour, IBreachable
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

        /// <summary>Seconds a successful Signal Breach switches the robot off.</summary>
        public const float BreachDisableSeconds = 10f;

        private const string CombatModel = "K7_HospitalCombat";
        private const string StatusLightMaterial = "MAT_K7_StatusLight";
        private const string BreachBone = "Chest";

        // The malfunction blinks the status lights at 12 frames a second for 8 frames, then holds them dim.
        private const float MalfunctionFramesPerSecond = 12f;
        private const int MalfunctionFrames = 8;
        private const float MalfunctionBright = 2.6f;
        private const float MalfunctionDim = .16f;

        private static readonly int Emissive = Shader.PropertyToID("_EmissiveColor");
        private static readonly Color MalfunctionColor = new Color(1f, .12f, .06f);

        [SerializeField] private Transform model;
        [SerializeField] private Vector3[] route;
        [SerializeField] private float speed = 1.10f;

        private readonly Hinge[] thighs = new Hinge[2];
        private readonly Hinge[] knees = new Hinge[2];
        private readonly Hinge[] feet = new Hinge[2];
        private readonly Hinge[] arms = new Hinge[2];
        private readonly List<(Renderer Renderer, int Slot, Color Emission)> indicators = new List<(Renderer, int, Color)>();
        private Hinge head;
        private Hinge pelvis;
        private Patrol patrol;
        private float height;
        private float scanOffset;
        private FirstPersonController player;
        private K7ForearmCannon cannon;
        private Transform breachPoint;
        private bool breachTracing;
        private float disabledAt;
        private float disabledUntil;
        private int malfunctionFrame = -1;
        private MaterialPropertyBlock indicatorBlock;

        /// <summary>The route in the scene, first point to last; the robot walks it back and forth.</summary>
        public IReadOnlyList<Vector3> Waypoints => route;

        public float BreachRecovery => Mathf.Max(0f, disabledUntil - Time.time);

        public bool CanBreach => isActiveAndEnabled && cannon != null && BreachRecovery <= 0f;

        public float DisableSeconds => BreachDisableSeconds;

        public Vector3 BreachPoint => breachPoint.position;

        private void Awake()
        {
            InstallCombatModel();

            // The FBX imports with its visor towards -Z; turn the model round to look the way the robot walks.
            model.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.localRotation;
            var parts = new Dictionary<string, Transform>();
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                parts.TryAdd(bone.name, bone);
                AddHinge(bone);
            }

            cannon = K7ForearmCannon.Create(transform, parts);
            breachPoint = parts[BreachBone];
            FindStatusLights();
            indicatorBlock = new MaterialPropertyBlock();

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
            BreachTargets.Add(this);
        }

        private void Start()
        {
            // Temporary (#55 step 3): the scene builder will set this reference instead.
            player = FindAnyObjectByType<FirstPersonController>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            if (BreachRecovery > 0f)
            {
                AnimateMalfunction();
                return;
            }

            if (malfunctionFrame >= 0)
            {
                RestoreIndicators();
            }

            bool attacking = cannon != null && cannon.Track(player, dt);
            (float X, float Z)? target = null;
            if (attacking)
            {
                var aim = player.transform.position;
                target = (aim.x, aim.z);
            }

            // During a live breach the robot keeps walking its route, even while the cannon tracks the player.
            patrol.Step(dt, target, breachTracing);
            var (x, z) = patrol.Position;
            transform.SetPositionAndRotation(new Vector3(x, height, z), Quaternion.Euler(0f, patrol.Heading, 0f));
            Pose(patrol.DistanceWalked, patrol.IsWalking, Time.time, attacking);
            cannon?.AimAndFire(player, dt);
        }

        private void OnDisable()
        {
            BreachTargets.Remove(this);
            if (malfunctionFrame >= 0)
            {
                RestoreIndicators();
            }
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

        public void SetBreachTracing(bool active) => breachTracing = active;

        public bool TryBreachDisable()
        {
            if (!CanBreach)
            {
                return false;
            }

            disabledAt = Time.time;
            disabledUntil = disabledAt + BreachDisableSeconds;
            cannon.Suspend();
            arms[K7Gait.Left].Pose(0f);
            AnimateMalfunction();
            return true;
        }

        // Phase 11 swaps the scene's patrol model for the combat model with the cannon, keeping its place.
        private void InstallCombatModel()
        {
            var prefab = Resources.Load<GameObject>(CombatModel);
            if (!prefab)
            {
                Debug.LogError("The K-7 combat model is missing from Resources.", this);
                return;
            }

            var replacement = Instantiate(prefab, model.parent, false).transform;
            replacement.SetLocalPositionAndRotation(model.localPosition, model.localRotation);
            replacement.localScale = model.localScale;
            if (replacement.TryGetComponent<Animator>(out var animator))
            {
                animator.enabled = false;
            }

            var renderers = new[] { new List<Renderer>(), new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };
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
            lod.SetLODs(new[]
            {
                new LOD(.42f, renderers[0].ToArray()),
                new LOD(.22f, renderers[1].ToArray()),
                new LOD(.095f, renderers[2].ToArray()),
                new LOD(.037f, renderers[3].ToArray()),
            });
            lod.RecalculateBounds();
            model.gameObject.SetActive(false);
            model = replacement;
        }

        private void FindStatusLights()
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    var material = materials[slot];
                    if (material && material.name == StatusLightMaterial)
                    {
                        indicators.Add((renderer, slot, material.GetColor(Emissive)));
                    }
                }
            }
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

        private void Pose(float distanceWalked, bool walking, float time, bool attacking)
        {
            for (int side = K7Gait.Left; side <= K7Gait.Right; side++)
            {
                thighs[side].Pose(K7Gait.Thigh(distanceWalked, side, walking));
                knees[side].Pose(K7Gait.Knee(distanceWalked, side, walking));
                feet[side].Pose(K7Gait.Foot(distanceWalked, side, walking));
                arms[side].Pose(K7Gait.Arm(distanceWalked, side, walking));
            }

            pelvis.Pose(K7Gait.Pelvis(distanceWalked, walking));

            // While it attacks, the head looks straight ahead along the body that faces the player.
            head.Pose(attacking ? 0f : K7Gait.Head(time, scanOffset, walking));
            model.localPosition = Vector3.up * K7Gait.Bob(distanceWalked, walking);
        }

        private void AnimateMalfunction()
        {
            int frame = Mathf.Min(MalfunctionFrames, Mathf.FloorToInt((Time.time - disabledAt) * MalfunctionFramesPerSecond));
            if (frame == malfunctionFrame)
            {
                return;
            }

            malfunctionFrame = frame;
            bool blink = !GameOptions.Active.reducedMotion && frame < MalfunctionFrames && frame % 2 == 0;
            foreach (var indicator in indicators)
            {
                SetIndicator(indicator.Renderer, indicator.Slot, MalfunctionColor * (blink ? MalfunctionBright : MalfunctionDim));
            }
        }

        private void RestoreIndicators()
        {
            malfunctionFrame = -1;
            foreach (var indicator in indicators)
            {
                SetIndicator(indicator.Renderer, indicator.Slot, indicator.Emission);
            }
        }

        private void SetIndicator(Renderer renderer, int slot, Color color)
        {
            if (!renderer)
            {
                return;
            }

            renderer.GetPropertyBlock(indicatorBlock, slot);
            indicatorBlock.SetColor(Emissive, color);
            renderer.SetPropertyBlock(indicatorBlock, slot);
        }
    }
}
