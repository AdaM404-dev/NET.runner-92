using System;
using System.IO;
using System.Linq;
using NetRunner.Atmosphere;
using NetRunner.Enemy;
using NetRunner.Menu;
using NetRunner.Player;
using NetRunner.SceneBuilding.Editor;
using NetRunner.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
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
    public static class RuntimeReferencesInstaller
    {
        private const string SessionObject = "Northline session";
        private const string MixerPath = HospitalAssets.Atmosphere + "/Audio/Northline_Ambience.mixer";
        private const string MixerField = "mixer";
        private const string MenuPanelPath = HospitalAssets.Menu + "/MainMenu_Panel.asset";
        private const string MenuStylesPath = HospitalAssets.Menu + "/MainMenu.uss";
        private const string SchedulePath = HospitalAssets.Environment + "/Hospital/RoomSchedule.json";

        /// <summary>Wires the hospital and the main menu to the shared assets and saves both scenes.</summary>
        [MenuItem("NET.runner-92/Phase 11/Install runtime references")]
        public static void Install()
        {
            Preferences preferences = SceneParts.CreateOrLoad<Preferences>(HospitalAssets.PreferencesPath);
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (!mixer)
            {
                throw new InvalidOperationException("The hospital's audio mixer is missing.");
            }

            SetField(preferences, MixerField, mixer);
            RunSession session = SceneParts.CreateOrLoad<RunSession>(HospitalAssets.RunSessionPath);
            AssetDatabase.SaveAssets();
            WireHospital(preferences, session);
            WireMenu(preferences, session);
            Debug.Log("NET92 PHASE 11 | Runtime references installed in the hospital and the main menu.");
        }

        private static void WireHospital(Preferences preferences, RunSession session)
        {
            Scene scene = EditorSceneManager.OpenScene(HospitalAssets.ScenePath);
            FirstPersonController player = Single<FirstPersonController>();
            player.SetPreferences(preferences);
            EditorUtility.SetDirty(player);
            K7Patrol[] robots = UnityEngine.Object.FindObjectsByType<K7Patrol>(FindObjectsInactive.Include);
            foreach (K7Patrol robot in robots)
            {
                robot.SetTarget(player);
                EditorUtility.SetDirty(robot);
            }

            HospitalInteraction interaction = player.GetComponent<HospitalInteraction>();
            interaction.SetBreachables(robots.OrderBy(robot => robot.name).Cast<MonoBehaviour>().ToArray());
            EditorUtility.SetDirty(interaction);
            foreach (PracticalPulse light in UnityEngine.Object.FindObjectsByType<PracticalPulse>(FindObjectsInactive.Include))
            {
                light.SetPreferences(preferences);
                EditorUtility.SetDirty(light);
            }

            foreach (ResidualPower power in UnityEngine.Object.FindObjectsByType<ResidualPower>(FindObjectsInactive.Include))
            {
                power.SetPreferences(preferences);
                EditorUtility.SetDirty(power);
            }

            HospitalRun run = UnityEngine.Object.FindAnyObjectByType<HospitalRun>(FindObjectsInactive.Include);
            if (!run)
            {
                run = new GameObject(SessionObject).AddComponent<HospitalRun>();
            }

            run.Configure(session, player, Single<StairAccess>(), Single<HospitalLift>(), Single<SecurityCctv>(),
                Single<LatchRelay>(), Single<ReceptionSecurityTerminal>(),
                UnityEngine.Object.FindObjectsByType<HospitalDoor>(FindObjectsInactive.Include).OrderBy(door => door.name).ToArray(),
                SceneParts.Load<PanelSettings>(MenuPanelPath), SceneParts.Load<StyleSheet>(MenuStylesPath),
                SceneParts.Load<TextAsset>(SchedulePath));
            EditorUtility.SetDirty(run);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WireMenu(Preferences preferences, RunSession session)
        {
            if (!File.Exists(HospitalAssets.MainMenuPath))
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(HospitalAssets.MainMenuPath);
            MainMenuView view = Single<MainMenuView>();
            view.SetShared(preferences, session);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T Single<T>() where T : UnityEngine.Object
        {
            T found = UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            return found ? found : throw new InvalidOperationException($"No {typeof(T).Name} in the open scene.");
        }

        // Sets a private serialized field the way the Inspector does, so the scene or asset saves it.
        private static void SetField(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field) ?? throw new MissingFieldException(target.GetType().Name, field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
