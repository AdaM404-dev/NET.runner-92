using System;
using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;

namespace NetRunner.World
{
    /// <summary>
    /// One opening in the hospital: swing leaves, a sliding pair or the
    /// telescopic research barrier. E toggles it; it opens again instead of
    /// closing on the player. A locked door prompts "Locked" and does not
    /// move. Each leaf eases between the closed and open pose stored in
    /// <see cref="Panel"/>. Lift doors move only for the lift. What it says,
    /// when it moves and how fast is Core's <see cref="DoorRules"/>.
    /// </summary>
    public sealed class HospitalDoor : MonoBehaviour, IInteractable
    {
        /// <summary>One moving leaf and its closed and open poses, in its parent's space.</summary>
        [Serializable]
        public struct Panel
        {
            [SerializeField] private Transform leaf;
            [SerializeField] private Vector3 closed;
            [SerializeField] private Vector3 open;
            [SerializeField] private Quaternion closedRotation;
            [SerializeField] private Quaternion openRotation;

            /// <summary>A leaf with its closed and open position and rotation.</summary>
            public Panel(Transform leaf, Vector3 closed, Vector3 open, Quaternion closedRotation, Quaternion openRotation)
            {
                this.leaf = leaf;
                this.closed = closed;
                this.open = open;
                this.closedRotation = closedRotation;
                this.openRotation = openRotation;
            }

            /// <summary>The leaf that moves.</summary>
            public Transform Leaf => leaf;

            /// <summary>Its position when the door is shut.</summary>
            public Vector3 Closed => closed;

            /// <summary>Its position when the door is open.</summary>
            public Vector3 Open => open;

            /// <summary>Its rotation when the door is shut.</summary>
            public Quaternion ClosedRotation => closedRotation;

            /// <summary>Its rotation when the door is open.</summary>
            public Quaternion OpenRotation => openRotation;
        }

        private const float ShutPositionTolerance = .001f;
        private const float ShutAngleTolerance = .1f;

        // The hospital checks open doors this far ahead of a walked path.
        private const float DiagnosticReach = 1.4f;

        [SerializeField] private Panel[] panels;
        [SerializeField] private float width;
        [SerializeField] private bool hinged;
        [SerializeField] private bool liftOnly;
        [SerializeField] private bool offline;
        [SerializeField] private bool locked;

        [Tooltip("Layers whose door colliders the diagnostic route may open.")]
        [SerializeField] private LayerMask diagnosticLayers = Physics.DefaultRaycastLayers;

        private float amount;
        private bool requested;
        private FirstPersonController actor;

        /// <summary>Whether it is fully shut.</summary>
        public bool Closed => amount <= 0f;

        /// <summary>Whether it is fully open.</summary>
        public bool Open => amount >= 1f;

        /// <summary>Whether it is a lift door, moved only by the lift.</summary>
        public bool LiftOnly => liftOnly;

        /// <summary>Whether it is locked.</summary>
        public bool Locked => locked;
        public (GameAction? Action, string Text) Prompt => offline ? (null, "A lift üzemen kívül")
            : liftOnly ? (null, "Használd a lift hívógombját")
            : locked ? (null, "Zárva")
            : (GameAction.Interact, requested ? "Ajtó bezárása" : "Ajtó kinyitása");


        private void Update()
        {
            if (liftOnly)
            {
                return;
            }

            if (amount > 0f && !requested && actor && Obstructed(actor))
            {
                requested = true;
            }

            Advance(Time.deltaTime);
        }

        /// <summary>
        /// Opens the first door that a ray from the player's eyes meets within
        /// 1.4 m along <paramref name="direction"/>, unless it is a lift or
        /// locked door; at once when <paramref name="immediate"/>. The
        /// hospital checks and the route benchmark walk through doors with it.
        /// </summary>
        public static void OpenOnDiagnosticPath(FirstPersonController player, Vector3 direction, bool immediate)
        {
            if (!Physics.Raycast(player.Eyes.transform.position, direction, out RaycastHit hit, DiagnosticReach,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                return;
            }

            HospitalDoor door = hit.collider.GetComponentInParent<HospitalDoor>();
            if (!door || (door.diagnosticLayers.value & (1 << hit.collider.gameObject.layer)) == 0 || door.liftOnly || door.locked)
            {
                return;
            }

            door.requested = true;
            if (immediate)
            {
                door.Advance(1f);
                Physics.SyncTransforms();
            }
        }

        /// <summary>Sets the leaves, the opening's width and what kind of door it is; the scene builder calls it.</summary>
        public void Configure(Panel[] leaves, float aperture, bool swing, bool controlledByLift, bool outOfService)
        {
            panels = leaves;
            width = aperture;
            hinged = swing;
            liftOnly = controlledByLift;
            offline = outOfService;
            Pose();
        }

        /// <summary>Whether <paramref name="player"/> stands in the doorway, where the door never closes.</summary>
        public bool Obstructed(FirstPersonController player)
        {
            Vector3 local = transform.InverseTransformPoint(player.transform.position);
            return DoorRules.InDoorway(local.x, local.y, local.z, width, hinged);
        }

        /// <inheritdoc/>
        public void Interact(FirstPersonController player)
        {
            if (!DoorRules.AnswersUse(liftOnly, locked, requested, Obstructed(player)))
            {
                return;
            }

            actor = player;
            requested = !requested;
        }

        public bool SaveOpen => !liftOnly && (requested || amount > 0);
        public void RestoreOpen()
        {
            if (liftOnly) return;
            requested = true;
            amount = 1;
            Pose();
        }

        public void SetLiftOpen(bool open) => requested = open && !offline;

        /// <summary>
        /// Locks or unlocks the door. Locking does not close it; a locked door
        /// only stops answering E.
        /// </summary>
        public void SetLocked(bool value)
        {
            locked = value;
        }

        /// <summary>
        /// Whether every leaf stands at its closed pose, within 1 mm and 0.1°.
        /// A leaf saved in the scene at another pose would make the door start
        /// half open (#16).
        /// </summary>
        public bool IsPosedShut()
        {
            foreach (Panel panel in panels)
            {
                if (Vector3.Distance(panel.Leaf.localPosition, panel.Closed) > ShutPositionTolerance
                    || Quaternion.Angle(panel.Leaf.localRotation, panel.ClosedRotation) > ShutAngleTolerance)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Moves the leaves on by <paramref name="dt"/> seconds; the lift moves its doors with it.</summary>
        public void Advance(float dt)
        {
            float next = DoorRules.Advance(amount, requested, hinged, dt);
            if (next == amount)
            {
                return;
            }

            amount = next;
            Pose();
        }

        private void Pose()
        {
            float eased = Easing.SmoothStep01(amount);
            foreach (Panel panel in panels)
            {
                panel.Leaf.SetLocalPositionAndRotation(Vector3.Lerp(panel.Closed, panel.Open, eased),
                    Quaternion.Slerp(panel.ClosedRotation, panel.OpenRotation, eased));
            }
        }
    }
}
