using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Player.Editor
{
    /// <summary>
    /// Builds the first-person player at the hospital's south gate: a capsule
    /// character controller, the eyes with the HDRP camera and the audio
    /// listener, and <see cref="FirstPersonController"/>. Saves it as
    /// <c>PlayerController.prefab</c> and keeps the scene's copy linked to it.
    /// </summary>
    public static class PlayerPrefabBuilder
    {
        private const string PrefabPath = HospitalAssets.Player + "/Prefabs/PlayerController.prefab";
        private const string MainCameraTag = "MainCamera";

        // The capsule: 1.8 m tall, 0.3 m radius, steps up to 0.35 m and slopes to 50°.
        private const float Height = 1.80f;
        private const float Radius = .30f;
        private const float StepOffset = .35f;
        private const float SlopeLimit = 50f;
        private const float SkinWidth = .034f;

        private const float EyeHeight = 1.65f;
        private const float FieldOfView = 68f;
        private const float NearClip = .055f;
        private const float FarClip = 150f;

        private static readonly Vector3 SouthGate = new Vector3(-.55f, .03f, -36.45f);

        /// <summary>Adds the player to the open scene, saves the prefab and returns its controller.</summary>
        public static FirstPersonController Add()
        {
            Transform player = SceneParts.Node("PlayerController");
            player.position = SouthGate;
            CharacterController body = player.gameObject.AddComponent<CharacterController>();
            body.height = Height;
            body.radius = Radius;
            body.center = Vector3.up * (Height / 2f);
            body.stepOffset = StepOffset;
            body.slopeLimit = SlopeLimit;
            body.skinWidth = SkinWidth;
            body.minMoveDistance = 0f;
            Transform eyes = SceneParts.Node("First-person eyes", player);
            eyes.localPosition = Vector3.up * EyeHeight;
            eyes.gameObject.tag = MainCameraTag;
            Camera camera = eyes.gameObject.AddComponent<Camera>();
            camera.fieldOfView = FieldOfView;
            camera.nearClipPlane = NearClip;
            camera.farClipPlane = FarClip;
            eyes.gameObject.AddComponent<AudioListener>();
            eyes.gameObject.AddComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            FirstPersonController controller = player.gameObject.AddComponent<FirstPersonController>();
            controller.SetCamera(camera);
            PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject, PrefabPath, InteractionMode.AutomatedAction);
            return controller;
        }
    }
}
