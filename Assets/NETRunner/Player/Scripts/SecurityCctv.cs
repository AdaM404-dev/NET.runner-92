using System;
using NetRunner.Core;
using UnityEngine;

namespace NetRunner.Player
{
    /// <summary>
    /// The hospital's security cameras, watched on the LATCH-07 once the
    /// reception breach has unlocked them: one HDRP camera moves between the
    /// feeds' viewpoints and renders the selected one into a texture, at most
    /// <see cref="CctvFeeds.FramesPerSecond"/> times a second and only while
    /// it is watched. Which feed comes next is Core's <see cref="CctvFeeds"/>.
    /// </summary>
    public sealed class SecurityCctv : MonoBehaviour
    {
        /// <summary>One camera viewpoint and its name on the tablet.</summary>
        [Serializable]
        public struct Feed
        {
            [SerializeField] private string label;
            [SerializeField] private Transform view;

            /// <summary>A viewpoint called <paramref name="label"/> at <paramref name="view"/>.</summary>
            public Feed(string label, Transform view)
            {
                this.label = label;
                this.view = view;
            }

            /// <summary>The name shown on the tablet.</summary>
            public string Label => label;

            /// <summary>Where the camera stands and looks.</summary>
            public Transform View => view;
        }

        // The feed texture, in pixels.
        private const int FeedWidth = 576;
        private const int FeedHeight = 288;
        private const int DepthBits = 24;

        [SerializeField] private Camera feedCamera;
        [SerializeField] private Feed[] feeds;

        private RenderTexture surface;
        private float nextFrame;
        private int selected;

        /// <summary>Whether the reception breach has unlocked the cameras.</summary>
        public bool Unlocked { get; private set; }

        /// <summary>Whether the tablet shows a feed now.</summary>
        public bool Watching { get; private set; }

        /// <summary>The index of the selected feed.</summary>
        public int Selected => selected;

        /// <summary>How many feeds there are.</summary>
        public int Count => feeds.Length;

        /// <summary>The selected feed's name.</summary>
        public string Label => feeds[selected].Label;

        /// <summary>The texture the selected feed renders into.</summary>
        public RenderTexture Surface => surface;

        /// <summary>The camera that renders the feeds.</summary>
        public Camera FeedCamera => feedCamera;

        private void LateUpdate()
        {
            if (!Watching)
            {
                return;
            }

            float now = Time.unscaledTime;
            bool due = CctvFeeds.IsFrameDue(now, nextFrame);
            feedCamera.enabled = due;
            if (due)
            {
                nextFrame = CctvFeeds.NextFrame(now);
            }
        }

        private void OnDisable()
        {
            Close();
        }

        private void OnDestroy()
        {
            if (!surface)
            {
                return;
            }

            if (feedCamera)
            {
                feedCamera.targetTexture = null;
            }

            surface.Release();
            Destroy(surface);
        }

        /// <summary>Sets the camera and the viewpoints; the CCTV installer calls it.</summary>
        public void Configure(Camera camera, Feed[] views)
        {
            feedCamera = camera;
            feeds = views;
        }

        /// <summary>Unlocks the cameras for this play session; the reception terminal calls it.</summary>
        public void Unlock()
        {
            Unlocked = true;
        }

        /// <summary>Starts showing the selected feed. Returns false while locked or without feeds.</summary>
        public bool Open()
        {
            if (!Unlocked || Count == 0)
            {
                return false;
            }

            if (!surface)
            {
                surface = new RenderTexture(FeedWidth, FeedHeight, DepthBits, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    name = "Northline live CCTV",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                surface.Create();
                feedCamera.targetTexture = surface;
            }

            Watching = true;
            ApplyView();
            return true;
        }

        /// <summary>Selects the next (+1) or previous (−1) feed, wrapping round; only while watching.</summary>
        public void Step(int direction)
        {
            if (!Watching)
            {
                return;
            }

            selected = CctvFeeds.Step(selected, direction, Count);
            ApplyView();
        }

        /// <summary>Stops showing feeds and switches the camera off.</summary>
        public void Close()
        {
            Watching = false;
            if (feedCamera)
            {
                feedCamera.enabled = false;
            }
        }

        // Moves the camera to the selected viewpoint and renders it at once.
        private void ApplyView()
        {
            Transform view = feeds[selected].View;
            feedCamera.transform.SetPositionAndRotation(view.position, view.rotation);
            nextFrame = 0f;
        }
    }
}
