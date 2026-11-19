using System;

namespace PuzzleKingdoms.Logic.Army
{
    /// <summary>
    /// One recruited unit: a class definition plus a tier. Combat-time state
    /// (current hp, position) lives in the combat feature.
    /// </summary>
    public sealed class ArmyUnit
    {
        public const int NoUid = -1;

        public readonly UnitDef Def;
        public readonly int Tier;
        /// <summary>Unique within a Roster; lets views track a unit across moves and merges.</summary>
        public readonly int Uid;
        /// <summary>Share of its HP the unit starts a battle with, 0..1 (PB-26: wounds carry over in an expedition).</summary>
        public float HpFraction = 1f;

        public ArmyUnit(UnitDef def, int tier = 1, int uid = NoUid)
        {
            Def = def ?? throw new ArgumentNullException(nameof(def));
            Tier = tier;
            Uid = uid;
        }

        public int MaxHp(ArmyRules rules) => RoundToInt(Def.MaxHp * rules.TierMultiplier(Tier));

        public int Damage(ArmyRules rules) => RoundToInt(Def.Damage * rules.TierMultiplier(Tier));

        /// <summary>True when both units share class and tier, i.e. can merge together.</summary>
        public bool IsSameKind(ArmyUnit other) => other != null && other.Def == Def && other.Tier == Tier;

        internal static int RoundToInt(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
