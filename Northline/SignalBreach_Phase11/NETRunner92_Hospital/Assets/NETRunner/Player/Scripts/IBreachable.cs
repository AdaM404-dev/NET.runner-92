using UnityEngine;

namespace NetRunner.Player
{
    /// <summary>
    /// Something LATCH-07 can breach with the Signal Breach hack, such as a
    /// K-7 robot. The player's <see cref="HospitalInteraction"/> picks the
    /// one it aims at through <see cref="BreachTargets"/>, and
    /// <see cref="LatchViewmodel"/> runs the puzzle against it. It lives in
    /// Player so the enemy can implement it without the player knowing the
    /// enemy's code.
    /// </summary>
    public interface IBreachable
    {
        /// <summary>Whether a breach can start now: it is active and not still recovering.</summary>
        bool CanBreach { get; }

        /// <summary>Seconds until it recovers from the last successful breach; 0 when it is not disabled.</summary>
        float BreachRecovery { get; }

        /// <summary>How many seconds a successful breach disables it; LATCH-07's meter counts them down.</summary>
        float DisableSeconds { get; }

        /// <summary>The point the player must aim at and see, in world space.</summary>
        Vector3 BreachPoint { get; }

        /// <summary>Called with true while the player traces the breach and false when the trace ends.</summary>
        void SetBreachTracing(bool active);

        /// <summary>Disables it after a successful breach; false when it could not be breached.</summary>
        bool TryBreachDisable();
    }
}
