using System.Collections;
using System.IO;
using NetRunner.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NetRunner.Player.Tests
{
    /// <summary>
    /// PlayMode tests: they load the real gameplay scene and run frames, like
    /// pressing Play. They replace the old "-nexus-autotest" command-line test.
    /// Screenshots go to Logs/PlayModeTests/ (not committed).
    /// </summary>
    public class PlayerPlayModeTests
    {
        const float Frame = 1f / 60f;

        NexusPlayer player;

        [UnitySetUp]
        public IEnumerator LoadTheGameplayScene()
        {
            // "Warehouse" is Assets/Scenes/Game/Warehouse.unity, first in Build Settings.
            SceneManager.LoadScene("Warehouse");
            yield return null;
            yield return null;

            player = Object.FindAnyObjectByType<NexusPlayer>();
            Assert.That(player, Is.Not.Null, "the gameplay scene has no NexusPlayer");

            // The test moves the player itself, so the script must not read the
            // keyboard; and the mouse goes back to whoever runs the tests.
            player.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Let the player settle on the floor.
            for (int i = 0; i < 90; i++)
            {
                player.Simulate(Vector2.zero, false, false, Frame);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator PlayerStandsWalksAndRunsAtTheMeasuredSpeeds()
        {
            Assert.That(player.Grounded, Is.True, "the player should stand on the floor");

            Vector3 start = player.Position;
            for (int i = 0; i < 120; i++)
            {
                player.Simulate(new Vector2(0f, 1f), false, false, Frame);
                yield return null;
            }

            // 2 seconds at the walk speed of 1.65 m/s.
            Assert.That(Vector3.Distance(start, player.Position), Is.EqualTo(3.3f).Within(0.1f), "walked distance");

            start = player.Position;
            for (int i = 0; i < 30; i++)
            {
                player.Simulate(new Vector2(0f, 1f), true, false, Frame);
                yield return null;
            }

            // Half a second at the run speed of 3.25 m/s.
            Assert.That(Vector3.Distance(start, player.Position), Is.EqualTo(1.625f).Within(0.1f), "run distance");

            yield return SaveScreenshot("walked-third-person.png");
        }

        [UnityTest]
        public IEnumerator PlayerJumpsAboutTwoThirdsOfAMetreAndLands()
        {
            float floor = player.Position.y;
            float peak = floor;
            player.Simulate(Vector2.zero, false, true, Frame);
            yield return null;
            for (int i = 0; i < 60; i++)
            {
                player.Simulate(Vector2.zero, false, false, Frame);
                peak = Mathf.Max(peak, player.Position.y);
                yield return null;
            }

            Assert.That(peak - floor, Is.EqualTo(0.66f).Within(0.06f), "jump height");
            Assert.That(player.Grounded, Is.True, "the player should land again");
        }

        [UnityTest]
        public IEnumerator ADoorNearTheStartOpensWhenToggled()
        {
            var door = GameObject.Find("DOOR_Hall_X-18_4").GetComponent<NexusDoor>();
            Quaternion closed = door.transform.rotation;

            door.Toggle();
            yield return new WaitForSeconds(1f);

            Assert.That(door.IsOpen, Is.True);
            Assert.That(Quaternion.Angle(closed, door.transform.rotation), Is.EqualTo(95f).Within(1f), "swing angle");
        }

        IEnumerator SaveScreenshot(string fileName)
        {
            // Let the script place the camera for one frame (LateUpdate).
            player.enabled = true;
            yield return null;
            player.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Camera camera = player.viewCamera;
            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;

            string folder = Path.Combine(Application.dataPath, "..", "Logs", "PlayModeTests");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, fileName), image.EncodeToPNG());

            Object.Destroy(image);
            target.Release();
            Object.Destroy(target);
        }
    }
}
