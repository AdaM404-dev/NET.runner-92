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
    /// Installs LATCH-07 in the hospital: the hospital's systems first, then
    /// the tablet on the player and the service relay (<see cref="Latch07Installer"/>),
    /// and saves the scene. Does nothing when the player already has the tablet.
    /// </summary>
    public static class HospitalLatchInstaller
    {
        /// <summary>Installs the tablet and the relay in the hospital scene and saves it.</summary>
        [MenuItem("NET.runner-92/LATCH-07/Install in Hospital")]
        public static void Install()
        {
            HospitalSystemsBuilder.Install();
            Scene scene = EditorSceneManager.GetActiveScene();
            FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
            if (player.GetComponent<LatchViewmodel>())
            {
                return;
            }

            Latch07Installer.Install(player, GameObject.Find(HospitalHierarchy.Systems).transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("LATCH07 | Installed viewmodel and optional south forecourt relay.");
        }
    }
}
