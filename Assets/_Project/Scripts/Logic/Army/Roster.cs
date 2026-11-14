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

        /// <summary>
        /// What <see cref="MergeAll"/> would do, without doing it: per class and tier,
        /// how many merges happen. Merges chain: three tier 1 merges make three tier 2
        /// units, which merge again. Ordered by tier, then by first recruitment.
        /// </summary>
        public List<MergePlanEntry> PlanMergeAll()
        {
            var plan = new List<MergePlanEntry>();
            List<ArmyUnit> units = AllUnits();
            units.Sort((a, b) => a.Uid.CompareTo(b.Uid));
            var defs = new List<UnitDef>();
            foreach (ArmyUnit unit in units)
            {
                if (!defs.Contains(unit.Def))
                {
                    defs.Add(unit.Def);
                }
            }

            for (int tier = 1; tier < Rules.MaxTier; tier++)
            {
                foreach (UnitDef def in defs)
                {
                    int count = 0;
                    foreach (ArmyUnit unit in units)
                    {
                        count += unit.Def == def && unit.Tier == tier ? 1 : 0;
                    }
                    int merges = count / Rules.MergeCount;
                    if (merges > 0)
                    {
                        plan.Add(new MergePlanEntry(def, tier, merges));
                        // The new units count as recruits of the next tier for the next pass.
                        for (int i = 0; i < merges; i++)
                        {
                            units.Add(new ArmyUnit(def, tier + 1, int.MaxValue));
                        }
                    }
                }
            }
            return plan;
        }

        /// <summary>True if <see cref="MergeAll"/> would merge anything.</summary>
        public bool CanMergeAny => PlanMergeAll().Count > 0;

        /// <summary>
        /// Merges everything that can be merged, lowest tier first, until nothing is left
        /// to merge. A deployed unit is the merge target when the class has one, so the
        /// result keeps its place (D-016 still applies: bench copies are consumed first).
        /// Returns the new units in the order they were made.
        /// </summary>
        public List<ArmyUnit> MergeAll()
        {
            var created = new List<ArmyUnit>();
            for (int tier = 1; tier < Rules.MaxTier; tier++)
            {
                foreach (UnitDef def in DefsAtTier(tier))
                {
                    while (true)
                    {
                        ArmyUnit target = PickMergeTarget(def, tier);
                        ArmyUnit merged = target == null ? null : Merge(target);
                        if (merged == null)
                        {
                            break;
                        }
                        created.Add(merged);
                    }
                }
            }
            return created;
        }

        private List<UnitDef> DefsAtTier(int tier)
        {
            var defs = new List<UnitDef>();
            List<ArmyUnit> units = AllUnits();
            units.Sort((a, b) => a.Uid.CompareTo(b.Uid));
            foreach (ArmyUnit unit in units)
            {
                if (unit.Tier == tier && !defs.Contains(unit.Def))
                {
                    defs.Add(unit.Def);
                }
            }
            return defs;
        }

        /// <summary>A deployed unit of the kind if there is one, otherwise the first bench copy.</summary>
        private ArmyUnit PickMergeTarget(UnitDef def, int tier)
        {
            foreach (ArmyUnit unit in Formation.Units())
            {
                if (unit.Def == def && unit.Tier == tier)
                {
                    return unit;
                }
            }
            foreach (ArmyUnit unit in _bench)
            {
                if (unit.Def == def && unit.Tier == tier)
                {
                    return unit;
                }
            }
            return null;
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
