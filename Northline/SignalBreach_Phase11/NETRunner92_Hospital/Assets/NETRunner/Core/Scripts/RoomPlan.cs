using System;

namespace NetRunner.Core
{
    /// <summary>
    /// Finding where a point is in the hospital's room schedule (Phase 11 uses
    /// it to name the room of a save): the floor from the height, and whether
    /// a point lies on a rectangular floor tile. Floors are numbered from 1;
    /// a point counts as on a floor from 0.2 m below its level, so a player
    /// standing on a stair landing a little low still counts as on it.
    /// </summary>
    public static class RoomPlan
    {
        /// <summary>Metres below a floor's level that still count as that floor.</summary>
        public const float FloorTolerance = .20f;

        /// <summary>The floor number (1 = ground) at height <paramref name="y"/>, with floors <paramref name="floorHeight"/> metres apart.</summary>
        public static int FloorAt(float y, float floorHeight)
        {
            return (int)MathF.Floor((y + FloorTolerance) / floorHeight) + 1;
        }

        /// <summary>Whether the point (x, z) lies on the tile, edges included.</summary>
        public static bool OnTile(float x, float z, float x0, float x1, float z0, float z1)
        {
            return x >= x0 && x <= x1 && z >= z0 && z <= z1;
        }
    }
}
