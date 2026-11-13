using System.Collections.Generic;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Puzzle;

namespace PuzzleKingdoms.Tests.Puzzle
{
    public class MoveReplayTests
    {
        private static bool SameGrid(int[,] a, int[,] b)
        {
            if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
            {
                return false;
            }
            for (int x = 0; x < a.GetLength(0); x++)
            {
                for (int y = 0; y < a.GetLength(1); y++)
                {
                    if (a[x, y] != b[x, y])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        [Test]
        public void FramesEndOnTheBoardTheModelEndedOn()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var board = new BoardModel(6, 6, 4, new SeededRandom(seed));
                board.Generate();
                for (int move = 0; move < 12; move++)
                {
                    int[,] before = board.ToGrid();
                    Assert.IsTrue(board.TryFindValidMove(out GridPos a, out GridPos b));
                    MoveResult result = board.TrySwap(a, b);
                    List<MoveReplay.Frame> frames = MoveReplay.Frames(before, result);

                    Assert.AreEqual(MoveReplay.FrameKind.Swapped, frames[0].Kind);
                    Assert.AreEqual(1 + 2 * result.Steps.Count + (result.Reshuffled ? 1 : 0), frames.Count);
                    Assert.IsTrue(SameGrid(board.ToGrid(), frames[frames.Count - 1].Grid), $"seed {seed} move {move}");
                }
            }
        }

        [Test]
        public void ClearedFrameEmptiesExactlyTheMatchedCells()
        {
            var board = new BoardModel(6, 6, 4, new SeededRandom(9));
            board.Generate();
            int[,] before = board.ToGrid();
            board.TryFindValidMove(out GridPos a, out GridPos b);
            MoveResult result = board.TrySwap(a, b);
            List<MoveReplay.Frame> frames = MoveReplay.Frames(before, result);

            MoveReplay.Frame cleared = frames[1];
            Assert.AreEqual(MoveReplay.FrameKind.Cleared, cleared.Kind);
            Assert.AreSame(result.Steps[0], cleared.Step);
            int empty = 0;
            foreach (int gem in cleared.Grid)
            {
                empty += gem == BoardModel.Empty ? 1 : 0;
            }
            var matched = new HashSet<GridPos>();
            foreach (MatchGroup group in result.Steps[0].Groups)
            {
                matched.UnionWith(group.Cells);
            }
            Assert.AreEqual(matched.Count, empty);
            foreach (GridPos cell in matched)
            {
                Assert.AreEqual(BoardModel.Empty, cleared.Grid[cell.X, cell.Y]);
            }
        }

        [Test]
        public void InvalidMoveHasNoFrames()
        {
            var board = new BoardModel(6, 6, 4, new SeededRandom(3));
            board.Generate();
            MoveResult result = board.TrySwap(new GridPos(0, 0), new GridPos(5, 5));
            Assert.IsFalse(result.IsValid);
            Assert.IsEmpty(MoveReplay.Frames(board.ToGrid(), result));
        }
    }
}
