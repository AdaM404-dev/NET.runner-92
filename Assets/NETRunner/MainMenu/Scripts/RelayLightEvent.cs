using System.Collections;
using UnityEngine;

namespace NetRunner.MenuPrototype
{
    public sealed class RelayLightEvent : FacilityEvent
    {
        public Light target;
        public Renderer indicator;
        public Material offMaterial;
        public Material onMaterial;
        [Min(0.1f)] public float holdDuration = 4.5f;
        private float originalIntensity;
        private Material originalMaterial;

        private void Awake()
        {
            originalIntensity = target.intensity;
            originalMaterial = indicator.sharedMaterial;
        }

        public override IEnumerator Run()
        {
            target.intensity = 14f;
            indicator.sharedMaterial = onMaterial;
            yield return new WaitForSecondsRealtime(holdDuration);
            Restore();
        }

        private void Restore()
        {
            if (target != null)
            {
                target.intensity = originalIntensity;
            }
            if (indicator != null)
            {
                indicator.sharedMaterial = originalMaterial;
            }
        }

        private void OnDisable() => Restore();
    }
}
