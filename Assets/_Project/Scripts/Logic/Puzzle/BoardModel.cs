using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// Pure Match-3 board rules: no engine objects, no animation, seeded randomness.
    /// Coordinates are (x, y) with (0, 0) at the top-left; gems fall towards
    /// larger y. Gem types are ints in [0, GemTypeCount); their meaning is
    /// defined by the caller (see <see cref="PuzzleSession"/>).
    /// </summary>
    public sealed class BoardModel
    {
        public const int Empty = -1;
        public const int MinMatch = 3;
        private const int MaxGenerationAttempts = 100;
        private const int MaxCascades = 100;

        public readonly int Width;
        public readonly int Height;
        public readonly int GemTypeCount;

        private readonly int[] _cells;
        private readonly IRandom _random;

        public BoardModel(int width, int height, int gemTypeCount, IRandom random)
        {
            if (width < MinMatch || height < MinMatch)
            {
                throw new ArgumentException("Board too small");
            }
            if (gemTypeCount < 3)
            {
                throw new ArgumentException("Need at least 3 gem types to avoid forced matches");
            }
            Width = width;
            Height = height;
            GemTypeCount = gemTypeCount;
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _cells = new int[width * height];
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = Empty;
            }
        }

        // --- Queries ------------------------------------------------------

        public bool IsInside(GridPos cell) =>
            cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;

        public int GetGem(GridPos cell) => _cells[Index(cell)];

        public static bool AreAdjacent(GridPos a, GridPos b) => a.ManhattanDistance(b) == 1;

        /// <summary>Returns a copy of the board: result[x, y] = gem type.</summary>
        public int[,] ToGrid()
        {
            var grid = new int[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    grid[x, y] = _cells[y * Width + x];
                }
            }
            return grid;
        }

        /// <summary>True if swapping a and b would create at least one match.</summary>
        public bool WouldSwapMatch(GridPos a, GridPos b)
        {
            if (!IsInside(a) || !IsInside(b) || !AreAdjacent(a, b) || GetGem(a) == GetGem(b))
            {
                return false;
            }
            Swap(a, b);
            bool result = HasMatchAt(a) || HasMatchAt(b);
            Swap(a, b);
            return result;
        }

        /// <summary>Finds one valid swap, or returns false when the board is stuck.</summary>
        public bool TryFindValidMove(out GridPos a, out GridPos b)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = new GridPos(x, y);
                    if (WouldSwapMatch(cell, cell + GridPos.Right))
                    {
                        a = cell;
                        b = cell + GridPos.Right;
                        return true;
                    }
                    if (WouldSwapMatch(cell, cell + GridPos.Down))
                    {
                        a = cell;
                        b = cell + GridPos.Down;
                        return true;
                    }
                }
            }
            a = default;
            b = default;
            return false;
        }

        public bool HasValidMove() => TryFindValidMove(out _, out _);

        /// <summary>Finds every match on the board, grouped by connected shape.</summary>
        public List<MatchGroup> FindMatchGroups()
        {
            // Union-find over cell indices; -1 means "not part of any match".
            var parent = new int[_cells.Length];
            for (int i = 0; i < parent.Length; i++)
            {
                parent[i] = -1;
            }
            for (int y = 0; y < Height; y++)
            {
                MarkRuns(parent, new GridPos(0, y), GridPos.Right, Width);
            }
            for (int x = 0; x < Width; x++)
            {
                MarkRuns(parent, new GridPos(x, 0), GridPos.Down, Height);
            }

            var groupsByRoot = new Dictionary<int, MatchGroup>();
            var groups = new List<MatchGroup>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    if (parent[index] == -1)
                    {
                        continue;
                    }
                    int root = FindRoot(parent, index);
                    if (!groupsByRoot.TryGetValue(root, out MatchGroup group))
                    {
                        group = new MatchGroup(_cells[index]);
                        groupsByRoot[root] = group;
                        groups.Add(group);
                    }
                    group.Cells.Add(new GridPos(x, y));
                }
            }
            return groups;
        }

        // --- Commands -----------------------------------------------------

        /// <summary>Fills the board with no ready-made matches and at least one move.</summary>
        public void Generate()
        {
            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        _cells[y * Width + x] = PickTypeWithoutMatch(x, y);
                    }
                }
                if (HasValidMove())
                {
                    return;
                }
            }
            throw new InvalidOperationException("Could not generate a board with a valid move");
        }

        /// <summary>
        /// Replaces the board content. rows[y][x] = gem type. For tests, tutorials
        /// and handcrafted boards. Does not resolve existing matches.
        /// </summary>
        public void LoadRows(int[][] rows)
        {
            if (rows.Length != Height)
            {
                throw new ArgumentException("Row count mismatch");
            }
            for (int y = 0; y < Height; y++)
            {
                if (rows[y].Length != Width)
                {
                    throw new ArgumentException("Column count mismatch");
                }
                for (int x = 0; x < Width; x++)
                {
                    _cells[y * Width + x] = rows[y][x];
                }
            }
        }

        /// <summary>
        /// Attempts a player swap and fully resolves all cascades. Returns a
        /// result with IsValid == false (board untouched) when the swap is not
        /// allowed or creates no match.
        /// </summary>
        public MoveResult TrySwap(GridPos a, GridPos b)
        {
            var result = new MoveResult { SwapA = a, SwapB = b };
            if (!WouldSwapMatch(a, b))
            {
                return result;
            }
            result.IsValid = true;
            Swap(a, b);

            for (int cascade = 0; cascade < MaxCascades; cascade++)
            {
                List<MatchGroup> groups = FindMatchGroups();
                if (groups.Count == 0)
                {
                    break;
                }
                var step = new MoveResult.CascadeStep();
                step.Groups.AddRange(groups);
                foreach (MatchGroup group in groups)
                {
                    foreach (GridPos cell in group.Cells)
                    {
                        _cells[Index(cell)] = Empty;
                    }
                    result.ClearedByType[group.GemType] = result.ClearedCount(group.GemType) + group.Size;
                }
                ApplyGravity(step.Falls);
                Refill(step.Spawns);
                result.Steps.Add(step);
            }

            if (!HasValidMove())
            {
                Shuffle();
                result.Reshuffled = true;
                result.BoardAfterReshuffle = ToGrid();
            }
            return result;
        }

        /// <summary>
        /// Rearranges the existing gems so the board has no matches and a valid
        /// move. Falls back to a fresh board if none is found quickly.
        /// </summary>
        public void Shuffle()
        {
            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                for (int i = _cells.Length - 1; i > 0; i--)
                {
                    int j = _random.Range(0, i + 1);
                    (_cells[i], _cells[j]) = (_cells[j], _cells[i]);
                }
                if (FindMatchGroups().Count == 0 && HasValidMove())
                {
                    return;
                }
            }
            Generate();
        }

        // --- Internals ----------------------------------------------------

        private int Index(GridPos cell)
        {
            if (!IsInside(cell))
            {
                throw new ArgumentOutOfRangeException(nameof(cell), $"Cell outside board: {cell}");
            }
            return cell.Y * Width + cell.X;
        }

        private void Swap(GridPos a, GridPos b)
        {
            int ia = Index(a);
            int ib = Index(b);
            (_cells[ia], _cells[ib]) = (_cells[ib], _cells[ia]);
        }

        private int PickTypeWithoutMatch(int x, int y)
        {
            var options = new List<int>(GemTypeCount);
            for (int gemType = 0; gemType < GemTypeCount; gemType++)
            {
                bool rowMatch = x >= 2 && _cells[y * Width + x - 1] == gemType
                    && _cells[y * Width + x - 2] == gemType;
                bool columnMatch = y >= 2 && _cells[(y - 1) * Width + x] == gemType
                    && _cells[(y - 2) * Width + x] == gemType;
                if (!rowMatch && !columnMatch)
                {
                    options.Add(gemType);
                }
            }
            return options[_random.Range(0, options.Count)];
        }

        private bool HasMatchAt(GridPos cell)
        {
            int gemType = GetGem(cell);
            if (gemType == Empty)
            {
                return false;
            }
            int horizontal = 1 + CountSame(cell, GridPos.Left, gemType) + CountSame(cell, GridPos.Right, gemType);
            if (horizontal >= MinMatch)
            {
                return true;
            }
            int vertical = 1 + CountSame(cell, GridPos.Up, gemType) + CountSame(cell, GridPos.Down, gemType);
            return vertical >= MinMatch;
        }

        private int CountSame(GridPos start, GridPos direction, int gemType)
        {
            int count = 0;
            GridPos cell = start + direction;
            while (IsInside(cell) && GetGem(cell) == gemType)
            {
                count++;
                cell += direction;
            }
            return count;
        }

        /// <summary>Scans one line and unions the cells of every run of MinMatch or more.</summary>
        private void MarkRuns(int[] parent, GridPos start, GridPos direction, int length)
        {
            int runStart = 0;
            for (int i = 1; i <= length; i++)
            {
                bool runContinues = i < length
                    && GetGem(start + direction * i) != Empty
                    && GetGem(start + direction * i) == GetGem(start + direction * runStart);
                if (runContinues)
                {
                    continue;
                }
                int runLength = i - runStart;
                if (runLength >= MinMatch && GetGem(start + direction * runStart) != Empty)
                {
                    int first = Index(start + direction * runStart);
                    if (parent[first] == -1)
                    {
                        parent[first] = first;
                    }
                    for (int k = runStart + 1; k < i; k++)
                    {
                        int index = Index(start + direction * k);
                        if (parent[index] == -1)
                        {
                            parent[index] = index;
                        }
                        Union(parent, first, index);
                    }
                }
                runStart = i;
            }
        }

        private static int FindRoot(int[] parent, int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }
            return index;
        }

        private static void Union(int[] parent, int a, int b)
        {
            int rootA = FindRoot(parent, a);
            int rootB = FindRoot(parent, b);
            if (rootA != rootB)
            {
                parent[rootB] = rootA;
            }
        }

        /// <summary>Moves gems down into empty cells, recording the moves in column order.</summary>
        private void ApplyGravity(List<MoveResult.GemFall> falls)
        {
            for (int x = 0; x < Width; x++)
            {
                int writeY = Height - 1;
                for (int y = Height - 1; y >= 0; y--)
                {
                    int gemType = _cells[y * Width + x];
                    if (gemType == Empty)
                    {
                        continue;
                    }
                    if (y != writeY)
                    {
                        _cells[writeY * Width + x] = gemType;
                        _cells[y * Width + x] = Empty;
                        falls.Add(new MoveResult.GemFall(new GridPos(x, y), new GridPos(x, writeY)));
                    }
                    writeY--;
                }
            }
        }

        /// <summary>Fills empty cells (all at the top after gravity) with random gems.</summary>
        private void Refill(List<MoveResult.GemSpawn> spawns)
        {
            for (int x = 0; x < Width; x++)
            {
                int emptyCount = 0;
                while (emptyCount < Height && _cells[emptyCount * Width + x] == Empty)
                {
                    emptyCount++;
                }
                for (int y = 0; y < emptyCount; y++)
                {
                    int gemType = _random.Range(0, GemTypeCount);
                    _cells[y * Width + x] = gemType;
                    spawns.Add(new MoveResult.GemSpawn(new GridPos(x, y), gemType, emptyCount));
                }
            }
        }
    }
}
