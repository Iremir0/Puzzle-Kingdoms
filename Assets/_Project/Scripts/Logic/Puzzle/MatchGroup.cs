using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// A connected set of same-type gems cleared together. Horizontal and
    /// vertical runs (and 2×2 squares) that share a cell form one group, so
    /// L and T shapes are one group.
    /// </summary>
    public sealed class MatchGroup
    {
        public readonly int GemType;
        public readonly List<GridPos> Cells = new List<GridPos>();
        /// <summary>Length of the longest straight run in the group (0 for a lone square).</summary>
        public int LongestRun;
        /// <summary>True if the group contains a 2×2 block of its gem type.</summary>
        public bool HasSquare;

        public MatchGroup(int gemType)
        {
            GemType = gemType;
        }

        public int Size => Cells.Count;

        /// <summary>The special gem this group leaves behind (D-028): a line of 5+ beats a square.</summary>
        public SpecialKind Creates =>
            LongestRun >= BoardModel.PrismRun ? SpecialKind.Prism
            : HasSquare ? SpecialKind.Blast
            : SpecialKind.None;

        /// <summary>True for a plain line of exactly 4 (no special gem), which earns bonus points.</summary>
        public bool IsFourMatch => Creates == SpecialKind.None && LongestRun == BoardModel.FourRun;
    }
}
