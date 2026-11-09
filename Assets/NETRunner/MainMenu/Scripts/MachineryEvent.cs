using System.Collections;
using UnityEngine;

namespace NetRunner.MainMenu
{
    /// <summary>
    /// Facility event: a ventilation rotor spins up and down with its sound.
    /// </summary>
    public sealed class MachineryEvent : FacilityEvent
    {
        [SerializeField] internal Transform rotor;
        [SerializeField] internal MenuAudioController audioController;
        [Min(0.1f)] [SerializeField] private float duration = 7f;

        private Quaternion originalRotation;

        private void Awake() => originalRotation = rotor.localRotation;

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
    }
}
