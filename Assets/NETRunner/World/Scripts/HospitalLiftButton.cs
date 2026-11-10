using NetRunner.Player;
using UnityEngine;

namespace NetRunner.World
{
    public sealed class HospitalLiftButton : HospitalInteractable
    {
        [SerializeField] HospitalLift lift;
        [SerializeField] int floor;
        [SerializeField] bool cabin;

        public override string Prompt => !lift.Unlocked(floor) ? $"L{floor + 1}  Reach this level by stairs"
            : lift.Busy ? "Lift in transit" : cabin ? $"E  Select level {floor + 1}" : $"E  Call lift to level {floor + 1}";

        public void Configure(HospitalLift controller, int level, bool inside)
        {
            lift = controller;
            floor = level;
            cabin = inside;
        }

        public override void Use(FirstPersonController player) => lift.Request(floor, cabin);
    }
}
