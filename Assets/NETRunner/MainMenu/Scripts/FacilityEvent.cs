using System.Collections;
using UnityEngine;

namespace NetRunner.MenuPrototype
{
    // Add another subclass to extend the event bank without editing the scheduler.
    public abstract class FacilityEvent : MonoBehaviour
    {
        public string telemetry = "REMOTE ACTIVITY DETECTED";
        public abstract IEnumerator Run();
    }
}
