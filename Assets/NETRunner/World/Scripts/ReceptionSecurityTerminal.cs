using NetRunner.Player;
using UnityEngine.InputSystem;
using UnityEngine;

namespace NetRunner.World
{
    public sealed class ReceptionSecurityTerminal : MonoBehaviour, ITerminal
    {
        static readonly int Emission = Shader.PropertyToID("_EmissiveColor");
        static readonly int[] Signature = { 3, 1, 4 };
        static readonly Color Ink = new(.047f, .095f, .108f);
        static readonly Color Plate = new(.075f, .154f, .168f);
        [SerializeField] SecurityCctv cctv;
        [SerializeField] Renderer linkIndicator;
        readonly int[] route = new int[Signature.Length];
        HospitalInteraction operatorInput;
        GUIStyle heading, copy, digit;
        int cursor;
        bool mismatch;

        public bool IsOpen => operatorInput;
        public string Prompt => cctv.Unlocked ? "CCTV unlocked   /   1  Live cameras" : "E  Breach reception CCTV terminal";

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
            cctv.Unlock();
            if (linkIndicator)
            {
                var block = new MaterialPropertyBlock();
                linkIndicator.GetPropertyBlock(block);
                block.SetColor(Emission, new Color(.12f, 1f, .82f) * 2.2f);
                linkIndicator.SetPropertyBlock(block);
            }
            Close();
            return true;
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
            else if (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        void OnDisable() => Close();

        void OnGUI()
        {
            if (!IsOpen) return;
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
                copy = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                digit = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }
            const float width = 606f, height = 340f;
            float scale = Mathf.Clamp(Screen.width / 1300f, .65f, 1.35f);
            scale = Mathf.Min(scale, Screen.height * .87f / height);
            var previous = GUI.matrix;
            var color = GUI.color;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - width * scale) * .5f,
                (Screen.height - height * scale) * .5f, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            Box(new Rect(0, 0, width, height), Ink);
            Box(new Rect(0, 0, width, 3), LatchScreen.Cyan);
            Write(new Rect(24, 18, 560, 27), "NORTHLINE  /  RECEPTION  /  SECURITY BUS", copy, LatchScreen.Cyan);
            Write(new Rect(24, 49, 560, 52), "RECONSTRUCT THE SIGNAL", heading, Color.white);
            Write(new Rect(24, 104, 560, 26), "Match the intercepted signature to restore the camera link.", copy, Color.white);
            Write(new Rect(24, 148, 210, 31), "INTERCEPTED SIGNATURE", copy, LatchScreen.Muted);
            Write(new Rect(24, 207, 210, 35), "LOCAL ROUTE", copy, LatchScreen.Muted);
            for (int i = 0; i < route.Length; i++)
            {
                float x = 258 + i * 104;
                Box(new Rect(x, 143, 86, 42), Plate);
                Write(new Rect(x, 143, 86, 42), $"0{Signature[i]}", digit, LatchScreen.Cyan);
                Box(new Rect(x, 202, 86, 52), i == cursor ? new Color(.17f,.33f,.36f) : Plate);
                if (i == cursor) Box(new Rect(x, 250, 86, 4), LatchScreen.Cyan);
                Write(new Rect(x, 202, 86, 52), $"0{route[i]}", digit, Color.white);
            }
            Write(new Rect(24, 267, 554, 25), mismatch ? "SIGNATURE MISMATCH  /  TRY AGAIN" : "ACCESS LOCKED UNTIL SIGNATURE MATCHES",
                copy, mismatch ? LatchScreen.Fault : LatchScreen.Amber);
            Write(new Rect(24, 303, 567, 24), "LEFT / RIGHT  Select      UP / DOWN  Adjust      ENTER  Verify      E  Exit", copy, LatchScreen.Cyan);
            GUI.color = color;
            GUI.matrix = previous;
        }

        static void Box(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        static void Write(Rect rect, string text, GUIStyle style, Color color)
        {
            GUI.color = Color.white;
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }
    }
}
