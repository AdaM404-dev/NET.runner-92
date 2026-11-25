namespace NetRunner.Core
{
    /// <summary>
    /// What the LATCH-07 tablet is doing. Every phase but
    /// <see cref="Stowed"/> plays its own animation clip; <see cref="LatchSession"/>
    /// says how one phase leads to the next.
    /// </summary>
    public enum LatchPhase
    {
        /// <summary>Put away; the arms are hidden.</summary>
        Stowed,

        /// <summary>Being raised; the tablet boots.</summary>
        Equip,

        /// <summary>Raised and waiting for a key.</summary>
        Ready,

        /// <summary>A finger reaching for the screen to link the relay.</summary>
        Tap,

        /// <summary>The relay linked.</summary>
        Success,

        /// <summary>The tap found no relay in reach.</summary>
        Failed,

        /// <summary>Being put away normally.</summary>
        Unequip,

        /// <summary>Being put away after a cancel.</summary>
        Cancel,
    }
}
