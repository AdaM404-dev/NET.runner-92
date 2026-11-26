using NetRunner.Core;
using UnityEngine;

namespace NetRunner.Player.Tests
{
    /// <summary>Something to aim at in the interaction tests; it counts its uses.</summary>
    public sealed class TestInteractable : MonoBehaviour, IInteractable
    {
        /// <summary>How often <see cref="Interact"/> was called.</summary>
        public int Uses { get; private set; }

        /// <inheritdoc/>
        public (GameAction? Action, string Text) Prompt => (GameAction.Interact, "Test");

        /// <inheritdoc/>
        public void Interact(FirstPersonController player)
        {
            Uses++;
        }
    }
}
