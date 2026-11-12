using System;
using NetRunner.Core;
using UnityEngine;

namespace NetRunner.World
{
    /// <summary>
    /// The hospital's rooms by floor, read from
    /// <c>Environment/Hospital/RoomSchedule.json</c>: each room has a name, a
    /// floor (1 = ground) and the rectangular floor tiles it covers. Phase 11
    /// uses it to name the room of a save; the hospital checks use it to
    /// prove every room can be reached. The field names are the file's keys.
    /// </summary>
    [Serializable]
    public sealed class HospitalRoomSchedule
    {
        /// <summary>One room of the schedule.</summary>
        [Serializable]
        public sealed class Room
        {
            [SerializeField] private string name;
            [SerializeField] private int floor;
            [SerializeField] private Tile[] tiles;

            public string Name => name;

            /// <summary>The floor, 1 = ground.</summary>
            public int Floor => floor;

            public Tile[] Tiles => tiles;
        }

        /// <summary>A rectangle of floor in the scene's metres (x0 to x1, z0 to z1).</summary>
        [Serializable]
        public struct Tile
        {
            [SerializeField] private float x0;
            [SerializeField] private float x1;
            [SerializeField] private float z0;
            [SerializeField] private float z1;

            public float X0 => x0;

            public float X1 => x1;

            public float Z0 => z0;

            public float Z1 => z1;

            public bool Contains(Vector3 point) => RoomPlan.OnTile(point.x, point.z, x0, x1, z0, z1);
        }

        [SerializeField] private Room[] rooms;

        public Room[] Rooms => rooms;

        /// <summary>The room at <paramref name="point"/>, or null when it is in none.</summary>
        public Room At(Vector3 point)
        {
            int floor = RoomPlan.FloorAt(point.y, StairAccess.FloorHeight);
            foreach (var room in rooms)
            {
                if (room.Floor != floor)
                {
                    continue;
                }

                foreach (var tile in room.Tiles)
                {
                    if (tile.Contains(point))
                    {
                        return room;
                    }
                }
            }

            return null;
        }
    }
}
