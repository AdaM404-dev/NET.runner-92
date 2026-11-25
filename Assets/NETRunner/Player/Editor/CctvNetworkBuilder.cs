using System;
using System.Linq;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Player.Editor
{
    /// <summary>
    /// Builds the hospital's CCTV network for LATCH-07: one camera that
    /// renders the selected feed into the tablet, six security viewpoints and
    /// <see cref="SecurityCctv"/>. Moves the tablet onto the viewmodel layer,
    /// which the CCTV camera leaves out, and stops the camera drawing small
    /// clutter beyond 24 m and the expensive screen-space effects.
    /// </summary>
    public static class CctvNetworkBuilder
    {
        private const int LayerCount = 32;
        private const float CameraDepth = -8f;
        private const float NearClip = .075f;
        private const float FarClip = 64f;
        private const float FieldOfView = 54f;
        private const float NearDetailDistance = 24f;

        private static readonly FrameSettingsField[] SkippedEffects =
        {
            FrameSettingsField.SSR, FrameSettingsField.SSAO, FrameSettingsField.Volumetrics, FrameSettingsField.MotionVectors,
            FrameSettingsField.ContactShadows, FrameSettingsField.PlanarProbe,
        };

        // Each viewpoint: its label on the tablet, where the camera hangs and what it looks at.
        private static readonly (string Name, Vector3 At, Vector3 Aim)[] Viewpoints =
        {
            ("PUBLIC ENTRY", new Vector3(-4.9f, 3.18f, -23.2f), new Vector3(0, 1.45f, -20.2f)),
            ("RECEPTION", new Vector3(3.8f, 3.15f, -12.8f), new Vector3(-5.6f, 1.24f, -6.8f)),
            ("EMERGENCY CONCOURSE", new Vector3(-29f, 3.14f, 1.6f), new Vector3(-10f, 1.47f, 0)),
            ("PATIENT WARDS", new Vector3(-22f, 7.34f, 1.56f), new Vector3(-4f, 5.62f, 0)),
            ("SURGERY CONCOURSE", new Vector3(-11f, 11.52f, -1.56f), new Vector3(9f, 9.78f, 0)),
            ("RESEARCH ACCESS", new Vector3(7.3f, 15.70f, 1.58f), new Vector3(13f, 13.95f, 0)),
        };

        /// <summary>
        /// Builds the network under <paramref name="systems"/> and gives it to
        /// <paramref name="latch"/>. The caller adds the terminal that unlocks
        /// it and saves the scene.
        /// </summary>
        public static SecurityCctv Install(LatchViewmodel latch, Transform systems)
        {
            string occupant = HospitalLayers.NameOf(HospitalLayers.Viewmodel);
            if (occupant != "" && occupant != HospitalLayers.ViewmodelName)
            {
                throw new InvalidOperationException("CCTV viewmodel layer slot is already occupied.");
            }

            HospitalLayers.Assign(HospitalLayers.Viewmodel, HospitalLayers.ViewmodelName);
            foreach (Transform part in latch.Model.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = HospitalLayers.Viewmodel;
                PrefabUtility.RecordPrefabInstancePropertyModifications(part.gameObject);
            }

            Transform root = SceneParts.Node("Northline CCTV network", systems);
            Camera camera = FeedCamera(root);
            Transform views = SceneParts.Node("Virtual security viewpoints", root);
            SecurityCctv.Feed[] feeds = Viewpoints.Select((viewpoint, index) =>
            {
                Transform view = SceneParts.Node($"CAM {index + 1:00} / {viewpoint.Name}", views);
                view.SetPositionAndRotation(viewpoint.At, Quaternion.LookRotation(viewpoint.Aim - viewpoint.At));
                return new SecurityCctv.Feed(viewpoint.Name, view);
            }).ToArray();
            SecurityCctv network = root.gameObject.AddComponent<SecurityCctv>();
            network.Configure(camera, feeds);
            latch.SetCctv(network);
            return network;
        }

        private static Camera FeedCamera(Transform root)
        {
            Camera camera = SceneParts.Node("CCTV selected feed camera", root).gameObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.depth = CameraDepth;
            camera.nearClipPlane = NearClip;
            camera.farClipPlane = FarClip;
            camera.fieldOfView = FieldOfView;
            camera.cullingMask = ~(1 << HospitalLayers.Viewmodel);
            var distances = new float[LayerCount];
            distances[HospitalLayers.NearDetail] = NearDetailDistance;
            camera.layerCullDistances = distances;
            HDAdditionalCameraData hd = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            hd.customRenderingSettings = true;
            foreach (FrameSettingsField setting in SkippedEffects)
            {
                hd.renderingPathCustomFrameSettings.SetEnabled(setting, false);
                hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)setting] = true;
            }

            return camera;
        }
    }
}
