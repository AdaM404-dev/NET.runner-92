using System.Collections;
using UnityEngine;

namespace NetRunner.MainMenu
{
    /// <summary>
    /// Runs a random facility event now and then while the menu is observed.
    /// </summary>
    public sealed class FacilityEventController : MonoBehaviour
    {
        [SerializeField] internal MenuPrototypeSettings settings;
        [SerializeField] internal FacilityEvent[] events;
        [SerializeField] internal MenuView view;

        private Coroutine timer;
        private int previous = -1;

        public bool Suspended { get; set; }

        public bool IsRunning { get; private set; }

        public int CompletedEvents { get; private set; }

        private void OnDisable()
        {
            if (timer != null)
            {
                StopCoroutine(timer);
            }

            IsRunning = false;
        }

        public void Begin() => timer = StartCoroutine(Schedule());

        public IEnumerator RunEvent(int index)
        {
            if (IsRunning || index < 0 || index >= events.Length)
            {
                yield break;
            }

            IsRunning = true;
            previous = index;
            view.ActivityText.text = events[index].telemetry;
            yield return events[index].Run();
            CompletedEvents++;
            view.ActivityText.text = "OBSERVATION CHANNEL OPEN / NO OPERATOR PRESENT";
            IsRunning = false;
        }

        private IEnumerator Schedule()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(settings.EventDelay);
                if (!Suspended && !IsRunning && events.Length > 0 && Random.value <= settings.eventProbability)
                {
                    int index = Random.Range(0, events.Length);
                    if (events.Length > 1 && index == previous)
                    {
                        index = (index + 1) % events.Length;
                    }

                    yield return RunEvent(index);
                }
            }
        }
    }
}
