using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// Everything that happened during one player swap, in playback order.
    /// The puzzle view animates <see cref="Steps"/>; gameplay reads
    /// <see cref="ClearedByType"/> and <see cref="FourMatchesByType"/>.
    /// </summary>
    public sealed class MoveResult
    {
        /// <summary>One wave of clearing: gems disappear, specials appear, gems fall, new gems spawn.</summary>
        public sealed class CascadeStep
        {
            /// <summary>The matches found this wave (lines of 3+ and 2×2 squares).</summary>
            public readonly List<MatchGroup> Groups = new List<MatchGroup>();
            /// <summary>
            /// Every cell emptied this wave: the matches plus whatever special gems
            /// destroyed. A cell that turns into a special gem is not listed.
            /// </summary>
            public readonly List<GridPos> Cleared = new List<GridPos>();
            /// <summary>Special gems that appear in place this wave (before gravity).</summary>
            public readonly List<SpecialCreated> Created = new List<SpecialCreated>();
            /// <summary>Special gems that went off this wave, in the order they fired.</summary>
            public readonly List<SpecialTrigger> Triggers = new List<SpecialTrigger>();
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

        public readonly struct SpecialCreated
        {
            public readonly GridPos Cell;
            public readonly int GemType;
            public readonly SpecialKind Kind;

            public SpecialCreated(GridPos cell, int gemType, SpecialKind kind)
            {
                Cell = cell;
                GemType = gemType;
                Kind = kind;
            }
        }

        public readonly struct SpecialTrigger
        {
            public readonly SpecialKind Kind;
            public readonly GridPos Origin;

            public SpecialTrigger(SpecialKind kind, GridPos origin)
            {
                Kind = kind;
                Origin = origin;
            }
        }

        public bool IsValid;
        public GridPos SwapA;
        public GridPos SwapB;
        public readonly List<CascadeStep> Steps = new List<CascadeStep>();
        /// <summary>Gem type → gems of that type cleared during the whole move (special effects included).</summary>
        public readonly Dictionary<int, int> ClearedByType = new Dictionary<int, int>();
        /// <summary>Gem type → plain lines of exactly 4 made during the whole move (bonus points, D-028).</summary>
        public readonly Dictionary<int, int> FourMatchesByType = new Dictionary<int, int>();
        /// <summary>True when the board had no valid move afterwards and was reshuffled.</summary>
        public bool Reshuffled;
        /// <summary>Board after the reshuffle (only set when <see cref="Reshuffled"/>).</summary>
        public int[,] BoardAfterReshuffle;
        /// <summary>Special gems after the reshuffle (only set when <see cref="Reshuffled"/>).</summary>
        public SpecialKind[,] SpecialsAfterReshuffle;

        /// <summary>Cascade waves after the first clear (0 = no cascade).</summary>
        public int CascadeCount => Math.Max(Steps.Count - 1, 0);

        public int ClearedCount(int gemType) =>
            ClearedByType.TryGetValue(gemType, out int count) ? count : 0;

        public int FourMatchCount(int gemType) =>
            FourMatchesByType.TryGetValue(gemType, out int count) ? count : 0;
    }
}
