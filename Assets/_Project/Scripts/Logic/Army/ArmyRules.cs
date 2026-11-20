using System;

namespace PuzzleKingdoms.Logic.Army
{
    /// <summary>War Camp balancing numbers (docs/GDD.md §6).</summary>
    public sealed class ArmyRules
    {
        /// <summary>
        /// HP and damage multiplier per tier of the player's units; index 0 is tier 1. Its length is the max tier.
        /// Each tier beats the units merged into it (D-068).
        /// </summary>
        public float[] TierMultipliers = { 1f, 4f, 13f };
        /// <summary>
        /// The same for enemy units (Expedition and Daily Challenge enemies come in tiers 2 and 3). Enemies are not
        /// made by merging, so they keep the gentler curve the modes were tuned with (D-068).
        /// </summary>
        public float[] EnemyTierMultipliers = { 1f, 1.8f, 3.2f };
        /// <summary>Identical units (same class and tier) consumed by one merge.</summary>
        public int MergeCount = 3;

        public int MaxTier => TierMultipliers.Length;

        public float TierMultiplier(int tier)
        {
            if (tier < 1 || tier > MaxTier)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), $"Tier out of range: {tier}");
            }
            return TierMultipliers[tier - 1];
        }

        /// <summary>The rules an enemy unit uses: <see cref="EnemyTierMultipliers"/> as its tier multipliers.</summary>
        public ArmyRules ForEnemies() => new ArmyRules
        {
            TierMultipliers = EnemyTierMultipliers,
            EnemyTierMultipliers = EnemyTierMultipliers,
            MergeCount = MergeCount,
        };
    }
}
