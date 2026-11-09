using UnityEngine;

namespace NetRunner.MainMenu
{
    /// <summary>
    /// The menu's ambience and interface sounds, scaled by one master volume.
    /// </summary>
    public sealed class MenuAudioController : MonoBehaviour
    {
        [Header("Replace these original synthesized placeholders with authored audio later")]
        [SerializeField] internal AudioSource electricalHum;
        [SerializeField] internal AudioSource ventilation;
        [SerializeField] internal AudioSource machinery;
        [SerializeField] internal AudioSource uiRelay;
        [SerializeField] internal AudioSource signalInterference;
        [Range(0, 1)] [SerializeField] internal float masterVolume = 0.35f;

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
    }
}
