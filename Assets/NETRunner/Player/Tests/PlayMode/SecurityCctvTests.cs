using NUnit.Framework;
using UnityEngine;

namespace NetRunner.Player.Tests
{
    /// <summary>
    /// PlayMode tests of the security cameras on a test-built network of
    /// three views: nothing can be watched until the reception breach unlocks
    /// it, opening shows the first camera, stepping past either end wraps
    /// round, and closing stops the feed camera. Behaviour from
    /// docs/systems/player.md and Northline/Documentation/cctv_unity.md.
    /// </summary>
    public sealed class SecurityCctvTests
    {
        private const int Views = 3;

        private TestWorld world;
        private SecurityCctv network;
        private Transform[] views;

        [SetUp]
        public void Build()
        {
            world = new TestWorld();
            world.HideHospital();
            var feedCamera = world.Empty("Test feed camera", Vector3.zero).AddComponent<Camera>();
            feedCamera.enabled = false;
            views = new Transform[Views];
            var feeds = new SecurityCctv.Feed[Views];
            for (int i = 0; i < Views; i++)
            {
                views[i] = world.Empty($"Test view {i}", new Vector3(i * 10f, 3f, 0f)).transform;
                views[i].rotation = Quaternion.Euler(20f, i * 30f, 0f);
                feeds[i] = Feed($"View {i}", views[i]);
            }

            network = world.Empty("Test CCTV", Vector3.zero).AddComponent<SecurityCctv>();
            network.Configure(feedCamera, feeds);
        }

        [TearDown]
        public void Clear()
        {
            world.Clear();
        }

        [Test]
        public void NothingCanBeWatchedUntilItIsUnlocked()
        {
            Assert.That(network.Open(), Is.False);
            Assert.That(network.Watching, Is.False);
        }

        [Test]
        public void OpeningShowsTheFirstCamera()
        {
            network.Unlock();

            Assert.That(network.Open(), Is.True);
            Assert.That(network.Watching, Is.True);
            Assert.That(network.Selected, Is.EqualTo(0));
            Assert.That(network.Label, Is.EqualTo("View 0"));
            Assert.That(network.FeedCamera.transform.position, Is.EqualTo(views[0].position));
            Assert.That(network.FeedCamera.targetTexture, Is.SameAs(network.Surface));
        }

        [Test]
        public void SteppingPastEitherEndWrapsRound()
        {
            network.Unlock();
            network.Open();

            network.Step(-1);
            int last = network.Selected;
            network.Step(1);

            Assert.That(last, Is.EqualTo(Views - 1));
            Assert.That(network.Selected, Is.EqualTo(0));
            Assert.That(network.FeedCamera.transform.position, Is.EqualTo(views[0].position));
        }

        [Test]
        public void SteppingWhileClosedChangesNothing()
        {
            network.Unlock();

            network.Step(1);

            Assert.That(network.Selected, Is.EqualTo(0));
        }

        [Test]
        public void ClosingStopsTheFeedCamera()
        {
            network.Unlock();
            network.Open();
            network.FeedCamera.enabled = true;

            network.Close();

            Assert.That(network.Watching, Is.False);
            Assert.That(network.FeedCamera.enabled, Is.False);
        }

        private static SecurityCctv.Feed Feed(string label, Transform view)
        {
            return new SecurityCctv.Feed(label, view);
        }
    }
}
