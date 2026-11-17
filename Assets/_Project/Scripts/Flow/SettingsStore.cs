using System;
using System.IO;
using PuzzleKingdoms.Logic.Progression;
using UnityEngine;

namespace PuzzleKingdoms.Flow
{
    /// <summary>
    /// Saves <see cref="GameSettings"/> as <c>settings.json</c> next to the profile
    /// (D-039), so resetting progress keeps them. A broken file gives the defaults.
    /// </summary>
    public sealed class SettingsStore
    {
        public const string FileName = "settings.json";

        public readonly string Path;

        public SettingsStore(string folder = null)
        {
            Path = System.IO.Path.Combine(folder ?? Application.persistentDataPath, FileName);
        }

        public GameSettings Load()
        {
            string json = SafeFile.Read(Path);
            if (json == null)
            {
                return new GameSettings();
            }
            try
            {
                GameSettings settings = JsonUtility.FromJson<GameSettings>(json);
                if (settings == null)
                {
                    throw new InvalidDataException("empty settings");
                }
                return settings.Normalize();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SettingsStore] could not read {Path}, using defaults: {e.Message}");
                File.WriteAllText(Path + ProfileStore.BackupSuffix, json);
                return new GameSettings();
            }
        }

        public void Save(GameSettings settings) => SafeFile.Write(Path, JsonUtility.ToJson(settings, true));
    }
}
