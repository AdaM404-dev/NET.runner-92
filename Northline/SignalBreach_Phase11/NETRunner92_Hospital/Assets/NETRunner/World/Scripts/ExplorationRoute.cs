using NetRunner.Core;
using UnityEngine;

namespace NetRunner.World
{
    /// <summary>
    /// The path through one stair leg, for the hospital checks and the route
    /// benchmark: out of the concourse door, down the vestibule to the
    /// flight, up (or down) both flights by the half landing, and back out on
    /// the other floor. Points are (x, z) on the floor, in metres.
    /// </summary>
    public static class ExplorationRoute
    {
        // The leg's door is 1.05 m beyond the core's centre (Core's StairRules
        // uses the same exit); the flights start 4.42 m in and turn at the half
        // landing 9.10 m in, 1.185 m either side of the core's centre.
        private const float DoorOffset = 1.05f;
        private const float FlightSide = 1.185f;
        private const float FlightStart = -4.42f;
        private const float HalfLanding = -9.10f;

        /// <summary>The leg's path round the stair core at <paramref name="core"/> (x), climbing or descending.</summary>
        public static Vector2[] StairLeg(int core, bool ascending)
        {
            float door = core + DoorOffset;
            float from = core + (ascending ? -FlightSide : FlightSide);
            float to = core + (ascending ? FlightSide : -FlightSide);
            return new[]
            {
                new Vector2(door, 0f), new Vector2(door, FlightStart),
                new Vector2(from, FlightStart), new Vector2(from, HalfLanding),
                new Vector2(to, HalfLanding), new Vector2(to, FlightStart),
                new Vector2(door, FlightStart), new Vector2(door, 0f),
            };
        }
    }
}
