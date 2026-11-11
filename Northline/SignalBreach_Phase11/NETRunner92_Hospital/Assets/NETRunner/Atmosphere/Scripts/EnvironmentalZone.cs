using NetRunner.Core;
using UnityEngine;

namespace NetRunner.Atmosphere
{
    /// <summary>
    /// A part of the hospital for the atmosphere, such as one floor or an
    /// entrance wing: a box saying how much the player counts as inside it,
    /// which floor it is, and how dusty its air is. Its weight is 1 inside the
    /// box and fades to 0 over its blend distance outside
    /// (<see cref="ZoneBlend"/>); <see cref="HospitalAtmosphere"/> reads it
    /// every frame. The hospital's scene builder adds one next to each floor's
    /// fog volume and sets it up through <see cref="Configure"/>.
    /// </summary>
    public sealed class EnvironmentalZone : MonoBehaviour
    {
        [SerializeField] private Vector3 size = Vector3.one;
        [SerializeField, Min(0.1f)] private float blendDistance = 2f;
        [SerializeField, Range(0, 4)] private int floor;
        [SerializeField, Range(0, 1)] private float dustIntensity;

        /// <summary>The hospital level the zone covers, 1 to 4; a zone on floor 0 gives no cover.</summary>
        public int Floor => floor;

        /// <summary>How dusty the zone's air is, 0 (clean) to 1.</summary>
        public float DustIntensity => dustIntensity;

        /// <summary>
        /// Sets the zone up from the scene builder: the box's full size, how
        /// far outside it the weight fades to 0, the floor and the dust.
        /// </summary>
        public void Configure(Vector3 dimensions, float transition, int level, float dust)
        {
            size = dimensions;
            blendDistance = transition;
            floor = level;
            dustIntensity = dust;
        }

        /// <summary>How much a position in the world counts as inside the zone, 0 to 1.</summary>
        public float WeightAt(Vector3 position)
        {
            var local = transform.InverseTransformPoint(position);
            return ZoneBlend.Weight((local.x, local.y, local.z), (size.x, size.y, size.z), blendDistance);
        }
    }
}
