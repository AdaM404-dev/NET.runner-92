using System.Collections;
using UnityEngine;

namespace NetRunner.MainMenu
{
    /// <summary>
    /// The menu's state machine: boot, observation, modal panels, loading and disconnect.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public enum MenuState
        {
            Booting,
            Observation,
            Modal,
            Loading,
            Disconnected
        }

        [SerializeField] internal MenuView view;
        [SerializeField] internal MenuBootSequence boot;
        [SerializeField] internal LoadingScreenController loading;
        [SerializeField] internal SurveillanceCameraController surveillance;
        [SerializeField] internal FacilityEventController facilityEvents;
        [SerializeField] internal MenuAudioController audioController;

        private string lastAction;
        private CursorLockMode previousLock;
        private bool previousCursorVisible;

        public MenuState State { get; private set; } = MenuState.Booting;

        private IEnumerator Start()
        {
            previousLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            view.Build();
            view.ActionSelected += HandleAction;
            view.VolumeChanged += audioController.SetVolume;
            view.FeedBrightnessChanged += view.SetFeedBrightness;
            view.CameraCycleChanged += SetCameraCycle;
            audioController.Begin();
            surveillance.Suspended = true;
            facilityEvents.Suspended = true;
            surveillance.Begin();
            facilityEvents.Begin();
            yield return boot.Run(view, surveillance);
            ResumeObservation();
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.ActionSelected -= HandleAction;
                view.VolumeChanged -= audioController.SetVolume;
                view.FeedBrightnessChanged -= view.SetFeedBrightness;
                view.CameraCycleChanged -= SetCameraCycle;
            }

            Cursor.lockState = previousLock;
            Cursor.visible = previousCursorVisible;
        }

        public void HandleAction(string action)
        {
            if (State == MenuState.Booting)
            {
                if (action == "skip-boot" || action == "back")
                {
                    boot.Skip();
                }

                return;
            }

            if (State == MenuState.Loading || State == MenuState.Disconnected)
            {
                return;
            }

            audioController.Click();
            if (State == MenuState.Modal)
            {
                if (action == "back" || action == "close-modal")
                {
                    view.CloseModal();
                    State = MenuState.Observation;
                    view.Focus(lastAction);
                }
                else if (action == "confirm-disconnect")
                {
                    StartCoroutine(Disconnect());
                }

                return;
            }

            if (action == "continue" || action == "new-session")
            {
                State = MenuState.Loading;
                StartCoroutine(LoadPrototype(action));
            }
            else if (action == "load-session" || action == "configuration" || action == "disconnect")
            {
                lastAction = action;
                State = MenuState.Modal;
                view.ShowModal(action);
            }
        }

        private void SetCameraCycle(bool enabled) => surveillance.AutomaticCycling = enabled;

        private void ResumeObservation()
        {
            State = MenuState.Observation;
            surveillance.Suspended = false;
            facilityEvents.Suspended = false;
            view.ShowMenu();
        }

        private IEnumerator LoadPrototype(string entry)
        {
            surveillance.Suspended = true;
            facilityEvents.Suspended = true;
            // No SceneManager dependency: the handoff is deliberately simulated.
            yield return loading.Run(view, surveillance, entry == "continue" ? "RESUME CHANNEL / NODE_92" : "NEW CREDENTIAL / NODE_92");
            ResumeObservation();
        }

        private IEnumerator Disconnect()
        {
            State = MenuState.Disconnected;
            surveillance.Suspended = true;
            facilityEvents.Suspended = true;
            view.CloseModal();
            MenuView.SetVisible(view.Terminal, true);
            view.Terminal.style.opacity = 1;
            view.BootText.text = "REMOTE CHANNEL RELEASED\nCONNECTION CLOSED";
            yield return new WaitForSecondsRealtime(1.2f);
            if (Application.isEditor)
            {
                // Stay in Play mode and return to the terminal; never stop the editor.
                ResumeObservation();
            }
            else
            {
                Application.Quit();
            }
        }
    }
}
