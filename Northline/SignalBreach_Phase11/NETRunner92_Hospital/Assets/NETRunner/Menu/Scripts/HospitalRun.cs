using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using NetRunner.Player;
using NetRunner.Saves;
using NetRunner.Settings;
using NetRunner.World;

namespace NetRunner.Menu
{
    [DefaultExecutionOrder(-100)]
    public sealed class HospitalRun : MonoBehaviour
    {
        public const string MenuScene = "MainMenu";
        public const string HospitalScene = "Hospital_Exploration";
        static int pendingSlot;
        static HospitalCheckpoint pendingCheckpoint;
        static PanelSettings menuPanel;
        static StyleSheet menuStyle;
        static TextAsset scheduleAsset;

        int slot;
        float previousTimeScale;
        bool paused, returning;
        FirstPersonController player;
        HospitalInteraction interaction;
        LatchViewmodel latch;
        StairAccess stairs;
        HospitalLift lift;
        SecurityCctv network;
        LatchRelay relay;
        HospitalRoomSchedule schedule;
        VisualElement overlay, actions;
        Label status;
        Button resume, save, leave;
        PanelSettings pausePanel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ResetSession()
        {
            pendingSlot = -1;
            pendingCheckpoint = null;
            menuPanel = null;
            menuStyle = null;
            scheduleAsset = null;
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        public static void Enter(int slot, HospitalCheckpoint checkpoint, PanelSettings panel,
            StyleSheet style, TextAsset roomSchedule)
        {
            pendingSlot = slot;
            pendingCheckpoint = checkpoint;
            menuPanel = panel;
            menuStyle = style;
            scheduleAsset = roomSchedule;
        }

        static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != HospitalScene || pendingSlot < 0) return;
            var run = new GameObject("Northline session").AddComponent<HospitalRun>();
            var checkpoint = pendingCheckpoint;
            run.slot = pendingSlot;
            pendingSlot = -1;
            pendingCheckpoint = null;
            run.Initialize(checkpoint);
        }

        // The player raises Defeated 1.5 s after going down: start again from this slot's last checkpoint.
        void RestartAfterDefeat()
        {
            returning = true;
            Enter(slot, CheckpointStore.Read(slot), menuPanel, menuStyle, scheduleAsset);
            SceneManager.LoadSceneAsync(HospitalScene);
        }

        void Initialize(HospitalCheckpoint checkpoint)
        {
            player = FindAnyObjectByType<FirstPersonController>();
            interaction = player.GetComponent<HospitalInteraction>();
            latch = player.GetComponent<LatchViewmodel>();
            player.Defeated += RestartAfterDefeat;
            stairs = FindAnyObjectByType<StairAccess>();
            lift = FindAnyObjectByType<HospitalLift>();
            network = FindAnyObjectByType<SecurityCctv>();
            relay = FindAnyObjectByType<LatchRelay>();
            schedule = scheduleAsset ? JsonUtility.FromJson<HospitalRoomSchedule>(scheduleAsset.text) : null;
            if (checkpoint != null) Restore(checkpoint);
            BuildPause();
            if (checkpoint == null && !WriteCheckpoint())
                status.text = "A kezdőpontot nem sikerült menteni.";
        }

        void Restore(HospitalCheckpoint checkpoint)
        {
            if (stairs) stairs.RestoreProgress(checkpoint.highestFloor);
            if (checkpoint.cctv)
                FindAnyObjectByType<ReceptionSecurityTerminal>()?.RestoreUnlock();
            if (checkpoint.relay && relay) relay.RestoreLink();
            var open = new HashSet<string>(checkpoint.openDoors, StringComparer.Ordinal);
            foreach (var door in FindObjectsByType<HospitalDoor>())
                if (open.Contains(door.name)) door.RestoreOpen();

            player.Warp(checkpoint.position + Vector3.up * .025f, Quaternion.Euler(0, checkpoint.yaw, 0));
            player.RestorePitch(checkpoint.pitch);
            Physics.SyncTransforms();
        }

        bool WriteCheckpoint()
        {
            var openDoors = new List<string>();
            foreach (var door in FindObjectsByType<HospitalDoor>())
                if (door.SaveOpen) openDoors.Add(door.name);
            var position = player.transform.position;
            var room = schedule?.At(position);
            return CheckpointStore.Save(slot, new HospitalCheckpoint
            {
                time = DateTime.UtcNow.Ticks,
                position = position,
                yaw = player.transform.eulerAngles.y,
                pitch = player.ViewPitch,
                highestFloor = stairs ? stairs.Highest : 0,
                cctv = network && network.Unlocked,
                relay = relay && relay.Linked,
                location = room == null ? "Northline · Karanténzóna" : $"{room.floor}. szint · {room.name}",
                openDoors = openDoors.ToArray()
            });
        }

        void BuildPause()
        {
            var ui = gameObject.AddComponent<UIDocument>();
            pausePanel = Instantiate(menuPanel);
            pausePanel.sortingOrder = 30;
            ui.panelSettings = pausePanel;
            var root = ui.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.styleSheets.Add(menuStyle);
            root.AddToClassList("net-menu");
            overlay = new VisualElement();
            overlay.AddToClassList("pause-overlay");
            var card = new VisualElement();
            card.AddToClassList("dialog-card");
            card.AddToClassList("pause-card");
            var mark = new Label("NORTHLINE  /  MŰVELET FELFÜGGESZTVE");
            mark.AddToClassList("eyebrow");
            card.Add(mark);
            var heading = new Label("Szünet");
            heading.AddToClassList("dialog-title");
            card.Add(heading);
            status = new Label();
            status.AddToClassList("dialog-copy");
            card.Add(status);
            actions = new VisualElement();
            card.Add(actions);
            resume = MenuControls.Button("Vissza a játékba", Resume);
            save = MenuControls.Button("Játék mentése", SaveFromPause);
            leave = MenuControls.Button("Vissza a főmenübe", ConfirmLeave);
            resume.name = "pause-resume";
            save.name = "pause-save";
            leave.name = "pause-leave";
            ShowPauseActions();
            overlay.Add(card);
            root.Add(overlay);
            overlay.style.display = DisplayStyle.None;
        }

        void Update()
        {
            if (returning || player.IsDown || Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            if (paused)
            {
                StartCoroutine(ResumeNextFrame());
                return;
            }
            if (Cursor.lockState == CursorLockMode.Locked && !interaction.TerminalActive && !(latch && latch.Active))
            {
                paused = true;
                player.PauseInput(true);
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0;
                player.CaptureCursor(false);
                overlay.EnableInClassList("contrast", GameOptions.Active.highContrast);
                pausePanel.scale = GameOptions.Active.uiScale;
                overlay.style.display = DisplayStyle.Flex;
                ShowPauseActions();
                overlay.schedule.Execute(resume.Focus);
            }
        }

        IEnumerator ResumeNextFrame()
        {
            yield return null;
            Resume();
        }

        void Resume()
        {
            if (!paused || returning) return;
            paused = false;
            player.PauseInput(false);
            overlay.style.display = DisplayStyle.None;
            Time.timeScale = previousTimeScale;
            player.CaptureCursor(true);
        }

        void ShowPauseActions()
        {
            actions.Clear();
            actions.Add(resume);
            actions.Add(save);
            actions.Add(leave);
            save.SetEnabled(player.IsGrounded && !(lift && lift.PlayerInside));
        }

        void SaveFromPause()
        {
            if (!paused || returning) return;
            if (!player.IsGrounded || lift && lift.PlayerInside)
            {
                status.text = "Menteni szilárd talajon, a liftkabinen kívül lehet.";
                return;
            }
            status.text = WriteCheckpoint() ? $"Mentve a(z) {slot + 1}. helyre." : "A mentés sikertelen.";
        }

        void ConfirmLeave()
        {
            status.text = "Az utolsó mentés óta történt változások elvesznek.";
            actions.Clear();
            actions.Add(MenuControls.Button("Vissza", () =>
            {
                status.text = "";
                ShowPauseActions();
                leave.Focus();
            }));
            var confirm = MenuControls.Button("Főmenü megnyitása", ReturnToMenu);
            confirm.name = "pause-confirm";
            actions.Add(confirm);
            confirm.Focus();
        }

        void ReturnToMenu()
        {
            if (!paused || returning) return;
            returning = true;
            Time.timeScale = previousTimeScale;
            SceneManager.LoadSceneAsync(MenuScene);
        }

        void OnDestroy()
        {
            if (paused) Time.timeScale = previousTimeScale;
            if (player)
            {
                player.PauseInput(false);
                player.Defeated -= RestartAfterDefeat;
            }
            if (pausePanel) Destroy(pausePanel);
        }
    }
}
