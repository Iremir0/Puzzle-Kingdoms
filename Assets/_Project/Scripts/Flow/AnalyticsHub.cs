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
    /// Passes game events to the sinks, and only while the player allows play statistics
    /// (<see cref="Consent.AllowsAnalytics"/>, D-047). Without consent an event is dropped, not kept for later.
    /// A sink that fails is logged and skipped: statistics must never break the game.
    /// </summary>
    public sealed class AnalyticsHub
    {
        private readonly Func<GameSettings> _settings;
        private readonly List<IAnalyticsSink> _sinks = new List<IAnalyticsSink>();

        public AnalyticsHub(Func<GameSettings> settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Events passed on to the sinks since the start (tests read it).</summary>
        public int Sent { get; private set; }

        public void Add(IAnalyticsSink sink)
        {
            if (sink != null)
            {
                _sinks.Add(sink);
            }
        }

        public void Send(AnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null || !Consent.AllowsAnalytics(_settings()))
            {
                return;
            }
            string at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            foreach (IAnalyticsSink sink in _sinks)
            {
                try
                {
                    sink.Write(analyticsEvent, at);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Analytics] {sink.GetType().Name} failed: {e.Message}");
                }
            }
            Sent++;
        }
    }
}
