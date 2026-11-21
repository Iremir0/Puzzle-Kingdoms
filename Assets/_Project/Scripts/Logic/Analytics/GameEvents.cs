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
        public const string TutorialStepName = "tutorial_step";
        public const string ScreenOpenedName = "screen_opened";
        public const string BuildingUpgradedName = "building_upgraded";
        public const string QuestClaimedName = "quest_claimed";
        public const string CommanderChosenName = "commander_chosen";
        public const string ExpeditionEndedName = "expedition_ended";
        public const string AdRequestedName = "ad_requested";
        public const string AdRewardedName = "ad_rewarded";
        public const string SessionPausedName = "session_paused";
        public const string FeatureUnlockedName = "feature_unlocked";

        /// <summary>The screens <see cref="ScreenOpened"/> names (PB-36).</summary>
        public static readonly string[] Screens = { "map", "kingdom", "commanders", "quests", "shop", "daily", "expedition", "settings", "replays" };

        /// <summary>The places a rewarded ad is offered (PB-36).</summary>
        public static readonly string[] AdPlacements = { "extra_moves", "extra_rally", "shop_gold" };

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

        /// <summary>A tutorial prompt was shown (PB-36): the funnel of the first session, step by step.</summary>
        public static AnalyticsEvent TutorialStep(string stepId) =>
            new AnalyticsEvent(TutorialStepName).With("step", stepId);

        /// <summary>A main screen was opened (PB-36); <paramref name="screen"/> is one of <see cref="Screens"/>.</summary>
        public static AnalyticsEvent ScreenOpened(string screen)
        {
            if (System.Array.IndexOf(Screens, screen) < 0)
            {
                throw new System.ArgumentException("unknown screen " + screen, nameof(screen));
            }
            return new AnalyticsEvent(ScreenOpenedName).With("screen", screen);
        }

        /// <summary>A Kingdom building went up to <paramref name="level"/>; <paramref name="gold"/> is what is left.</summary>
        public static AnalyticsEvent BuildingUpgraded(string buildingId, int level, int gold) =>
            new AnalyticsEvent(BuildingUpgradedName).With("building", buildingId).With("level", level).With("gold_left", gold);

        /// <summary>A quest, achievement or star chest was claimed; <paramref name="gold"/> is the player's gold after it.</summary>
        public static AnalyticsEvent QuestClaimed(int gold, int stars) =>
            new AnalyticsEvent(QuestClaimedName).With("gold_after", gold).With("stars", stars);

        /// <summary>The player picked the commander who leads the army ("" = none).</summary>
        public static AnalyticsEvent CommanderChosen(string commanderId) =>
            new AnalyticsEvent(CommanderChosenName).With("commander", commanderId);

        /// <summary>An expedition ended: how deep it went, whether it was won, the gold it paid.</summary>
        public static AnalyticsEvent ExpeditionEnded(int depth, bool won, int gold) =>
            new AnalyticsEvent(ExpeditionEndedName).With("depth", depth).With("won", won).With("gold", gold);

        /// <summary>The player pressed a rewarded-ad button; <paramref name="placement"/> is one of <see cref="AdPlacements"/>.</summary>
        public static AnalyticsEvent AdRequested(string placement, bool supporter) =>
            new AnalyticsEvent(AdRequestedName).With("placement", Placement(placement)).With("supporter", supporter);

        /// <summary>The reward of a rewarded ad was granted (the ad was watched to the end, or the player is a Supporter).</summary>
        public static AnalyticsEvent AdRewarded(string placement) =>
            new AnalyticsEvent(AdRewardedName).With("placement", Placement(placement));

        /// <summary>The app went to the background after <paramref name="seconds"/> of play since it opened or came back.</summary>
        public static AnalyticsEvent SessionPaused(float seconds, string stageReached) =>
            new AnalyticsEvent(SessionPausedName).With("seconds", seconds).With("stage_reached", stageReached);

        /// <summary>A feature opened for the first time (PB-37): kingdom, quests, commanders, shop, daily, expedition.</summary>
        public static AnalyticsEvent FeatureUnlocked(string feature) =>
            new AnalyticsEvent(FeatureUnlockedName).With("feature", feature);

        private static string Placement(string placement)
        {
            if (System.Array.IndexOf(AdPlacements, placement) < 0)
            {
                throw new System.ArgumentException("unknown ad placement " + placement, nameof(placement));
            }
            return placement;
        }

        /// <summary>A setting changed (sound, music, battle_speed, language).</summary>
        public static AnalyticsEvent SettingChanged(string setting, string value) =>
            new AnalyticsEvent(SettingChangedName).With("setting", setting).With("value", value);
    }
}
