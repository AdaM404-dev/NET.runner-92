using NetRunner.Core;
using NetRunner.Player;
using NetRunner.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.World
{
    /// <summary>
    /// The reception desk's security monitor. E opens a full-screen puzzle
    /// that takes the keyboard (an <see cref="ITerminal"/>); matching the
    /// local route to the intercepted signature (Core's
    /// <see cref="SignalPuzzle"/>) unlocks the CCTV for this session and
    /// lights the link indicator. The hospital HUD draws the puzzle.
    /// </summary>
    public sealed class ReceptionSecurityTerminal : MonoBehaviour, ITerminal
    {
        private static readonly int Emission = Shader.PropertyToID("_EmissiveColor");
        private static readonly Color LinkedGlow = new Color(.12f, 1f, .82f) * 2.2f;
        [SerializeField] private SecurityCctv cctv;
        [SerializeField] private Renderer linkIndicator;

        private readonly SignalPuzzle puzzle = new SignalPuzzle();
        private HospitalInteraction operatorInput;

        /// <summary>Whether the puzzle is open and has the keyboard.</summary>
        public bool IsOpen => operatorInput;
        public (GameAction? Action, string Text) Prompt => cctv.Unlocked ? (GameAction.Cctv, "Élő kamerák")
            : (GameAction.Interact, "Recepciós terminál");
        public static int CellCount => SignalPuzzle.CellCount;
        public static int ReferenceCell(int index) => SignalPuzzle.ReferenceCell(index);
        public int RouteCell(int index) => puzzle.RouteAt(index);
        public int SelectedCell => puzzle.Cursor;
        public bool Mismatch => puzzle.Mismatch;

        private void OnDisable()
        {
            Close();
        }

        /// <summary>Sets the CCTV network it unlocks and its link indicator; the scene builder calls it.</summary>
        public void Configure(SecurityCctv network, Renderer indicator)
        {
            cctv = network;
            linkIndicator = indicator;
        }

        /// <inheritdoc/>
        public void Interact(FirstPersonController player)
        {
            if (cctv.Unlocked || IsOpen)
            {
                return;
            }

            operatorInput = player.GetComponent<HospitalInteraction>();
            if (!operatorInput)
            {
                return;
            }

            operatorInput.BeginTerminal(this);
            puzzle.Reset();
        }

        /// <summary>Selects the next (+1) or previous (−1) cell while open.</summary>
        public void Select(int step)
        {
            if (IsOpen)
            {
                puzzle.Select(step);
            }
        }

        /// <summary>Changes the selected cell up (+1) or down (−1) while open.</summary>
        public void Adjust(int step)
        {
            if (IsOpen)
            {
                puzzle.Adjust(step);
            }
        }

        /// <summary>Checks the route; when it matches, unlocks the CCTV, lights the indicator and closes.</summary>
        public bool Submit()
        {
            if (!IsOpen || !puzzle.Submit())
            {
                return false;
            }
            RestoreUnlock();
            Close();
            return true;
        }

        public void RestoreUnlock()
        {
            cctv.Unlock();
            if (linkIndicator)
            {
                var block = new MaterialPropertyBlock();
                linkIndicator.GetPropertyBlock(block);
                block.SetColor(Emission, LinkedGlow);
                linkIndicator.SetPropertyBlock(block);
            }
        }

        /// <inheritdoc/>
        public void Close()
        {
            if (operatorInput)
            {
                operatorInput.EndTerminal(this);
            }

            operatorInput = null;
        }

        public void ReadInput(Keyboard keyboard, Preferences bindings)
        {
            if (keyboard == null) return;
            if (keyboard.leftArrowKey.wasPressedThisFrame) Select(-1);
            else if (keyboard.rightArrowKey.wasPressedThisFrame) Select(1);
            else if (keyboard.upArrowKey.wasPressedThisFrame) Adjust(1);
            else if (keyboard.downArrowKey.wasPressedThisFrame) Adjust(-1);
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Submit();
            else if (bindings.Pressed(keyboard, GameAction.Interact) || keyboard.escapeKey.wasPressedThisFrame) Close();
        }

    }
}
