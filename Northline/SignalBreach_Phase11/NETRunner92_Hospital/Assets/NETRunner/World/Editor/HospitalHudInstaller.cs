using NetRunner.Hud.Editor;
using NetRunner.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Installs the HUD in the hospital: the CCTV first (which installs
    /// LATCH-07 and the systems before it), then the HUD on the player
    /// (<see cref="HudBuilder"/>), and saves the scene.
    /// </summary>
    public static class HospitalHudInstaller
    {
        /// <summary>Installs or updates the HUD in the hospital scene and saves it.</summary>
        [MenuItem("NET.runner-92/HUD/Install in Hospital")]
        public static void Install()
        {
            HospitalCctvInstaller.Install();
            Scene scene = EditorSceneManager.GetActiveScene();
            HudBuilder.Install(Object.FindAnyObjectByType<FirstPersonController>(), Object.FindAnyObjectByType<SecurityCctv>(),
                Object.FindAnyObjectByType<HospitalLift>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("NET92 HUD | Installed the hospital overlay and linked the existing interaction systems.");
        }
    }
}
