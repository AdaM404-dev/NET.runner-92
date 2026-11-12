using NetRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.Player
{
    /// <summary>
    /// The LATCH-07 tablet in the player's hands. It reads Q, E, R, 1 and the
    /// arrow keys, plays the arms' animation clip for each phase, shows the
    /// status or the CCTV feed on the tablet's screen, and links the service
    /// relay at the moment the finger touches the screen. What each key does
    /// in each phase, and when a clip moves the tablet on, is Core's
    /// <see cref="LatchSession"/>; the words on the screen are
    /// <see cref="LatchStatus"/>.
    /// </summary>
    [RequireComponent(typeof(FirstPersonController), typeof(LatchScreen))]
    public sealed class LatchViewmodel : MonoBehaviour
    {
        // The animator state of each phase's clip, in LatchPhase order; Stowed has none.
        private static readonly int[] Clips =
        {
            0, ClipHash("Equip"), ClipHash("Idle"), ClipHash("Interact"), ClipHash("Success"), ClipHash("Failed"),
            ClipHash("Unequip"), ClipHash("Cancel"),
        };

        // Seconds of crossfade into each phase's clip, in LatchPhase order; 0 starts the clip outright.
        private static readonly float[] Blends = { 0f, 0f, .065f, .055f, .045f, .045f, .07f, .055f };

        [SerializeField] private GameObject model;
        [SerializeField] private Animator animator;
        [SerializeField] private LatchScreen screen;
        [SerializeField] private SecurityCctv cctv;

        private readonly LatchSession session = new LatchSession();
        private FirstPersonController player;
        private LatchRelay relay;
        private const float BreachCooldown = 3f;
        private const float FailureDisplay = 1f;
        private const float BreachPointerGain = 1.2f;
        private readonly System.Random random = new System.Random();
        private SignalBreach breach;
        private IBreachable breachTarget;
        private float breachDisableSeconds;
        private bool breachApplied;
        private float retryAt;
        private float failureClosesAt;

        /// <summary>The phase the tablet is in.</summary>
        public LatchPhase Current => session.Current;

        /// <summary>Whether the tablet is out.</summary>
        public bool Active => session.Active;

        /// <summary>The arms and tablet model, shown only while the tablet is out.</summary>
        public GameObject Model => model;

        /// <summary>Whether the reception breach has unlocked the CCTV.</summary>
        public bool CctvAvailable => cctv && cctv.Unlocked;
        public bool BreachOpen => breach != null;
        public bool BreachCapturesLook => BreachOpen &&
            breach.State is SignalBreach.Outcome.Pending or SignalBreach.Outcome.Tracing;
        public int BreachRetrySeconds => Mathf.CeilToInt(Mathf.Max(0f, retryAt - Time.time));

        /// <summary>Whether the CCTV hint is up over the reticle.</summary>
        public bool CctvHintVisible => session.IsHintShown(Time.unscaledTime);

        /// <summary>The CCTV hint's text.</summary>
        public string CctvHint => LatchStatus.CctvHint(CctvAvailable);

        private void Awake()
        {
            player = GetComponent<FirstPersonController>();
            session.PhaseChanged += HandlePhaseChanged;
            session.CctvOpened += ShowCctv;
            Stow();
        }

        private void Update()
        {
            if (Active && player.IsDown) Stow();
            if (BreachOpen) UpdateBreach();
            if (!Active || animator.IsInTransition(0))
            {
                return;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != Clips[(int)Current])
            {
                return;
            }

            session.Advance(state.normalizedTime, LinkRelay);
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus && Active)
            {
                Stow();
            }
        }

        /// <summary>Sets the arms model, its animator and the screen; the LATCH-07 installer calls it.</summary>
        public void Configure(GameObject root, Animator animation, LatchScreen liveScreen)
        {
            model = root;
            animator = animation;
            screen = liveScreen;
        }

        /// <summary>Sets the CCTV network shown on the tablet; the CCTV installer calls it.</summary>
        public void SetCctv(SecurityCctv network)
        {
            cctv = network;
        }

        public bool ReadInput(Keyboard keyboard, IInteractable target) => ReadInput(keyboard, target, null);

        public bool ReadInput(Keyboard keyboard, IInteractable target, IBreachable robot)
        {
            if (BreachOpen)
            {
                ReadBreachInput(keyboard, Mouse.current);
                return true;
            }
            bool owned = Active;
            if (keyboard == null)
            {
                return owned;
            }

            if (player.Preferences.CctvPressed(keyboard))
            {
                ToggleCctv();
            }
            else if (player.Preferences.Pressed(keyboard, GameAction.Tablet))
            {
                if (BreachTargets.Alive(robot) && (Current == LatchPhase.Stowed ||
                    Current == LatchPhase.Ready && !relay && !session.CctvRequested))
                {
                    BeginBreach(robot);
                }
                else
                {
                    Toggle(target as LatchRelay);
                }
            }
            else if (player.Preferences.Pressed(keyboard, GameAction.Stow) && Active)
            {
                Cancel();
            }
            else if (player.Preferences.Pressed(keyboard, GameAction.Interact) && Active)
            {
                Tap();
            }
            else if (session.CctvRequested && Current == LatchPhase.Ready)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                {
                    StepCctv(-1);
                }
                else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                {
                    StepCctv(1);
                }
            }

            return owned || Active;
        }

        public bool BeginBreach(IBreachable robot)
        {
            if (BreachOpen || Time.time < retryAt || !BreachTargets.Alive(robot) || !robot.CanBreach
                || Current != LatchPhase.Stowed && (Current != LatchPhase.Ready || relay || session.CctvRequested)) return false;
            breach = new SignalBreach(random);
            breachTarget = robot;
            breachDisableSeconds = robot.DisableSeconds;
            breachApplied = false;
            failureClosesAt = 0f;
            if (Current == LatchPhase.Stowed) Toggle(null);
            else StartBreach();
            return true;
        }

        void StartBreach()
        {
            if (!BreachTargets.Alive(breachTarget) || !breachTarget.CanBreach)
            {
                TerminateBreach("SIGNAL LOST");
                return;
            }
            breach.Begin(Time.time);
            breachTarget.SetBreachTracing(true);
            screen.PresentBreach(breach, SignalBreach.Duration, breachDisableSeconds, player.Preferences.Active.ReduceMotion);
        }

        void ReadBreachInput(Keyboard keyboard, Mouse mouse)
        {
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame
                || player.Preferences.Pressed(keyboard, GameAction.Stow) || player.Preferences.Pressed(keyboard, GameAction.Tablet)))
            {
                Cancel();
                return;
            }
            if (breach.State != SignalBreach.Outcome.Tracing) return;
            breach.Tick(Time.time);
            if (breach.State != SignalBreach.Outcome.Tracing) return;

            if (mouse != null)
            {
                var delta = mouse.delta.ReadValue() * BreachPointerGain;
                breach.MovePointer(delta.x, delta.y);
                if (mouse.leftButton.wasPressedThisFrame) breach.Click();
            }
            if (keyboard != null && breach.State == SignalBreach.Outcome.Tracing)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) breach.StepFocus(-1);
                else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) breach.StepFocus(1);
                if (keyboard.backspaceKey.wasPressedThisFrame) breach.Undo();
                else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) breach.Confirm();
            }
            UpdateBreach();
        }

        void UpdateBreach()
        {
            if (breach.State == SignalBreach.Outcome.Pending) return;
            if (breach.State == SignalBreach.Outcome.Tracing)
            {
                if (!BreachTargets.Alive(breachTarget) || !breachTarget.CanBreach) TerminateBreach("SIGNAL LOST");
                else breach.Tick(Time.time);
            }

            if (breach.State == SignalBreach.Outcome.Successful)
            {
                if (!breachApplied)
                {
                    if (!BreachTargets.Alive(breachTarget) || !breachTarget.TryBreachDisable())
                    {
                        TerminateBreach("SIGNAL LOST");
                        return;
                    }
                    breachApplied = true;
                    breachTarget.SetBreachTracing(false);
                }
                if (BreachTargets.Alive(breachTarget) && breachTarget.BreachRecovery > 0f)
                    screen.PresentBreach(breach, breachTarget.BreachRecovery, breachDisableSeconds, player.Preferences.Active.ReduceMotion);
                else CloseBreach();
                return;
            }
            if (breach.State == SignalBreach.Outcome.Terminated)
            {
                if (failureClosesAt == 0f) TerminateBreach(breach.Feedback);
                if (Time.time >= failureClosesAt) CloseBreach();
                return;
            }
            screen.PresentBreach(breach, breach.Remaining(Time.time), breachDisableSeconds, player.Preferences.Active.ReduceMotion);
        }

        void TerminateBreach(string message)
        {
            breach.Terminate(message);
            if (BreachTargets.Alive(breachTarget)) breachTarget.SetBreachTracing(false);
            retryAt = Time.time + BreachCooldown;
            failureClosesAt = Time.time + FailureDisplay;
            screen.PresentBreach(breach, 0f, breachDisableSeconds, player.Preferences.Active.ReduceMotion);
        }

        void ClearBreach()
        {
            if (BreachTargets.Alive(breachTarget)) breachTarget.SetBreachTracing(false);
            breach = null;
            breachTarget = null;
        }

        void CloseBreach()
        {
            ClearBreach();
            session.Toggle();
        }

        public void Toggle(LatchRelay target)
        {
            if (!Active)
            {
                relay = target;
            }

            session.Toggle();
        }

        /// <summary>E: taps Link.</summary>
        public void Tap()
        {
            session.Tap();
        }

        /// <summary>R: cancels and puts the tablet away.</summary>
        public void Cancel()
        {
            if (!Active || Current == LatchPhase.Cancel) return;
            if (BreachOpen)
            {
                if (breach.State == SignalBreach.Outcome.Successful) CloseBreach();
                else if (breach.State != SignalBreach.Outcome.Terminated) TerminateBreach("LINK CANCELLED");
                return;
            }
            session.Cancel();
        }

        /// <summary>1: opens or closes the CCTV page, or shows the hint while it is locked.</summary>
        public bool ToggleCctv()
        {
            if (BreachOpen) return false;
            return session.ToggleCctv(CctvAvailable, Time.unscaledTime);
        }

        /// <summary>Shows the next (+1) or previous (−1) camera while the CCTV page is up.</summary>
        public void StepCctv(int step)
        {
            if (!session.CctvRequested || !cctv.Watching)
            {
                return;
            }

            cctv.Step(step);
            screen.PresentFeed(cctv);
        }

        /// <summary>Puts the tablet away at once and hides the arms.</summary>
        public void Stow()
        {
            if (BreachCapturesLook) retryAt = Time.time + BreachCooldown;
            ClearBreach();
            session.Stow();
        }

        private static int ClipHash(string clip)
        {
            return Animator.StringToHash("Base Layer.Hack_" + clip);
        }

        private void HandlePhaseChanged(LatchPhase phase)
        {
            if (phase == LatchPhase.Stowed)
            {
                CloseCctv();
                relay = null;
                if (screen)
                {
                    screen.Close();
                }

                if (model)
                {
                    model.SetActive(false);
                }

                return;
            }

            if (phase == LatchPhase.Equip)
            {
                model.SetActive(true);
                screen.Open();
            }
            else if (phase == LatchPhase.Unequip || phase == LatchPhase.Cancel)
            {
                CloseCctv();
            }

            PlayClip(phase);
            if (phase == LatchPhase.Ready && BreachOpen)
            {
                StartBreach();
                return;
            }

            if (phase == LatchPhase.Ready && session.CctvRequested)
            {
                // The CCTV page shows instead (LatchSession.CctvOpened).
                return;
            }

            var status = LatchStatus.For(phase, relay, relay && relay.Linked, CctvAvailable,
                player.Preferences.Label(GameAction.Interact), player.Preferences.Label(GameAction.Tablet),
                player.Preferences.Label(GameAction.Stow), player.Preferences.Label(GameAction.Cctv));
            screen.Present(status);
        }

        private void PlayClip(LatchPhase phase)
        {
            int clip = Clips[(int)phase];
            float blend = Blends[(int)phase];
            if (blend > 0f)
            {
                animator.CrossFadeInFixedTime(clip, blend, 0, 0f);
            }
            else
            {
                animator.Play(clip, 0, 0f);
            }

            if (phase == LatchPhase.Equip)
            {
                // Pose the arms at once, so the first rendered frame already shows them raising.
                animator.Update(0f);
            }
        }

        private bool LinkRelay()
        {
            return relay && relay.TryLink(player);
        }

        private void ShowCctv()
        {
            if (cctv.Open())
            {
                screen.PresentFeed(cctv);
            }
        }

        private void CloseCctv()
        {
            if (cctv)
            {
                cctv.Close();
            }
        }
    }
}
