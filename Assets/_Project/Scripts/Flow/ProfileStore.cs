using System;
using System.IO;
using PuzzleKingdoms.Logic.Progression;
using UnityEngine;

namespace PuzzleKingdoms.Flow
{
    /// <summary>
    /// Saves the <see cref="PlayerProfile"/> as JSON in the app's persistent data
    /// folder, through <see cref="SafeFile"/> so a crash never loses the save
    /// (PB-14). A file that cannot be read, or one from a newer game version, is
    /// kept as <c>.bak</c> and a new profile starts, so a bad save never blocks
    /// the game (D-039).
    /// </summary>
    public sealed class ProfileStore
    {
        public const string FileName = "profile.json";
        public const string BackupSuffix = ".bak";

        public readonly string Folder;
        public readonly string Path;

        public ProfileStore(string folder = null)
        {
            Folder = folder ?? Application.persistentDataPath;
            Path = System.IO.Path.Combine(Folder, FileName);
        }

        public PlayerProfile Load()
        {
            string json = SafeFile.Read(Path);
            if (json == null)
            {
                return new PlayerProfile();
            }
            try
            {
                var snapshot = JsonUtility.FromJson<PlayerProfile.Snapshot>(json);
                if (snapshot == null)
                {
                    throw new InvalidDataException("empty save");
                }
                return PlayerProfile.FromSnapshot(snapshot);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProfileStore] could not read {Path}, starting fresh (kept as {BackupSuffix}): {e.Message}");
                File.WriteAllText(Path + BackupSuffix, json);
                return new PlayerProfile();
            }
        }

        public void Save(PlayerProfile profile) => SafeFile.Write(Path, JsonUtility.ToJson(profile.ToSnapshot(), true));
    }
}
