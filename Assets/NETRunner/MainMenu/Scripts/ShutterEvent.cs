using System.Collections;
using UnityEngine;

namespace NetRunner.MenuPrototype
{
    public sealed class ShutterEvent : FacilityEvent
    {
        public Transform shutter;
        public float travel = 1.6f;
        public float duration = 5f;
        private Vector3 restingPosition;

        private void Awake() => restingPosition = shutter.localPosition;

        public override IEnumerator Run()
        {
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < duration)
            {
                float fraction = (Time.unscaledTime - start) / Mathf.Max(0.1f, duration);
                shutter.localPosition = restingPosition + Vector3.down * travel * Mathf.SmoothStep(0, 1, Mathf.Sin(fraction * Mathf.PI));
                yield return null;
            }
            shutter.localPosition = restingPosition;
        }

        private void OnDisable()
        {
            if (shutter != null)
            {
                shutter.localPosition = restingPosition;
            }
        }
    }
}
