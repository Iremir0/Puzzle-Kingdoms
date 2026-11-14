using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Puzzle;

namespace PuzzleKingdoms.Tests.Puzzle
{
    /// <summary>
    /// Gems encased in stone (D-032): the first match (or special effect) only breaks the
    /// stone and the gem stays; the next one clears it. Boards use gem type 0 only for the
    /// shapes under test; the filler pattern (types 1-3) has no run and no square.
    /// </summary>
    public class StoneGemTests
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

        private static bool[,] MakeStoneGrid(int x, int y)
        {
            var stones = new bool[6, 6];
            stones[x, y] = true;
            return stones;
        }

        private static int StoneCount(BoardModel board) => board.ToStoneGrid().Cast<bool>().Count(s => s);

        private static MoveReplay.Frame Filled(BoardModel board, MoveResult result, int[,] before, SpecialKind[,] specials, bool[,] stones) =>
            MoveReplay.Frames(before, result, specials, stones).First(f => f.Kind == MoveReplay.FrameKind.Filled && f.StepIndex == 0);

        // --- Placing stones -------------------------------------------------

        [Test]
        public void PlaceStonesPlacesTheRequestedNumberNeverNextToEachOther()
        {
            for (ulong seed = 1; seed <= 50; seed++)
            {
                var board = new BoardModel(6, 6, 4, new SeededRandom(seed));
                board.Generate();
                Assert.AreEqual(6, board.PlaceStones(6), $"seed {seed}");
                Assert.AreEqual(6, StoneCount(board));
                foreach (GridPos cell in AllCells(board).Where(board.IsStone))
                {
                    foreach (GridPos direction in new[] { GridPos.Up, GridPos.Down, GridPos.Left, GridPos.Right })
                    {
                        GridPos neighbour = cell + direction;
                        Assert.IsFalse(board.IsInside(neighbour) && board.IsStone(neighbour), $"seed {seed}: stones at {cell} and {neighbour} touch");
                    }
                }
                Assert.IsEmpty(board.FindMatchGroups(), "stones never create a match");
            }
        }

        [Test]
        public void ZeroStonesPlacesNothing()
        {
            var board = new BoardModel(6, 6, 4, new SeededRandom(2));
            board.Generate();
            Assert.AreEqual(0, board.PlaceStones(0));
            Assert.AreEqual(0, StoneCount(board));
        }

        [Test]
        public void StonePlacementIsDeterministicForASeed()
        {
            var a = new BoardModel(6, 6, 4, new SeededRandom(9));
            var b = new BoardModel(6, 6, 4, new SeededRandom(9));
            a.Generate();
            b.Generate();
            a.PlaceStones(5);
            b.PlaceStones(5);
            Assert.AreEqual(a.ToStoneGrid(), b.ToStoneGrid());
        }

        private static IEnumerable<GridPos> AllCells(BoardModel board)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    yield return new GridPos(x, y);
                }
            }
        }

        // --- Breaking stones ------------------------------------------------

        [Test]
        public void AStoneGemTakesPartInAMatchButOnlyItsStoneBreaks()
        {
            BoardModel board = MakeBoard((0, 0), (1, 0), (3, 0), (2, 1));
            board.SetStone(new GridPos(1, 0), true);
            int[,] before = board.ToGrid();
            SpecialKind[,] specials = board.ToSpecialGrid();
            bool[,] stones = board.ToStoneGrid();

            MoveResult result = board.TrySwap(new GridPos(2, 0), new GridPos(2, 1));

            Assert.IsTrue(result.IsValid, "a stone gem can be part of a valid swap");
            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(4, first.Groups[0].Size, "the stone gem counts as part of the match");
            Assert.AreEqual(3, first.Cleared.Count, "the stone gem is not cleared");
            Assert.AreEqual(new[] { new GridPos(1, 0) }, first.Cracked.ToArray());
            MoveReplay.Frame filled = Filled(board, result, before, specials, stones);
            Assert.AreEqual(0, filled.Grid[1, 0], "the gem is still there");
            Assert.IsFalse(filled.Stones[1, 0], "but its stone is gone");
        }

        [Test]
        public void ABrokenStoneGemIsClearedByTheNextMatch()
        {
            BoardModel board = MakeBoard((0, 0), (1, 0), (3, 0), (2, 1));
            board.SetStone(new GridPos(1, 0), true);
            int[,] before = board.ToGrid();
            MoveResult first = board.TrySwap(new GridPos(2, 0), new GridPos(2, 1));
            // Start from the board as it was right after the first wave (the gem is still there).
            Assert.IsFalse(MoveReplay.Frames(before, first, null, MakeStoneGrid(1, 0)).First(f => f.Kind == MoveReplay.FrameKind.Cleared).Stones[1, 0]);
            int[,] grid = MoveReplay.Frames(before, first, null, MakeStoneGrid(1, 0))
                .First(f => f.Kind == MoveReplay.FrameKind.Filled).Grid;
            Assert.AreEqual(0, grid[1, 0], "the gem survived the first match");

            // A second 4-line through that gem: now it has no stone and is cleared like any other.
            var rows = Enumerable.Range(0, 6).Select(y => Enumerable.Range(0, 6).Select(x => grid[x, y]).ToArray()).ToArray();
            rows[0][0] = 0; rows[0][2] = 3; rows[0][3] = 0; rows[1][2] = 0;
            board.LoadRows(rows);
            Assert.IsEmpty(board.FindMatchGroups(), "precondition");

            MoveResult second = board.TrySwap(new GridPos(2, 0), new GridPos(2, 1));

            Assert.IsTrue(second.IsValid);
            Assert.IsEmpty(second.Steps[0].Cracked);
            Assert.AreEqual(4, second.Steps[0].Cleared.Count);
            Assert.IsTrue(second.Steps[0].Cleared.Contains(new GridPos(1, 0)), "second hit clears the gem");
        }

        [Test]
        public void EveryGemOfAMatchBeingStoneBreaksAllAndClearsNone()
        {
            BoardModel board = MakeBoard((0, 0), (1, 0), (2, 1));
            foreach (var cell in new[] { new GridPos(0, 0), new GridPos(1, 0), new GridPos(2, 1) })
            {
                board.SetStone(cell, true);
            }

            MoveResult result = board.TrySwap(new GridPos(2, 0), new GridPos(2, 1));

            Assert.IsTrue(result.IsValid);
            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(3, first.Cracked.Count);
            Assert.IsEmpty(first.Cleared);
            Assert.IsEmpty(first.Created);
            Assert.AreEqual(0, first.Spawns.Count, "nothing was cleared, nothing refills");
        }

        [Test]
        public void ABlastBreaksStonesInsteadOfClearingThem()
        {
            BoardModel board = MakeBoard((1, 2), (2, 2), (4, 2));
            board.SetSpecial(new GridPos(2, 2), SpecialKind.Blast);
            board.SetStone(new GridPos(3, 3), true);

            MoveResult result = board.TrySwap(new GridPos(3, 2), new GridPos(4, 2));

            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(8, first.Cleared.Count, "the 3x3 area minus the stone gem");
            Assert.IsFalse(first.Cleared.Contains(new GridPos(3, 3)));
            CollectionAssert.Contains(first.Cracked, new GridPos(3, 3));
        }

        [Test]
        public void APrismBreaksStonesOfItsTypeInsteadOfClearingThem()
        {
            BoardModel board = MakeBoard((2, 2));
            board.SetSpecial(new GridPos(2, 2), SpecialKind.Prism);
            int[,] before = board.ToGrid();
            int target = before[3, 2];
            var targetCells = AllCells(board).Where(c => before[c.X, c.Y] == target).ToList();
            GridPos stone = targetCells.First(c => c != new GridPos(2, 2) && c != new GridPos(3, 2));
            board.SetStone(stone, true);

            MoveResult result = board.TrySwap(new GridPos(2, 2), new GridPos(3, 2));

            MoveResult.CascadeStep first = result.Steps[0];
            CollectionAssert.Contains(first.Cracked, stone);
            Assert.IsFalse(first.Cleared.Contains(stone));
            Assert.AreEqual(targetCells.Count - 1 + 1, first.Cleared.Count, "all of the type except the stone gem, plus the Prism");
        }

        // --- Moving stones --------------------------------------------------

        [Test]
        public void StonesFallWithTheirGem()
        {
            BoardModel board = MakeBoard((0, 3), (1, 3), (2, 4));
            board.SetStone(new GridPos(1, 2), true);
            int[,] before = board.ToGrid();
            SpecialKind[,] specials = board.ToSpecialGrid();
            bool[,] stones = board.ToStoneGrid();

            MoveResult result = board.TrySwap(new GridPos(2, 3), new GridPos(2, 4));

            Assert.IsTrue(result.IsValid);
            MoveReplay.Frame filled = Filled(board, result, before, specials, stones);
            Assert.IsTrue(filled.Stones[1, 3], "the stone fell one cell with its gem");
            Assert.IsFalse(filled.Stones[1, 2]);
            Assert.AreEqual(before[1, 2], filled.Grid[1, 3]);
        }

        [Test]
        public void SwappingTradesTheStonesToo()
        {
            BoardModel board = MakeBoard((0, 0), (1, 0), (3, 0), (2, 1));
            board.SetStone(new GridPos(2, 1), true);
            int[,] before = board.ToGrid();
            SpecialKind[,] specials = board.ToSpecialGrid();
            bool[,] stones = board.ToStoneGrid();

            MoveResult result = board.TrySwap(new GridPos(2, 0), new GridPos(2, 1));

            // The zero from (2,1) moves to (2,0) wearing its stone, so that stone breaks there.
            CollectionAssert.Contains(result.Steps[0].Cracked, new GridPos(2, 0));
            Assert.IsFalse(MoveReplay.Frames(before, result, specials, stones)[0].Stones[2, 1]);
            Assert.IsTrue(MoveReplay.Frames(before, result, specials, stones)[0].Stones[2, 0]);
        }

        [Test]
        public void AShuffleKeepsEveryStone()
        {
            var board = new BoardModel(6, 6, 4, new SeededRandom(8));
            board.Generate();
            board.PlaceStones(5);
            board.Shuffle();
            Assert.AreEqual(5, StoneCount(board));
        }

        // --- Whole games ------------------------------------------------------

        [Test]
        public void PlayingManyMovesWithStonesKeepsTheBoardValidAndReplayable()
        {
            int cracked = 0;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var board = new BoardModel(6, 6, 4, new SeededRandom(seed));
                board.Generate();
                board.PlaceStones(6);
                for (int move = 0; move < 40; move++)
                {
                    int[,] before = board.ToGrid();
                    SpecialKind[,] specials = board.ToSpecialGrid();
                    bool[,] stones = board.ToStoneGrid();
                    int stonesBefore = StoneCount(board);
                    Assert.IsTrue(board.TryFindValidMove(out GridPos a, out GridPos b), $"seed {seed} move {move}");
                    MoveResult result = board.TrySwap(a, b);
                    Assert.IsTrue(result.IsValid);

                    List<MoveReplay.Frame> frames = MoveReplay.Frames(before, result, specials, stones);
                    MoveReplay.Frame last = frames[frames.Count - 1];
                    Assert.AreEqual(board.ToGrid(), last.Grid, $"grid seed {seed} move {move}");
                    Assert.AreEqual(board.ToStoneGrid(), last.Stones, $"stones seed {seed} move {move}");
                    Assert.LessOrEqual(StoneCount(board), stonesBefore, "stones are never created during play");
                    Assert.AreEqual(stonesBefore - StoneCount(board), result.Steps.Sum(s => s.Cracked.Count), "every broken stone is reported");
                    Assert.IsTrue(board.ToGrid().Cast<int>().All(g => g != BoardModel.Empty));
                    cracked += result.Steps.Sum(s => s.Cracked.Count);
                }
            }
            Assert.Greater(cracked, 0, "stones should break during normal play");
        }

        [Test]
        public void AStageWithStoneGemsStartsItsPuzzleWithThem()
        {
            var stage = new PuzzleKingdoms.Logic.Stages.StageDef { Id = "stage_test", Moves = 12, StoneGems = 4 };
            stage.Enemies.Add(new PuzzleKingdoms.Logic.Stages.EnemySlot
            {
                Unit = new PuzzleKingdoms.Logic.Army.UnitDef { Id = "goblin" },
                Cell = new GridPos(0, 0),
            });
            var unit = new PuzzleKingdoms.Logic.Army.UnitDef { Id = "warrior" };
            var run = new StageRun(
                stage,
                new[] { unit, new PuzzleKingdoms.Logic.Army.UnitDef { Id = "archer" }, new PuzzleKingdoms.Logic.Army.UnitDef { Id = "mage" } },
                new PuzzleRules(), new PuzzleKingdoms.Logic.Army.ArmyRules(), new PuzzleKingdoms.Logic.Combat.BattleRules(),
                new PuzzleKingdoms.Logic.Progression.ProgressionRules(), new SeededRandom(3));

            Assert.AreEqual(4, StoneCount(run.Puzzle.Board));
        }
    }
}
