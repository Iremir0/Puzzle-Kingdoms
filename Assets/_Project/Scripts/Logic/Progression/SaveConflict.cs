using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>What to do when the phone and the cloud both hold a save (PB-31, D-065).</summary>
    public enum SaveChoice
    {
        /// <summary>The cloud has nothing, or nothing newer: keep this phone's save (and upload it).</summary>
        KeepLocal = 0,
        /// <summary>The cloud save is ahead or newer: take it.</summary>
        UseCloud = 1,
        /// <summary>Each side has progress the other lacks: the player chooses.</summary>
        Ask = 2,
        /// <summary>The cloud save comes from a newer version of the game: leave it alone.</summary>
        CloudTooNew = 3,
    }

    /// <summary>
    /// The rule for two saves (PB-31, D-065): progress first, then time. Progress is what cannot be bought back:
    /// the best stars of every stage, building levels, the expedition record, achievements and chests claimed.
    /// A save that is ahead on something and behind on nothing wins; saves with the same progress go by the time
    /// they were written; saves that are each ahead somewhere make the player choose. Gold is not progress.
    /// </summary>
    public static class SaveConflict
    {
        public static SaveChoice Resolve(PlayerProfile.Snapshot local, DateTime localUtc, PlayerProfile.Snapshot cloud, DateTime cloudUtc)
        {
            if (cloud == null)
            {
                return SaveChoice.KeepLocal;
            }
            if (cloud.version > PlayerProfile.CurrentVersion)
            {
                return SaveChoice.CloudTooNew;
            }
            if (local == null)
            {
                return SaveChoice.UseCloud;
            }
            Dictionary<string, int> a = Marks(local);
            Dictionary<string, int> b = Marks(cloud);
            bool localAhead = Ahead(a, b);
            bool cloudAhead = Ahead(b, a);
            if (localAhead && cloudAhead)
            {
                return SaveChoice.Ask;
            }
            if (localAhead)
            {
                return SaveChoice.KeepLocal;
            }
            if (cloudAhead)
            {
                return SaveChoice.UseCloud;
            }
            return cloudUtc > localUtc ? SaveChoice.UseCloud : SaveChoice.KeepLocal;
        }

        /// <summary>True when <paramref name="x"/> has more of something than <paramref name="y"/>.</summary>
        private static bool Ahead(Dictionary<string, int> x, Dictionary<string, int> y)
        {
            foreach (KeyValuePair<string, int> pair in x)
            {
                y.TryGetValue(pair.Key, out int other);
                if (pair.Value > other)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Every kind of progress of a save as name → amount.</summary>
        public static Dictionary<string, int> Marks(PlayerProfile.Snapshot save)
        {
            var marks = new Dictionary<string, int>();
            int stages = Math.Min(save.stageIds?.Count ?? 0, save.stageStars?.Count ?? 0);
            for (int i = 0; i < stages; i++)
            {
                marks["stage:" + save.stageIds[i]] = save.stageStars[i];
            }
            int buildings = Math.Min(save.buildingIds?.Count ?? 0, save.buildingLevels?.Count ?? 0);
            for (int i = 0; i < buildings; i++)
            {
                marks["building:" + save.buildingIds[i]] = save.buildingLevels[i];
            }
            marks["expedition:best"] = save.bestExpeditionDepth;
            marks["expedition:won"] = save.expeditionsWon;
            marks["chests"] = save.chestsClaimed;
            int claimed = 0;
            if (save.achievementClaimed != null)
            {
                foreach (bool done in save.achievementClaimed)
                {
                    claimed += done ? 1 : 0;
                }
            }
            marks["achievements"] = claimed;
            marks["supporter"] = save.supporter ? 1 : 0;
            if (save.commanderId != null && save.commanderId.Length > 0)
            {
                marks["commander"] = 1;
            }
            marks["daily"] = save.dailyDays?.Count ?? 0;
            return marks;
        }

        /// <summary>The numbers a conflict dialog shows for one save.</summary>
        public static (int stars, int stages, int gold) Summary(PlayerProfile.Snapshot save)
        {
            int stars = 0;
            int stages = 0;
            int count = Math.Min(save.stageIds?.Count ?? 0, save.stageStars?.Count ?? 0);
            for (int i = 0; i < count; i++)
            {
                stars += Math.Max(0, Math.Min(3, save.stageStars[i]));
                stages += save.stageStars[i] > 0 ? 1 : 0;
            }
            return (stars, stages, Math.Max(0, save.gold));
        }
    }
}
