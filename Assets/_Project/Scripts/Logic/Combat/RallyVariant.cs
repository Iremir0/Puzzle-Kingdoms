namespace PuzzleKingdoms.Logic.Combat
{
    /// <summary>What a commander's Rally does (PB-23, D-053).</summary>
    public enum RallyKind
    {
        /// <summary>The plain Rally of GDD §9: the army hits harder and faster. Also what a player without a commander has.</summary>
        Classic = 0,
        /// <summary>The front row takes less damage for a while.</summary>
        ShieldWall = 1,
        /// <summary>Volleys hit the enemy's back row.</summary>
        ArrowRain = 2,
        /// <summary>One blast on the densest enemy group.</summary>
        Fireball = 3,
    }

    /// <summary>
    /// One Rally variant: numbers only, the simulator reads them. A variant may add a smaller damage and
    /// attack-speed buff to its own effect. Everything is deterministic (no randomness), so a battle still
    /// replays exactly from the armies, the rules, the variant and the Rally input log.
    /// </summary>
    public sealed class RallyVariant
    {
        public string Id = "classic";
        public string DisplayName = "Rally";
        public RallyKind Kind = RallyKind.Classic;
        public float DurationSeconds = 5f;
        public float DamageBonus = 0.5f;
        public float AttackSpeedBonus = 0.5f;

        /// <summary>Shield Wall: share of damage the player's front formation row does not take while Rally lasts.</summary>
        public float FrontRowDamageReduction;

        /// <summary>Arrow Rain: how many volleys, spread evenly over the duration, and the share of max HP each takes off.</summary>
        public int VolleyCount;
        public float VolleyPercentMaxHp;

        /// <summary>Fireball: radius in cells around the densest group's centre and the share of max HP taken off once.</summary>
        public float BlastRadius;
        public float BlastPercentMaxHp;

        /// <summary>The plain Rally with the numbers of <paramref name="rules"/>.</summary>
        public static RallyVariant Classic(BattleRules rules) => new RallyVariant
        {
            DurationSeconds = rules.RallyDurationSeconds,
            DamageBonus = rules.RallyDamageBonus,
            AttackSpeedBonus = rules.RallyAttackSpeedBonus,
        };
    }
}
