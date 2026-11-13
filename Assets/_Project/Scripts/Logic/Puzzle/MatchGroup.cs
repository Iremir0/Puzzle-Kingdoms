using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// A connected set of same-type gems cleared together. Horizontal and
    /// vertical runs that share a cell (L/T shapes) form one group.
    /// </summary>
    public sealed class MatchGroup
    {
        public readonly int GemType;
        public readonly List<GridPos> Cells = new List<GridPos>();

        public MatchGroup(int gemType)
        {
            GemType = gemType;
        }

        public int Size => Cells.Count;
    }
}
