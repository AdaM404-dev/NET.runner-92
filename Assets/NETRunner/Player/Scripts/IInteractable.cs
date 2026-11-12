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

        /// <summary>Uses it; <paramref name="player"/> is the player who pressed E.</summary>
        void Interact(FirstPersonController player);
    }
}
