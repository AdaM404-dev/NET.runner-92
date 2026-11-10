using UnityEngine.InputSystem;
using UnityEngine;

namespace NetRunner.Player
{
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class HospitalInteraction : MonoBehaviour
    {
        public const float Reach = 2.65f;
        FirstPersonController player;
        LatchViewmodel latch;
        ITerminal terminal;
        IInteractable target;
        GUIStyle label;
        public bool TerminalActive => Alive(terminal);

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
            if (TerminalActive)
            {
                if (Cursor.lockState == CursorLockMode.Locked) terminal.ReadInput(Keyboard.current);
                else terminal.Close();
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                target = null;
                if (latch && latch.Active) latch.Stow();
                return;
            }
            target = TargetInView();
            if (latch && latch.ReadInput(Keyboard.current, target)) return;
            if (Alive(target) && Keyboard.current?.eKey.wasPressedThisFrame == true) target.Interact(player);
        }

        void OnGUI()
        {
            if (Cursor.lockState != CursorLockMode.Locked || TerminalActive) return;
            if (latch && latch.Active && !latch.CctvHintVisible) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
                label.normal.textColor = new Color(.88f, .95f, .97f);
            }
            GUI.Label(new Rect(Screen.width * .5f - 8, Screen.height * .5f - 8, 16, 16), "·", label);
            if (latch) GUI.Label(new Rect(16, Screen.height - 48, 128, 30), "Q  LATCH-07", label);
            if (latch && latch.CctvAvailable)
                GUI.Label(new Rect(140, Screen.height - 48, 118, 30), "1  CCTV", label);
            if (latch && latch.CctvHintVisible)
            {
                var hint = new Rect(Screen.width * .5f - 190, Screen.height * .5f - 62, 380, 30);
                GUI.Box(hint, GUIContent.none);
                GUI.Label(hint, latch.CctvHint, label);
            }
            if (latch && latch.Active) return;
            if (!Alive(target)) return;
            var box = new Rect(Screen.width * .5f - 187, Screen.height * .5f + 26, 374, 30);
            GUI.Box(box, GUIContent.none);
            GUI.Label(box, target.Prompt, label);
        }
    }
}
