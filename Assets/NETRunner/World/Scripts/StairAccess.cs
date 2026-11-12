using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;

namespace NetRunner.World
{
    /// <summary>
    /// Unlocks floors in the lift as the player first climbs to them: each
    /// frame it asks Core's <see cref="StairRules"/> whether the player stands
    /// on the next floor at the exit of the stair that leads there.
    /// </summary>
    public sealed class StairAccess : MonoBehaviour
    {
        /// <summary>Metres from one floor to the next; the scene builders and the checks use it.</summary>
        public const float FloorHeight = StairRules.FloorHeight;

        [SerializeField] private FirstPersonController player;

        private int highest;

        /// <summary>The highest floor reached so far (0-based); a fresh load starts on L1.</summary>
        public int Highest => highest;

        private void Update()
        {
            Vector3 position = player.transform.position;
            highest = StairRules.Reached(highest, player.IsGrounded, position.x, position.y, position.z);
        }

        /// <summary>The x of the stair core that climbs from <paramref name="lowerFloor"/>; the scene builders and the checks use it.</summary>
        public static int CoreForLeg(int lowerFloor)
        {
            return StairRules.CoreForLeg(lowerFloor);
        }

        /// <summary>Sets the player whose climb counts; the scene builder calls it.</summary>
        public void Configure(FirstPersonController controller)
        {
            player = controller;
        }
    }
}
