namespace PuzzleKingdoms.Logic.Army
{
    public enum DamageType
    {
        Physical = 0,
        Magic = 1,
    }

    /// <summary>
    /// Tier 1 stats of a unit class (player or enemy, see D-015). Built from a
    /// UnitDefinition asset by the Data layer; tiers come from
    /// <see cref="ArmyRules.TierMultipliers"/>.
    /// </summary>
    public sealed class UnitDef
    {
        public string Id = "";
        public string DisplayName = "";
        public int MaxHp = 100;
        public int Damage = 10;
        public DamageType DamageType = DamageType.Physical;
        /// <summary>Seconds between attacks.</summary>
        public float AttackInterval = 1f;
        /// <summary>Attack range in battlefield cells.</summary>
        public float AttackRange = 1f;
        /// <summary>Cells per second.</summary>
        public float MoveSpeed = 1f;
        /// <summary>Fraction of Physical damage blocked, in [0, 1).</summary>
        public float Armor;
        /// <summary>Fraction of Magic damage blocked, in [0, 1).</summary>
        public float Ward;

        /// <summary>
        /// A copy with HP and damage multiplied (rounded, damage at least 1); everything else
        /// stays. Used to make a stage's enemies stronger or weaker through data (D-033).
        /// </summary>
        public UnitDef Scaled(float hpMultiplier, float damageMultiplier) => new UnitDef
        {
            Id = Id,
            DisplayName = DisplayName,
            MaxHp = System.Math.Max(1, (int)System.Math.Round(MaxHp * hpMultiplier, System.MidpointRounding.AwayFromZero)),
            Damage = System.Math.Max(1, (int)System.Math.Round(Damage * damageMultiplier, System.MidpointRounding.AwayFromZero)),
            DamageType = DamageType,
            AttackInterval = AttackInterval,
            AttackRange = AttackRange,
            MoveSpeed = MoveSpeed,
            Armor = Armor,
            Ward = Ward,
        };
    }
}
