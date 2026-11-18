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
    /// Error reports (PB-29): every exception the game logs becomes one JSON line in
    /// <c>error-log.jsonl</c>, but only while the player allows error reports, and at most
    /// <see cref="MaxPerSession"/> per session. An online crash service replaces the file later.
    /// </summary>
    public sealed class ErrorReporter : IDisposable
    {
        public const string FileName = "error-log.jsonl";
        public const int MaxPerSession = 20;
        private const int MaxMessage = 300;

        public readonly string Path;
        private readonly Func<GameSettings> _settings;
        private readonly string _version;
        private int _written;

        public ErrorReporter(string folder, Func<GameSettings> settings, string version)
        {
            Path = System.IO.Path.Combine(folder ?? Application.persistentDataPath, FileName);
            _settings = settings;
            _version = version ?? "";
            Application.logMessageReceived += Record;
        }

        public void Dispose() => Application.logMessageReceived -= Record;

        /// <summary>Handles one log message; public so tests can call it without throwing an exception.</summary>
        public void Record(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Exception || _written >= MaxPerSession || !Consent.AllowsCrashReports(_settings()))
            {
                return;
            }
            _written++;
            string text = message ?? "";
            string where = (stackTrace ?? "").Split('\n')[0].Trim();
            AnalyticsEvent report = new AnalyticsEvent("error")
                .With("message", text.Length > MaxMessage ? text.Substring(0, MaxMessage) : text)
                .With("where", where)
                .With("version", _version);
            try
            {
                File.AppendAllText(Path, report.ToJsonLine(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ",
                    CultureInfo.InvariantCulture)) + "\n");
            }
            catch (Exception)
            {
                // Nowhere to report a failed error report; never throw from a log callback.
            }
        }
    }
}
