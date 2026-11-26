using System;
using System.Text.RegularExpressions;

namespace NetRunner.Core
{
    /// <summary>
    /// Tells integrated graphics (built into the processor) from a separate
    /// graphics card by the name the driver reports, so that a first start can
    /// pick a preset the machine can run (#36, Samuel's decision of
    /// 2026-11-26). Intel graphics are integrated except the Arc A and B series
    /// cards; AMD's integrated ones are the Radeon "… Graphics" without an RX
    /// model number. A name it does not know counts as a separate card, which
    /// keeps the start as it was before this rule.
    /// </summary>
    public static class GraphicsHardware
    {
        private static readonly Regex IntelCard = new Regex(@"Arc(\(TM\))?\s+[AB]\d", RegexOptions.IgnoreCase);
        private static readonly Regex RadeonCard = new Regex(@"RX\s*\d{3,4}", RegexOptions.IgnoreCase);

        /// <summary>Whether <paramref name="deviceName"/> names integrated graphics.</summary>
        public static bool IsIntegrated(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                return false;
            }

            if (Mentions(deviceName, "Intel"))
            {
                return !IntelCard.IsMatch(deviceName);
            }

            if (Mentions(deviceName, "Radeon"))
            {
                return Mentions(deviceName, "Graphics") && !RadeonCard.IsMatch(deviceName);
            }

            return false;
        }

        private static bool Mentions(string text, string word)
        {
            return text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
