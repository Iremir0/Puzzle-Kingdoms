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

namespace PuzzleKingdoms.Tests.Puzzle
{
    /// <summary>
    /// PB-20: chains, ice, special gem combos and the boss dropping stones. Boards use gem type 0 for the
    /// shapes under test; the filler pattern (types 1-3) has no run and no square (as in StoneGemTests).
    /// </summary>
    public class ObstacleTests
    {
        private static int Filler(int x, int y) => 1 + (x + 2 * y) % 3;

        private static BoardModel MakeBoard(params (int x, int y)[] zeros)
        {
            var board = new BoardModel(6, 6, 4, new SeededRandom(5));
            var rows = new int[6][];
            for (int y = 0; y < 6; y++)
            {
                rows[y] = new int[6];
                for (int x = 0; x < 6; x++)
                {
                    rows[y][x] = Filler(x, y);
                }
            }
            foreach ((int x, int y) in zeros)
            {
                rows[y][x] = 0;
            }
            board.LoadRows(rows);
            Assert.IsEmpty(board.FindMatchGroups(), "precondition: no match before the swap");
            return board;
        }

        private static readonly GridPos A = new GridPos(2, 0);
        private static readonly GridPos B = new GridPos(2, 1);

        /// <summary>Zeros at (0,0), (1,0) and (2,1): swapping (2,0) with (2,1) makes a row of three in row 0.</summary>
        private static BoardModel RowOfThreeBoard() => MakeBoard((0, 0), (1, 0), (2, 1));

        // ---- Chains ----

        [Test]
        public void AChainedGemCanNotBeSwapped()
        {
            BoardModel board = RowOfThreeBoard();
            Assert.IsTrue(board.WouldSwapMatch(A, B), "precondition");
            board.SetChain(B, true);
            Assert.IsFalse(board.WouldSwapMatch(A, B));
            Assert.IsFalse(board.TrySwap(A, B).IsValid);
        }

        [Test]
        public void AMatchBreaksTheChainAndTheGemStays()
        {
            BoardModel board = RowOfThreeBoard();
            var chained = new GridPos(0, 0);
            board.SetChain(chained, true);
            MoveResult result = board.TrySwap(A, B);
            Assert.IsTrue(result.IsValid);
            MoveResult.CascadeStep first = result.Steps[0];
            CollectionAssert.Contains(first.Unchained, chained);
            CollectionAssert.DoesNotContain(first.Cleared, chained, "the chained gem stays on the board");
            Assert.AreEqual(2, first.Cleared.Count);
            Assert.IsFalse(board.IsChained(chained));
            Assert.AreEqual(0, board.GetGem(chained), "same gem, now free");
        }

        [Test]
        public void ChainsFallWithTheirGemAndReplayKeepsThem()
        {
            // Zeros at (0,2), (1,2), (3,2): swapping (2,2) and (3,2) clears row 2's first three cells.
            BoardModel board = MakeBoard((0, 2), (1, 2), (3, 2));
            board.SetChain(new GridPos(0, 1), true);
            board.SetIce(new GridPos(0, 1), 1);
            board.SetIce(new GridPos(0, 2), 1);
            int[,] before = board.ToGrid();
            bool[,] chainsBefore = board.ToChainGrid();
            int[,] iceBefore = board.ToIceGrid();
            MoveResult result = board.TrySwap(new GridPos(2, 2), new GridPos(3, 2));
            Assert.IsTrue(board.IsChained(new GridPos(0, 2)), "the chained gem fell one cell");
            Assert.IsFalse(board.IsChained(new GridPos(0, 1)));
            Assert.AreEqual(1, board.IceAt(new GridPos(0, 1)), "ice stays on its cell");
            Assert.AreEqual(0, board.IceAt(new GridPos(0, 2)), "the cleared cell's ice melted");

            MoveReplay.Frame filled = MoveReplay.Frames(before, result, null, null, chainsBefore, iceBefore)
                .First(f => f.Kind == MoveReplay.FrameKind.Filled && f.StepIndex == 0);
            Assert.IsTrue(filled.Chains[0, 2]);
            Assert.IsFalse(filled.Chains[0, 1]);
            Assert.AreEqual(1, filled.Ice[0, 1]);
            Assert.AreEqual(0, filled.Ice[0, 2]);
        }

        // ---- Ice ----

        [Test]
        public void IceMeltsOneLayerPerHit()
        {
            BoardModel board = RowOfThreeBoard();
            board.SetIce(new GridPos(1, 0), 2);
            board.SetIce(new GridPos(0, 0), 1);
            board.SetIce(new GridPos(5, 5), 1);
            Assert.AreEqual(4, board.IceLeft);
            MoveResult result = board.TrySwap(A, B);
            Assert.AreEqual(1, board.IceAt(new GridPos(1, 0)), "two layers: one left");
            Assert.AreEqual(0, board.IceAt(new GridPos(0, 0)));
            Assert.AreEqual(1, board.IceAt(new GridPos(5, 5)), "not hit");
            CollectionAssert.IsSubsetOf(new[] { new GridPos(0, 0), new GridPos(1, 0) }, result.Steps[0].IceMelted);
            Assert.AreEqual(2, board.IceLeft);
        }

        [Test]
        public void BreakingAChainMeltsTheIceUnderIt()
        {
            BoardModel board = RowOfThreeBoard();
            board.SetChain(new GridPos(0, 0), true);
            board.SetIce(new GridPos(0, 0), 1);
            board.TrySwap(A, B);
            Assert.AreEqual(0, board.IceAt(new GridPos(0, 0)));
        }

        [Test]
        public void IceIsLimitedToTwoLayers()
        {
            BoardModel board = RowOfThreeBoard();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => board.SetIce(A, 3));
        }

        // ---- Combos ----

        [Test]
        public void TwoSpecialGemsAlwaysSwap()
        {
            BoardModel board = MakeBoard();
            board.SetSpecial(new GridPos(2, 2), SpecialKind.Blast);
            board.SetSpecial(new GridPos(3, 2), SpecialKind.Blast);
            Assert.IsTrue(board.WouldSwapMatch(new GridPos(2, 2), new GridPos(3, 2)));
        }

        [Test]
        public void BlastPlusBlastHitsFiveByFive()
        {
            BoardModel board = MakeBoard();
            board.SetSpecial(new GridPos(2, 2), SpecialKind.Blast);
            board.SetSpecial(new GridPos(3, 2), SpecialKind.Blast);
            MoveResult result = board.TrySwap(new GridPos(2, 2), new GridPos(3, 2));
            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(ComboKind.BlastBlast, first.Combo);
            Assert.AreEqual(25, first.Cleared.Count, "x 1-5, y 0-4 around (3,2)");
            Assert.IsTrue(first.Cleared.All(c => c.X >= 1 && c.X <= 5 && c.Y >= 0 && c.Y <= 4));
        }

        [Test]
        public void PrismPlusPrismHitsTheWholeBoard()
        {
            BoardModel board = MakeBoard();
            board.SetSpecial(new GridPos(0, 0), SpecialKind.Prism);
            board.SetSpecial(new GridPos(1, 0), SpecialKind.Prism);
            board.SetStone(new GridPos(5, 5), true);
            MoveResult result = board.TrySwap(new GridPos(0, 0), new GridPos(1, 0));
            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(ComboKind.PrismPrism, first.Combo);
            Assert.AreEqual(35, first.Cleared.Count, "every cell but the stone");
            CollectionAssert.Contains(first.Cracked, new GridPos(5, 5));
        }

        [Test]
        public void PrismPlusBlastSetsOffEveryGemOfTheBlastsColour()
        {
            BoardModel board = MakeBoard();
            var prism = new GridPos(0, 0);
            var blast = new GridPos(1, 0);
            board.SetSpecial(prism, SpecialKind.Prism);
            board.SetSpecial(blast, SpecialKind.Blast);
            int colour = board.GetGem(blast);
            int[,] grid = board.ToGrid();
            int sameColour = grid.Cast<int>().Count(g => g == colour);
            MoveResult result = board.TrySwap(prism, blast);
            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(ComboKind.PrismBlast, first.Combo);
            Assert.GreaterOrEqual(first.Triggers.Count(t => t.Kind == SpecialKind.Blast), sameColour - 1,
                "every gem of the colour went off as a Blast");
            Assert.Greater(first.Cleared.Count, sameColour, "their blasts cleared more than the colour itself");
        }

        // ---- Covers on a stage, boss stones, the ice goal ----

        private static readonly UnitDef Warrior = ArmyFixtures.UnitDef("warrior", 120, 12, 1f, 1f, 1.2f);
        private static readonly UnitDef Archer = ArmyFixtures.UnitDef("archer", 60, 10, 0.8f, 4f, 1f);
        private static readonly UnitDef Mage = ArmyFixtures.UnitDef("mage", 55, 22, 1.6f, 3.5f, 0.9f);

        private static StageRun Run(StageDef stage, ulong seed = 9) => new StageRun(stage, new[] { Warrior, Archer, Mage },
            new PuzzleRules(), new ArmyRules(), new BattleRules(), new ProgressionRules(), new SeededRandom(seed));

        private static StageDef Stage() => new StageDef { Id = "stage_ice", Moves = 12 };

        [Test]
        public void CoversReadAndWriteBack()
        {
            string[] rows = { "c.....", "......", "..i...", "...I..", "....k.", ".....K" };
            Assert.IsTrue(BoardLayout.TryParseCovers(rows, 6, 6, out int[,] ice, out bool[,] chains, out string error), error);
            Assert.IsTrue(chains[0, 0]);
            Assert.AreEqual(1, ice[2, 2]);
            Assert.AreEqual(2, ice[3, 3]);
            Assert.IsTrue(chains[4, 4] && ice[4, 4] == 1);
            Assert.IsTrue(chains[5, 5] && ice[5, 5] == 2);
            CollectionAssert.AreEqual(rows, BoardLayout.FormatCovers(ice, chains));
            Assert.IsNull(BoardLayout.FormatCovers(new int[6, 6], new bool[6, 6]), "no covers: no rows");
            Assert.IsTrue(BoardLayout.TryParseCovers(null, 6, 6, out _, out _, out _));
            Assert.IsFalse(BoardLayout.TryParseCovers(new[] { "z....." , "......", "......", "......", "......", "......" },
                6, 6, out _, out _, out string bad));
            StringAssert.Contains("unknown", bad);
        }

        [Test]
        public void AStageStartsWithItsCovers()
        {
            StageDef stage = Stage();
            stage.Covers = new[] { "c.....", "......", "......", "......", "......", ".....I" };
            for (ulong seed = 1; seed <= 15; seed++)
            {
                StageRun run = Run(stage, seed);
                Assert.IsTrue(run.Puzzle.Board.IsChained(new GridPos(0, 0)), $"seed {seed}");
                Assert.AreEqual(2, run.Puzzle.Board.IceAt(new GridPos(5, 5)));
                Assert.AreEqual(2, run.Puzzle.IceAtStart);
                Assert.IsTrue(run.Puzzle.Board.HasValidMove());
            }
        }

        [Test]
        public void TheBossDropsStonesOnSchedule()
        {
            StageDef stage = Stage();
            stage.BossStonesEvery = 2;
            stage.BossStonesCount = 2;
            StageRun run = Run(stage);
            PuzzleSession puzzle = run.Puzzle;
            Assert.AreEqual(2, puzzle.MovesUntilBossStones);
            Assert.IsTrue(puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
            MoveResult first = puzzle.PlaySwap(a, b);
            CollectionAssert.IsEmpty(first.StonesDropped);
            Assert.AreEqual(1, puzzle.MovesUntilBossStones);
            int[,] before = puzzle.Board.ToGrid();
            Assert.IsTrue(puzzle.Board.TryFindValidMove(out a, out b));
            MoveResult second = puzzle.PlaySwap(a, b);
            Assert.AreEqual(2, second.StonesDropped.Count, "two stones after the second move");
            foreach (GridPos cell in second.StonesDropped)
            {
                Assert.IsTrue(puzzle.Board.IsStone(cell));
            }
            Assert.AreEqual(2, puzzle.MovesUntilBossStones, "the countdown starts again");
            MoveReplay.Frame last = MoveReplay.Frames(before, second).Last();
            Assert.AreEqual(MoveReplay.FrameKind.StonesDropped, last.Kind);
            Assert.IsTrue(last.Stones[second.StonesDropped[0].X, second.StonesDropped[0].Y]);
        }

        [Test]
        public void NoBossMeansNoStonesAndNoCountdown()
        {
            StageRun run = Run(Stage());
            Assert.AreEqual(-1, run.Puzzle.MovesUntilBossStones);
            Assert.IsTrue(run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
            CollectionAssert.IsEmpty(run.Puzzle.PlaySwap(a, b).StonesDropped);
        }

        [Test]
        public void TheIceGoalNeedsEveryLayerMelted()
        {
            var won = new BattleResult { Victory = true, Ticks = 100, Deployed = 3, Survivors = 3 };
            Assert.IsTrue(StageGoal.BreakIce().IsValid);
            Assert.IsFalse(StageGoal.BreakIce().IsMet(won, 0, 20, iceLeft: 1));
            Assert.IsTrue(StageGoal.BreakIce().IsMet(won, 0, 20, iceLeft: 0));
            Assert.IsFalse(StageGoal.BreakIce().IsMet(new BattleResult { Victory = false }, 0, 20, iceLeft: 0));
        }
    }
}
