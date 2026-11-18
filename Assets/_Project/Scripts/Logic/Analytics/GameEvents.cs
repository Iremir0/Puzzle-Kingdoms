using PuzzleKingdoms.Logic.Flow;

namespace PuzzleKingdoms.Logic.Analytics
{
    /// <summary>
    /// The events the game records (PB-29, D-047), built here so their names and values are tested.
    /// Adding an event: add a builder, a test, and a line to docs/PRIVACY.md.
    /// </summary>
    public static class GameEvents
    {
        public const string AppStartedName = "app_started";
        public const string StageStartedName = "stage_started";
        public const string StageWonName = "stage_won";
        public const string StageLostName = "stage_lost";
        public const string TutorialSkippedName = "tutorial_skipped";
        public const string SettingChangedName = "setting_changed";

        /// <summary>The game opened: build version and the language shown.</summary>
        public static AnalyticsEvent AppStarted(string version, string language) =>
            new AnalyticsEvent(AppStartedName).With("version", version).With("language", language);

        /// <summary>A stage attempt began; <paramref name="attempt"/> counts attempts at this stage since the game opened.</summary>
        public static AnalyticsEvent StageStarted(string stageId, int attempt) =>
            new AnalyticsEvent(StageStartedName).With("stage", stageId).With("attempt", attempt);

        /// <summary>A finished attempt: won or lost, and how (from the playtest record, D-037).</summary>
        public static AnalyticsEvent StageFinished(StageAttempt attempt)
        {
            return new AnalyticsEvent(attempt.victory ? StageWonName : StageLostName)
                .With("stage", attempt.stage)
                .With("stars", attempt.stars)
                .With("timed_out", attempt.timedOut)
                .With("puzzle_seconds", attempt.puzzleSeconds)
                .With("camp_seconds", attempt.campSeconds)
                .With("battle_seconds", attempt.battleSeconds)
                .With("recruited", attempt.recruited)
                .With("merges", attempt.merges)
                .With("deployed", attempt.deployed)
                .With("survivors", attempt.survivors)
                .With("rally_earned", attempt.rallyEarned)
                .With("rally_used", attempt.rallyUsed)
                .With("gold", attempt.gold);
        }

        /// <summary>The player skipped the tutorial at the prompt <paramref name="stepId"/>.</summary>
        public static AnalyticsEvent TutorialSkipped(string stepId) =>
            new AnalyticsEvent(TutorialSkippedName).With("step", stepId);

        /// <summary>A setting changed (sound, music, battle_speed, language).</summary>
        public static AnalyticsEvent SettingChanged(string setting, string value) =>
            new AnalyticsEvent(SettingChangedName).With("setting", setting).With("value", value);
    }
}
