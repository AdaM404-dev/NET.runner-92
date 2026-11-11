using System;
using System.Collections.Generic;

namespace NetRunner.Core
{
    /// <summary>
    /// Which saved checkpoints the game accepts (Phase 11 saves). A file that
    /// fails these checks is shown as damaged and cannot be loaded, so a
    /// hand-edited or broken save never puts the player outside the hospital.
    /// Plain checks on the saved values; reading the file is the Saves feature's job.
    /// </summary>
    public static class CheckpointRules
    {
        public const int Version = 1;

        /// <summary>Save slots the menu offers.</summary>
        public const int Slots = 3;

        /// <summary>The hospital's highest floor index: four floors, 0 to 3.</summary>
        public const int HighestFloor = 3;

        // The campus fits in ±220 m across, and from 1 m below the ground to 16 m up.
        private const float HalfExtent = 220f;
        private const float LowestY = -1f;
        private const float HighestY = 16f;

        private const float MaxYaw = 360f;
        private const float MaxPitch = 90f;
        private const int MaxLocationLength = 128;
        private const int MaxOpenDoors = 200;

        /// <summary>Whether a checkpoint's saved values are all within what the game can produce.</summary>
        public static bool IsValid(int version, long ticks, float x, float y, float z, float yaw, float pitch,
            int highestFloor, string location, int openDoorCount)
        {
            return version == Version
                && ticks > 0 && ticks <= DateTime.MaxValue.Ticks
                && highestFloor >= 0 && highestFloor <= HighestFloor
                && Math.Abs(x) < HalfExtent && y >= LowestY && y <= HighestY && Math.Abs(z) < HalfExtent
                && Math.Abs(yaw) <= MaxYaw && Math.Abs(pitch) <= MaxPitch
                && !string.IsNullOrWhiteSpace(location) && location.Length <= MaxLocationLength
                && openDoorCount >= 0 && openDoorCount <= MaxOpenDoors;
        }

        /// <summary>
        /// The slot with the latest save time, or -1 when none has one.
        /// <paramref name="ticks"/> holds each slot's save time, or 0 for an
        /// empty or damaged slot. On a tie the lower slot wins.
        /// </summary>
        public static int MostRecent(IReadOnlyList<long> ticks)
        {
            int latest = -1;
            long time = 0;
            for (int i = 0; i < ticks.Count; i++)
            {
                if (ticks[i] <= time)
                {
                    continue;
                }

                time = ticks[i];
                latest = i;
            }

            return latest;
        }
    }
}
