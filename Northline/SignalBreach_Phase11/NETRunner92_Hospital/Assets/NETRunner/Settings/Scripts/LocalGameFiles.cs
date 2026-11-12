using System;
using System.IO;
using NetRunner.Core;
using UnityEngine;

namespace NetRunner.Settings
{
    /// <summary>
    /// Where the game keeps its local files (settings and save slots) and how
    /// it writes them. The profile folder is the player's persistent data
    /// folder, or the folder given with <c>-net92-profile</c> (the acceptance
    /// checks use a fresh one). Files are written to a temporary file first
    /// and then swapped in, so a crash never leaves half a file behind.
    /// </summary>
    public static class LocalGameFiles
    {
        private const string ProfileOption = "-net92-profile";
        private const string ProfileFolder = "Northline";

        /// <summary>The folder for settings and saves.</summary>
        public static string ProfileDirectory
        {
            get
            {
                string chosen = LaunchArguments.ValueAfter(Environment.GetCommandLineArgs(), ProfileOption);
                return chosen != null ? Path.GetFullPath(chosen) : Path.Combine(Application.persistentDataPath, ProfileFolder);
            }
        }

        /// <summary>Writes <paramref name="text"/> to <paramref name="path"/> through a temporary file.</summary>
        public static void Write(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, text);
                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }
    }
}
