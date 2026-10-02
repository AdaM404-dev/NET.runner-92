#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace NetRunner.MenuPrototype.Tests
{
    public sealed class MenuPrototypeTests
    {
        private MainMenuController menu;
        private MenuPrototypeSettings runtimeSettings;
        private const string ScenePath = "Assets/NETRunner/MainMenu/Scenes/NETRunner_MainMenu_Prototype.unity";

        [UnitySetUp]
        public IEnumerator OpenPrototype()
        {
            Application.logMessageReceived += AcceptUnrelatedEditorAccountWarning;
            yield return EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            menu = Object.FindAnyObjectByType<MainMenuController>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.State, Is.EqualTo(MainMenuController.MenuState.Booting));
            Assert.That(menu.view.Menu.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(menu.surveillance.feeds.Length, Is.EqualTo(4));
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(ScenePath));
            // Clone only in Play mode; tests never mutate the configuration asset.
            runtimeSettings = Object.Instantiate(menu.loading.settings);
            runtimeSettings.loadingDuration = 1.3f;
            runtimeSettings.terminalHoldDuration = 0.25f;
            menu.loading.settings = runtimeSettings;
        }

        [UnityTest]
        public IEnumerator BootPanelsSettingsEventsAndBothLoadingEntriesStayInPrototype()
        {
            yield return WaitForState(MainMenuController.MenuState.Observation, 8);
            Assert.That(menu.view.Title.text, Is.EqualTo("NET.RUNNER"));
            Assert.That(menu.view.BootText.text, Does.Contain("WAREHOUSE NETWORK FOUND"));
            Assert.That(menu.view.Root.focusController.focusedElement, Is.EqualTo(menu.view.Root.Q<Button>("continue")));

            Submit("load-session");
            yield return null;
            Assert.That(menu.State, Is.EqualTo(MainMenuController.MenuState.Modal));
            Assert.That(menu.view.Modal.Query<Label>().ToList().Exists(label => label.text.Contains("NO LOCAL RECORDS FOUND")), Is.True);
            Submit("close-modal");
            yield return null;
            Assert.That(menu.State, Is.EqualTo(MainMenuController.MenuState.Observation));
            Assert.That(menu.view.Root.focusController.focusedElement, Is.EqualTo(menu.view.Root.Q<Button>("load-session")));

            Submit("configuration");
            yield return null;
            menu.view.Root.Q<Slider>("ambience-volume").value = 0.7f;
            menu.view.Root.Q<Slider>("feed-attenuation").value = 0.3f;
            menu.view.Root.Q<Toggle>("camera-cycling").value = false;
            yield return null;
            Assert.That(menu.audioController.masterVolume, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(menu.audioController.electricalHum.volume, Is.EqualTo(0.154f).Within(0.001f));
            Assert.That(menu.view.FeedShade.resolvedStyle.backgroundColor.a, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(menu.surveillance.AutomaticCycling, Is.False);
            menu.view.Root.Q<Toggle>("camera-cycling").value = true;
            using (var escape = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None))
            {
                menu.view.Root.SendEvent(escape);
            }
            yield return null;
            Assert.That(menu.State, Is.EqualTo(MainMenuController.MenuState.Observation));

            foreach (string entry in new[] { "continue", "new-session" })
            {
                int completed = menu.loading.CompletedSequences;
                Submit(entry);
                yield return null;
                Assert.That(menu.State, Is.EqualTo(MainMenuController.MenuState.Loading));
                menu.HandleAction(entry); // Double clicks are ignored while transferring.
                yield return WaitForState(MainMenuController.MenuState.Observation, 7);
                Assert.That(menu.loading.CompletedSequences, Is.EqualTo(completed + 1));
                Assert.That(menu.loading.Progress, Is.EqualTo(1));
                Assert.That(menu.view.ProgressText.text, Is.EqualTo("NETWORK ACCESS: 100%"));
                Assert.That(menu.view.LoadingLog.text, Does.Contain("synchronizing local systems"));
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(ScenePath));
            }

            string one = menu.loading.logs.ChooseArchive();
            Assert.That(menu.loading.logs.ChooseArchive(), Is.Not.EqualTo(one));
            menu.view.SetFacilityStatus("UNKNOWN", "CONNECTED", "19 / 18", "+417%");
            Assert.That(menu.view.Root.Query<Label>().ToList().Exists(label => label.text == "19 / 18"), Is.True);

            var relay = (RelayLightEvent)menu.facilityEvents.events[0];
            float initialIntensity = relay.target.intensity;
            var initialMaterial = relay.indicator.sharedMaterial;
            var shutter = (ShutterEvent)menu.facilityEvents.events[2];
            Vector3 initialPosition = shutter.shutter.localPosition;
            for (int i = 0; i < menu.facilityEvents.events.Length; i++)
            {
                int completed = menu.facilityEvents.CompletedEvents;
                // The scheduler stays live; wait for any current rare event to finish.
                while (menu.facilityEvents.IsRunning)
                {
                    yield return null;
                }
                yield return menu.facilityEvents.RunEvent(i);
                Assert.That(menu.facilityEvents.CompletedEvents, Is.GreaterThan(completed));
            }
            Assert.That(relay.target.intensity, Is.EqualTo(initialIntensity));
            Assert.That(relay.indicator.sharedMaterial, Is.SameAs(initialMaterial));
            Assert.That(Vector3.Distance(shutter.shutter.localPosition, initialPosition), Is.LessThan(0.001f));
            Assert.That(menu.audioController.machinery.isPlaying, Is.False);
            // Natural 18–27 s timer has elapsed during the interaction/event sequence.
            Assert.That(menu.surveillance.SwitchCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, Is.EqualTo(4));
            int enabledCameras = 0;
            foreach (var feed in menu.surveillance.feeds)
            {
                if (feed.camera.enabled)
                {
                    enabledCameras++;
                }
            }
            Assert.That(enabledCameras, Is.EqualTo(1));

            Submit("disconnect");
            yield return null;
            Submit("confirm-disconnect");
            yield return null;
            Assert.That(menu.State, Is.EqualTo(MainMenuController.MenuState.Disconnected));
            yield return WaitForState(MainMenuController.MenuState.Observation, 3);
            Assert.That(Application.isPlaying, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BootCanBeSkippedThroughFocusedButton()
        {
            Submit("skip-boot");
            yield return WaitForState(MainMenuController.MenuState.Observation, 2);
            Assert.That(menu.view.Title.text, Is.EqualTo("NET.RUNNER"));
            Assert.That(menu.view.Terminal.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            LogAssert.NoUnexpectedReceived();
        }

        private void Submit(string name)
        {
            var button = menu.view.Root.Q<Button>(name);
            Assert.That(button, Is.Not.Null, name);
            button.Focus();
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                button.SendEvent(submit);
            }
        }

        private IEnumerator WaitForState(MainMenuController.MenuState expected, float timeout)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (menu.State != expected && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(menu.State, Is.EqualTo(expected));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator CleanUpWithoutSavingScenes()
        {
            var prototype = SceneManager.GetSceneByPath(ScenePath);
            var cleanup = SceneManager.CreateScene("Menu test cleanup");
            SceneManager.SetActiveScene(cleanup);
            if (prototype.IsValid() && prototype.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(prototype);
            }
            Object.Destroy(runtimeSettings);
            yield return null;
            LogAssert.NoUnexpectedReceived();
            Application.logMessageReceived -= AcceptUnrelatedEditorAccountWarning;
        }

        private static void AcceptUnrelatedEditorAccountWarning(string message, string trace, LogType type)
        {
            // The existing editor AI package retries its cloud account during tests.
            // Consume ONLY its known warning; gameplay/runtime errors still fail.
            const string known = "Account API did not become accessible within 30 seconds. This may be due to network issues or editor focus.";
            if (type == LogType.Warning && message == known)
            {
                LogAssert.Expect(LogType.Warning, known);
            }
        }
    }
}
#endif
