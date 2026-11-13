using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// Everything that happened during one player swap, in playback order.
    /// The puzzle view animates <see cref="Steps"/>; gameplay reads
    /// <see cref="ClearedByType"/>.
    /// </summary>
    public sealed class MoveResult
    {
        /// <summary>One wave of clearing: groups disappear, gems fall, new gems spawn.</summary>
        public sealed class CascadeStep
        {
            public readonly List<MatchGroup> Groups = new List<MatchGroup>();
            public readonly List<GemFall> Falls = new List<GemFall>();
            public readonly List<GemSpawn> Spawns = new List<GemSpawn>();
        }

        public readonly struct GemFall
        {
            public readonly GridPos From;
            public readonly GridPos To;

            public GemFall(GridPos from, GridPos to)
            {
                From = from;
                To = to;
            }
        }

        public readonly struct GemSpawn
        {
            public readonly GridPos Cell;
            public readonly int GemType;
            /// <summary>How many cells above the board the gem starts its fall from.</summary>
            public readonly int DropDistance;

            public GemSpawn(GridPos cell, int gemType, int dropDistance)
            {
                Cell = cell;
                GemType = gemType;
                DropDistance = dropDistance;
            }
        }

        public bool IsValid;
        public GridPos SwapA;
        public GridPos SwapB;
        public readonly List<CascadeStep> Steps = new List<CascadeStep>();
        /// <summary>Gem type → gems of that type cleared during the whole move.</summary>
        public readonly Dictionary<int, int> ClearedByType = new Dictionary<int, int>();
        /// <summary>True when the board had no valid move afterwards and was reshuffled.</summary>
        public bool Reshuffled;
        /// <summary>Board after the reshuffle (only set when <see cref="Reshuffled"/>).</summary>
        public int[,] BoardAfterReshuffle;

        /// <summary>Cascade waves after the first clear (0 = no cascade).</summary>
        public int CascadeCount => Math.Max(Steps.Count - 1, 0);

        public int ClearedCount(int gemType) =>
            ClearedByType.TryGetValue(gemType, out int count) ? count : 0;
    }
}
