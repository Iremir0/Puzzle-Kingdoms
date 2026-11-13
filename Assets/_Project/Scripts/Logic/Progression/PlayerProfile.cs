using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>
    /// Persistent player progress: gold and best stars per stage. Campaign
    /// stages unlock in order: a stage is playable once the previous one has
    /// at least one star. Saving is done by the Flow layer via <see cref="Snapshot"/>.
    /// </summary>
    public sealed class PlayerProfile
    {
        /// <summary>Plain, serializer-friendly copy of the profile (JsonUtility-compatible).</summary>
        [Serializable]
        public sealed class Snapshot
        {
            public int version = CurrentVersion;
            public int gold;
            public List<string> stageIds = new List<string>();
            public List<int> stageStars = new List<int>();
        }

        public const int CurrentVersion = 1;

        public event Action Changed;

        private readonly Dictionary<string, int> _bestStars = new Dictionary<string, int>();

        public int Gold { get; private set; }

        public int BestStars(string stageId) => _bestStars.TryGetValue(stageId, out int stars) ? stars : 0;

        /// <summary>True if the stage at <paramref name="index"/> in <paramref name="campaignOrder"/> can be played.</summary>
        public bool IsUnlocked(IReadOnlyList<string> campaignOrder, int index)
        {
            if (index < 0 || index >= campaignOrder.Count)
            {
                return false;
            }
            return index == 0 || BestStars(campaignOrder[index - 1]) > 0;
        }

        /// <summary>Adds the reward's gold and keeps the best star count for the stage.</summary>
        public void Apply(string stageId, StageReward reward)
        {
            Gold += reward.TotalGold;
            if (reward.Stars > BestStars(stageId))
            {
                _bestStars[stageId] = reward.Stars;
            }
            Changed?.Invoke();
        }

        public Snapshot ToSnapshot()
        {
            var snapshot = new Snapshot { gold = Gold };
            foreach (KeyValuePair<string, int> pair in _bestStars)
            {
                snapshot.stageIds.Add(pair.Key);
                snapshot.stageStars.Add(pair.Value);
            }
            return snapshot;
        }

        public static PlayerProfile FromSnapshot(Snapshot snapshot)
        {
            var profile = new PlayerProfile();
            if (snapshot == null)
            {
                return profile;
            }
            if (snapshot.version > CurrentVersion)
            {
                throw new InvalidOperationException($"Save version {snapshot.version} is newer than {CurrentVersion}");
            }
            profile.Gold = Math.Max(0, snapshot.gold);
            int count = Math.Min(snapshot.stageIds.Count, snapshot.stageStars.Count);
            for (int i = 0; i < count; i++)
            {
                profile._bestStars[snapshot.stageIds[i]] = Math.Max(0, Math.Min(3, snapshot.stageStars[i]));
            }
            return profile;
        }
    }
}
