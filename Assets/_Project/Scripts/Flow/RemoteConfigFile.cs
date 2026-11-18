using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PuzzleKingdoms.Logic.Analytics;
using PuzzleKingdoms.Logic.Progression;
using UnityEngine;

namespace PuzzleKingdoms.Flow
{
    /// <summary>
    /// The local stand-in for an online config service (PB-29): <c>remote-config.txt</c> next to the save
    /// file, <c>key = value</c> lines such as <c>stage.stage_1_3.moves = 14</c>. Development builds only.
    /// </summary>
    public static class RemoteConfigFile
    {
        public const string FileName = "remote-config.txt";

        public static RemoteConfig Load(string folder)
        {
            string path = System.IO.Path.Combine(folder ?? Application.persistentDataPath, FileName);
            try
            {
                return File.Exists(path) ? RemoteConfig.Parse(File.ReadAllText(path)) : RemoteConfig.Empty;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RemoteConfig] could not read {path}: {e.Message}");
                return RemoteConfig.Empty;
            }
        }
    }
}
