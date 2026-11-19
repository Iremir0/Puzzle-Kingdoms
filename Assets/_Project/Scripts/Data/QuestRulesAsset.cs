using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Quests;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>
    /// The quest pool (PB-24, D-060): daily quests, achievements and the star chest. Select
    /// <c>Data/Quests.asset</c> and edit the numbers in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Quest Rules", fileName = "Quests")]
    public sealed class QuestRulesAsset : ScriptableObject
    {
        [Serializable]
        public sealed class QuestData
        {
            [Tooltip("Saved in the profile: never rename a quest players may have progress in.")]
            public string id = "";
            public QuestKind kind;
            [Min(1)] public int target = 1;
            [Min(0)] public int rewardGold = 50;
        }

        [Tooltip("How many daily quests a day has.")]
        [SerializeField, Range(1, 6)] private int dailyCount = 3;
        [Tooltip("A star chest is earned for every this many stars.")]
        [SerializeField, Min(1)] private int starsPerChest = 30;
        [SerializeField, Min(0)] private int chestGold = 150;
        [Tooltip("Daily quests are drawn from this pool; kinds differ within a day.")]
        [SerializeField] private List<QuestData> daily = new List<QuestData>();
        [Tooltip("One-time quests that count over the whole game.")]
        [SerializeField] private List<QuestData> achievements = new List<QuestData>();

        [NonSerialized] private QuestRules _rules;

        public QuestRules Rules
        {
            get
            {
                if (_rules == null)
                {
                    _rules = new QuestRules { DailyCount = dailyCount, StarsPerChest = starsPerChest, ChestGold = chestGold };
                    foreach (QuestData data in daily)
                    {
                        _rules.Daily.Add(ToDef(data));
                    }
                    foreach (QuestData data in achievements)
                    {
                        _rules.Achievements.Add(ToDef(data));
                    }
                }
                return _rules;
            }
        }

        private static QuestDef ToDef(QuestData data) =>
            new QuestDef { Id = data.id, Kind = data.kind, Target = data.target, RewardGold = data.rewardGold };

        private void OnValidate() => _rules = null;
    }
}
