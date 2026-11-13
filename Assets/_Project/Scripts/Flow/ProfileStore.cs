using System;
using System.IO;
using PuzzleKingdoms.Logic.Progression;
using UnityEngine;

namespace PuzzleKingdoms.Flow
{
    /// <summary>
    /// Saves the <see cref="PlayerProfile"/> as JSON in the app's persistent data
    /// folder. A file that cannot be read is kept as <c>.bak</c> and a new
    /// profile starts, so a bad save never blocks the game.
    /// </summary>
    public sealed class ProfileStore
    {
        public const string FileName = "profile.json";

        public readonly string Path;

        public ProfileStore(string folder = null)
        {
            Path = System.IO.Path.Combine(folder ?? Application.persistentDataPath, FileName);
        }

        public PlayerProfile Load()
        {
            if (!File.Exists(Path))
            {
                return new PlayerProfile();
            }
            try
            {
                var snapshot = JsonUtility.FromJson<PlayerProfile.Snapshot>(File.ReadAllText(Path));
                return PlayerProfile.FromSnapshot(snapshot);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProfileStore] could not read {Path}, starting fresh: {e.Message}");
                File.Copy(Path, Path + ".bak", true);
                return new PlayerProfile();
            }
        }

        public void Save(PlayerProfile profile)
        {
            string json = JsonUtility.ToJson(profile.ToSnapshot(), true);
            string temp = Path + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
            File.Move(temp, Path);
        }
    }
}
