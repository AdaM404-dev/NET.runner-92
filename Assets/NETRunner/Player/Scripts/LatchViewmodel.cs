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

        /// <summary>The phase the tablet is in.</summary>
        public LatchPhase Current => session.Current;

        /// <summary>Whether the tablet is out.</summary>
        public bool Active => session.Active;

        /// <summary>The arms and tablet model, shown only while the tablet is out.</summary>
        public GameObject Model => model;

        /// <summary>Whether the reception breach has unlocked the CCTV.</summary>
        public bool CctvAvailable => cctv && cctv.Unlocked;

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

        /// <summary>
        /// Reads this frame's tablet keys. Returns true when the tablet has the
        /// keyboard, so E does not also use what the player aims at.
        /// </summary>
        public bool ReadInput(Keyboard keyboard, IInteractable target)
        {
            bool owned = Active;
            if (keyboard == null)
            {
                return owned;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                ToggleCctv();
            }
            else if (keyboard.qKey.wasPressedThisFrame)
            {
                Toggle(target as LatchRelay);
            }
            else if (keyboard.rKey.wasPressedThisFrame && Active)
            {
                Cancel();
            }
            else if (keyboard.eKey.wasPressedThisFrame && Active)
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

        /// <summary>
        /// Q: raises the tablet, acquiring <paramref name="target"/> (the relay
        /// aimed at, or null), puts it away from Ready, or cancels.
        /// </summary>
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
            session.Cancel();
        }

        /// <summary>1: opens or closes the CCTV page, or shows the hint while it is locked.</summary>
        public bool ToggleCctv()
        {
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
            if (phase == LatchPhase.Ready && session.CctvRequested)
            {
                // The CCTV page shows instead (LatchSession.CctvOpened).
                return;
            }

            screen.Present(LatchStatus.For(phase, relay, relay && relay.Linked, CctvAvailable));
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
