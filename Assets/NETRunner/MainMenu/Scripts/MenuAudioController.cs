using UnityEngine;

namespace NetRunner.MenuPrototype
{
    public sealed class MenuAudioController : MonoBehaviour
    {
        [Header("Replace these original synthesized placeholders with authored audio later")]
        public AudioSource electricalHum;
        public AudioSource ventilation;
        public AudioSource machinery;
        public AudioSource uiRelay;
        public AudioSource signalInterference;
        [Range(0, 1)] public float masterVolume = 0.35f;

        public void Begin()
        {
            SetVolume(masterVolume);
            electricalHum.Play();
            ventilation.Play();
        }

        public void SetVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            electricalHum.volume = masterVolume * 0.22f;
            ventilation.volume = masterVolume * 0.13f;
            machinery.volume = masterVolume * 0.2f;
            uiRelay.volume = masterVolume * 0.18f;
            signalInterference.volume = masterVolume * 0.08f;
        }

        public void Click() => uiRelay.Play();
        public void Signal() => signalInterference.Play();
        public void StartMachinery() => machinery.Play();
        public void StopMachinery() => machinery.Stop();

        private void OnDisable()
        {
            foreach (var source in new[] { electricalHum, ventilation, machinery, uiRelay, signalInterference })
            {
                if (source != null)
                {
                    source.Stop();
                }
            }
        }
    }
}
