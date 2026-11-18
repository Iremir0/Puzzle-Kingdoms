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
            /// <summary>The stage boss dropped stones onto the board (PB-20).</summary>
            StonesDropped,
        }

        /// <summary>One board state to show, with the cascade step that produced it.</summary>
        public sealed class Frame
        {
            public readonly FrameKind Kind;
            public readonly int[,] Grid;
            /// <summary>Special gems on the board: Specials[x, y].</summary>
            public readonly SpecialKind[,] Specials;
            /// <summary>Stone layer: Stones[x, y] is true while the gem on that cell is encased.</summary>
            public readonly bool[,] Stones;
            /// <summary>Chain layer (PB-20): Chains[x, y] is true while the gem on that cell is chained.</summary>
            public readonly bool[,] Chains;
            /// <summary>Ice layer (PB-20): layers on each cell.</summary>
            public readonly int[,] Ice;
            /// <summary>The cascade step for Cleared/Filled frames, otherwise null.</summary>
            public readonly MoveResult.CascadeStep Step;
            /// <summary>0 for the player's match, 1+ for cascades.</summary>
            public readonly int StepIndex;

            public Frame(FrameKind kind, int[,] grid, SpecialKind[,] specials, bool[,] stones, MoveResult.CascadeStep step,
                int stepIndex, bool[,] chains = null, int[,] ice = null)
            {
                Kind = kind;
                Grid = grid;
                Specials = specials;
                Stones = stones;
                Chains = chains ?? new bool[grid.GetLength(0), grid.GetLength(1)];
                Ice = ice ?? new int[grid.GetLength(0), grid.GetLength(1)];
                Step = step;
                StepIndex = stepIndex;
            }
        }

        /// <summary>
        /// All frames of a move, starting from the board before the swap. The layers before the swap
        /// (special gems, stones, chains, ice) default to none. Empty for an invalid move.
        /// </summary>
        public static List<Frame> Frames(int[,] before, MoveResult result, SpecialKind[,] beforeSpecials = null,
            bool[,] beforeStones = null, bool[,] beforeChains = null, int[,] beforeIce = null)
        {
            var frames = new List<Frame>();
            if (result == null || !result.IsValid)
            {
                return frames;
            }

            int width = before.GetLength(0);
            int height = before.GetLength(1);
            int[,] grid = (int[,])before.Clone();
            SpecialKind[,] specials = beforeSpecials != null ? (SpecialKind[,])beforeSpecials.Clone() : new SpecialKind[width, height];
            bool[,] stones = beforeStones != null ? (bool[,])beforeStones.Clone() : new bool[width, height];
            bool[,] chains = beforeChains != null ? (bool[,])beforeChains.Clone() : new bool[width, height];
            int[,] ice = beforeIce != null ? (int[,])beforeIce.Clone() : new int[width, height];

            Frame Snapshot(FrameKind kind, MoveResult.CascadeStep step, int index) => new Frame(kind, (int[,])grid.Clone(),
                (SpecialKind[,])specials.Clone(), (bool[,])stones.Clone(), step, index, (bool[,])chains.Clone(), (int[,])ice.Clone());

            GridPos a = result.SwapA;
            GridPos b = result.SwapB;
            (grid[a.X, a.Y], grid[b.X, b.Y]) = (grid[b.X, b.Y], grid[a.X, a.Y]);
            (specials[a.X, a.Y], specials[b.X, b.Y]) = (specials[b.X, b.Y], specials[a.X, a.Y]);
            (stones[a.X, a.Y], stones[b.X, b.Y]) = (stones[b.X, b.Y], stones[a.X, a.Y]);
            (chains[a.X, a.Y], chains[b.X, b.Y]) = (chains[b.X, b.Y], chains[a.X, a.Y]);
            frames.Add(Snapshot(FrameKind.Swapped, null, -1));

            for (int i = 0; i < result.Steps.Count; i++)
            {
                MoveResult.CascadeStep step = result.Steps[i];
                foreach (GridPos cell in step.Cleared)
                {
                    grid[cell.X, cell.Y] = BoardModel.Empty;
                    specials[cell.X, cell.Y] = SpecialKind.None;
                }
                foreach (GridPos cell in step.Cracked)
                {
                    stones[cell.X, cell.Y] = false;
                }
                foreach (GridPos cell in step.Unchained)
                {
                    chains[cell.X, cell.Y] = false;
                }
                foreach (GridPos cell in step.IceMelted)
                {
                    ice[cell.X, cell.Y] = System.Math.Max(0, ice[cell.X, cell.Y] - 1);
                }
                foreach (MoveResult.SpecialCreated created in step.Created)
                {
                    specials[created.Cell.X, created.Cell.Y] = created.Kind;
                }
                frames.Add(Snapshot(FrameKind.Cleared, step, i));

                // Read every falling gem first so the order of Falls does not matter. Ice stays on its cell.
                int count = step.Falls.Count;
                var fallingGems = new int[count];
                var fallingSpecials = new SpecialKind[count];
                var fallingStones = new bool[count];
                var fallingChains = new bool[count];
                for (int f = 0; f < count; f++)
                {
                    GridPos from = step.Falls[f].From;
                    fallingGems[f] = grid[from.X, from.Y];
                    fallingSpecials[f] = specials[from.X, from.Y];
                    fallingStones[f] = stones[from.X, from.Y];
                    fallingChains[f] = chains[from.X, from.Y];
                }
                foreach (MoveResult.GemFall fall in step.Falls)
                {
                    grid[fall.From.X, fall.From.Y] = BoardModel.Empty;
                    specials[fall.From.X, fall.From.Y] = SpecialKind.None;
                    stones[fall.From.X, fall.From.Y] = false;
                    chains[fall.From.X, fall.From.Y] = false;
                }
                for (int f = 0; f < count; f++)
                {
                    GridPos to = step.Falls[f].To;
                    grid[to.X, to.Y] = fallingGems[f];
                    specials[to.X, to.Y] = fallingSpecials[f];
                    stones[to.X, to.Y] = fallingStones[f];
                    chains[to.X, to.Y] = fallingChains[f];
                }
                foreach (MoveResult.GemSpawn spawn in step.Spawns)
                {
                    grid[spawn.Cell.X, spawn.Cell.Y] = spawn.GemType;
                    specials[spawn.Cell.X, spawn.Cell.Y] = SpecialKind.None;
                    stones[spawn.Cell.X, spawn.Cell.Y] = false;
                    chains[spawn.Cell.X, spawn.Cell.Y] = false;
                }
                frames.Add(Snapshot(FrameKind.Filled, step, i));
            }

            if (result.Reshuffled && result.BoardAfterReshuffle != null)
            {
                grid = (int[,])result.BoardAfterReshuffle.Clone();
                specials = result.SpecialsAfterReshuffle != null
                    ? (SpecialKind[,])result.SpecialsAfterReshuffle.Clone()
                    : new SpecialKind[width, height];
                stones = result.StonesAfterReshuffle != null ? (bool[,])result.StonesAfterReshuffle.Clone() : new bool[width, height];
                chains = result.ChainsAfterReshuffle != null ? (bool[,])result.ChainsAfterReshuffle.Clone() : new bool[width, height];
                frames.Add(Snapshot(FrameKind.Reshuffled, null, -1));
            }

            if (result.StonesDropped.Count > 0)
            {
                foreach (GridPos cell in result.StonesDropped)
                {
                    stones[cell.X, cell.Y] = true;
                }
                frames.Add(Snapshot(FrameKind.StonesDropped, null, -1));
            }
            return frames;
        }
    }
}
