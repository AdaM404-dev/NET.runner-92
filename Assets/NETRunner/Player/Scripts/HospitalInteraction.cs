using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.Player
{
    /// <summary>
    /// The player's E key and reticle: each frame it casts a ray from the
    /// eyes, <see cref="Reach"/> long, finds the <see cref="IInteractable"/>
    /// aimed at and shows its prompt; E uses it. While the LATCH-07 is up it
    /// gets the keys first, and while a terminal is open (<see cref="BeginTerminal"/>)
    /// the keyboard goes to the terminal instead. Prompts are drawn with OnGUI.
    /// </summary>
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class HospitalInteraction : MonoBehaviour
    {
        /// <summary>How far the player can reach, in metres from the eyes.</summary>
        public const float Reach = 2.65f;

        // The on-screen texts and their layout, in pixels.
        private const string LatchKey = "Q  LATCH-07";
        private const string CctvKey = "1  CCTV";
        private const string Reticle = "·";
        private const int FontSize = 16;
        private const float ReticleSize = 16f;
        private const float LineHeight = 30f;
        private const float Margin = 16f;
        private const float KeyRowRise = 48f;
        private const float LatchKeyWidth = 128f;
        private const float CctvKeyLeft = 140f;
        private const float CctvKeyWidth = 118f;
        private const float HintWidth = 380f;
        private const float HintRise = 62f;
        private const float PromptWidth = 374f;
        private const float PromptDrop = 26f;

        private static readonly Color TextColour = new Color(.88f, .95f, .97f);

        [Tooltip("Layers the aim ray can hit; by default everything but Ignore Raycast.")]
        [SerializeField] private LayerMask aimMask = Physics.DefaultRaycastLayers;

        private FirstPersonController player;
        private LatchViewmodel latch;
        private ITerminal terminal;
        private IInteractable target;
        private GUIStyle label;

        /// <summary>Whether a terminal has the keyboard.</summary>
        public bool TerminalActive => Alive(terminal);

        private void Awake()
        {
            player = GetComponent<FirstPersonController>();
            latch = GetComponent<LatchViewmodel>();
        }

        private void Update()
        {
            if (TerminalActive)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    terminal.ReadInput(Keyboard.current);
                }
                else
                {
                    terminal.Close();
                }

                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                target = null;
                if (latch && latch.Active)
                {
                    latch.Stow();
                }

                return;
            }

            target = TargetInView();
            if (latch && latch.ReadInput(Keyboard.current, target))
            {
                return;
            }

            if (Alive(target) && Keyboard.current?.eKey.wasPressedThisFrame == true)
            {
                target.Interact(player);
            }
        }

        private void OnGUI()
        {
            if (Cursor.lockState != CursorLockMode.Locked || TerminalActive)
            {
                return;
            }

            if (latch && latch.Active && !latch.CctvHintVisible)
            {
                return;
            }

            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = FontSize };
                label.normal.textColor = TextColour;
            }

            float centreX = Screen.width * .5f;
            float centreY = Screen.height * .5f;
            GUI.Label(new Rect(centreX - ReticleSize * .5f, centreY - ReticleSize * .5f, ReticleSize, ReticleSize), Reticle, label);
            if (latch)
            {
                GUI.Label(new Rect(Margin, Screen.height - KeyRowRise, LatchKeyWidth, LineHeight), LatchKey, label);
            }

            if (latch && latch.CctvAvailable)
            {
                GUI.Label(new Rect(CctvKeyLeft, Screen.height - KeyRowRise, CctvKeyWidth, LineHeight), CctvKey, label);
            }

            if (latch && latch.CctvHintVisible)
            {
                var hint = new Rect(centreX - HintWidth * .5f, centreY - HintRise, HintWidth, LineHeight);
                GUI.Box(hint, GUIContent.none);
                GUI.Label(hint, latch.CctvHint, label);
            }

            if ((latch && latch.Active) || !Alive(target))
            {
                return;
            }

            var box = new Rect(centreX - PromptWidth * .5f, centreY + PromptDrop, PromptWidth, LineHeight);
            GUI.Box(box, GUIContent.none);
            GUI.Label(box, target.Prompt, label);
        }

        /// <summary>Gives the keyboard to <paramref name="value"/> until <see cref="EndTerminal"/>.</summary>
        public void BeginTerminal(ITerminal value)
        {
            terminal = value;
        }

        /// <summary>Takes the keyboard back from <paramref name="value"/>, if it has it.</summary>
        public void EndTerminal(ITerminal value)
        {
            if (terminal == value)
            {
                terminal = null;
            }
        }

        /// <summary>The interactable the eyes aim at within reach, or null.</summary>
        public IInteractable TargetInView()
        {
            Transform view = player.Eyes.transform;
            return Physics.Raycast(view.position, view.forward, out RaycastHit hit, Reach, aimMask, QueryTriggerInteraction.Collide)
                ? hit.collider.GetComponentInParent<IInteractable>()
                : null;
        }

        // Interfaces skip Unity's "destroyed object equals null" check, so
        // ask the component itself whether it still exists.
        private static bool Alive(IInteractable value)
        {
            return value is Object component && component;
        }
    }
}
