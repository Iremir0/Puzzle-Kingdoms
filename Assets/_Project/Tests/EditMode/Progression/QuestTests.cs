using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Quests;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>PB-24 (D-060): daily quests with a day change, achievements, star chests, and the save.</summary>
    public class QuestTests
    {
        private static QuestRules Rules()
        {
            var rules = new QuestRules { DailyCount = 3, StarsPerChest = 30, ChestGold = 150 };
            rules.Daily.Add(new QuestDef { Id = "d_win", Kind = QuestKind.WinStages, Target = 2, RewardGold = 100 });
            rules.Daily.Add(new QuestDef { Id = "d_rally", Kind = QuestKind.UseRally, Target = 3, RewardGold = 60 });
            rules.Daily.Add(new QuestDef { Id = "d_recruit", Kind = QuestKind.RecruitUnits, Target = 10, RewardGold = 60 });
            rules.Daily.Add(new QuestDef { Id = "d_merge", Kind = QuestKind.MergeUnits, Target = 2, RewardGold = 60 });
            rules.Daily.Add(new QuestDef { Id = "d_win2", Kind = QuestKind.WinStages, Target = 5, RewardGold = 200 });
            rules.Achievements.Add(new QuestDef { Id = "a_win", Kind = QuestKind.WinStages, Target = 3, RewardGold = 300 });
            rules.Achievements.Add(new QuestDef { Id = "a_tier", Kind = QuestKind.ReachTier3, Target = 1, RewardGold = 400 });
            return rules;
        }

        private static QuestEvent Win() => new QuestEvent { Won = true, Recruited = 6, Merges = 1, RallyUsed = 1, Gold = 80, MaxTier = 2 };

        [Test]
        public void TheSameDayDrawsTheSameQuestsAndTheyDifferInKind()
        {
            var a = new QuestBoard(Rules(), new QuestLog());
            var b = new QuestBoard(Rules(), new QuestLog());
            var first = a.PickDaily("2026-11-18").Select(q => q.Id).ToList();

            CollectionAssert.AreEqual(first, b.PickDaily("2026-11-18").Select(q => q.Id).ToList());
            Assert.AreEqual(3, first.Count);
            Assert.AreEqual(3, a.PickDaily("2026-11-18").Select(q => q.Kind).Distinct().Count(), "different kinds");
        }

        [Test]
        public void ADayChangeDrawsNewQuestsAndResetsProgress()
        {
            var log = new QuestLog();
            var board = new QuestBoard(Rules(), log);
            Assert.IsTrue(board.Refresh("2026-11-18"));
            board.Record(Win());
            Assert.IsTrue(log.DailyProgress.Any(p => p > 0), "today has progress");

            Assert.IsFalse(board.Refresh("2026-11-18"), "the same day changes nothing");
            Assert.IsTrue(board.Refresh("2026-11-19"));
            Assert.AreEqual("2026-11-19", log.Day);
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, log.DailyProgress);
            CollectionAssert.AreEqual(new[] { false, false, false }, log.DailyClaimed);
        }

        [Test]
        public void AFinishedQuestIsPaidOnceAndOnlyWhenFinished()
        {
            var log = new QuestLog();
            var board = new QuestBoard(Rules(), log);
            board.Refresh("2026-11-18");
            QuestStatus quest = board.Daily().First();

            Assert.IsFalse(board.TryClaim(quest.Def.Id, out _), "not finished yet");
            for (int i = 0; i < quest.Def.Target + 15; i++)
            {
                board.Record(new QuestEvent { Won = true, RallyUsed = 5, Recruited = 20, Merges = 5, Gold = 500, MaxTier = 3 });
            }
            Assert.IsTrue(board.TryClaim(quest.Def.Id, out int gold));
            Assert.AreEqual(quest.Def.RewardGold, gold);
            Assert.IsFalse(board.TryClaim(quest.Def.Id, out _), "claimed already");
            Assert.IsFalse(board.TryClaim("nope", out _), "unknown quest");
        }

        [Test]
        public void ProgressNeverGoesAboveTheTarget()
        {
            var log = new QuestLog();
            var board = new QuestBoard(Rules(), log);
            board.Refresh("2026-11-18");
            for (int i = 0; i < 20; i++)
            {
                board.Record(Win());
            }
            foreach (QuestStatus status in board.Daily().Concat(board.Achievements()))
            {
                Assert.LessOrEqual(status.Progress, status.Def.Target, status.Def.Id);
            }
        }

        [Test]
        public void AchievementsKeepTheirProgressAcrossDays()
        {
            var log = new QuestLog();
            var board = new QuestBoard(Rules(), log);
            board.Refresh("2026-11-18");
            board.Record(Win());
            board.Refresh("2026-11-19");
            board.Record(Win());

            QuestStatus wins = board.Achievements().First(s => s.Def.Id == "a_win");
            Assert.AreEqual(2, wins.Progress);
            Assert.IsFalse(board.Achievements().First(s => s.Def.Id == "a_tier").Complete, "tier 2 is not tier 3");
            board.Record(new QuestEvent { Won = true, MaxTier = 3 });
            Assert.IsTrue(board.TryClaim("a_win", out int gold));
            Assert.AreEqual(300, gold);
            Assert.IsTrue(board.TryClaim("a_tier", out _));
        }

        [Test]
        public void ANewAchievementInThePoolAppearsInAnOldSave()
        {
            var log = new QuestLog { Day = "2026-11-18" };
            var rules = Rules();
            var board = new QuestBoard(rules, log);
            board.Refresh("2026-11-18");
            rules.Achievements.Add(new QuestDef { Id = "a_new", Kind = QuestKind.EarnGold, Target = 100, RewardGold = 10 });
            Assert.IsTrue(board.Refresh("2026-11-18"));
            Assert.AreEqual(3, board.Achievements().Count);
        }

        [Test]
        public void AStarChestComesWithEveryThirtyStars()
        {
            var log = new QuestLog();
            var board = new QuestBoard(Rules(), log);

            Assert.AreEqual(0, board.ChestsReady(29));
            Assert.IsFalse(board.TryClaimChest(29, out _));
            Assert.AreEqual(1, board.ChestsReady(30));
            Assert.AreEqual(2, board.ChestsReady(65));
            Assert.AreEqual(5, board.StarsTowardsNextChest(65));

            Assert.IsTrue(board.TryClaimChest(65, out int gold));
            Assert.AreEqual(150, gold);
            Assert.AreEqual(1, board.ChestsReady(65));
            Assert.IsTrue(board.TryClaimChest(65, out _));
            Assert.IsFalse(board.TryClaimChest(65, out _), "both claimed");
        }

        [Test]
        public void TheBadgeCountsWhatWaitsToBeClaimed()
        {
            var log = new QuestLog();
            var board = new QuestBoard(Rules(), log);
            board.Refresh("2026-11-18");
            Assert.AreEqual(0, board.ClaimableCount(0));
            Assert.AreEqual(1, board.ClaimableCount(30), "a chest");
            for (int i = 0; i < 6; i++)
            {
                board.Record(new QuestEvent { Won = true, RallyUsed = 5, Recruited = 20, Merges = 5, Gold = 500, MaxTier = 3 });
            }
            Assert.Greater(board.ClaimableCount(0), 3, "quests and achievements are ready");
        }

        [Test]
        public void QuestsAreSavedAndAnOlderSaveLoadsWithNone()
        {
            var profile = new PlayerProfile();
            var board = new QuestBoard(Rules(), profile.Quests);
            board.Refresh("2026-11-18");
            board.Record(Win());
            board.TryClaimChest(60, out _);

            PlayerProfile loaded = PlayerProfile.FromSnapshot(profile.ToSnapshot());
            CollectionAssert.AreEqual(profile.Quests.DailyIds, loaded.Quests.DailyIds);
            CollectionAssert.AreEqual(profile.Quests.DailyProgress, loaded.Quests.DailyProgress);
            CollectionAssert.AreEqual(profile.Quests.AchievementProgress, loaded.Quests.AchievementProgress);
            Assert.AreEqual(profile.Quests.ChestsClaimed, loaded.Quests.ChestsClaimed);
            Assert.AreEqual("2026-11-18", loaded.Quests.Day);

            var old = new PlayerProfile.Snapshot { version = 4, gold = 120 };
            old.dailyIds = null; // a version 4 file has no quest fields at all
            PlayerProfile fromOld = PlayerProfile.FromSnapshot(old);
            Assert.AreEqual(120, fromOld.Gold);
            Assert.AreEqual("", fromOld.Quests.Day);
            Assert.IsEmpty(fromOld.Quests.DailyIds);
        }

        [Test]
        public void AddGoldAndTotalStarsWork()
        {
            var profile = new PlayerProfile();
            profile.AddGold(40);
            profile.AddGold(-5);
            Assert.AreEqual(40, profile.Gold);
            Assert.AreEqual(0, profile.TotalStars);
        }
    }
}
