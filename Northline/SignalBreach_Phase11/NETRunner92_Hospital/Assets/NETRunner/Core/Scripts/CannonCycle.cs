using System;

namespace NetRunner.Core
{
    /// <summary>
    /// The timing of the K-7's forearm cannon (Phase 11). While the robot
    /// sees the player the cannon deploys over <see cref="DeploySeconds"/>:
    /// first the armour hatches open, then the emitters slide out. Fully
    /// deployed and on target, it charges for <see cref="ChargeSeconds"/>,
    /// fires one pulse that does <see cref="Damage"/>, and then recovers for
    /// <see cref="RecoverySeconds"/> before it can charge again. Losing sight
    /// of the player, or the aim, drops the charge. Plain maths: the
    /// component checks sight and aim and moves the bones; this class keeps
    /// the timers, moved on with <c>dt</c>.
    /// </summary>
    public sealed class CannonCycle
    {
        /// <summary>Seconds from stowed to fully deployed, and back.</summary>
        public const float DeploySeconds = .48f;

        public const float ChargeSeconds = .85f;
        public const float RecoverySeconds = 1.45f;

        /// <summary>Seconds the pulse stays visible after a shot.</summary>
        public const float PulseSeconds = .12f;

        /// <summary>Health a hit takes off the player, out of 100.</summary>
        public const float Damage = 25f;

        // The hatches open over the first 74 % of the deployment, the emitters slide out over the last 74 %.
        private const float HatchSpan = .74f;
        private const float EmitterDelay = .26f;

        private float charge;
        private float recovery;
        private float pulse;

        /// <summary>0 stowed to 1 deployed.</summary>
        public float Deployment { get; private set; }

        /// <summary>How far the armour hatches stand open, 0 to 1, eased.</summary>
        public float HatchOpening => Easing.SmoothStep01(Math.Clamp(Deployment / HatchSpan, 0f, 1f));

        /// <summary>How far the emitters stand out, 0 to 1, eased.</summary>
        public float EmitterExtension => Easing.SmoothStep01(Math.Clamp((Deployment - EmitterDelay) / HatchSpan, 0f, 1f));

        public bool PulseVisible => pulse > 0f;

        /// <summary>Whether it may aim precisely and charge: fully deployed and not recovering.</summary>
        public bool CanCharge => recovery <= 0f && Deployment >= 1f;

        /// <summary>
        /// Moves the timers on by <paramref name="dt"/> seconds and deploys
        /// towards out (the player is seen) or in (not seen).
        /// </summary>
        public void Step(float dt, bool seesPlayer)
        {
            pulse -= dt;
            recovery = Math.Max(0f, recovery - dt);
            if (!seesPlayer)
            {
                charge = 0f;
            }

            float target = seesPlayer ? 1f : 0f;
            float maxStep = dt / DeploySeconds;
            Deployment = Math.Abs(target - Deployment) <= maxStep ? target : Deployment + Math.Sign(target - Deployment) * maxStep;
        }

        /// <summary>
        /// Charges while <paramref name="onTarget"/>; returns true on the frame
        /// it fires, which starts the pulse and the recovery. Off target, the
        /// charge is lost.
        /// </summary>
        public bool Charge(float dt, bool onTarget)
        {
            if (!onTarget || !CanCharge)
            {
                charge = 0f;
                return false;
            }

            charge += dt;
            if (charge < ChargeSeconds)
            {
                return false;
            }

            charge = 0f;
            pulse = PulseSeconds;
            recovery = RecoverySeconds;
            return true;
        }

        /// <summary>Stows it at once, as a successful Signal Breach does.</summary>
        public void Suspend()
        {
            charge = 0f;
            pulse = 0f;
            Deployment = 0f;
        }
    }
}
