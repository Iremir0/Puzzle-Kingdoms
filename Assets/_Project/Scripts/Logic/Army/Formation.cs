using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Stages;

namespace PuzzleKingdoms.Logic.Army
{
    /// <summary>
    /// The 4×4 battle formation (D-031). Cells are (x, y): x = lane (0 left ... 3 right),
    /// y = row (0 front ... 3 back). The front row faces the enemy.
    /// </summary>
    public sealed class Formation
    {
        public const int Size = 4;
        public static readonly GridPos NoCell = new GridPos(-1, -1);

        private readonly ArmyUnit[] _slots = new ArmyUnit[Size * Size];

        /// <summary>Builds an enemy formation from a stage's slots, optionally with stronger or weaker units.</summary>
        public static Formation FromEnemySlots(IEnumerable<EnemySlot> slots, float hpMultiplier = 1f, float damageMultiplier = 1f)
        {
            var result = new Formation();
            var scaled = new Dictionary<UnitDef, UnitDef>();
            foreach (EnemySlot slot in slots)
            {
                UnitDef def = slot.Unit;
                if (hpMultiplier != 1f || damageMultiplier != 1f)
                {
                    if (!scaled.TryGetValue(def, out UnitDef copy))
                    {
                        copy = def.Scaled(hpMultiplier, damageMultiplier);
                        scaled[def] = copy;
                    }
                    def = copy;
                }
                result.Place(new ArmyUnit(def, slot.Tier), slot.Cell);
            }
            return result;
        }

        /// <summary>The enemy army of a stage, with the stage's enemy strength applied (D-033).</summary>
        public static Formation ForStage(StageDef stage) =>
            FromEnemySlots(stage.Enemies, stage.EnemyHpMultiplier, stage.EnemyDamageMultiplier);

        public static bool IsInside(GridPos cell) =>
            cell.X >= 0 && cell.X < Size && cell.Y >= 0 && cell.Y < Size;

        public ArmyUnit GetUnit(GridPos cell) => _slots[Index(cell)];

        /// <summary>Puts a unit on a cell and returns the unit that was there (or null).</summary>
        public ArmyUnit Place(ArmyUnit unit, GridPos cell)
        {
            int index = Index(cell);
            ArmyUnit displaced = _slots[index];
            _slots[index] = unit;
            return displaced;
        }

        /// <summary>Empties a cell and returns the unit that was there (or null).</summary>
        public ArmyUnit Remove(GridPos cell) => Place(null, cell);

        /// <summary>Swaps the contents of two cells (either may be empty).</summary>
        public void Move(GridPos from, GridPos to)
        {
            int a = Index(from);
            int b = Index(to);
            (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
        }

        public GridPos FindCell(ArmyUnit unit)
        {
            if (unit == null)
            {
                return NoCell;
            }
            int index = Array.IndexOf(_slots, unit);
            return index == -1 ? NoCell : new GridPos(index % Size, index / Size);
        }

        public bool Contains(ArmyUnit unit) => FindCell(unit) != NoCell;

        /// <summary>Deployed units ordered front to back, left to right.</summary>
        public List<ArmyUnit> Units()
        {
            var result = new List<ArmyUnit>();
            foreach (ArmyUnit unit in _slots)
            {
                if (unit != null)
                {
                    result.Add(unit);
                }
            }
            return result;
        }

        public int Count => Units().Count;

        public List<GridPos> EmptyCells()
        {
            var result = new List<GridPos>();
            for (int index = 0; index < _slots.Length; index++)
            {
                if (_slots[index] == null)
                {
                    result.Add(new GridPos(index % Size, index / Size));
                }
            }
            return result;
        }

        private static int Index(GridPos cell)
        {
            if (!IsInside(cell))
            {
                throw new ArgumentOutOfRangeException(nameof(cell), $"Cell outside formation: {cell}");
            }
            return cell.Y * Size + cell.X;
        }
    }
}
