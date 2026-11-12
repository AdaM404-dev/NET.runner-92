using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;

namespace NetRunner.World
{
    /// <summary>
    /// A lift button: a call button on a landing, or a floor button in the
    /// cabin. E asks the lift for its floor; the prompt says whether that
    /// works (Core's <see cref="LiftCar.ButtonPrompt"/>).
    /// </summary>
    public sealed class HospitalLiftButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private HospitalLift lift;
        [SerializeField] private int floor;
        [SerializeField] private bool cabin;

        public (GameAction? Action, string Text) Prompt => !lift.Unlocked(floor) ? (null, "Ezt a szintet előbb lépcsőn érd el")
            : lift.Busy ? (null, "A lift mozgásban van")
            : (GameAction.Interact, cabin ? $"{floor + 1}. szint kiválasztása" : $"Lift hívása · {floor + 1}. szint");

        /// <summary>Sets the lift, the floor (0-based) and whether the button is in the cabin; the scene builder calls it.</summary>
        public void Configure(HospitalLift controller, int level, bool inside)
        {
            lift = controller;
            floor = level;
            cabin = inside;
        }

        /// <inheritdoc/>
        public void Interact(FirstPersonController player)
        {
            lift.Request(floor, cabin);
        }
    }
}
