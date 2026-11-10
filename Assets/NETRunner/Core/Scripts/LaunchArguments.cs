using System;

namespace NetRunner.Core
{
    /// <summary>
    /// Reads options given when the game is started, such as
    /// <c>NET.runner-92.exe -net92-quality Low</c> (see LaunchArgumentsTests).
    /// </summary>
    public static class LaunchArguments
    {
        /// <summary>
        /// The word right after <paramref name="option"/>, or null when the
        /// option is missing or is the last word. The option is matched exactly.
        /// </summary>
        public static string ValueAfter(string[] arguments, string option)
        {
            if (arguments == null)
            {
                return null;
            }

            int index = Array.IndexOf(arguments, option);
            if (index < 0 || index + 1 >= arguments.Length)
            {
                return null;
            }

            return arguments[index + 1];
        }
    }
}
