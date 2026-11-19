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

        // PB-18 behaviours (D-051). All default to "off", so older units are unchanged.

        /// <summary>
        /// Fraction of damage from ranged attackers (range above <see cref="RangedThreshold"/>)
        /// blocked while this unit is the front-most of its side. Shield goblin.
        /// </summary>
        public float RangedDamageReduction;
        /// <summary>HP restored to the most hurt ally in range per heal. 0 = cannot heal. Shaman.</summary>
        public int HealAmount;
        /// <summary>Seconds between heals.</summary>
        public float HealInterval = 2f;
        /// <summary>Heal range in cells.</summary>
        public float HealRange = 3.5f;
        /// <summary>Picks the enemy in the back-most formation row first. Wolf rider.</summary>
        public bool TargetsBackRow;
        /// <summary>Other enemies this close to the target also take splash damage. 0 = none. Bomber.</summary>
        public float SplashRadius;
        /// <summary>Splash damage as a fraction of the main hit.</summary>
        public float SplashFraction = 0.5f;
        /// <summary>
        /// Enemies of this unit within this many cells target it before anyone else (taunt). 0 = no taunt. Defender (PB-21).
        /// </summary>
        public float TauntRadius;

        /// <summary>Attack range above which an attacker counts as ranged.</summary>
        public const float RangedThreshold = 1.5f;

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
            RangedDamageReduction = RangedDamageReduction,
            HealAmount = HealAmount,
            HealInterval = HealInterval,
            HealRange = HealRange,
            TargetsBackRow = TargetsBackRow,
            SplashRadius = SplashRadius,
            SplashFraction = SplashFraction,
            TauntRadius = TauntRadius,
        };
    }
}
