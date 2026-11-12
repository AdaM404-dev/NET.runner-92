using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;

namespace NetRunner.World
{
    public sealed class HospitalLiftButton : MonoBehaviour, IInteractable
    {
        [SerializeField] HospitalLift lift;
        [SerializeField] int floor;
        [SerializeField] bool cabin;

        public (GameAction? Action, string Text) Prompt => !lift.Unlocked(floor) ? (null, "Ezt a szintet előbb lépcsőn érd el")
            : lift.Busy ? (null, "A lift mozgásban van")
            : (GameAction.Interact, cabin ? $"{floor + 1}. szint kiválasztása" : $"Lift hívása · {floor + 1}. szint");

        public void Configure(HospitalLift controller, int level, bool inside)
        {
            lift = controller;
            floor = level;
            cabin = inside;
        }

        public void Interact(FirstPersonController player) => lift.Request(floor, cabin);
    }
}
