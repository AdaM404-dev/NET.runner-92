using System.Collections.Generic;
using UnityEngine;

namespace NetRunner.Player
{
    /// <summary>
    /// The breachable things in the loaded scene, and the choice of the one
    /// the player aims at. Carried over from Phase 11's <c>K7Patrol</c> list
    /// unchanged; the static list is temporary and is replaced in #55 step 3.
    /// </summary>
    public static class BreachTargets
    {
        /// <summary>Metres: farther away than this nothing can be breached.</summary>
        public const float Range = 8f;

        // Cosine of the largest angle between the view and the target: about 12.8°.
        private const float MinimumAlignment = .975f;

        private static readonly List<IBreachable> Active = new List<IBreachable>();

        public static void Add(IBreachable target) => Active.Add(target);

        public static void Remove(IBreachable target) => Active.Remove(target);

        /// <summary>
        /// The breachable target closest to the centre of the view within
        /// <see cref="Range"/>, with nothing in between; null when none.
        /// </summary>
        public static IBreachable Find(Camera eyes)
        {
            Vector3 origin = eyes.transform.position;
            Vector3 forward = eyes.transform.forward;
            IBreachable best = null;
            float alignment = MinimumAlignment;
            foreach (var target in Active)
            {
                if (!target.CanBreach)
                {
                    continue;
                }

                Vector3 point = target.BreachPoint;
                Vector3 offset = point - origin;
                if (offset.sqrMagnitude > Range * Range)
                {
                    continue;
                }

                float dot = Vector3.Dot(forward, offset.normalized);
                if (dot < alignment || Physics.Linecast(origin, point, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                alignment = dot;
                best = target;
            }

            return best;
        }

        /// <summary>Whether the target is set and its component still exists (interfaces skip Unity's destroyed check).</summary>
        public static bool Alive(IBreachable target) => target is Object component && component;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Active.Clear();
    }
}
