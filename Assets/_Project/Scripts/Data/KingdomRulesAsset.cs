using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Kingdom;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>
    /// The Kingdom's buildings, level cap and cost curve (PB-22, docs/GDD.md §10). Select
    /// <c>Data/KingdomRules.asset</c> and edit the numbers in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Kingdom Rules", fileName = "KingdomRules")]
    public sealed class KingdomRulesAsset : ScriptableObject
    {
        [Serializable]
        public sealed class BuildingData
        {
            [Tooltip("Saved in the profile: never rename a building that players may already have upgraded.")]
            public string id = "";
            public string displayName = "";
            [Tooltip("What the building improves.")]
            public BuildingStat stat;
            [Tooltip("Id of the unit class it improves (warrior, archer, mage); empty for the Treasury.")]
            public string unitId = "";
            [Tooltip("0.03 = +3 % per level.")]
            [Range(0f, 0.2f)] public float bonusPerLevel = 0.03f;
            [Tooltip("Multiplies the shared cost curve.")]
            [Min(0.1f)] public float costFactor = 1f;
        }

        [SerializeField, Min(1)] private int maxLevel = 10;
        [Tooltip("Gold for level 1 of a building with cost factor 1.")]
        [SerializeField, Min(5)] private int baseCost = 60;
        [Tooltip("Each level costs this many times the one before.")]
        [SerializeField, Min(1f)] private float costGrowth = 1.4f;
        [SerializeField] private List<BuildingData> buildings = new List<BuildingData>();

        [NonSerialized] private KingdomRules _rules;

        public KingdomRules Rules
        {
            get
            {
                if (_rules == null)
                {
                    _rules = new KingdomRules { MaxLevel = maxLevel, BaseCost = baseCost, CostGrowth = costGrowth };
                    foreach (BuildingData data in buildings)
                    {
                        _rules.Buildings.Add(new BuildingRule
                        {
                            Id = data.id,
                            DisplayName = data.displayName,
                            Stat = data.stat,
                            UnitId = data.unitId ?? "",
                            BonusPerLevel = data.bonusPerLevel,
                            CostFactor = data.costFactor,
                        });
                    }
                }
                return _rules;
            }
        }

        private void OnValidate() => _rules = null;
    }
}
