using System;
using NetRunner.Saves;
using UnityEngine;

namespace NetRunner.Menu
{
    /// <summary>
    /// Carries the run the main menu starts into the hospital scene: the save
    /// slot and, when loading, its checkpoint. One asset, referenced by the
    /// menu and by the hospital's <see cref="HospitalRun"/>, so nothing static
    /// is needed across the scene change. The hospital takes the run once;
    /// a hospital started without one (the editor, tests, benchmarks) has
    /// no run: no pause menu and no saves.
    /// </summary>
    [CreateAssetMenu(menuName = "NET.runner-92/Run session", fileName = "RunSession")]
    public sealed class RunSession : ScriptableObject
    {
        private const int NoSlot = -1;

        [NonSerialized] private int slot = NoSlot;
        [NonSerialized] private HospitalCheckpoint checkpoint;
        [NonSerialized] private CheckpointStore store;

        /// <summary>The save slots the menu and the run use: the player's profile unless a test chose another folder.</summary>
        public CheckpointStore Store => store ??= CheckpointStore.ForProfile();

        // The asset outlives play sessions in the editor; start each one without a run.
        private void OnEnable()
        {
            slot = NoSlot;
            checkpoint = null;
            store = null;
        }

        /// <summary>Starts a run in <paramref name="saveSlot"/>, from <paramref name="start"/> or, when null, from the beginning.</summary>
        public void Begin(int saveSlot, HospitalCheckpoint start)
        {
            slot = saveSlot;
            checkpoint = start;
        }

        /// <summary>Keeps the save slots in <paramref name="folder"/>; for tests, which may write only under Logs/.</summary>
        public void UseSaveFolder(string folder)
        {
            store = new CheckpointStore(folder);
        }

        /// <summary>Takes the pending run, if there is one; afterwards none is pending.</summary>
        public bool TryTake(out int saveSlot, out HospitalCheckpoint start)
        {
            saveSlot = slot;
            start = checkpoint;
            bool pending = slot != NoSlot;
            slot = NoSlot;
            checkpoint = null;
            return pending;
        }
    }
}
