using UnityEngine;
using UnityEngine.InputSystem;

namespace NETRunner92
{
    public abstract class HospitalInteractable : MonoBehaviour
    {
        public abstract (string Key, string Text) Prompt { get; }
        public abstract void Use(FirstPersonController player);
    }

    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class HospitalInteraction : MonoBehaviour
    {
        public const float Reach = 2.65f;
        FirstPersonController player;
        LatchViewmodel latch;
        ReceptionSecurityTerminal terminal;
        HospitalInteractable target;
        public bool TerminalActive => terminal;
        internal K7Patrol BreachTarget { get; private set; }
        internal bool BreachOpen => latch && latch.BreachOpen;
        internal bool BreachCapturesLook => latch && latch.BreachCapturesLook;
        internal ReceptionSecurityTerminal ActiveTerminal => terminal;
        internal HospitalInteractable Target => target;

        public void BeginTerminal(ReceptionSecurityTerminal value) => terminal = value;
        public void EndTerminal(ReceptionSecurityTerminal value)
        {
            if (terminal == value) terminal = null;
        }

        void Awake()
        {
            player = GetComponent<FirstPersonController>();
            latch = GetComponent<LatchViewmodel>();
        }

        public HospitalInteractable TargetInView()
        {
            var view = player.Eyes.transform;
            return Physics.Raycast(view.position, view.forward, out var hit, Reach, ~(1 << 2), QueryTriggerInteraction.Collide)
                ? hit.collider.GetComponentInParent<HospitalInteractable>() : null;
        }

        void Update()
        {
            if (player.IsDown) return;
            if (terminal)
            {
                BreachTarget = null;
                if (Cursor.lockState == CursorLockMode.Locked) terminal.ReadInput(Keyboard.current);
                else terminal.Close();
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                target = null;
                BreachTarget = null;
                if (latch && latch.Active) latch.Stow();
                return;
            }
            target = TargetInView();
            BreachTarget = latch && !latch.BreachOpen && target is not LatchRelay ? K7Patrol.FindBreachTarget(player.Eyes) : null;
            if (latch && latch.ReadInput(Keyboard.current, target, BreachTarget)) return;
            if (target && HospitalKeys.Pressed(Keyboard.current, GameAction.Interact)) target.Use(player);
        }

    }
}
