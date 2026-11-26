using System.Linq;
using NetRunner.Player;
using NetRunner.Player.Editor;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Installs the CCTV in the hospital: LATCH-07 first, then the camera
    /// network (<see cref="CctvNetworkBuilder"/>), and the reception desk's
    /// right monitor as the <see cref="ReceptionSecurityTerminal"/> that
    /// unlocks it, with a small status strip. Saves the scene. Does nothing
    /// when the network is already there.
    /// </summary>
    public static class HospitalCctvInstaller
    {
        private const string ReceptionDesk = "F01_FF_040_Reception_Counter";
        private const float StandbyMetallic = .15f;

        // The right monitor on the desk, in the desk's own space, and the
        // trigger the player aims at, just in front of its screen.
        private static readonly Vector3 MonitorOnDesk = new Vector3(.62f, 1.18f, .365f);
        private static readonly Vector3 AimCentre = new Vector3(0, 0, .072f);
        private static readonly Vector3 AimSize = new Vector3(.46f, .28f, .145f);
        private static readonly Color StandbyColor = new Color(.19f, .075f, .022f);
        private static readonly Color StandbyGlow = new Color(1f, .38f, .055f) * 1.3f;

        /// <summary>Installs the network and its terminal in the hospital scene and saves it.</summary>
        [MenuItem("NET.runner-92/CCTV/Install in Hospital")]
        public static void Install()
        {
            HospitalLatchInstaller.Install();
            if (Object.FindAnyObjectByType<SecurityCctv>())
            {
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            LatchViewmodel latch = Object.FindAnyObjectByType<LatchViewmodel>();
            MeshRenderer desk = Object.FindObjectsByType<MeshRenderer>().Single(r => r.name == ReceptionDesk);
            SecurityCctv network = CctvNetworkBuilder.Install(latch, GameObject.Find(HospitalHierarchy.Systems).transform);
            AddTerminal(network, desk.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"NET92 CCTV | Reused the reception's right monitor and installed {network.Count} virtual views.");
        }

        private static void AddTerminal(SecurityCctv network, Transform desk)
        {
            Transform terminal = SceneParts.Node("Reception right monitor / CCTV terminal", network.transform);
            terminal.SetPositionAndRotation(desk.TransformPoint(MonitorOnDesk), desk.rotation);
            BoxCollider aim = terminal.gameObject.AddComponent<BoxCollider>();
            aim.center = AimCentre;
            aim.size = AimSize;
            aim.isTrigger = true;
            Material standby = SceneParts.SystemMaterial("Reception CCTV standby", StandbyColor, StandbyMetallic, StandbyGlow);
            Transform marker = SceneParts.Block(terminal, "CCTV link status", new Vector3(0, -.011f, -.031f),
                new Vector3(.32f, .018f, .008f), standby, false);
            terminal.gameObject.AddComponent<ReceptionSecurityTerminal>().Configure(network, marker.GetComponent<Renderer>());
        }
    }
}
