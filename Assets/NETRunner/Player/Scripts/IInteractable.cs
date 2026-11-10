using UnityEngine.InputSystem;

namespace NetRunner.Player
{
    /// <summary>
    /// Something the player can use: a door, a lift button, a terminal. The
    /// player's <see cref="HospitalInteraction"/> finds it on the collider it
    /// aims at (or a parent), shows <see cref="Prompt"/> and calls
    /// <see cref="Interact"/> when E is pressed.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>The text shown while the player aims at it, for example "E  Open door" or "Locked".</summary>
        string Prompt { get; }

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
        void ReadInput(Keyboard keyboard);

        void Close();
    }
}
