using NetRunner.Core;
using NetRunner.Settings;
using UnityEngine.InputSystem;

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

        void Interact(FirstPersonController player);
    }

    /// <summary>
    /// An interactable that takes over the keyboard while it is open, such as
    /// the reception security terminal. Between
    /// <see cref="HospitalInteraction.BeginTerminal"/> and
    /// <see cref="HospitalInteraction.EndTerminal"/> the player's input goes
    /// to <see cref="ReadInput"/> instead of movement and aiming.
    /// </summary>
    public interface ITerminal : IInteractable
    {
        /// <summary>Reads this frame's keys; <paramref name="bindings"/> gives the player's key for leaving it.</summary>
        void ReadInput(Keyboard keyboard, Preferences bindings);

        void Close();
    }
}
