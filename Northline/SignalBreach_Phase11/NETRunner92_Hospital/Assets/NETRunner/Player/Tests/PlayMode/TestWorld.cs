using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NetRunner.Player.Tests
{
    /// <summary>
    /// Builds what the player tests need from plain objects: a player with the
    /// hospital player's capsule and eyes, a floor, boxes and a service relay,
    /// with the hospital out of the way (<see cref="HideHospital"/>). The
    /// player's own Update is switched off, so a test moves it frame by frame
    /// with <see cref="FirstPersonController.Move"/>. <see cref="Clear"/>
    /// destroys everything that was built.
    /// </summary>
    public sealed class TestWorld
    {
        // The hospital player's capsule, from docs/systems/player.md.
        private const float CapsuleHeight = 1.8f;
        private const float CapsuleRadius = .3f;
        private const float StepOffset = .35f;
        private const float SlopeLimit = 50f;

        private const float FloorSize = 80f;
        private const string IgnoreRaycastLayer = "Ignore Raycast";
        private const string HospitalScene = "Hospital_Exploration";

        private readonly List<GameObject> built = new List<GameObject>();
        private readonly List<GameObject> hidden = new List<GameObject>();

        /// <summary>
        /// Switches the hospital off if an earlier test left it loaded, so that
        /// none of its walls is in the way; <see cref="Clear"/> switches it on
        /// again. It looks for the hospital by name: the test runner's own
        /// scene also has a build index during a run, and switching that off
        /// stops the run.
        /// </summary>
        public void HideHospital()
        {
            Scene hospital = SceneManager.GetSceneByName(HospitalScene);
            if (!hospital.isLoaded)
            {
                return;
            }

            foreach (GameObject root in hospital.GetRootGameObjects())
            {
                if (root.activeSelf)
                {
                    root.SetActive(false);
                    hidden.Add(root);
                }
            }
        }

        /// <summary>A player standing with its feet at <paramref name="feet"/>, facing +z.</summary>
        public FirstPersonController Player(Vector3 feet)
        {
            var body = new GameObject("Test player");
            body.SetActive(false);
            body.transform.position = feet;
            var eyes = new GameObject("Test eyes").AddComponent<Camera>();
            eyes.transform.SetParent(body.transform, false);
            eyes.enabled = false;
            var capsule = body.AddComponent<CharacterController>();
            capsule.height = CapsuleHeight;
            capsule.radius = CapsuleRadius;
            capsule.center = new Vector3(0f, CapsuleHeight * .5f, 0f);
            capsule.stepOffset = StepOffset;
            capsule.slopeLimit = SlopeLimit;
            var player = body.AddComponent<FirstPersonController>();
            built.Add(body);
            body.SetActive(true);
            player.enabled = false;
            Physics.SyncTransforms();
            return player;
        }

        /// <summary>A large flat floor whose top is at y = 0.</summary>
        public void Floor()
        {
            Box("Test floor", new Vector3(0f, -.5f, 0f), new Vector3(FloorSize, 1f, FloorSize));
        }

        /// <summary>A solid box centred at <paramref name="centre"/>.</summary>
        public GameObject Box(string name, Vector3 centre, Vector3 size)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = centre;
            box.transform.localScale = size;
            built.Add(box);
            Physics.SyncTransforms();
            return box;
        }

        /// <summary>A box that rays ignore, on the Ignore Raycast layer.</summary>
        public GameObject IgnoredBox(string name, Vector3 centre, Vector3 size)
        {
            GameObject box = Box(name, centre, size);
            box.layer = LayerMask.NameToLayer(IgnoreRaycastLayer);
            return box;
        }

        /// <summary>
        /// A LATCH-07 service relay like the hospital's: a face with a trigger
        /// collider and a lamp beside it with no collider.
        /// </summary>
        public LatchRelay Relay(Vector3 centre, out Renderer lamp)
        {
            var face = Box("Test relay face", centre, new Vector3(.32f, .26f, .04f));
            face.GetComponent<BoxCollider>().isTrigger = true;
            var light = Box("Test relay lamp", centre + new Vector3(.5f, 0f, 0f), new Vector3(.23f, .043f, .016f));
            Object.DestroyImmediate(light.GetComponent<BoxCollider>());
            lamp = light.GetComponent<Renderer>();
            var relay = face.AddComponent<LatchRelay>();
            relay.Configure(lamp);
            Physics.SyncTransforms();
            return relay;
        }

        /// <summary>An empty object to hang components on.</summary>
        public GameObject Empty(string name, Vector3 position)
        {
            var empty = new GameObject(name);
            empty.transform.position = position;
            built.Add(empty);
            return empty;
        }

        /// <summary>
        /// Destroys everything this world built, at once, and switches the
        /// hospital back on: tests run back to
        /// back within a frame, and a wall left over from the last test would
        /// block the next one.
        /// </summary>
        public void Clear()
        {
            foreach (GameObject item in built)
            {
                if (item)
                {
                    Object.DestroyImmediate(item);
                }
            }

            built.Clear();
            foreach (GameObject root in hidden)
            {
                if (root)
                {
                    root.SetActive(true);
                }
            }

            hidden.Clear();
        }
    }
}
