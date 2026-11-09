using System.Collections;
using UnityEngine;

namespace NetRunner.MainMenu
{
    /// <summary>
    /// Facility event: a dormant relay light flares up, then returns to normal.
    /// </summary>
    public sealed class RelayLightEvent : FacilityEvent
    {
        [SerializeField] internal Light target;
        [SerializeField] internal Renderer indicator;
        [SerializeField] internal Material offMaterial;
        [SerializeField] internal Material onMaterial;
        [Min(0.1f)] [SerializeField] private float holdDuration = 4.5f;

        private float originalIntensity;
        private Material originalMaterial;

        private void Awake()
        {
            originalIntensity = target.intensity;
            originalMaterial = indicator.sharedMaterial;
        }

        private void OnDisable() => Restore();

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
    }
}
