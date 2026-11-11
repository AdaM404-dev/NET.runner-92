using System;
using System.Collections;
using NetRunner.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
        [SerializeField] float eyeHeight = 1.65f;
        [SerializeField] float detailCullDistance = 34f;

        CharacterController capsule;
        Vector3 planarVelocity;
        float verticalSpeed;
        float pitch;
        float stride;
        bool hasGroundContact;
        bool suppressLook, sprintLatched;
        Vector2 smoothedLook;
        public float ViewPitch => pitch;
        HospitalInteraction interaction;

        public bool IsGrounded => hasGroundContact;
        public CharacterController Capsule => capsule;
        public Camera Eyes => eyes;
        public float Health { get; private set; } = 100f;
        public float LastHitTime { get; private set; } = float.NegativeInfinity;
        public bool IsDown => Health <= 0f;
        public Vector3 TargetCenter => capsule.transform.TransformPoint(capsule.center);

        /// <summary>True while the game is paused (the pause menu is open): the player ignores input and damage.</summary>
        public bool InputPaused { get; private set; }

        /// <summary>
        /// Raised 1.5 s after health reaches 0. The run controller in Menu
        /// restarts from the last checkpoint; without one, the scene reloads.
        /// </summary>
        public event Action Defeated;

        public void PauseInput(bool paused) => InputPaused = paused;

        public void TakeDamage(float amount)
        {
            if (IsDown || InputPaused) return;
            Health = Mathf.Max(0f, Health - amount);
            LastHitTime = Time.unscaledTime;
            if (IsDown) StartCoroutine(RetryAfterDefeat());
        }

        IEnumerator RetryAfterDefeat()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (Defeated != null) Defeated();
            else SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }

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
            GameOptions.UseCamera(eyes);
        }

        void Start() => CaptureCursor(true);

        void Update()
        {
            if (InputPaused || IsDown) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var options = GameOptions.Active;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame &&
                !(interaction && (interaction.TerminalActive || interaction.BreachOpen)))
                CaptureCursor(false);
            if (Cursor.lockState != CursorLockMode.Locked && mouse != null && mouse.leftButton.wasPressedThisFrame)
                CaptureCursor(true);

            Vector2 input = Vector2.zero;
            bool sprint = false;
            if (Cursor.lockState == CursorLockMode.Locked && !(interaction && interaction.TerminalActive))
            {
                if (mouse != null && !suppressLook && !(interaction && interaction.BreachCapturesLook))
                {
                    var delta = mouse.delta.ReadValue();
                    smoothedLook = options.smoothing <= 0
                        ? delta : Vector2.Lerp(smoothedLook, delta, 1f - options.smoothing);
                    var look = smoothedLook * options.sensitivity;
                    if (options.invertY) look.y = -look.y;
                    Look(look);
                }
                suppressLook = false;
                if (keyboard != null)
                {
                    input = new Vector2(
                        (HospitalKeys.Held(keyboard, GameAction.Right) ? 1 : 0) - (HospitalKeys.Held(keyboard, GameAction.Left) ? 1 : 0),
                        (HospitalKeys.Held(keyboard, GameAction.Forward) ? 1 : 0) - (HospitalKeys.Held(keyboard, GameAction.Back) ? 1 : 0));
                    if (options.sprintToggle)
                    {
                        if (HospitalKeys.Pressed(keyboard, GameAction.Sprint)) sprintLatched = !sprintLatched;
                        sprint = sprintLatched;
                    }
                    else
                    {
                        sprintLatched = false;
                        sprint = HospitalKeys.Held(keyboard, GameAction.Sprint);
                    }
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
            smoothedLook = Vector2.zero;
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
            var options = GameOptions.Active;
            float amplitude = options.reducedMotion ? 0f : options.headBob;
            float bob = amplitude > 0 && hasGroundContact
                ? amplitude * Mathf.Sin(stride) * Mathf.Min(.021f, planarVelocity.magnitude * .004f) : 0f;
            eyes.transform.localPosition = Vector3.up * (eyeHeight + bob);
        }

        public void RestorePitch(float value)
        {
            pitch = Mathf.Clamp(value, -pitchLimit, pitchLimit);
            eyes.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
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

