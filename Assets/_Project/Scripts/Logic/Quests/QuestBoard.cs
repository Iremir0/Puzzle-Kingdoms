using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Quests
{
    /// <summary>A quest with where the player stands.</summary>
    public readonly struct QuestStatus
    {
        public readonly QuestDef Def;
        public readonly int Progress;
        public readonly bool Claimed;

        public QuestStatus(QuestDef def, int progress, bool claimed)
        {
            Def = def;
            Progress = Math.Min(progress, def.Target);
            Claimed = claimed;
        }

        public bool Complete => Progress >= Def.Target;
        public bool CanClaim => Complete && !Claimed;
    }

    /// <summary>
    /// Daily quests, achievements and star chests (PB-24, D-060). Works on a <see cref="QuestLog"/>; the day
    /// is passed in as a string (Logic has no clock), and the daily draw comes from the day itself, so every
    /// restart sees the same quests for a given day.
    /// </summary>
    public sealed class QuestBoard
    {
        private readonly QuestRules _rules;
        private readonly QuestLog _log;

        public QuestBoard(QuestRules rules, QuestLog log)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Draws the daily quests when the day has changed (or on the first call) and brings the achievement
        /// lists in line with the pool. Returns true if anything changed.
        /// </summary>
        public bool Refresh(string today)
        {
            bool changed = SyncAchievements();
            if (_log.Day == today && _log.DailyIds.Count > 0)
            {
                return changed;
            }
            _log.Day = today;
            _log.DailyIds.Clear();
            _log.DailyProgress.Clear();
            _log.DailyClaimed.Clear();
            foreach (QuestDef quest in PickDaily(today))
            {
                _log.DailyIds.Add(quest.Id);
                _log.DailyProgress.Add(0);
                _log.DailyClaimed.Add(false);
            }
            return true;
        }

        /// <summary>The day's quests: different kinds where the pool allows, the same for the same day string.</summary>
        public List<QuestDef> PickDaily(string today)
        {
            var random = new SeededRandom(Hash(today));
            var pool = new List<QuestDef>(_rules.Daily);
            var picked = new List<QuestDef>();
            int wanted = Math.Min(_rules.DailyCount, pool.Count);
            while (picked.Count < wanted)
            {
                List<QuestDef> fresh = pool.FindAll(q => !picked.Exists(p => p.Kind == q.Kind));
                List<QuestDef> from = fresh.Count > 0 ? fresh : pool;
                QuestDef choice = from[random.Range(0, from.Count)];
                picked.Add(choice);
                pool.Remove(choice);
            }
            return picked;
        }

        private static ulong Hash(string text)
        {
            ulong hash = 1469598103934665603UL; // FNV-1a
            foreach (char c in text ?? "")
            {
                hash = (hash ^ c) * 1099511628211UL;
            }
            return hash;
        }

        private bool SyncAchievements()
        {
            bool changed = false;
            var ids = new List<string>();
            var progress = new List<int>();
            var claimed = new List<bool>();
            foreach (QuestDef quest in _rules.Achievements)
            {
                int at = _log.AchievementIds.IndexOf(quest.Id);
                ids.Add(quest.Id);
                progress.Add(at >= 0 ? _log.AchievementProgress[at] : 0);
                claimed.Add(at >= 0 && _log.AchievementClaimed[at]);
                changed |= at < 0;
            }
            changed |= ids.Count != _log.AchievementIds.Count;
            _log.AchievementIds = ids;
            _log.AchievementProgress = progress;
            _log.AchievementClaimed = claimed;
            return changed;
        }

        /// <summary>Counts a finished attempt towards today's quests and the achievements.</summary>
        public void Record(QuestEvent e)
        {
            for (int i = 0; i < _log.DailyIds.Count; i++)
            {
                QuestDef quest = Find(_rules.Daily, _log.DailyIds[i]);
                if (quest != null)
                {
                    _log.DailyProgress[i] = Math.Min(quest.Target, _log.DailyProgress[i] + e.Amount(quest.Kind));
                }
            }
            for (int i = 0; i < _log.AchievementIds.Count; i++)
            {
                QuestDef quest = Find(_rules.Achievements, _log.AchievementIds[i]);
                if (quest != null)
                {
                    _log.AchievementProgress[i] = Math.Min(quest.Target, _log.AchievementProgress[i] + e.Amount(quest.Kind));
                }
            }
        }

        public List<QuestStatus> Daily()
        {
            var result = new List<QuestStatus>();
            for (int i = 0; i < _log.DailyIds.Count; i++)
            {
                QuestDef quest = Find(_rules.Daily, _log.DailyIds[i]);
                if (quest != null)
                {
                    result.Add(new QuestStatus(quest, _log.DailyProgress[i], _log.DailyClaimed[i]));
                }
            }
            return result;
        }

        public List<QuestStatus> Achievements()
        {
            var result = new List<QuestStatus>();
            for (int i = 0; i < _log.AchievementIds.Count; i++)
            {
                QuestDef quest = Find(_rules.Achievements, _log.AchievementIds[i]);
                if (quest != null)
                {
                    result.Add(new QuestStatus(quest, _log.AchievementProgress[i], _log.AchievementClaimed[i]));
                }
            }
            return result;
        }

        /// <summary>Claims a finished quest (daily or achievement); false if it is unknown, not finished or claimed already.</summary>
        public bool TryClaim(string questId, out int gold)
        {
            gold = 0;
            int d = _log.DailyIds.IndexOf(questId);
            if (d >= 0)
            {
                QuestDef quest = Find(_rules.Daily, questId);
                if (quest == null || _log.DailyClaimed[d] || _log.DailyProgress[d] < quest.Target)
                {
                    return false;
                }
                _log.DailyClaimed[d] = true;
                gold = quest.RewardGold;
                return true;
            }
            int a = _log.AchievementIds.IndexOf(questId);
            if (a >= 0)
            {
                QuestDef quest = Find(_rules.Achievements, questId);
                if (quest == null || _log.AchievementClaimed[a] || _log.AchievementProgress[a] < quest.Target)
                {
                    return false;
                }
                _log.AchievementClaimed[a] = true;
                gold = quest.RewardGold;
                return true;
            }
            return false;
        }

        // ---- star chests ----

        public int ChestsEarned(int totalStars) => _rules.StarsPerChest > 0 ? totalStars / _rules.StarsPerChest : 0;

        public int ChestsReady(int totalStars) => Math.Max(0, ChestsEarned(totalStars) - _log.ChestsClaimed);

        /// <summary>Stars into the current chest, for a progress bar (0 .. StarsPerChest - 1).</summary>
        public int StarsTowardsNextChest(int totalStars) => _rules.StarsPerChest > 0 ? totalStars % _rules.StarsPerChest : 0;

        public bool TryClaimChest(int totalStars, out int gold)
        {
            gold = 0;
            if (ChestsReady(totalStars) <= 0)
            {
                return false;
            }
            _log.ChestsClaimed++;
            gold = _rules.ChestGold;
            return true;
        }

        /// <summary>How many rewards wait to be claimed (the badge on the tab bar).</summary>
        public int ClaimableCount(int totalStars)
        {
            int count = ChestsReady(totalStars);
            foreach (QuestStatus status in Daily())
            {
                count += status.CanClaim ? 1 : 0;
            }
            foreach (QuestStatus status in Achievements())
            {
                count += status.CanClaim ? 1 : 0;
            }
            return count;
        }

        private static QuestDef Find(List<QuestDef> pool, string id) => pool.Find(q => q.Id == id);
    }
}
