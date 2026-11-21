using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Quests
{
    /// <summary>One quest or achievement: count <see cref="Target"/> of <see cref="Kind"/> to earn gold.</summary>
    public sealed class QuestDef
    {
        public string Id = "";
        public QuestKind Kind;
        public int Target = 1;
        public int RewardGold = 50;
    }

    /// <summary>The quest pool and numbers (PB-24), built from the Quests asset by the Data layer.</summary>
    public sealed class QuestRules
    {
        /// <summary>Quests the day's few are drawn from; they reset every day.</summary>
        public readonly List<QuestDef> Daily = new List<QuestDef>();
        /// <summary>One-time quests that count over the whole game.</summary>
        public readonly List<QuestDef> Achievements = new List<QuestDef>();
        /// <summary>How many daily quests a day has.</summary>
        public int DailyCount = 3;
        /// <summary>A star chest is earned for every this many stars (best stars summed over all stages).</summary>
        public int StarsPerChest = 12;
        public int ChestGold = 150;
    }

    /// <summary>What happened in one finished stage attempt, as far as quests care.</summary>
    public struct QuestEvent
    {
        public bool Won;
        public int RallyUsed;
        public int Recruited;
        public int Merges;
        public int Gold;
        public int MaxTier;

        /// <summary>How much of a quest of this kind the event is worth.</summary>
        public int Amount(QuestKind kind)
        {
            switch (kind)
            {
                case QuestKind.WinStages: return Won ? 1 : 0;
                case QuestKind.UseRally: return RallyUsed;
                case QuestKind.RecruitUnits: return Recruited;
                case QuestKind.MergeUnits: return Merges;
                case QuestKind.EarnGold: return Gold;
                case QuestKind.ReachTier3: return MaxTier >= 3 ? 1 : 0;
                default: return 0;
            }
        }
    }
}
