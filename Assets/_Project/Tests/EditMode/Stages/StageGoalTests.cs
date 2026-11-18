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

namespace PuzzleKingdoms.Tests.Stages
{
    /// <summary>PB-19: stage goals, hand-placed boards, and the shared balance runner.</summary>
    public class StageGoalTests
    {
        private static readonly UnitDef Warrior = ArmyFixtures.UnitDef("warrior", 120, 12, 1f, 1f, 1.2f);
        private static readonly UnitDef Archer = ArmyFixtures.UnitDef("archer", 60, 10, 0.8f, 4f, 1f);
        private static readonly UnitDef Mage = ArmyFixtures.UnitDef("mage", 55, 22, 1.6f, 3.5f, 0.9f);
        private static readonly UnitDef WeakGrunt = ArmyFixtures.UnitDef("goblin_grunt", 1, 1, 1f, 1f, 1.2f);

        private static StageDef Stage(StageGoal goal = null, string[] layout = null, int stones = 0)
        {
            var stage = new StageDef { Id = "stage_goal", Moves = 12, GoldReward = 30, StoneGems = stones, Layout = layout };
            stage.Goal = goal ?? StageGoal.DefeatArmy();
            stage.Enemies.Add(new EnemySlot { Unit = WeakGrunt, Cell = new GridPos(1, 0) });
            return stage;
        }

        private static StageRun Run(StageDef stage, ulong seed = 5) => new StageRun(stage, new[] { Warrior, Archer, Mage },
            new PuzzleRules(), new ArmyRules(), new BattleRules(), new ProgressionRules(), new SeededRandom(seed));

        /// <summary>Plays the puzzle with the first valid move each time; returns the stones the results cracked.</summary>
        private static int PlayPuzzle(StageRun run)
        {
            int cracked = 0;
            while (!run.Puzzle.IsFinished)
            {
                Assert.IsTrue(run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                foreach (MoveResult.CascadeStep step in run.Puzzle.PlaySwap(a, b).Steps)
                {
                    cracked += step.Cracked.Count;
                }
            }
            return cracked;
        }

        private static StageRun PlayToEnd(StageDef stage)
        {
            StageRun run = Run(stage);
            PlayPuzzle(run);
            AutoFormation.Fill(run.Roster);
            Assert.IsTrue(run.StartBattle());
            while (!run.Battle.IsFinished)
            {
                run.Battle.Step();
            }
            Assert.IsNotNull(run.Finish());
            return run;
        }

        // ---- Goals ----

        [Test]
        public void GoalsNeedTheBattleWonAndTheirCondition()
        {
            var won = new BattleResult { Victory = true, Ticks = 200, Deployed = 4, Survivors = 4 };
            var lost = new BattleResult { Victory = false, Ticks = 200, Deployed = 4 };
            Assert.IsTrue(StageGoal.DefeatArmy().IsMet(won, 0, 20));
            Assert.IsFalse(StageGoal.DefeatArmy().IsMet(lost, 0, 20));
            Assert.IsFalse(StageGoal.BreakStones(3).IsMet(won, 2, 20));
            Assert.IsTrue(StageGoal.BreakStones(3).IsMet(won, 3, 20));
            Assert.IsFalse(StageGoal.BreakStones(3).IsMet(lost, 9, 20), "stones without the battle do not count");
            Assert.IsTrue(StageGoal.WinWithin(10).IsMet(won, 0, 20), "200 ticks at 20 per second is 10 s");
            won.Ticks = 201;
            Assert.IsFalse(StageGoal.WinWithin(10).IsMet(won, 0, 20));
            Assert.IsFalse(StageGoal.BreakStones(0).IsValid);
            Assert.IsTrue(StageGoal.DefeatArmy().IsValid);
        }

        [Test]
        public void AMissedGoalPaysLikeADefeat()
        {
            var won = new BattleResult { Victory = true, Ticks = 100, Deployed = 4, Survivors = 4 };
            StageReward reward = StageReward.Compute(Stage(), won, 2, 3, new ProgressionRules(), goalMet: false);
            Assert.AreEqual(0, reward.Stars);
            Assert.AreEqual(0, reward.VictoryGold);
            Assert.Greater(reward.DismissGold, 0, "dismissal gold is earned anyway");
            Assert.AreEqual(3, StageReward.Compute(Stage(), won, 0, 0, new ProgressionRules()).Stars);
        }

        [Test]
        public void AStageIsWonOnlyWithItsGoal()
        {
            StageRun plain = PlayToEnd(Stage());
            Assert.IsTrue(plain.Battle.Result.Victory, "a 1 HP enemy is beaten");
            Assert.IsTrue(plain.Won);
            Assert.Greater(plain.Reward.Stars, 0);

            StageRun tooSlow = PlayToEnd(Stage(new StageGoal { Kind = StageGoalKind.WinWithin, Target = 0 }));
            Assert.IsTrue(tooSlow.Battle.Result.Victory);
            Assert.IsFalse(tooSlow.Won, "no battle ends at 0 s");
            Assert.AreEqual(0, tooSlow.Reward.Stars);
            Assert.AreEqual(0, tooSlow.Reward.VictoryGold);
        }

        [Test]
        public void StonesBrokenAddsUpEveryCrack()
        {
            StageRun run = Run(Stage(stones: 6));
            int cracked = PlayPuzzle(run);
            Assert.AreEqual(cracked, run.Puzzle.StonesBroken);
        }

        [Test]
        public void TheStoneGoalCountsTheBoard()
        {
            StageRun run = PlayToEnd(Stage(StageGoal.BreakStones(99), stones: 3));
            Assert.IsTrue(run.Battle.Result.Victory);
            Assert.IsFalse(run.Won, "99 stones can not be broken on this board");
            StageRun easy = PlayToEnd(Stage(StageGoal.BreakStones(1), new[] { "a.....", "......", "......", "......", "......", "......" }));
            Assert.AreEqual(easy.Puzzle.StonesBroken >= 1, easy.Won);
        }

        // ---- Hand-placed boards ----

        private static readonly string[] Layout =
        {
            "0.1.2.",
            "......",
            "..b...",
            "......",
            "x....3",
            "......",
        };

        [Test]
        public void LayoutReadsGemsAndStones()
        {
            Assert.IsTrue(BoardLayout.TryParse(Layout, 6, 6, 4, out int[,] gems, out bool[,] stones, out string error), error);
            Assert.AreEqual(0, gems[0, 0]);
            Assert.AreEqual(1, gems[2, 0]);
            Assert.AreEqual(BoardLayout.Random, gems[1, 0]);
            Assert.AreEqual(1, gems[2, 2]);
            Assert.IsTrue(stones[2, 2], "b = gem 1 in stone");
            Assert.AreEqual(BoardLayout.Random, gems[0, 4]);
            Assert.IsTrue(stones[0, 4], "x = a random gem in stone");
            CollectionAssert.AreEqual(Layout, BoardLayout.Format(gems, stones), "formats back to the same rows");
        }

        [TestCase(new[] { "......" }, "6 rows")]
        [TestCase(new[] { "......", "......", "......", "......", "......", "....." }, "needs 6 cells")]
        [TestCase(new[] { "?.....", "......", "......", "......", "......", "......" }, "unknown")]
        [TestCase(new[] { "7.....", "......", "......", "......", "......", "......" }, "gem type 7")]
        [TestCase(new[] { "ax....", "......", "......", "......", "......", "......" }, "may not touch")]
        [TestCase(new[] { "000...", "......", "......", "......", "......", "......" }, "already match")]
        [TestCase(new[] { "11....", "11....", "......", "......", "......", "......" }, "already match")]
        public void BadLayoutsAreExplained(string[] rows, string because)
        {
            Assert.IsFalse(BoardLayout.TryParse(rows, 6, 6, 4, out _, out _, out string error));
            StringAssert.Contains(because, error);
        }

        [Test]
        public void EmptyLayoutMeansARandomBoard()
        {
            Assert.IsTrue(BoardLayout.IsEmpty(null));
            Assert.IsTrue(BoardLayout.IsEmpty(new[] { "......", "  ...." }));
            Assert.IsFalse(BoardLayout.IsEmpty(Layout));
        }

        [Test]
        public void AStageStartsFromItsLayoutWithoutMatchesAndWithAMove()
        {
            for (ulong seed = 1; seed <= 25; seed++)
            {
                StageRun run = Run(Stage(layout: Layout), seed);
                BoardModel board = run.Puzzle.Board;
                Assert.AreEqual(0, board.GetGem(new GridPos(0, 0)), $"seed {seed}");
                Assert.AreEqual(2, board.GetGem(new GridPos(4, 0)));
                Assert.AreEqual(3, board.GetGem(new GridPos(5, 4)));
                Assert.IsTrue(board.IsStone(new GridPos(2, 2)));
                Assert.IsTrue(board.IsStone(new GridPos(0, 4)));
                CollectionAssert.IsEmpty(board.FindMatchGroups(), $"seed {seed}: no match at the start");
                Assert.IsTrue(board.HasValidMove(), $"seed {seed}");
            }
        }

        [Test]
        public void ABrokenLayoutStopsTheStageWithItsReason()
        {
            var ex = Assert.Throws<System.ArgumentException>(() =>
                Run(Stage(layout: new[] { "000...", "......", "......", "......", "......", "......" })));
            StringAssert.Contains("stage_goal", ex.Message);
        }

        // ---- Balance runner ----

        [Test]
        public void BalanceRunsAreRepeatable()
        {
            StageDef stage = Stage();
            BalanceStats first = BalanceRunner.Run(seed => Run(stage, seed), PuzzleBot.Greedy, RallyBot.Eager, 6);
            BalanceStats second = BalanceRunner.Run(seed => Run(stage, seed), PuzzleBot.Greedy, RallyBot.Eager, 6);
            Assert.AreEqual(6, first.Runs);
            Assert.AreEqual(first.Wins, second.Wins);
            CollectionAssert.AreEqual(first.Stars, second.Stars);
            Assert.AreEqual(first.Seconds, second.Seconds, 1e-9);
            Assert.AreEqual(6, first.Wins, "a 1 HP enemy loses every time");
            Assert.AreEqual(1.0, first.WinRate, 1e-9);
        }

        [Test]
        public void BalanceCountsMissedGoals()
        {
            StageDef stage = Stage(StageGoal.BreakStones(99), stones: 2);
            BalanceStats stats = BalanceRunner.Run(seed => Run(stage, seed), PuzzleBot.Random, RallyBot.None, 4);
            Assert.AreEqual(0, stats.Wins);
            Assert.AreEqual(4, stats.GoalMissed);
            Assert.AreEqual(4, stats.Stars[0]);
        }
    }
}
