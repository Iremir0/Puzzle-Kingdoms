using System.Collections.Generic;
using System.Text;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// A hand-placed start board for a stage (PB-19, D-048), one text row per board row, top row first.
    /// Each character is one cell: <c>.</c> a random gem, <c>0</c>–<c>9</c> that gem type,
    /// <c>a</c>–<c>j</c> that gem type (0–9) in stone, <c>x</c> a random gem in stone. Random cells are
    /// filled so that no match stands at the start and a move exists.
    /// </summary>
    public static class BoardLayout
    {
        public const char RandomCell = '.';
        public const char RandomStone = 'x';
        public const int Random = -1;

        /// <summary>True when there is no layout or it only has random cells without stones.</summary>
        public static bool IsEmpty(IReadOnlyList<string> rows)
        {
            if (rows == null)
            {
                return true;
            }
            foreach (string row in rows)
            {
                foreach (char c in row ?? "")
                {
                    if (c != RandomCell && !char.IsWhiteSpace(c))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Reads a layout; on failure <paramref name="error"/> says why (wrong size, unknown character,
        /// a gem type the stage does not have, two stones side by side, a ready-made match).
        /// </summary>
        public static bool TryParse(IReadOnlyList<string> rows, int width, int height, int gemTypes,
            out int[,] gems, out bool[,] stones, out string error)
        {
            gems = new int[width, height];
            stones = new bool[width, height];
            error = null;
            if (rows == null || rows.Count != height)
            {
                error = $"the layout needs {height} rows";
                return false;
            }
            for (int y = 0; y < height; y++)
            {
                string row = rows[y] ?? "";
                if (row.Length != width)
                {
                    error = $"row {y + 1} needs {width} cells, it has {row.Length}";
                    return false;
                }
                for (int x = 0; x < width; x++)
                {
                    char c = row[x];
                    if (c == RandomCell || c == RandomStone)
                    {
                        gems[x, y] = Random;
                        stones[x, y] = c == RandomStone;
                    }
                    else if (c >= '0' && c <= '9' || c >= 'a' && c <= 'j')
                    {
                        int type = c <= '9' ? c - '0' : c - 'a';
                        if (type >= gemTypes)
                        {
                            error = $"row {y + 1}, cell {x + 1}: gem type {type}, the stage has {gemTypes}";
                            return false;
                        }
                        gems[x, y] = type;
                        stones[x, y] = c >= 'a';
                    }
                    else
                    {
                        error = $"row {y + 1}, cell {x + 1}: unknown '{c}' (use . 0-9 a-j x)";
                        return false;
                    }
                }
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (stones[x, y] && (x + 1 < width && stones[x + 1, y] || y + 1 < height && stones[x, y + 1]))
                    {
                        error = $"row {y + 1}, cell {x + 1}: stones may not touch (D-032)";
                        return false;
                    }
                }
            }
            List<GridPos> matched = FixedMatches(gems);
            if (matched.Count > 0)
            {
                error = $"the placed gems already match at row {matched[0].Y + 1}, cell {matched[0].X + 1}";
                return false;
            }
            return true;
        }

        /// <summary>Writes a board back as layout rows (for the stage editor).</summary>
        public static string[] Format(int[,] gems, bool[,] stones)
        {
            int width = gems.GetLength(0);
            int height = gems.GetLength(1);
            var rows = new string[height];
            for (int y = 0; y < height; y++)
            {
                var row = new StringBuilder(width);
                for (int x = 0; x < width; x++)
                {
                    int type = gems[x, y];
                    bool stone = stones != null && stones[x, y];
                    row.Append(type < 0 ? (stone ? RandomStone : RandomCell) : (char)((stone ? 'a' : '0') + type));
                }
                rows[y] = row.ToString();
            }
            return rows;
        }

        /// <summary>Cells of placed gems that already form a line of 3 or a 2×2 square among themselves.</summary>
        public static List<GridPos> FixedMatches(int[,] gems)
        {
            int width = gems.GetLength(0);
            int height = gems.GetLength(1);
            var found = new List<GridPos>();
            bool Same(int x, int y, int type) => x >= 0 && y >= 0 && x < width && y < height && gems[x, y] == type;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int type = gems[x, y];
                    if (type < 0)
                    {
                        continue;
                    }
                    bool row = Same(x + 1, y, type) && Same(x + 2, y, type);
                    bool column = Same(x, y + 1, type) && Same(x, y + 2, type);
                    bool square = Same(x + 1, y, type) && Same(x, y + 1, type) && Same(x + 1, y + 1, type);
                    if (row || column || square)
                    {
                        found.Add(new GridPos(x, y));
                    }
                }
            }
            return found;
        }
    }
}
