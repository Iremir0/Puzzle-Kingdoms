using System;

namespace PuzzleKingdoms.Logic.Army
{
    /// <summary>War Camp balancing numbers (docs/GDD.md §6).</summary>
    public sealed class ArmyRules
    {
        /// <summary>HP and damage multiplier per tier; index 0 is tier 1. Its length is the max tier.</summary>
        public float[] TierMultipliers = { 1f, 1.8f, 3.2f };
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
    }
}
