using System;
using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;

namespace NetRunner.World
{
    /// <summary>
    /// One opening in the hospital: swing leaves, a sliding pair or the
    /// telescopic research barrier. E toggles it; it re-opens instead of
    /// closing on the player. A locked door prompts "Locked" and does not
    /// move. Each leaf blends between the closed and open pose stored in
    /// <see cref="Panel"/>.
    /// </summary>
    public sealed class HospitalDoor : MonoBehaviour, IInteractable
    {
        [Serializable]
        public struct Panel
        {
            public Transform leaf;
            public Vector3 closed, open;
            public Quaternion closedRotation, openRotation;
        }

        const float ShutPositionTolerance = .001f;
        const float ShutAngleTolerance = .1f;
        [SerializeField] Panel[] panels;
        [SerializeField] float width;
        [SerializeField] bool hinged;
        [SerializeField] bool liftOnly;
        [SerializeField] bool offline;
        [SerializeField] bool locked;
        float amount;
        bool requested;
        FirstPersonController actor;

        public bool Closed => amount <= 0f;
        public bool Open => amount >= 1f;
        public bool LiftOnly => liftOnly;
        public bool Locked => locked;
        public (GameAction? Action, string Text) Prompt => offline ? (null, "A lift üzemen kívül")
            : liftOnly ? (null, "Használd a lift hívógombját")
            : locked ? (null, "Zárva")
            : (GameAction.Interact, requested ? "Ajtó bezárása" : "Ajtó kinyitása");

        public void Configure(Panel[] leaves, float aperture, bool swing, bool controlledByLift, bool outOfService)
        {
            panels = leaves;
            width = aperture;
            hinged = swing;
            liftOnly = controlledByLift;
            offline = outOfService;
            Pose();
        }

        public bool Obstructed(FirstPersonController player)
        {
            var local = transform.InverseTransformPoint(player.transform.position);
            return Mathf.Abs(local.y) < .5f && Mathf.Abs(local.x) < width * .5f + .28f
                && Mathf.Abs(local.z) < (hinged ? 1.32f : .43f);
        }

        public void Interact(FirstPersonController player)
        {
            if (liftOnly || locked || (requested && Obstructed(player))) return;
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
        public void SetLocked(bool value) => locked = value;

        /// <summary>
        /// Whether every leaf stands at its closed pose, within 1 mm and 0.1°.
        /// A leaf saved in the scene at another pose would make the door start
        /// half open (#16).
        /// </summary>
        public bool IsPosedShut()
        {
            foreach (var panel in panels)
            {
                if (Vector3.Distance(panel.leaf.localPosition, panel.closed) > ShutPositionTolerance
                    || Quaternion.Angle(panel.leaf.localRotation, panel.closedRotation) > ShutAngleTolerance) return false;
            }
            return true;
        }

        public static void OpenOnDiagnosticPath(FirstPersonController player, Vector3 direction, bool immediate)
        {
            if (!Physics.Raycast(player.Eyes.transform.position, direction, out var hit, 1.4f, ~(1 << 2), QueryTriggerInteraction.Collide))
                return;
            var door = hit.collider.GetComponentInParent<HospitalDoor>();
            if (!door || door.liftOnly || door.locked) return;
            door.requested = true;
            if (immediate)
            {
                door.Advance(1f);
                Physics.SyncTransforms();
            }
        }

        public void Advance(float dt)
        {
            float next = Mathf.MoveTowards(amount, requested ? 1f : 0f, dt * (hinged ? 1.9f : 1.55f));
            if (next == amount) return;
            amount = next;
            Pose();
        }

        void Update()
        {
            if (liftOnly) return;
            if (amount > 0f && !requested && actor && Obstructed(actor)) requested = true;
            Advance(Time.deltaTime);
        }

        void Pose()
        {
            float smooth = amount * amount * (3f - 2f * amount);
            foreach (var panel in panels)
            {
                panel.leaf.SetLocalPositionAndRotation(Vector3.Lerp(panel.closed, panel.open, smooth),
                    Quaternion.Slerp(panel.closedRotation, panel.openRotation, smooth));
            }
        }
    }
}
