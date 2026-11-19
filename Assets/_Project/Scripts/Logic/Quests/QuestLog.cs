using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Quests
{
    /// <summary>
    /// The quest state of a player (saved in the profile): today's quests with progress and claims, the
    /// achievements' lifetime progress, and how many star chests were opened. The lists run side by side.
    /// </summary>
    public sealed class QuestLog
    {
        /// <summary>The day the daily quests were drawn for ("yyyy-MM-dd"); empty before the first draw.</summary>
        public string Day = "";
        public List<string> DailyIds = new List<string>();
        public List<int> DailyProgress = new List<int>();
        public List<bool> DailyClaimed = new List<bool>();
        public List<string> AchievementIds = new List<string>();
        public List<int> AchievementProgress = new List<int>();
        public List<bool> AchievementClaimed = new List<bool>();
        public int ChestsClaimed;
    }
}
