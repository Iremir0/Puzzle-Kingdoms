using NUnit.Framework;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Tests.Progression
{
    public class ProgressionTests
    {
        private static readonly string[] Campaign = { "stage_1_1", "stage_1_2", "stage_1_3" };

        private static StageDef Stage(int gold = 30) => new StageDef { Id = "stage_1_1", GoldReward = gold };

        private static BattleResult Result(bool victory, int deployed = 4, int survivors = 4) =>
            new BattleResult { Victory = victory, Deployed = deployed, Survivors = survivors };

        [Test]
        public void VictoryPaysStageGoldDismissalAndOverflow()
        {
            StageReward reward = StageReward.Compute(Stage(30), Result(true), 2, 4, new ProgressionRules());
            Assert.AreEqual(3, reward.Stars);
            Assert.AreEqual(30, reward.VictoryGold);
            Assert.AreEqual(10, reward.DismissGold, "2 units x 5 gold");
            Assert.AreEqual(4, reward.OverflowGold, "4 points x 1 gold");
            Assert.AreEqual(44, reward.TotalGold);
        }

        [Test]
        public void DefeatPaysNoStageGoldAndNoStars()
        {
            StageReward reward = StageReward.Compute(Stage(30), Result(false, 4, 0), 1, 0, new ProgressionRules());
            Assert.AreEqual(0, reward.Stars);
            Assert.AreEqual(0, reward.VictoryGold);
            Assert.AreEqual(5, reward.TotalGold, "dismissal gold is kept");
        }

        [Test]
        public void FirstStageIsUnlockedAndNextUnlocksAfterAStar()
        {
            var profile = new PlayerProfile();
            Assert.IsTrue(profile.IsUnlocked(Campaign, 0));
            Assert.IsFalse(profile.IsUnlocked(Campaign, 1));
            profile.Apply("stage_1_1", new StageReward { Stars = 1 });
            Assert.IsTrue(profile.IsUnlocked(Campaign, 1));
            Assert.IsFalse(profile.IsUnlocked(Campaign, 2));
            Assert.IsFalse(profile.IsUnlocked(Campaign, 3), "out of range");
        }

        [Test]
        public void ProfileKeepsBestStarsAndSumsGold()
        {
            var profile = new PlayerProfile();
            profile.Apply("stage_1_1", new StageReward { Stars = 2, VictoryGold = 30 });
            profile.Apply("stage_1_1", new StageReward { Stars = 1, VictoryGold = 30 });
            Assert.AreEqual(2, profile.BestStars("stage_1_1"));
            Assert.AreEqual(60, profile.Gold);
            Assert.AreEqual(0, profile.BestStars("stage_1_2"));
        }

        [Test]
        public void SnapshotRoundTripsProfile()
        {
            var profile = new PlayerProfile();
            profile.Apply("stage_1_1", new StageReward { Stars = 3, VictoryGold = 30 });
            profile.Apply("stage_1_2", new StageReward { Stars = 1, VictoryGold = 40, DismissGold = 5 });
            PlayerProfile restored = PlayerProfile.FromSnapshot(profile.ToSnapshot());
            Assert.AreEqual(75, restored.Gold);
            Assert.AreEqual(3, restored.BestStars("stage_1_1"));
            Assert.AreEqual(1, restored.BestStars("stage_1_2"));
        }

        [Test]
        public void SnapshotLoadingClampsBadValuesAndRejectsNewerVersions()
        {
            var snapshot = new PlayerProfile.Snapshot { gold = -5 };
            snapshot.stageIds.Add("stage_1_1");
            snapshot.stageStars.Add(9);
            PlayerProfile profile = PlayerProfile.FromSnapshot(snapshot);
            Assert.AreEqual(0, profile.Gold);
            Assert.AreEqual(3, profile.BestStars("stage_1_1"));
            Assert.AreEqual(0, PlayerProfile.FromSnapshot(null).Gold, "missing save = new profile");

            snapshot.version = PlayerProfile.CurrentVersion + 1;
            Assert.Throws<System.InvalidOperationException>(() => PlayerProfile.FromSnapshot(snapshot));
        }
    }
}
