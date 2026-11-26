using System;
using System.Collections;
using System.Collections.Generic;
using NetRunner.Player;
using NetRunner.Saves;
using NetRunner.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace NetRunner.Menu
{
    /// <summary>
    /// A hospital run started from the main menu (Phase 11): it restores the
    /// checkpoint the menu chose, writes the first checkpoint of a new game,
    /// offers the pause menu on Escape (resume, save, back to the main
    /// menu), and after a defeat starts again from the slot's last save.
    /// It sits in the hospital scene, set up by the hospital's installer
    /// (<see cref="Configure"/>), and takes its run from the
    /// <see cref="RunSession"/>; without a pending run it switches itself off.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class HospitalRun : MonoBehaviour
    {
        public const string MenuScene = "MainMenu";
        public const string HospitalScene = "Hospital_Exploration";

        // A restored player is lifted 2.5 cm so the capsule does not start inside the floor.
        private const float RestoreLift = .025f;
        private const int PauseSortingOrder = 30;
        private const string DefaultLocation = "Northline · Karanténzóna";

        [SerializeField] private RunSession session;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private StairAccess stairs;
        [SerializeField] private HospitalLift lift;
        [SerializeField] private SecurityCctv network;
        [SerializeField] private LatchRelay relay;
        [SerializeField] private ReceptionSecurityTerminal terminal;
        [SerializeField] private HospitalDoor[] doors = new HospitalDoor[0];
        [SerializeField] private PanelSettings menuPanel;
        [SerializeField] private StyleSheet menuStyle;
        [SerializeField] private TextAsset roomSchedule;

        private CheckpointStore store;
        private readonly MenuWidgets widgets = new MenuWidgets(null);
        private int slot;
        private float previousTimeScale;
        private bool paused;
        private bool returning;
        private LatchViewmodel latch;
        private HospitalInteraction interaction;
        private HospitalRoomSchedule schedule;
        private VisualElement overlay;
        private VisualElement actions;
        private Label status;
        private Button resume;
        private Button save;
        private Button leave;
        private PanelSettings pausePanel;

        /// <summary>The save slot of this run.</summary>
        public int Slot => slot;

        /// <summary>Whether the pause menu is open.</summary>
        public bool Paused => paused;

        private void Start()
        {
            store = session.Store;
            if (!session.TryTake(out slot, out var checkpoint))
            {
                enabled = false;
                return;
            }

            interaction = player.GetComponent<HospitalInteraction>();
            latch = player.GetComponent<LatchViewmodel>();
            schedule = roomSchedule ? JsonUtility.FromJson<HospitalRoomSchedule>(roomSchedule.text) : null;
            player.Defeated += RestartAfterDefeat;
            if (checkpoint != null)
            {
                Restore(checkpoint);
            }

            BuildPause();
            if (checkpoint == null && !WriteCheckpoint())
            {
                status.text = "A kezdőpontot nem sikerült menteni.";
            }
        }

        private void Update()
        {
            if (returning || player.IsDown || Keyboard.current?.escapeKey.wasPressedThisFrame != true)
            {
                return;
            }

            if (paused)
            {
                StartCoroutine(ResumeNextFrame());
                return;
            }

            if (Cursor.lockState == CursorLockMode.Locked && !interaction.TerminalActive && !(latch && latch.Active))
            {
                Pause();
            }
        }

        private void OnDestroy()
        {
            if (paused)
            {
                Time.timeScale = previousTimeScale;
            }

            if (player)
            {
                player.PauseInput(false);
                player.Defeated -= RestartAfterDefeat;
            }

            if (pausePanel)
            {
                Destroy(pausePanel);
            }
        }

        /// <summary>Sets the run up in the hospital scene; called by the hospital's installer.</summary>
        public void Configure(RunSession runs, FirstPersonController runner, StairAccess stairCores, HospitalLift westLift,
            SecurityCctv cameras, LatchRelay serviceRelay, ReceptionSecurityTerminal reception, HospitalDoor[] allDoors,
            PanelSettings panel, StyleSheet style, TextAsset rooms)
        {
            session = runs;
            player = runner;
            stairs = stairCores;
            lift = westLift;
            network = cameras;
            relay = serviceRelay;
            terminal = reception;
            doors = allDoors;
            menuPanel = panel;
            menuStyle = style;
            roomSchedule = rooms;
        }

        private void Restore(HospitalCheckpoint checkpoint)
        {
            stairs.RestoreProgress(checkpoint.HighestFloor);
            if (checkpoint.CctvUnlocked)
            {
                terminal.RestoreUnlock();
            }

            if (checkpoint.RelayLinked)
            {
                relay.RestoreLink();
            }

            var open = new HashSet<string>(checkpoint.OpenDoors, StringComparer.Ordinal);
            foreach (var door in doors)
            {
                if (open.Contains(door.name))
                {
                    door.RestoreOpen();
                }
            }

            player.Warp(checkpoint.Position + Vector3.up * RestoreLift, Quaternion.Euler(0f, checkpoint.Yaw, 0f));
            player.RestorePitch(checkpoint.Pitch);
            Physics.SyncTransforms();
        }

        private bool WriteCheckpoint()
        {
            var openDoors = new List<string>();
            foreach (var door in doors)
            {
                if (door.SaveOpen)
                {
                    openDoors.Add(door.name);
                }
            }

            var position = player.transform.position;
            var room = schedule?.At(position);
            string location = room == null ? DefaultLocation : $"{room.Floor}. szint · {room.Name}";
            return store.Save(slot, new HospitalCheckpoint(DateTime.UtcNow, position, player.transform.eulerAngles.y,
                player.ViewPitch, stairs.Highest, network.Unlocked, relay.Linked, location, openDoors.ToArray()));
        }

        // The player raises Defeated 1.5 s after going down: start again from this slot's last save.
        private void RestartAfterDefeat()
        {
            returning = true;
            session.Begin(slot, store.Read(slot));
            SceneManager.LoadSceneAsync(HospitalScene);
        }

        private void BuildPause()
        {
            var document = gameObject.AddComponent<UIDocument>();
            pausePanel = Instantiate(menuPanel);
            pausePanel.sortingOrder = PauseSortingOrder;
            document.panelSettings = pausePanel;
            var root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.styleSheets.Add(menuStyle);
            root.AddToClassList("net-menu");
            overlay = new VisualElement();
            overlay.AddToClassList("pause-overlay");
            var card = new VisualElement();
            card.AddToClassList("dialog-card");
            card.AddToClassList("pause-card");
            card.Add(MenuWidgets.Label("NORTHLINE  /  MŰVELET FELFÜGGESZTVE", "eyebrow"));
            card.Add(MenuWidgets.Label("Szünet", "dialog-title"));
            status = MenuWidgets.Label("", "dialog-copy");
            card.Add(status);
            actions = new VisualElement();
            card.Add(actions);
            resume = widgets.Button("Vissza a játékba", Resume);
            save = widgets.Button("Játék mentése", SaveFromPause);
            leave = widgets.Button("Vissza a főmenübe", ConfirmLeave);
            resume.name = "pause-resume";
            save.name = "pause-save";
            leave.name = "pause-leave";
            ShowPauseActions();
            overlay.Add(card);
            root.Add(overlay);
            overlay.style.display = DisplayStyle.None;
        }

        private void Pause()
        {
            var options = player.Preferences.Active;
            paused = true;
            player.PauseInput(true);
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            player.CaptureCursor(false);
            overlay.EnableInClassList("contrast", options.UseHighContrast);
            pausePanel.scale = options.UiScale;
            overlay.style.display = DisplayStyle.Flex;
            ShowPauseActions();
            overlay.schedule.Execute(resume.Focus);
        }

        private IEnumerator ResumeNextFrame()
        {
            yield return null;
            Resume();
        }

        private void Resume()
        {
            if (!paused || returning)
            {
                return;
            }

            paused = false;
            player.PauseInput(false);
            overlay.style.display = DisplayStyle.None;
            Time.timeScale = previousTimeScale;
            player.CaptureCursor(true);
        }

        private void ShowPauseActions()
        {
            actions.Clear();
            actions.Add(resume);
            actions.Add(save);
            actions.Add(leave);
            save.SetEnabled(CanSave());
        }

        // Saving needs firm ground outside the lift cabin, so a load never starts mid-fall or mid-ride.
        private bool CanSave() => player.IsGrounded && !(lift && lift.PlayerInside);

        private void SaveFromPause()
        {
            if (!paused || returning)
            {
                return;
            }

            if (!CanSave())
            {
                status.text = "Menteni szilárd talajon, a liftkabinen kívül lehet.";
                return;
            }

            status.text = WriteCheckpoint() ? $"Mentve a(z) {slot + 1}. helyre." : "A mentés sikertelen.";
        }

        private void ConfirmLeave()
        {
            status.text = "Az utolsó mentés óta történt változások elvesznek.";
            actions.Clear();
            actions.Add(widgets.Button("Vissza", () =>
            {
                status.text = "";
                ShowPauseActions();
                leave.Focus();
            }));
            var confirm = widgets.Button("Főmenü megnyitása", ReturnToMenu);
            confirm.name = "pause-confirm";
            actions.Add(confirm);
            confirm.Focus();
        }

        private void ReturnToMenu()
        {
            if (!paused || returning)
            {
                return;
            }

            returning = true;
            Time.timeScale = previousTimeScale;
            SceneManager.LoadSceneAsync(MenuScene);
        }
    }
}
