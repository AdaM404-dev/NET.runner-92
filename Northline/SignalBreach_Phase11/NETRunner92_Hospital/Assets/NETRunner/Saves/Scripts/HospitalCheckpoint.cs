using System;
using NetRunner.Core;
using UnityEngine;

namespace NetRunner.Saves
{
    /// <summary>
    /// One save of a hospital run (Phase 11): when it was saved, where the
    /// player stood and looked, how far they got (highest floor, CCTV
    /// unlocked, relay linked), the room's name for the menu, and the doors
    /// that stood open. Stored as JSON by <see cref="CheckpointStore"/>; the
    /// field names are the file's keys, so they must not be renamed.
    /// </summary>
    [Serializable]
    public sealed class HospitalCheckpoint
    {
        [SerializeField] private int version = CheckpointRules.Version;
        [SerializeField] private long time;
        [SerializeField] private Vector3 position;
        [SerializeField] private float yaw;
        [SerializeField] private float pitch;
        [SerializeField] private int highestFloor;
        [SerializeField] private bool cctv;
        [SerializeField] private bool relay;
        [SerializeField] private string location;
        [SerializeField] private string[] openDoors;

        /// <summary>An empty checkpoint, for the JSON reader.</summary>
        public HospitalCheckpoint()
        {
        }

        public HospitalCheckpoint(DateTime savedUtc, Vector3 position, float yaw, float pitch, int highestFloor,
            bool cctv, bool relay, string location, string[] openDoors)
        {
            time = savedUtc.Ticks;
            this.position = position;
            this.yaw = yaw;
            this.pitch = pitch;
            this.highestFloor = highestFloor;
            this.cctv = cctv;
            this.relay = relay;
            this.location = location;
            this.openDoors = openDoors;
        }

        /// <summary>The save time in UTC ticks.</summary>
        public long Time => time;

        public Vector3 Position => position;

        public float Yaw => yaw;

        public float Pitch => pitch;

        /// <summary>The highest floor reached by the stairs, 0 to 3.</summary>
        public int HighestFloor => highestFloor;

        public bool CctvUnlocked => cctv;

        public bool RelayLinked => relay;

        /// <summary>The room's name as the menu shows it.</summary>
        public string Location => location;

        /// <summary>The names of the doors that stood open.</summary>
        public string[] OpenDoors => openDoors;

        /// <summary>Whether every value is one the game can produce (<see cref="CheckpointRules.IsValid"/>).</summary>
        public bool IsValid => CheckpointRules.IsValid(version, time, position.x, position.y, position.z, yaw, pitch,
            highestFloor, location, openDoors?.Length ?? -1);
    }
}
