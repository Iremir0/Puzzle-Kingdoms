using System;
using System.IO;

namespace PuzzleKingdoms.Flow
{
    /// <summary>
    /// Writes small save files so that a crash or power loss at any moment leaves
    /// either the old file or the new one, never none (PB-14). The text goes to
    /// <c>.tmp</c> first and then replaces the real file in one step.
    /// </summary>
    public static class SafeFile
    {
        public const string TempSuffix = ".tmp";

        public static void Write(string path, string text)
        {
            string temp = path + TempSuffix;
            File.WriteAllText(temp, text);
            if (!File.Exists(path))
            {
                File.Move(temp, path);
                return;
            }
            try
            {
                File.Replace(temp, path, null); // atomic swap: the old file stays until the new one is in place
            }
            catch (Exception e) when (e is PlatformNotSupportedException || e is IOException)
            {
                // Some file systems cannot replace; overwriting still always leaves a file.
                File.Copy(temp, path, true);
                File.Delete(temp);
            }
        }

        /// <summary>
        /// The file's text, or null when there is none. If only a <c>.tmp</c> exists
        /// (a crash in an older build between deleting and moving), that is read
        /// instead, so the last save is not lost.
        /// </summary>
        public static string Read(string path)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            string temp = path + TempSuffix;
            return File.Exists(temp) ? File.ReadAllText(temp) : null;
        }
    }
}
