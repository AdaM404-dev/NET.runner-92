using System;
using System.IO;
using System.Linq;
using NetRunner.Atmosphere;
using NetRunner.Enemy;
using NetRunner.Menu;
using NetRunner.Player;
using NetRunner.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Sets the references the Phase 11 features need at runtime, so no
    /// component searches the scene (#55): it creates the preferences and
    /// run-session assets, gives the player the preferences and the robots
    /// it can breach, gives each robot the player, gives the pulsing and
    /// flickering lights the preferences, places the <see cref="HospitalRun"/>
    /// in the hospital scene, and gives the main menu the preferences and the
    /// session. Saves both scenes. Run it again after rebuilding a scene.
    /// </summary>
    public static partial class HospitalSceneBuilder
    {
        public const string PreferencesPath = "Assets/NETRunner/Settings/Resources/Preferences.asset";
        public const string RunSessionPath = "Assets/NETRunner/Menu/Data/RunSession.asset";
        private const string SessionObject = "Northline session";

        [MenuItem("NET.runner-92/Phase 11/Install runtime references")]
        public static void InstallPhase11References()
        {
            var preferences = CreateOrLoad<Preferences>(PreferencesPath);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Atmos + "/Audio/Northline_Ambience.mixer");
            if (!mixer)
            {
                throw new InvalidOperationException("The hospital's audio mixer is missing.");
            }

            SetField(preferences, "mixer", mixer);
            var session = CreateOrLoad<RunSession>(RunSessionPath);
            AssetDatabase.SaveAssets();
            WireHospital(preferences, session);
            WireMenu(preferences, session);
            Debug.Log("NET92 PHASE 11 | Runtime references installed in the hospital and the main menu.");
        }

        private static void WireHospital(Preferences preferences, RunSession session)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var player = Single<FirstPersonController>();
            player.SetPreferences(preferences);
            EditorUtility.SetDirty(player);
            var robots = UnityEngine.Object.FindObjectsByType<K7Patrol>(FindObjectsInactive.Include);
            foreach (var robot in robots)
            {
                robot.SetTarget(player);
                EditorUtility.SetDirty(robot);
            }

            var interaction = player.GetComponent<HospitalInteraction>();
            interaction.SetBreachables(robots.OrderBy(robot => robot.name).Cast<MonoBehaviour>().ToArray());
            EditorUtility.SetDirty(interaction);
            foreach (var light in UnityEngine.Object.FindObjectsByType<PracticalPulse>(FindObjectsInactive.Include))
            {
                light.SetPreferences(preferences);
                EditorUtility.SetDirty(light);
            }

            foreach (var power in UnityEngine.Object.FindObjectsByType<ResidualPower>(FindObjectsInactive.Include))
            {
                power.SetPreferences(preferences);
                EditorUtility.SetDirty(power);
            }

            var run = UnityEngine.Object.FindAnyObjectByType<HospitalRun>(FindObjectsInactive.Include);
            if (!run)
            {
                run = new GameObject(SessionObject).AddComponent<HospitalRun>();
            }

            run.Configure(session, player, Single<StairAccess>(), Single<HospitalLift>(), Single<SecurityCctv>(),
                Single<LatchRelay>(), Single<ReceptionSecurityTerminal>(),
                UnityEngine.Object.FindObjectsByType<HospitalDoor>(FindObjectsInactive.Include).OrderBy(door => door.name).ToArray(),
                Load<PanelSettings>(MenuAssets + "/MainMenu_Panel.asset"), Load<StyleSheet>(MenuAssets + "/MainMenu.uss"),
                Load<TextAsset>(Root + "/Environment/Hospital/RoomSchedule.json"));
            EditorUtility.SetDirty(run);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WireMenu(Preferences preferences, RunSession session)
        {
            if (!File.Exists(MainMenuPath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(MainMenuPath);
            var view = Single<MainMenuView>();
            view.SetShared(preferences, session);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset)
            {
                return asset;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset ? asset : throw new FileNotFoundException(path);
        }

        private static T Single<T>() where T : UnityEngine.Object
        {
            var found = UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            return found ? found : throw new InvalidOperationException($"No {typeof(T).Name} in the open scene.");
        }

        // Sets a private serialized field the way the Inspector does, so the scene or asset saves it.
        private static void SetField(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field) ?? throw new MissingFieldException(target.GetType().Name, field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
