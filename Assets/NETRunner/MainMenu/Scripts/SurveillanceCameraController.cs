using System;
using System.Collections;
using UnityEngine;

namespace NetRunner.MainMenu
{
    /// <summary>
    /// Switches between the surveillance camera feeds behind the menu.
    /// </summary>
    public sealed class SurveillanceCameraController : MonoBehaviour
    {
        /// <summary>One camera feed: its label and the camera that renders it.</summary>
        [Serializable]
        public sealed class Feed
        {
            [SerializeField] internal string id;
            [SerializeField] internal string location;
            [SerializeField] internal Camera camera;
        }

        [SerializeField] internal MenuPrototypeSettings settings;
        [SerializeField] internal Feed[] feeds;
        [SerializeField] internal MenuView view;
        [SerializeField] internal MenuAudioController audioController;

        private Coroutine timer;
        private bool interrupting;

        public bool AutomaticCycling { get; set; } = true;

        public bool Suspended { get; set; }

        public int ActiveIndex { get; private set; }

        public int SwitchCount { get; private set; }

        private void OnDisable()
        {
            if (timer != null)
            {
                StopCoroutine(timer);
            }

            interrupting = false;
        }

        public void Begin()
        {
            Select(0);
            timer = StartCoroutine(Cycle());
        }

        public void Select(int index)
        {
            ActiveIndex = (index + feeds.Length) % feeds.Length;
            for (int i = 0; i < feeds.Length; i++)
            {
                feeds[i].camera.enabled = i == ActiveIndex;
            }

            view.CameraText.text = feeds[ActiveIndex].id + " // " + feeds[ActiveIndex].location;
            SwitchCount++;
        }

        public IEnumerator InterruptSignal(float duration = -1f)
        {
            if (interrupting)
            {
                yield break;
            }

            interrupting = true;
            view.SignalText.text = "SIGNAL: REACQUIRING";
            view.Signal.style.opacity = 0.5f;
            audioController.Signal();
            yield return new WaitForSecondsRealtime(duration > 0 ? duration : settings.signalDuration);
            view.Signal.style.opacity = 0;
            view.SignalText.text = "SIGNAL: STABLE";
            interrupting = false;
        }

        private IEnumerator Cycle()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(settings.CameraDelay);
                if (!Suspended && AutomaticCycling)
                {
                    yield return InterruptSignal();
                    Select(ActiveIndex + 1);
                }
            }
        }
    }
}
