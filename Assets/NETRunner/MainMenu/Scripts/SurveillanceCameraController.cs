using System;
using System.Collections;
using UnityEngine;

namespace NetRunner.MenuPrototype
{
    public sealed class SurveillanceCameraController : MonoBehaviour
    {
        [Serializable]
        public sealed class Feed
        {
            public string id;
            public string location;
            public Camera camera;
        }

        public MenuPrototypeSettings settings;
        public Feed[] feeds;
        public MenuView view;
        public MenuAudioController audioController;
        public bool AutomaticCycling { get; set; } = true;
        public bool Suspended { get; set; }
        public int ActiveIndex { get; private set; }
        public int SwitchCount { get; private set; }
        private Coroutine timer;
        private bool interrupting;

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

        private void OnDisable()
        {
            if (timer != null)
            {
                StopCoroutine(timer);
            }
            interrupting = false;
        }
    }
}
