using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Army
{
    /// <summary>
    /// Every unit recruited during one stage: the unlimited bench plus the 3×3
    /// formation. A unit is always in exactly one of the two.
    /// </summary>
    public sealed class Roster
    {
        public event Action Changed;

        public readonly ArmyRules Rules;
        public readonly Formation Formation = new Formation();
        private readonly List<ArmyUnit> _bench = new List<ArmyUnit>();
        private int _nextUid;

        public Roster(ArmyRules rules)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public IReadOnlyList<ArmyUnit> Bench => _bench;

        /// <summary>Adds <paramref name="count"/> new tier 1 units to the bench and returns them.</summary>
        public List<ArmyUnit> Recruit(UnitDef def, int count = 1)
        {
            var added = new List<ArmyUnit>();
            for (int i = 0; i < count; i++)
            {
                ArmyUnit unit = NewUnit(def, 1);
                _bench.Add(unit);
                added.Add(unit);
            }
            if (count > 0)
            {
                Changed?.Invoke();
            }
            return added;
        }

        public List<ArmyUnit> AllUnits()
        {
            List<ArmyUnit> result = Formation.Units();
            result.AddRange(_bench);
            return result;
        }

        public bool Owns(ArmyUnit unit) => unit != null && (_bench.Contains(unit) || Formation.Contains(unit));

        /// <summary>
        /// Places a bench or deployed unit on <paramref name="cell"/>. A unit already
        /// there goes to the bench if <paramref name="unit"/> came from the bench, or
        /// swaps cells with it if <paramref name="unit"/> was deployed. Returns false
        /// if nothing changed.
        /// </summary>
        public bool Deploy(ArmyUnit unit, GridPos cell)
        {
            if (!Owns(unit) || !Formation.IsInside(cell))
            {
                return false;
            }
            GridPos from = Formation.FindCell(unit);
            if (from != Formation.NoCell)
            {
                Formation.Move(from, cell);
            }
            else
            {
                _bench.Remove(unit);
                ArmyUnit displaced = Formation.Place(unit, cell);
                if (displaced != null)
                {
                    _bench.Add(displaced);
                }
            }
            Changed?.Invoke();
            return true;
        }

        /// <summary>Moves the unit on <paramref name="cell"/> back to the bench. False if empty.</summary>
        public bool Undeploy(GridPos cell)
        {
            if (!Formation.IsInside(cell) || Formation.GetUnit(cell) == null)
            {
                return false;
            }
            _bench.Add(Formation.Remove(cell));
            Changed?.Invoke();
            return true;
        }

        public bool CanMerge(ArmyUnit target) => Owns(target) && MergeSet(target).Count > 0;

        /// <summary>
        /// Merges <paramref name="target"/> with identical units, preferring bench
        /// copies so the formation stays intact (D-016). The new unit takes the
        /// target's place (its cell, or the bench). Returns null if not possible.
        /// </summary>
        public ArmyUnit Merge(ArmyUnit target)
        {
            if (!Owns(target))
            {
                return null;
            }
            List<ArmyUnit> consumed = MergeSet(target);
            if (consumed.Count == 0)
            {
                return null;
            }

            ArmyUnit merged = NewUnit(target.Def, target.Tier + 1);
            GridPos targetCell = Formation.FindCell(target);
            foreach (ArmyUnit unit in consumed)
            {
                GridPos cell = Formation.FindCell(unit);
                if (cell != Formation.NoCell)
                {
                    Formation.Remove(cell);
                }
                else
                {
                    _bench.Remove(unit);
                }
            }
            if (targetCell != Formation.NoCell)
            {
                Formation.Place(merged, targetCell);
            }
            else
            {
                _bench.Add(merged);
            }
            Changed?.Invoke();
            return merged;
        }

        private List<ArmyUnit> MergeSet(ArmyUnit target)
        {
            var candidates = new List<ArmyUnit>(_bench);
            candidates.AddRange(Formation.Units());
            return MergeRules.FindMergeSet(target, candidates, Rules);
        }

        private ArmyUnit NewUnit(UnitDef def, int tier) => new ArmyUnit(def, tier, _nextUid++);
    }
}
