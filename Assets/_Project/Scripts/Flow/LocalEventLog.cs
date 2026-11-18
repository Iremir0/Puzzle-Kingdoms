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
    /// Development sink: one JSON line per event in <c>analytics-log.jsonl</c> next to the save file, so
    /// the events can be checked before an online service is connected. Not used in store builds.
    /// </summary>
    public sealed class LocalEventLog : IAnalyticsSink
    {
        public const string FileName = "analytics-log.jsonl";

        public readonly string Path;

        public LocalEventLog(string folder = null)
        {
            Path = System.IO.Path.Combine(folder ?? Application.persistentDataPath, FileName);
        }

        public void Write(AnalyticsEvent analyticsEvent, string utcTime) =>
            File.AppendAllText(Path, analyticsEvent.ToJsonLine(utcTime) + "\n");
    }
}
