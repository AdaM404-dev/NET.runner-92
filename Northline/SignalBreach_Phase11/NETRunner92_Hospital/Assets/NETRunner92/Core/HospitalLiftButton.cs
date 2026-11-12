using UnityEngine;

namespace NETRunner92
{
    public sealed class HospitalLiftButton : HospitalInteractable
    {
        [SerializeField] HospitalLift lift;
        [SerializeField] int floor;
        [SerializeField] bool cabin;

        public override (string Key, string Text) Prompt => !lift.Unlocked(floor) ? (null, "Ezt a szintet előbb lépcsőn érd el")
            : lift.Busy ? (null, "A lift mozgásban van")
            : (HospitalKeys.Label(GameAction.Interact), cabin ? $"{floor + 1}. szint kiválasztása" : $"Lift hívása · {floor + 1}. szint");

        public void Configure(HospitalLift controller, int level, bool inside)
        {
            lift = controller;
            floor = level;
            cabin = inside;
        }

        public override void Use(FirstPersonController player) => lift.Request(floor, cabin);
    }
}
