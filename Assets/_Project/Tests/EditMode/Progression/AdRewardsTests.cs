using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Progression
{
    /// <summary>PB-30 (D-064): rewarded ads (gold, extra moves, extra Rally) and the Supporter pack.</summary>
    public class AdRewardsTests
    {
        private static readonly UnitDef Warrior = ArmyFixtures.UnitDef("warrior", 260, 24, 1f, 1f, 1.2f);
        private static readonly UnitDef Archer = ArmyFixtures.UnitDef("archer", 90, 20, 0.8f, 4f, 1f);
        private static readonly UnitDef Mage = ArmyFixtures.UnitDef("mage", 80, 36, 1.6f, 3.5f, 0.9f);
        private static readonly UnitDef Grunt = ArmyFixtures.UnitDef("goblin_grunt", 60, 8, 1f, 1f, 1.2f);

        private static StageRun NewRun(int moves = 6)
        {
            var stage = new StageDef { Id = "ad_test", Moves = moves };
            stage.Enemies.Add(new EnemySlot { Unit = Grunt, Cell = new GridPos(1, 0) });
            return new StageRun(stage, new[] { Warrior, Archer, Mage }, new PuzzleRules(), new ArmyRules(), new BattleRules(),
                new ProgressionRules(), new SeededRandom(5));
        }

        private static void PlayAllMoves(StageRun run)
        {
            while (!run.Puzzle.IsFinished)
            {
                Assert.IsTrue(run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                run.Puzzle.PlaySwap(a, b);
            }
        }

        [Test]
        public void GoldAdsAreLimitedPerDayAndPayEachTime()
        {
            var profile = new PlayerProfile();
            var rules = new MonetizationRules { BonusGold = 100, GoldAdsPerDay = 3 };

            Assert.AreEqual(3, profile.AdLedger.GoldAdsLeft("2026-11-19", rules));
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(profile.TakeGoldAd("2026-11-19", rules));
            }
            Assert.IsFalse(profile.TakeGoldAd("2026-11-19", rules), "the day's limit");
            Assert.AreEqual(300, profile.Gold);
            Assert.AreEqual(0, profile.AdLedger.GoldAdsLeft("2026-11-19", rules));

            Assert.AreEqual(3, profile.AdLedger.GoldAdsLeft("2026-11-20", rules), "a new day");
            Assert.IsTrue(profile.TakeGoldAd("2026-11-20", rules));
            Assert.AreEqual(2, profile.AdLedger.GoldAdsLeft("2026-11-20", rules));
        }

        [Test]
        public void TheAdStateAndSupporterSurviveTheSaveAndOldSavesHaveNone()
        {
            var profile = new PlayerProfile();
            var rules = new MonetizationRules();
            profile.TakeGoldAd("2026-11-19", rules);
            profile.SetSupporter(true);

            PlayerProfile loaded = PlayerProfile.FromSnapshot(profile.ToSnapshot());

            Assert.IsTrue(loaded.AdLedger.Supporter);
            Assert.AreEqual(rules.GoldAdsPerDay - 1, loaded.AdLedger.GoldAdsLeft("2026-11-19", rules), "today's count is kept");
            Assert.AreEqual(PlayerProfile.CurrentVersion, profile.ToSnapshot().version);

            var old = new PlayerProfile.Snapshot { version = 8, adGoldDay = null };
            PlayerProfile fresh = PlayerProfile.FromSnapshot(old);
            Assert.IsFalse(fresh.AdLedger.Supporter);
            Assert.AreEqual(rules.GoldAdsPerDay, fresh.AdLedger.GoldAdsLeft("2026-11-19", rules));
        }

        [Test]
        public void ExtraMovesReopenThePuzzleOncePerAttempt()
        {
            StageRun run = NewRun(4);
            Assert.IsFalse(run.CanGrantExtraMoves, "not before the moves have run out");
            Assert.IsFalse(run.TryGrantExtraMoves(5));

            PlayAllMoves(run);
            Assert.AreEqual(StagePhase.WarCamp, run.Phase);
            Assert.IsTrue(run.CanGrantExtraMoves);
            int recruited = run.Roster.Bench.Count;

            Assert.IsTrue(run.TryGrantExtraMoves(5));
            Assert.AreEqual(StagePhase.Puzzle, run.Phase);
            Assert.AreEqual(5, run.Puzzle.MovesLeft);
            Assert.IsFalse(run.Puzzle.IsFinished);
            Assert.IsFalse(run.TryGrantExtraMoves(5), "once per attempt");

            PlayAllMoves(run);
            Assert.AreEqual(StagePhase.WarCamp, run.Phase, "the War Camp again after the extra moves");
            Assert.GreaterOrEqual(run.Roster.Bench.Count, recruited, "the army keeps what it had");
            Assert.IsFalse(run.CanGrantExtraMoves);
        }

        [Test]
        public void ExtraRallyAddsACharge_OncePerAttempt_AndNotAfterTheBattleStarted()
        {
            StageRun run = NewRun();
            Assert.IsFalse(run.CanGrantExtraRally, "the puzzle is still on");
            PlayAllMoves(run);
            AutoFormation.Fill(run.Roster);
            int charges = run.RallyCharges;

            Assert.IsTrue(run.TryGrantExtraRally(1));
            Assert.IsFalse(run.TryGrantExtraRally(1), "once per attempt");
            Assert.IsTrue(run.StartBattle());
            Assert.AreEqual(charges + 1, run.Battle.Rally.Charges);

            StageRun other = NewRun();
            PlayAllMoves(other);
            AutoFormation.Fill(other.Roster);
            other.StartBattle();
            Assert.IsFalse(other.TryGrantExtraRally(1), "too late");
        }
    }
}
