using PuzzleKingdoms.Logic.Progression;

namespace PuzzleKingdoms.Logic.Analytics
{
    /// <summary>
    /// The player's consent to data collection (PB-29, D-047). Opt-in: every switch starts off and
    /// nothing is collected before the player answers. When the consent text changes, raise
    /// <see cref="CurrentVersion"/> and every player is asked again.
    /// </summary>
    public static class Consent
    {
        /// <summary>The version of the consent text shown today.</summary>
        public const int CurrentVersion = 1;

        /// <summary>True when the player has not answered the current consent text yet.</summary>
        public static bool NeedsAsking(GameSettings settings) => settings == null || settings.consentVersion < CurrentVersion;

        /// <summary>Stores the player's answer to the current consent text.</summary>
        public static void Answer(GameSettings settings, bool analytics, bool crashReports, bool personalisedAds)
        {
            settings.analytics = analytics;
            settings.crashReports = crashReports;
            settings.personalisedAds = personalisedAds;
            settings.consentVersion = CurrentVersion;
        }

        /// <summary>May anonymous play statistics be recorded? Only after a yes to the current text.</summary>
        public static bool AllowsAnalytics(GameSettings settings) => !NeedsAsking(settings) && settings.analytics;

        /// <summary>May error reports be recorded? Only after a yes to the current text.</summary>
        public static bool AllowsCrashReports(GameSettings settings) => !NeedsAsking(settings) && settings.crashReports;
    }
}
