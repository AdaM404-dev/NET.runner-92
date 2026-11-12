using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using NetRunner.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetRunner.Settings.Tests
{
    /// <summary>
    /// PlayMode tests of the preferences (Phase 11 settings): the defaults
    /// without a file, saving and reading back, repairing values the menu
    /// cannot produce, F2's level choice without saving, and the runtime
    /// applying a previewed quality level. Files go to Logs/Tests only.
    /// </summary>
    public sealed class PreferencesTests
    {
        private string folder;
        private int levelBefore;

        [SetUp]
        public void UseAnEmptyFolder()
        {
            folder = Path.Combine(Application.dataPath, "..", "Logs", "Tests", "Preferences-" + Guid.NewGuid().ToString("N"));
            levelBefore = QualitySettings.GetQualityLevel();
        }

        [TearDown]
        public void RemoveFolder()
        {
            QualitySettings.SetQualityLevel(levelBefore, true);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void WithoutAFileTheDefaultsAreTheHighPreset()
        {
            var preferences = Loaded();

            Assert.That(preferences.Active.Preset, Is.EqualTo(QualityPreset.High));
            Assert.That(preferences.Active.Quality, Is.EqualTo(QualityPreset.High));
            Assert.That(preferences.Active.RenderScale, Is.EqualTo(100));
            Assert.That(preferences.Active.FieldOfView, Is.EqualTo(68f));
        }

        [Test]
        public void ACommittedChoiceIsReadBackOnTheNextStart()
        {
            var preferences = Loaded();
            var next = preferences.Saved.Copy();
            next.Master = .4f;

            bool saved = preferences.Commit(next, out _);
            var restarted = Loaded();

            Assert.That(saved, Is.True);
            Assert.That(restarted.Saved.Master, Is.EqualTo(.4f).Within(1e-4f));
        }

        [Test]
        public void ValuesTheMenuCannotProduceAreRepairedOnLoad()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "settings.json"), "{\"version\":1,\"fov\":400,\"frameLimit\":75}");

            var preferences = Loaded();

            Assert.That(preferences.Saved.FieldOfView, Is.EqualTo(100f));
            Assert.That(preferences.Saved.FrameLimit, Is.EqualTo(60));
        }

        [Test]
        public void SelectingALevelWithoutSavingAppliesItsPresetOnlyForThisRun()
        {
            var preferences = Loaded();

            preferences.SelectLevel(QualityPreset.Low, false);

            Assert.That(preferences.Active.Quality, Is.EqualTo(0));
            Assert.That(preferences.Active.RenderScale, Is.EqualTo(50));
            Assert.That(preferences.Saved.Preset, Is.EqualTo(QualityPreset.High));
            Assert.That(File.Exists(Path.Combine(folder, "settings.json")), Is.False);
        }

        [Test]
        public void APreviewIsAnnounced()
        {
            var preferences = Loaded();
            int changes = 0;
            preferences.Changed += () => changes++;

            preferences.Preview(preferences.Saved.Copy());

            Assert.That(changes, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TheRuntimeSwitchesToThePreviewedQualityLevel()
        {
            // The game's own preferences asset, which the runtime created at start applies.
            var shared = Resources.Load<Preferences>(PreferencesRuntime.ResourcePath);
            var low = shared.Active.Copy();
            low.SetPreset(QualityPreset.Low);

            shared.Preview(low);
            yield return null;
            int level = QualitySettings.GetQualityLevel();
            shared.Preview(shared.Saved);

            Assert.That(level, Is.EqualTo(0));
        }

        private Preferences Loaded()
        {
            var preferences = ScriptableObject.CreateInstance<Preferences>();
            preferences.UseProfileDirectory(folder);
            preferences.Load();
            return preferences;
        }
    }
}
