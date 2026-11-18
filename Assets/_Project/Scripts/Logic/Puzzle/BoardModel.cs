using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Puzzle
{
    /// <summary>
    /// Pure Match-3 board rules: no engine objects, no animation, seeded randomness.
    /// Coordinates are (x, y) with (0, 0) at the top-left; gems fall towards
    /// larger y. Gem types are ints in [0, GemTypeCount); their meaning is
    /// defined by the caller (see <see cref="PuzzleSession"/>).
    ///
    /// A match is a straight line of 3 or more, or a 2×2 square, of one type (D-028).
    /// Lines of 4 earn bonus points (counted by the caller), a square leaves a
    /// <see cref="SpecialKind.Blast"/> gem and a line of 5 or more leaves a
    /// <see cref="SpecialKind.Prism"/> gem; both stay on the board until used.
    /// A gem can also be encased in stone (D-032): the first match it takes part in
    /// (or the first special effect that reaches it) only breaks the stone, the gem
    /// stays; the next one clears it.
    /// </summary>
    public sealed class BoardModel
    {
        public const int Empty = -1;
        public const int MinMatch = 3;
        public const int FourRun = 4;
        public const int PrismRun = 5;
        private const int MaxGenerationAttempts = 100;
        private const int MaxCascades = 100;

        public readonly int Width;
        public readonly int Height;
        public readonly int GemTypeCount;

        private readonly int[] _cells;
        private readonly SpecialKind[] _specials;
        private readonly bool[] _stones;
        private readonly IRandom _random;
        private bool[] _cracked = new bool[0];
        private readonly List<int> _crackedOrder = new List<int>();

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
            _specials = new SpecialKind[width * height];
            _stones = new bool[width * height];
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = Empty;
            }
        }

        // --- Queries ------------------------------------------------------

        public bool IsInside(GridPos cell) =>
            cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;

        public int GetGem(GridPos cell) => _cells[Index(cell)];

        public SpecialKind GetSpecial(GridPos cell) => _specials[Index(cell)];

        /// <summary>True if the gem on <paramref name="cell"/> is still encased in stone.</summary>
        public bool IsStone(GridPos cell) => _stones[Index(cell)];

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

        /// <summary>Returns a copy of the special gems: result[x, y] = kind (None for plain gems).</summary>
        public SpecialKind[,] ToSpecialGrid()
        {
            var grid = new SpecialKind[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    grid[x, y] = _specials[y * Width + x];
                }
            }
            return grid;
        }

        /// <summary>Returns a copy of the stone layer: result[x, y] = true while the gem is encased.</summary>
        public bool[,] ToStoneGrid()
        {
            var grid = new bool[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    grid[x, y] = _stones[y * Width + x];
                }
            }
            return grid;
        }

        /// <summary>
        /// True if swapping a and b would create at least one match, or if one of them
        /// is a <see cref="SpecialKind.Prism"/> (a Prism can always be swapped).
        /// </summary>
        public bool WouldSwapMatch(GridPos a, GridPos b)
        {
            if (!IsInside(a) || !IsInside(b) || !AreAdjacent(a, b))
            {
                return false;
            }
            if (GetGem(a) == Empty || GetGem(b) == Empty)
            {
                return false;
            }
            if (GetSpecial(a) == SpecialKind.Prism || GetSpecial(b) == SpecialKind.Prism)
            {
                return true;
            }
            if (GetGem(a) == GetGem(b))
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

        /// <summary>Finds every match on the board (lines and squares), grouped by connected shape.</summary>
        public List<MatchGroup> FindMatchGroups()
        {
            // Union-find over cell indices; -1 means "not part of any match".
            var parent = new int[_cells.Length];
            for (int i = 0; i < parent.Length; i++)
            {
                parent[i] = -1;
            }
            var runs = new List<(int first, int length)>();
            var squares = new List<int>();
            for (int y = 0; y < Height; y++)
            {
                MarkRuns(parent, runs, new GridPos(0, y), GridPos.Right, Width);
            }
            for (int x = 0; x < Width; x++)
            {
                MarkRuns(parent, runs, new GridPos(x, 0), GridPos.Down, Height);
            }
            MarkSquares(parent, squares);

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
            foreach ((int first, int length) in runs)
            {
                MatchGroup group = groupsByRoot[FindRoot(parent, first)];
                group.LongestRun = Math.Max(group.LongestRun, length);
            }
            foreach (int topLeft in squares)
            {
                groupsByRoot[FindRoot(parent, topLeft)].HasSquare = true;
            }
            return groups;
        }

        // --- Commands -----------------------------------------------------

        /// <summary>Fills the board with no ready-made matches and at least one move.</summary>
        public void Generate()
        {
            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                for (int i = 0; i < _specials.Length; i++)
                {
                    _specials[i] = SpecialKind.None;
                    _stones[i] = false;
                }
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
        /// Like <see cref="Generate()"/>, but keeps every cell of <paramref name="placed"/> that is not
        /// <see cref="BoardLayout.Random"/> (a hand-placed board, PB-19). Random cells are drawn again until
        /// no match stands and a move exists.
        /// </summary>
        public void Generate(int[,] placed)
        {
            if (placed == null)
            {
                Generate();
                return;
            }
            if (placed.GetLength(0) != Width || placed.GetLength(1) != Height)
            {
                throw new ArgumentException("Layout size mismatch");
            }
            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                for (int i = 0; i < _specials.Length; i++)
                {
                    _specials[i] = SpecialKind.None;
                    _stones[i] = false;
                }
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int fixedType = placed[x, y];
                        _cells[y * Width + x] = fixedType >= 0 ? fixedType : PickTypeWithoutMatch(x, y);
                    }
                }
                if (FindMatchGroups().Count == 0 && HasValidMove())
                {
                    return;
                }
            }
            throw new InvalidOperationException("Could not fill a board around the placed gems without a match and with a move");
        }

        /// <summary>
        /// Replaces the board content. rows[y][x] = gem type. For tests, tutorials
        /// and handcrafted boards. Does not resolve existing matches and removes
        /// all special gems (add them back with <see cref="SetSpecial"/>).
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
                    _specials[y * Width + x] = SpecialKind.None;
                    _stones[y * Width + x] = false;
                }
            }
        }

        /// <summary>Encases the gem on <paramref name="cell"/> in stone (or frees it). For tests and handcrafted boards.</summary>
        public void SetStone(GridPos cell, bool stone)
        {
            if (GetGem(cell) == Empty)
            {
                throw new ArgumentException($"No gem on {cell}");
            }
            _stones[Index(cell)] = stone;
        }

        /// <summary>
        /// Encases up to <paramref name="count"/> random gems in stone, never two next to each
        /// other (so a match can not be left standing after its stones break). Returns how
        /// many were placed. Stones do not change gem types, so no match appears.
        /// </summary>
        public int PlaceStones(int count)
        {
            int placed = 0;
            for (int attempt = 0; attempt < count * 20 && placed < count; attempt++)
            {
                int index = _random.Range(0, _cells.Length);
                GridPos cell = CellAt(index);
                if (_stones[index] || _cells[index] == Empty || HasStoneNeighbour(cell))
                {
                    continue;
                }
                _stones[index] = true;
                placed++;
            }
            return placed;
        }

        private bool HasStoneNeighbour(GridPos cell)
        {
            foreach (GridPos direction in new[] { GridPos.Up, GridPos.Down, GridPos.Left, GridPos.Right })
            {
                GridPos neighbour = cell + direction;
                if (IsInside(neighbour) && _stones[Index(neighbour)])
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Makes the gem on <paramref name="cell"/> a special gem. For tests and handcrafted boards.</summary>
        public void SetSpecial(GridPos cell, SpecialKind kind)
        {
            if (GetGem(cell) == Empty)
            {
                throw new ArgumentException($"No gem on {cell}");
            }
            _specials[Index(cell)] = kind;
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

            // A swapped Prism fires once, together with the first wave of matches.
            var swapEffects = new List<(GridPos origin, int targetType)>();
            if (GetSpecial(a) == SpecialKind.Prism)
            {
                swapEffects.Add((a, GetGem(b)));
            }
            if (GetSpecial(b) == SpecialKind.Prism)
            {
                swapEffects.Add((b, GetGem(a)));
            }

            for (int cascade = 0; cascade < MaxCascades; cascade++)
            {
                List<MatchGroup> groups = FindMatchGroups();
                if (groups.Count == 0 && swapEffects.Count == 0)
                {
                    break;
                }
                var step = new MoveResult.CascadeStep();
                step.Groups.AddRange(groups);
                ResolveStep(step, groups, swapEffects, cascade == 0 ? new[] { a, b } : Array.Empty<GridPos>(), result);
                swapEffects.Clear();
                ApplyGravity(step.Falls);
                Refill(step.Spawns);
                result.Steps.Add(step);
            }

            if (!HasValidMove())
            {
                Shuffle();
                result.Reshuffled = true;
                result.BoardAfterReshuffle = ToGrid();
                result.SpecialsAfterReshuffle = ToSpecialGrid();
                result.StonesAfterReshuffle = ToStoneGrid();
            }
            return result;
        }

        /// <summary>
        /// Rearranges the existing gems (special gems travel with them) so the board
        /// has no matches and a valid move. Falls back to a fresh board if none is
        /// found quickly.
        /// </summary>
        public void Shuffle()
        {
            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                for (int i = _cells.Length - 1; i > 0; i--)
                {
                    int j = _random.Range(0, i + 1);
                    (_cells[i], _cells[j]) = (_cells[j], _cells[i]);
                    (_specials[i], _specials[j]) = (_specials[j], _specials[i]);
                    (_stones[i], _stones[j]) = (_stones[j], _stones[i]);
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
            (_specials[ia], _specials[ib]) = (_specials[ib], _specials[ia]);
            (_stones[ia], _stones[ib]) = (_stones[ib], _stones[ia]);
        }

        /// <summary>
        /// One wave of clearing: decides which cells empty (matches plus special
        /// effects, chained), which match leaves a special gem behind, and records it.
        /// </summary>
        private void ResolveStep(
            MoveResult.CascadeStep step,
            List<MatchGroup> groups,
            List<(GridPos origin, int targetType)> swapEffects,
            GridPos[] swapCells,
            MoveResult result)
        {
            var toClear = new bool[_cells.Length];
            _cracked = new bool[_cells.Length];
            _crackedOrder.Clear();
            var kept = new bool[_cells.Length];
            var fired = new bool[_cells.Length];
            var queue = new Queue<int>();

            // Which match leaves a special gem, and where.
            foreach (MatchGroup group in groups)
            {
                if (group.IsFourMatch)
                {
                    result.FourMatchesByType[group.GemType] = result.FourMatchCount(group.GemType) + 1;
                }
                SpecialKind kind = group.Creates;
                if (kind == SpecialKind.None)
                {
                    continue;
                }
                int anchor = ChooseAnchor(group, swapCells);
                if (anchor >= 0)
                {
                    kept[anchor] = true;
                    step.Created.Add(new MoveResult.SpecialCreated(CellAt(anchor), group.GemType, kind));
                }
            }

            // Seeds: every matched cell and everything a swapped Prism removes.
            foreach (MatchGroup group in groups)
            {
                foreach (GridPos cell in group.Cells)
                {
                    Mark(Index(cell), toClear, kept, queue);
                }
            }
            foreach ((GridPos origin, int targetType) in swapEffects)
            {
                int originIndex = Index(origin);
                fired[originIndex] = true;
                step.Triggers.Add(new MoveResult.SpecialTrigger(SpecialKind.Prism, origin));
                MarkType(targetType, toClear, kept, queue);
                Mark(originIndex, toClear, kept, queue);
            }

            // Chain reaction: special gems that get cleared fire in turn.
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                SpecialKind kind = _specials[index];
                if (kind == SpecialKind.None || fired[index])
                {
                    continue;
                }
                fired[index] = true;
                GridPos origin = CellAt(index);
                step.Triggers.Add(new MoveResult.SpecialTrigger(kind, origin));
                if (kind == SpecialKind.Blast)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            var neighbour = new GridPos(origin.X + dx, origin.Y + dy);
                            if (IsInside(neighbour))
                            {
                                Mark(Index(neighbour), toClear, kept, queue);
                            }
                        }
                    }
                }
                else if (kind == SpecialKind.Prism)
                {
                    MarkType(_cells[index], toClear, kept, queue);
                }
            }

            // Stones that were hit break: the gem stays and is no longer encased.
            _crackedOrder.Sort();
            foreach (int index in _crackedOrder)
            {
                _stones[index] = false;
                step.Cracked.Add(CellAt(index));
            }

            // Apply: clear, then place the new special gems.
            for (int index = 0; index < toClear.Length; index++)
            {
                if (!toClear[index])
                {
                    continue;
                }
                int type = _cells[index];
                result.ClearedByType[type] = result.ClearedCount(type) + 1;
                step.Cleared.Add(CellAt(index));
                _cells[index] = Empty;
                _specials[index] = SpecialKind.None;
            }
            foreach (MoveResult.SpecialCreated created in step.Created)
            {
                _specials[Index(created.Cell)] = created.Kind;
            }
        }

        /// <summary>
        /// The cell of a group that becomes the special gem: a swapped cell if the
        /// group contains one, otherwise the middle cell; never a cell that already
        /// holds a special gem. Returns -1 if every cell does.
        /// </summary>
        private int ChooseAnchor(MatchGroup group, GridPos[] swapCells)
        {
            foreach (GridPos cell in swapCells)
            {
                if (group.Cells.Contains(cell) && _specials[Index(cell)] == SpecialKind.None && !_stones[Index(cell)])
                {
                    return Index(cell);
                }
            }
            int middle = group.Size / 2;
            for (int i = 0; i < group.Size; i++)
            {
                GridPos cell = group.Cells[(middle + i) % group.Size];
                if (_specials[Index(cell)] == SpecialKind.None && !_stones[Index(cell)])
                {
                    return Index(cell);
                }
            }
            return -1;
        }

        /// <summary>
        /// Marks a cell for clearing. A gem still in stone is not cleared: its stone breaks
        /// (once per wave) and the gem stays.
        /// </summary>
        private void Mark(int index, bool[] toClear, bool[] kept, Queue<int> queue)
        {
            if (_cells[index] == Empty || toClear[index] || kept[index])
            {
                return;
            }
            if (_stones[index])
            {
                if (!_cracked[index])
                {
                    _cracked[index] = true;
                    _crackedOrder.Add(index);
                }
                return;
            }
            toClear[index] = true;
            queue.Enqueue(index);
        }

        private void MarkType(int gemType, bool[] toClear, bool[] kept, Queue<int> queue)
        {
            for (int index = 0; index < _cells.Length; index++)
            {
                if (_cells[index] == gemType)
                {
                    Mark(index, toClear, kept, queue);
                }
            }
        }

        private GridPos CellAt(int index) => new GridPos(index % Width, index / Width);

        private int PickTypeWithoutMatch(int x, int y)
        {
            var options = new List<int>(GemTypeCount);
            for (int gemType = 0; gemType < GemTypeCount; gemType++)
            {
                bool rowMatch = x >= 2 && _cells[y * Width + x - 1] == gemType
                    && _cells[y * Width + x - 2] == gemType;
                bool columnMatch = y >= 2 && _cells[(y - 1) * Width + x] == gemType
                    && _cells[(y - 2) * Width + x] == gemType;
                bool squareMatch = x >= 1 && y >= 1 && _cells[y * Width + x - 1] == gemType
                    && _cells[(y - 1) * Width + x] == gemType
                    && _cells[(y - 1) * Width + x - 1] == gemType;
                if (!rowMatch && !columnMatch && !squareMatch)
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
            if (vertical >= MinMatch)
            {
                return true;
            }
            // Any of the four 2×2 blocks that contain the cell.
            for (int dy = -1; dy <= 0; dy++)
            {
                for (int dx = -1; dx <= 0; dx++)
                {
                    if (IsSquareOf(cell.X + dx, cell.Y + dy, gemType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>True if the 2×2 block with top-left (x, y) is on the board and all of gemType.</summary>
        private bool IsSquareOf(int x, int y, int gemType)
        {
            return x >= 0 && y >= 0 && x + 1 < Width && y + 1 < Height
                && _cells[y * Width + x] == gemType
                && _cells[y * Width + x + 1] == gemType
                && _cells[(y + 1) * Width + x] == gemType
                && _cells[(y + 1) * Width + x + 1] == gemType;
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
        private void MarkRuns(int[] parent, List<(int first, int length)> runs, GridPos start, GridPos direction, int length)
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
                    runs.Add((first, runLength));
                }
                runStart = i;
            }
        }

        /// <summary>Unions the four cells of every 2×2 block of one type; records its top-left cell.</summary>
        private void MarkSquares(int[] parent, List<int> squares)
        {
            for (int y = 0; y + 1 < Height; y++)
            {
                for (int x = 0; x + 1 < Width; x++)
                {
                    int gemType = _cells[y * Width + x];
                    if (gemType == Empty || !IsSquareOf(x, y, gemType))
                    {
                        continue;
                    }
                    int topLeft = y * Width + x;
                    foreach (int index in new[] { topLeft, topLeft + 1, topLeft + Width, topLeft + Width + 1 })
                    {
                        if (parent[index] == -1)
                        {
                            parent[index] = index;
                        }
                        Union(parent, topLeft, index);
                    }
                    squares.Add(topLeft);
                }
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

        /// <summary>Moves gems (and their special marks) down into empty cells, recording the moves in column order.</summary>
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
                        _specials[writeY * Width + x] = _specials[y * Width + x];
                        _stones[writeY * Width + x] = _stones[y * Width + x];
                        _cells[y * Width + x] = Empty;
                        _stones[y * Width + x] = false;
                        _specials[y * Width + x] = SpecialKind.None;
                        falls.Add(new MoveResult.GemFall(new GridPos(x, y), new GridPos(x, writeY)));
                    }
                    writeY--;
                }
            }
        }

        /// <summary>Fills empty cells (all at the top after gravity) with random plain gems.</summary>
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
                    _stones[y * Width + x] = false;
                    _specials[y * Width + x] = SpecialKind.None;
                    spawns.Add(new MoveResult.GemSpawn(new GridPos(x, y), gemType, emptyCount));
                }
            }
        }
    }
}
