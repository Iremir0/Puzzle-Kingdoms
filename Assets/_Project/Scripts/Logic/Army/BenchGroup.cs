using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Army
{
    /// <summary>
    /// The bench units of one class and tier, shown as a single tile with a count
    /// in the War Camp (PB-9). <see cref="Units"/> keeps bench order, so
    /// <see cref="First"/> is the unit a tap or drag picks.
    /// </summary>
    public sealed class BenchGroup
    {
        public readonly UnitDef Def;
        public readonly int Tier;
        public readonly List<ArmyUnit> Units = new List<ArmyUnit>();

        public BenchGroup(UnitDef def, int tier)
        {
            Def = def;
            Tier = tier;
        }

        public int Count => Units.Count;

        public ArmyUnit First => Units[0];

        /// <summary>
        /// Groups <paramref name="bench"/> by class and tier: classes in
        /// <paramref name="classOrder"/> (unit ids; unknown classes after them, in
        /// bench order), then tier high to low.
        /// </summary>
        public static List<BenchGroup> FromBench(IReadOnlyList<ArmyUnit> bench, IReadOnlyList<string> classOrder)
        {
            var groups = new List<BenchGroup>();
            var firstSeen = new Dictionary<UnitDef, int>();
            foreach (ArmyUnit unit in bench)
            {
                if (!firstSeen.ContainsKey(unit.Def))
                {
                    firstSeen[unit.Def] = firstSeen.Count;
                }
                BenchGroup group = groups.Find(g => g.Def == unit.Def && g.Tier == unit.Tier);
                if (group == null)
                {
                    group = new BenchGroup(unit.Def, unit.Tier);
                    groups.Add(group);
                }
                group.Units.Add(unit);
            }

            int ClassRank(UnitDef def)
            {
                int index = -1;
                for (int i = 0; classOrder != null && i < classOrder.Count; i++)
                {
                    if (classOrder[i] == def.Id)
                    {
                        index = i;
                        break;
                    }
                }
                return index >= 0 ? index : (classOrder?.Count ?? 0) + firstSeen[def];
            }

            groups.Sort((a, b) =>
            {
                int byClass = ClassRank(a.Def).CompareTo(ClassRank(b.Def));
                return byClass != 0 ? byClass : b.Tier.CompareTo(a.Tier);
            });
            return groups;
        }
    }
}
