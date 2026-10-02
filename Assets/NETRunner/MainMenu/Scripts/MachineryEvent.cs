using System.Collections;
using UnityEngine;

namespace NetRunner.MenuPrototype
{
    public sealed class MachineryEvent : FacilityEvent
    {
        public Transform rotor;
        public MenuAudioController audioController;
        [Min(0.1f)] public float duration = 7f;
        private Quaternion originalRotation;

        private void Awake() => originalRotation = rotor.localRotation;

        public override IEnumerator Run()
        {
            audioController.StartMachinery();
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < duration)
            {
                float fraction = (Time.unscaledTime - start) / duration;
                rotor.Rotate(Vector3.forward, Mathf.Sin(fraction * Mathf.PI) * 190 * Time.unscaledDeltaTime, Space.Self);
                yield return null;
            }
            audioController.StopMachinery();
        }

        private void OnDisable()
        {
            if (audioController != null)
            {
                audioController.StopMachinery();
            }
            if (rotor != null)
            {
                rotor.localRotation = originalRotation;
            }
        }
    }
}
