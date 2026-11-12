using NetRunner.Core;
using NetRunner.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetRunner.World
{
    /// <summary>
    /// The reception desk's security monitor. E opens a full-screen puzzle
    /// that takes the keyboard (an <see cref="ITerminal"/>); matching the
    /// local route to the intercepted signature (Core's
    /// <see cref="SignalPuzzle"/>) unlocks the CCTV for this session and
    /// lights the link indicator. The puzzle is drawn with OnGUI.
    /// </summary>
    public sealed class ReceptionSecurityTerminal : MonoBehaviour, ITerminal
    {
        // The panel's size in its own pixels; every layout number in OnGUI is
        // in these pixels, and the panel is scaled to the screen.
        private const float PanelWidth = 606f;
        private const float PanelHeight = 340f;
        private const float ReferenceScreenWidth = 1300f;
        private const float SmallestScale = .65f;
        private const float LargestScale = 1.35f;
        private const float HeightShare = .87f;
        private const float CellLeft = 258f;
        private const float CellPitch = 104f;
        private const int HeadingSize = 30;
        private const int CopySize = 15;
        private const int DigitSize = 26;

        private static readonly int Emission = Shader.PropertyToID("_EmissiveColor");
        private static readonly Color LinkedGlow = new Color(.12f, 1f, .82f) * 2.2f;
        private static readonly Color Ink = new Color(.047f, .095f, .108f);
        private static readonly Color Plate = new Color(.075f, .154f, .168f);
        private static readonly Color SelectedPlate = new Color(.17f, .33f, .36f);

        [SerializeField] private SecurityCctv cctv;
        [SerializeField] private Renderer linkIndicator;

        private readonly SignalPuzzle puzzle = new SignalPuzzle();
        private HospitalInteraction operatorInput;
        private GUIStyle heading;
        private GUIStyle copy;
        private GUIStyle digit;

        /// <summary>Whether the puzzle is open and has the keyboard.</summary>
        public bool IsOpen => operatorInput;

        /// <inheritdoc/>
        public string Prompt => SignalPuzzle.Prompt(cctv.Unlocked);

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = HeadingSize, fontStyle = FontStyle.Bold };
                copy = new GUIStyle(GUI.skin.label) { fontSize = CopySize };
                digit = new GUIStyle(GUI.skin.label) { fontSize = DigitSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }

            float scale = Mathf.Clamp(Screen.width / ReferenceScreenWidth, SmallestScale, LargestScale);
            scale = Mathf.Min(scale, Screen.height * HeightShare / PanelHeight);
            Matrix4x4 previous = GUI.matrix;
            Color color = GUI.color;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - PanelWidth * scale) * .5f,
                (Screen.height - PanelHeight * scale) * .5f, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            Box(new Rect(0, 0, PanelWidth, PanelHeight), Ink);
            Box(new Rect(0, 0, PanelWidth, 3), LatchScreen.Cyan);
            Write(new Rect(24, 18, 560, 27), "NORTHLINE  /  RECEPTION  /  SECURITY BUS", copy, LatchScreen.Cyan);
            Write(new Rect(24, 49, 560, 52), "RECONSTRUCT THE SIGNAL", heading, Color.white);
            Write(new Rect(24, 104, 560, 26), "Match the intercepted signature to restore the camera link.", copy, Color.white);
            Write(new Rect(24, 148, 210, 31), "INTERCEPTED SIGNATURE", copy, LatchScreen.Muted);
            Write(new Rect(24, 207, 210, 35), "LOCAL ROUTE", copy, LatchScreen.Muted);
            for (int i = 0; i < puzzle.Length; i++)
            {
                float x = CellLeft + i * CellPitch;
                bool selected = i == puzzle.Cursor;
                Box(new Rect(x, 143, 86, 42), Plate);
                Write(new Rect(x, 143, 86, 42), $"0{puzzle.SignatureAt(i)}", digit, LatchScreen.Cyan);
                Box(new Rect(x, 202, 86, 52), selected ? SelectedPlate : Plate);
                if (selected)
                {
                    Box(new Rect(x, 250, 86, 4), LatchScreen.Cyan);
                }

                Write(new Rect(x, 202, 86, 52), $"0{puzzle.RouteAt(i)}", digit, Color.white);
            }

            Write(new Rect(24, 267, 554, 25), puzzle.Mismatch ? "SIGNATURE MISMATCH  /  TRY AGAIN" : "ACCESS LOCKED UNTIL SIGNATURE MATCHES",
                copy, puzzle.Mismatch ? LatchScreen.Fault : LatchScreen.Amber);
            Write(new Rect(24, 303, 567, 24), "LEFT / RIGHT  Select      UP / DOWN  Adjust      ENTER  Verify      E  Exit", copy, LatchScreen.Cyan);
            GUI.color = color;
            GUI.matrix = previous;
        }

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

            cctv.Unlock();
            if (linkIndicator)
            {
                var block = new MaterialPropertyBlock();
                linkIndicator.GetPropertyBlock(block);
                block.SetColor(Emission, LinkedGlow);
                linkIndicator.SetPropertyBlock(block);
            }

            Close();
            return true;
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

        /// <inheritdoc/>
        public void ReadInput(Keyboard keyboard)
        {
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                Select(-1);
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                Select(1);
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                Adjust(1);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                Adjust(-1);
            }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                Submit();
            }
            else if (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private static void Box(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private static void Write(Rect rect, string text, GUIStyle style, Color color)
        {
            GUI.color = Color.white;
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }
    }
}
