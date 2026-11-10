using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.Player
{
    [RequireComponent(typeof(FirstPersonController), typeof(LatchScreen))]
    public sealed class LatchViewmodel : MonoBehaviour
    {
        public enum Phase { Stowed, Equip, Ready, Tap, Success, Failed, Unequip, Cancel }
        static readonly int[] Motion = { 0, Hash("Equip"), Hash("Idle"), Hash("Interact"),
            Hash("Success"), Hash("Failed"), Hash("Unequip"), Hash("Cancel") };
        const float TouchPoint = .42f;

        [SerializeField] GameObject model;
        [SerializeField] Animator animator;
        [SerializeField] LatchScreen screen;
        [SerializeField] SecurityCctv cctv;
        FirstPersonController player;
        LatchRelay relay;
        bool committed, success;
        bool cctvRequested;
        float noticeUntil;

        public Phase Current { get; private set; }
        public bool Active => Current != Phase.Stowed;
        public GameObject Model => model;
        public bool CctvAvailable => cctv && cctv.Unlocked;
        public bool CctvHintVisible => Time.unscaledTime < noticeUntil;
        public string CctvHint => CctvAvailable ? "CCTV ready   /   Press 1" : "CCTV locked   /   Use the reception terminal";

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

        public bool ReadInput(Keyboard keyboard, HospitalInteractable target)
        {
            bool owned = Active;
            if (keyboard == null) return owned;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) ToggleCctv();
            else if (keyboard.qKey.wasPressedThisFrame) Toggle(target as LatchRelay);
            else if (keyboard.rKey.wasPressedThisFrame && Active) Cancel();
            else if (keyboard.eKey.wasPressedThisFrame && Active) Tap();
            else if (cctvRequested && Current == Phase.Ready)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) StepCctv(-1);
                else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) StepCctv(1);
            }
            return owned || Active;
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
            StopCctv();
            Play(Phase.Cancel, .055f);
        }

        public bool ToggleCctv()
        {
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
            StopCctv();
            Current = Phase.Stowed;
            relay = null;
            if (screen) screen.Close();
            if (model) model.SetActive(false);
        }

        void Update()
        {
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
            switch (phase)
            {
                case Phase.Equip:
                    screen.Present("BOOTING", "ESTABLISHING LOCAL SESSION", "R  CANCEL", LatchScreen.Cyan, .25f);
                    break;
                case Phase.Ready:
                    if (cctvRequested)
                    {
                        ShowCctv();
                        break;
                    }
                    screen.Present(relay && relay.Linked ? "RELAY ONLINE" : "SIGNAL STANDBY",
                        relay ? "TARGET  /  NORTHLINE SERVICE RELAY" : "TARGET  /  NO RELAY IN VIEW",
                        CctvAvailable ? "E  LINK     Q  STOW     1  CCTV" : "E  LINK     Q  STOW     R  CANCEL",
                        relay ? LatchScreen.Cyan : LatchScreen.Amber, relay ? .86f : .14f);
                    break;
                case Phase.Tap:
                    screen.Present("HANDSHAKE", "SENDING SERVICE REQUEST", "R  CANCEL", LatchScreen.Cyan, .57f);
                    break;
                case Phase.Success:
                    screen.Present("LINK ESTABLISHED", "SERVICE RELAY RESPONDING", "Q  STOW     R  CANCEL", LatchScreen.Cyan, 1f);
                    break;
                case Phase.Failed:
                    screen.Present("LINK FAILED", "EQUIP WHILE AIMING AT A RELAY", "Q  STOW     R  CANCEL", LatchScreen.Fault, .08f);
                    break;
                default:
                    screen.Present("DISCONNECTING", "CLOSING LOCAL SESSION", "", LatchScreen.Muted, .13f);
                    break;
            }
        }
    }
}
