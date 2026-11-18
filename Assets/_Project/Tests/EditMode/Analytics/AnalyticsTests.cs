using System.Collections.Generic;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Analytics;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Tests.Analytics
{
    public class ConsentTests
    {
        [Test]
        public void NothingIsAllowedBeforeTheAnswer()
        {
            var settings = new GameSettings();
            Assert.IsTrue(Consent.NeedsAsking(settings));
            Assert.IsFalse(settings.analytics || settings.crashReports || settings.personalisedAds, "every switch starts off");
            Assert.IsFalse(Consent.AllowsAnalytics(settings));
            Assert.IsFalse(Consent.AllowsCrashReports(settings));
        }

        [Test]
        public void AnAnswerIsStoredWithTheTextVersion()
        {
            var settings = new GameSettings();
            Consent.Answer(settings, analytics: true, crashReports: false, personalisedAds: false);
            Assert.IsFalse(Consent.NeedsAsking(settings));
            Assert.AreEqual(Consent.CurrentVersion, settings.consentVersion);
            Assert.IsTrue(Consent.AllowsAnalytics(settings));
            Assert.IsFalse(Consent.AllowsCrashReports(settings));
        }

        [Test]
        public void AnAnswerToAnOlderTextNoLongerCounts()
        {
            var settings = new GameSettings { analytics = true, crashReports = true, consentVersion = Consent.CurrentVersion - 1 };
            Assert.IsTrue(Consent.NeedsAsking(settings));
            Assert.IsFalse(Consent.AllowsAnalytics(settings), "a yes to an old text is asked again");
            Assert.AreEqual(0, new GameSettings { consentVersion = -3 }.Normalize().consentVersion);
        }
    }

    public class AnalyticsEventTests
    {
        [Test]
        public void JsonLineKeepsOrderAndTypes()
        {
            string line = new AnalyticsEvent("stage_won")
                .With("stage", "stage_1_1").With("stars", 3).With("battle_seconds", 21.26f).With("timed_out", false)
                .ToJsonLine("2026-11-18T10:00:00Z");
            Assert.AreEqual("{\"event\":\"stage_won\",\"at\":\"2026-11-18T10:00:00Z\",\"stage\":\"stage_1_1\"," +
                            "\"stars\":3,\"battle_seconds\":21.3,\"timed_out\":false}", line);
        }

        [Test]
        public void TextIsEscaped()
        {
            string line = new AnalyticsEvent("error").With("message", "a \"quote\" \\ and\nnew line\u0001").ToJsonLine(null);
            Assert.AreEqual("{\"event\":\"error\",\"message\":\"a \\\"quote\\\" \\\\ and\\nnew line\\u0001\"}", line);
        }

        [Test]
        public void NamesAndKeysAreRequired()
        {
            Assert.Throws<System.ArgumentException>(() => new AnalyticsEvent(""));
            Assert.Throws<System.ArgumentException>(() => new AnalyticsEvent("x").With("", 1));
        }
    }

    public class GameEventsTests
    {
        private static StageAttempt Attempt(bool victory) => new StageAttempt
        {
            stage = "stage_1_2", victory = victory, timedOut = !victory, stars = victory ? 2 : 0,
            puzzleSeconds = 40.5f, campSeconds = 12f, battleSeconds = 25.25f, recruited = 9, merges = 1,
            deployed = 7, survivors = 3, rallyEarned = 2, rallyUsed = 1, gold = 55,
            formation = "W1 W1 . . | . . . . | . . . . | A1 . . A1", session = "s", recruitedByClass = "warrior 5",
        };

        [Test]
        public void FinishedStageIsWonOrLostWithItsNumbers()
        {
            AnalyticsEvent won = GameEvents.StageFinished(Attempt(true));
            Assert.AreEqual(GameEvents.StageWonName, won.Name);
            Assert.AreEqual("stage_1_2", won["stage"]);
            Assert.AreEqual(2, won["stars"]);
            Assert.AreEqual(25.3f, (float)won["battle_seconds"], 0.001f);
            Assert.AreEqual(1, won["rally_used"]);
            Assert.AreEqual(GameEvents.StageLostName, GameEvents.StageFinished(Attempt(false)).Name);
        }

        [Test]
        public void EventsCarryNoFreeText()
        {
            // The formation, session and class list stay in the local playtest log only.
            AnalyticsEvent won = GameEvents.StageFinished(Attempt(true));
            Assert.IsNull(won["formation"]);
            Assert.IsNull(won["session"]);
            Assert.IsNull(won["recruitedByClass"]);
        }

        [Test]
        public void OtherEvents()
        {
            Assert.AreEqual(3, GameEvents.StageStarted("stage_1_1", 3)["attempt"]);
            Assert.AreEqual("1-1.camp", GameEvents.TutorialSkipped("1-1.camp")["step"]);
            AnalyticsEvent setting = GameEvents.SettingChanged("language", "tr");
            Assert.AreEqual(GameEvents.SettingChangedName, setting.Name);
            Assert.AreEqual("tr", setting["value"]);
            Assert.AreEqual("tr", GameEvents.AppStarted("0.1.0", "tr")["language"]);
        }
    }

    public class RemoteConfigTests
    {
        private static StageDef Stage() => new StageDef
        {
            Id = "stage_1_3", Moves = 12, StoneGems = 5, EnemyHpMultiplier = 1.2f, EnemyDamageMultiplier = 1f,
        };

        [Test]
        public void ParsesKeyValueLines()
        {
            RemoteConfig config = RemoteConfig.Parse("# note\n stage.stage_1_3.moves = 14 \r\nbroken line\n=5\nx = 1\nx = 2\n");
            Assert.AreEqual(2, config.Count);
            Assert.IsTrue(config.TryGetInt("stage.stage_1_3.moves", out int moves));
            Assert.AreEqual(14, moves);
            Assert.IsTrue(config.TryGetInt("x", out int x));
            Assert.AreEqual(2, x, "a later line wins");
        }

        [Test]
        public void NumbersUseADotEverywhere()
        {
            RemoteConfig config = RemoteConfig.Parse("a = 1.25\nb = 1,25\nc = NaN\nd = twelve");
            Assert.IsTrue(config.TryGetFloat("a", out float a));
            Assert.AreEqual(1.25f, a, 0.0001f);
            Assert.IsFalse(config.TryGetFloat("b", out _));
            Assert.IsFalse(config.TryGetFloat("c", out _));
            Assert.IsFalse(config.TryGetInt("d", out _));
            Assert.IsFalse(config.TryGetInt("missing", out _));
        }

        [Test]
        public void OverridesChangeOnlyTheirStage()
        {
            StageDef stage = Stage();
            var config = new RemoteConfig(new Dictionary<string, string>
            {
                ["stage.stage_1_3.moves"] = "14",
                ["stage.stage_1_3.enemy_hp"] = "1.1",
                ["stage.stage_1_2.moves"] = "20",
            });
            List<string> applied = StageTuning.Apply(stage, config);
            Assert.AreEqual(14, stage.Moves);
            Assert.AreEqual(1.1f, stage.EnemyHpMultiplier, 0.0001f);
            Assert.AreEqual(5, stage.StoneGems, "no key: the asset's value");
            Assert.AreEqual(1f, stage.EnemyDamageMultiplier);
            CollectionAssert.AreEquivalent(new[] { "stage.stage_1_3.moves", "stage.stage_1_3.enemy_hp" }, applied);
        }

        [Test]
        public void ValuesOutOfRangeAreIgnored()
        {
            StageDef stage = Stage();
            var config = new RemoteConfig(new Dictionary<string, string>
            {
                ["stage.stage_1_3.moves"] = "0",
                ["stage.stage_1_3.stones"] = "99",
                ["stage.stage_1_3.enemy_hp"] = "50",
                ["stage.stage_1_3.enemy_damage"] = "abc",
            });
            CollectionAssert.IsEmpty(StageTuning.Apply(stage, config));
            Assert.AreEqual(12, stage.Moves);
            Assert.AreEqual(5, stage.StoneGems);
            Assert.AreEqual(1.2f, stage.EnemyHpMultiplier, 0.0001f);
        }

        [Test]
        public void NoConfigChangesNothing()
        {
            StageDef stage = Stage();
            CollectionAssert.IsEmpty(StageTuning.Apply(stage, RemoteConfig.Empty));
            CollectionAssert.IsEmpty(StageTuning.Apply(stage, null));
            Assert.AreEqual(12, stage.Moves);
        }
    }
}
