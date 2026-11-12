namespace NetRunner.Core
{
    /// <summary>
    /// The colour of the LATCH-07 screen's heading. <c>LatchScreen</c> turns
    /// each into the actual colour; <c>Core</c> has no colours.
    /// </summary>
    public enum LatchAccent
    {
        /// <summary>Working normally.</summary>
        Cyan,

        /// <summary>Waiting without a target.</summary>
        Amber,

        /// <summary>Something went wrong.</summary>
        Fault,

        /// <summary>Shutting down.</summary>
        Muted,
    }
}
