using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Puzzle;

namespace PuzzleKingdoms.Tests.Puzzle
{
    /// <summary>
    /// Squares, lines of 4 and 5, and the special gems they leave (D-028).
    /// Boards use gem type 0 only for the shapes under test; the filler pattern
    /// (types 1-3) has no run and no square, so nothing else matches.
    /// </summary>
    public class SpecialGemTests
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

        private static MoveReplay.Frame FilledFrame(BoardModel board, MoveResult result, int[,] before, SpecialKind[,] beforeSpecials, int step = 0)
        {
            return MoveReplay.Frames(before, result, beforeSpecials)
                .First(f => f.Kind == MoveReplay.FrameKind.Filled && f.StepIndex == step);
        }

        // --- Squares -------------------------------------------------------

        [Test]
        public void ReadySquareFormsOneGroupOfFour()
        {
            var board = new BoardModel(6, 6, 4, new SeededRandom(5));
            var rows = Enumerable.Range(0, 6).Select(y => Enumerable.Range(0, 6).Select(x => Filler(x, y)).ToArray()).ToArray();
            foreach ((int x, int y) in new[] { (1, 2), (2, 2), (1, 3), (2, 3) })
            {
                rows[y][x] = 0;
            }
            board.LoadRows(rows);
            List<MatchGroup> groups = board.FindMatchGroups();
            Assert.AreEqual(1, groups.Count);
            Assert.AreEqual(4, groups[0].Size);
            Assert.IsTrue(groups[0].HasSquare);
            Assert.AreEqual(0, groups[0].LongestRun);
            Assert.AreEqual(SpecialKind.Blast, groups[0].Creates);
        }

        [Test]
        public void SwapCompletingASquareIsValidAndLeavesABlast()
        {
            BoardModel board = MakeBoard((1, 2), (2, 2), (1, 3), (3, 3));
            int[,] before = board.ToGrid();
            SpecialKind[,] beforeSpecials = board.ToSpecialGrid();
            Assert.IsTrue(board.WouldSwapMatch(new GridPos(3, 3), new GridPos(2, 3)));

            MoveResult result = board.TrySwap(new GridPos(3, 3), new GridPos(2, 3));

            Assert.IsTrue(result.IsValid);
            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(1, first.Groups.Count);
            Assert.IsTrue(first.Groups[0].HasSquare);
            Assert.AreEqual(1, first.Created.Count);
            Assert.AreEqual(SpecialKind.Blast, first.Created[0].Kind);
            Assert.AreEqual(new GridPos(2, 3), first.Created[0].Cell, "the swapped gem becomes the Blast");
            Assert.AreEqual(3, first.Cleared.Count, "the Blast gem itself stays");
            Assert.IsFalse(first.Cleared.Contains(new GridPos(2, 3)));

            MoveReplay.Frame filled = FilledFrame(board, result, before, beforeSpecials);
            Assert.AreEqual(SpecialKind.Blast, filled.Specials[2, 3]);
            Assert.AreEqual(0, filled.Grid[2, 3], "keeps its color");
        }

        [Test]
        public void GeneratedBoardsNeverStartWithASquare()
        {
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var board = new BoardModel(6, 6, 4, new SeededRandom(seed));
                board.Generate();
                Assert.IsEmpty(board.FindMatchGroups(), $"seed {seed}");
            }
        }

        // --- Lines of 4 and 5 ---------------------------------------------

        [Test]
        public void FourInARowEarnsABonusAndLeavesNoSpecialGem()
        {
            BoardModel board = MakeBoard((0, 0), (1, 0), (3, 0), (2, 1));
            MoveResult result = board.TrySwap(new GridPos(2, 0), new GridPos(2, 1));

            Assert.IsTrue(result.IsValid);
            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(4, first.Groups[0].LongestRun);
            Assert.IsTrue(first.Groups[0].IsFourMatch);
            Assert.AreEqual(SpecialKind.None, first.Groups[0].Creates);
            Assert.AreEqual(1, result.FourMatchCount(0));
            Assert.IsEmpty(first.Created);
            Assert.AreEqual(4, first.Cleared.Count);
        }

        [Test]
        public void ThreeInARowEarnsNoBonus()
        {
            BoardModel board = MakeBoard((0, 0), (1, 0), (2, 1));
            MoveResult result = board.TrySwap(new GridPos(2, 0), new GridPos(2, 1));
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.FourMatchCount(0));
            Assert.IsEmpty(result.Steps[0].Created);
        }

        [Test]
        public void FiveInARowLeavesAPrism()
        {
            BoardModel board = MakeBoard((0, 0), (1, 0), (3, 0), (4, 0), (2, 1));
            int[,] before = board.ToGrid();
            SpecialKind[,] beforeSpecials = board.ToSpecialGrid();
            MoveResult result = board.TrySwap(new GridPos(2, 0), new GridPos(2, 1));

            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(5, first.Groups[0].LongestRun);
            Assert.AreEqual(1, first.Created.Count);
            Assert.AreEqual(SpecialKind.Prism, first.Created[0].Kind);
            Assert.AreEqual(new GridPos(2, 0), first.Created[0].Cell);
            Assert.AreEqual(4, first.Cleared.Count);
            Assert.AreEqual(0, result.FourMatchCount(0), "a 5-line is not a 4-bonus");
            Assert.AreEqual(SpecialKind.Prism, FilledFrame(board, result, before, beforeSpecials).Specials[2, 0]);
        }

        // --- Using special gems -------------------------------------------

        [Test]
        public void ClearingABlastAlsoClearsTheThreeByThreeAroundIt()
        {
            BoardModel board = MakeBoard((1, 2), (2, 2), (4, 2));
            board.SetSpecial(new GridPos(2, 2), SpecialKind.Blast);

            MoveResult result = board.TrySwap(new GridPos(3, 2), new GridPos(4, 2));

            Assert.IsTrue(result.IsValid);
            MoveResult.CascadeStep first = result.Steps[0];
            var expected = new HashSet<GridPos>();
            for (int y = 1; y <= 3; y++)
            {
                for (int x = 1; x <= 3; x++)
                {
                    expected.Add(new GridPos(x, y));
                }
            }
            Assert.AreEqual(expected, new HashSet<GridPos>(first.Cleared));
            Assert.AreEqual(1, first.Triggers.Count);
            Assert.AreEqual(SpecialKind.Blast, first.Triggers[0].Kind);
            Assert.AreEqual(new GridPos(2, 2), first.Triggers[0].Origin);
        }

        [Test]
        public void SwappingAPrismClearsEveryGemOfTheNeighboursType()
        {
            BoardModel board = MakeBoard((2, 2));
            board.SetSpecial(new GridPos(2, 2), SpecialKind.Prism);
            int[,] before = board.ToGrid();
            int target = before[3, 2];
            int expected = before.Cast<int>().Count(g => g == target) + 1;

            MoveResult result = board.TrySwap(new GridPos(2, 2), new GridPos(3, 2));

            Assert.IsTrue(result.IsValid);
            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(expected, first.Cleared.Count);
            Assert.AreEqual(SpecialKind.Prism, first.Triggers[0].Kind);
            Assert.GreaterOrEqual(result.ClearedCount(target), expected - 1);
        }

        [Test]
        public void APrismCanAlwaysBeSwapped()
        {
            BoardModel board = MakeBoard((2, 2));
            Assert.IsFalse(board.WouldSwapMatch(new GridPos(2, 2), new GridPos(3, 2)), "plain gem: no match");
            board.SetSpecial(new GridPos(2, 2), SpecialKind.Prism);
            Assert.IsTrue(board.WouldSwapMatch(new GridPos(2, 2), new GridPos(3, 2)));
            Assert.IsTrue(board.HasValidMove());
        }

        [Test]
        public void ABlastCaughtInAnExplosionFiresToo()
        {
            BoardModel board = MakeBoard((1, 2), (2, 2), (4, 2));
            board.SetSpecial(new GridPos(2, 2), SpecialKind.Blast);
            // A second Blast inside the first one's 3x3 area, at the board edge of the next one.
            board.SetSpecial(new GridPos(3, 3), SpecialKind.Blast);

            MoveResult result = board.TrySwap(new GridPos(3, 2), new GridPos(4, 2));

            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(2, first.Triggers.Count(t => t.Kind == SpecialKind.Blast));
            Assert.IsTrue(first.Cleared.Contains(new GridPos(4, 4)), "the second Blast reaches (4,4)");
        }

        [Test]
        public void ABlastCaughtInAnExplosionSetsOffAPrism()
        {
            BoardModel board = MakeBoard((1, 2), (2, 2), (4, 2));
            board.SetSpecial(new GridPos(2, 2), SpecialKind.Blast);
            board.SetSpecial(new GridPos(3, 3), SpecialKind.Prism);
            int[,] before = board.ToGrid();
            int prismType = before[3, 3];

            MoveResult result = board.TrySwap(new GridPos(3, 2), new GridPos(4, 2));

            MoveResult.CascadeStep first = result.Steps[0];
            Assert.AreEqual(SpecialKind.Blast, first.Triggers[0].Kind);
            Assert.AreEqual(SpecialKind.Prism, first.Triggers[1].Kind);
            // After the swap (3,2) and (4,2) trade places, but no gem of prismType may remain
            // outside this step's cleared cells in the board the step started from.
            var cleared = new HashSet<GridPos>(first.Cleared);
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    int gem = (x == 3 && y == 2) ? before[4, 2] : (x == 4 && y == 2) ? before[3, 2] : before[x, y];
                    if (gem == prismType)
                    {
                        Assert.IsTrue(cleared.Contains(new GridPos(x, y)), $"({x},{y}) of the prism's type is cleared");
                    }
                }
            }
        }

        [Test]
        public void SpecialGemsFallWithTheirGem()
        {
            BoardModel board = MakeBoard((0, 3), (1, 3), (2, 4));
            board.SetSpecial(new GridPos(1, 2), SpecialKind.Blast);
            int[,] before = board.ToGrid();
            SpecialKind[,] beforeSpecials = board.ToSpecialGrid();

            MoveResult result = board.TrySwap(new GridPos(2, 3), new GridPos(2, 4));

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(3, result.Steps[0].Cleared.Count);
            MoveReplay.Frame filled = FilledFrame(board, result, before, beforeSpecials);
            Assert.AreEqual(SpecialKind.Blast, filled.Specials[1, 3], "fell one cell with its gem");
            Assert.AreEqual(SpecialKind.None, filled.Specials[1, 2]);
            Assert.AreEqual(before[1, 2], filled.Grid[1, 3]);
        }

        [Test]
        public void AShuffleKeepsSpecialGems()
        {
            var board = new BoardModel(6, 6, 4, new SeededRandom(8));
            board.Generate();
            board.SetSpecial(new GridPos(1, 1), SpecialKind.Blast);
            board.SetSpecial(new GridPos(4, 3), SpecialKind.Prism);
            board.Shuffle();
            int blasts = 0, prisms = 0;
            foreach (SpecialKind kind in board.ToSpecialGrid())
            {
                blasts += kind == SpecialKind.Blast ? 1 : 0;
                prisms += kind == SpecialKind.Prism ? 1 : 0;
            }
            Assert.AreEqual(1, blasts);
            Assert.AreEqual(1, prisms);
        }

        // --- Whole games ---------------------------------------------------

        [Test]
        public void PlayingManyMovesKeepsTheBoardValidAndReplayable()
        {
            int specialsSeen = 0;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var board = new BoardModel(6, 6, 4, new SeededRandom(seed));
                board.Generate();
                for (int move = 0; move < 40; move++)
                {
                    int[,] before = board.ToGrid();
                    SpecialKind[,] beforeSpecials = board.ToSpecialGrid();
                    Assert.IsTrue(board.TryFindValidMove(out GridPos a, out GridPos b), $"seed {seed} move {move}");
                    MoveResult result = board.TrySwap(a, b);
                    Assert.IsTrue(result.IsValid);

                    List<MoveReplay.Frame> frames = MoveReplay.Frames(before, result, beforeSpecials);
                    MoveReplay.Frame last = frames[frames.Count - 1];
                    Assert.AreEqual(board.ToGrid(), last.Grid, $"grid seed {seed} move {move}");
                    Assert.AreEqual(board.ToSpecialGrid(), last.Specials, $"specials seed {seed} move {move}");
                    Assert.IsEmpty(board.FindMatchGroups(), $"stable seed {seed} move {move}");
                    Assert.IsTrue(board.ToGrid().Cast<int>().All(g => g != BoardModel.Empty));
                    specialsSeen += result.Steps.Sum(s => s.Created.Count);
                }
            }
            Assert.Greater(specialsSeen, 0, "natural play should leave some special gems");
        }

        [Test]
        public void SameSeedSameSpecialOutcome()
        {
            var outcomes = new List<string>();
            for (int run = 0; run < 2; run++)
            {
                var board = new BoardModel(6, 6, 4, new SeededRandom(77));
                board.Generate();
                var log = new System.Text.StringBuilder();
                for (int move = 0; move < 30; move++)
                {
                    board.TryFindValidMove(out GridPos a, out GridPos b);
                    MoveResult result = board.TrySwap(a, b);
                    log.Append(result.Steps.Count).Append(':')
                        .Append(result.Steps.Sum(s => s.Cleared.Count)).Append(':')
                        .Append(result.Steps.Sum(s => s.Created.Count)).Append(';');
                }
                outcomes.Add(log.ToString());
            }
            Assert.AreEqual(outcomes[0], outcomes[1]);
        }

        [Test]
        public void SessionAddsTheFourMatchBonusToTheMeter()
        {
            BoardModel board = MakeBoard((0, 0), (1, 0), (3, 0), (2, 1));
            string[] keys = { "warrior", "archer", "mage", "rally" };
            var meters = keys.ToDictionary(k => k, k => new RecruitmentMeter(1000));
            var session = new PuzzleSession(board, 5, keys, meters, fourMatchBonusPoints: 2);

            MoveResult result = session.PlaySwap(new GridPos(2, 0), new GridPos(2, 1));

            Assert.AreEqual(1, result.FourMatchCount(0));
            Assert.AreEqual(result.ClearedCount(0) + 2, meters["warrior"].Points);
        }

        [Test]
        public void SessionWithoutBonusJustCountsClearedGems()
        {
            BoardModel board = MakeBoard((0, 0), (1, 0), (3, 0), (2, 1));
            string[] keys = { "warrior", "archer", "mage", "rally" };
            var meters = keys.ToDictionary(k => k, k => new RecruitmentMeter(1000));
            var session = new PuzzleSession(board, 5, keys, meters);

            MoveResult result = session.PlaySwap(new GridPos(2, 0), new GridPos(2, 1));

            Assert.AreEqual(result.ClearedCount(0), meters["warrior"].Points);
        }
    }
}
