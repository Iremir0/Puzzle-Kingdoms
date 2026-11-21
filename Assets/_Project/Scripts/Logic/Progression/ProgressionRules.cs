namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>Reward numbers (docs/GDD.md §5, §6, §10).</summary>
    public sealed class ProgressionRules
    {
        /// <summary>Gold for every unit still on the bench when the battle starts.</summary>
        public int DismissGoldPerUnit = 5;
        /// <summary>Gold for every Banner point earned after Rally charges were full.</summary>
        public int GoldPerOverflowRallyPoint = 1;
        /// <summary>
        /// Share of a stage's gold reward paid when the stage was already won before (PB-38, D-071): the first win
        /// pays it all, replays pay this much, so new stages pay more than farming old ones.
        /// </summary>
        public float ReplayGoldShare = 0.5f;
        /// <summary>Which stage opens which game system (PB-37, D-070).</summary>
        public FeatureUnlockRules Unlocks = FeatureUnlockRules.Default();
    }
}
