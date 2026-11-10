using UnityEngine;

namespace NetRunner.Atmosphere
{
    [RequireComponent(typeof(Light))]
    public sealed class PracticalPulse : MonoBehaviour
    {
        public enum Pattern { Intermittent, Emergency }
        [SerializeField] Pattern pattern;
        [SerializeField, Range(0, 1)] float depth = 0.4f;
        [SerializeField, Min(0.1f)] float frequency = 0.8f;
        [SerializeField] float phase;

        Light fixture;
        float nominal;

        public void Configure(Pattern mode, float amount, float speed, float offset)
        {
            pattern = mode; depth = amount; frequency = speed; phase = offset;
        }

        void Awake()
        {
            fixture = GetComponent<Light>();
            nominal = fixture.intensity;
        }

        void Update()
        {
            float t = Time.time * frequency + phase;
            float factor = pattern == Pattern.Emergency
                ? (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f))
                : Mathf.SmoothStep(0f, 1f, (Mathf.PerlinNoise(t, phase) - 0.27f) * 3.5f);
            fixture.intensity = nominal * Mathf.Lerp(1f - depth, 1f, factor);
        }
    }
}

