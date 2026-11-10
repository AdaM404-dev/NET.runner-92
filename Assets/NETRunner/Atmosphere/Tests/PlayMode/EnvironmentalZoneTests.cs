using NUnit.Framework;
using UnityEngine;

namespace NetRunner.Atmosphere.Tests
{
    /// <summary>
    /// PlayMode tests of a fog zone's weight: 1 inside its box, falling
    /// smoothly to 0 at its blend distance outside, measured from the nearest
    /// point of the box in the zone's own frame, so a turned zone turns its
    /// box with it. Expected values come from docs/systems/atmosphere.md.
    /// </summary>
    public sealed class EnvironmentalZoneTests
    {
        private const float Blend = 2f;

        // 4 m wide (x), 3 m high (y) and 6 m deep (z) in the zone's own frame.
        private static readonly Vector3 Size = new Vector3(4f, 3f, 6f);

        private GameObject zoneObject;
        private EnvironmentalZone zone;

        [SetUp]
        public void BuildTurnedZone()
        {
            zoneObject = new GameObject("Test zone");
            zoneObject.transform.SetPositionAndRotation(new Vector3(10f, 1f, -5f), Quaternion.Euler(0f, 90f, 0f));
            zone = zoneObject.AddComponent<EnvironmentalZone>();
            zone.Configure(Size, Blend, 1, 0.5f);
        }

        [TearDown]
        public void RemoveZone()
        {
            Object.Destroy(zoneObject);
        }

        [Test]
        public void InsideItsBoxTheWeightIsOne()
        {
            Assert.That(zone.WeightAt(Local(0f, 0f, 0f)), Is.EqualTo(1f));
            Assert.That(zone.WeightAt(Local(1.9f, 1.4f, -2.9f)), Is.EqualTo(1f));
        }

        [Test]
        public void HalfTheBlendDistanceOutsideAFaceTheWeightIsOneHalf()
        {
            // 1 m outside the 2 m half-width; the S-curve is exactly halfway at half the distance.
            Assert.That(zone.WeightAt(Local(3f, 0f, 0f)), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void PastACornerItMeasuresTheStraightDistanceToTheBox()
        {
            // 0.6 m past the x face and 0.8 m past the z face: 1 m from the box's edge.
            Assert.That(zone.WeightAt(Local(2.6f, 0f, 3.8f)), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void AtTheBlendDistanceAndBeyondTheWeightIsZero()
        {
            Assert.That(zone.WeightAt(Local(4f, 0f, 0f)), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(zone.WeightAt(Local(0f, 0f, 30f)), Is.EqualTo(0f));
        }

        [Test]
        public void ItsBoxTurnsWithTheZone()
        {
            // A quarter turn lays the zone's 6 m depth along the world's x axis:
            // 3 m east of its centre is on its face, not 1 m outside it.
            var east = zoneObject.transform.position + new Vector3(3f, 0f, 0f);

            Assert.That(zone.WeightAt(east), Is.EqualTo(1f).Within(1e-5f));
        }

        // A point given in the zone's own frame, as a world position.
        private Vector3 Local(float x, float y, float z)
        {
            return zoneObject.transform.TransformPoint(new Vector3(x, y, z));
        }
    }
}
