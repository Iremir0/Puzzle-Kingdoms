using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// Rebuilds the intermediate boards of a resolved <see cref="MoveResult"/>
    /// so a view can animate them ("compute, then animate"). Grids are
    /// <c>int[x, y]</c> like <see cref="BoardModel.ToGrid"/>.
    /// </summary>
    public static class MoveReplay
    {
        /// <summary>What happened right before a frame's grid.</summary>
        public enum FrameKind
        {
            /// <summary>The two swapped gems traded places.</summary>
            Swapped,
            /// <summary>The matched gems of <see cref="Frame.Step"/> were removed.</summary>
            Cleared,
            /// <summary>Gems fell and new gems dropped in for <see cref="Frame.Step"/>.</summary>
            Filled,
            /// <summary>No move was left, so the board was reshuffled.</summary>
            Reshuffled,
        }

        /// <summary>One board state to show, with the cascade step that produced it.</summary>
        public sealed class Frame
        {
            public readonly FrameKind Kind;
            public readonly int[,] Grid;
            /// <summary>The cascade step for Cleared/Filled frames, otherwise null.</summary>
            public readonly MoveResult.CascadeStep Step;
            /// <summary>0 for the player's match, 1+ for cascades.</summary>
            public readonly int StepIndex;

            public Frame(FrameKind kind, int[,] grid, MoveResult.CascadeStep step, int stepIndex)
            {
                Kind = kind;
                Grid = grid;
                Step = step;
                StepIndex = stepIndex;
            }
        }

        /// <summary>
        /// All frames of a move, starting from the board before the swap.
        /// Empty for an invalid move.
        /// </summary>
        public static List<Frame> Frames(int[,] before, MoveResult result)
        {
            var frames = new List<Frame>();
            if (result == null || !result.IsValid)
            {
                return frames;
            }

            int[,] grid = (int[,])before.Clone();
            (grid[result.SwapA.X, result.SwapA.Y], grid[result.SwapB.X, result.SwapB.Y]) =
                (grid[result.SwapB.X, result.SwapB.Y], grid[result.SwapA.X, result.SwapA.Y]);
            frames.Add(new Frame(FrameKind.Swapped, (int[,])grid.Clone(), null, -1));

            for (int i = 0; i < result.Steps.Count; i++)
            {
                MoveResult.CascadeStep step = result.Steps[i];
                foreach (MatchGroup group in step.Groups)
                {
                    foreach (GridPos cell in group.Cells)
                    {
                        grid[cell.X, cell.Y] = BoardModel.Empty;
                    }
                }
                frames.Add(new Frame(FrameKind.Cleared, (int[,])grid.Clone(), step, i));

                // Read every falling gem first so the order of Falls does not matter.
                var falling = new int[step.Falls.Count];
                for (int f = 0; f < step.Falls.Count; f++)
                {
                    falling[f] = grid[step.Falls[f].From.X, step.Falls[f].From.Y];
                }
                foreach (MoveResult.GemFall fall in step.Falls)
                {
                    grid[fall.From.X, fall.From.Y] = BoardModel.Empty;
                }
                for (int f = 0; f < step.Falls.Count; f++)
                {
                    grid[step.Falls[f].To.X, step.Falls[f].To.Y] = falling[f];
                }
                foreach (MoveResult.GemSpawn spawn in step.Spawns)
                {
                    grid[spawn.Cell.X, spawn.Cell.Y] = spawn.GemType;
                }
                frames.Add(new Frame(FrameKind.Filled, (int[,])grid.Clone(), step, i));
            }

            if (result.Reshuffled && result.BoardAfterReshuffle != null)
            {
                frames.Add(new Frame(FrameKind.Reshuffled, (int[,])result.BoardAfterReshuffle.Clone(), null, -1));
            }
            return frames;
        }
    }
}
