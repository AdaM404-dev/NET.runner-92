using UnityEngine;
using UnityEngine.InputSystem;

namespace NETRunner92
{
    public abstract class HospitalInteractable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public abstract void Use(FirstPersonController player);
    }

    [RequireComponent(typeof(FirstPersonController))]
    public sealed class HospitalInteraction : MonoBehaviour
    {
        public const float Reach = 2.65f;
        FirstPersonController player;
        LatchViewmodel latch;
        ReceptionSecurityTerminal terminal;
        HospitalInteractable target;
        GUIStyle label;
        public bool TerminalActive => terminal;

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
            if (terminal)
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
            if (target && Keyboard.current?.eKey.wasPressedThisFrame == true) target.Use(player);
        }

        void OnGUI()
        {
            if (Cursor.lockState != CursorLockMode.Locked || terminal) return;
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
            if (!target) return;
            var box = new Rect(Screen.width * .5f - 187, Screen.height * .5f + 26, 374, 30);
            GUI.Box(box, GUIContent.none);
            GUI.Label(box, target.Prompt, label);
        }
    }
}
