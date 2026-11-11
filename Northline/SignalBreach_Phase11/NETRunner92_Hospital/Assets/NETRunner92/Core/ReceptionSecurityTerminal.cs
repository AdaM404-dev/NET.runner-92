using UnityEngine;
using UnityEngine.InputSystem;

namespace NETRunner92
{
    public sealed class ReceptionSecurityTerminal : HospitalInteractable
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
        public override (string Key, string Text) Prompt => cctv.Unlocked ? (HospitalKeys.Label(GameAction.Cctv), "Élő kamerák")
            : (HospitalKeys.Label(GameAction.Interact), "Recepciós terminál");
        internal static int CellCount => Signature.Length;
        internal static int ReferenceCell(int index) => Signature[index];
        internal int RouteCell(int index) => route[index];
        internal int SelectedCell => cursor;
        internal bool Mismatch => mismatch;

        public void Configure(SecurityCctv network, Renderer indicator)
        {
            cctv = network;
            linkIndicator = indicator;
        }

        public override void Use(FirstPersonController player)
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

        internal void RestoreUnlock()
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

        public void ReadInput(Keyboard keyboard)
        {
            if (keyboard == null) return;
            if (keyboard.leftArrowKey.wasPressedThisFrame) Select(-1);
            else if (keyboard.rightArrowKey.wasPressedThisFrame) Select(1);
            else if (keyboard.upArrowKey.wasPressedThisFrame) Adjust(1);
            else if (keyboard.downArrowKey.wasPressedThisFrame) Adjust(-1);
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Submit();
            else if (HospitalKeys.Pressed(keyboard, GameAction.Interact) || keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        void OnDisable() => Close();
    }
}
