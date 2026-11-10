using UnityEngine;

namespace NETRunner92
{
    public sealed class EnvironmentalZone : MonoBehaviour
    {
        [SerializeField] Vector3 size = Vector3.one;
        [SerializeField, Min(0.1f)] float blendDistance = 2f;
        [SerializeField, Range(0, 4)] int floor;
        [SerializeField, Range(0, 1)] float dustIntensity;
        public int Floor => floor;
        public float DustIntensity => dustIntensity;

        public void Configure(Vector3 dimensions, float transition, int level, float dust)
        {
            size = dimensions; blendDistance = transition; floor = level; dustIntensity = dust;
        }

        public float WeightAt(Vector3 position)
        {
            Vector3 p = transform.InverseTransformPoint(position);
            Vector3 d = new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z)) - size * 0.5f;
            float distance = Vector3.Max(Vector3.zero, d).magnitude;
            return 1f - Mathf.SmoothStep(0f, 1f, distance / blendDistance);
        }
    }
}

