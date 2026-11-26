namespace NetRunner.Core
{
    /// <summary>
    /// The player's rebindable actions, in the order the settings menu lists
    /// them and the preferences file stores their keys. Prompts name an
    /// action instead of a key, so they follow the player's bindings.
    /// </summary>
    public enum GameAction
    {
        Forward,
        Back,
        Left,
        Right,
        Sprint,
        Interact,
        Tablet,
        Cctv,
        Stow,
    }
}
