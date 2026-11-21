namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>Reward numbers (docs/GDD.md §5, §6, §10).</summary>
    public sealed class ProgressionRules
    {
        /// <summary>Gold for every unit still on the bench when the battle starts.</summary>
        public int DismissGoldPerUnit = 5;
        /// <summary>Gold for every Banner point earned after Rally charges were full.</summary>
        public int GoldPerOverflowRallyPoint = 1;
        /// <summary>Which stage opens which game system (PB-37, D-070).</summary>
        public FeatureUnlockRules Unlocks = FeatureUnlockRules.Default();
    }
}
