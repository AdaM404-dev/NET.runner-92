namespace NetRunner.Core
{
    /// <summary>
    /// The reception terminal's puzzle: match the local route, three cells of
    /// 01 to 04, to the intercepted signature 03 01 04. Left and right select
    /// a cell, wrapping at the ends; up and down change it, wrapping from 04
    /// to 01; a wrong route shows a mismatch until the next change. Plain C#
    /// (see SignalPuzzleTests).
    /// </summary>
    public sealed class SignalPuzzle
    {
        /// <summary>The highest value of a cell; cells count 1 to this.</summary>
        public const int HighestValue = 4;

        private static readonly int[] Signature = { 3, 1, 4 };

        private readonly int[] route = new int[Signature.Length];

        /// <summary>How many cells the route has.</summary>
        public int Length => route.Length;

        /// <summary>The selected cell.</summary>
        public int Cursor { get; private set; }

        /// <summary>Whether the last check failed and nothing has changed since.</summary>
        public bool Mismatch { get; private set; }

        /// <summary>The prompt of the terminal: what E does, or that the CCTV is already unlocked.</summary>
        public static string Prompt(bool unlocked)
        {
            return unlocked ? "CCTV unlocked   /   1  Live cameras" : "E  Breach reception CCTV terminal";
        }

        /// <summary>The intercepted signature's value in a cell.</summary>
        public int SignatureAt(int cell)
        {
            return Signature[cell];
        }

        /// <summary>The route's value in a cell.</summary>
        public int RouteAt(int cell)
        {
            return route[cell];
        }

        /// <summary>Starts over: every cell 01, the first cell selected, no mismatch.</summary>
        public void Reset()
        {
            for (int i = 0; i < route.Length; i++)
            {
                route[i] = 1;
            }

            Cursor = 0;
            Mismatch = false;
        }

        /// <summary>Selects the next (+1) or previous (−1) cell, wrapping round.</summary>
        public void Select(int step)
        {
            Cursor = (Cursor + step + route.Length) % route.Length;
        }

        /// <summary>Changes the selected cell up (+1) or down (−1), wrapping between 04 and 01.</summary>
        public void Adjust(int step)
        {
            route[Cursor] = (route[Cursor] - 1 + step + HighestValue) % HighestValue + 1;
            Mismatch = false;
        }

        /// <summary>Checks the route; true when it matches the signature, else it marks a mismatch.</summary>
        public bool Submit()
        {
            for (int i = 0; i < route.Length; i++)
            {
                if (route[i] != Signature[i])
                {
                    Mismatch = true;
                    return false;
                }
            }

            return true;
        }
    }
}
