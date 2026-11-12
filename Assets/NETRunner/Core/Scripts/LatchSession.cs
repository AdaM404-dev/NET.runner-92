using System;

namespace NetRunner.Core
{
    /// <summary>
    /// One use of the LATCH-07 tablet, from raising it to putting it away:
    /// which <see cref="LatchPhase"/> it is in, what Q (<see cref="Toggle"/>),
    /// E (<see cref="Tap"/>), R (<see cref="Cancel"/>) and 1
    /// (<see cref="ToggleCctv"/>) do in each phase, and how the end of a
    /// phase's animation clip moves it on (<see cref="Advance"/>). A tap
    /// commits at <see cref="TouchPoint"/> of the touch clip, when the finger
    /// reaches the screen, so cancelling before that leaves the relay alone.
    /// The component plays the clip of each phase it is told about through
    /// <see cref="PhaseChanged"/>. Plain C# with no scene objects, so it is
    /// tested without starting the game (see LatchSessionTests).
    /// </summary>
    public sealed class LatchSession
    {
        /// <summary>How far through the touch clip (0–1) the finger reaches the screen and the tap commits.</summary>
        public const float TouchPoint = .42f;

        /// <summary>A clip counts as played to its end from this point (0–1).</summary>
        public const float ClipEnd = .999f;

        /// <summary>Seconds the CCTV hint stays up after 1 was pressed while the CCTV is locked.</summary>
        public const float HintSeconds = 3f;

        private bool committed;
        private bool linked;
        private float hintUntil;

        /// <summary>The tablet entered a phase: play its clip, or hide the arms for <see cref="LatchPhase.Stowed"/>.</summary>
        public event Action<LatchPhase> PhaseChanged;

        /// <summary>The CCTV page is to be shown now, in place of the status page.</summary>
        public event Action CctvOpened;

        /// <summary>The phase the tablet is in; it starts stowed.</summary>
        public LatchPhase Current { get; private set; }

        /// <summary>Whether the tablet is out, in any phase but <see cref="LatchPhase.Stowed"/>.</summary>
        public bool Active => Current != LatchPhase.Stowed;

        /// <summary>Whether 1 asked for the CCTV page and it has not been closed since.</summary>
        public bool CctvRequested { get; private set; }

        /// <summary>
        /// Q: raises a stowed tablet, puts it away from Ready, and cancels
        /// anything else it is doing. While it is already being put away, Q
        /// does nothing.
        /// </summary>
        public void Toggle()
        {
            switch (Current)
            {
                case LatchPhase.Stowed:
                    Enter(LatchPhase.Equip);
                    break;
                case LatchPhase.Ready:
                    CctvRequested = false;
                    Enter(LatchPhase.Unequip);
                    break;
                case LatchPhase.Unequip:
                case LatchPhase.Cancel:
                    break;
                default:
                    Cancel();
                    break;
            }
        }

        /// <summary>E: taps the screen to link a relay, only while Ready and not showing the CCTV.</summary>
        public void Tap()
        {
            if (Current != LatchPhase.Ready || CctvRequested)
            {
                return;
            }

            committed = false;
            linked = false;
            Enter(LatchPhase.Tap);
        }

        /// <summary>R: stops whatever the tablet is doing and puts it away.</summary>
        public void Cancel()
        {
            if (!Active || Current == LatchPhase.Cancel)
            {
                return;
            }

            CctvRequested = false;
            Enter(LatchPhase.Cancel);
        }

        /// <summary>
        /// 1: while the CCTV is locked, only shows the hint for
        /// <see cref="HintSeconds"/>. Once unlocked it raises the tablet with
        /// the CCTV page, shows the page at once if the tablet is Ready, or,
        /// when the page was asked for already, closes it and puts the tablet
        /// away. Returns whether anything happened besides the hint.
        /// </summary>
        /// <param name="available">Whether the CCTV is unlocked.</param>
        /// <param name="now">The time in seconds, for the hint.</param>
        public bool ToggleCctv(bool available, float now)
        {
            if (!available)
            {
                hintUntil = now + HintSeconds;
                return false;
            }

            if (CctvRequested)
            {
                Toggle();
                return true;
            }

            if (Current != LatchPhase.Stowed && Current != LatchPhase.Equip && Current != LatchPhase.Ready)
            {
                return false;
            }

            CctvRequested = true;
            hintUntil = 0f;
            if (Current == LatchPhase.Stowed)
            {
                Toggle();
            }
            else if (Current == LatchPhase.Ready)
            {
                CctvOpened?.Invoke();
            }

            return true;
        }

        /// <summary>Whether the CCTV hint is up at <paramref name="now"/> (seconds).</summary>
        public bool IsHintShown(float now)
        {
            return now < hintUntil;
        }

        /// <summary>
        /// Moves the session on by how far the current phase's clip has played
        /// (0–1). During a tap, the first progress from <see cref="TouchPoint"/>
        /// on asks <paramref name="tryLink"/>, once, whether the relay linked;
        /// a clip played to its end leads to the next phase.
        /// </summary>
        public void Advance(float progress, Func<bool> tryLink)
        {
            if (Current == LatchPhase.Tap && progress >= TouchPoint && !committed)
            {
                linked = tryLink();
                committed = true;
            }

            if (progress < ClipEnd)
            {
                return;
            }

            switch (Current)
            {
                case LatchPhase.Tap:
                    Enter(linked ? LatchPhase.Success : LatchPhase.Failed);
                    break;
                case LatchPhase.Equip:
                case LatchPhase.Success:
                case LatchPhase.Failed:
                    Enter(LatchPhase.Ready);
                    break;
                case LatchPhase.Unequip:
                case LatchPhase.Cancel:
                    Stow();
                    break;
            }
        }

        /// <summary>
        /// Puts the tablet away at once, without a clip: when its clip ends,
        /// at the start, when the window loses focus or the cursor is freed.
        /// Always announces <see cref="LatchPhase.Stowed"/>, even when it was
        /// stowed already, so the arms and the screen are surely hidden.
        /// </summary>
        public void Stow()
        {
            CctvRequested = false;
            Enter(LatchPhase.Stowed);
        }

        private void Enter(LatchPhase phase)
        {
            Current = phase;
            PhaseChanged?.Invoke(phase);
            if (phase == LatchPhase.Ready && CctvRequested)
            {
                CctvOpened?.Invoke();
            }
        }
    }
}
