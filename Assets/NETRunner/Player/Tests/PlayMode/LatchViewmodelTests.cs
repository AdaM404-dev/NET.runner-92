using System.Collections;
using NetRunner.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NetRunner.Player.Tests
{
    /// <summary>
    /// PlayMode tests of the LATCH-07 tablet in the real Hospital_Exploration
    /// scene, where its arms, animations and screen are: raised, it boots and
    /// then waits; a tap with no relay in view fails and it goes back to
    /// waiting; R puts it away; while the CCTV is locked, 1 only shows the
    /// hint. The relay link and the CCTV page are covered by the hospital
    /// checks (HospitalChecksTests). Behaviour from docs/systems/player.md and
    /// Northline/Documentation/latch07_unity.md.
    /// </summary>
    public sealed class LatchViewmodelTests
    {
        private const string SceneName = "Hospital_Exploration";
        private const float PhaseTimeout = 4f;

        private LatchViewmodel latch;
        private LatchScreen screen;

        [UnitySetUp]
        public IEnumerator LoadHospital()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            var player = Object.FindAnyObjectByType<FirstPersonController>();
            latch = player.GetComponent<LatchViewmodel>();
            screen = player.GetComponent<LatchScreen>();

            // Only the test drives the tablet: no keyboard, no cursor checks.
            player.GetComponent<HospitalInteraction>().enabled = false;
            player.enabled = false;
        }

        [UnityTest]
        public IEnumerator RaisedItBootsAndAFailedTapReturnsToReady()
        {
            latch.Toggle(null);

            Assert.That(latch.Current, Is.EqualTo(LatchPhase.Equip));
            Assert.That(latch.Model.activeSelf, Is.True, "The arms are hidden");
            Assert.That(screen.Status, Is.EqualTo("BOOTING"));

            yield return Reach(LatchPhase.Ready);
            Assert.That(screen.Status, Is.EqualTo("SIGNAL STANDBY"));

            latch.Tap();
            Assert.That(latch.Current, Is.EqualTo(LatchPhase.Tap));
            yield return Reach(LatchPhase.Failed);
            Assert.That(screen.Status, Is.EqualTo("LINK FAILED"));
            yield return Reach(LatchPhase.Ready);

            latch.Toggle(null);
            Assert.That(latch.Current, Is.EqualTo(LatchPhase.Unequip));
            yield return Reach(LatchPhase.Stowed);
            Assert.That(latch.Model.activeSelf, Is.False, "The arms are still shown");
        }

        [UnityTest]
        public IEnumerator CancellingPutsItAway()
        {
            latch.Toggle(null);
            yield return Reach(LatchPhase.Ready);

            latch.Cancel();
            Assert.That(latch.Current, Is.EqualTo(LatchPhase.Cancel));
            yield return Reach(LatchPhase.Stowed);

            Assert.That(latch.Model.activeSelf, Is.False);
        }

        [Test]
        public void WhileTheCctvIsLockedPressingOneOnlyShowsTheHint()
        {
            bool opened = latch.ToggleCctv();

            Assert.That(opened, Is.False);
            Assert.That(latch.Current, Is.EqualTo(LatchPhase.Stowed));
            Assert.That(latch.CctvHintVisible, Is.True);
            Assert.That(latch.CctvHint, Is.EqualTo("CCTV locked   /   Use the reception terminal"));
        }

        private IEnumerator Reach(LatchPhase phase)
        {
            float until = Time.realtimeSinceStartup + PhaseTimeout;
            while (latch.Current != phase && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            Assert.That(latch.Current, Is.EqualTo(phase), $"Did not reach {phase}");
        }
    }
}
