using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] Camera eyes;
        [SerializeField, Min(0.1f)] float walkingSpeed = 3.5f;
        [SerializeField, Min(0.1f)] float sprintSpeed = 5.5f;
        [SerializeField, Min(1f)] float acceleration = 22f;
        [SerializeField, Min(0.01f)] float sensitivity = 0.13f;
        [SerializeField, Range(45f, 89f)] float pitchLimit = 85f;
        [SerializeField, Min(1f)] float gravity = 18f;
        [SerializeField, Min(0.1f)] float groundAdhesion = 4.2f;
        [SerializeField] bool headBob;
        [SerializeField] float eyeHeight = 1.65f;
        [SerializeField] float detailCullDistance = 34f;

        CharacterController capsule;
        Vector3 planarVelocity;
        float verticalSpeed;
        float pitch;
        float stride;
        bool hasGroundContact;
        bool suppressLook;
        HospitalInteraction interaction;

        public bool IsGrounded => hasGroundContact;
        public CharacterController Capsule => capsule;
        public Camera Eyes => eyes;

        public void SetCamera(Camera camera) => eyes = camera;

        void Awake()
        {
            capsule = GetComponent<CharacterController>();
            interaction = GetComponent<HospitalInteraction>();
            if (!eyes) eyes = GetComponentInChildren<Camera>();
            eyes.transform.localPosition = Vector3.up * eyeHeight;
            float[] distances = new float[32];
            distances[8] = detailCullDistance;
            eyes.layerCullDistances = distances;
        }

        void Start() => CaptureCursor(true);

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) CaptureCursor(false);
            if (Cursor.lockState != CursorLockMode.Locked && mouse != null && mouse.leftButton.wasPressedThisFrame)
                CaptureCursor(true);

            Vector2 input = Vector2.zero;
            bool sprint = false;
            if (Cursor.lockState == CursorLockMode.Locked && !(interaction && interaction.TerminalActive))
            {
                if (mouse != null && !suppressLook) Look(mouse.delta.ReadValue());
                suppressLook = false;
                if (keyboard != null)
                {
                    input = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                                        (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                    sprint = keyboard.leftShiftKey.isPressed;
                }
            }
            Vector3 wish = transform.right * input.x + transform.forward * input.y;
            Move(wish, sprint, Time.deltaTime);
        }

        public void CaptureCursor(bool capture)
        {
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
            suppressLook = capture;
        }

        public void Look(Vector2 delta)
        {
            transform.Rotate(0f, delta.x * sensitivity, 0f);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -pitchLimit, pitchLimit);
            eyes.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        public void Move(Vector3 wish, bool sprint, float dt)
        {
            wish.y = 0f;
            var target = Vector3.ClampMagnitude(wish, 1f) * (sprint ? sprintSpeed : walkingSpeed);
            planarVelocity = Vector3.MoveTowards(planarVelocity, target, acceleration * dt);
            float remaining = Mathf.Min(dt, 0.10f);
            while (remaining > 0.00001f)
            {
                float step = Mathf.Min(remaining, 1f / 90f);
                verticalSpeed = hasGroundContact ? -groundAdhesion : Mathf.Max(-34f, verticalSpeed - gravity * step);
                var hit = capsule.Move((planarVelocity + Vector3.up * verticalSpeed) * step);
                hasGroundContact = (hit & CollisionFlags.Below) != 0 || capsule.isGrounded;
                if ((hit & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
                remaining -= step;
            }

            stride += planarVelocity.magnitude * dt * 3f;
            float bob = headBob && hasGroundContact ? Mathf.Sin(stride) * Mathf.Min(0.021f, planarVelocity.magnitude * 0.004f) : 0f;
            eyes.transform.localPosition = Vector3.up * (eyeHeight + bob);
        }

        public void FollowPlatform(float deltaY)
        {
            capsule.enabled = false;
            transform.position += Vector3.up * deltaY;
            capsule.enabled = true;
        }

        public void Warp(Vector3 position, Quaternion rotation)
        {
            capsule.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            capsule.enabled = true;
            planarVelocity = Vector3.zero;
            verticalSpeed = 0;
            hasGroundContact = false;
            pitch = 0;
            eyes.transform.localRotation = Quaternion.identity;
        }
    }
}

