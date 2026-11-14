using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// Rebuilds the intermediate boards of a resolved <see cref="MoveResult"/>
    /// so a view can animate them ("compute, then animate"). Grids are
    /// <c>[x, y]</c> like <see cref="BoardModel.ToGrid"/>.
    /// </summary>
    public static class MoveReplay
    {
        /// <summary>What happened right before a frame's grid.</summary>
        public enum FrameKind
        {
            /// <summary>The two swapped gems traded places.</summary>
            Swapped,
            /// <summary>The cells of <see cref="Frame.Step"/> were removed and its special gems appeared.</summary>
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
            /// <summary>Special gems on the board: Specials[x, y].</summary>
            public readonly SpecialKind[,] Specials;
            /// <summary>The cascade step for Cleared/Filled frames, otherwise null.</summary>
            public readonly MoveResult.CascadeStep Step;
            /// <summary>0 for the player's match, 1+ for cascades.</summary>
            public readonly int StepIndex;

            public Frame(FrameKind kind, int[,] grid, SpecialKind[,] specials, MoveResult.CascadeStep step, int stepIndex)
            {
                Kind = kind;
                Grid = grid;
                Specials = specials;
                Step = step;
                StepIndex = stepIndex;
            }
        }

        /// <summary>
        /// All frames of a move, starting from the board before the swap.
        /// <paramref name="beforeSpecials"/> is the special-gem grid before the swap
        /// (null = none). Empty for an invalid move.
        /// </summary>
        public static List<Frame> Frames(int[,] before, MoveResult result, SpecialKind[,] beforeSpecials = null)
        {
            var frames = new List<Frame>();
            if (result == null || !result.IsValid)
            {
                return frames;
            }

            int[,] grid = (int[,])before.Clone();
            SpecialKind[,] specials = beforeSpecials != null
                ? (SpecialKind[,])beforeSpecials.Clone()
                : new SpecialKind[before.GetLength(0), before.GetLength(1)];
            GridPos a = result.SwapA;
            GridPos b = result.SwapB;
            (grid[a.X, a.Y], grid[b.X, b.Y]) = (grid[b.X, b.Y], grid[a.X, a.Y]);
            (specials[a.X, a.Y], specials[b.X, b.Y]) = (specials[b.X, b.Y], specials[a.X, a.Y]);
            frames.Add(new Frame(FrameKind.Swapped, (int[,])grid.Clone(), (SpecialKind[,])specials.Clone(), null, -1));

            for (int i = 0; i < result.Steps.Count; i++)
            {
                MoveResult.CascadeStep step = result.Steps[i];
                foreach (GridPos cell in step.Cleared)
                {
                    grid[cell.X, cell.Y] = BoardModel.Empty;
                    specials[cell.X, cell.Y] = SpecialKind.None;
                }
                foreach (MoveResult.SpecialCreated created in step.Created)
                {
                    specials[created.Cell.X, created.Cell.Y] = created.Kind;
                }
                frames.Add(new Frame(FrameKind.Cleared, (int[,])grid.Clone(), (SpecialKind[,])specials.Clone(), step, i));

                // Read every falling gem first so the order of Falls does not matter.
                var fallingGems = new int[step.Falls.Count];
                var fallingSpecials = new SpecialKind[step.Falls.Count];
                for (int f = 0; f < step.Falls.Count; f++)
                {
                    GridPos from = step.Falls[f].From;
                    fallingGems[f] = grid[from.X, from.Y];
                    fallingSpecials[f] = specials[from.X, from.Y];
                }
                foreach (MoveResult.GemFall fall in step.Falls)
                {
                    grid[fall.From.X, fall.From.Y] = BoardModel.Empty;
                    specials[fall.From.X, fall.From.Y] = SpecialKind.None;
                }
                for (int f = 0; f < step.Falls.Count; f++)
                {
                    GridPos to = step.Falls[f].To;
                    grid[to.X, to.Y] = fallingGems[f];
                    specials[to.X, to.Y] = fallingSpecials[f];
                }
                foreach (MoveResult.GemSpawn spawn in step.Spawns)
                {
                    grid[spawn.Cell.X, spawn.Cell.Y] = spawn.GemType;
                    specials[spawn.Cell.X, spawn.Cell.Y] = SpecialKind.None;
                }
                frames.Add(new Frame(FrameKind.Filled, (int[,])grid.Clone(), (SpecialKind[,])specials.Clone(), step, i));
            }

            if (result.Reshuffled && result.BoardAfterReshuffle != null)
            {
                SpecialKind[,] reshuffled = result.SpecialsAfterReshuffle != null
                    ? (SpecialKind[,])result.SpecialsAfterReshuffle.Clone()
                    : new SpecialKind[before.GetLength(0), before.GetLength(1)];
                frames.Add(new Frame(FrameKind.Reshuffled, (int[,])result.BoardAfterReshuffle.Clone(), reshuffled, null, -1));
            }
            return frames;
        }
    }
}
