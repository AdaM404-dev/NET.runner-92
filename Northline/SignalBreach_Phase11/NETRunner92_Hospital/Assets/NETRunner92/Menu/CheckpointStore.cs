using System;
using System.IO;
using UnityEngine;

namespace NETRunner92
{
    [Serializable]
    internal sealed class HospitalCheckpoint
    {
        public int version = 1;
        public long time;
        public Vector3 position;
        public float yaw, pitch;
        public int highestFloor;
        public bool cctv, relay;
        public string location;
        public string[] openDoors;

        internal bool Valid => version == 1 && time > 0 && time <= DateTime.MaxValue.Ticks
            && highestFloor >= 0 && highestFloor <= 3
            && Mathf.Abs(position.x) < 220 && position.y >= -1 && position.y <= 16 && Mathf.Abs(position.z) < 220
            && Mathf.Abs(yaw) <= 360 && Mathf.Abs(pitch) <= 90
            && !string.IsNullOrWhiteSpace(location) && location.Length <= 128
            && openDoors != null && openDoors.Length <= 200;
    }

    internal static class CheckpointStore
    {
        internal const int Slots = 3;

        static string PathFor(int slot)
        {
            if (slot < 0 || slot >= Slots) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(LocalGameFiles.ProfileDirectory, $"slot-{slot + 1}.json");
        }

        internal static bool Exists(int slot) => File.Exists(PathFor(slot));

        internal static HospitalCheckpoint Read(int slot)
        {
            var path = PathFor(slot);
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists || file.Length > 65536) return null;
                var data = JsonUtility.FromJson<HospitalCheckpoint>(File.ReadAllText(path));
                return data != null && data.Valid ? data : null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }

        internal static int MostRecent()
        {
            int latest = -1;
            long time = 0;
            for (int i = 0; i < Slots; i++)
            {
                var checkpoint = Read(i);
                if (checkpoint == null || checkpoint.time <= time) continue;
                time = checkpoint.time;
                latest = i;
            }
            return latest;
        }

        internal static bool Save(int slot, HospitalCheckpoint data)
        {
            if (!data.Valid) return false;
            try
            {
                LocalGameFiles.Write(PathFor(slot), JsonUtility.ToJson(data, true));
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"Checkpoint write failed: {ex.Message}");
                return false;
            }
        }

        internal static bool Delete(int slot)
        {
            try
            {
                File.Delete(PathFor(slot));
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"Checkpoint deletion failed: {ex.Message}");
                return false;
            }
        }
    }
}
