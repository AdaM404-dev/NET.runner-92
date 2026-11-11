using NetRunner.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.Player
{
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class HospitalInteraction : MonoBehaviour
    {
        public const float Reach = 2.65f;
        FirstPersonController player;
        LatchViewmodel latch;
        ITerminal terminal;
        IInteractable target;
        public bool TerminalActive => Alive(terminal);
        public IBreachable BreachTarget { get; private set; }
        public bool BreachOpen => latch && latch.BreachOpen;
        public bool BreachCapturesLook => latch && latch.BreachCapturesLook;
        public ITerminal ActiveTerminal => TerminalActive ? terminal : null;
        public IInteractable Target => Alive(target) ? target : null;

        public void BeginTerminal(ITerminal value) => terminal = value;
        public void EndTerminal(ITerminal value)
        {
            if (terminal == value) terminal = null;
        }

        // Interfaces skip Unity's "destroyed object equals null" check, so
        // ask the component itself whether it still exists.
        static bool Alive(IInteractable value) => value is Object component && component;

        void Awake()
        {
            player = GetComponent<FirstPersonController>();
            latch = GetComponent<LatchViewmodel>();
        }

        public IInteractable TargetInView()
        {
            var view = player.Eyes.transform;
            return Physics.Raycast(view.position, view.forward, out var hit, Reach, ~(1 << 2), QueryTriggerInteraction.Collide)
                ? hit.collider.GetComponentInParent<IInteractable>() : null;
        }

        void Update()
        {
            if (player.IsDown) return;
            if (TerminalActive)
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
            BreachTarget = latch && !latch.BreachOpen && target is not LatchRelay ? BreachTargets.Find(player.Eyes) : null;
            if (latch && latch.ReadInput(Keyboard.current, target, BreachTarget)) return;
            if (Alive(target) && HospitalKeys.Pressed(Keyboard.current, GameAction.Interact)) target.Interact(player);
        }
    }
}
