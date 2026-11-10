using NetRunner.Player;
using UnityEngine;

namespace NetRunner.World
{
    public sealed class StairAccess : MonoBehaviour
    {
        public const float FloorHeight = 4.2f;
        [SerializeField] FirstPersonController player;
        int highest;

        public int Highest => highest;
        public static int CoreForLeg(int lowerFloor) => lowerFloor == 1 ? 32 : -32;
        public void Configure(FirstPersonController controller) => player = controller;

        void Update()
        {
            if (highest >= 3 || !player.IsGrounded) return;
            var p = player.transform.position;
            if (Mathf.Abs(p.y - (highest + 1) * FloorHeight) < .12f
                && Mathf.Abs(p.x - (CoreForLeg(highest) + 1.05f)) < 2f && p.z > -1.95f && p.z < 2.15f)
                highest++;
        }
    }
}
