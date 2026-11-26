using System;

namespace NetRunner.Core
{
    /// <summary>
    /// What the LATCH-07 screen's status page shows in a phase: a heading, a
    /// line of detail, the keys that work, the heading's colour and the
    /// signal strength (0–1) that lights the bars. <see cref="For"/> chooses
    /// it; <c>LatchScreen</c> draws it. Plain data and rules (see
    /// LatchStatusTests).
    /// </summary>
    public readonly struct LatchStatus
    {
        /// <summary>A status page.</summary>
        public LatchStatus(string heading, string message, string commands, LatchAccent accent, float strength)
        {
            Heading = heading;
            Message = message;
            Commands = commands;
            Accent = accent;
            Strength = strength;
        }

        /// <summary>The big line at the top, such as "SIGNAL STANDBY".</summary>
        public string Heading { get; }

        /// <summary>The line of detail under the heading.</summary>
        public string Message { get; }

        /// <summary>The keys that work now, at the bottom of the page.</summary>
        public string Commands { get; }

        /// <summary>The heading's colour.</summary>
        public LatchAccent Accent { get; }

        /// <summary>Signal strength from 0 to 1.</summary>
        public float Strength { get; }

        /// <summary>
        /// The status page for a phase. While Ready it depends on whether a
        /// relay was in view when the tablet was raised, whether that relay is
        /// linked, and whether the CCTV is unlocked (then 1 is offered).
        /// </summary>
        public static LatchStatus For(LatchPhase phase, bool relayAcquired, bool relayLinked, bool cctvAvailable,
            string interactKey = "E", string tabletKey = "Q", string cancelKey = "R", string cctvKey = "1")
        {
            switch (phase)
            {
                case LatchPhase.Equip:
                    return new LatchStatus("BOOTING", "ESTABLISHING LOCAL SESSION", $"{cancelKey}  CANCEL", LatchAccent.Cyan, .25f);
                case LatchPhase.Ready:
                    return new LatchStatus(
                        relayLinked ? "RELAY ONLINE" : "SIGNAL STANDBY",
                        relayAcquired ? "TARGET  /  NORTHLINE SERVICE RELAY" : "TARGET  /  NO RELAY IN VIEW",
                        cctvAvailable ? $"{interactKey}  LINK     {tabletKey}  STOW     {cctvKey}  CCTV"
                            : $"{interactKey}  LINK     {tabletKey}  STOW     {cancelKey}  CANCEL",
                        relayAcquired ? LatchAccent.Cyan : LatchAccent.Amber,
                        relayAcquired ? .86f : .14f);
                case LatchPhase.Tap:
                    return new LatchStatus("HANDSHAKE", "SENDING SERVICE REQUEST", $"{cancelKey}  CANCEL", LatchAccent.Cyan, .57f);
                case LatchPhase.Success:
                    return new LatchStatus("LINK ESTABLISHED", "SERVICE RELAY RESPONDING", $"{tabletKey}  STOW     {cancelKey}  CANCEL", LatchAccent.Cyan, 1f);
                case LatchPhase.Failed:
                    return new LatchStatus("LINK FAILED", "EQUIP WHILE AIMING AT A RELAY", $"{tabletKey}  STOW     {cancelKey}  CANCEL", LatchAccent.Fault, .08f);
                default:
                    return new LatchStatus("DISCONNECTING", "CLOSING LOCAL SESSION", string.Empty, LatchAccent.Muted, .13f);
            }
        }

        /// <summary>
        /// The hint over the reticle after 1 is pressed: whether the CCTV is
        /// ready, or where to unlock it.
        /// </summary>
        public static string CctvHint(bool available)
        {
            return available ? "CCTV ready   /   Press 1" : "CCTV locked   /   Use the reception terminal";
        }

        /// <summary>
        /// How many of a meter's <paramref name="bars"/> light up: the strength's
        /// share, rounded to the nearest bar (a half rounds to even, as Unity's
        /// <c>Mathf.RoundToInt</c> does).
        /// </summary>
        public int LitBars(int bars)
        {
            float lit = Strength * bars;
            return (int)Math.Round(lit);
        }
    }
}
