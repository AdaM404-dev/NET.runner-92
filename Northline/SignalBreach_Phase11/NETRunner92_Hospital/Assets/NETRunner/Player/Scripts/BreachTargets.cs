using System.Collections.Generic;
using UnityEngine;

namespace NetRunner.Player
{
    /// <summary>
    /// Choosing the breachable target the player aims at: the one closest to
    /// the centre of the view within <see cref="Range"/> metres and about
    /// 12.8°, with nothing in between. Stateless; the targets come from the
    /// player's <see cref="HospitalInteraction"/>.
    /// </summary>
    public static class BreachTargets
    {
        /// <summary>Metres: farther away than this nothing can be breached.</summary>
        public const float Range = 8f;

        // Cosine of the largest angle between the view and the target: about 12.8°.
        private const float MinimumAlignment = .975f;

        /// <summary>The target to breach among <paramref name="targets"/>, or null when none qualifies.</summary>
        public static IBreachable Find(Camera eyes, IReadOnlyList<IBreachable> targets)
        {
            Vector3 origin = eyes.transform.position;
            Vector3 forward = eyes.transform.forward;
            IBreachable best = null;
            float alignment = MinimumAlignment;
            foreach (var target in targets)
            {
                if (!Alive(target) || !target.CanBreach)
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
    }
}
