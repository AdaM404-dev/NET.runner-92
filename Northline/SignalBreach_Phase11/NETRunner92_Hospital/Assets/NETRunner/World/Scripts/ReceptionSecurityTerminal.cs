using NetRunner.Core;
using NetRunner.Player;
using NetRunner.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.World
{
    public sealed class ReceptionSecurityTerminal : MonoBehaviour, ITerminal
    {
        static readonly int Emission = Shader.PropertyToID("_EmissiveColor");
        static readonly int[] Signature = { 3, 1, 4 };
        [SerializeField] SecurityCctv cctv;
        [SerializeField] Renderer linkIndicator;
        readonly int[] route = new int[Signature.Length];
        HospitalInteraction operatorInput;
        int cursor;
        bool mismatch;

        public bool IsOpen => operatorInput;
        public (GameAction? Action, string Text) Prompt => cctv.Unlocked ? (GameAction.Cctv, "Élő kamerák")
            : (GameAction.Interact, "Recepciós terminál");
        public static int CellCount => Signature.Length;
        public static int ReferenceCell(int index) => Signature[index];
        public int RouteCell(int index) => route[index];
        public int SelectedCell => cursor;
        public bool Mismatch => mismatch;

        public void Configure(SecurityCctv network, Renderer indicator)
        {
            cctv = network;
            linkIndicator = indicator;
        }

        public void Interact(FirstPersonController player)
        {
            if (cctv.Unlocked || IsOpen) return;
            operatorInput = player.GetComponent<HospitalInteraction>();
            if (!operatorInput) return;
            operatorInput.BeginTerminal(this);
            for (int i = 0; i < route.Length; i++) route[i] = 1;
            cursor = 0;
            mismatch = false;
        }

        public void Select(int step)
        {
            if (IsOpen) cursor = (cursor + step + route.Length) % route.Length;
        }

        public void Adjust(int step)
        {
            if (!IsOpen) return;
            route[cursor] = (route[cursor] - 1 + step + 4) % 4 + 1;
            mismatch = false;
        }

        public bool Submit()
        {
            if (!IsOpen) return false;
            for (int i = 0; i < route.Length; i++)
            {
                if (route[i] == Signature[i]) continue;
                mismatch = true;
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
                block.SetColor(Emission, new Color(.12f, 1f, .82f) * 2.2f);
                linkIndicator.SetPropertyBlock(block);
            }
        }

        public void Close()
        {
            if (operatorInput) operatorInput.EndTerminal(this);
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

        void OnDisable() => Close();
    }
}
