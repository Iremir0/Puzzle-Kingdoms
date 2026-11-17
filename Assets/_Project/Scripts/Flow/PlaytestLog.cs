using System;
using System.Globalization;
using System.IO;
using PuzzleKingdoms.Logic.Flow;
using UnityEngine;

namespace PuzzleKingdoms.Flow
{
    /// <summary>
    /// Appends one JSON line per finished stage attempt to
    /// <c>playtest-log.jsonl</c> next to the save file (PB-5, D-037), so a
    /// playtest can be read afterwards: wins per stage, merges, Rally use,
    /// formations, time spent per screen. Local only, nothing is sent anywhere.
    /// Turn off for release (PB-17).
    /// </summary>
    public sealed class PlaytestLog
    {
        public const string FileName = "playtest-log.jsonl";

        public readonly string Path;
        private readonly string _session;
        private int _attempts;

        public PlaytestLog(string folder = null)
        {
            Path = System.IO.Path.Combine(folder ?? Application.persistentDataPath, FileName);
            _session = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        }

        /// <summary>Writes the attempt with the session fields filled in. Never throws: a log must not break the game.</summary>
        public void Write(StageAttempt attempt, float puzzleSeconds, float campSeconds)
        {
            if (attempt == null)
            {
                return;
            }
            attempt.session = _session;
            attempt.attempt = ++_attempts;
            attempt.finishedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            attempt.puzzleSeconds = Mathf.Round(puzzleSeconds * 10f) / 10f;
            attempt.campSeconds = Mathf.Round(campSeconds * 10f) / 10f;
            try
            {
                File.AppendAllText(Path, JsonUtility.ToJson(attempt) + "\n");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlaytestLog] could not write {Path}: {e.Message}");
            }
        }
    }
}
