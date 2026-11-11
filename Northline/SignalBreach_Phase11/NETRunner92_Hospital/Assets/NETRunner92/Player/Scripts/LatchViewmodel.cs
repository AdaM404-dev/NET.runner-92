using UnityEngine;
using UnityEngine.InputSystem;

namespace NETRunner92
{
    [RequireComponent(typeof(FirstPersonController), typeof(LatchScreen))]
    public sealed class LatchViewmodel : MonoBehaviour
    {
        public enum Phase { Stowed, Equip, Ready, Tap, Success, Failed, Unequip, Cancel }
        static readonly int[] Motion = { 0, Hash("Equip"), Hash("Idle"), Hash("Interact"),
            Hash("Success"), Hash("Failed"), Hash("Unequip"), Hash("Cancel") };
        const float TouchPoint = .42f;
        const float BreachCooldown = 3f;
        const float FailureDisplay = 1f;

        [SerializeField] GameObject model;
        [SerializeField] Animator animator;
        [SerializeField] LatchScreen screen;
        [SerializeField] SecurityCctv cctv;
        FirstPersonController player;
        LatchRelay relay;
        bool committed, success;
        bool cctvRequested;
        float noticeUntil;
        SignalBreachPuzzle breach;
        K7Patrol breachTarget;
        bool breachApplied;
        float retryAt, failureClosesAt;

        public Phase Current { get; private set; }
        public bool Active => Current != Phase.Stowed;
        public GameObject Model => model;
        public bool CctvAvailable => cctv && cctv.Unlocked;
        public bool CctvHintVisible => Time.unscaledTime < noticeUntil;
        internal bool BreachOpen => breach != null;
        internal bool BreachCapturesLook => BreachOpen &&
            breach.State is SignalBreachPuzzle.Outcome.Pending or SignalBreachPuzzle.Outcome.Tracing;
        internal int BreachRetrySeconds => Mathf.CeilToInt(Mathf.Max(0f, retryAt - Time.time));

        static int Hash(string clip) => Animator.StringToHash("Base Layer.Hack_" + clip);

        public void Configure(GameObject root, Animator animation, LatchScreen liveScreen)
        {
            model = root;
            animator = animation;
            screen = liveScreen;
        }

        public void SetCctv(SecurityCctv network) => cctv = network;

        void Awake()
        {
            player = GetComponent<FirstPersonController>();
            Stow();
        }

        public bool ReadInput(Keyboard keyboard, HospitalInteractable target) => ReadInput(keyboard, target, null);

        internal bool ReadInput(Keyboard keyboard, HospitalInteractable target, K7Patrol robot)
        {
            if (BreachOpen)
            {
                ReadBreachInput(keyboard, Mouse.current);
                return true;
            }
            bool owned = Active;
            if (keyboard == null) return owned;
            if (HospitalKeys.CctvPressed(keyboard)) ToggleCctv();
            else if (HospitalKeys.Pressed(keyboard, GameAction.Tablet))
            {
                if (robot && (Current == Phase.Stowed || Current == Phase.Ready && !relay && !cctvRequested))
                    BeginBreach(robot);
                else Toggle(target as LatchRelay);
            }
            else if (HospitalKeys.Pressed(keyboard, GameAction.Stow) && Active) Cancel();
            else if (HospitalKeys.Pressed(keyboard, GameAction.Interact) && Active) Tap();
            else if (cctvRequested && Current == Phase.Ready)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) StepCctv(-1);
                else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) StepCctv(1);
            }
            return owned || Active;
        }

        internal bool BeginBreach(K7Patrol robot)
        {
            if (BreachOpen || Time.time < retryAt || !robot || !robot.CanBreach
                || Current != Phase.Stowed && (Current != Phase.Ready || relay || cctvRequested)) return false;
            breach = new SignalBreachPuzzle();
            breachTarget = robot;
            breachApplied = false;
            failureClosesAt = 0f;
            if (Current == Phase.Stowed) Toggle(null);
            else StartBreach();
            return true;
        }

        void StartBreach()
        {
            if (!breachTarget || !breachTarget.CanBreach)
            {
                TerminateBreach("SIGNAL LOST");
                return;
            }
            breach.Start(Time.time);
            breachTarget.SetBreachTracing(true);
            screen.PresentBreach(breach, SignalBreachPuzzle.Duration);
        }

        void ReadBreachInput(Keyboard keyboard, Mouse mouse)
        {
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame
                || HospitalKeys.Pressed(keyboard, GameAction.Stow) || HospitalKeys.Pressed(keyboard, GameAction.Tablet)))
            {
                Cancel();
                return;
            }
            if (breach.State != SignalBreachPuzzle.Outcome.Tracing) return;
            breach.Tick(Time.time);
            if (breach.State != SignalBreachPuzzle.Outcome.Tracing) return;

            if (mouse != null)
            {
                breach.MovePointer(mouse.delta.ReadValue() * 1.2f);
                if (mouse.leftButton.wasPressedThisFrame) breach.Click();
            }
            if (keyboard != null && breach.State == SignalBreachPuzzle.Outcome.Tracing)
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
            if (breach.State == SignalBreachPuzzle.Outcome.Pending) return;
            if (breach.State == SignalBreachPuzzle.Outcome.Tracing)
            {
                if (!breachTarget || !breachTarget.CanBreach) TerminateBreach("SIGNAL LOST");
                else breach.Tick(Time.time);
            }

            if (breach.State == SignalBreachPuzzle.Outcome.Successful)
            {
                if (!breachApplied)
                {
                    if (!breachTarget || !breachTarget.TryBreachDisable())
                    {
                        TerminateBreach("SIGNAL LOST");
                        return;
                    }
                    breachApplied = true;
                    breachTarget.SetBreachTracing(false);
                }
                if (breachTarget && breachTarget.BreachRecovery > 0f)
                    screen.PresentBreach(breach, breachTarget.BreachRecovery);
                else CloseBreach();
                return;
            }
            if (breach.State == SignalBreachPuzzle.Outcome.Terminated)
            {
                if (failureClosesAt == 0f) TerminateBreach(breach.Feedback);
                if (Time.time >= failureClosesAt) CloseBreach();
                return;
            }
            screen.PresentBreach(breach, breach.Remaining(Time.time));
        }

        void TerminateBreach(string message)
        {
            breach.Terminate(message);
            if (breachTarget) breachTarget.SetBreachTracing(false);
            retryAt = Time.time + BreachCooldown;
            failureClosesAt = Time.time + FailureDisplay;
            screen.PresentBreach(breach, 0f);
        }

        void ClearBreach()
        {
            if (breachTarget) breachTarget.SetBreachTracing(false);
            breach = null;
            breachTarget = null;
        }

        void CloseBreach()
        {
            ClearBreach();
            Play(Phase.Unequip, .07f);
        }

        public void Toggle(LatchRelay target)
        {
            switch (Current)
            {
                case Phase.Stowed:
                    relay = target;
                    model.SetActive(true);
                    screen.Open();
                    Play(Phase.Equip);
                    animator.Update(0f);
                    break;
                case Phase.Ready:
                    StopCctv();
                    Play(Phase.Unequip, .07f);
                    break;
                case Phase.Unequip:
                case Phase.Cancel:
                    break;
                default:
                    Cancel();
                    break;
            }
        }

        public void Tap()
        {
            if (Current != Phase.Ready || cctvRequested) return;
            committed = success = false;
            Play(Phase.Tap, .055f);
        }

        public void Cancel()
        {
            if (!Active || Current == Phase.Cancel) return;
            if (BreachOpen)
            {
                if (breach.State == SignalBreachPuzzle.Outcome.Successful) CloseBreach();
                else if (breach.State != SignalBreachPuzzle.Outcome.Terminated) TerminateBreach("LINK CANCELLED");
                return;
            }
            StopCctv();
            Play(Phase.Cancel, .055f);
        }

        public bool ToggleCctv()
        {
            if (BreachOpen) return false;
            if (!CctvAvailable)
            {
                noticeUntil = Time.unscaledTime + 3f;
                return false;
            }
            if (cctvRequested)
            {
                Toggle(null);
                return true;
            }
            if (Current != Phase.Stowed && Current != Phase.Equip && Current != Phase.Ready) return false;
            cctvRequested = true;
            noticeUntil = 0f;
            if (Current == Phase.Stowed) Toggle(null);
            else if (Current == Phase.Ready) ShowCctv();
            return true;
        }

        public void StepCctv(int step)
        {
            if (!cctvRequested || !cctv.Watching) return;
            cctv.Step(step);
            screen.PresentFeed(cctv);
        }

        void ShowCctv()
        {
            if (cctv.Open()) screen.PresentFeed(cctv);
        }

        void StopCctv()
        {
            cctvRequested = false;
            if (cctv) cctv.Close();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && Active) Stow();
        }

        public void Stow()
        {
            if (BreachCapturesLook) retryAt = Time.time + BreachCooldown;
            ClearBreach();
            StopCctv();
            Current = Phase.Stowed;
            relay = null;
            if (screen) screen.Close();
            if (model) model.SetActive(false);
        }

        void Update()
        {
            if (Active && player.IsDown) Stow();
            if (BreachOpen) UpdateBreach();
            if (!Active || animator.IsInTransition(0)) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != Motion[(int)Current]) return;
            float progress = state.normalizedTime;
            if (Current == Phase.Tap && progress >= TouchPoint && !committed)
            {
                success = relay && relay.TryLink(player);
                committed = true;
            }
            if (progress < .999f) return;
            switch (Current)
            {
                case Phase.Tap:
                    Play(success ? Phase.Success : Phase.Failed, .045f);
                    break;
                case Phase.Equip:
                case Phase.Success:
                case Phase.Failed:
                    Play(Phase.Ready, .065f);
                    break;
                case Phase.Unequip:
                case Phase.Cancel:
                    Stow();
                    break;
            }
        }

        void Play(Phase phase, float blend = 0f)
        {
            Current = phase;
            if (blend > 0f) animator.CrossFadeInFixedTime(Motion[(int)phase], blend, 0, 0f);
            else animator.Play(Motion[(int)phase], 0, 0f);
            string cancel = HospitalKeys.Label(GameAction.Stow) + "  CANCEL";
            string stow = HospitalKeys.Label(GameAction.Tablet) + "  STOW";
            string link = HospitalKeys.Label(GameAction.Interact) + "  LINK";
            switch (phase)
            {
                case Phase.Equip:
                    screen.Present("BOOTING", "ESTABLISHING LOCAL SESSION", cancel, LatchScreen.Cyan, .25f);
                    break;
                case Phase.Ready:
                    if (BreachOpen)
                    {
                        if (breach.State == SignalBreachPuzzle.Outcome.Pending) StartBreach();
                        break;
                    }
                    if (cctvRequested)
                    {
                        ShowCctv();
                        break;
                    }
                    screen.Present(relay && relay.Linked ? "RELAY ONLINE" : "SIGNAL STANDBY",
                        relay ? "TARGET  /  NORTHLINE SERVICE RELAY" : "TARGET  /  NO RELAY IN VIEW",
                        CctvAvailable ? $"{link}     {stow}     {HospitalKeys.Label(GameAction.Cctv)}  CCTV" : $"{link}     {stow}     {cancel}",
                        relay ? LatchScreen.Cyan : LatchScreen.Amber, relay ? .86f : .14f);
                    break;
                case Phase.Tap:
                    screen.Present("HANDSHAKE", "SENDING SERVICE REQUEST", cancel, LatchScreen.Cyan, .57f);
                    break;
                case Phase.Success:
                    screen.Present("LINK ESTABLISHED", "SERVICE RELAY RESPONDING", $"{stow}     {cancel}", LatchScreen.Cyan, 1f);
                    break;
                case Phase.Failed:
                    screen.Present("LINK FAILED", "EQUIP WHILE AIMING AT A RELAY", $"{stow}     {cancel}", LatchScreen.Fault, .08f);
                    break;
                default:
                    screen.Present("DISCONNECTING", "CLOSING LOCAL SESSION", "", LatchScreen.Muted, .13f);
                    break;
            }
        }
    }
}
