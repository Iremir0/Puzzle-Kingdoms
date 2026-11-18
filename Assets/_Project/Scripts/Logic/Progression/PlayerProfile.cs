using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Kingdom;

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
            /// <summary>Version 2 (D-040): tutorial prompts already shown.</summary>
            public List<string> seenTutorialSteps = new List<string>();
            public bool tutorialSkipped;
            /// <summary>Version 3 (D-052): building levels, ids and levels side by side.</summary>
            public List<string> buildingIds = new List<string>();
            public List<int> buildingLevels = new List<int>();
        }

        /// <summary>3 = building levels added (D-052), 2 = tutorial progress (D-040); older files load with none.</summary>
        public const int CurrentVersion = 3;

        public event Action Changed;

        private readonly Dictionary<string, int> _bestStars = new Dictionary<string, int>();
        private readonly List<string> _seenSteps = new List<string>();
        private readonly Dictionary<string, int> _buildingLevels = new Dictionary<string, int>();

        public int Gold { get; private set; }

        /// <summary>True after "Skip tutorial": no more prompts until the tutorial is reset.</summary>
        public bool TutorialSkipped { get; private set; }

        public IReadOnlyList<string> SeenTutorialSteps => _seenSteps;

        public bool HasSeenStep(string stepId) => _seenSteps.Contains(stepId);

        /// <summary>Remembers that a prompt was shown, so it never shows again.</summary>
        public void MarkStepSeen(string stepId)
        {
            if (string.IsNullOrEmpty(stepId) || _seenSteps.Contains(stepId))
            {
                return;
            }
            _seenSteps.Add(stepId);
            Changed?.Invoke();
        }

        public void SkipTutorial()
        {
            if (!TutorialSkipped)
            {
                TutorialSkipped = true;
                Changed?.Invoke();
            }
        }

        /// <summary>Shows every prompt again (Settings → Reset tutorial); stars and gold stay.</summary>
        public void ResetTutorial()
        {
            _seenSteps.Clear();
            TutorialSkipped = false;
            Changed?.Invoke();
        }

        public int BuildingLevel(string buildingId) => _buildingLevels.TryGetValue(buildingId, out int level) ? level : 0;

        /// <summary>Spends the gold for the next level of a building. False at the cap, for an unknown building or without the gold.</summary>
        public bool TryUpgradeBuilding(KingdomRules rules, string buildingId)
        {
            int level = BuildingLevel(buildingId);
            int cost = rules.CostToUpgrade(buildingId, level);
            if (cost < 0 || cost > Gold)
            {
                return false;
            }
            Gold -= cost;
            _buildingLevels[buildingId] = level + 1;
            Changed?.Invoke();
            return true;
        }

        /// <summary>The bonuses of the buildings as they stand now.</summary>
        public KingdomBonuses Bonuses(KingdomRules rules) => rules.BonusesFor(BuildingLevel);

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
            var snapshot = new Snapshot
            {
                gold = Gold,
                seenTutorialSteps = new List<string>(_seenSteps),
                tutorialSkipped = TutorialSkipped,
            };
            foreach (KeyValuePair<string, int> building in _buildingLevels)
            {
                snapshot.buildingIds.Add(building.Key);
                snapshot.buildingLevels.Add(building.Value);
            }
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
            if (snapshot.seenTutorialSteps != null)
            {
                foreach (string step in snapshot.seenTutorialSteps)
                {
                    if (!string.IsNullOrEmpty(step) && !profile._seenSteps.Contains(step))
                    {
                        profile._seenSteps.Add(step);
                    }
                }
            }
            profile.TutorialSkipped = snapshot.tutorialSkipped;
            if (snapshot.buildingIds != null && snapshot.buildingLevels != null)
            {
                int buildings = Math.Min(snapshot.buildingIds.Count, snapshot.buildingLevels.Count);
                for (int i = 0; i < buildings; i++)
                {
                    if (!string.IsNullOrEmpty(snapshot.buildingIds[i]))
                    {
                        profile._buildingLevels[snapshot.buildingIds[i]] = Math.Max(0, snapshot.buildingLevels[i]);
                    }
                }
            }
            return profile;
        }
    }
}
