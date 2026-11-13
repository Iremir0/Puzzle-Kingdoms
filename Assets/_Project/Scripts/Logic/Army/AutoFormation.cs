using System.Collections.Generic;
using System.Linq;

namespace PuzzleKingdoms.Logic.Army
{
    /// <summary>
    /// Suggests a formation from the bench: melee in front, long range in the
    /// back, the rest in the middle, strongest (highest tier) units first. A
    /// starting point for the player and for headless balance runs; the player
    /// can rearrange afterwards.
    /// </summary>
    public static class AutoFormation
    {
        public const float MeleeRangeMax = 1.5f;
        public const float BackRowRangeMin = 3.75f;

        /// <summary>Deploys bench units into empty cells. Returns how many were deployed.</summary>
        public static int Fill(Roster roster)
        {
            int deployed = 0;
            List<ArmyUnit> candidates = roster.Bench
                .OrderByDescending(u => u.Tier)
                .ThenBy(u => u.Uid)
                .ToList();
            foreach (ArmyUnit unit in candidates)
            {
                List<GridPos> empty = roster.Formation.EmptyCells();
                if (empty.Count == 0)
                {
                    break;
                }
                GridPos cell = PickCell(empty, PreferredRow(unit.Def));
                if (roster.Deploy(unit, cell))
                {
                    deployed++;
                }
            }
            return deployed;
        }

        public static int PreferredRow(UnitDef def)
        {
            if (def.AttackRange <= MeleeRangeMax)
            {
                return 0;
            }
            return def.AttackRange >= BackRowRangeMin ? 2 : 1;
        }

        /// <summary>Closest row to the preferred one; center lane first, then left, then right.</summary>
        private static GridPos PickCell(List<GridPos> empty, int preferredRow)
        {
            return empty
                .OrderBy(c => System.Math.Abs(c.Y - preferredRow))
                .ThenBy(c => c.Y)
                .ThenBy(c => c.X == 1 ? 0 : c.X == 0 ? 1 : 2)
                .First();
        }
    }
}
