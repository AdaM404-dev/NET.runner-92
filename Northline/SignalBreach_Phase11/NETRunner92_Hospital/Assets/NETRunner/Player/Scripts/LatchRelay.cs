using NetRunner.Core;
using UnityEngine;

namespace NetRunner.Player
{
    /// <summary>
    /// The LATCH-07 service relay by the south gate. Aimed at, it offers the
    /// tablet (Q); a tap on the tablet links it when the player's eyes are
    /// within <see cref="HospitalInteraction.Reach"/> and can see it, and its
    /// indicator lamp turns cyan.
    /// </summary>
    public sealed class LatchRelay : MonoBehaviour, IInteractable
    {
        private static readonly int Emission = Shader.PropertyToID("_EmissiveColor");
        private static readonly Color LinkedGlow = new Color(.19f, 1f, .79f) * 2f;

        [SerializeField] private Renderer indicator;

        [Tooltip("Layers that block the link; by default everything but Ignore Raycast.")]
        [SerializeField] private LayerMask sightMask = Physics.DefaultRaycastLayers;

        /// <summary>Whether a tap linked it in this play session.</summary>
        public bool Linked { get; private set; }
        public (GameAction? Action, string Text) Prompt => (GameAction.Tablet, Linked ? "Szervizrelé csatlakoztatva" : "LATCH-07 · Szervizrelé");

        /// <summary>Sets the indicator lamp; the relay's installer calls it.</summary>
        public void Configure(Renderer lamp)
        {
            indicator = lamp;
        }

        /// <inheritdoc/>
        public void Interact(FirstPersonController player)
        {
            LatchViewmodel latch = player.GetComponent<LatchViewmodel>();
            if (latch)
            {
                latch.Toggle(this);
            }
        }

        /// <summary>
        /// Links the relay if the player's eyes are within reach and nothing
        /// solid is in between, and lights the lamp. Returns whether it linked.
        /// </summary>
        public bool TryLink(FirstPersonController player)
        {
            Vector3 eye = player.Eyes.transform.position;
            Vector3 contact = transform.position;
            if ((eye - contact).sqrMagnitude > HospitalInteraction.Reach * HospitalInteraction.Reach
                || Physics.Linecast(eye, contact, sightMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            RestoreLink();
            return true;
        }

        public void RestoreLink()
        {
            Linked = true;
            if (!indicator) return;
            var block = new MaterialPropertyBlock();
            indicator.GetPropertyBlock(block);
            block.SetColor(Emission, LinkedGlow);
            indicator.SetPropertyBlock(block);
        }
    }
}
