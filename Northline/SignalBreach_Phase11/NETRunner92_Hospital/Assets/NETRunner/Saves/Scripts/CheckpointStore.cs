using System;
using System.IO;
using NetRunner.Core;
using NetRunner.Settings;
using UnityEngine;

namespace NetRunner.Saves
{
    /// <summary>
    /// The three save slots (Phase 11): one JSON file each, slot-1.json to
    /// slot-3.json, in a folder (the player's profile, see
    /// <see cref="LocalGameFiles"/>). A file that is too large, unreadable or
    /// fails <see cref="HospitalCheckpoint.IsValid"/> counts as damaged: it
    /// exists but cannot be loaded.
    /// </summary>
    public sealed class CheckpointStore
    {
        // Larger files are not saves; they are treated as damaged rather than read.
        private const long MaxFileBytes = 65536;

        private readonly string directory;

        /// <summary>Slots in <paramref name="folder"/>; tests use a folder under Logs/.</summary>
        public CheckpointStore(string folder)
        {
            directory = folder;
        }

        /// <summary>The slots in the player's profile folder.</summary>
        public static CheckpointStore ForProfile() => new CheckpointStore(LocalGameFiles.ProfileDirectory);

        /// <summary>Whether a file is in the slot, damaged or not.</summary>
        public bool Exists(int slot) => File.Exists(PathFor(slot));

        /// <summary>The slot's checkpoint, or null when it is empty or damaged.</summary>
        public HospitalCheckpoint Read(int slot)
        {
            var path = PathFor(slot);
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists || file.Length > MaxFileBytes)
                {
                    return null;
                }

                var data = JsonUtility.FromJson<HospitalCheckpoint>(File.ReadAllText(path));
                return data != null && data.IsValid ? data : null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }

        /// <summary>The slot with the latest valid save, or -1.</summary>
        public int MostRecent()
        {
            var times = new long[CheckpointRules.Slots];
            for (int i = 0; i < times.Length; i++)
            {
                times[i] = Read(i)?.Time ?? 0;
            }

            return CheckpointRules.MostRecent(times);
        }

        /// <summary>Writes the checkpoint; false when it is not valid or cannot be written.</summary>
        public bool Save(int slot, HospitalCheckpoint data)
        {
            if (!data.IsValid)
            {
                return false;
            }

            try
            {
                LocalGameFiles.Write(PathFor(slot), JsonUtility.ToJson(data, true));
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"Checkpoint write failed: {exception.Message}");
                return false;
            }
        }

        public bool Delete(int slot)
        {
            try
            {
                File.Delete(PathFor(slot));
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"Checkpoint deletion failed: {exception.Message}");
                return false;
            }
        }

        private string PathFor(int slot)
        {
            if (slot < 0 || slot >= CheckpointRules.Slots)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            return Path.Combine(directory, $"slot-{slot + 1}.json");
        }
    }
}
