using NetRunner.Core;

namespace NetRunner.Player
{
    /// <summary>
    /// Something the player can use: a door, a lift button, a terminal. The
    /// player's <see cref="HospitalInteraction"/> finds it on the collider it
    /// aims at (or a parent), the HUD shows <see cref="Prompt"/>, and
    /// <see cref="Interact"/> is called when the interact key is pressed.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// The prompt the HUD shows while the player aims at it: the action
        /// whose key to press (null when none applies, such as a locked door)
        /// and the text, for example (Interact, "Ajtó kinyitása"). The HUD
        /// shows the key the player bound to the action.
        /// </summary>
        (GameAction? Action, string Text) Prompt { get; }

        /// <summary>Uses it; <paramref name="player"/> is the player who pressed E.</summary>
        void Interact(FirstPersonController player);
    }
}
