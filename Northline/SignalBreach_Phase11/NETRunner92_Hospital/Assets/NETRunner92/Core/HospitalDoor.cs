using System;
using UnityEngine;

namespace NETRunner92
{
    public sealed class HospitalDoor : HospitalInteractable
    {
        [Serializable]
        public struct Panel
        {
            public Transform leaf;
            public Vector3 closed, open;
            public Quaternion closedRotation, openRotation;
        }

        [SerializeField] Panel[] panels;
        [SerializeField] float width;
        [SerializeField] bool hinged;
        [SerializeField] bool liftOnly;
        [SerializeField] bool offline;
        float amount;
        bool requested;
        FirstPersonController actor;

        public bool Closed => amount <= 0f;
        public bool Open => amount >= 1f;
        public bool LiftOnly => liftOnly;
        public override (string Key, string Text) Prompt => offline ? (null, "A lift üzemen kívül")
            : liftOnly ? (null, "Használd a lift hívógombját")
            : (HospitalKeys.Label(GameAction.Interact), requested ? "Ajtó bezárása" : "Ajtó kinyitása");

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

        public override void Use(FirstPersonController player)
        {
            if (liftOnly || (requested && Obstructed(player))) return;
            actor = player;
            requested = !requested;
        }

        internal bool SaveOpen => !liftOnly && (requested || amount > 0);
        internal void RestoreOpen()
        {
            if (liftOnly) return;
            requested = true;
            amount = 1;
            Pose();
        }

        public void SetLiftOpen(bool open) => requested = open && !offline;

        public static void OpenOnDiagnosticPath(FirstPersonController player, Vector3 direction, bool immediate)
        {
            if (!Physics.Raycast(player.Eyes.transform.position, direction, out var hit, 1.4f, ~(1 << 2), QueryTriggerInteraction.Collide))
                return;
            var door = hit.collider.GetComponentInParent<HospitalDoor>();
            if (!door || door.liftOnly) return;
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
