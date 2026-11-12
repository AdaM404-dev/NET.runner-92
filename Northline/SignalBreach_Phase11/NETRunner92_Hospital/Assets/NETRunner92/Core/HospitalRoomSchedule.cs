using System;
using UnityEngine;

namespace NETRunner92
{
    [Serializable]
    internal sealed class HospitalRoomSchedule
    {
        [Serializable]
        internal sealed class Room
        {
            public string name;
            public int floor;
            public Tile[] tiles;
        }

        [Serializable]
        internal struct Tile
        {
            public float x0, x1, z0, z1;

            public bool Contains(Vector3 point) =>
                point.x >= x0 && point.x <= x1 && point.z >= z0 && point.z <= z1;
        }

        public Room[] rooms;

        public Room At(Vector3 point)
        {
            int floor = Mathf.FloorToInt((point.y + .20f) / StairAccess.FloorHeight) + 1;
            foreach (var room in rooms)
            {
                if (room.floor != floor) continue;
                foreach (var tile in room.tiles)
                    if (tile.Contains(point)) return room;
            }
            return null;
        }
    }
}
