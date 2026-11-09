using NetRunner.Core;
using UnityEngine;

namespace NetRunner.World
{
    /// <summary>
    /// A warehouse door that swings open (hinged leaf) or lifts (loading
    /// shutter) when toggled. The pose it starts in counts as closed.
    /// </summary>
    public sealed class NexusDoor : MonoBehaviour
    {
        private const float SwingOpenSpeed = 1.3f;
        private const float LiftOpenSpeed = 0.45f;

        [Tooltip("A loading shutter that lifts instead of swinging.")]
        [SerializeField] private bool loadingDoor;

        [Tooltip("How far a hinged leaf swings, in degrees.")]
        [SerializeField] private float angle = 95;

        [Tooltip("How far a loading shutter lifts, in metres.")]
        [SerializeField] private float lift = 3.8f;

        private Quaternion closedRotation;
        private Vector3 closedPosition;
        private float progress;

        public bool IsOpen { get; private set; }

        public bool IsLoadingDoor => loadingDoor;

        public float SwingAngle => angle;

        public float LiftHeight => lift;

        private void Awake()
        {
            closedRotation = transform.rotation;
            closedPosition = transform.position;
        }

        private void Update()
        {
            float openSpeed = loadingDoor ? LiftOpenSpeed : SwingOpenSpeed;
            progress = Mathf.MoveTowards(progress, IsOpen ? 1 : 0, Time.deltaTime * openSpeed);
            float eased = Easing.SmoothStep01(progress);
            if (loadingDoor)
            {
                transform.position = closedPosition + Vector3.up * (lift * eased);
            }
            else
            {
                transform.rotation = Quaternion.AngleAxis(angle * eased, Vector3.up) * closedRotation;
            }
        }

        public void Toggle()
        {
            IsOpen = !IsOpen;
        }
    }
}
