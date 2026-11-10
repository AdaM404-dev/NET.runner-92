using UnityEngine;

namespace NETRunner92
{
    static class ExplorationRoute
    {
        public static Vector2[] StairLeg(int core, bool ascending)
        {
            float door = core + 1.05f;
            float from = core + (ascending ? -1.185f : 1.185f);
            float to = core + (ascending ? 1.185f : -1.185f);
            return new[]
            {
                new Vector2(door, 0f), new Vector2(door, -4.42f),
                new Vector2(from, -4.42f), new Vector2(from, -9.10f),
                new Vector2(to, -9.10f), new Vector2(to, -4.42f),
                new Vector2(door, -4.42f), new Vector2(door, 0f)
            };
        }
    }
}

