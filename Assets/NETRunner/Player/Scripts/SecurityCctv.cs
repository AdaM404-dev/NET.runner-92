using System;
using UnityEngine;

namespace NetRunner.Player
{
    public sealed class SecurityCctv : MonoBehaviour
    {
        [Serializable]
        public struct Feed
        {
            public string label;
            public Transform view;
        }

        const float FrameInterval = 1f / 15f;
        [SerializeField] Camera feedCamera;
        [SerializeField] Feed[] feeds;
        RenderTexture surface;
        float nextFrame;
        int selected;

        public bool Unlocked { get; private set; }
        public bool Watching { get; private set; }
        public int Selected => selected;
        public int Count => feeds.Length;
        public string Label => feeds[selected].label;
        public RenderTexture Surface => surface;
        public Camera FeedCamera => feedCamera;

        public void Configure(Camera camera, Feed[] views)
        {
            feedCamera = camera;
            feeds = views;
        }

        public void Unlock() => Unlocked = true;

        public bool Open()
        {
            if (!Unlocked || Count == 0) return false;
            if (!surface)
            {
                surface = new RenderTexture(576, 288, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    name = "Northline live CCTV",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                surface.Create();
                feedCamera.targetTexture = surface;
            }
            Watching = true;
            ApplyView();
            return true;
        }

        public void Step(int direction)
        {
            if (!Watching) return;
            selected = (selected + direction + Count) % Count;
            ApplyView();
        }

        void ApplyView()
        {
            var view = feeds[selected].view;
            feedCamera.transform.SetPositionAndRotation(view.position, view.rotation);
            nextFrame = 0f;
        }

        public void Close()
        {
            Watching = false;
            if (feedCamera) feedCamera.enabled = false;
        }

        void LateUpdate()
        {
            if (!Watching) return;
            bool due = Time.unscaledTime >= nextFrame;
            feedCamera.enabled = due;
            if (due) nextFrame = Time.unscaledTime + FrameInterval;
        }

        void OnDisable() => Close();

        void OnDestroy()
        {
            if (!surface) return;
            if (feedCamera) feedCamera.targetTexture = null;
            surface.Release();
            Destroy(surface);
        }
    }
}
