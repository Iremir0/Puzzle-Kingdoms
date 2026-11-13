using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Puzzle;

namespace PuzzleKingdoms.Tests.Puzzle
{
    public class BoardModelTests
    {
        /// <summary>Diagonal pattern (x + y) % 4: contains no match and no valid move.</summary>
        private static int[][] DeadlockRows() => new[]
        {
            new[] { 0, 1, 2, 3, 0, 1 },
            new[] { 1, 2, 3, 0, 1, 2 },
            new[] { 2, 3, 0, 1, 2, 3 },
            new[] { 3, 0, 1, 2, 3, 0 },
            new[] { 0, 1, 2, 3, 0, 1 },
            new[] { 1, 2, 3, 0, 1, 2 },
        };

        private static BoardModel MakeBoard(ulong seed = 1) => new BoardModel(6, 6, 4, new SeededRandom(seed));

        private static int[][] RowsWith(params (int x, int y, int gem)[] changes)
        {
            int[][] rows = DeadlockRows();
            foreach (var (x, y, gem) in changes)
            {
                rows[y][x] = gem;
            }
            return rows;
        }

        private static Dictionary<int, int> CountTypes(int[,] grid)
        {
            var counts = new Dictionary<int, int>();
            foreach (int gem in grid)
            {
                counts[gem] = counts.TryGetValue(gem, out int n) ? n + 1 : 1;
            }
            return counts;
        }

        private static Dictionary<int, int> CountTypes(int[][] rows) =>
            CountTypes(ToGrid(rows));

        private static int[,] ToGrid(int[][] rows)
        {
            var grid = new int[rows[0].Length, rows.Length];
            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    grid[x, y] = rows[y][x];
                }
            }
            return grid;
        }

        private static bool IsFull(BoardModel board) => board.ToGrid().Cast<int>().All(g => g != BoardModel.Empty);

        [Test]
        public void GenerateHasNoMatchesAndAValidMove()
        {
            for (ulong seed = 1; seed <= 50; seed++)
            {
                BoardModel board = MakeBoard(seed);
                board.Generate();
                Assert.IsTrue(IsFull(board), $"full (seed {seed})");
                Assert.IsEmpty(board.FindMatchGroups(), $"no matches (seed {seed})");
                Assert.IsTrue(board.HasValidMove(), $"has move (seed {seed})");
            }
        }

        [Test]
        public void GenerateIsDeterministicForASeed()
        {
            BoardModel a = MakeBoard(42);
            BoardModel b = MakeBoard(42);
            a.Generate();
            b.Generate();
            Assert.AreEqual(a.ToGrid(), b.ToGrid());
        }

        [Test]
        public void DeadlockPatternHasNoMove()
        {
            BoardModel board = MakeBoard();
            board.LoadRows(DeadlockRows());
            Assert.IsEmpty(board.FindMatchGroups());
            Assert.IsFalse(board.HasValidMove());
        }

        [Test]
        public void LShapeFormsSingleGroup()
        {
            BoardModel board = MakeBoard();
            board.LoadRows(RowsWith((1, 0, 0), (2, 0, 0), (0, 1, 0), (0, 2, 0)));
            List<MatchGroup> groups = board.FindMatchGroups();
            Assert.AreEqual(1, groups.Count);
            Assert.AreEqual(0, groups[0].GemType);
            Assert.AreEqual(5, groups[0].Size);
        }

        [Test]
        public void SeparateMatchesFormSeparateGroups()
        {
            BoardModel board = MakeBoard();
            board.LoadRows(RowsWith(
                (5, 0, 0), (5, 1, 0), (5, 2, 0), (5, 3, 0),
                (0, 5, 1), (1, 5, 1), (2, 5, 1)));
            List<MatchGroup> groups = board.FindMatchGroups();
            Assert.AreEqual(2, groups.Count);
            var sizes = groups.ToDictionary(g => g.GemType, g => g.Size);
            Assert.AreEqual(new Dictionary<int, int> { { 0, 4 }, { 1, 3 } }, sizes);
        }

        [Test]
        public void SwapRejectedWhenNotAdjacentOrNoMatch()
        {
            BoardModel board = MakeBoard();
            board.LoadRows(DeadlockRows());
            int[,] before = board.ToGrid();
            Assert.IsFalse(board.TrySwap(new GridPos(0, 0), new GridPos(2, 0)).IsValid, "not adjacent");
            Assert.IsFalse(board.TrySwap(new GridPos(0, 0), new GridPos(1, 0)).IsValid, "no match");
            Assert.IsFalse(board.TrySwap(new GridPos(5, 5), new GridPos(6, 5)).IsValid, "outside board");
            Assert.AreEqual(before, board.ToGrid());
        }

        [Test]
        public void ValidSwapClearsMatchAndRefills()
        {
            BoardModel board = MakeBoard(7);
            // Swapping (2,0) and (2,1) completes 0-0-0 on row 0.
            board.LoadRows(RowsWith((1, 0, 0), (2, 1, 0), (2, 0, 2)));
            Assert.IsEmpty(board.FindMatchGroups(), "precondition");

            MoveResult result = board.TrySwap(new GridPos(2, 0), new GridPos(2, 1));
            Assert.IsTrue(result.IsValid);
            Assert.GreaterOrEqual(result.Steps.Count, 1);
            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(1, first.Groups.Count);
            Assert.AreEqual(3, first.Groups[0].Size);
            Assert.AreEqual(0, first.Falls.Count, "nothing above row 0 to fall");
            Assert.AreEqual(3, first.Spawns.Count);
            Assert.GreaterOrEqual(result.ClearedCount(0), 3);
            Assert.IsTrue(IsFull(board));
            Assert.IsEmpty(board.FindMatchGroups(), "stable after move");
        }

        [Test]
        public void GravityMovesGemsDown()
        {
            BoardModel board = MakeBoard(3);
            // Swapping (2,2) and (2,3) completes 0-0-0 on row 2; rows 0-1 fall by one.
            board.LoadRows(RowsWith((0, 2, 0), (1, 2, 0), (2, 2, 1), (3, 2, 1), (2, 3, 0)));
            Assert.IsEmpty(board.FindMatchGroups(), "precondition");

            MoveResult result = board.TrySwap(new GridPos(2, 2), new GridPos(2, 3));
            Assert.IsTrue(result.IsValid);
            MoveResult.CascadeStep first = result.Steps[0];

            var moves = first.Falls.ToDictionary(f => f.From, f => f.To);
            var expected = new Dictionary<GridPos, GridPos>();
            for (int x = 0; x < 3; x++)
            {
                expected[new GridPos(x, 1)] = new GridPos(x, 2);
                expected[new GridPos(x, 0)] = new GridPos(x, 1);
            }
            Assert.AreEqual(expected, moves);
            Assert.AreEqual(3, first.Spawns.Count);
            foreach (MoveResult.GemSpawn spawn in first.Spawns)
            {
                Assert.AreEqual(0, spawn.Cell.Y, "spawn on top row");
                Assert.AreEqual(1, spawn.DropDistance);
            }
        }

        [Test]
        public void SameSeedSameMoveSameOutcome()
        {
            int[][] rows = RowsWith((1, 0, 0), (2, 1, 0), (2, 0, 2));
            BoardModel a = MakeBoard(99);
            BoardModel b = MakeBoard(99);
            a.LoadRows(rows);
            b.LoadRows(rows);
            MoveResult ra = a.TrySwap(new GridPos(2, 0), new GridPos(2, 1));
            MoveResult rb = b.TrySwap(new GridPos(2, 0), new GridPos(2, 1));
            Assert.AreEqual(ra.ClearedByType, rb.ClearedByType);
            Assert.AreEqual(a.ToGrid(), b.ToGrid());
        }

        [Test]
        public void ShuffleResolvesDeadlockAndKeepsGems()
        {
            BoardModel board = MakeBoard(5);
            board.LoadRows(DeadlockRows());
            board.Shuffle();
            Assert.IsEmpty(board.FindMatchGroups());
            Assert.IsTrue(board.HasValidMove());
            Assert.AreEqual(CountTypes(DeadlockRows()), CountTypes(board.ToGrid()));
        }

        [Test]
        public void MovesAlwaysLeaveAPlayableBoard()
        {
            BoardModel board = MakeBoard(11);
            board.Generate();
            for (int i = 0; i < 200; i++)
            {
                Assert.IsTrue(board.TryFindValidMove(out GridPos a, out GridPos b), $"move available ({i})");
                Assert.IsTrue(board.TrySwap(a, b).IsValid);
                Assert.IsTrue(IsFull(board));
                Assert.IsEmpty(board.FindMatchGroups());
            }
        }
    }
}
