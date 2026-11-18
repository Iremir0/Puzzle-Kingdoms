using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PuzzleKingdoms.Logic.Analytics;
using PuzzleKingdoms.Logic.Progression;
using UnityEngine;

namespace PuzzleKingdoms.Flow
{
    /// <summary>Somewhere events go: a local file today, an online analytics service later (PB-29).</summary>
    public interface IAnalyticsSink
    {
        void Write(AnalyticsEvent analyticsEvent, string utcTime);
    }
}
