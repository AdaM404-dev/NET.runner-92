using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NetRunner.Saves.Tests
{
    /// <summary>
    /// EditMode tests of the save slots (Phase 11 saves): a checkpoint is
    /// written and read back, an invalid one is refused, a damaged file
    /// exists but cannot be loaded, the latest save is the most recent, and
    /// deleting empties the slot. Files go to Logs/Tests only.
    /// </summary>
    public sealed class CheckpointStoreTests
    {
        private string folder;
        private CheckpointStore store;

        [SetUp]
        public void UseAnEmptyFolder()
        {
            folder = Path.Combine(Application.dataPath, "..", "Logs", "Tests", "Saves-" + Guid.NewGuid().ToString("N"));
            store = new CheckpointStore(folder);
        }

        [TearDown]
        public void RemoveFolder()
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void ASavedCheckpointIsReadBack()
        {
            bool saved = store.Save(1, Checkpoint(new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc)));
            var read = store.Read(1);

            Assert.That(saved, Is.True);
            Assert.That(read.Location, Is.EqualTo("2. szint · Recepció"));
            Assert.That(read.Position, Is.EqualTo(new Vector3(1f, 4.2f, -3f)));
            Assert.That(read.CctvUnlocked, Is.True);
            Assert.That(read.OpenDoors, Is.EqualTo(new[] { "Door A" }));
        }

        [Test]
        public void AnInvalidCheckpointIsNotWritten()
        {
            var outside = new HospitalCheckpoint(DateTime.UtcNow, new Vector3(500f, 0f, 0f), 0f, 0f, 1, false, false, "Kint", new string[0]);

            Assert.That(store.Save(0, outside), Is.False);
            Assert.That(store.Exists(0), Is.False);
        }

        [Test]
        public void ADamagedFileExistsButCannotBeLoaded()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "slot-1.json"), "{ invalid");

            Assert.That(store.Exists(0), Is.True);
            Assert.That(store.Read(0), Is.Null);
        }

        [Test]
        public void TheLatestSaveIsTheMostRecent()
        {
            store.Save(0, Checkpoint(new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc)));
            store.Save(2, Checkpoint(new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc)));

            Assert.That(store.MostRecent(), Is.EqualTo(2));
        }

        [Test]
        public void DeletingEmptiesTheSlot()
        {
            store.Save(0, Checkpoint(DateTime.UtcNow));

            bool deleted = store.Delete(0);

            Assert.That(deleted, Is.True);
            Assert.That(store.Exists(0), Is.False);
        }

        [Test]
        public void ThereAreThreeSlots()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Exists(3));
        }

        private static HospitalCheckpoint Checkpoint(DateTime savedUtc)
        {
            return new HospitalCheckpoint(savedUtc, new Vector3(1f, 4.2f, -3f), 90f, -10f, 1, true, false,
                "2. szint · Recepció", new[] { "Door A" });
        }
    }
}
