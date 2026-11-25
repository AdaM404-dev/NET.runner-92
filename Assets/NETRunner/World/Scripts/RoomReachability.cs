#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NetRunner.Core;
using UnityEngine;

namespace NetRunner.World
{
    /// <summary>
    /// A check for the hospital checks: from the centre of each floor it
    /// floods outwards over the floor in 0.32 m steps wherever a player's
    /// capsule fits, and reports which rooms of the room schedule it reached.
    /// Editor only; it reads <c>RoomSchedule.json</c> from the art folder.
    /// </summary>
    public static class RoomReachability
    {
        [Serializable]
        private sealed class Plan
        {
            [SerializeField] private Room[] rooms;

            public Room[] Rooms => rooms;
        }

        [Serializable]
        private sealed class Room
        {
            [SerializeField] private string name;
            [SerializeField] private int floor;
            [SerializeField] private Tile[] tiles;

            public string Name => name;

            public int Floor => floor;

            public Tile[] Tiles => tiles;
        }

        [Serializable]
        private sealed class Tile
        {
            [SerializeField] private float x0;
            [SerializeField] private float x1;
            [SerializeField] private float z0;
            [SerializeField] private float z1;

            // Whether the point is on the tile, more than the margin inside its edges.
            public bool HasInside(Vector2 point, float margin)
            {
                return point.x > x0 + margin && point.x < x1 - margin && point.y > z0 + margin && point.y < z1 - margin;
            }
        }

        private const string ScheduleFile = "NETRunner92/Environment/Hospital/RoomSchedule.json";
        private const string WalkableLayer = "Default";
        private const int Floors = 4;

        // The grid: 0.32 m steps, 113 either way along x and 61 along z from the centre.
        private const float Pitch = .32f;
        private const int HalfStepsX = 113;
        private const int HalfStepsZ = 61;

        // A floor sample: a ray from 0.10 m above the floor's height, 0.20 m long,
        // must find floor within 0.065 m of it; then a capsule from 0.31 to 1.49 m
        // up, 0.275 m wide, must fit. A room counts when a sample lies 0.28 m inside one of its tiles.
        private const float RayStart = .10f;
        private const float RayLength = .20f;
        private const float FloorTolerance = .065f;
        private const float CapsuleBottom = .31f;
        private const float CapsuleTop = 1.49f;
        private const float CapsuleRadius = .275f;
        private const float RoomMargin = .28f;

        private static readonly int[] StepX = { 1, 0, -1, 0 };
        private static readonly int[] StepZ = { 0, 1, 0, -1 };

        /// <summary>One line per floor with the rooms reached, then any rooms missed.</summary>
        public static string Probe()
        {
            string file = Path.Combine(Application.dataPath, ScheduleFile);
            Plan plan = JsonUtility.FromJson<Plan>(File.ReadAllText(file));
            int walkable = LayerMask.GetMask(WalkableLayer);
            var missed = new List<string>();
            var totals = new List<string>();
            for (int level = 1; level <= Floors; level++)
            {
                float height = (level - 1) * StairRules.FloorHeight;
                List<Vector2> visited = Flood(height, walkable);
                int count = 0;
                int found = 0;
                foreach (Room room in plan.Rooms)
                {
                    if (room.Floor != level)
                    {
                        continue;
                    }

                    count++;
                    bool hit = visited.Exists(point => Array.Exists(room.Tiles, tile => tile.HasInside(point, RoomMargin)));
                    if (hit)
                    {
                        found++;
                    }
                    else
                    {
                        missed.Add($"L{level} {room.Name}");
                    }
                }

                totals.Add($"L{level} {found}/{count} ({visited.Count} accessible samples)");
            }

            return string.Join("; ", totals) + (missed.Count == 0 ? string.Empty : " | Missing: " + string.Join(", ", missed));
        }

        // Every floor sample reachable from the centre of the floor at this height.
        private static List<Vector2> Flood(float height, int walkable)
        {
            var reached = new byte[2 * HalfStepsX + 1, 2 * HalfStepsZ + 1];
            var open = new Queue<Vector2Int>();
            var visited = new List<Vector2>();
            open.Enqueue(Vector2Int.zero);
            reached[HalfStepsX, HalfStepsZ] = 1;
            while (open.Count > 0)
            {
                Vector2Int cell = open.Dequeue();
                float x = cell.x * Pitch;
                float z = cell.y * Pitch;
                if (!Physics.Raycast(new Vector3(x, height + RayStart, z), Vector3.down, out RaycastHit floor, RayLength, walkable,
                        QueryTriggerInteraction.Ignore)
                    || Mathf.Abs(floor.point.y - height) > FloorTolerance)
                {
                    continue;
                }

                if (Physics.CheckCapsule(new Vector3(x, floor.point.y + CapsuleBottom, z), new Vector3(x, floor.point.y + CapsuleTop, z),
                        CapsuleRadius, walkable, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                visited.Add(new Vector2(x, z));
                for (int i = 0; i < StepX.Length; i++)
                {
                    int nextX = cell.x + StepX[i];
                    int nextZ = cell.y + StepZ[i];
                    if (Mathf.Abs(nextX) > HalfStepsX || Mathf.Abs(nextZ) > HalfStepsZ || reached[nextX + HalfStepsX, nextZ + HalfStepsZ] != 0)
                    {
                        continue;
                    }

                    reached[nextX + HalfStepsX, nextZ + HalfStepsZ] = 1;
                    open.Enqueue(new Vector2Int(nextX, nextZ));
                }
            }

            return visited;
        }
    }
}
#endif
