using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Flow
{
    public class StageAttemptTests
    {
        private static readonly UnitDef Warrior = ArmyFixtures.UnitDef("warrior", 120, 12, 1f, 1f, 1.2f);
        private static readonly UnitDef Archer = ArmyFixtures.UnitDef("archer", 60, 10, 0.8f, 4f, 1f);
        private static readonly UnitDef Mage = ArmyFixtures.UnitDef("mage", 55, 22, 1.6f, 3.5f, 0.9f);
        private static readonly UnitDef Grunt = ArmyFixtures.UnitDef("goblin_grunt", 90, 10, 1f, 1f, 1.2f);

        private static StageDef Stage()
        {
            var stage = new StageDef { Id = "stage_test", Moves = 12, GoldReward = 30 };
            for (int x = 0; x < 3; x++)
            {
                stage.Enemies.Add(new EnemySlot { Unit = Grunt, Cell = new GridPos(x, 0) });
            }
            return stage;
        }

        private static StageRun Run(ulong seed) => new StageRun(
            Stage(),
            new[] { Warrior, Archer, Mage },
            new PuzzleRules(),
            new ArmyRules(),
            new BattleRules(),
            new ProgressionRules(),
            new SeededRandom(seed));

        /// <summary>A run with its puzzle played by always taking the first valid move.</summary>
        private static StageRun PlayedRun(ulong seed)
        {
            StageRun run = Run(seed);
            while (!run.Puzzle.IsFinished)
            {
                Assert.IsTrue(run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                run.Puzzle.PlaySwap(a, b);
            }
            return run;
        }

        private static void Fight(StageRun run, bool useRally)
        {
            Assert.IsTrue(run.StartBattle());
            if (useRally)
            {
                Assert.IsTrue(run.Battle.ActivateRally());
            }
            run.Battle.RunToEnd();
            Assert.IsNotNull(run.Finish());
        }

        [Test]
        public void NullUntilTheBattleIsOver()
        {
            StageRun run = PlayedRun(3);
            Assert.IsNull(StageAttempt.From(run), "War Camp");
            AutoFormation.Fill(run.Roster);
            run.StartBattle();
            Assert.IsNull(StageAttempt.From(run), "battle running");
            Assert.IsNull(StageAttempt.From(null));
        }

        [Test]
        public void CopiesTheOutcomeOfTheRun()
        {
            StageRun run = PlayedRun(3);
            AutoFormation.Fill(run.Roster);
            int bench = run.Roster.Bench.Count;
            Fight(run, false);

            StageAttempt attempt = StageAttempt.From(run);
            BattleResult result = run.Battle.Result;
            Assert.AreEqual("stage_test", attempt.stage);
            Assert.AreEqual(result.Victory, attempt.victory);
            Assert.AreEqual(result.TimedOut, attempt.timedOut);
            Assert.AreEqual(run.Reward.Stars, attempt.stars);
            Assert.AreEqual(result.Ticks / 20f, attempt.battleSeconds, 1e-4);
            Assert.AreEqual(result.Deployed, attempt.deployed);
            Assert.AreEqual(result.Survivors, attempt.survivors);
            Assert.AreEqual(bench, attempt.dismissed);
            Assert.AreEqual(run.Reward.TotalGold, attempt.gold);
            Assert.AreEqual(0, attempt.merges, "nothing was merged");
            Assert.AreEqual(0, attempt.rallyUsed);
            Assert.AreEqual(run.RallyCharges, attempt.rallyEarned);
        }

        [Test]
        public void RecruitedCountsEveryClassInGemOrder()
        {
            StageRun run = PlayedRun(3);
            AutoFormation.Fill(run.Roster);
            Fight(run, false);

            StageAttempt attempt = StageAttempt.From(run);
            int w = run.Puzzle.RewardCount("warrior");
            int a = run.Puzzle.RewardCount("archer");
            int m = run.Puzzle.RewardCount("mage");
            Assert.AreEqual(w + a + m, attempt.recruited);
            Assert.AreEqual($"warrior {w}, archer {a}, mage {m}", attempt.recruitedByClass);
            Assert.AreEqual(attempt.recruited, attempt.deployed + attempt.dismissed);
        }

        [Test]
        public void CountsMergesFromTheUnitsThatDisappeared()
        {
            // Find a seed whose puzzle recruits enough copies of one kind to merge.
            StageRun run = null;
            for (ulong seed = 1; seed < 200 && run == null; seed++)
            {
                StageRun candidate = PlayedRun(seed);
                if (candidate.Roster.CanMergeAny)
                {
                    run = candidate;
                }
            }
            Assert.IsNotNull(run, "some seed recruits three of a kind");

            int unitsBefore = run.Roster.AllUnits().Count;
            run.Roster.MergeAll();
            int expectedMerges = (unitsBefore - run.Roster.AllUnits().Count) / 2;
            Assert.Greater(expectedMerges, 0);
            AutoFormation.Fill(run.Roster);
            Fight(run, false);

            Assert.AreEqual(expectedMerges, StageAttempt.From(run).merges);
        }

        [Test]
        public void CountsRallyChargesUsed()
        {
            StageRun run = null;
            for (ulong seed = 1; seed < 200 && run == null; seed++)
            {
                StageRun candidate = PlayedRun(seed);
                if (candidate.RallyCharges > 0)
                {
                    run = candidate;
                }
            }
            Assert.IsNotNull(run, "some seed earns a Rally charge");
            AutoFormation.Fill(run.Roster);
            Fight(run, true);

            StageAttempt attempt = StageAttempt.From(run);
            Assert.AreEqual(run.RallyCharges, attempt.rallyEarned);
            Assert.AreEqual(1, attempt.rallyUsed);
        }

        [Test]
        public void DescribesTheFormationFrontToBack()
        {
            var roster = new Roster(new ArmyRules());
            ArmyUnit[] warriors = roster.Recruit(Warrior, 2).ToArray();
            ArmyUnit archer = roster.Recruit(Archer).Single();
            ArmyUnit mage = roster.Recruit(Mage).Single();
            roster.Deploy(warriors[0], new GridPos(0, 0));
            roster.Deploy(warriors[1], new GridPos(3, 0));
            roster.Deploy(mage, new GridPos(1, 1));
            roster.Deploy(archer, new GridPos(2, 3));

            Assert.AreEqual("W1 . . W1 | . M1 . . | . . . . | . . A1 .",
                StageAttempt.Describe(roster.Formation));
        }

        [Test]
        public void DescribeShowsTheTier()
        {
            var roster = new Roster(new ArmyRules());
            roster.Recruit(Archer, 3);
            ArmyUnit merged = roster.MergeAll().Single();
            roster.Deploy(merged, new GridPos(1, 0));
            StringAssert.StartsWith(". A2 . .", StageAttempt.Describe(roster.Formation));
        }
    }
}
