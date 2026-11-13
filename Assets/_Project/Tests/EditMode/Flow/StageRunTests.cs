using System.Collections.Generic;
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
    public class StageRunTests
    {
        private static readonly UnitDef Warrior = ArmyFixtures.UnitDef("warrior", 120, 12, 1f, 1f, 1.2f);
        private static readonly UnitDef Archer = ArmyFixtures.UnitDef("archer", 60, 10, 0.8f, 4f, 1f);
        private static readonly UnitDef Mage = ArmyFixtures.UnitDef("mage", 55, 22, 1.6f, 3.5f, 0.9f);
        private static readonly UnitDef Grunt = ArmyFixtures.UnitDef("goblin_grunt", 90, 10, 1f, 1f, 1.2f);

        private static StageDef Stage(int moves = 12)
        {
            var stage = new StageDef { Id = "stage_test", Moves = moves, GoldReward = 30 };
            for (int x = 0; x < 3; x++)
            {
                stage.Enemies.Add(new EnemySlot { Unit = Grunt, Cell = new GridPos(x, 0) });
            }
            return stage;
        }

        private static StageRun Run(StageDef stage, ulong seed = 3) => new StageRun(
            stage,
            new[] { Warrior, Archer, Mage },
            new PuzzleRules(),
            new ArmyRules(),
            new BattleRules(),
            new ProgressionRules(),
            new SeededRandom(seed));

        private static void PlayPuzzle(StageRun run)
        {
            while (!run.Puzzle.IsFinished)
            {
                Assert.IsTrue(run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                run.Puzzle.PlaySwap(a, b);
            }
        }

        [Test]
        public void PuzzleRewardsBecomeBenchUnits()
        {
            StageRun run = Run(Stage());
            var recruited = new List<ArmyUnit>();
            run.UnitRecruited += recruited.Add;
            PlayPuzzle(run);
            int expected = run.Puzzle.RewardCount("warrior") + run.Puzzle.RewardCount("archer") + run.Puzzle.RewardCount("mage");
            Assert.Greater(expected, 0, "a 12-move puzzle recruits someone");
            Assert.AreEqual(expected, run.Roster.Bench.Count);
            Assert.AreEqual(expected, recruited.Count);
        }

        [Test]
        public void PhasesAdvanceInOrder()
        {
            StageRun run = Run(Stage());
            var phases = new List<StagePhase>();
            run.PhaseChanged += phases.Add;

            Assert.AreEqual(StagePhase.Puzzle, run.Phase);
            Assert.IsFalse(run.StartBattle(), "no battle during the puzzle");
            PlayPuzzle(run);
            Assert.AreEqual(StagePhase.WarCamp, run.Phase);
            Assert.IsFalse(run.StartBattle(), "no battle with an empty formation");

            AutoFormation.Fill(run.Roster);
            int bench = run.Roster.Bench.Count;
            Assert.IsTrue(run.StartBattle());
            Assert.AreEqual(bench, run.DismissedUnits);
            Assert.IsNull(run.Finish(), "battle not over yet");

            run.Battle.RunToEnd();
            StageReward reward = run.Finish();
            Assert.IsNotNull(reward);
            Assert.AreEqual(StagePhase.Finished, run.Phase);
            CollectionAssert.AreEqual(
                new[] { StagePhase.WarCamp, StagePhase.Battle, StagePhase.Finished }, phases);
            Assert.AreEqual(bench * 5, reward.DismissGold);
        }

        [Test]
        public void BattleGetsTheRallyChargesEarnedOnTheBoard()
        {
            StageRun run = Run(Stage());
            PlayPuzzle(run);
            AutoFormation.Fill(run.Roster);
            run.StartBattle();
            Assert.AreEqual(run.RallyCharges, run.Battle.Rally.Charges);
            Assert.LessOrEqual(run.RallyCharges, 3);
        }

        [Test]
        public void SameSeedSameRun()
        {
            var outcomes = new List<string>();
            for (int i = 0; i < 2; i++)
            {
                StageRun run = Run(Stage(), 77);
                PlayPuzzle(run);
                AutoFormation.Fill(run.Roster);
                run.StartBattle();
                run.Battle.RunToEnd();
                StageReward reward = run.Finish();
                outcomes.Add($"{run.Roster.AllUnits().Count}/{run.RallyCharges}/{run.Battle.Result.Ticks}/{reward.TotalGold}/{reward.Stars}");
            }
            Assert.AreEqual(outcomes[0], outcomes[1]);
        }

        [Test]
        public void RallyIdIsReserved()
        {
            UnitDef bad = ArmyFixtures.UnitDef(StageRun.RallyKey);
            Assert.Throws<System.ArgumentException>(() => new StageRun(Stage(), new[] { Warrior, Archer, bad },
                new PuzzleRules(), new ArmyRules(), new BattleRules(), new ProgressionRules(), new SeededRandom(1)));
        }
    }
}
