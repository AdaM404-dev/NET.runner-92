using System.Collections;
using UnityEngine;

namespace NetRunner.MainMenu
{
    /// <summary>
    /// One scripted happening in the facility (a relay, a rotor, a shutter).
    /// Add another subclass to extend the event bank without editing the scheduler.
    /// </summary>
    public abstract class FacilityEvent : MonoBehaviour
    {
        [SerializeField] internal string telemetry = "REMOTE ACTIVITY DETECTED";

        public abstract IEnumerator Run();
    }
}
