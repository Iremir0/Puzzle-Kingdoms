using System;
using System.IO;
using PuzzleKingdoms.Logic.Progression;
using UnityEngine;

namespace PuzzleKingdoms.Flow
{
    /// <summary>
    /// Saves the <see cref="PlayerProfile"/> as JSON in the app's persistent data
    /// folder, through <see cref="SafeFile"/> so a crash never loses the save
    /// (PB-14). A file that cannot be read is kept as <c>.bak</c> and a new
    /// profile starts, so a bad save never blocks the game (D-039). A file from a
    /// newer game version (an older build installed over a newer one) is also
    /// copied to <c>.bak</c>, and the store is <see cref="Locked"/>: nothing is
    /// written over it, so updating the game brings the progress back (D-067).
    /// </summary>
    public sealed class ProfileStore
    {
        public const string FileName = "profile.json";
        public const string BackupSuffix = ".bak";

        public readonly string Folder;
        public readonly string Path;

        /// <summary>
        /// True when the file on disk comes from a newer game version. The game plays on a fresh profile, but
        /// <see cref="Save"/> writes nothing, so the newer save stays as it is until the game is updated (D-067).
        /// </summary>
        public bool Locked { get; private set; }

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
                if (PlayerProfile.IsTooNew(snapshot))
                {
                    Debug.LogWarning($"[ProfileStore] {Path} is save version {snapshot.version}, newer than {PlayerProfile.CurrentVersion}: not writing until the game is updated");
                    KeepBackup(json);
                    Locked = true;
                    return new PlayerProfile();
                }
                return PlayerProfile.FromSnapshot(snapshot);
            }
            catch (Exception e)
            {
                string backup = KeepBackup(json);
                Debug.LogWarning($"[ProfileStore] could not read {Path}, starting fresh (kept as {backup}): {e.Message}");
                return new PlayerProfile();
            }
        }

        public void Save(PlayerProfile profile)
        {
            if (Locked)
            {
                return;
            }
            SafeFile.Write(Path, JsonUtility.ToJson(profile.ToSnapshot(), true));
        }

        /// <summary>
        /// Copies a save that was not loaded to <c>profile.json.bak</c>, or <c>.bak2</c>, <c>.bak3</c> ... when
        /// that exists, so a second bad file never replaces the first backup. Returns the path written.
        /// </summary>
        private string KeepBackup(string json)
        {
            string backup = Path + BackupSuffix;
            for (int n = 2; File.Exists(backup); n++)
            {
                if (File.ReadAllText(backup) == json)
                {
                    return backup; // the same file again (a locked save is seen at every start)
                }
                backup = Path + BackupSuffix + n;
            }
            File.WriteAllText(backup, json);
            return backup;
        }
    }
}
