namespace PuzzleKingdoms.Logic.Quests
{
    /// <summary>What a quest counts (PB-24). Values are stored in assets.</summary>
    public enum QuestKind
    {
        /// <summary>Stages won.</summary>
        WinStages = 0,
        /// <summary>Rally charges used in battle.</summary>
        UseRally = 1,
        /// <summary>Units recruited on the board.</summary>
        RecruitUnits = 2,
        /// <summary>Merges done in the War Camp.</summary>
        MergeUnits = 3,
        /// <summary>Gold earned from stage rewards.</summary>
        EarnGold = 4,
        /// <summary>Battles fought with a Tier 3 unit in the formation.</summary>
        ReachTier3 = 5,
    }
}
