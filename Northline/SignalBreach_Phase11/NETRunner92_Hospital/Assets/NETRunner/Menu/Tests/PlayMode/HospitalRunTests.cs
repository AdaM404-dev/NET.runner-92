using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using NetRunner.Player;
using NetRunner.Saves;
using NetRunner.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace NetRunner.Menu.Tests
{
    /// <summary>
    /// PlayMode tests of a hospital run in the real hospital scene (Phase 11):
    /// a new game writes its first save, loading a checkpoint restores the
    /// player's place and progress, and a hospital started without a run has
    /// no pause menu. Saves go to Logs/Tests only.
    /// </summary>
    public sealed class HospitalRunTests
    {
        private const string SessionPath = "Assets/NETRunner/Menu/Data/RunSession.asset";
        private const int StartupFrames = 5;

        private string folder;
        private RunSession session;

        [SetUp]
        public void UseAnEmptySaveFolder()
        {
            folder = Path.Combine(Application.dataPath, "..", "Logs", "Tests", "Run-" + Guid.NewGuid().ToString("N"));
#if UNITY_EDITOR
            session = UnityEditor.AssetDatabase.LoadAssetAtPath<RunSession>(SessionPath);
#endif
            session.UseSaveFolder(folder);
        }

        [TearDown]
        public void RemoveFolder()
        {
            session.TryTake(out _, out _);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        [UnityTest]
        public IEnumerator ANewGameWritesItsFirstSave()
        {
            session.Begin(0, null);

            yield return LoadHospital();

            var saved = session.Store.Read(0);
            Assert.That(saved, Is.Not.Null, "No first save");
            Assert.That(saved.HighestFloor, Is.EqualTo(0));
            Assert.That(saved.CctvUnlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator LoadingACheckpointRestoresThePlayersPlaceAndProgress()
        {
            var start = new Vector3(0f, .04f, -34.3f);
            session.Begin(1, new HospitalCheckpoint(DateTime.UtcNow, start, 90f, 0f, 2, true, true, "Teszt", new string[0]));

            yield return LoadHospital();

            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();
            Assert.That(Vector3.Distance(player.transform.position, start), Is.LessThan(.1f));
            Assert.That(UnityEngine.Object.FindAnyObjectByType<StairAccess>().Highest, Is.EqualTo(2));
            Assert.That(UnityEngine.Object.FindAnyObjectByType<SecurityCctv>().Unlocked, Is.True);
            Assert.That(UnityEngine.Object.FindAnyObjectByType<LatchRelay>().Linked, Is.True);
        }

        [UnityTest]
        public IEnumerator WithoutARunTheHospitalHasNoPauseMenuOrSaves()
        {
            yield return LoadHospital();

            var run = UnityEngine.Object.FindAnyObjectByType<HospitalRun>(FindObjectsInactive.Include);
            Assert.That(run.enabled, Is.False);
            Assert.That(run.GetComponent<UIDocument>(), Is.Null);
            Assert.That(Directory.Exists(folder), Is.False);
        }

        private static IEnumerator LoadHospital()
        {
            yield return SceneManager.LoadSceneAsync(HospitalRun.HospitalScene, LoadSceneMode.Single);
            for (int i = 0; i < StartupFrames; i++)
            {
                yield return null;
            }
        }
    }
}
