using System.Collections.Generic;
using NetRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.Player
{
    /// <summary>
    /// What the player aims at and uses: each frame it finds the
    /// <see cref="IInteractable"/> in reach and the breachable robot in view,
    /// passes the keyboard to LATCH-07 or an open terminal, and uses the
    /// target when the interact key is pressed. The HUD reads
    /// <see cref="Target"/> and <see cref="BreachTarget"/> for its prompts.
    /// The breachable robots are set by the hospital's installer.
    /// </summary>
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class HospitalInteraction : MonoBehaviour
    {
        /// <summary>Metres the player reaches to use something.</summary>
        public const float Reach = 2.65f;

        [SerializeField] private LayerMask interactionLayers = Physics.DefaultRaycastLayers;
        [SerializeField] private MonoBehaviour[] breachables = new MonoBehaviour[0];

        private readonly List<IBreachable> breachTargets = new List<IBreachable>();
        private FirstPersonController player;
        private LatchViewmodel latch;
        private ITerminal terminal;
        private IInteractable target;

        public bool TerminalActive => Alive(terminal);

        /// <summary>The robot the player could breach now, or null.</summary>
        public IBreachable BreachTarget { get; private set; }

        public bool BreachOpen => latch && latch.BreachOpen;

        public bool BreachCapturesLook => latch && latch.BreachCapturesLook;

        /// <summary>The terminal that has the keyboard, or null.</summary>
        public ITerminal ActiveTerminal => TerminalActive ? terminal : null;

        /// <summary>What the player aims at within reach, or null.</summary>
        public IInteractable Target => Alive(target) ? target : null;

        private void Awake()
        {
            player = GetComponent<FirstPersonController>();
            latch = GetComponent<LatchViewmodel>();
            breachTargets.Clear();
            foreach (var behaviour in breachables)
            {
                if (behaviour is IBreachable breachable)
                {
                    breachTargets.Add(breachable);
                }
            }
        }

        private void Update()
        {
            if (player.IsDown)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (TerminalActive)
            {
                BreachTarget = null;
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    terminal.ReadInput(keyboard, player.Preferences);
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
                BreachTarget = null;
                if (latch && latch.Active)
                {
                    latch.Stow();
                }

                return;
            }

            target = TargetInView();
            BreachTarget = latch && !latch.BreachOpen && target is not LatchRelay ? BreachTargets.Find(player.Eyes, breachTargets) : null;
            if (latch && latch.ReadInput(keyboard, target, BreachTarget))
            {
                return;
            }

            if (Alive(target) && player.Preferences.Pressed(keyboard, GameAction.Interact))
            {
                target.Interact(player);
            }
        }

        /// <summary>Sets the robots the player can breach; called by the hospital's installer.</summary>
        public void SetBreachables(MonoBehaviour[] robots)
        {
            breachables = robots;
        }

        public void BeginTerminal(ITerminal value) => terminal = value;

        public void EndTerminal(ITerminal value)
        {
            if (terminal == value)
            {
                terminal = null;
            }
        }

        /// <summary>The interactable on the collider in the centre of the view within reach, or null.</summary>
        public IInteractable TargetInView()
        {
            var view = player.Eyes.transform;
            return Physics.Raycast(view.position, view.forward, out var hit, Reach, interactionLayers, QueryTriggerInteraction.Collide)
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
