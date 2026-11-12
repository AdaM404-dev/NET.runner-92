using System;
using System.Collections;
using NetRunner.Core;
using NetRunner.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NetRunner.Player
{
    /// <summary>
    /// The hospital player's body: walking, sprinting, gravity and mouse look
    /// on a <see cref="CharacterController"/>, with a camera as its eyes. It
    /// reads W, A, S, D, Shift and the mouse, takes the numbers from Core's
    /// <see cref="PlanarMotion"/>, <see cref="VerticalMotion"/>,
    /// <see cref="MotionSteps"/> and <see cref="ViewMotion"/>, and moves the
    /// capsule and the camera. Other code moves it with <see cref="Move"/>,
    /// <see cref="Look"/>, <see cref="Warp"/> and <see cref="FollowPlatform"/>
    /// (the lift).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        // Objects on this layer, set up by HospitalOptimization, are culled beyond detailCullDistance.
        private const string NearDetailLayer = "Near-detail geometry";
        private const int LayerCount = 32;

        [SerializeField] private Camera eyes;
        [SerializeField, Min(0.1f)] private float walkingSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 5.5f;
        [SerializeField, Min(1f)] private float acceleration = 22f;
        [SerializeField, Min(0.01f)] private float sensitivity = 0.13f;
        [SerializeField, Range(45f, 89f)] private float pitchLimit = 85f;
        [SerializeField, Min(1f)] private float gravity = 18f;
        [SerializeField, Min(0.1f)] private float groundAdhesion = 4.2f;
        [SerializeField] private Preferences preferences;
        private bool sprintLatched;
        private Vector2 smoothedLook;

        public float ViewPitch => pitch;
        public Camera Eyes => eyes;
        [SerializeField] private float eyeHeight = 1.65f;
        [SerializeField] private float detailCullDistance = 34f;

        private CharacterController capsule;
        private HospitalInteraction interaction;
        private Vector3 planarVelocity;
        private float verticalSpeed;
        private float pitch;
        private float stride;
        private bool hasGroundContact;
        private bool suppressLook;

        /// <summary>Whether the capsule touched the floor in its last movement step.</summary>
        public bool IsGrounded => hasGroundContact;

        public CharacterController Capsule => capsule;

        /// <summary>The player's settings and key bindings; the player's other components read them from here.</summary>
        public Preferences Preferences => preferences;
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

        /// <summary>Gives it the player's settings; called by the hospital's installer and tests.</summary>
        public void SetPreferences(Preferences settings)
        {
            preferences = settings;
        }


        void Awake()
        {
            capsule = GetComponent<CharacterController>();
            interaction = GetComponent<HospitalInteraction>();
            if (!eyes)
            {
                eyes = GetComponentInChildren<Camera>();
            }

            eyes.transform.localPosition = Vector3.up * eyeHeight;
            eyes.layerCullDistances = DetailCullDistances();
            if (preferences) preferences.UseCamera(eyes);
        }

        private void Start()
        {
            CaptureCursor(true);
        }

        private void Update()
        {
            if (InputPaused || IsDown || !preferences) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var options = preferences.Active;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame &&
                !(interaction && (interaction.TerminalActive || interaction.BreachOpen)))
                CaptureCursor(false);
            if (Cursor.lockState != CursorLockMode.Locked && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                CaptureCursor(true);
            }

            Vector2 input = Vector2.zero;
            bool sprint = false;
            if (Cursor.lockState == CursorLockMode.Locked && !(interaction && interaction.TerminalActive))
            {
                if (mouse != null && !suppressLook && !(interaction && interaction.BreachCapturesLook))
                {
                    var delta = mouse.delta.ReadValue();
                    smoothedLook = options.Smoothing <= 0
                        ? delta : Vector2.Lerp(smoothedLook, delta, 1f - options.Smoothing);
                    var look = smoothedLook * options.Sensitivity;
                    if (options.YInverted) look.y = -look.y;
                    Look(look);
                }
                suppressLook = false;
                if (keyboard != null)
                {
                    input = new Vector2(
                        (preferences.Held(keyboard, GameAction.Right) ? 1 : 0) - (preferences.Held(keyboard, GameAction.Left) ? 1 : 0),
                        (preferences.Held(keyboard, GameAction.Forward) ? 1 : 0) - (preferences.Held(keyboard, GameAction.Back) ? 1 : 0));
                    if (options.SprintToggles)
                    {
                        if (preferences.Pressed(keyboard, GameAction.Sprint)) sprintLatched = !sprintLatched;
                        sprint = sprintLatched;
                    }
                    else
                    {
                        sprintLatched = false;
                        sprint = preferences.Held(keyboard, GameAction.Sprint);
                    }
                }
            }

            Vector3 wish = transform.right * input.x + transform.forward * input.y;
            Move(wish, sprint, Time.deltaTime);
        }

        /// <summary>Sets the camera that serves as the eyes; the scene builder calls it.</summary>
        public void SetCamera(Camera camera)
        {
            eyes = camera;
        }

        /// <summary>Locks and hides the cursor for mouse look, or frees it.</summary>
        public void CaptureCursor(bool capture)
        {
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
            suppressLook = capture;
            smoothedLook = Vector2.zero;
        }

        /// <summary>Turns the body and tilts the eyes by a mouse movement.</summary>
        public void Look(Vector2 delta)
        {
            transform.Rotate(0f, ViewMotion.Turn(delta.x, sensitivity), 0f);
            pitch = ViewMotion.Pitch(pitch, delta.y, sensitivity, pitchLimit);
            eyes.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        /// <summary>
        /// Moves the player for one frame of <paramref name="dt"/> seconds
        /// towards <paramref name="wish"/>, a direction in the scene whose
        /// height is ignored.
        /// </summary>
        public void Move(Vector3 wish, bool sprint, float dt)
        {
            float speed = sprint ? sprintSpeed : walkingSpeed;
            (float X, float Z) target = PlanarMotion.Target(wish.x, wish.z, speed);
            (float x, float z) = PlanarMotion.Approach(planarVelocity.x, planarVelocity.z, target.X, target.Z, acceleration * dt);
            planarVelocity = new Vector3(x, 0f, z);

            float remaining = MotionSteps.Budget(dt);
            for (float step = MotionSteps.Next(remaining); step > 0f; step = MotionSteps.Next(remaining))
            {
                verticalSpeed = VerticalMotion.Step(verticalSpeed, hasGroundContact, gravity, groundAdhesion, step);
                CollisionFlags hit = capsule.Move((planarVelocity + Vector3.up * verticalSpeed) * step);
                hasGroundContact = (hit & CollisionFlags.Below) != 0 || capsule.isGrounded;
                if ((hit & CollisionFlags.Above) != 0)
                {
                    verticalSpeed = VerticalMotion.AfterHittingCeiling(verticalSpeed);
                }

                remaining -= step;
            }

            float walking = PlanarMotion.Speed(planarVelocity.x, planarVelocity.z);
            stride = ViewMotion.Stride(stride, walking, dt);
            var options = preferences ? preferences.Active : null;
            float amplitude = options != null && !options.ReduceMotion ? options.HeadBob : 0f;
            float bob = amplitude * ViewMotion.Bob(stride, walking, hasGroundContact, amplitude > 0f);
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

        /// <summary>Puts the player somewhere else, standing still and looking level.</summary>
        public void Warp(Vector3 position, Quaternion rotation)
        {
            capsule.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            capsule.enabled = true;
            planarVelocity = Vector3.zero;
            verticalSpeed = 0f;
            hasGroundContact = false;
            pitch = 0f;
            eyes.transform.localRotation = Quaternion.identity;
        }

        private float[] DetailCullDistances()
        {
            var distances = new float[LayerCount];
            int layer = LayerMask.NameToLayer(NearDetailLayer);
            if (layer >= 0)
            {
                distances[layer] = detailCullDistance;
            }
            else
            {
                Debug.LogWarning($"Player: no layer named \"{NearDetailLayer}\", so nothing is culled by distance.");
            }

            return distances;
        }
    }
}
