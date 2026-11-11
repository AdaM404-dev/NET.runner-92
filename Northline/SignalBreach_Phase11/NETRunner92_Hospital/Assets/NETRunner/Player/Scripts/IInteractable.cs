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
        /// The prompt the HUD shows while the player aims at it: the label of
        /// the key to press (null when no key applies, such as a locked door)
        /// and the text, for example ("E", "Ajtó kinyitása").
        /// </summary>
        (string Key, string Text) Prompt { get; }

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
