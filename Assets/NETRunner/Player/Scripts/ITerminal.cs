using UnityEngine.InputSystem;

namespace NetRunner.Player
{
    /// <summary>
    /// An interactable that takes over the keyboard while it is open, such as
    /// the reception security terminal. Between
    /// <see cref="HospitalInteraction.BeginTerminal"/> and
    /// <see cref="HospitalInteraction.EndTerminal"/> the player's input goes
    /// to <see cref="ReadInput"/> instead of movement and aiming.
    /// </summary>
    public interface ITerminal : IInteractable
    {
        /// <summary>Reads this frame's keys; called every frame while the terminal is open.</summary>
        void ReadInput(Keyboard keyboard);

        /// <summary>Closes the terminal, for example when the cursor is freed.</summary>
        void Close();
    }
}
