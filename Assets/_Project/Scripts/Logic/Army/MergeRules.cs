using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Army
{
    /// <summary>
    /// Decides which units a merge consumes (docs/GDD.md §6): MergeCount
    /// identical units (class + tier) become one unit of the next tier, up to
    /// MaxTier.
    /// </summary>
    public static class MergeRules
    {
        /// <summary>
        /// Returns the units consumed when merging into <paramref name="target"/>,
        /// target first, or an empty list if the merge is not possible.
        /// <paramref name="candidates"/> is searched in order, so callers list the
        /// units they would rather give up first.
        /// </summary>
        public static List<ArmyUnit> FindMergeSet(ArmyUnit target, IEnumerable<ArmyUnit> candidates, ArmyRules rules)
        {
            var result = new List<ArmyUnit>();
            if (target == null || target.Tier >= rules.MaxTier)
            {
                return result;
            }
            result.Add(target);
            foreach (ArmyUnit unit in candidates)
            {
                if (result.Count == rules.MergeCount)
                {
                    break;
                }
                if (unit != target && target.IsSameKind(unit))
                {
                    result.Add(unit);
                }
            }
            if (result.Count < rules.MergeCount)
            {
                result.Clear();
            }
            return result;
        }
    }
}
