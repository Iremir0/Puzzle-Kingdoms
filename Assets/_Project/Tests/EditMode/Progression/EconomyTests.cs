using NUnit.Framework;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Quests;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>PB-38 (D-071): first wins pay full gold, replays pay part; chests come every 12 stars; vibration is a setting.</summary>
    public class EconomyTests
    {
        private static readonly StageDef Stage = new StageDef { Id = "stage_1_1", GoldReward = 80 };
        private static readonly BattleResult Won = new BattleResult { Victory = true, Deployed = 4, Survivors = 4 };

        [Test]
        public void AFirstWinPaysTheWholeStageGold()
        {
            StageReward reward = StageReward.Compute(Stage, Won, 2, 3, new ProgressionRules());
            Assert.AreEqual(80, reward.VictoryGold);
            Assert.IsFalse(reward.Replay);
        }

        [Test]
        public void AReplayPaysTheReplayShareButKeepsTheOtherGold()
        {
            var rules = new ProgressionRules();
            StageReward reward = StageReward.Compute(Stage, Won, 2, 3, rules, replay: true);
            Assert.AreEqual(40, reward.VictoryGold, "half of 80");
            Assert.AreEqual(2 * rules.DismissGoldPerUnit, reward.DismissGold, "dismissal is still paid in full");
            Assert.AreEqual(3, reward.OverflowGold);
            Assert.AreEqual(3, reward.Stars, "stars are not cut");
            Assert.IsTrue(reward.Replay);
        }

        [Test]
        public void ALostReplayPaysNoVictoryGold()
        {
            var lost = new BattleResult { Victory = false, Deployed = 4, Survivors = 0 };
            Assert.AreEqual(0, StageReward.Compute(Stage, lost, 0, 0, new ProgressionRules(), replay: true).VictoryGold);
        }

        [Test]
        public void AChestEveryTwelveStars()
        {
            var rules = new QuestRules();
            Assert.AreEqual(12, rules.StarsPerChest);
            Assert.AreEqual(3, 42 / rules.StarsPerChest, "the 14-stage campaign has three chests");
        }

        [Test]
        public void VibrationIsOnByDefaultAndCopied()
        {
            var settings = new GameSettings();
            Assert.IsTrue(settings.haptics);
            settings.haptics = false;
            Assert.IsFalse(settings.Copy().haptics);
        }
    }
}
