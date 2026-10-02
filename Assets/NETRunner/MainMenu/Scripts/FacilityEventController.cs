using System.Collections;
using UnityEngine;

namespace NetRunner.MenuPrototype
{
    public sealed class FacilityEventController : MonoBehaviour
    {
        public MenuPrototypeSettings settings;
        public FacilityEvent[] events;
        public MenuView view;
        public bool Suspended { get; set; }
        public bool IsRunning { get; private set; }
        public int CompletedEvents { get; private set; }
        private Coroutine timer;
        private int previous = -1;

        public void Begin() => timer = StartCoroutine(Schedule());

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

        private void OnDisable()
        {
            if (timer != null)
            {
                StopCoroutine(timer);
            }
            IsRunning = false;
        }
    }
}
